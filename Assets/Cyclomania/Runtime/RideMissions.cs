using System;
using SBPScripts;
using UnityEngine;

namespace Cyclomania
{
    [DefaultExecutionOrder(60)]
    public sealed class RideMissions : MonoBehaviour
    {
        public MissionSession Attempt { get; }=new MissionSession();
        public MissionCareer Career { get; private set; }=new MissionCareer();
        public MissionCareer Freestyle { get; private set; }=MissionCareer.FromJson("",FreestyleCatalog.Count);
        public bool IsFreestyle { get; private set; }
        public MissionCareer ActiveCareer=>IsFreestyle?Freestyle:Career;
        private string freestyleSaveKey;
        public string SaveNotice { get; private set; }="";
        public int Selected { get; private set; }
        public bool ResultsPending { get; private set; }
        private GameplayHud hud;
        private BicycleController bike;
        private CycleRecovery recovery;
        private StuntScoring scoring;
        private string saveKey;
        private float lastHeading;
        private bool heldForCountdown;
        private Rigidbody[] countdownBodies;
        private bool[] originalKinematic;
        private MissionState lastState;
        private GameObject marker, packageVisual, speedZone, checkpointGate;
        private GameObject rivalVisual, raceFinish;
        private Material rivalMaterial, tireMaterial, policeLightMaterial;
        private GameObject policeBeacon;
        private Material packageMaterial;
        private Material markerMaterial;
        public void Initialize(GameplayHud owner,string key)
        {
            hud=owner;saveKey=key;
            Career=MissionCareer.FromJson(string.IsNullOrEmpty(key)?"":PlayerPrefs.GetString(key,""));
            freestyleSaveKey=string.IsNullOrEmpty(key)?"":key+".Freestyle.v1";
            Freestyle=MissionCareer.FromJson(string.IsNullOrEmpty(freestyleSaveKey)?"":PlayerPrefs.GetString(freestyleSaveKey,""),FreestyleCatalog.Count);
            Attempt.Completed+=Complete;Bind();
        }
        private void Bind()
        {
            if(bike==hud.bicycle)return;
            Unbind();bike=hud.bicycle;
            if(bike==null)return;
            lastHeading=bike.transform.eulerAngles.y;
            recovery=bike.GetComponent<CycleRecovery>();scoring=bike.GetComponent<BicycleStunts>()?.Scoring;
            if(recovery!=null) {recovery.Crashed+=Crash;recovery.Respawned+=Reset;}
            if(scoring!=null) {scoring.ComboBanked+=BankScore;scoring.StuntLanded+=LandStunt;scoring.ComboFinished+=BankCombo;}
        }
        private void Unbind()
        {
            if(recovery!=null) {recovery.Crashed-=Crash;recovery.Respawned-=Reset;}
            if(scoring!=null) {scoring.ComboBanked-=BankScore;scoring.StuntLanded-=LandStunt;scoring.ComboFinished-=BankCombo;}
        }
        private bool CanCollect=>hud!=null && !hud.IsPaused && (!IsFreestyle || (bike!=null && FreestyleCatalog.Contains(Selected/3,bike.transform.position)));
        private void BankScore(int points) {if(CanCollect)Attempt.BankScore(points);}
        private void BankCombo(int points,int count) {if(CanCollect)Attempt.BankCombo(points,count);}
        private void LandStunt(StuntResult result) {if(CanCollect)Attempt.LandStunt(result);}
        public bool Begin(int index)
        {
            if(index<0 || index>=MissionCatalog.Count)return false;
            return BeginAttempt(index,false,MissionCatalog.Create(index,Career.NextLevel(index)));
        }
        public bool BeginFreestyle(int index)
        {
            if(index<0 || index>=FreestyleCatalog.Count || !FreestyleCatalog.Unlocked(index/3,Freestyle.points))return false;
            return BeginAttempt(index,true,FreestyleCatalog.Create(index,Freestyle.NextLevel(index)));
        }
        public bool ExploreFreestyle(int zone)
        {
            if(hud==null || hud.World==null || Attempt.Active || !FreestyleCatalog.Unlocked(zone,Freestyle.points))return false;
            if(!hud.World.Travel(zone))return false;
            Attempt.Clear();ResultsPending=false;IsFreestyle=true;Selected=zone*3;UpdateMarker();
            hud.Session.SetObjective("FREESTYLE · "+RideWorld.Destinations[zone].Name,"Explore and practise. Start challenges to earn Freestyle Points.");return true;
        }
        private bool BeginAttempt(int index,bool freestyle,MissionDefinition definition)
        {
            if(hud==null || hud.World==null || Attempt.Active)return false;
            Bind();
            if(!hud.World.Travel(definition.Destination))return false;
            if(!freestyle && index==MissionCatalog.CanyonJumpIndex) {recovery.SetCheckpoint(new Vector3(1000,0,245),0);recovery.Respawn();}
            lastHeading=bike.transform.eulerAngles.y;
            IsFreestyle=freestyle;Selected=index;ResultsPending=false;SaveNotice="";
            if(!Attempt.Start(definition))return false;
            bike.SetSuspended(true);heldForCountdown=true;
            countdownBodies=bike.GetComponentsInChildren<Rigidbody>(true);originalKinematic=new bool[countdownBodies.Length];
            for(int i=0;i<countdownBodies.Length;i++) {originalKinematic[i]=countdownBodies[i].isKinematic;countdownBodies[i].isKinematic=true;}
            lastState=Attempt.State;
            RefreshObjective();return true;
        }
        public void Interrupt(string reason) {Attempt.Fail(reason);ReleaseCountdown();}
        public void Abandon() {Attempt.Abandon();ReleaseCountdown();}
        public void Dismiss()
        {
            if(Attempt.Active)return;
            Attempt.Clear();ResultsPending=false;hud.Session.SetObjective("FREE RIDE","Explore the city at your own pace.");UpdateMarker();
        }
        public void AcknowledgeResults() {ResultsPending=false;lastState=Attempt.State;}
        private void Crash() {Interrupt("You crashed. Retry the mission.");}
        private void Reset() {Interrupt("Reset ended this attempt.");}
        private void Complete()
        {
            if(ActiveCareer.Record(Selected,Attempt.Definition.Level,Attempt.Elapsed,Attempt.Definition.Reward))
                hud.RewardMission(IsFreestyle,Selected,Attempt.Definition.Level,Attempt.Definition.Reward);
            string key=IsFreestyle?freestyleSaveKey:saveKey;
            if(string.IsNullOrEmpty(key))return;
            try {PlayerPrefs.SetString(key,ActiveCareer.ToJson());PlayerPrefs.Save();SaveNotice="";}
            catch(Exception e) {SaveNotice="Progress could not be saved on this device.";Debug.LogWarning("Mission save failed: "+e.Message);}
        }
        private void FixedUpdate()
        {
            if(hud==null)return;Bind();
            if(bike==null || bike.rb==null)return;
            if(IsFreestyle && Attempt.State==MissionState.Running && !hud.IsPaused && !FreestyleCatalog.Contains(Selected/3,bike.rb.position)) {Interrupt("You left the selected Freestyle zone.");return;}
            float nose=Mathf.Asin(Mathf.Clamp(Vector3.Dot(bike.rb.rotation*Vector3.forward,bike.GroundNormal),-1,1))*Mathf.Rad2Deg;
            float speed=Vector3.ProjectOnPlane(bike.rb.linearVelocity,Vector3.up).magnitude;
            float heading=bike.rb.rotation.eulerAngles.y;float yawDelta=Mathf.DeltaAngle(lastHeading,heading);lastHeading=heading;
            Attempt.Tick(new MissionSample {Acceleration=bike.rawCustomAccelerationAxis,Steering=bike.customSteerAxis,YawDelta=yawDelta,Position=bike.rb.position,SpeedKmh=speed*3.6f,Grounded=bike.IsGrounded,StableGrounded=bike.FrontGrounded && bike.RearGrounded && Vector3.Dot(bike.rb.rotation*Vector3.up,bike.GroundNormal)>=.8f,Wheelie=bike.RearGrounded && !bike.FrontGrounded && nose>=12 && speed>=1},Time.fixedDeltaTime,hud.IsPaused);
            ReleaseCountdown();
        }
        private void Update()
        {
            if(hud==null)return;Bind();
            if(lastState!=Attempt.State) {
                if(Attempt.State==MissionState.Completed || Attempt.State==MissionState.Failed || Attempt.State==MissionState.Abandoned)ResultsPending=true;
                lastState=Attempt.State;
            }
            ReleaseCountdown();RefreshObjective();
        }
        private void ReleaseCountdown()
        {
            if(!heldForCountdown || Attempt.State==MissionState.Countdown)return;
            heldForCountdown=false;
            for(int i=0;i<countdownBodies.Length;i++) if(countdownBodies[i]!=null) countdownBodies[i].isKinematic=originalKinematic[i];
            countdownBodies=null;originalKinematic=null;
            if(bike!=null && (recovery==null || !recovery.IsCrashed))bike.SetSuspended(false);
        }
        private void RefreshObjective()
        {
            if(Attempt.State==MissionState.Idle)return;
            var d=Attempt.Definition;var o=Attempt.Objective;
            string detail=Attempt.Result;float progress=Attempt.State==MissionState.Completed?1:0;
            if(Attempt.State==MissionState.Countdown)detail=$"GET READY  {Mathf.CeilToInt(Attempt.Countdown)}";
            if(Attempt.State==MissionState.Running && o!=null) {
                string amount=o.kind==ObjectiveKind.ReachPoint?$"{Vector3.Distance(bike.transform.position,o.point):0} m away":$"{Attempt.Progress:0.#} / {o.target:0.#}";
                if(d.Id=="checkpoint_run" || d.Id=="mountain_ascent") {
                    Vector3 delta=o.point-bike.transform.position;
                    float angle=Mathf.DeltaAngle(bike.transform.eulerAngles.y,Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg);
                    string direction=Mathf.Abs(angle)<35?"ahead":Mathf.Abs(angle)>135?"turn back":angle>0?"turn right":"turn left";
                    amount=$"{Vector3.Distance(bike.transform.position,o.point):0} m · {direction} · {Attempt.ObjectiveIndex}/{d.Objectives.Length} passed";
                    if(d.Id=="mountain_ascent")amount+=$" · Altitude {Mathf.Max(0,bike.transform.position.y):0}/24 m";
                }
                if(o.kind==ObjectiveKind.PickupPackage || o.kind==ObjectiveKind.DeliverPackage) {
                    float meters=Vector3.Distance(bike.transform.position,o.point);
                    Vector3 delta=o.point-bike.transform.position;
                    float angle=Mathf.DeltaAngle(bike.transform.eulerAngles.y,Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg);
                    string direction=Mathf.Abs(angle)<35?"ahead":Mathf.Abs(angle)>135?"turn back":angle>0?"turn right":"turn left";
                    amount=meters>o.radius?$"{meters:0} m · {direction}":$"Hold below {o.speedKmh:0} KM/H: {Attempt.Progress:0.0}/{o.target:0.0}s";
                    amount+=Attempt.CarryingPackage?" · PIZZA ON BOARD":" · COLLECT PIZZA";
                }
                if((int)o.kind>=(int)ObjectiveKind.LandedWheelieDistance)amount=$"Best validated: {Attempt.Progress:0.#}/{o.target:0.#} · land cleanly";
                if(o.kind==ObjectiveKind.LandedCombo)amount=$"Best banked combo: {Attempt.Progress:0}/{o.target:0} stunts · land and bank";
                if(o.kind==ObjectiveKind.WheelieHold) {
                    amount=$"{Attempt.Progress:0.0}/{o.target:0}s continuously";
                    if(Attempt.WheelieHoldBroken)amount+=" · Hold reset — lift front wheel again";
                    else if(Attempt.Progress<=0)amount+=" · Ride forward and lift front wheel";
                }
                if(o.kind==ObjectiveKind.CanyonJump) {
                    amount=Attempt.JumpInFlight?$"Land in GREEN · Settle {Attempt.Progress:0.0}/{o.target:0.0}s":Attempt.JumpReady?"Release JUMP now; keep straight":"Build speed; hold JUMP to charge";
                }
                if(o.kind==ObjectiveKind.PoliceEscape) {
                    amount=$"Gap {Attempt.PoliceGap:0}/{o.radius:0} m · Escape {Attempt.Progress:0.0}/{o.target:0.#}s";
                    if(Attempt.CaptureProgress>0)amount+=$" · CAPTURE {Attempt.CaptureProgress:0.0}/2s";
                }
                if(o.kind==ObjectiveKind.RivalRace)
                    amount=$"{(Attempt.Progress>=Attempt.RivalDistance?"1st":"2nd")} / 2 · {Attempt.Progress:0}/{o.target:0} m · {(d.Id=="boss_race"?"Boss":"Rival")} {Attempt.RivalDistance:0} m";
                if(o.kind==ObjectiveKind.SpeedHold) {
                    amount=$"{bike.SpeedKmh:0.#}/{o.speedKmh:0.#} KM/H · {Attempt.Progress:0.0}/{o.target:0}s";
                    if(o.useZone && !o.Contains(bike.transform.position))amount="Return inside the marked speed zone · hold reset";
                    else if(!bike.IsGrounded)amount="Land on the road · hold reset";
                    else if(bike.SpeedKmh<o.speedKmh)amount+=" · build speed";
                }
                if(o.kind==ObjectiveKind.BrakeStop && !Attempt.BrakeArmed)amount=$"{bike.SpeedKmh:0.#} / {o.speedKmh:0} KM/H";
                else if(o.kind==ObjectiveKind.SteerTurn)amount+="°";
                else if(o.kind==ObjectiveKind.PedalHold || o.kind==ObjectiveKind.BrakeStop)amount+="s";
                string instruction=o.kind==ObjectiveKind.CanyonJump && Attempt.JumpInFlight?"Land upright in GREEN; BRAKE to settle":o.instruction;
                detail=$"{instruction}  {amount}\n{Mathf.CeilToInt(d.TimeLimit-Attempt.Elapsed)}s left  |  Step {Attempt.ObjectiveIndex+1}/{d.Objectives.Length}";
                progress=(Attempt.ObjectiveIndex+Mathf.Clamp01(Attempt.Progress/o.target))/d.Objectives.Length;
            }
            hud.Session.SetObjective($"{(IsFreestyle?"FREESTYLE · ":"")}{d.Title} · LEVEL {d.Level}",detail,progress);UpdateMarker();
        }
        private void UpdateMarker()
        {
            var o=Attempt.Objective;bool visible=Attempt.Active && o!=null && (o.kind==ObjectiveKind.CanyonJump || o.kind==ObjectiveKind.ReachPoint || o.kind==ObjectiveKind.PickupPackage || o.kind==ObjectiveKind.DeliverPackage);
            if(visible && marker==null) {
                marker=GameObject.CreatePrimitive(PrimitiveType.Cylinder);marker.name="Active mission checkpoint";
                marker.GetComponent<Collider>().enabled=false;Destroy(marker.GetComponent<Collider>());
                markerMaterial=new Material(Shader.Find("Standard")){color=new Color(1,.8f,.05f)};
                marker.GetComponent<Renderer>().sharedMaterial=markerMaterial;
            }
            UpdateCheckpointGate(o);
            UpdateRace(o);
            UpdateSpeedZone(o);
            UpdatePackageVisual();
            if(marker==null)return;marker.SetActive(visible);
            if(visible)marker.transform.rotation=Attempt.Definition.Id=="mountain_ascent"?MountainGateRotation():Quaternion.identity;
            if(visible) {markerMaterial.color=o.kind==ObjectiveKind.PickupPackage?new Color(.05f,.6f,1):((o.kind==ObjectiveKind.CanyonJump && Attempt.JumpInFlight) || o.kind==ObjectiveKind.DeliverPackage || ((Attempt.Definition.Id=="checkpoint_run" || Attempt.Definition.Id=="mountain_ascent") && Attempt.ObjectiveIndex==Attempt.Definition.Objectives.Length-1))?new Color(.1f,.9f,.3f):new Color(1,.8f,.05f);bool landing=o.kind==ObjectiveKind.CanyonJump && Attempt.JumpInFlight;
                float radius=landing?o.landingRadius:o.radius;
                marker.transform.position=(landing?o.landingPoint:o.point)+Vector3.up*.06f;marker.transform.localScale=new Vector3(radius*2,.035f,radius*2);}
        }
        private Quaternion MountainGateRotation()
        {
            Vector3 previous=Attempt.ObjectiveIndex==0?RideWorld.Destinations[2].Position:Attempt.Definition.Objectives[Attempt.ObjectiveIndex-1].point;
            return Quaternion.LookRotation(Attempt.Objective.point-previous,Vector3.up);
        }
        private void UpdateCheckpointGate(MissionObjective objective)
        {
            bool visible=Attempt.Active && (Attempt.Definition.Id=="checkpoint_run" || Attempt.Definition.Id=="mountain_ascent") && objective!=null;
            if(visible && checkpointGate==null) {
                checkpointGate=new GameObject("Checkpoint run gate");
                for(int i=0;i<3;i++) {
                    var part=GameObject.CreatePrimitive(PrimitiveType.Cube);part.name=i==2?"Crossbar":"Post";part.transform.SetParent(checkpointGate.transform,false);
                    part.GetComponent<Collider>().enabled=false;Destroy(part.GetComponent<Collider>());
                    part.GetComponent<Renderer>().sharedMaterial=markerMaterial;
                }
            }
            if(checkpointGate==null)return;checkpointGate.SetActive(visible);
            if(!visible)return;
            checkpointGate.name=Attempt.Definition.Id=="mountain_ascent"?"Mountain ascent gate":"Checkpoint run gate";
            checkpointGate.transform.rotation=Attempt.Definition.Id=="mountain_ascent"?MountainGateRotation():Quaternion.identity;
            checkpointGate.transform.position=objective.point;
            for(int i=0;i<3;i++) {
                var part=checkpointGate.transform.GetChild(i);
                part.localPosition=i==2?new Vector3(0,3,0):new Vector3((i==0?-1:1)*objective.radius,1.5f,0);
                part.localScale=i==2?new Vector3(objective.radius*2,.15f,.15f):new Vector3(.15f,3,.15f);
            }
        }
        private void UpdateRace(MissionObjective objective)
        {
            bool police=objective!=null && objective.kind==ObjectiveKind.PoliceEscape;
            bool visible=Attempt.Active && objective!=null && (objective.kind==ObjectiveKind.RivalRace || police);
            if(visible && rivalVisual==null) {
                rivalMaterial=new Material(Shader.Find("Standard")){color=new Color(.95f,.25f,.1f)};
                tireMaterial=new Material(Shader.Find("Standard")){color=new Color(.08f,.09f,.12f)};
                rivalVisual=new GameObject("Rival cyclist");
                // Standalone visual: no player controllers, cameras, physics or global rider scripts.
                for(int i=0;i<2;i++) {
                    var wheel=RacePart(rivalVisual.transform,"Wheel",PrimitiveType.Cylinder,new Vector3(0,.36f,i==0?-.6f:.6f),new Vector3(.7f,.045f,.7f));
                    wheel.localRotation=Quaternion.Euler(0,0,90);wheel.GetComponent<Renderer>().sharedMaterial=tireMaterial;
                }
                RaceBar("Frame",new Vector3(0,.4f,-.6f),new Vector3(0,.9f,0),.07f);
                RaceBar("Frame",new Vector3(0,.9f,0),new Vector3(0,.4f,.6f),.07f);
                RaceBar("Frame",new Vector3(0,.4f,.6f),new Vector3(0,.4f,-.6f),.07f);
                RaceBar("Fork",new Vector3(0,.4f,.6f),new Vector3(0,1.05f,.55f),.06f);
                RaceBar("Handlebar",new Vector3(-.3f,1.05f,.55f),new Vector3(.3f,1.05f,.55f),.06f);
                RaceBar("Rider torso",new Vector3(0,1,0),new Vector3(0,1.5f,.25f),.25f);
                RacePart(rivalVisual.transform,"Helmet",PrimitiveType.Sphere,new Vector3(0,1.7f,.3f),Vector3.one*.3f);
                foreach(float side in new[]{-.16f,.16f}) {
                    RaceBar("Arm",new Vector3(side,1.45f,.25f),new Vector3(side,1.05f,.55f),.09f);
                    RaceBar(side<0?"Left thigh":"Right thigh",new Vector3(side,1,0),new Vector3(side,.7f,.25f),.12f);
                    RaceBar(side<0?"Left shin":"Right shin",new Vector3(side,.7f,.25f),new Vector3(side,.4f,.1f),.10f);
                }
                policeBeacon=RacePart(rivalVisual.transform,"Police beacon",PrimitiveType.Sphere,new Vector3(0,1.9f,.3f),Vector3.one*.16f).gameObject;
                policeLightMaterial=new Material(Shader.Find("Standard"));policeBeacon.GetComponent<Renderer>().sharedMaterial=policeLightMaterial;
                raceFinish=new GameObject("Rival sprint finish");
                RacePart(raceFinish.transform,"Start line",PrimitiveType.Cube,Vector3.zero,new Vector3(20,.06f,.5f));
                RacePart(raceFinish.transform,"Left boundary",PrimitiveType.Cube,Vector3.zero,Vector3.one);
                RacePart(raceFinish.transform,"Right boundary",PrimitiveType.Cube,Vector3.zero,Vector3.one);
                RacePart(raceFinish.transform,"Finish line",PrimitiveType.Cube,Vector3.zero,new Vector3(20,.06f,1));
                RacePart(raceFinish.transform,"Left post",PrimitiveType.Cube,new Vector3(-10,1.5f,0),new Vector3(.2f,3,.2f));
                RacePart(raceFinish.transform,"Right post",PrimitiveType.Cube,new Vector3(10,1.5f,0),new Vector3(.2f,3,.2f));
            }
            if(rivalVisual==null)return;
            rivalVisual.SetActive(visible);raceFinish.SetActive(visible && !police);policeBeacon.SetActive(visible && police);
            if(!visible)return;
            bool boss=Attempt.Definition.Id=="boss_race";
            rivalVisual.name=police?"Police cyclist":boss?"Boss cyclist":"Rival cyclist";
            raceFinish.name=boss?"Boss race finish":"Rival sprint finish";
            rivalMaterial.color=police?new Color(.08f,.25f,.8f):boss?new Color(1,.7f,.05f):new Color(.95f,.25f,.1f);
            policeLightMaterial.color=((int)(Attempt.Elapsed*4)%2==0)?Color.blue:Color.red;
            float distance=police?Attempt.PoliceDistance:Attempt.RivalDistance;
            rivalVisual.transform.position=police?Attempt.PolicePosition:objective.point+new Vector3(4,0,Attempt.RivalDistance);
            rivalVisual.transform.rotation=police?Quaternion.LookRotation(Attempt.PoliceFacing):Quaternion.identity;
            raceFinish.transform.position=objective.point+new Vector3(0,.05f,objective.target);
            raceFinish.transform.Find("Start line").localPosition=new Vector3(0,0,-objective.target);
            foreach(string sideName in new[]{"Left boundary","Right boundary"}) {
                var edge=raceFinish.transform.Find(sideName);edge.localPosition=new Vector3(sideName=="Left boundary"?-10:10,0,-objective.target*.5f);edge.localScale=new Vector3(.12f,.04f,objective.target);
            }
            for(int i=0;i<2;i++) {
                float side=i==0?-.16f:.16f,phase=distance*3+i*Mathf.PI;
                var hip=new Vector3(side,1,0);var knee=new Vector3(side,.7f,.18f+Mathf.Cos(phase)*.1f);
                var foot=new Vector3(side,.4f+Mathf.Sin(phase)*.13f,Mathf.Cos(phase)*.13f);
                PoseBar(rivalVisual.transform.Find(i==0?"Left thigh":"Right thigh"),hip,knee,.12f);
                PoseBar(rivalVisual.transform.Find(i==0?"Left shin":"Right shin"),knee,foot,.10f);
            }
            foreach(Transform child in rivalVisual.transform)
                if(child.name=="Wheel")child.localRotation=Quaternion.Euler(distance/.35f*Mathf.Rad2Deg,0,90);
        }
        private Transform RacePart(Transform parent,string name,PrimitiveType shape,Vector3 position,Vector3 scale)
        {
            var part=GameObject.CreatePrimitive(shape);part.name=name;part.transform.SetParent(parent,false);
            part.transform.localPosition=position;part.transform.localScale=scale;
            part.GetComponent<Collider>().enabled=false;Destroy(part.GetComponent<Collider>());
            part.GetComponent<Renderer>().sharedMaterial=rivalMaterial;return part.transform;
        }
        private void RaceBar(string name,Vector3 a,Vector3 b,float width)
        {
            var bar=RacePart(rivalVisual.transform,name,PrimitiveType.Cylinder,(a+b)*.5f,new Vector3(width,(b-a).magnitude*.5f,width));
            PoseBar(bar,a,b,width);
        }
        private static void PoseBar(Transform bar,Vector3 a,Vector3 b,float width)
        {
            bar.localPosition=(a+b)*.5f;bar.localScale=new Vector3(width,(b-a).magnitude*.5f,width);
            bar.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
        }
        private void UpdateSpeedZone(MissionObjective objective)
        {
            bool police=objective!=null && objective.kind==ObjectiveKind.PoliceEscape;
            bool visible=Attempt.Active && objective!=null && ((objective.kind==ObjectiveKind.SpeedHold && objective.useZone) || police);
            if(visible && speedZone==null) {
                if(markerMaterial==null)markerMaterial=new Material(Shader.Find("Standard")){color=new Color(1,.8f,.05f)};
                speedZone=new GameObject("Speed trial zone");
                for(int i=0;i<4;i++) {
                    var edge=GameObject.CreatePrimitive(PrimitiveType.Cube);edge.name="Boundary "+i;edge.transform.SetParent(speedZone.transform,false);
                    edge.GetComponent<Collider>().enabled=false;Destroy(edge.GetComponent<Collider>());
                    edge.GetComponent<Renderer>().sharedMaterial=markerMaterial;
                }
            }
            if(speedZone==null)return;speedZone.SetActive(visible);
            if(!visible)return;
            speedZone.name=police?"Pursuit area":"Speed trial zone";
            Vector3 size=police?new Vector3(20,8,660):objective.zoneSize;
            speedZone.transform.position=(police?objective.point+Vector3.forward*310:objective.point)+Vector3.up*.04f;
            for(int i=0;i<4;i++) {
                var edge=speedZone.transform.GetChild(i);bool side=i<2;float sign=i%2==0?-1:1;
                edge.localPosition=side?new Vector3(sign*size.x*.5f,0,0):new Vector3(0,0,sign*size.z*.5f);
                edge.localScale=side?new Vector3(.25f,.04f,size.z):new Vector3(size.x,.04f,.25f);
            }
        }
        private void UpdatePackageVisual()
        {
            bool visible=Attempt.Active && Attempt.CarryingPackage && bike!=null;
            if(visible && packageVisual==null) {
                packageVisual=GameObject.CreatePrimitive(PrimitiveType.Cube);packageVisual.name="Delivery pizza box";
                packageVisual.GetComponent<Collider>().enabled=false;Destroy(packageVisual.GetComponent<Collider>());
                if(packageMaterial==null)packageMaterial=new Material(Shader.Find("Standard")){color=new Color(1,.75f,.35f)};
                packageVisual.GetComponent<Renderer>().sharedMaterial=packageMaterial;
                packageVisual.transform.SetParent(bike.transform,false);
                packageVisual.transform.localPosition=new Vector3(0,.85f,-.75f);packageVisual.transform.localScale=new Vector3(.5f,.12f,.5f);
            }
            if(packageVisual!=null)packageVisual.SetActive(visible);
        }
        private void OnDisable() {if(Attempt.Active)Interrupt("Mission interrupted.");}
        private void OnDestroy()
        {
            if(Attempt.Active)Attempt.Abandon();ReleaseCountdown();Unbind();Attempt.Completed-=Complete;
            if(rivalVisual!=null)Destroy(rivalVisual);if(raceFinish!=null)Destroy(raceFinish);if(rivalMaterial!=null)Destroy(rivalMaterial);if(tireMaterial!=null)Destroy(tireMaterial);if(policeLightMaterial!=null)Destroy(policeLightMaterial);
            if(checkpointGate!=null)Destroy(checkpointGate);
            if(speedZone!=null)Destroy(speedZone);
            if(marker!=null)Destroy(marker);if(markerMaterial!=null)Destroy(markerMaterial);
            if(packageVisual!=null)Destroy(packageVisual);if(packageMaterial!=null)Destroy(packageMaterial);
        }
    }
}

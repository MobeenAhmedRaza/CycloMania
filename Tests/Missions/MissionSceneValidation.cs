using System;
using System.Reflection;
using Cyclomania;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
public static class MissionSceneValidation
{
    static GameplayHud hud;static int frames,stage,count,errors;
    static Transform Page => (Transform)typeof(GameplayHud).GetField("page",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(hud);
    static void Check(bool valid,string name) {if(!valid)throw new Exception(name);count++;Debug.Log("PASS: "+name);}
    static void Invoke(object o,string name)=>o.GetType().GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,null);
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/gameplay.unity");hud=UnityEngine.Object.FindFirstObjectByType<GameplayHud>();hud.SettingsSaveKey="";hud.AdvancedSaveKey="";hud.EconomySaveKey="";hud.GarageSaveKey="";hud.MissionSaveKey="";
        EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload|EnterPlayModeOptions.DisableSceneReload;
        EditorApplication.playModeStateChanged+=state=> {if(state!=PlayModeStateChange.EnteredPlayMode)return;Application.logMessageReceived+=(m,t,type)=>{if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;};EditorApplication.update+=Tick;};
        EditorApplication.EnterPlaymode();
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying || ++frames<35)return;frames=0;
        try {
            var m=hud.Missions;
            if(stage==0) {
                Check(m!=null,"mission service initializes in gameplay scene");hud.Pause();Page.Find("Missions").GetComponent<Button>().onClick.Invoke();
                Page.Find("NextPage").GetComponent<Button>().onClick.Invoke();
                Page.Find("Start0").GetComponent<Button>().onClick.Invoke();
                Check(m.Attempt.State==MissionState.Countdown && !hud.IsPaused,"mission start button begins countdown and resumes");
                Check(hud.bicycle.IsSuspended && hud.bicycle.rb.isKinematic,"countdown holds the complete bicycle safely");
                hud.Pause();float before=m.Attempt.Countdown;Invoke(m,"FixedUpdate");Check(m.Attempt.Countdown==before,"paused runtime does not advance countdown");
                hud.Resume();m.Attempt.Tick(new MissionSample(),3,false);Invoke(m,"Update");
                Check(!hud.bicycle.IsSuspended && !hud.bicycle.rb.isKinematic,"countdown releases the rig for riding");
                hud.bicycle.GetComponent<CycleRecovery>().Crash();Check(m.Attempt.State==MissionState.Failed,"actual crash event fails active mission");
                Invoke(m,"Update");Invoke(hud,"Update");Check(hud.CurrentPage=="Mission Result" && hud.IsPaused,"failed mission opens paused results");
                Page.Find("Retry").GetComponent<Button>().onClick.Invoke();Check(m.Attempt.State==MissionState.Countdown && m.Attempt.Definition.Level==1,"retry restarts same difficulty at mission spawn");
                m.Attempt.Tick(new MissionSample(),3,false);Invoke(m,"Update");
                m.Attempt.Tick(new MissionSample {SpeedKmh=36,Grounded=true},20,false);Invoke(m,"Update");Invoke(hud,"Update");
                Check(m.Attempt.State==MissionState.Completed && m.Career.points==100 && m.Career.completed[0]==1,"mission completion records career reward once");
                Check(hud.IsPaused && hud.CurrentPage=="Mission Result","completion opens results screen");
                Page.Find("Retry").GetComponent<Button>().onClick.Invoke();Check(m.Attempt.Definition.Level==2 && m.Attempt.Objective.target==300,"next level button loads harder contract");
                hud.World.Travel(0);Check(!m.Attempt.Active && !hud.bicycle.rb.isKinematic,"world travel terminates mission and releases countdown lock");
                m.Dismiss();Check(m.Attempt.State==MissionState.Idle && hud.Session.Objective=="FREE RIDE","free ride clears objective");
                Check(m.Begin(1),"checkpoint contract starts");Invoke(m,"Update");
                var marker=GameObject.Find("Active mission checkpoint");Check(marker!=null && marker.activeSelf,"checkpoint mission displays target marker");
                hud.ResetCurrentBicycle();Check(m.Attempt.State==MissionState.Failed,"touch reset path fails active attempt");
                m.Dismiss();Check(m.Begin(2),"wheelie contract starts");m.Abandon();hud.ShowMissionResult();Check(m.Attempt.State==MissionState.Abandoned && !hud.bicycle.rb.isKinematic,"abandon ends attempt and unfreezes rig");
                Page.Find("FreeRide").GetComponent<Button>().onClick.Invoke();Check(!hud.IsPaused && m.Attempt.State==MissionState.Idle,"free ride results action resumes gameplay");
                var cityBike=hud.bicycle;var cityMode=Physics.simulationMode;Physics.simulationMode=SimulationMode.Script;
                foreach(float cityDirection in new[]{-1f,1f}) {
                    hud.World.Travel(3);cityBike.wheelieInput=false;cityBike.rawCustomAccelerationAxis=cityBike.customAccelerationAxis=cityBike.customSteerAxis=cityBike.customLeanAxis=0;
                    for(int i=0;i<20;i++){Invoke(cityBike,"FixedUpdate");Physics.Simulate(.02f);}
                    float cityHeading=cityBike.rb.rotation.eulerAngles.y;cityBike.customSteerAxis=cityDirection;
                    cityBike.rb.linearVelocity=cityBike.fWheelRb.linearVelocity=cityBike.rWheelRb.linearVelocity=Vector3.forward*6;
                    for(int i=0;i<50;i++){Invoke(cityBike,"FixedUpdate");Physics.Simulate(.02f);}
                    float cityTurn=Mathf.DeltaAngle(cityHeading,cityBike.rb.rotation.eulerAngles.y)*cityDirection;
                    Check(cityTurn>15 && cityTurn<140 && !cityBike.GetComponent<CycleRecovery>().IsCrashed,"default City bicycle turns "+(cityDirection<0?"left":"right")+" without crashing");
                }
                Physics.simulationMode=cityMode;
                m.Begin(0);hud.Garage.Earn(5000);hud.ShowGarage();Page.Find("Next").GetComponent<Button>().onClick.Invoke();Page.Find("Equip").GetComponent<Button>().onClick.Invoke();stage++;return;
            }
            Check(hud.Garage.selected==1 && !m.Attempt.Active,"garage change ends attempt and equips selected bicycle");
            m.Dismiss();Check(m.Begin(0),"missions bind to replacement bicycle");hud.bicycle.GetComponent<CycleRecovery>().Crash();Check(m.Attempt.State==MissionState.Failed,"replacement bicycle crash still fails mission");
            m.Dismiss();hud.ShowMissions();
            Check(Page.Find("Mission3").GetComponent<Text>().text.Contains("Riding Basics"),"tutorial is listed with its riding lessons");
            Page.Find("Start3").GetComponent<Button>().onClick.Invoke();
            Check(m.Attempt.Definition.Id=="riding_basics" && m.Attempt.Definition.Objectives.Length==5,"tutorial menu button starts five-step lesson");
            m.Attempt.Tick(new MissionSample(),3,false);Invoke(m,"Update");
            var rider=hud.bicycle;
            var simulationMode=Physics.simulationMode;Physics.simulationMode=SimulationMode.Script;
            for(int i=0;i<20;i++)Physics.Simulate(.02f);
            Physics.simulationMode=simulationMode;
            Invoke(rider,"UpdateGroundContact");Check(rider.IsGrounded,"tutorial spawn settles onto ground after countdown");
            rider.rawCustomAccelerationAxis=0;rider.rb.linearVelocity=Vector3.forward*3;
            Invoke(m,"FixedUpdate");Check(m.Attempt.Progress==0,"runtime tutorial rejects coasting without pedal input");
            rider.rawCustomAccelerationAxis=1;
            for(int i=0;i<101;i++)Invoke(m,"FixedUpdate");
            Check(m.Attempt.ObjectiveIndex==1,"controller pedal telemetry completes tutorial acceleration step");
            rider.rawCustomAccelerationAxis=-1;Invoke(m,"FixedUpdate");rider.rb.linearVelocity=Vector3.zero;
            for(int i=0;i<51;i++)Invoke(m,"FixedUpdate");
            Check(m.Attempt.ObjectiveIndex==2,"controller brake telemetry completes moving stop lesson");
            rider.rb.linearVelocity=Vector3.forward*2;rider.rawCustomAccelerationAxis=1;rider.customSteerAxis=-1;
            for(int i=0;i<41;i++) {rider.rb.rotation=Quaternion.Euler(0,-(i+1)*.5f,0);Invoke(m,"FixedUpdate");}
            Check(m.Attempt.ObjectiveIndex==3,"rig heading telemetry completes left turn lesson");
            rider.customSteerAxis=1;
            for(int i=0;i<41;i++) {rider.rb.rotation=Quaternion.Euler(0,-20.5f+(i+1)*.5f,0);Invoke(m,"FixedUpdate");}
            Check(m.Attempt.ObjectiveIndex==4,"rig heading telemetry completes right turn lesson");
            rider.rawCustomAccelerationAxis=-1;Invoke(m,"FixedUpdate");rider.rb.linearVelocity=Vector3.zero;
            for(int i=0;i<51;i++)Invoke(m,"FixedUpdate");
            Invoke(m,"Update");Invoke(hud,"Update");
            Check(m.Attempt.State==MissionState.Completed && m.Career.completed[3]==1 && hud.CurrentPage=="Mission Result","tutorial records completion and presents results");
            Page.Find("Retry").GetComponent<Button>().onClick.Invoke();
            Check(m.Attempt.Definition.Level==2 && m.Attempt.Objective.target==2.5f,"tutorial next level loads harder pedal hold");
            m.Abandon();
            m.Dismiss();hud.ShowMissions();Check(Page.Find("Mission4").GetComponent<Text>().text.Contains("Pizza Delivery"),"delivery appears in mission selection");
            Page.Find("NextPage").GetComponent<Button>().onClick.Invoke();
            var lastCard=Page.Find("Start2").GetComponent<RectTransform>();Check(lastCard.anchoredPosition.y-lastCard.sizeDelta.y/2>=-190,"practice mission page buttons remain above footer");
            Page.Find("PreviousPage").GetComponent<Button>().onClick.Invoke();
            Page.Find("Start4").GetComponent<Button>().onClick.Invoke();
            Check(m.Attempt.Definition.Id=="pizza_delivery" && m.Attempt.State==MissionState.Countdown,"delivery start button opens correct mission");
            var contract=m.Attempt.Definition;
            foreach(var stop in contract.Objectives) {
                Check(Physics.Raycast(stop.point+Vector3.up*.1f,Vector3.down,out var ground,1) && ground.normal.y>.9f,"delivery stop has flat road ground");
                foreach(var obstacle in Physics.OverlapBox(stop.point+Vector3.up*1.3f,new Vector3(.6f,.9f,.6f)))
                    Check(obstacle.isTrigger || obstacle.transform.IsChildOf(rider.transform),"delivery stop footprint is clear");
            }
            m.Attempt.Tick(new MissionSample(),3,false);Invoke(m,"Update");
            var deliveryMarker=GameObject.Find("Active mission checkpoint");
            Check(deliveryMarker!=null && deliveryMarker.transform.position.z==contract.Objectives[0].point.z,"delivery marker identifies pickup first");
            Check(deliveryMarker.GetComponent<Renderer>().sharedMaterial.color==new Color(.05f,.6f,1),"pickup marker is blue");
            var stopped=new MissionSample {Grounded=true,Position=contract.Objectives[0].point};m.Attempt.Tick(stopped,2,false);Invoke(m,"Update");
            var pizza=GameObject.Find("Delivery pizza box");
            Check(m.Attempt.CarryingPackage && pizza!=null && pizza.transform.parent==rider.transform,"pickup attaches visual pizza box to equipped bicycle");
            Check(pizza.GetComponent<Collider>()==null || !pizza.GetComponent<Collider>().enabled,"carried pizza does not collide with bicycle");
            Check(deliveryMarker.transform.position.z==contract.Objectives[1].point.z && deliveryMarker.GetComponent<Renderer>().sharedMaterial.color==new Color(.1f,.9f,.3f),"pickup changes target to green delivery zone");
            Check(hud.Session.ObjectiveDetail.Contains("PIZZA ON BOARD"),"HUD shows carried package status");
            int pointsBefore=m.Career.points;stopped.Position=contract.Objectives[1].point;m.Attempt.Tick(stopped,2,false);Invoke(m,"Update");Invoke(hud,"Update");
            Check(hud.CurrentPage=="Mission Result" && m.Career.completed[4]==1 && m.Career.points==pointsBefore+150,"delivery completion opens results and records reward");
            Check(!pizza.activeSelf && !deliveryMarker.activeSelf,"delivery completion hides package and target");
            Page.Find("Retry").GetComponent<Button>().onClick.Invoke();Check(m.Attempt.Definition.Level==2 && m.Attempt.Definition.Objectives[1].point.z==contract.Objectives[1].point.z+20,"next delivery tier increases travel distance");
            m.Attempt.Tick(new MissionSample(),3,false);Invoke(m,"Update");stopped.Position=m.Attempt.Objective.point;m.Attempt.Tick(stopped,3,false);Invoke(m,"Update");
            Check(m.Attempt.CarryingPackage && pizza.activeSelf,"next attempt reuses package visual after pickup");
            hud.ResetCurrentBicycle();Invoke(m,"Update");Check(m.Attempt.State==MissionState.Failed && !m.Attempt.CarryingPackage && !pizza.activeSelf,"reset fails delivery and clears cargo visual");
            Check(m.Career.completed[4]==1 && m.Career.points==pointsBefore+150,"failed delivery does not grant another reward");
            m.Dismiss();hud.ShowMissions();
            Check(Page.Find("Mission5").GetComponent<Text>().text.Contains("18 KM/H for 10s"),"speed trial card states target speed and duration");
            Check(Page.Find("Start3")!=null && Page.Find("Start4")!=null && Page.Find("Start5")!=null,"first mission page contains tutorial delivery and speed trial");
            Page.Find("Start5").GetComponent<Button>().onClick.Invoke();
            Check(m.Attempt.Definition.Id=="speed_trial" && m.Attempt.State==MissionState.Countdown,"speed trial menu starts correct contract");
            m.Attempt.Tick(new MissionSample(),3,false);Invoke(m,"Update");
            var entry=m.Attempt.Objective.point;Check(Physics.Raycast(entry+Vector3.up*.1f,Vector3.down,out var trialGround,1) && trialGround.normal.y>.9f,"speed trial entry has flat road surface");
            m.Attempt.Tick(new MissionSample {Position=entry,Grounded=true,SpeedKmh=20},.02f,false);Invoke(m,"Update");
            var zone=GameObject.Find("Speed trial zone");Check(zone!=null && zone.activeSelf && zone.transform.childCount==4,"speed objective shows four zone boundary lines");
            foreach(var edge in zone.GetComponentsInChildren<Collider>())Check(!edge.enabled,"speed boundaries do not collide with rider");
            Check(hud.Session.ObjectiveDetail.Contains("Return inside"),"HUD guides rider outside speed zone");
            var sample=new MissionSample {Position=new Vector3(850,0,0),Grounded=true,SpeedKmh=18};m.Attempt.Tick(sample,5,false);
            sample.SpeedKmh=17;m.Attempt.Tick(sample,.02f,false);Check(m.Attempt.Progress==0,"speed trial integration resets hold after speed drop");
            sample.SpeedKmh=20;int beforeTrial=m.Career.points;m.Attempt.Tick(sample,10,false);Invoke(m,"Update");Invoke(hud,"Update");
            Check(m.Career.completed[5]==1 && m.Career.points==beforeTrial+150 && hud.CurrentPage=="Mission Result","speed trial completion saves tier reward and opens results");
            Check(!zone.activeSelf,"completed speed trial hides zone lines");
            Page.Find("Retry").GetComponent<Button>().onClick.Invoke();Check(m.Attempt.Definition.Level==2 && m.Attempt.Definition.Objectives[1].speedKmh==18.5f,"next level starts harder speed trial");
            m.Abandon();Invoke(m,"Update");Check(!zone.activeSelf && !rider.rb.isKinematic,"abandoned speed trial clears visuals and countdown lock");
            m.Dismiss();hud.ShowMissions();
            Page.Find("NextPage").GetComponent<Button>().onClick.Invoke();Page.Find("NextPage").GetComponent<Button>().onClick.Invoke();
            Check(Page.Find("Start6")!=null && Page.Find("NextPage").GetComponent<Button>().interactable,"third menu page exposes rival sprint and leads to more missions");
            Page.Find("Start6").GetComponent<Button>().onClick.Invoke();
            var rival=GameObject.Find("Rival cyclist");var finish=GameObject.Find("Rival sprint finish");
            Check(rival!=null && finish!=null && m.Attempt.State==MissionState.Countdown,"race start spawns rival and finish during countdown");
            Check(rival.GetComponentsInChildren<MonoBehaviour>().Length==0 && rival.GetComponentsInChildren<Rigidbody>().Length==0,"rival visual cannot bind player scripts or physics");
            foreach(var collider in rival.GetComponentsInChildren<Collider>())Check(!collider.enabled,"rival visual cannot collide with player");
            var raceSample=new MissionSample {Position=m.Attempt.Objective.point,Grounded=true};
            for(int z=-240;z<=360;z+=100)Check(Physics.Raycast(new Vector3(854,.1f,z),Vector3.down,1),"rival lane has ground along maximum course");
            m.Attempt.Tick(raceSample,3,false);Invoke(m,"Update");m.Attempt.Tick(raceSample,5,false);Invoke(m,"Update");
            Check(rival.transform.position.z> -240 && hud.Session.ObjectiveDetail.Contains("2nd / 2"),"rival advances and HUD reports player position");
            Check(finish.transform.Find("Left boundary").localScale.z==300 && finish.transform.position.z==60,"race boundaries and finish match selected course length");
            var legRotation=rival.transform.Find("Left shin").localRotation;
            float rivalZ=rival.transform.position.z;m.Attempt.Tick(raceSample,20,true);Invoke(m,"Update");Check(rival.transform.position.z==rivalZ,"paused race keeps visible rival fixed");
            Check(rival.transform.Find("Left shin").localRotation==legRotation,"paused race freezes pedaling pose");
            int racePoints=m.Career.points;raceSample.Position.z+=300;m.Attempt.Tick(raceSample,35,false);Invoke(m,"Update");Invoke(hud,"Update");
            Check(m.Career.completed[6]==1 && m.Career.points==racePoints+175 && hud.CurrentPage=="Mission Result","race victory records tier reward and presents result");
            Check(!rival.activeSelf && !finish.activeSelf,"race result hides rival and finish");
            Page.Find("Retry").GetComponent<Button>().onClick.Invoke();Check(m.Attempt.Definition.Level==2 && rival.activeSelf && rival.transform.position.z== -240,"next race starts harder tier with reused rival on grid");
            raceSample.Position=m.Attempt.Objective.point;m.Attempt.Tick(raceSample,3,false);m.Attempt.Tick(raceSample,100,false);Invoke(m,"Update");Invoke(hud,"Update");
            Check(m.Attempt.State==MissionState.Failed && m.Career.points==racePoints+175 && Page.Find("Outcome").GetComponent<Text>().text.Contains("2nd place"),"rival victory shows loss without rewarding player");
            Page.Find("Retry").GetComponent<Button>().onClick.Invoke();hud.ResetCurrentBicycle();Invoke(m,"Update");
            Check(m.Attempt.State==MissionState.Failed && !rival.activeSelf && !rider.rb.isKinematic,"reset removes race visual and releases countdown lock");
            m.Dismiss();hud.ShowMissions();Page.Find("NextPage").GetComponent<Button>().onClick.Invoke();Page.Find("NextPage").GetComponent<Button>().onClick.Invoke();
            Check(Page.Find("Start6")!=null && Page.Find("Mission7").GetComponent<Text>().text.Contains("10s"),"third page contains rival sprint and first wheelie duration");
            Page.Find("Start7").GetComponent<Button>().onClick.Invoke();
            Check(m.Attempt.Definition.Id=="first_wheelie" && rider.rb.isKinematic,"first wheelie menu starts countdown with bicycle locked");
            m.Attempt.Tick(new MissionSample(),3,false);Invoke(m,"Update");
            Check(!rider.rb.isKinematic && hud.Session.ObjectiveDetail.Contains("PEDAL + WHEELIE"),"wheelie start releases physics and shows touch control guidance");
            var rearField=rider.GetType().GetField("rearGrounded",BindingFlags.Instance|BindingFlags.NonPublic);
            var frontProperty=rider.GetType().GetProperty("FrontGrounded");
            rearField.SetValue(rider,true);frontProperty.SetValue(rider,true);rider.rb.rotation=Quaternion.identity;rider.rb.linearVelocity=Vector3.forward*3;rider.wheelieInput=true;rider.rawCustomAccelerationAxis=1;
            Invoke(m,"FixedUpdate");Check(m.Attempt.Progress==0,"holding wheelie control with both wheels down cannot earn mission time");
            frontProperty.SetValue(rider,false);Invoke(m,"FixedUpdate");Check(m.Attempt.Progress==0,"wheel contact alone without nose lift cannot count wheelie");
            rider.rb.rotation=Quaternion.Euler(-25,0,0);rider.rb.linearVelocity=Vector3.zero;Invoke(m,"FixedUpdate");Check(m.Attempt.Progress==0,"stationary lifted wheel cannot earn mission time");
            rider.rb.linearVelocity=Vector3.forward*3;rearField.SetValue(rider,false);Invoke(m,"FixedUpdate");Check(m.Attempt.Progress==0,"airborne wheel lift cannot earn mission time");
            rearField.SetValue(rider,true);Invoke(m,"FixedUpdate");Check(m.Attempt.Progress>0,"rear supported forward moving nose lift advances wheelie mission through telemetry");
            float held=m.Attempt.Progress;hud.Pause();Invoke(m,"FixedUpdate");Check(m.Attempt.Progress==held,"runtime pause freezes wheelie progress");hud.Resume();
            frontProperty.SetValue(rider,true);Invoke(m,"FixedUpdate");Invoke(m,"Update");Check(m.Attempt.Progress==0 && hud.Session.ObjectiveDetail.Contains("Hold reset"),"front touchdown resets duration and shows recovery guidance");
            frontProperty.SetValue(rider,false);int wheeliePoints=m.Career.points;
            for(int i=0;i<501;i++)Invoke(m,"FixedUpdate");Invoke(m,"Update");Invoke(hud,"Update");
            Check(m.Attempt.State==MissionState.Completed && m.Career.completed[7]==1 && m.Career.points==wheeliePoints+150 && hud.CurrentPage=="Mission Result","continuous wheelie telemetry completes mission with saved reward and results");
            Page.Find("Retry").GetComponent<Button>().onClick.Invoke();Check(m.Attempt.Definition.Level==2 && m.Attempt.Objective.target==35 && !m.Attempt.WheelieHoldBroken,"next wheelie level requires fresh thirty-five second hold");
            rider.GetComponent<CycleRecovery>().Crash();Invoke(m,"Update");Invoke(hud,"Update");Check(m.Attempt.State==MissionState.Failed && m.Career.points==wheeliePoints+150,"crash ends wheelie attempt without reward");
            Page.Find("Retry").GetComponent<Button>().onClick.Invoke();Check(m.Attempt.Definition.Level==2 && m.Attempt.Progress==0,"failed wheelie retries same tier with cleared duration");
            m.Abandon();hud.ShowMissionResult();Page.Find("FreeRide").GetComponent<Button>().onClick.Invoke();Check(!hud.IsPaused && m.Attempt.State==MissionState.Idle && !rider.rb.isKinematic,"wheelie abandon and free ride release countdown and clear mission");
            hud.ShowMissions();Page.Find("NextPage").GetComponent<Button>().onClick.Invoke();Page.Find("NextPage").GetComponent<Button>().onClick.Invoke();
            Check(Page.Find("Mission8").GetComponent<Text>().text.Contains("5 checkpoints"),"checkpoint run card shows ordered gate count on third page");
            Page.Find("Start8").GetComponent<Button>().onClick.Invoke();var checkpointContract=m.Attempt.Definition;
            var checkpointGate=GameObject.Find("Checkpoint run gate");var checkpointMarker=GameObject.Find("Active mission checkpoint");
            Check(checkpointContract.Id=="checkpoint_run" && checkpointGate!=null && checkpointGate.transform.childCount==3 && rider.rb.isKinematic,"checkpoint menu starts countdown with visible gate");
            foreach(var collider in checkpointGate.GetComponentsInChildren<Collider>())Check(!collider.enabled,"checkpoint gate cannot obstruct bicycle");
            foreach(int tier in new[]{1,100})foreach(var gate in MissionCatalog.Create(8,tier).Objectives) {
                Check(Physics.Raycast(gate.point+Vector3.up*.1f,Vector3.down,out var courseGround,1) && courseGround.normal.y>.9f,"checkpoint has flat highway ground");
                foreach(var obstacle in Physics.OverlapBox(gate.point+Vector3.up*1.3f,new Vector3(.6f,.9f,.6f)))Check(obstacle.isTrigger || obstacle.transform.IsChildOf(rider.transform),"checkpoint riding footprint has no obstacle");
            }
            m.Attempt.Tick(new MissionSample(),3,false);Invoke(m,"Update");
            var gateSample=new MissionSample {Grounded=true,Position=checkpointContract.Objectives[4].point};m.Attempt.Tick(gateSample,.02f,false);Invoke(m,"Update");
            Check(m.Attempt.ObjectiveIndex==0 && checkpointGate.transform.position==checkpointContract.Objectives[0].point,"visiting finish early leaves first gate active");
            int checkpointPoints=m.Career.points;
            for(int i=0;i<checkpointContract.Objectives.Length;i++) {
                if(i==4)Check(checkpointMarker.GetComponent<Renderer>().sharedMaterial.color==new Color(.1f,.9f,.3f),"final checkpoint marker turns green");
                gateSample.Position=checkpointContract.Objectives[i].point;m.Attempt.Tick(gateSample,1,false);Invoke(m,"Update");
                if(i<4)Check(checkpointGate.transform.position==checkpointContract.Objectives[i+1].point && hud.Session.ObjectiveDetail.Contains((i+1)+"/5 passed"),"gate and HUD advance to next ordered checkpoint");
            }
            Invoke(hud,"Update");Check(m.Attempt.State==MissionState.Completed && m.Career.completed[8]==1 && m.Career.points==checkpointPoints+175 && hud.CurrentPage=="Mission Result","checkpoint finish saves reward and displays results");
            Check(!checkpointGate.activeSelf && !checkpointMarker.activeSelf,"checkpoint results hide gate and ground marker");
            Page.Find("Retry").GetComponent<Button>().onClick.Invoke();Check(m.Attempt.Definition.Level==2 && m.Attempt.Definition.TimeLimit==118 && checkpointGate.activeSelf && m.Attempt.ObjectiveIndex==0,"next checkpoint run starts harder tier from first gate");
            hud.ResetCurrentBicycle();Invoke(m,"Update");Invoke(hud,"Update");Check(m.Attempt.State==MissionState.Failed && !checkpointGate.activeSelf && m.Career.points==checkpointPoints+175,"reset ends checkpoint attempt and clears gate without reward");
            Page.Find("Retry").GetComponent<Button>().onClick.Invoke();Check(m.Attempt.Definition.Level==2 && m.Attempt.ObjectiveIndex==0,"failed checkpoint retry preserves level and resets route");
            m.Abandon();Invoke(m,"Update");Check(!checkpointGate.activeSelf && !rider.rb.isKinematic,"checkpoint abandon removes gate and countdown lock");
            m.Dismiss();hud.ShowMissions();for(int i=0;i<3;i++)Page.Find("NextPage").GetComponent<Button>().onClick.Invoke();
            Check(Page.Find("Start9")!=null && Page.Find("NextPage").GetComponent<Button>().interactable,"fourth mission page exposes police escape and leads to boss page");
            Page.Find("Start9").GetComponent<Button>().onClick.Invoke();
            var police=GameObject.Find("Police cyclist");var pursuitArea=GameObject.Find("Pursuit area");
            Check(police!=null && pursuitArea!=null && police.transform.position.z== -258 && rider.rb.isKinematic,"police mission spawns pursuit behind locked countdown rider");
            Check(police.GetComponentsInChildren<MonoBehaviour>().Length==0 && police.GetComponentsInChildren<Rigidbody>().Length==0 && police.transform.Find("Police beacon").gameObject.activeSelf,"police visual has beacon and no player controllers or rigid bodies");
            Check(!finish.activeSelf && !checkpointGate.activeSelf,"police mission does not show rival finish or old checkpoint gate");
            m.Attempt.Tick(new MissionSample(),3,false);Invoke(m,"Update");
            var escapeRider=new MissionSample {Position=new Vector3(850,0,-240),Grounded=true};
            for(int i=0;i<100;i++) {escapeRider.Position.z+=.16f;m.Attempt.Tick(escapeRider,.02f,false);}Invoke(m,"Update");
            Check(police.transform.position.z> -258 && hud.Session.ObjectiveDetail.Contains("Gap"),"police move and HUD displays lead and escape hold");
            var policePosition=police.transform.position;var beaconColor=police.transform.Find("Police beacon").GetComponent<Renderer>().sharedMaterial.color;
            m.Attempt.Tick(escapeRider,5,true);Invoke(m,"Update");Check(police.transform.position==policePosition && police.transform.Find("Police beacon").GetComponent<Renderer>().sharedMaterial.color==beaconColor,"paused pursuit freezes police and beacon phase");
            int policePoints=m.Career.points;
            for(int i=0;i<2000 && m.Attempt.Active;i++) {escapeRider.Position.z+=.16f;m.Attempt.Tick(escapeRider,.02f,false);}Invoke(m,"Update");Invoke(hud,"Update");
            Check(m.Attempt.State==MissionState.Completed && m.Career.completed[9]==1 && m.Career.points==policePoints+200 && hud.CurrentPage=="Mission Result","escape opens results and grants career reward");
            Check(!police.activeSelf && !pursuitArea.activeSelf,"escaped pursuit removes police and area visuals");
            Page.Find("Retry").GetComponent<Button>().onClick.Invoke();Check(m.Attempt.Definition.Level==2 && m.Attempt.Objective.radius==37 && police.activeSelf,"next police tier increases escape gap and restores police");
            m.Attempt.Tick(new MissionSample(),3,false);escapeRider.Position=new Vector3(850,0,-240);
            for(int i=0;i<1000 && m.Attempt.Active;i++)m.Attempt.Tick(escapeRider,.02f,false);Invoke(m,"Update");Invoke(hud,"Update");
            Check(m.Attempt.State==MissionState.Failed && m.Career.points==policePoints+200 && Page.Find("Outcome").GetComponent<Text>().text.Contains("Caught"),"capture produces loss result without reward");
            Page.Find("Retry").GetComponent<Button>().onClick.Invoke();hud.ResetCurrentBicycle();Invoke(m,"Update");Check(m.Attempt.State==MissionState.Failed && !police.activeSelf && !pursuitArea.activeSelf && !rider.rb.isKinematic,"reset clears pursuit visuals and releases countdown lock");
            m.Dismiss();m.Begin(6);Check(GameObject.Find("Rival cyclist")!=null && !police.transform.Find("Police beacon").gameObject.activeSelf && finish.activeSelf,"returning to rival sprint restores race appearance without police beacon");m.Abandon();Invoke(m,"Update");
            m.Dismiss();hud.ShowMissions();for(int i=0;i<3;i++)Page.Find("NextPage").GetComponent<Button>().onClick.Invoke();
            Check(Page.Find("Start9")!=null && Page.Find("Mission10").GetComponent<Text>().text.Contains("24 m summit"),"fourth page shows mountain ascent beside police escape");
            Page.Find("Start10").GetComponent<Button>().onClick.Invoke();var mountain=m.Attempt.Definition;
            var mountainGate=GameObject.Find("Mountain ascent gate");
            Check(mountain.Id=="mountain_ascent" && hud.World.LastDestination==2 && mountainGate!=null && rider.rb.isKinematic,"ascent menu travels to mountain base and starts locked countdown");
            Check(Quaternion.Angle(mountainGate.transform.rotation,Quaternion.identity)>1,"climb gate follows sloping road orientation");
            foreach(var collider in mountainGate.GetComponentsInChildren<Collider>())Check(!collider.enabled,"mountain gate does not obstruct climb");
            foreach(int tier in new[]{1,100})foreach(var target in MissionCatalog.Create(10,tier).Objectives) {
                Check(Physics.Raycast(target.point+Vector3.up*.5f,Vector3.down,out var mountainGround,1) && Mathf.Abs(mountainGround.point.y-target.point.y)<.1f,"mountain checkpoint is supported at specified road elevation");
                foreach(var obstacle in Physics.OverlapBox(target.point+Vector3.up*1.4f,new Vector3(.5f,.7f,.5f)))Check(obstacle.isTrigger || obstacle.transform.IsChildOf(rider.transform),"mountain checkpoint rider footprint is clear");
            }
            m.Attempt.Tick(new MissionSample(),3,false);Invoke(m,"Update");Check(!rider.rb.isKinematic && hud.Session.ObjectiveDetail.Contains("Altitude"),"climb countdown releases bicycle and HUD displays altitude");
            var ascentSample=new MissionSample {Grounded=true,Position=mountain.Objectives[6].point};m.Attempt.Tick(ascentSample,.02f,false);Check(m.Attempt.ObjectiveIndex==0,"summit visit before ascent checkpoints does not finish mission");
            int ascentPoints=m.Career.points;
            for(int i=0;i<7;i++) {
                if(i==6)Check(checkpointMarker.GetComponent<Renderer>().sharedMaterial.color==new Color(.1f,.9f,.3f),"summit finish marker turns green");
                ascentSample.Position=mountain.Objectives[i].point;m.Attempt.Tick(ascentSample,1,false);Invoke(m,"Update");
                if(i<6)Check(mountainGate.transform.position==mountain.Objectives[i+1].point,"climb gate advances along ordered mountain route");
            }
            Invoke(hud,"Update");Check(m.Career.completed[10]==1 && m.Career.points==ascentPoints+200 && hud.CurrentPage=="Mission Result","summit completion records ascent reward and shows results");
            Check(!mountainGate.activeSelf && !checkpointMarker.activeSelf,"ascent result clears mountain markers");
            Page.Find("Retry").GetComponent<Button>().onClick.Invoke();Check(m.Attempt.Definition.Level==2 && m.Attempt.Definition.TimeLimit==147 && m.Attempt.ObjectiveIndex==0 && hud.World.LastDestination==2,"next ascent starts harder tier from mountain base");
            hud.ResetCurrentBicycle();Invoke(m,"Update");Invoke(hud,"Update");Check(m.Attempt.State==MissionState.Failed && !mountainGate.activeSelf && m.Career.points==ascentPoints+200,"reset ends climb and hides markers without reward");
            Page.Find("Retry").GetComponent<Button>().onClick.Invoke();Check(m.Attempt.Definition.Level==2 && m.Attempt.ObjectiveIndex==0,"failed ascent retry preserves tier and resets checkpoints");m.Abandon();Invoke(m,"Update");
            m.Dismiss();m.Begin(8);Check(checkpointGate.transform.rotation==Quaternion.identity && checkpointMarker.transform.rotation==Quaternion.identity,"switching from climb to highway restores flat checkpoint orientation");m.Abandon();Invoke(m,"Update");
            m.Dismiss();hud.ShowMissions();for(int i=0;i<3;i++)Page.Find("NextPage").GetComponent<Button>().onClick.Invoke();
            Check(Page.Find("Mission11").GetComponent<Text>().text.Contains("6 m gap"),"fourth page exposes Canyon Jump course");Page.Find("Start11").GetComponent<Button>().onClick.Invoke();
            Check(m.Attempt.Definition.Id=="canyon_jump" && Vector3.Distance(rider.transform.position,new Vector3(1000,0,245))<.3f && rider.rb.isKinematic,"canyon mission places rider on run-up and holds countdown");
            Check(Physics.Raycast(new Vector3(1000,3.1f,279),Vector3.down,out var launchGround,1) && launchGround.collider.name=="Canyon takeoff deck","takeoff zone has solid elevated deck");
            Check(!Physics.Raycast(new Vector3(1000,3,283),Vector3.down,5),"six metre canyon gap has no rideable surface between banks");
            Check(Physics.Raycast(new Vector3(1000,3.1f,289),Vector3.down,out var landingGround,1) && landingGround.collider.name=="Canyon landing deck","landing zone has solid elevated deck");
            Check(Physics.Raycast(new Vector3(1000,.1f,245),Vector3.down,1),"canyon run-up spawn has flat ground");
            m.Attempt.Tick(new MissionSample(),3,false);Invoke(m,"Update");
            Check(hud.Session.ObjectiveDetail.Contains("charge") && !rider.rb.isKinematic,"jump HUD teaches charge and countdown releases bicycle");
            var jumpSample=new MissionSample {Position=new Vector3(1000,3.5f,279),Grounded=true,StableGrounded=true};m.Attempt.Tick(jumpSample,.02f,false);Invoke(m,"Update");
            Check(m.Attempt.JumpReady && hud.Session.ObjectiveDetail.Contains("Release JUMP"),"takeoff support arms jump and prompts release");
            jumpSample.Grounded=jumpSample.StableGrounded=false;jumpSample.Position=new Vector3(1000,4.5f,283);m.Attempt.Tick(jumpSample,.4f,false);Invoke(m,"Update");
            Check(checkpointMarker.transform.position.z==289 && checkpointMarker.GetComponent<Renderer>().sharedMaterial.color==new Color(.1f,.9f,.3f),"airborne jump targets green landing zone");
            int canyonPoints=m.Career.points;jumpSample.Position=new Vector3(1000,3.5f,289);jumpSample.Grounded=jumpSample.StableGrounded=true;m.Attempt.Tick(jumpSample,.3f,false);Invoke(m,"Update");Invoke(hud,"Update");
            Check(m.Career.completed[11]==1 && m.Career.points==canyonPoints+225 && hud.CurrentPage=="Mission Result" && !checkpointMarker.activeSelf,"clean canyon landing records reward and hides target");
            Page.Find("Retry").GetComponent<Button>().onClick.Invoke();Check(m.Attempt.Definition.Level==2 && m.Attempt.Definition.TimeLimit==88 && !m.Attempt.JumpInFlight,"next canyon tier starts tighter attempt with cleared flight");
            hud.ResetCurrentBicycle();Invoke(m,"Update");Invoke(hud,"Update");Check(m.Attempt.State==MissionState.Failed && !checkpointMarker.activeSelf && m.Career.points==canyonPoints+225,"reset terminates canyon attempt without another reward");
            Page.Find("Retry").GetComponent<Button>().onClick.Invoke();Check(m.Attempt.Definition.Level==2 && !m.Attempt.JumpReady,"canyon retry keeps same tier and clears takeoff");m.Abandon();Invoke(m,"Update");
            Check(!rider.rb.isKinematic && !checkpointMarker.activeSelf,"canyon abandon releases countdown and clears target");
            m.Dismiss();hud.ShowMissions();for(int i=0;i<4;i++)Page.Find("NextPage").GetComponent<Button>().onClick.Invoke();
            Check(Page.Find("Start12")!=null && !Page.Find("NextPage").GetComponent<Button>().interactable,"fifth mission page exposes boss race and stops pagination");
            Page.Find("Start12").GetComponent<Button>().onClick.Invoke();var bossVisual=GameObject.Find("Boss cyclist");var bossFinishVisual=GameObject.Find("Boss race finish");
            Check(m.Attempt.Definition.Id=="boss_race" && bossVisual!=null && bossFinishVisual!=null && rider.rb.isKinematic,"boss mission spawns opponent and finish during countdown");
            Check(bossVisual.transform.Find("Frame").GetComponent<Renderer>().sharedMaterial.color==new Color(1,.7f,.05f) && !bossVisual.transform.Find("Police beacon").gameObject.activeSelf,"boss has gold appearance without police beacon");
            Check(bossFinishVisual.transform.position.z==260 && bossFinishVisual.transform.Find("Left boundary").localScale.z==500,"boss first course markers cover five hundred metres");
            Check(Physics.Raycast(new Vector3(854,.1f,400),Vector3.down,1),"maximum boss finish has highway ground");
            m.Attempt.Tick(new MissionSample(),3,false);Invoke(m,"Update");var bossSample=new MissionSample {Position=new Vector3(850,0,-240),Grounded=true};m.Attempt.Tick(bossSample,1,false);Invoke(m,"Update");
            Check(bossVisual.transform.position.z> -240 && hud.Session.ObjectiveDetail.Contains("Boss"),"boss moves and HUD identifies boss race position");
            var bossPose=bossVisual.transform.position;m.Attempt.Tick(bossSample,10,true);Invoke(m,"Update");Check(bossVisual.transform.position==bossPose,"pause freezes boss cyclist");
            int bossPoints=m.Career.points;bossSample.Position.z+=500;m.Attempt.Tick(bossSample,59,false);Invoke(m,"Update");Invoke(hud,"Update");
            Check(m.Career.completed[12]==1 && m.Career.points==bossPoints+300 && hud.CurrentPage=="Mission Result" && Page.Find("Outcome").GetComponent<Text>().text.Contains("defeated the boss"),"boss victory records reward and named result");
            Check(!bossVisual.activeSelf && !bossFinishVisual.activeSelf,"boss result hides opponent and course");
            Page.Find("Retry").GetComponent<Button>().onClick.Invoke();Check(m.Attempt.Definition.Level==2 && m.Attempt.Objective.target==510 && bossVisual.activeSelf,"next boss level expands course and restores opponent");
            m.Attempt.Tick(new MissionSample(),3,false);bossSample.Position=new Vector3(850,0,-240);m.Attempt.Tick(bossSample,100,false);Invoke(m,"Update");Invoke(hud,"Update");
            Check(m.Attempt.State==MissionState.Failed && m.Career.points==bossPoints+300 && Page.Find("Outcome").GetComponent<Text>().text.Contains("boss reached"),"boss loss displays result without reward");
            Page.Find("Retry").GetComponent<Button>().onClick.Invoke();hud.ResetCurrentBicycle();Invoke(m,"Update");Check(m.Attempt.State==MissionState.Failed && !bossVisual.activeSelf && !bossFinishVisual.activeSelf && !rider.rb.isKinematic,"reset clears boss visuals and countdown lock");
            m.Dismiss();m.Begin(6);Check(GameObject.Find("Rival cyclist")!=null && GameObject.Find("Rival sprint finish")!=null && bossVisual.transform.Find("Frame").GetComponent<Renderer>().sharedMaterial.color==new Color(.95f,.25f,.1f),"switching from boss restores ordinary rival appearance");m.Abandon();Invoke(m,"Update");
            m.Dismiss();hud.Resume();
            var handlingMode=Physics.simulationMode;Physics.simulationMode=SimulationMode.Script;
            foreach(float turnDirection in new[]{-1f,1f}) {
                hud.World.Travel(3);rider.wheelieInput=false;rider.rawCustomAccelerationAxis=rider.customAccelerationAxis=rider.customSteerAxis=rider.customLeanAxis=0;
                for(int i=0;i<20;i++){Invoke(rider,"FixedUpdate");Physics.Simulate(.02f);}
                float headingBefore=rider.rb.rotation.eulerAngles.y;rider.customSteerAxis=turnDirection;
                rider.rb.linearVelocity=rider.fWheelRb.linearVelocity=rider.rWheelRb.linearVelocity=Vector3.forward*6;
                for(int i=0;i<50;i++){Invoke(rider,"FixedUpdate");Physics.Simulate(.02f);}
                float actualTurn=Mathf.DeltaAngle(headingBefore,rider.rb.rotation.eulerAngles.y)*turnDirection;
                Check(actualTurn>15 && actualTurn<140 && !rider.GetComponent<CycleRecovery>().IsCrashed,"imported gameplay bicycle makes controlled "+(turnDirection<0?"left":"right")+" turn without crashing");
            }
            Physics.simulationMode=handlingMode;
            m.Dismiss();hud.Pause();Page.Find("Freestyle").GetComponent<Button>().onClick.Invoke();
            Check(hud.CurrentPage=="Freestyle" && Page.Find("ChallengeStart0")!=null && Page.Find("Points").GetComponent<Text>().text.Contains("FREESTYLE POINTS"),"pause opens freestyle with downtown challenges and separate points");
            Page.Find("NextZone").GetComponent<Button>().onClick.Invoke();Check(Page.Find("Locked")!=null && Page.Find("ChallengeStart3")==null && !m.BeginFreestyle(3),"industrial challenges are locked in UI and service before points threshold");
            Page.Find("PreviousZone").GetComponent<Button>().onClick.Invoke();int originalCareer=m.Career.points;
            Page.Find("ChallengeStart0").GetComponent<Button>().onClick.Invoke();Check(m.IsFreestyle && m.Attempt.State==MissionState.Countdown && hud.World.LastDestination==0 && !hud.IsPaused,"freestyle start travels to selected zone and begins countdown");
            Check(!m.Begin(0) && !m.BeginFreestyle(1),"career and freestyle cannot replace active attempt");
            m.Attempt.Tick(new MissionSample(),3,false);Invoke(m,"Update");
            var landMethod=m.GetType().GetMethod("LandStunt",BindingFlags.Instance|BindingFlags.NonPublic);
            var longWheelie=new StuntResult {Kind=StuntKind.Wheelie,Distance=100,Duration=20};
            hud.Pause();landMethod.Invoke(m,new object[]{longWheelie});Check(m.Attempt.Progress==0,"paused freestyle ignores stunt callbacks");hud.Resume();landMethod.Invoke(m,new object[]{longWheelie});Invoke(m,"Update");Invoke(hud,"Update");
            Check(m.Freestyle.completed[0]==1 && m.Freestyle.points==150 && m.Career.points==originalCareer && hud.CurrentPage=="Freestyle Result","freestyle completion grants only freestyle points and opens dedicated results");
            Page.Find("Retry").GetComponent<Button>().onClick.Invoke();Check(m.Attempt.Definition.Level==2 && m.Attempt.Objective.target==110,"freestyle next level increases challenge difficulty");
            m.Attempt.Tick(new MissionSample(),3,false);Invoke(m,"Update");longWheelie.Distance=110;landMethod.Invoke(m,new object[]{longWheelie});Invoke(m,"Update");Invoke(hud,"Update");
            Check(m.Freestyle.points==325 && FreestyleCatalog.Unlocked(1,m.Freestyle.points),"earned freestyle points unlock industrial zone");
            Page.Find("Challenges").GetComponent<Button>().onClick.Invoke();Page.Find("NextZone").GetComponent<Button>().onClick.Invoke();Check(Page.Find("ChallengeStart3")!=null && Page.Find("Locked")==null,"unlocked industrial zone exposes its challenge cards");
            Page.Find("Explore").GetComponent<Button>().onClick.Invoke();Check(!hud.IsPaused && m.Attempt.State==MissionState.Idle && hud.World.LastDestination==1 && hud.Session.Objective.Contains("FREESTYLE"),"explore enters unlocked zone without timed attempt");
            Check(m.BeginFreestyle(4),"industrial balance route starts after exploration");m.Attempt.Tick(new MissionSample(),3,false);Invoke(m,"Update");Check(GameObject.Find("Active mission checkpoint")!=null,"freestyle route shows next checkpoint marker");
            hud.ResetCurrentBicycle();Invoke(m,"Update");Invoke(hud,"Update");Check(m.Attempt.State==MissionState.Failed && m.Freestyle.points==325 && hud.CurrentPage=="Freestyle Result","reset ends freestyle attempt without points");
            Page.Find("Retry").GetComponent<Button>().onClick.Invoke();Check(m.IsFreestyle && m.Selected==4 && m.Attempt.Definition.Level==1,"freestyle failure retries same challenge and tier");
            m.Attempt.Tick(new MissionSample(),3,false);Invoke(m,"Update");rider.rb.position=new Vector3(850,0,0);Invoke(m,"FixedUpdate");Check(m.Attempt.State==MissionState.Failed,"leaving selected freestyle zone ends challenge");
            m.Dismiss();Check(m.Begin(0) && !m.IsFreestyle,"career mission remains selectable after freestyle");m.Abandon();Invoke(m,"Update");
            var freestyleRoundTrip=MissionCareer.FromJson(m.Freestyle.ToJson(),FreestyleCatalog.Count);Check(freestyleRoundTrip.completed[0]==2 && freestyleRoundTrip.points==325 && m.Career.points==originalCareer,"freestyle save round trip preserves independent progress");
            m.Dismiss();hud.Pause();Page.Find("Settings").GetComponent<Button>().onClick.Invoke();
            Check(hud.CurrentPage=="Settings" && Page.Find("SoundFX")!=null && Page.Find("Music")!=null,"settings page exposes independent effects and music sliders");
            var soundSlider=Page.Find("SoundFX").GetComponent<Slider>();var musicSlider=Page.Find("Music").GetComponent<Slider>();
            musicSlider.value=.6f;soundSlider.value=.2f;Check(hud.Settings.Preferences.effects==.2f && hud.Settings.Preferences.music==.6f && AudioListener.volume==1,"settings sliders update separate channels without global master scaling");
            Check(hud.Settings.Music.clip!=null && hud.Settings.Music.ignoreListenerPause && hud.IsPaused,"background music is available while paused settings preview volume");
            var cycleSounds=UnityEngine.Object.FindFirstObjectByType<SBPScripts.BicycleSounds>();
            Check(cycleSounds!=null && cycleSounds.pedallingAudioSource.GetComponent<SettingsAudioChannel>()!=null,"existing bicycle effects source is routed through settings");
            Page.Find("Quality0").GetComponent<Button>().onClick.Invoke();Check(hud.Settings.Preferences.graphics==0 && Application.targetFrameRate==30 && !Page.Find("Quality0").GetComponent<Button>().interactable,"Low quality applies immediately and selection is shown");
            Page.Find("Quality3").GetComponent<Button>().onClick.Invoke();Check(hud.Settings.Preferences.graphics==3 && Application.targetFrameRate==60 && !Page.Find("Quality3").GetComponent<Button>().interactable,"2K quality applies and stays selected");
            foreach(string control in new[]{"SoundFX","Music","Quality0","Quality1","Quality2","Quality3","Back"}) {
                var rect=Page.Find(control).GetComponent<RectTransform>();Check(Mathf.Abs(rect.anchoredPosition.x)+rect.sizeDelta.x/2<=550 && Mathf.Abs(rect.anchoredPosition.y)+rect.sizeDelta.y/2<=285,"settings control stays inside safe modal: "+control);
            }
            Page.Find("Back").GetComponent<Button>().onClick.Invoke();hud.ShowSettings();Check(Page.Find("Music").GetComponent<Slider>().value==.6f && !Page.Find("Quality3").GetComponent<Button>().interactable,"reopening settings retains volumes and graphics selection");
            hud.Settings.SetEffects(1);hud.Settings.SetMusic(.35f);hud.Settings.SetGraphics(1);hud.Resume();Check(!hud.IsPaused && !AudioListener.pause,"settings exit restores gameplay audio and simulation");
            hud.Pause();Invoke(hud,"ShowPausePage");Page.Find("Shop").GetComponent<Button>().onClick.Invoke();
            Check(hud.CurrentPage=="Shop" && hud.IsPaused,"pause menu opens shop safely");
            int balance=hud.Economy.coins;Page.Find("Daily").GetComponent<Button>().onClick.Invoke();
            Check(hud.Economy.coins==balance+100 && !Page.Find("Daily").GetComponent<Button>().interactable,"daily button grants coins once and disables");
            Check(hud.Session.Coins==hud.Economy.coins && hud.Session.Gems==hud.Economy.gems,"HUD balances follow wallet transactions");
            Check(Page.Find("BuyBike")!=null && Page.Find("BuyUpgrade0")!=null && Page.Find("BuyPaint")!=null,"shop exposes bicycles upgrades and cosmetic purchases");
            Page.Find("Garage").GetComponent<Button>().onClick.Invoke();Check(hud.CurrentPage=="Garage","shop opens garage to equip purchases");
            Invoke(hud,"ShowPausePage");Page.Find("Advanced").GetComponent<Button>().onClick.Invoke();
            Check(hud.CurrentPage=="Advanced" && hud.Environment!=null && hud.Replay!=null,"advanced menu and services initialize");
            Page.Find("View").GetComponent<Button>().onClick.Invoke();var rideCamera=UnityEngine.Object.FindFirstObjectByType<SBPScripts.BicycleCamera>();
            Check(hud.Advanced.firstPerson && rideCamera.FirstPerson && Vector3.Distance(rideCamera.transform.position,hud.bicycle.transform.position)<2,"first person switches to rider eye position");
            Page.Find("View").GetComponent<Button>().onClick.Invoke();Check(!rideCamera.FirstPerson,"third person restores follow camera");
            Page.Find("Weather").GetComponent<Button>().onClick.Invoke();Check(hud.Environment.Weather==1,"weather button enables rain");
            Page.Find("Time").GetComponent<Button>().onClick.Invoke();Check(hud.Environment.Hour==18,"time button selects dusk");
            Page.Find("Cycle").GetComponent<Button>().onClick.Invoke();Check(hud.Environment.Cycle,"day-night cycle toggles on");
            Check(!Page.Find("Record").GetComponent<Button>().interactable,"editor clearly disables unsupported native video recording");
            Page.Find("Skills").GetComponent<Button>().onClick.Invoke();Check(hud.CurrentPage=="Skills","skill tree opens");
            if(hud.Advanced.Available(1000)>0){hud.Advanced.schoolCompleted=3;Invoke(hud,"ShowSkills");Page.Find("Learn0").GetComponent<Button>().onClick.Invoke();Check(hud.Advanced.skills[0]==1 && hud.bicycle.balanceAssist==10,"learning balance applies assistance to equipped bicycle");}
            Page.Find("Back").GetComponent<Button>().onClick.Invoke();Page.Find("School").GetComponent<Button>().onClick.Invoke();
            var checkpointBefore=hud.bicycle.GetComponent<CycleRecovery>().CheckpointPosition;
            Page.Find("Lesson0").GetComponent<Button>().onClick.Invoke();Check(hud.School.Active && !hud.IsPaused && hud.bicycle.transform.position.x>1400,"school starts on isolated practice course");
            hud.ResetCurrentBicycle();Check(!hud.School.Active && !hud.School.Passed,"reset interrupts school attempt");
            hud.Pause();Page.Find("Advanced").GetComponent<Button>().onClick.Invoke();Page.Find("School").GetComponent<Button>().onClick.Invoke();
            Check(Vector3.Distance(hud.bicycle.GetComponent<CycleRecovery>().CheckpointPosition,checkpointBefore)<.01f,"leaving school restores original respawn checkpoint");
            hud.ShowGarage();Invoke(hud,"ShowShop");Page.Find("Content").GetComponent<Button>().onClick.Invoke();
            Check(hud.CurrentPage=="Recurring" && hud.IsPaused,"shop opens daily challenge screen");
            long today=GameEconomy.Day(DateTime.UtcNow);
            Invoke(hud,"TickRecurring");Invoke(hud,"FlushRecurring");hud.Economy.recurring.daily[0]=0;
            hud.Session.RecordTravel(Vector3.forward*10,10,true);Invoke(hud,"TickRecurring");Invoke(hud,"FlushRecurring");
            Check(Mathf.Abs(hud.Economy.recurring.daily[0]-100)<.01f,"actual ride-distance accounting feeds recurring goals");
            hud.Economy.recurring.daily[1]=0;typeof(GameplayHud).GetMethod("OnGarageScore",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(hud,new object[]{200});
            Check(hud.Economy.recurring.daily[1]==200,"banked-score reward path feeds recurring goals");
            hud.Economy.recurring.daily[2]=0;hud.Economy.career[0]=0;hud.RewardMission(false,0,1,100);hud.RewardMission(false,0,1,100);
            Check(hud.Economy.recurring.daily[2]==1,"new mission reward feeds recurring goals once");
            hud.Economy.recurring.Add(0,1000,today);Invoke(hud,"ShowRecurring");
            int coinsBefore=hud.Economy.coins;Page.Find("Claim0").GetComponent<Button>().onClick.Invoke();
            Check(hud.Economy.coins==coinsBefore+200 && !Page.Find("Claim0").GetComponent<Button>().interactable,"daily claim button pays once and disables");
            Page.Find("Period").GetComponent<Button>().onClick.Invoke();Check(Page.Find("Reset")!=null && hud.CurrentPage=="Recurring","weekly challenge page opens");
            Page.Find("Events").GetComponent<Button>().onClick.Invoke();Check(hud.CurrentPage=="Events" && !Page.Find("RewardedAd").GetComponent<Button>().interactable,"events keep rewarded ads unavailable as requested");
            hud.Economy.gems=40;Invoke(hud,"ShowEvents");Page.Find("Sunset").GetComponent<Button>().onClick.Invoke();Check(hud.Economy.sunsetPaint && hud.Economy.gems==30,"cosmetic purchase debits gems through wallet transaction");
            Page.Find("Back").GetComponent<Button>().onClick.Invoke();Page.Find("Season").GetComponent<Button>().onClick.Invoke();
            Page.Find("BuyPass").GetComponent<Button>().onClick.Invoke();Check(hud.Economy.recurring.bonusPass && hud.Economy.gems==5,"season bonus pass purchased with earned gems");
            hud.Economy.recurring.AddXP(2500);Invoke(hud,"ShowSeason");Page.Find("Free1").GetComponent<Button>().onClick.Invoke();Check((hud.Economy.recurring.freeClaims&1)!=0,"season free-track claim persists");
            Page.Find("Next").GetComponent<Button>().onClick.Invoke();Page.Find("Bonus10").GetComponent<Button>().onClick.Invoke();Check(hud.Economy.violetPaint,"final season reward grants permanent cosmetic");
            foreach(string control in new[]{"Free6","Bonus6","Free10","Bonus10","Back","BuyPass","Next"}) {var rect=Page.Find(control).GetComponent<RectTransform>();Check(Mathf.Abs(rect.anchoredPosition.x)+rect.sizeDelta.x/2<=550 && Mathf.Abs(rect.anchoredPosition.y)+rect.sizeDelta.y/2<=285,"season control stays inside modal: "+control);}
            Check(errors==0,"mission integration produced no runtime errors");Debug.Log("MISSION_SCENE_VALIDATION_SUCCESS "+count);EditorApplication.Exit(0);
        } catch(Exception e) {Debug.LogException(e);EditorApplication.Exit(1);}
    }
}

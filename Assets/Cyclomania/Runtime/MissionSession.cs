using System;
using UnityEngine;

namespace Cyclomania
{
    public enum MissionState { Idle, Countdown, Running, Completed, Failed, Abandoned }
    public enum ObjectiveKind { ReachPoint, Distance, SpeedHold, WheelieHold, BankedScore, PedalHold, BrakeStop, SteerTurn, PickupPackage, DeliverPackage, RivalRace, PoliceEscape, CanyonJump, LandedWheelieDistance, LandedWheelieHold, LandedStoppieHold, LandedFlips, LandedBackflips, LandedAirtime, LandedJumpCount, LandedCombo }
    [Serializable]
    public sealed class MissionObjective
    {
        public ObjectiveKind kind;
        public string instruction;
        public float target=1, speedKmh, radius=6;
        public float rivalAcceleration=2;
        public int direction=1;
        public Vector3 point, zoneSize, landingPoint;
        public float landingRadius=3;
        public bool useZone;
        public bool Contains(Vector3 position) => !useZone || (Mathf.Abs(position.x-point.x)<=zoneSize.x*.5f && Mathf.Abs(position.y-point.y)<=zoneSize.y*.5f && Mathf.Abs(position.z-point.z)<=zoneSize.z*.5f);
    }
    public sealed class MissionDefinition
    {
        public string Id, Title;
        public int Level=1, Destination, Reward;
        public float TimeLimit;
        public MissionObjective[] Objectives;
    }
    public struct MissionSample
    {
        public Vector3 Position;
        public float SpeedKmh, Acceleration, Steering, YawDelta;
        public bool Grounded, Wheelie, StableGrounded;
    }
    // Independent of scene, UI, and save storage. One attempt can finish only once.
    public sealed class MissionSession
    {
        public MissionState State { get; private set; }
        public MissionDefinition Definition { get; private set; }
        public int ObjectiveIndex { get; private set; }
        public float Progress { get; private set; }
        public float Elapsed { get; private set; }
        public float Countdown { get; private set; }
        public bool CarryingPackage { get; private set; }
        public bool BrakeArmed { get; private set; }
        public bool WheelieHoldBroken { get; private set; }
        public float RivalDistance { get; private set; }
        public bool JumpReady { get; private set; }
        public bool JumpInFlight { get; private set; }
        public float JumpAirtime { get; private set; }
        private float landingWait;
        private bool jumpTouchedDown;
        private float previousRaceDistance;
        public Vector3 PolicePosition { get; private set; }
        public Vector3 PoliceFacing { get; private set; }=Vector3.forward;
        public float PoliceGap { get; private set; }
        public float CaptureProgress { get; private set; }
        public float PoliceDistance { get; private set; }
        private Vector3 previousPolicePlayer;
        private float policeSpeed;
        public string Result { get; private set; }="";
        public bool Active => State==MissionState.Countdown || State==MissionState.Running;
        public MissionObjective Objective => Definition!=null && ObjectiveIndex<Definition.Objectives.Length ? Definition.Objectives[ObjectiveIndex] : null;
        public event Action Completed;
        public bool Start(MissionDefinition definition)
        {
            if(Active || definition==null || string.IsNullOrEmpty(definition.Id) || definition.Level<1 || !Valid(definition.TimeLimit) || definition.TimeLimit<=0 || definition.Objectives==null || definition.Objectives.Length==0) return false;
            foreach(var item in definition.Objectives)
                if(item==null || !Valid(item.target) || item.target<=0 || !Valid(item.radius) || item.radius<=0 || !Valid(item.speedKmh) || item.speedKmh<0 || !Valid(item.point.sqrMagnitude) || !Enum.IsDefined(typeof(ObjectiveKind),item.kind) || (item.kind==ObjectiveKind.SteerTurn && item.direction!=-1 && item.direction!=1)) return false;
            foreach(var item in definition.Objectives)
                if(item.useZone && (item.kind!=ObjectiveKind.SpeedHold || !Valid(item.zoneSize.sqrMagnitude) || item.zoneSize.x<=0 || item.zoneSize.y<=0 || item.zoneSize.z<=0))return false;
            foreach(var item in definition.Objectives)
                if((item.kind==ObjectiveKind.RivalRace || item.kind==ObjectiveKind.PoliceEscape) && (definition.Objectives.Length!=1 || item.speedKmh<=0))return false;
            foreach(var item in definition.Objectives)if(item.kind==ObjectiveKind.PoliceEscape && item.radius<=3)return false;
            foreach(var item in definition.Objectives)if(item.kind==ObjectiveKind.CanyonJump && (definition.Objectives.Length!=1 || !Valid(item.landingPoint.sqrMagnitude) || !Valid(item.landingRadius) || item.landingRadius<=0))return false;
            foreach(var item in definition.Objectives)if(item.kind==ObjectiveKind.RivalRace && (!Valid(item.rivalAcceleration) || item.rivalAcceleration<=0))return false;
            bool package=false;
            foreach(var item in definition.Objectives) {
                if(item.kind==ObjectiveKind.PickupPackage) {if(package)return false;package=true;}
                if(item.kind==ObjectiveKind.DeliverPackage) {if(!package)return false;package=false;}
            }
            if(package)return false;
            // Freeze the attempt so catalog or editor changes cannot mutate a running objective.
            var copy=new MissionObjective[definition.Objectives.Length];
            for(int i=0;i<copy.Length;i++) {var o=definition.Objectives[i];copy[i]=new MissionObjective {kind=o.kind,instruction=o.instruction,target=o.target,speedKmh=o.speedKmh,radius=o.radius,point=o.point,direction=o.direction,useZone=o.useZone,zoneSize=o.zoneSize,landingPoint=o.landingPoint,landingRadius=o.landingRadius,rivalAcceleration=o.rivalAcceleration};}
            Definition=new MissionDefinition {Id=definition.Id,Title=definition.Title,Level=definition.Level,Destination=definition.Destination,Reward=Math.Max(0,definition.Reward),TimeLimit=definition.TimeLimit,Objectives=copy};
            previousPolicePlayer=copy[0].point;PolicePosition=previousPolicePlayer-Vector3.forward*18;
            PoliceFacing=Vector3.forward;PoliceGap=18;CaptureProgress=PoliceDistance=policeSpeed=0;
            JumpReady=JumpInFlight=jumpTouchedDown=false;JumpAirtime=landingWait=0;WheelieHoldBroken=false;RivalDistance=previousRaceDistance=0;ObjectiveIndex=0;CarryingPackage=false;BrakeArmed=false;Progress=Elapsed=0;Countdown=3;Result="";State=MissionState.Countdown;return true;
        }
        public void Tick(MissionSample sample,float seconds,bool paused)
        {
            if(!Active || paused || !Valid(seconds) || seconds<=0 || !Valid(sample.Position.sqrMagnitude) || !Valid(sample.SpeedKmh) || sample.SpeedKmh<0 || !Valid(sample.Acceleration) || !Valid(sample.Steering) || !Valid(sample.YawDelta)) return;
            if(State==MissionState.Countdown) {
                float consumed=Mathf.Min(seconds,Countdown);Countdown-=consumed;seconds-=consumed;
                if(Countdown>0)return;State=MissionState.Running;
                if(seconds<=0)return;
            }
            float remaining=Definition.TimeLimit-Elapsed;
            float step=Mathf.Min(seconds,remaining);Elapsed+=step;
            var objective=Objective;
            if(objective.kind==ObjectiveKind.RivalRace) { TickRace(sample,step);return; }
            if(objective.kind==ObjectiveKind.PoliceEscape) { TickPolice(sample,step);return; }
            if(objective.kind==ObjectiveKind.CanyonJump) { TickCanyon(sample,step);return; }
            switch(objective.kind) {
                case ObjectiveKind.ReachPoint:
                    if(sample.Grounded && Vector3.Distance(sample.Position,objective.point)<=objective.radius) Progress=objective.target;
                    break;
                case ObjectiveKind.PickupPackage:
                case ObjectiveKind.DeliverPackage:
                    bool hasRequiredPackage=objective.kind==ObjectiveKind.PickupPackage?!CarryingPackage:CarryingPackage;
                    Progress=hasRequiredPackage && sample.Grounded && sample.SpeedKmh<=objective.speedKmh && Vector3.Distance(sample.Position,objective.point)<=objective.radius?Progress+step:0;
                    break;
                case ObjectiveKind.PedalHold:
                    Progress=sample.Grounded && sample.Acceleration>.5f && sample.SpeedKmh>=objective.speedKmh?Progress+step:0;break;
                case ObjectiveKind.BrakeStop:
                    if(sample.Grounded && sample.SpeedKmh>=objective.speedKmh)BrakeArmed=true;
                    Progress=BrakeArmed && sample.Grounded && sample.Acceleration<-.5f && sample.SpeedKmh<=1?Progress+step:0;break;
                case ObjectiveKind.SteerTurn:
                    if(sample.Grounded && sample.SpeedKmh>=objective.speedKmh && sample.Steering*objective.direction>.5f)
                        Progress=Mathf.Max(0,Progress+sample.YawDelta*objective.direction);
                    break;
                case ObjectiveKind.Distance: Progress+=sample.SpeedKmh/3.6f*step;break;
                case ObjectiveKind.SpeedHold: Progress=sample.Grounded && objective.Contains(sample.Position) && sample.SpeedKmh>=objective.speedKmh?Progress+step:0;break;
                case ObjectiveKind.WheelieHold:
                    if(sample.Wheelie) {WheelieHoldBroken=false;Progress+=step;}
                    else {if(Progress>0)WheelieHoldBroken=true;Progress=0;}
                    break;
            }
            CheckObjective();
            if(State==MissionState.Running && Elapsed>=Definition.TimeLimit) Fail("Time ran out.");
        }
        // Rival accelerates to its tier speed without rubber banding.
        private static float RivalPosition(float time,float speed,float acceleration)
        {
            float ramp=speed/acceleration;
            return time<ramp?.5f*acceleration*time*time:speed*(time-ramp*.5f);
        }
        private void TickRace(MissionSample sample,float step)
        {
            var o=Objective;
            float distance=sample.Position.z-o.point.z;
            if(Mathf.Abs(sample.Position.x-o.point.x)>o.radius || Mathf.Abs(sample.Position.y-o.point.y)>5 || distance< -10) {
                Fail("You left the race course. Retry the sprint.");return;
            }
            float startTime=Elapsed-step,speed=o.speedKmh/3.6f;
            float acceleration=o.rivalAcceleration;
            float rivalFinish=o.target<=speed*speed/(2*acceleration)?Mathf.Sqrt(2*o.target/acceleration):o.target/speed+speed/(2*acceleration);
            RivalDistance=Mathf.Min(o.target,RivalPosition(Elapsed,speed,acceleration));
            Progress=Mathf.Clamp(distance,0,o.target);
            float playerFinish=float.PositiveInfinity;
            if(distance>=o.target && previousRaceDistance<o.target)
                playerFinish=startTime+step*Mathf.Clamp01((o.target-previousRaceDistance)/(distance-previousRaceDistance));
            previousRaceDistance=distance;
            if(playerFinish<=Elapsed && playerFinish<=rivalFinish+.0001f) {
                Elapsed=playerFinish;RivalDistance=Mathf.Min(o.target,RivalPosition(Elapsed,speed,acceleration));
                State=MissionState.Completed;Result=Definition.Id=="boss_race"?"1st place! You defeated the boss.":"1st place! You beat the rival.";Completed?.Invoke();return;
            }
            if(rivalFinish<=Elapsed) {Elapsed=rivalFinish;Fail(Definition.Id=="boss_race"?"2nd place. The boss reached the finish first.":"2nd place. The rival reached the finish first.");return;}
            if(Elapsed>=Definition.TimeLimit)Fail("Time ran out.");
        }
        private void TickCanyon(MissionSample sample,float step)
        {
            var o=Objective;
            if(sample.Position.y<1 || Mathf.Abs(sample.Position.x-o.point.x)>10 || sample.Position.z>326) {
                // The ground-level run-up is allowed until the jump is armed.
                if(JumpReady || JumpInFlight || sample.Position.z>=270) {Fail("Missed the landing. Retry the canyon jump.");return;}
            }
            if(!JumpInFlight) {
                if(sample.Grounded)JumpReady=Vector3.Distance(sample.Position,o.point)<=o.radius;
                else if(JumpReady) {JumpInFlight=true;JumpAirtime=step;}
            } else {
                if(!sample.Grounded && !jumpTouchedDown)JumpAirtime+=step;
                if(sample.Grounded) {
                    jumpTouchedDown=true;
                    if(JumpAirtime<.25f || Vector3.Distance(sample.Position,o.landingPoint)>o.landingRadius) {Fail("Land inside the GREEN zone after clearing the gap.");return;}
                }
                if(jumpTouchedDown) {
                    landingWait+=step;
                    Progress=sample.StableGrounded && sample.Grounded && Vector3.Distance(sample.Position,o.landingPoint)<=o.landingRadius?Progress+step:0;
                    if(Progress+.0001f>=o.target) {State=MissionState.Completed;Result="Canyon cleared! Clean landing.";Completed?.Invoke();return;}
                    if(landingWait>=2) {Fail("Landing was unstable. Retry the jump.");return;}
                }
            }
            if(Elapsed>=Definition.TimeLimit)Fail("Time ran out. Retry the canyon jump.");
        }
        private void TickPolice(MissionSample sample,float step)
        {
            var o=Objective;
            Vector3 offset=sample.Position-o.point;
            if(Mathf.Abs(offset.x)>10 || Mathf.Abs(offset.y)>5 || offset.z< -20 || offset.z>640) {
                Fail("You left the pursuit area. Stay on the highway.");return;
            }
            float start=Elapsed-step,consumed=0;
            // Fixed small integration steps also prevent large ticks skipping capture.
            while(consumed<step && State==MissionState.Running) {
                float dt=Mathf.Min(.02f,step-consumed);consumed+=dt;
                Vector3 player=Vector3.Lerp(previousPolicePlayer,sample.Position,consumed/step);player.y=o.point.y;
                policeSpeed=Mathf.MoveTowards(policeSpeed,o.speedKmh/3.6f,2*dt);
                Vector3 delta=player-PolicePosition;
                if(delta.sqrMagnitude>.0001f)PoliceFacing=delta.normalized;
                Vector3 next=Vector3.MoveTowards(PolicePosition,player,policeSpeed*dt);
                PoliceDistance+=Vector3.Distance(next,PolicePosition);PolicePosition=next;
                PoliceGap=Vector3.Distance(player,PolicePosition);
                CaptureProgress=PoliceGap<=3?CaptureProgress+dt:0;
                Progress=sample.Grounded && PoliceGap>=o.radius?Progress+dt:0;
                if(CaptureProgress+.0001f>=2) {Elapsed=start+consumed;Fail("Caught! Police stayed within 3 m for 2 seconds.");}
                else if(Progress+.0001f>=o.target) {Elapsed=start+consumed;State=MissionState.Completed;Result="Escaped! You lost the police pursuit.";Completed?.Invoke();}
            }
            previousPolicePlayer=sample.Position;
            if(State==MissionState.Running && Elapsed>=Definition.TimeLimit)Fail("Time ran out. Police are still pursuing you.");
        }
        public void LandStunt(StuntResult result)
        {
            if(State!=MissionState.Running || !Valid(result.Duration) || !Valid(result.Distance) || result.Duration<0 || result.Distance<0 || result.FrontFlips<0 || result.BackFlips<0)return;
            float value=0;
            switch(Objective.kind) {
                case ObjectiveKind.LandedWheelieDistance:if(result.Kind==StuntKind.Wheelie)value=result.Distance;break;
                case ObjectiveKind.LandedWheelieHold:if(result.Kind==StuntKind.Wheelie)value=result.Duration;break;
                case ObjectiveKind.LandedStoppieHold:if(result.Kind==StuntKind.Stoppie)value=result.Duration;break;
                case ObjectiveKind.LandedFlips:if(result.Kind==StuntKind.Jump)value=Mathf.Max(result.FrontFlips,result.BackFlips);break;
                case ObjectiveKind.LandedBackflips:if(result.Kind==StuntKind.Jump)value=result.BackFlips;break;
                case ObjectiveKind.LandedAirtime:if(result.Kind==StuntKind.Jump)value=result.Duration;break;
                case ObjectiveKind.LandedJumpCount:if(result.Kind==StuntKind.Jump && result.Duration>=.25f)value=Progress+1;break;
                default:return;
            }
            Progress=Mathf.Max(Progress,value);CheckObjective();
        }
        public void BankCombo(int points,int count)
        {
            if(State!=MissionState.Running || Objective.kind!=ObjectiveKind.LandedCombo || points<=0 || count<=0)return;
            Progress=Mathf.Max(Progress,count);CheckObjective();
        }
        public void BankScore(int points)
        {
            if(State!=MissionState.Running || Objective.kind!=ObjectiveKind.BankedScore || points<=0)return;
            Progress+=points;CheckObjective();
        }
        private void CheckObjective()
        {
            if(Progress+.0001f<Objective.target)return;
            if(Objective.kind==ObjectiveKind.PickupPackage)CarryingPackage=true;
            if(Objective.kind==ObjectiveKind.DeliverPackage)CarryingPackage=false;
            ObjectiveIndex++;Progress=0;BrakeArmed=false;
            if(ObjectiveIndex<Definition.Objectives.Length)return;
            State=MissionState.Completed;Result="Mission complete.";Completed?.Invoke();
        }
        public void Fail(string reason) {if(!Active)return;State=MissionState.Failed;CarryingPackage=false;Result=reason;}
        public void Abandon() {if(!Active)return;State=MissionState.Abandoned;CarryingPackage=false;Result="Mission abandoned.";}
        public void Clear() { if(Active)return;State=MissionState.Idle;CarryingPackage=false;Definition=null;Result=""; }
        private static bool Valid(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    public static class MissionCatalog
    {
        public const int TutorialIndex=3;
        public const int DeliveryIndex=4;
        public const int SpeedTrialIndex=5;
        public const int RivalSprintIndex=6;
        public const int FirstWheelieIndex=7;
        public const int CheckpointRunIndex=8;
        public const int PoliceEscapeIndex=9;
        public const int MountainAscentIndex=10;
        public const int CanyonJumpIndex=11;
        public const int BossRaceIndex=12;
        public const int Count=13;
        public static MissionDefinition Create(int index,int level)
        {
            if(index<0 || index>=Count) return null;
            level=Mathf.Clamp(level,1,100);
            if(index==BossRaceIndex) return new MissionDefinition {Id="boss_race",Title="Boss Race",Level=level,Destination=3,Reward=300+50*(level-1),TimeLimit=110,
                Objectives=new[]{new MissionObjective {kind=ObjectiveKind.RivalRace,instruction="Beat the BOSS · PEDAL + SPRINT",point=new Vector3(850,0,-240),radius=10,target=Mathf.Min(640,500+10*(level-1)),speedKmh=Mathf.Min(30,24+.2f*(level-1)),rivalAcceleration=3}}};
            if(index==CanyonJumpIndex) return new MissionDefinition {Id="canyon_jump",Title="Canyon Jump",Level=level,Destination=4,Reward=225+25*(level-1),TimeLimit=Mathf.Max(45,90-2*(level-1)),
                Objectives=new[]{new MissionObjective {kind=ObjectiveKind.CanyonJump,instruction="PEDAL + SPRINT; charge JUMP, release at GOLD",point=new Vector3(1000,3,279),radius=3,landingPoint=new Vector3(1000,3,289),landingRadius=Mathf.Max(2,3-.05f*(level-1)),target=Mathf.Min(.8f,.3f+.02f*(level-1))}}};
            if(index==MountainAscentIndex) return MountainAscent(level);
            if(index==PoliceEscapeIndex) return new MissionDefinition {Id="police_escape",Title="Police Escape",Level=level,Destination=3,Reward=200+25*(level-1),TimeLimit=120,
                Objectives=new[]{new MissionObjective {kind=ObjectiveKind.PoliceEscape,instruction="PEDAL + SPRINT to lose police",point=new Vector3(850,0,-240),speedKmh=Mathf.Min(22,14+.4f*(level-1)),radius=Mathf.Min(65,35+2*(level-1)),target=Mathf.Min(12,5+.5f*(level-1))}}};
            if(index==CheckpointRunIndex) return CheckpointRun(level);
            if(index==FirstWheelieIndex) {
                float hold=10+25*(level-1);
                return new MissionDefinition {Id="first_wheelie",Title="First Wheelie",Level=level,Destination=3,Reward=150+25*(level-1),TimeLimit=hold+90,
                    Objectives=new[]{new MissionObjective {kind=ObjectiveKind.WheelieHold,instruction="Hold PEDAL + WHEELIE",target=hold}}};
            }
            if(index==TutorialIndex) return Tutorial(level);
            if(index==DeliveryIndex) return Delivery(level);
            if(index==SpeedTrialIndex) return SpeedTrial(level);
            if(index==RivalSprintIndex) return new MissionDefinition {
                Id="rival_sprint",Title="Rival Sprint",Level=level,Destination=3,Reward=175+25*(level-1),TimeLimit=140,
                Objectives=new[]{new MissionObjective {kind=ObjectiveKind.RivalRace,instruction="Beat the rival · PEDAL + SPRINT",point=new Vector3(850,0,-240),radius=10,target=Mathf.Min(600,300+15*(level-1)),speedKmh=Mathf.Min(28.8f,18+.4f*(level-1))}}
            };
            var definition=new MissionDefinition {Id=new[]{"yard_distance","highway_checkpoints","wheelie_training"}[index],Title=new[]{"Yard Explorer","Highway Checkpoint Run","Wheelie Practice"}[index],Level=level,Reward=100+25*(level-1)};
            if(index==0) {
                definition.Destination=1;definition.TimeLimit=90+10*(level-1);
                definition.Objectives=new[]{new MissionObjective {kind=ObjectiveKind.Distance,instruction="Ride around the yard",target=200+100*(level-1)}};
            } else if(index==1) {
                definition.Destination=3;definition.TimeLimit=Mathf.Max(65,120-3*(level-1));
                definition.Objectives=new[]{
                    new MissionObjective {kind=ObjectiveKind.ReachPoint,instruction="Pass checkpoint 1",point=new Vector3(850,0,-100)},
                    new MissionObjective {kind=ObjectiveKind.ReachPoint,instruction="Pass checkpoint 2",point=new Vector3(850,0,80)},
                    new MissionObjective {kind=ObjectiveKind.ReachPoint,instruction="Reach the finish",point=new Vector3(850,0,220)}};
            } else {
                definition.Destination=3;float hold=10+25*(level-1);definition.TimeLimit=hold+90;
                definition.Objectives=new[]{new MissionObjective {kind=ObjectiveKind.WheelieHold,instruction="Hold one continuous wheelie",target=hold}};
            }
            return definition;
        }
        private static MissionDefinition MountainAscent(int level)
        {
            var points=new[]{new Vector3(730,5,120),new Vector3(730,10,160),new Vector3(755,12,170),
                new Vector3(780,14,180),new Vector3(755,19,215),new Vector3(730,24,250),new Vector3(Mathf.Min(785,775+level-1),24,250)};
            var objectives=new MissionObjective[points.Length];
            for(int i=0;i<points.Length;i++)objectives[i]=new MissionObjective {kind=ObjectiveKind.ReachPoint,
                instruction=i==points.Length-1?"Reach the GREEN summit finish":"Climb through the GOLD checkpoint",
                point=points[i],radius=Mathf.Max(2.5f,4.5f-.1f*(level-1))};
            return new MissionDefinition {Id="mountain_ascent",Title="Mountain Ascent",Level=level,Destination=2,
                Reward=200+25*(level-1),TimeLimit=Mathf.Max(75,150-3*(level-1)),Objectives=objectives};
        }
        private static MissionDefinition CheckpointRun(int level)
        {
            int count=Mathf.Min(10,5+(level-1)/2);
            float finish=Mathf.Min(360,160+20*(level-1)),radius=Mathf.Max(3,6-.15f*(level-1));
            var objectives=new MissionObjective[count];
            for(int i=0;i<count;i++) {
                bool last=i==count-1;
                objectives[i]=new MissionObjective {kind=ObjectiveKind.ReachPoint,
                    instruction=last?"Pass the GREEN finish":"Pass the GOLD checkpoint",
                    point=new Vector3(last?850:i%2==0?847:853,0,Mathf.Lerp(-160,finish,(float)i/(count-1))),radius=radius};
            }
            return new MissionDefinition {Id="checkpoint_run",Title="Checkpoint Run",Level=level,Destination=3,
                Reward=175+25*(level-1),TimeLimit=Mathf.Max(95,120-2*(level-1)),Objectives=objectives};
        }
        private static MissionDefinition SpeedTrial(int level)
        {
            float speed=Mathf.Min(28,18+(level-1)*.5f),hold=Mathf.Min(35,10+(level-1)*2);
            return new MissionDefinition {
                Id="speed_trial",Title="Speed Trial",Level=level,Destination=3,Reward=150+25*(level-1),TimeLimit=Mathf.Max(65,120-(level-1)*2),
                Objectives=new[]{
                    new MissionObjective {kind=ObjectiveKind.ReachPoint,instruction="Ride to the speed-trial start",point=new Vector3(850,0,-180),radius=6},
                    new MissionObjective {kind=ObjectiveKind.SpeedHold,instruction=$"Hold PEDAL + SPRINT above {speed:0.#} KM/H",speedKmh=speed,target=hold,useZone=true,point=new Vector3(850,0,110),zoneSize=new Vector3(20,8,580)}
                }};
        }
        private static MissionDefinition Delivery(int level)
        {
            float distance=140+Mathf.Min(360,(level-1)*20),radius=Mathf.Max(3,6-(level-1)*.15f),hold=Mathf.Min(4,2+(level-1)*.1f);
            return new MissionDefinition {
                Id="pizza_delivery",Title="Pizza Delivery",Level=level,Destination=3,Reward=150+25*(level-1),TimeLimit=Mathf.Max(100,180-3*(level-1)),
                Objectives=new[]{
                    new MissionObjective {kind=ObjectiveKind.PickupPackage,instruction="Stop in the BLUE pickup zone",point=new Vector3(850,0,-220),radius=radius,speedKmh=2,target=hold},
                    new MissionObjective {kind=ObjectiveKind.DeliverPackage,instruction="Stop in the GREEN delivery zone",point=new Vector3(850,0,-220+distance),radius=radius,speedKmh=2,target=hold}
                }};
        }
        private static MissionDefinition Tutorial(int level)
        {
            float speed=8+Mathf.Min(6,level-1),hold=2+Mathf.Min(4,(level-1)*.5f),turn=20+Mathf.Min(15,(level-1)*5);
            return new MissionDefinition {
                Id="riding_basics",Title="Riding Basics",Level=level,Destination=3,Reward=100+25*(level-1),TimeLimit=Mathf.Max(90,180-5*(level-1)),
                Objectives=new[]{
                    new MissionObjective {kind=ObjectiveKind.PedalHold,instruction=$"Hold PEDAL above {speed:0} KM/H",speedKmh=speed,target=hold},
                    new MissionObjective {kind=ObjectiveKind.BrakeStop,instruction="Reach 6 KM/H, then hold BRAKE to stop",speedKmh=6,target=1},
                    new MissionObjective {kind=ObjectiveKind.SteerTurn,instruction="PEDAL gently + LEFT to turn left",speedKmh=4,direction=-1,target=turn},
                    new MissionObjective {kind=ObjectiveKind.SteerTurn,instruction="PEDAL gently + RIGHT to turn right",speedKmh=4,direction=1,target=turn},
                    new MissionObjective {kind=ObjectiveKind.BrakeStop,instruction="Reach 6 KM/H, then BRAKE to finish",speedKmh=6,target=1+Mathf.Min(1,(level-1)*.25f)}
                }};
        }
    }
}

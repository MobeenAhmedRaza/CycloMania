using UnityEngine;
namespace Cyclomania
{
    public static class FreestyleCatalog
    {
        public const int Count=15;
        public static int UnlockPoints(int zone)=>zone*300;
        public static bool Unlocked(int zone,int points)=>zone>=0 && zone<5 && points>=UnlockPoints(zone);
        public static bool Contains(int zone,Vector3 p)
        {
            switch(zone) {
                case 0:return p.x>=-400 && p.x<=380 && p.z>=-600 && p.z<=400;
                case 1:return p.x>=570 && p.x<=670 && p.z>=-200 && p.z<=-20;
                case 2:return p.x>=690 && p.x<=815 && p.z>=70 && p.z<=420;
                case 3:return p.x>=838 && p.x<=862 && p.z>=-340 && p.z<=420;
                case 4:return p.x>=910 && p.x<=1090 && p.z>=-30 && p.z<=230;
                default:return false;
            }
        }
        public static MissionDefinition Create(int index,int level)
        {
            if(index<0 || index>=Count)return null;level=Mathf.Clamp(level,1,100);
            var d=new MissionDefinition {Id="freestyle_"+index,Level=level,Destination=index/3,Reward=150+25*(level-1),TimeLimit=180};
            float extra=level-1;MissionObjective goal;
            switch(index) {
                case 0:d.Title="City Wheelie Distance";goal=Goal(ObjectiveKind.LandedWheelieDistance,"Land one wheelie of",Mathf.Min(250,100+10*extra));break;
                case 1:d.Title="City Combo";goal=Goal(ObjectiveKind.LandedCombo,"Bank one combo of",Mathf.Min(6,3+Mathf.Floor(extra/5)));break;
                case 2:d.Title="City Score Session";goal=Goal(ObjectiveKind.BankedScore,"Bank stunt points",Mathf.Min(6000,1000+200*extra));break;
                case 3:d.Title="Industrial Triple Flip";goal=Goal(ObjectiveKind.LandedFlips,"Land flips in ONE jump",3);d.TimeLimit=Mathf.Max(120,240-2*extra);break;
                case 4:d.Title="Industrial Balance Line";
                    d.Objectives=new[]{Point(new Vector3(650,.7f,-145),"Enter balance line",1.5f),Point(new Vector3(650,.7f,-130),"Cross balance line",1.5f)};d.TimeLimit=Mathf.Max(45,90-extra);return d;
                case 5:d.Title="Industrial Combo Chain";goal=Goal(ObjectiveKind.LandedCombo,"Bank one combo of",Mathf.Min(8,5+Mathf.Floor(extra/5)));break;
                case 6:d.Title="Mountain Wheelie";goal=Goal(ObjectiveKind.LandedWheelieHold,"Land one wheelie lasting",Mathf.Min(45,15+extra));break;
                case 7:d.Title="Summit Bridge Crossing";
                    d.Objectives=new[]{Point(new Vector3(730,24,250),"Reach summit bridge",4),Point(new Vector3(780,24,250),"Cross summit bridge",Mathf.Max(2.5f,4-.1f*extra))};d.TimeLimit=Mathf.Max(100,200-2*extra);return d;
                case 8:d=MissionCatalog.Create(MissionCatalog.MountainAscentIndex,level);d.Title="Mountain Climb Route";d.Reward=150+25*(level-1);return d;
                case 9:d.Title="Highway Stoppie";goal=Goal(ObjectiveKind.LandedStoppieHold,"Land one stoppie lasting",Mathf.Min(1.5f,.5f+.05f*extra));break;
                case 10:d.Title="Highway Speed Hold";goal=Goal(ObjectiveKind.SpeedHold,"Hold speed continuously",Mathf.Min(25,15+extra));goal.speedKmh=Mathf.Min(65,45+.5f*extra);break;
                case 11:d.Title="Highway Combo";goal=Goal(ObjectiveKind.LandedCombo,"Bank one combo of",Mathf.Min(8,4+Mathf.Floor(extra/5)));break;
                case 12:d.Title="Desert Airtime";goal=Goal(ObjectiveKind.LandedAirtime,"Land one jump with airtime",Mathf.Min(2,1+.025f*extra));break;
                case 13:d.Title="Desert Double Backflip";goal=Goal(ObjectiveKind.LandedBackflips,"Land backflips in ONE jump",2);d.TimeLimit=Mathf.Max(120,240-2*extra);break;
                default:d.Title="Five Dune Jumps";goal=Goal(ObjectiveKind.LandedJumpCount,"Land clean jumps",Mathf.Min(10,5+Mathf.Floor(extra/5)));break;
            }
            d.Objectives=new[]{goal};return d;
        }
        private static MissionObjective Goal(ObjectiveKind kind,string instruction,float target)=>new MissionObjective {kind=kind,instruction=instruction,target=target};
        private static MissionObjective Point(Vector3 point,string instruction,float radius)=>new MissionObjective {kind=ObjectiveKind.ReachPoint,point=point,instruction=instruction,radius=radius};
        public static string Summary(MissionDefinition d)
        {
            var o=d.Objectives[0];
            if(o.kind==ObjectiveKind.ReachPoint)return $"{d.Objectives.Length} ordered route targets";
            if(o.kind==ObjectiveKind.SpeedHold)return $"{o.speedKmh:0.#} KM/H for {o.target:0}s continuously";
            return $"{o.instruction} {o.target:0.#}"+(o.kind==ObjectiveKind.LandedWheelieDistance?" m":o.kind==ObjectiveKind.LandedWheelieHold || o.kind==ObjectiveKind.LandedStoppieHold || o.kind==ObjectiveKind.LandedAirtime?" s":o.kind==ObjectiveKind.LandedCombo?" stunts":"");
        }
    }
}

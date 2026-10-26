using UnityEngine;
namespace Cyclomania
{
    public sealed partial class GameplayHud
    {
        private int freestyleZone;
        public void ShowFreestyle()
        {
            if(!initialized || Missions==null || changingBicycle)return;
            if(!IsPaused)Pause();
            NewPage("Freestyle","FREESTYLE CHALLENGES");
            Label(page,"Points",$"FREESTYLE POINTS  {Missions.Freestyle.points} · Career points are separate",23,new Vector2(0,145),new Vector2(1000,35));
            if(Missions.Attempt.Active) {
                Label(page,"Active",Session.Objective+"\nFinish or abandon the current attempt before starting another.",26,new Vector2(0,25),new Vector2(980,130));
                ActionButton(page,"Continue","CONTINUE",new Vector2(-180,-120),Resume);
                ActionButton(page,"Abandon","ABANDON",new Vector2(180,-120),()=>{Missions.Abandon();ShowMissionResult();});
            } else {
                bool unlocked=FreestyleCatalog.Unlocked(freestyleZone,Missions.Freestyle.points);
                Label(page,"Zone",RideWorld.Destinations[freestyleZone].Name.ToUpperInvariant()+$" · ZONE {freestyleZone+1}/5",25,new Vector2(0,100),new Vector2(900,40));
                if(unlocked) {
                    for(int row=0;row<3;row++) {
                        int index=freestyleZone*3+row;var d=FreestyleCatalog.Create(index,Missions.Freestyle.NextLevel(index));
                        int reward=Missions.Freestyle.completed[index]>=100?0:d.Reward;float y=40-row*80;
                        Label(page,"Challenge"+index,$"{d.Title} · LEVEL {d.Level}\n{FreestyleCatalog.Summary(d)} | {d.TimeLimit:0}s | {reward} FP",21,new Vector2(-140,y),new Vector2(720,75));
                        ActionButton(page,"ChallengeStart"+index,"START",new Vector2(380,y),()=>StartFreestyle(index),new Vector2(200,60));
                    }
                    ActionButton(page,"Explore","EXPLORE",new Vector2(340,-240),()=>{if(Missions.ExploreFreestyle(freestyleZone)){Refresh();Resume();}},new Vector2(230,55));
                } else Label(page,"Locked",$"Unlock this challenge zone at {FreestyleCatalog.UnlockPoints(freestyleZone)} Freestyle Points.\nComplete challenges in unlocked zones to earn points.",27,new Vector2(0,0),new Vector2(980,130));
                ActionButton(page,"PreviousZone","<",new Vector2(-130,-240),()=>{freestyleZone--;ShowFreestyle();},new Vector2(90,55)).interactable=freestyleZone>0;
                ActionButton(page,"NextZone",">",new Vector2(130,-240),()=>{freestyleZone++;ShowFreestyle();},new Vector2(90,55)).interactable=freestyleZone<4;
                Label(page,"Hint","Land and bank stunts. Crash, reset, travel or leaving the zone ends a challenge.",17,new Vector2(0,-185),new Vector2(1000,40));
            }
            ActionButton(page,"Back","BACK",new Vector2(-370,-240),ShowPausePage,new Vector2(200,55));
        }
        private void StartFreestyle(int index) {if(Missions.BeginFreestyle(index)){Refresh();Resume();}}
        private void ShowFreestyleResult()
        {
            Missions.AcknowledgeResults();if(!IsPaused)Pause();
            var a=Missions.Attempt;bool won=a.State==MissionState.Completed;
            NewPage("Freestyle Result",won?"CHALLENGE COMPLETE":"CHALLENGE ENDED");
            Label(page,"Challenge",a.Definition.Title+$" · LEVEL {a.Definition.Level}",30,new Vector2(0,120),new Vector2(950,60));
            Label(page,"Outcome",(won?"Challenge complete.":a.Result)+$"\nTime: {a.Elapsed:0.0}s | Freestyle Points: {Missions.Freestyle.points}",25,new Vector2(0,30),new Vector2(950,100));
            Label(page,"Save",Missions.SaveNotice,21,new Vector2(0,-45),new Vector2(950,40));
            ActionButton(page,"Retry",won?(Missions.Freestyle.completed[Missions.Selected]>=100?"REPLAY":"NEXT LEVEL"):"RETRY",new Vector2(-310,-135),()=>StartFreestyle(Missions.Selected));
            ActionButton(page,"Challenges","CHALLENGES",new Vector2(0,-135),()=>{freestyleZone=Missions.Selected/3;ShowFreestyle();});
            ActionButton(page,"FreeRide","FREE RIDE",new Vector2(310,-135),()=>{Missions.Dismiss();Resume();});
        }
    }
}

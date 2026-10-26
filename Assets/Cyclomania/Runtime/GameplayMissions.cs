using UnityEngine;
namespace Cyclomania
{
    public sealed partial class GameplayHud
    {
        private int missionPage;
        public void ShowMissions() { missionPage=0;DrawMissionPage(); }
        private void DrawMissionPage()
        {
            if(!initialized || Missions==null || changingBicycle)return;
            if(!IsPaused)Pause();
            NewPage("Missions","CAREER MISSIONS");
            Label(page,"Career",$"CAREER POINTS  {Missions.Career.points}",23,new Vector2(0,145),new Vector2(900,35));
            if(Missions.Attempt.Active) {
                Label(page,"Active",Session.Objective+"\n"+Session.ObjectiveDetail,28,new Vector2(0,30),new Vector2(980,150));
                ActionButton(page,"Resume","CONTINUE",new Vector2(-180,-140),Resume);
                ActionButton(page,"Abandon","ABANDON",new Vector2(180,-140),()=>{Missions.Abandon();ShowMissionResult();});
            } else {
                int[] order={MissionCatalog.TutorialIndex,MissionCatalog.DeliveryIndex,MissionCatalog.SpeedTrialIndex,0,1,2,MissionCatalog.RivalSprintIndex,MissionCatalog.FirstWheelieIndex,MissionCatalog.CheckpointRunIndex,MissionCatalog.PoliceEscapeIndex,MissionCatalog.MountainAscentIndex,MissionCatalog.CanyonJumpIndex,MissionCatalog.BossRaceIndex};
                for(int row=0;row<3 && missionPage*3+row<order.Length;row++) {
                    int i=order[missionPage*3+row];
                    int index=i;var d=MissionCatalog.Create(i,Missions.Career.NextLevel(i));if(Missions.Career.completed[i]>=100)d.Reward=0;float y=65-row*90;
                    string goal=i==MissionCatalog.BossRaceIndex?$"Beat the {d.Objectives[0].speedKmh:0.#} KM/H boss over {d.Objectives[0].target:0} m":i==MissionCatalog.CanyonJumpIndex?"Clear the 6 m gap and land in the green zone":i==MissionCatalog.MountainAscentIndex?"Climb 7 checkpoints to the 24 m summit":i==MissionCatalog.PoliceEscapeIndex?$"Keep a {d.Objectives[0].radius:0} m lead for {d.Objectives[0].target:0.#}s":i==MissionCatalog.CheckpointRunIndex?$"Pass {d.Objectives.Length} checkpoints in order":i==MissionCatalog.FirstWheelieIndex?$"Hold one continuous wheelie for {d.Objectives[0].target:0}s":i==MissionCatalog.RivalSprintIndex?$"Beat the AI cyclist over {d.Objectives[0].target:0} m":i==MissionCatalog.SpeedTrialIndex?$"Hold {d.Objectives[1].speedKmh:0.#} KM/H for {d.Objectives[1].target:0}s in the marked zone":i==MissionCatalog.DeliveryIndex?$"Collect pizza · Deliver {Vector3.Distance(d.Objectives[0].point,d.Objectives[1].point):0} m away":i==MissionCatalog.TutorialIndex?"Pedal · Brake · Left · Right · Stop":d.Objectives[0].kind==ObjectiveKind.ReachPoint?"3 ordered checkpoints":$"{d.Objectives[0].target:0} "+(d.Objectives[0].kind==ObjectiveKind.Distance?"meters":"seconds continuously");
                    Label(page,"Mission"+i,$"{d.Title} · LEVEL {d.Level}\n{goal}  |  {d.TimeLimit:0}s limit  |  {d.Reward} career points",23,new Vector2(-150,y),new Vector2(700,80));
                    ActionButton(page,"Start"+i,"START",new Vector2(370,y),()=>StartMission(index),new Vector2(230,65));
                }
            }
            if(!Missions.Attempt.Active) {
                ActionButton(page,"PreviousPage","<",new Vector2(-130,-240),()=>{missionPage--;DrawMissionPage();},new Vector2(90,55)).interactable=missionPage>0;
                Label(page,"Page",$"{missionPage+1} / {(MissionCatalog.Count+2)/3}",22,new Vector2(0,-240),new Vector2(140,40));
                ActionButton(page,"NextPage",">",new Vector2(130,-240),()=>{missionPage++;DrawMissionPage();},new Vector2(90,55)).interactable=missionPage<(MissionCatalog.Count-1)/3;
            }
            ActionButton(page,"Back","BACK",new Vector2(-370,-240),ShowPausePage,new Vector2(220,55));
            Label(page,"Hint","Crash, reset, travel or changing bicycles ends an attempt.",18,new Vector2(0,-185),new Vector2(1000,40));
        }
        private void StartMission(int index)
        { if(Missions.Begin(index)) {Refresh();Resume();} }
        public void ShowMissionResult()
        {
            if(Missions==null || Missions.Attempt.Active || Missions.Attempt.State==MissionState.Idle)return;
            if(Missions.IsFreestyle) {ShowFreestyleResult();return;}
            Missions.AcknowledgeResults();
            if(!IsPaused)Pause();
            var attempt=Missions.Attempt;bool won=attempt.State==MissionState.Completed;
            NewPage("Mission Result",won?"MISSION COMPLETE":"MISSION ENDED");
            Label(page,"Mission",attempt.Definition.Title+$" · LEVEL {attempt.Definition.Level}",30,new Vector2(0,120),new Vector2(950,60));
            Label(page,"Outcome",attempt.Result+$"\nTime: {attempt.Elapsed:0.0}s   |   Career points: {Missions.Career.points}",25,new Vector2(0,30),new Vector2(950,100));
            Label(page,"Save",Missions.SaveNotice,21,new Vector2(0,-45),new Vector2(950,40));
            ActionButton(page,"Retry",won?(Missions.Career.completed[Missions.Selected]>=100?"REPLAY":"NEXT LEVEL"):"RETRY",new Vector2(-310,-135),()=>StartMission(Missions.Selected));
            ActionButton(page,"Missions","MISSIONS",new Vector2(0,-135),ShowMissions);
            ActionButton(page,"FreeRide","FREE RIDE",new Vector2(310,-135),()=>{Missions.Dismiss();Resume();});
        }
    }
}

using System;
using SBPScripts;
using UnityEngine;
namespace Cyclomania
{
    public sealed partial class GameplayHud
    {
        [NonSerialized] public string AdvancedSaveKey="Cyclomania.Advanced.v1";
        public AdvancedProgress Advanced {get;private set;}=new AdvancedProgress();
        public SchoolLesson School {get;}=new SchoolLesson();
        public RideEnvironment Environment {get;private set;}
        public RideReplay Replay {get;private set;}
        private string advancedNotice="";private GameObject schoolCourse;private float schoolElapsed;
        private static readonly Vector3 SchoolOrigin=new Vector3(1500,5,0);
        private Vector3 schoolReturn;private float schoolReturnHeading;
        private CycleRecovery advancedRecovery;
        private void SchoolInterrupted(){if(School.Active)School.End("School interrupted by crash/reset. Retry from Advanced.");}
        private bool schoolVisit;private UnityEngine.UI.Text schoolStatus,recordingStatus;
        private void InitializeAdvanced()
        {
            Advanced=AdvancedProgress.FromJson(string.IsNullOrEmpty(AdvancedSaveKey)?"":PlayerPrefs.GetString(AdvancedSaveKey,""));
            Environment=gameObject.AddComponent<RideEnvironment>();Environment.Initialize();Environment.Configure(Advanced.weather,Advanced.timePreset,Advanced.cycleTime);
            Replay=gameObject.AddComponent<RideReplay>();ApplyAdvanced();
            var cameraButton=ActionButton(hud,"View","VIEW",new Vector2(-100,-155),ToggleView,new Vector2(150,60),new Vector2(1,1));
            schoolStatus=Label(hud,"School status","",21,new Vector2(0,125),new Vector2(800,70));schoolStatus.rectTransform.anchorMin=schoolStatus.rectTransform.anchorMax=new Vector2(.5f,0);
            recordingStatus=Label(hud,"Recording","",18,new Vector2(-300,-125),new Vector2(230,35));recordingStatus.rectTransform.anchorMin=recordingStatus.rectTransform.anchorMax=new Vector2(1,1);
        }
        private int CareerTiers(){int result=0;if(Missions!=null)foreach(int tier in Missions.Career.completed)result+=tier;return result;}
        private bool SaveAdvanced(AdvancedProgress next)
        {
            try{if(!string.IsNullOrEmpty(AdvancedSaveKey)){PlayerPrefs.SetString(AdvancedSaveKey,next.ToJson());PlayerPrefs.Save();}Advanced=next;advancedNotice="";ApplyAdvanced();return true;}
            catch(Exception e){advancedNotice="Could not save advanced settings. Try again.";Debug.LogWarning(e.Message);return false;}
        }
        private void ChangeAdvanced(Action<AdvancedProgress> change){var next=AdvancedProgress.FromJson(Advanced.ToJson());change(next);SaveAdvanced(next);}
        private void ToggleView(){ChangeAdvanced(p=>p.firstPerson=!p.firstPerson);}
        private void ApplyAdvanced()
        {
            if(bicycle==null)return;
            bicycle.balanceAssist=8+Advanced.skills[0]*2;
            bicycle.airFlipSpeed=7.5f*(1+.05f*Garage.handling[Garage.selected])*(1+.1f*Advanced.skills[2]);
            var r=bicycle.GetComponent<CycleRecovery>();if(r!=null)r.recoveryDelay=2-.35f*Advanced.skills[1];
            if(advancedRecovery!=r){if(advancedRecovery!=null){advancedRecovery.Crashed-=SchoolInterrupted;advancedRecovery.Respawned-=SchoolInterrupted;}advancedRecovery=r;if(r!=null){r.Crashed+=SchoolInterrupted;r.Respawned+=SchoolInterrupted;}SchoolInterrupted();}
            if(Environment!=null)Environment.Target=bicycle.transform;
            foreach(var camera in FindObjectsByType<BicycleCamera>(FindObjectsSortMode.None))camera.SetFirstPerson(Advanced.firstPerson);
        }
        private void TickAdvanced()
        {
            if(Replay==null)return;
            recordingStatus.text=Replay.Recording?"● RECORDING":"";
            if(CurrentPage=="Advanced") {
                var label=page.Find("Notice")?.GetComponent<UnityEngine.UI.Text>();if(label!=null)label.text=string.IsNullOrEmpty(advancedNotice)?Replay.Notice:advancedNotice;
                var start=page.Find("Record")?.GetComponent<UnityEngine.UI.Button>();if(start!=null)start.interactable=Replay.Supported && !Replay.Recording && !Replay.Available;
                var stop=page.Find("Stop")?.GetComponent<UnityEngine.UI.Button>();if(stop!=null)stop.interactable=Replay.Recording;
                var preview=page.Find("Preview")?.GetComponent<UnityEngine.UI.Button>();if(preview!=null)preview.interactable=Replay.Available && !Replay.Recording;
                var discard=page.Find("Discard")?.GetComponent<UnityEngine.UI.Button>();if(discard!=null)discard.interactable=Replay.Available && !Replay.Recording;
            }
            if(School.Active && !IsPaused){
                schoolElapsed+=Time.deltaTime;
                School.Tick(bicycle.transform.position-SchoolOrigin,bicycle.rb.linearVelocity.magnitude*3.6f,Time.deltaTime,schoolElapsed,recovery!=null && recovery.IsCrashed,false);
                if(Missions!=null && Missions.Attempt.Active)School.End("School ended: mission started.");
                if(School.Passed && Advanced.schoolCompleted==School.Lesson)ChangeAdvanced(p=>p.schoolCompleted++);
            }
            schoolStatus.text=schoolVisit?School.Notice+(School.Active && School.Lesson==2?(schoolElapsed<10?" RED":" GREEN"):""):"";
        }
        private void ShowAdvanced()
        {
            if(!initialized || Replay==null)return;if(!IsPaused)Pause();NewPage("Advanced","ADVANCED FEATURES");
            ActionButton(page,"View",Advanced.firstPerson?"FIRST PERSON":"THIRD PERSON",new Vector2(-350,135),()=>{ToggleView();ShowAdvanced();},new Vector2(310,55));
            ActionButton(page,"Weather",new[]{"CLEAR","RAIN","FOG","SNOW"}[Advanced.weather],new Vector2(0,135),()=>{ChangeAdvanced(p=>p.weather=(p.weather+1)%4);Environment.Configure(Advanced.weather,Advanced.timePreset,Advanced.cycleTime);ShowAdvanced();},new Vector2(310,55));
            ActionButton(page,"Time",new[]{"NOON","DUSK","MIDNIGHT","DAWN"}[Advanced.timePreset],new Vector2(350,135),()=>{ChangeAdvanced(p=>p.timePreset=(p.timePreset+1)%4);Environment.Configure(Advanced.weather,Advanced.timePreset,Advanced.cycleTime);ShowAdvanced();},new Vector2(310,55));
            ActionButton(page,"Cycle",Advanced.cycleTime?"DAY CYCLE ON":"DAY CYCLE OFF",new Vector2(-350,65),()=>{ChangeAdvanced(p=>p.cycleTime=!p.cycleTime);Environment.Cycle=Advanced.cycleTime;ShowAdvanced();},new Vector2(310,55));
            ActionButton(page,"School",schoolVisit?"LEAVE SCHOOL":"CYCLING SCHOOL",new Vector2(0,65),()=>{if(schoolVisit)LeaveSchool();else ShowSchool();},new Vector2(310,55));
            ActionButton(page,"Skills",$"SKILLS · {Advanced.Available(CareerTiers())} POINTS",new Vector2(350,65),ShowSkills,new Vector2(310,55));
            Label(page,"Replay title","RIDE VIDEO · iOS PREVIEW & SHARE",23,new Vector2(0,-5),new Vector2(1000,35));
            ActionButton(page,"Record","RECORD",new Vector2(-390,-65),()=>{Replay.StartRecording();},new Vector2(240,55)).interactable=Replay.Supported && !Replay.Recording && !Replay.Available;
            ActionButton(page,"Stop","STOP",new Vector2(-130,-65),Replay.StopRecording,new Vector2(240,55)).interactable=Replay.Recording;
            ActionButton(page,"Preview","PREVIEW / SHARE",new Vector2(130,-65),Replay.Preview,new Vector2(240,55)).interactable=Replay.Available && !Replay.Recording;
            ActionButton(page,"Discard","DISCARD",new Vector2(390,-65),Replay.Discard,new Vector2(240,55)).interactable=Replay.Available && !Replay.Recording;
            Label(page,"Notice",Replay.Supported?Replay.Notice:"Video recording and sharing require a supported iPhone or iPad.",19,new Vector2(0,-135),new Vector2(1000,65));
            Label(page,"Hint","Weather changes visibility; tire grip and acceleration stay unchanged.",18,new Vector2(0,-195),new Vector2(1000,35));
            ActionButton(page,"Back","BACK",new Vector2(-310,-245),ShowPausePage,new Vector2(280,50));ActionButton(page,"Resume","RESUME",new Vector2(310,-245),Resume,new Vector2(280,50));
        }
        private void ShowSkills()
        {
            NewPage("Skills","CYCLING SKILLS");Label(page,"Points",$"AVAILABLE: {Advanced.Available(CareerTiers())} · One per school lesson and every 5 Career tiers",21,new Vector2(0,135),new Vector2(1000,60));
            string[] names={"BALANCE","RECOVERY","AIR CONTROL"};string[] details={"Stronger grounded balance assistance","Recover 0.35 seconds faster per tier","10% faster aerial rotation per tier"};
            for(int i=0;i<3;i++){int index=i;Label(page,"Skill"+i,details[i],19,new Vector2(160,45-i*85),new Vector2(650,55));var b=ActionButton(page,"Learn"+i,$"{names[i]} {Advanced.skills[i]}/3",new Vector2(-330,45-i*85),()=>{var next=AdvancedProgress.FromJson(Advanced.ToJson());if(next.Learn(index,CareerTiers()))SaveAdvanced(next);ShowSkills();},new Vector2(330,60));b.interactable=Advanced.Available(CareerTiers())>0 && Advanced.skills[i]<3 && (i==0 || Advanced.skills[i-1]>0);}
            Label(page,"Notice",string.IsNullOrEmpty(advancedNotice)?"Unlock one Balance tier before Recovery, then one Recovery tier before Air Control.":advancedNotice,17,new Vector2(0,-185),new Vector2(1000,45));ActionButton(page,"Back","BACK",new Vector2(0,-245),ShowAdvanced,new Vector2(280,50));
        }
        private void ShowSchool()
        {
            NewPage("School","CYCLING SCHOOL");string[] names={"1 · KEEP RIGHT / SPEED LIMIT","2 · STOP SIGN","3 · RED SIGNAL"};
            Label(page,"Info","Closed practice course · 20 KM/H · Stay right of the centre line\nEach first pass earns one skill point. Retry lessons freely.",21,new Vector2(0,135),new Vector2(1000,65));
            for(int i=0;i<3;i++){int lesson=i;var b=ActionButton(page,"Lesson"+i,names[i]+(Advanced.schoolCompleted>i?" · PASSED":""),new Vector2(0,40-i*80),()=>BeginSchool(lesson),new Vector2(850,60));b.interactable=i<=Advanced.schoolCompleted && !changingBicycle;}
            ActionButton(page,"Back","BACK",new Vector2(0,-245),ShowAdvanced,new Vector2(280,50));
        }
        private void BeginSchool(int lesson)
        {
            if(changingBicycle || recovery==null || !recovery.IsReady || lesson>Advanced.schoolCompleted)return;
            Missions?.Interrupt("Cycling school started.");Missions?.Dismiss();
            if(!schoolVisit){schoolReturn=recovery.CheckpointPosition;schoolReturnHeading=recovery.CheckpointHeading;}
            if(schoolCourse==null)BuildSchool();schoolVisit=true;
            recovery.SetCheckpoint(SchoolOrigin+new Vector3(2.5f,1,0),0);recovery.Respawn();School.Begin(lesson);schoolElapsed=0;Resume();
        }
        private void LeaveSchool()
        {
            School.End("");schoolVisit=false;recovery.SetCheckpoint(schoolReturn,schoolReturnHeading);recovery.Respawn();ShowAdvanced();
        }
        private void BuildSchool()
        {
            schoolCourse=new GameObject("Cycling school course");schoolCourse.transform.SetParent(transform,false);schoolCourse.transform.position=SchoolOrigin;
            void Box(string name,Vector3 pos,Vector3 scale,Color color,bool solid){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(schoolCourse.transform,false);g.transform.localPosition=pos;g.transform.localScale=scale;if(!solid)Destroy(g.GetComponent<Collider>());var block=new MaterialPropertyBlock();block.SetColor("_Color",color);block.SetColor("_BaseColor",color);g.GetComponent<Renderer>().SetPropertyBlock(block);}
            Box("Road",new Vector3(0,-.5f,50),new Vector3(12,1,115),Color.gray,true);
            Box("Centre line",new Vector3(0,.015f,50),new Vector3(.12f,.02f,110),Color.yellow,false);
            Box("Stop box",new Vector3(2.5f,.02f,38),new Vector3(5,.02f,4),Color.red,false);
            Box("Finish",new Vector3(2.5f,.02f,95),new Vector3(5,.02f,1),Color.green,false);
            for(int z=0;z<=100;z+=20){var sign=new GameObject("20 KM/H sign");sign.transform.SetParent(schoolCourse.transform,false);sign.transform.localPosition=new Vector3(5,2,z);sign.transform.localRotation=Quaternion.Euler(0,180,0);var text=sign.AddComponent<TextMesh>();text.text=z==40?"STOP / SIGNAL":"20 KM/H\nKEEP RIGHT";text.characterSize=.22f;text.fontSize=40;text.anchor=TextAnchor.MiddleCenter;}
        }
        private void CloseAdvanced(){if(advancedRecovery!=null){advancedRecovery.Crashed-=SchoolInterrupted;advancedRecovery.Respawned-=SchoolInterrupted;}if(Environment!=null)Destroy(Environment);if(Replay!=null)Destroy(Replay);if(schoolCourse!=null)Destroy(schoolCourse);}
    }
}

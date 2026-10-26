using System;
using Cyclomania;
using UnityEngine;
public static class CanyonValidation
{
    static int count;
    static void Check(bool ok,string label){if(!ok)throw new Exception(label);count++;Debug.Log("PASS: "+label);}
    static MissionSession Start() {var s=new MissionSession();s.Start(MissionCatalog.Create(11,1));s.Tick(new MissionSample(),3,false);return s;}
    static MissionSample Takeoff()=>new MissionSample {Position=new Vector3(1000,3.5f,279),Grounded=true,StableGrounded=true,SpeedKmh=32};
    static void Launch(MissionSession s) {var r=Takeoff();s.Tick(r,.02f,false);r.Grounded=r.StableGrounded=false;r.Position+=new Vector3(0,1,3);s.Tick(r,.4f,false);}
    static MissionSample Landing()=>new MissionSample {Position=new Vector3(1000,3.5f,289),Grounded=true,StableGrounded=true};
    public static void Run() {
        var s=Start();s.Tick(Landing(),1,false);Check(!s.JumpReady && s.State==MissionState.Running,"walking onto landing deck cannot complete canyon jump");
        s.Tick(Takeoff(),.02f,false);Check(s.JumpReady,"grounded marked takeoff arms canyon jump");
        var r=Takeoff();r.Position.z=273;s.Tick(r,.02f,false);Check(!s.JumpReady,"leaving takeoff zone on ground disarms jump");
        s=Start();r=Takeoff();r.Grounded=r.StableGrounded=false;s.Tick(r,1,false);Check(!s.JumpInFlight,"arriving airborne without takeoff support cannot arm jump");
        s=Start();Launch(s);Check(s.JumpInFlight && s.JumpAirtime>=.25f,"supported takeoff followed by flight starts valid canyon attempt");
        float elapsed=s.Elapsed,air=s.JumpAirtime;s.Tick(Landing(),5,true);Check(s.Elapsed==elapsed && s.JumpAirtime==air && s.Progress==0,"pause freezes flight and landing progress");
        r=Landing();r.StableGrounded=false;s.Tick(r,.1f,false);Check(s.Progress==0 && s.Active,"one wheel landing does not complete jump");
        r.StableGrounded=true;s.Tick(r,.15f,false);r.StableGrounded=false;s.Tick(r,.02f,false);Check(s.Progress==0,"unstable landing resets settle timer");
        r.StableGrounded=true;int wins=0;s.Completed+=()=>wins++;s.Tick(r,.3f,false);s.Tick(r,5,false);Check(s.State==MissionState.Completed && wins==1,"stable landing inside target completes canyon once");
        s=Start();Launch(s);r=Landing();r.Position.x+=4;s.Tick(r,.1f,false);Check(s.State==MissionState.Failed,"landing outside target fails jump");
        s=Start();Launch(s);r=Landing();r.Position.y=0;s.Tick(r,.1f,false);Check(s.State==MissionState.Failed,"falling into canyon cannot count as landing");
        s=Start();Launch(s);r=Takeoff();s.Tick(r,.1f,false);Check(s.State==MissionState.Failed,"returning to takeoff bank fails canyon jump");
        s=Start();Launch(s);r=Landing();r.StableGrounded=false;s.Tick(r,2,false);Check(s.State==MissionState.Failed,"unsettled landing fails after two seconds");
        s=Start();r=Takeoff();s.Tick(r,.02f,false);r.Grounded=false;s.Tick(r,.1f,false);s.Tick(Landing(),.3f,false);Check(s.State==MissionState.Failed,"brief contact loss does not masquerade as canyon flight");
        s=Start();s.Tick(new MissionSample {Position=new Vector3(1000,0,245)},90,false);Check(s.State==MissionState.Failed,"canyon deadline expires without jump");
        s.Start(MissionCatalog.Create(11,1));Check(!s.JumpReady && !s.JumpInFlight && s.JumpAirtime==0,"retry clears all jump state");s.Abandon();
        var next=MissionCatalog.Create(11,2);var top=MissionCatalog.Create(11,100);Check(next.TimeLimit==88 && next.Objectives[0].landingRadius<3 && top.TimeLimit==45 && top.Objectives[0].landingRadius==2 && top.Objectives[0].target==.8f,"canyon repeats tighten deadline precision and settle hold within bounds");
        var save=MissionCareer.FromJson("{\"version\":1,\"points\":1800,\"completed\":[1,2,3,4,5,6,7,8,9,10,11]}");Check(save.completed.Length==MissionCatalog.Count && save.completed[10]==11 && save.completed[11]==0 && save.points==1800,"canyon append preserves previous eleven mission records");
        Check(save.Record(11,1,20,225) && !save.Record(11,1,20,225) && save.points==2025,"canyon reward granted once per tier");
        var invalid=MissionCatalog.Create(11,1);invalid.Objectives[0].landingRadius=float.NaN;Check(!s.Start(invalid),"invalid landing zone rejected");
        var copied=MissionCatalog.Create(11,1);s.Start(copied);copied.Objectives[0].landingPoint=Vector3.zero;Check(s.Objective.landingPoint.z==289,"landing zone snapshotted for active attempt");
        Debug.Log("CANYON_VALIDATION_SUCCESS "+count);
    }
}

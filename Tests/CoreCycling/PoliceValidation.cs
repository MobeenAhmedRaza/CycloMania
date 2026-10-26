using System;
using Cyclomania;
using UnityEngine;
public static class PoliceValidation
{
    static int count;
    static void Check(bool ok,string label) {if(!ok)throw new Exception(label);count++;Debug.Log("PASS: "+label);}
    static MissionSample Rider()=>new MissionSample {Position=new Vector3(850,0,-240),Grounded=true};
    static void Ride(MissionSession s,ref MissionSample rider,float speed,float seconds) {
        for(int i=0;i<Mathf.RoundToInt(seconds/.02f) && s.Active;i++) {rider.Position.z+=speed*.02f;s.Tick(rider,.02f,false);}
    }
    public static void Run() {
        var d=MissionCatalog.Create(9,1);var s=new MissionSession();var r=Rider();
        Check(d.Id=="police_escape" && d.Objectives[0].radius==35 && d.Objectives[0].target==5,"police escape starts with 35m gap held for five seconds");
        s.Start(d);s.Tick(r,2,false);Check(s.PolicePosition.z== -258 && s.Progress==0,"police wait behind player during countdown");s.Tick(r,1,false);
        Ride(s,ref r,8,4);Check(s.PolicePosition.z> -258 && s.PoliceGap>18,"police chase while faster cyclist builds lead");
        var position=s.PolicePosition;float time=s.Elapsed,hold=s.Progress;s.Tick(r,10,true);Check(s.PolicePosition==position && s.Elapsed==time && s.Progress==hold,"pause freezes police position escape hold and deadline");
        int rewards=0;s.Completed+=()=>rewards++;Ride(s,ref r,8,30);Check(s.State==MissionState.Completed && rewards==1 && s.Result.Contains("Escaped"),"sustained lead escapes pursuit once");s.Tick(r,10,false);Check(rewards==1,"escaped attempt cannot reward again");
        s.Start(d);r=Rider();s.Tick(r,3,false);Ride(s,ref r,0,20);Check(s.State==MissionState.Failed && s.Result.Contains("Caught"),"stationary cyclist is captured after close pursuit");
        s.Start(d);Check(s.PoliceGap==18 && s.CaptureProgress==0 && s.Progress==0,"retry resets police spawn capture and escape timers");s.Tick(r,3,false);
        Ride(s,ref r,8,6);Check(s.Progress>0,"opening required gap begins continuous escape hold");r.Grounded=false;s.Tick(r,.02f,false);Check(s.Progress==0,"airborne rider cannot complete escape hold");r.Grounded=true;
        Ride(s,ref r,0,7);Check(s.Progress==0 && s.PoliceGap<35,"police catching up resets escape hold");s.Abandon();
        s.Start(d);r=Rider();s.Tick(r,3,false);r.Position.x=861;s.Tick(r,.02f,false);Check(s.State==MissionState.Failed && s.Result.Contains("area"),"leaving marked highway pursuit area ends mission");
        var deadline=MissionCatalog.Create(9,1);deadline.TimeLimit=1;s.Start(deadline);r=Rider();s.Tick(r,3,false);s.Tick(r,1,false);Check(s.State==MissionState.Failed && s.Result.Contains("Time"),"police escape has a deadline");
        s.Start(MissionCatalog.Create(9,1));r=Rider();s.Tick(r,3,false);
        for(int i=0;i<1000 && s.CaptureProgress<.4f;i++)s.Tick(r,.02f,false);
        Check(s.CaptureProgress>0 && s.State==MissionState.Running,"brief close pursuit gives player a capture warning before failure");
        Ride(s,ref r,8,1);Check(s.State==MissionState.Running && s.CaptureProgress==0,"opening distance resets capture countdown");s.Abandon();
        var coarse=new MissionSession();var fine=new MissionSession();r=Rider();coarse.Start(d);fine.Start(d);coarse.Tick(r,3,false);fine.Tick(r,3,false);coarse.Tick(r,20,false);Ride(fine,ref r,0,20);
        Check(coarse.State==fine.State && Mathf.Abs(coarse.Elapsed-fine.Elapsed)<.03f,"large tick and physics ticks agree on stationary capture timing");
        var hard=MissionCatalog.Create(9,100);Check(hard.Objectives[0].speedKmh==22 && hard.Objectives[0].radius==65 && hard.Objectives[0].target==12,"maximum police tier has bounded pursuit speed gap and hold");
        s.Start(hard);r=Rider();s.Tick(r,3,false);Ride(s,ref r,8.9f,90);Check(s.State==MissionState.Completed && r.Position.z<400,"slowest stock sprint speed can escape maximum tier inside highway");
        var next=MissionCatalog.Create(9,2);Check(next.Objectives[0].speedKmh>d.Objectives[0].speedKmh && next.Objectives[0].radius>35 && next.Objectives[0].target>5,"repeat police mission increases all three difficulty requirements");
        var save=MissionCareer.FromJson("{\"version\":1,\"points\":1400,\"completed\":[1,2,3,4,5,6,7,8,9]}");Check(save.completed.Length==MissionCatalog.Count && save.completed[8]==9 && save.completed[9]==0 && save.points==1400,"police append preserves nine older mission tiers");
        Check(save.Record(9,1,30,200) && !save.Record(9,1,30,200) && save.points==1600,"police escape grants career reward once");
        d.Objectives[0].radius=3;Check(!s.Start(d),"escape range cannot overlap capture radius");
        Debug.Log("POLICE_VALIDATION_SUCCESS "+count);
    }
}

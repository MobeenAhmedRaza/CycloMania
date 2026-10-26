using System;
using Cyclomania;
using UnityEngine;
public static class FreestyleValidation
{
    static int count;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;Debug.Log("PASS: "+message);}
    static MissionSession Ready(int index) {var s=new MissionSession();s.Start(FreestyleCatalog.Create(index,1));s.Tick(new MissionSample(),3,false);return s;}
    public static void Run()
    {
        Check(FreestyleCatalog.Count==15 && FreestyleCatalog.Create(-1,1)==null && FreestyleCatalog.Create(15,1)==null,"freestyle catalog validates fifteen stable challenge indices");
        for(int zone=0;zone<5;zone++)Check(FreestyleCatalog.Unlocked(zone,zone*300) && (zone==0 || !FreestyleCatalog.Unlocked(zone,zone*300-1)),"freestyle zone threshold "+zone);
        bool valid=true;for(int i=0;i<15;i++)foreach(int level in new[]{1,100}) {var s=new MissionSession();valid &= s.Start(FreestyleCatalog.Create(i,level));}
        Check(valid,"all freestyle challenges have valid first and maximum tier definitions");
        var session=Ready(0);session.LandStunt(new StuntResult {Kind=StuntKind.Wheelie,Distance=60,Duration=12});session.LandStunt(new StuntResult {Kind=StuntKind.Wheelie,Distance=40,Duration=8});Check(session.Progress==60 && session.Active,"separate wheelies cannot combine into hundred metre challenge");
        session.LandStunt(new StuntResult {Kind=StuntKind.Jump,Distance=150,Duration=2});Check(session.Progress==60,"wrong stunt kind cannot complete wheelie challenge");
        session.LandStunt(new StuntResult {Kind=StuntKind.Wheelie,Distance=100,Duration=20});Check(session.State==MissionState.Completed,"validated hundred metre wheelie completes challenge");
        session=Ready(3);session.LandStunt(new StuntResult {Kind=StuntKind.Jump,FrontFlips=2,Duration=2});session.LandStunt(new StuntResult {Kind=StuntKind.Jump,FrontFlips=1,Duration=1});Check(session.Progress==2 && session.Active,"flips must occur in one jump");session.LandStunt(new StuntResult {Kind=StuntKind.Jump,BackFlips=3,Duration=3});Check(session.State==MissionState.Completed,"three flips in either direction complete industrial challenge");
        session=Ready(13);session.LandStunt(new StuntResult {Kind=StuntKind.Jump,FrontFlips=2,Duration=2});Check(session.Progress==0,"frontflips do not count as backflips");session.LandStunt(new StuntResult {Kind=StuntKind.Jump,BackFlips=2,Duration=2});Check(session.State==MissionState.Completed,"double backflip challenge recognizes landed result");
        session=Ready(14);for(int i=0;i<4;i++)session.LandStunt(new StuntResult {Kind=StuntKind.Jump,Duration=.5f});Check(session.Progress==4,"dune challenge accumulates clean jumps");session.Fail("Crash");session.LandStunt(new StuntResult {Kind=StuntKind.Jump,Duration=.5f});Check(session.State==MissionState.Failed,"failed freestyle cannot collect more stunts");
        session=Ready(1);session.BankCombo(100,2);session.BankCombo(100,1);Check(session.Progress==2 && session.Active,"separate banked combos cannot merge into a chain");session.BankCombo(150,3);Check(session.State==MissionState.Completed,"banked three stunt combo completes city challenge");
        session=new MissionSession();session.Start(FreestyleCatalog.Create(0,1));session.LandStunt(new StuntResult {Kind=StuntKind.Wheelie,Distance=200,Duration=20});Check(session.Progress==0,"countdown cannot earn freestyle stunt credit");
        var career=MissionCareer.FromJson("",FreestyleCatalog.Count);Check(career.Record(0,1,30,150) && !career.Record(0,1,30,150) && career.points==150,"freestyle tier reward granted once");career.Record(0,2,30,175);Check(FreestyleCatalog.Unlocked(1,career.points),"earned challenge points unlock industrial zone");
        var loaded=MissionCareer.FromJson(career.ToJson(),FreestyleCatalog.Count);Check(loaded.completed.Length==15 && loaded.completed[0]==2 && loaded.points==325,"freestyle save round trip preserves independent challenge progression");
        Check(MissionCareer.FromJson("broken",FreestyleCatalog.Count).completed.Length==15 && MissionCareer.FromJson("broken",FreestyleCatalog.Count).points==0,"corrupt freestyle save recovers fifteen challenge slots");
        bool bounds=true;for(int z=0;z<5;z++)bounds &= FreestyleCatalog.Contains(z,RideWorld.Destinations[z].Position);Check(bounds && !FreestyleCatalog.Contains(3,Vector3.zero),"freestyle zone bounds contain their spawns and reject other zones");
        // Exercise actual scoring callbacks, including the combo count captured before reset.
        session=Ready(1);var scoring=new StuntScoring();scoring.StuntLanded+=session.LandStunt;scoring.ComboFinished+=session.BankCombo;
        var ground=new StuntSample {FrontGrounded=true,RearGrounded=true,UprightDot=1,Speed=5};var wheelie=new StuntSample {RearGrounded=true,UprightDot=.85f,Speed=5,NoseAngle=35};
        for(int i=0;i<20;i++)scoring.Tick(ground,.02f);
        for(int j=0;j<3;j++) {for(int i=0;i<50;i++)scoring.Tick(wheelie,.02f);for(int i=0;i<20;i++)scoring.Tick(ground,.02f);}
        Check(session.State==MissionState.Running,"pending combo cannot complete freestyle chain before banking");
        for(int i=0;i<150;i++)scoring.Tick(ground,.02f);Check(session.State==MissionState.Completed,"real scoring callback banks full combo count into freestyle challenge");
        Debug.Log("FREESTYLE_VALIDATION_SUCCESS "+count);
    }
}

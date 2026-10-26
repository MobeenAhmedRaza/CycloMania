using System;
using System.Reflection;
using Cyclomania;
using SBPScripts;
using UnityEngine;

public static class StuntValidation
{
    static int checks;
    static void Check(bool pass,string label) { if(!pass) throw new Exception(label);checks++;Debug.Log("PASS: "+label); }
    static StuntSample Ground => new StuntSample { FrontGrounded=true,RearGrounded=true,UprightDot=1,Speed=5 };
    static StuntSample Wheelie => new StuntSample { RearGrounded=true,UprightDot=.85f,NoseAngle=35,Speed=5 };
    static StuntSample Stoppie => new StuntSample { FrontGrounded=true,UprightDot=.85f,NoseAngle=-28,Speed=3 };
    static void Tick(StuntScoring scoring,StuntSample sample,int frames=1,float dt=.02f) { for(int i=0;i<frames;i++) scoring.Tick(sample,dt); }
    static StuntScoring Ready() { var s=new StuntScoring();Tick(s,Ground,20);return s; }
    public static void Run(BicycleController bike)
    {
        var s=Ready();StuntResult landed=default;int results=0;s.StuntLanded+=r=>{landed=r;results++;};
        Tick(s,Wheelie,50);
        Check(Mathf.Abs(s.ActiveDuration-1)<.001f && Mathf.Abs(s.ActiveDistance-5)<.001f,"wheelie duration and distance track physical motion");
        Check(s.Score==0 && s.PendingScore==0,"held wheelie cannot bank before landing");
        Tick(s,Ground,5);Check(results==0 && s.PendingScore>0,"landing requires a settling period");
        Tick(s,Ground,20);Check(results==1 && landed.Kind==StuntKind.Wheelie,"clean landing emits validated wheelie result");
        Tick(s,Ground,140);Check(s.Score==150 && s.PendingScore==0 && s.BestCombo==150,"idle grounded combo banks exactly once");
        Tick(s,Ground,200);Check(s.Score==150,"banked score is not duplicated");
        s=Ready();Tick(s,Stoppie,50);Tick(s,Ground,20);Tick(s,Ground,140);
        Check(s.Score==130,"stoppie scores duration and distance");
        s=Ready();Tick(s,Wheelie,10);Tick(s,Ground,200);Check(s.Score==0,"brief wheel-contact noise does not score");
        s=Ready();var stationary=Wheelie;stationary.Speed=0;Tick(s,stationary,100);Tick(s,Ground,200);Check(s.Score==0,"stationary wheel lift cannot farm score");
        s=Ready();Tick(s,Wheelie,50);Tick(s,Ground,20);Tick(s,Stoppie,50);Tick(s,Ground,20);
        Check(s.ComboCount==2 && s.Multiplier==2 && s.PendingScore==560,"consecutive stunts chain into a multiplier");
        s.Cancel();Tick(s,Ground,200);Check(s.Score==0 && s.PendingScore==0,"crash or reset forfeits unbanked combo");
        s=Ready();Tick(s,new StuntSample { UprightDot=1,Speed=5,PitchDelta=-9 },40);
        Tick(s,Ground,20);Check(s.PendingScore==700,"complete backflip adds flip bonus to jump and airtime");
        Tick(s,Ground,140);Check(s.Score==700,"validated backflip banks after combo window");
        s=Ready();results=0;s.StuntLanded+=r=>{landed=r;results++;};
        Tick(s,new StuntSample {UprightDot=1,Speed=5,PitchDelta=9},80);Tick(s,Ground,20);
        Check(results==1 && landed.FrontFlips==2 && landed.BackFlips==0,"multiple frontflips count in a single jump");
        s=Ready();Tick(s,new StuntSample {UprightDot=1,Speed=5,PitchDelta=9},20);
        Tick(s,new StuntSample {UprightDot=1,Speed=5,PitchDelta=-9},20);Tick(s,Ground,20);
        Check(s.PendingScore==200,"rocking back and forth does not count a flip");
        s=Ready();Tick(s,new StuntSample {UprightDot=1,Speed=5},40);
        Tick(s,new StuntSample {FrontGrounded=true,UprightDot=-1},1);Tick(s,Ground,200);
        Check(s.Score==0 && s.PendingScore==0,"inverted landing loses air combo");
        s=new StuntScoring();Tick(s,new StuntSample {UprightDot=1,Speed=5},100);Tick(s,Ground,200);
        Check(s.Score==0,"initial spawn fall is not a jump");
        s=Ready();Tick(s,Wheelie,50);float duration=s.ActiveDuration;Tick(s,Wheelie,100,0);
        Check(s.ActiveDuration==duration,"paused scoring does not advance");
        var a=Ready();var b=Ready();Tick(a,Wheelie,50,.02f);Tick(b,Wheelie,100,.01f);Tick(a,Ground,200);Tick(b,Ground,200);
        Check(a.Score==b.Score,"scoring is independent of sample rate");
        s=Ready();for(int i=0;i<7;i++){Tick(s,Wheelie,50);Tick(s,Ground,20);}
        Check(s.Multiplier==5,"combo multiplier is capped");s.ResetSession();Check(s.Score==0 && s.BestCombo==0 && s.PendingScore==0,"restart clears stunt session");
        TestPhysics(bike);
        Debug.Log("STUNT_VALIDATION_SUCCESS "+checks);
    }
    static void TestPhysics(BicycleController bike)
    {
        var recovery=bike.GetComponent<CycleRecovery>();recovery.Respawn();
        // Fixture bodies have no gravity; check applied pitch control without contact impulses.
        bike.wheelieToggle=true;bike.wheelieInput=true;bike.rawCustomAccelerationAxis=1;
        foreach(var body in bike.GetComponentsInChildren<Rigidbody>()) {body.position+=Vector3.up*10;body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}
        Physics.SyncTransforms();
        bike.GetType().GetMethod("FixedUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(bike,null);Physics.Simulate(.02f);
        Check(Vector3.Dot(bike.rb.angularVelocity,bike.transform.right)<0,"airborne wheelie plus pedal rotates into a backflip");
        foreach(var body in bike.GetComponentsInChildren<Rigidbody>()) body.angularVelocity=Vector3.zero;
        bike.rawCustomAccelerationAxis=-1;
        bike.GetType().GetMethod("FixedUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(bike,null);Physics.Simulate(.02f);
        Check(Vector3.Dot(bike.rb.angularVelocity,bike.transform.right)>0,"airborne wheelie plus brake rotates into a frontflip");
        recovery.Respawn();bike.wheelieInput=false;
        bike.rb.position=Vector3.zero;bike.rb.rotation=Quaternion.identity;
        bike.fWheelRb.position=new Vector3(0,.35f,.7f);bike.rWheelRb.position=new Vector3(0,.35f,-.7f);
        foreach(var body in bike.GetComponentsInChildren<Rigidbody>()) {body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}
        Physics.SyncTransforms();
        bike.bunnyHopStrength=5;
        var queued=typeof(BicycleController).GetField("queuedHop",BindingFlags.Instance|BindingFlags.NonPublic);
        queued.SetValue(bike,1f);
        bike.GetType().GetMethod("FixedUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(bike,null);Physics.Simulate(.02f);
        Debug.Log($"Hop: grounded={bike.IsGrounded}, frame={bike.rb.linearVelocity}, front={bike.fWheelRb.linearVelocity}, rear={bike.rWheelRb.linearVelocity}");
        Check(bike.rb.linearVelocity.y>4 && bike.fWheelRb.linearVelocity.y>4 && bike.rWheelRb.linearVelocity.y>4,"jump lifts frame and both wheels together");
        Check((float)queued.GetValue(bike)==0,"jump impulse is consumed exactly once");
        queued.SetValue(bike,1f);bike.ClearInput();Check((float)queued.GetValue(bike)==0,"pause/reset cancels a queued jump");
        recovery.Respawn();
        var tracker=bike.gameObject.AddComponent<BicycleStunts>();
        typeof(BicycleStunts).GetMethod("Start",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(tracker,null);
        Tick(tracker.Scoring,Ground,20);Tick(tracker.Scoring,Wheelie,50);Tick(tracker.Scoring,Ground,20);
        recovery.Crash();Check(tracker.Scoring.PendingScore==0,"live crash event clears pending stunt score");
        recovery.Respawn();Tick(tracker.Scoring,Ground,20);Tick(tracker.Scoring,Wheelie,50);Tick(tracker.Scoring,Ground,20);
        recovery.Respawn();Check(tracker.Scoring.PendingScore==0 && tracker.Scoring.ActiveDuration==0,"live respawn event clears unfinished stunt state");
        bike.rb.position=new Vector3(0,.72f,0);bike.rb.rotation=Quaternion.Euler(180,0,0);
        bike.fWheelRb.position=new Vector3(0,5,.7f);bike.rWheelRb.position=new Vector3(0,5,-.7f);
        typeof(CycleRecovery).GetField("protectionTime",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(recovery,0f);
        Physics.SyncTransforms();Physics.Simulate(.02f);
        Check(recovery.IsCrashed,"low-speed inverted frame landing triggers crash recovery");
        recovery.Respawn();

    }
}

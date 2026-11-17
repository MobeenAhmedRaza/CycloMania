using System;
using System.Reflection;
using Cyclomania;
using SBPScripts;
using UnityEngine;
public static class HandlingValidation
{
    static int count;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;Debug.Log("PASS: "+message);}
    static void Invoke(object o,string name)=>o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,null);
    public static void Run(BicycleController bike)
    {
        var bodies=new[]{bike.rb,bike.fWheelRb,bike.rWheelRb};var positions=new Vector3[3];var rotations=new Quaternion[3];
        for(int i=0;i<3;i++){positions[i]=bodies[i].position;rotations[i]=bodies[i].rotation;}
        float oldTop=bike.topSpeed,oldBrake=bike.brakeDeceleration;var oldCurve=bike.accelerationCurve;var oldSteer=bike.steerAngle;
        void Reset(){bike.ClearInput();for(int i=0;i<3;i++){bodies[i].position=positions[i];bodies[i].rotation=rotations[i];bodies[i].linearVelocity=Vector3.zero;bodies[i].angularVelocity=Vector3.zero;}bike.customSteerAxis=bike.customLeanAxis=bike.rawCustomAccelerationAxis=bike.customAccelerationAxis=0;Physics.SyncTransforms();Invoke(bike,"UpdateGroundContact");}
        var garage=new GarageProgress();
        for(int i=0;i<GarageProgress.Catalog.Length;i++) {
            var spec=GarageProgress.Catalog[i];Check(garage.SprintSpeed(i)==spec.Speed*3 && garage.BrakePower(i)==spec.Brake*3 && garage.DriveForce(i)==spec.Drive,"triple speed and brake with unchanged drive: "+spec.Name);
        }
        garage.Apply(bike,0);Check(bike.topSpeed==37.5f && bike.brakeDeceleration==21 && bike.accelerationCurve.Evaluate(1)==200,"city bicycle applies 135 kmh sprint and stronger brake without increasing pedal force");
        var method=bike.GetType().GetMethod("ApplyGroundSteering",BindingFlags.Instance|BindingFlags.NonPublic);
        Reset();bike.customSteerAxis=1;foreach(var body in bodies)body.linearVelocity=new Vector3(0,2,30);
        var steered=(Quaternion)method.Invoke(bike,new object[]{bike.rb.rotation});
        Check(Mathf.DeltaAngle(bike.rb.rotation.eulerAngles.y,steered.eulerAngles.y)>0,"right steering directly changes heading at high speed");
        Check(Mathf.Abs(bike.rb.linearVelocity.magnitude-Mathf.Sqrt(904))<.001f && Mathf.Abs(bike.rb.linearVelocity.y-2)<.001f,"steering preserves speed and normal velocity without acceleration boost");
        Check(bike.rb.linearVelocity.x>0 && bike.fWheelRb.linearVelocity.x>0 && bike.rWheelRb.linearVelocity.x>0,"frame and both wheels grip toward steering direction");
        Reset();bike.customSteerAxis=1;var stationary=(Quaternion)method.Invoke(bike,new object[]{bike.rb.rotation});Check(Quaternion.Angle(stationary,bike.rb.rotation)<.001f,"stationary bicycle does not spin from steering");
        foreach(float direction in new[]{-1f,1f}) {
            Reset();float heading=bike.rb.rotation.eulerAngles.y;bike.customSteerAxis=direction;
            foreach(var body in bodies)body.linearVelocity=Vector3.forward*6;
            for(int i=0;i<50;i++){Invoke(bike,"FixedUpdate");Physics.Simulate(.02f);}
            float turn=Mathf.DeltaAngle(heading,bike.rb.rotation.eulerAngles.y)*direction;
            Check(turn>20 && turn<140,"grounded physics produces controlled "+(direction<0?"left":"right")+" turn within one second");
        }
        Reset();foreach(var body in bodies)body.linearVelocity=Vector3.forward*30;bike.rawCustomAccelerationAxis=bike.customAccelerationAxis=-1;
        for(int i=0;i<50;i++){Invoke(bike,"FixedUpdate");Physics.Simulate(.02f);}
        Check(Vector3.ProjectOnPlane(bike.rb.linearVelocity,Vector3.up).magnitude<12,"stronger brakes reduce high speed promptly");
        Reset();bike.rawCustomAccelerationAxis=bike.customAccelerationAxis=-1;
        for(int i=0;i<10;i++){Invoke(bike,"FixedUpdate");Physics.Simulate(.02f);}
        Check(Vector3.Dot(bike.rb.linearVelocity,bike.rb.rotation*Vector3.forward)>-.05f,"brief brake tap at rest does not reverse");
        for(int i=0;i<100;i++){Invoke(bike,"FixedUpdate");Physics.Simulate(.02f);}
        float reverseVelocity=Vector3.Dot(bike.rb.linearVelocity,bike.rb.rotation*Vector3.forward);
        Check(reverseVelocity<-.5f && reverseVelocity>=-bike.reverseSpeed-.2f,"holding brake at rest reverses at controlled speed");
        bike.rawCustomAccelerationAxis=bike.customAccelerationAxis=1;
        for(int i=0;i<60;i++){Invoke(bike,"FixedUpdate");Physics.Simulate(.02f);}
        Check(Vector3.Dot(bike.rb.linearVelocity,bike.rb.rotation*Vector3.forward)>0,"pedaling brakes reverse motion before driving forward");
        Reset();bike.rawCustomAccelerationAxis=bike.customAccelerationAxis=-1;bike.wheelieInput=true;
        for(int i=0;i<25;i++){Invoke(bike,"FixedUpdate");Physics.Simulate(.02f);}
        Check(Vector3.Dot(bike.rb.linearVelocity,bike.rb.rotation*Vector3.forward)>-.1f,"stoppie input does not engage reverse");
        Reset();var curb=GameObject.CreatePrimitive(PrimitiveType.Cube);curb.name="Curb test";
        curb.transform.position=new Vector3(0,.1f,2);curb.transform.localScale=new Vector3(4,.2f,2);
        Physics.SyncTransforms();bike.rawCustomAccelerationAxis=1;Vector3 beforeStep=bike.rb.position;Vector3 spacing=bike.fWheelRb.position-bike.rWheelRb.position;
        Invoke(bike,"TryClimbCurb");
        Check(bike.rb.position.y>beforeStep.y+.15f && bike.rb.position.z>beforeStep.z,"pedaling climbs a 20 cm sidewalk curb");
        Check(Vector3.Distance(spacing,bike.fWheelRb.position-bike.rWheelRb.position)<.001f && bike.rb.linearVelocity==Vector3.zero,"curb assist preserves joint spacing and does not add velocity");
        Reset();curb.transform.position=new Vector3(0,.5f,2);curb.transform.localScale=new Vector3(4,1,2);Physics.SyncTransforms();bike.rawCustomAccelerationAxis=1;beforeStep=bike.rb.position;Invoke(bike,"TryClimbCurb");
        Check(bike.rb.position==beforeStep,"curb assist rejects tall walls");
        Reset();curb.transform.position=new Vector3(0,.1f,2);curb.transform.localScale=new Vector3(4,.2f,2);Physics.SyncTransforms();beforeStep=bike.rb.position;Invoke(bike,"TryClimbCurb");Check(bike.rb.position==beforeStep,"idle bicycle does not climb nearby curbs");
        bike.rawCustomAccelerationAxis=1;bike.wheelieInput=true;Invoke(bike,"TryClimbCurb");Check(bike.rb.position==beforeStep,"curb assist does not interfere with stunt input");
        bike.wheelieInput=false;var ceiling=GameObject.CreatePrimitive(PrimitiveType.Cube);ceiling.transform.position=new Vector3(0,1.05f,0);ceiling.transform.localScale=new Vector3(5,.1f,5);Physics.SyncTransforms();Invoke(bike,"TryClimbCurb");Check(bike.rb.position==beforeStep,"curb assist respects overhead clearance");
        UnityEngine.Object.DestroyImmediate(ceiling);UnityEngine.Object.DestroyImmediate(curb);
        Reset();var hbObject=new GameObject("Handbrake physics",typeof(RectTransform));var hb=hbObject.AddComponent<MobileButtonHandler>();bike.mobileControls.handbrake=hb;hb.buttonPressed=1;Invoke(bike,"ApplyCustomInput");
        foreach(var body in bodies)body.linearVelocity=Vector3.forward*10;bike.rawCustomAccelerationAxis=bike.customAccelerationAxis=1;
        for(int i=0;i<40;i++){Invoke(bike,"FixedUpdate");Physics.Simulate(.02f);}
        Check(bike.rb.linearVelocity.magnitude<.3f,"handbrake overrides pedal and rapidly stops grounded bicycle");
        bike.rawCustomAccelerationAxis=bike.customAccelerationAxis=-1;
        for(int i=0;i<40;i++){Invoke(bike,"FixedUpdate");Physics.Simulate(.02f);}
        Check(Vector3.Dot(bike.rb.linearVelocity,bike.rb.rotation*Vector3.forward)>-.05f,"handbrake prevents reverse while brake/down is held");
        bike.ClearInput();UnityEngine.Object.DestroyImmediate(hbObject);bike.mobileControls.handbrake=null;
        Reset();bike.topSpeed=oldTop;bike.brakeDeceleration=oldBrake;bike.accelerationCurve=oldCurve;bike.steerAngle=oldSteer;
        Debug.Log("HANDLING_VALIDATION_SUCCESS "+count);
    }
}

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
        void Reset(){for(int i=0;i<3;i++){bodies[i].position=positions[i];bodies[i].rotation=rotations[i];bodies[i].linearVelocity=Vector3.zero;bodies[i].angularVelocity=Vector3.zero;}bike.customSteerAxis=bike.customLeanAxis=bike.rawCustomAccelerationAxis=bike.customAccelerationAxis=0;Physics.SyncTransforms();Invoke(bike,"UpdateGroundContact");}
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
        Reset();bike.topSpeed=oldTop;bike.brakeDeceleration=oldBrake;bike.accelerationCurve=oldCurve;bike.steerAngle=oldSteer;
        Debug.Log("HANDLING_VALIDATION_SUCCESS "+count);
    }
}

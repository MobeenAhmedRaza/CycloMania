using System;
using System.Reflection;
using Cyclomania;
using SBPScripts;
using UnityEditor;
using UnityEngine;

public static class CoreValidation
{
    static int passed;
    static void Invoke(object o, string name) => o.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(o, null);
    static void Check(bool ok, string name) { if (!ok) throw new Exception(name); Debug.Log("PASS: " + name); passed++; }
    public static void Run()
    {
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
        EditorApplication.playModeStateChanged += state => {
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            int errors=0;
            Application.logMessageReceived += (message,trace,type) => { if(type==LogType.Exception || type==LogType.Error || type==LogType.Assert) errors++; };
            try { Tests(); if(errors>0) throw new Exception("Unity logged "+errors+" runtime errors during validation."); Debug.Log("CORE_VALIDATION_SUCCESS " + passed); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        };
        EditorApplication.EnterPlaymode();
    }
    static void Tests()
    {
        Physics.simulationMode = SimulationMode.Script;
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.transform.position = new Vector3(0,-0.5f,0); ground.transform.localScale = new Vector3(100,1,100);
        var root = new GameObject("Test bicycle");
        var rb = root.AddComponent<Rigidbody>(); rb.useGravity=false; rb.mass=40;
        var box=root.AddComponent<BoxCollider>(); box.size = new Vector3(.1f,.1f,.1f); box.center=Vector3.up*.7f;
        var bike = root.AddComponent<BicycleController>(); bike.enabled=false;
        GameObject Child(string name) { var g=new GameObject(name);g.transform.SetParent(root.transform);return g; }
        GameObject Wheel(string name,float z) { var g=Child(name);g.transform.localPosition=new Vector3(0,.35f,z);g.AddComponent<SphereCollider>().radius=.35f;g.AddComponent<Rigidbody>().useGravity=false;g.AddComponent<ConfigurableJoint>().connectedBody=rb;return g; }
        bike.fPhysicsWheel=Wheel("Front",.7f);bike.rPhysicsWheel=Wheel("Rear",-.7f);
        var front=Child("FrontVisual");new GameObject("Mesh").transform.SetParent(front.transform);
        bike.cycleGeometry = new CycleGeometry {handles=Child("Handles"),lowerFork=Child("Fork"),fWheelVisual=front,RWheel=bike.rPhysicsWheel,crank=Child("Crank"),lPedal=Child("LPedal"),rPedal=Child("RPedal")};
        bike.pedalAdjustments=new PedalAdjustments();
        bike.wheelFrictionSettings=new WheelFrictionSettings {fPhysicMaterial=new PhysicsMaterial(),rPhysicMaterial=new PhysicsMaterial(),fFriction=Vector2.one,rFriction=Vector2.one};
        bike.accelerationCurve=AnimationCurve.Linear(0,0,1,200);bike.steerAngle=AnimationCurve.Linear(0,25,15,8);bike.leanCurve=AnimationCurve.Linear(-1,-12,1,12);
        bike.airTimeSettings=new AirTimeSettings();bike.mobileControls=new MobileControls();
        bike.wayPointSystem=new WayPointSystem {bicyclePositionTransform=new System.Collections.Generic.List<Vector3>(),bicycleRotationTransform=new System.Collections.Generic.List<Quaternion>(),movementInstructionSet=new System.Collections.Generic.List<Vector2Int>(),sprintInstructionSet=new System.Collections.Generic.List<bool>(),bHopInstructionSet=new System.Collections.Generic.List<int>()};
        bike.topSpeed=12.5f;bike.relaxedSpeed=.5f;bike.torque=10;
        Invoke(bike,"Start"); Physics.SyncTransforms();Invoke(bike,"UpdateGroundContact");
        Check(bike.IsGrounded && !bike.isAirborne,"flat ground contact");
        bike.rawCustomAccelerationAxis=bike.customAccelerationAxis=1;Invoke(bike,"FixedUpdate");Physics.Simulate(.02f);
        Debug.Log("Drive velocity="+rb.linearVelocity+" rear="+bike.rWheelRb.linearVelocity+" grounded="+bike.IsGrounded+" top="+bike.currentTopSpeed); Check(rb.linearVelocity.z>0,"pedaling propels grounded bicycle");
        rb.linearVelocity=Vector3.forward*.05f;bike.fWheelRb.linearVelocity=rb.linearVelocity;bike.rWheelRb.linearVelocity=rb.linearVelocity;
        bike.rawCustomAccelerationAxis=-1;bike.customAccelerationAxis=-1;Invoke(bike,"FixedUpdate");Physics.Simulate(.02f);
        Check(rb.linearVelocity.z>=-.001f && rb.linearVelocity.z<.05f,"braking stops without reverse propulsion");
        HandlingValidation.Run(bike);
        ground.SetActive(false);Invoke(bike,"UpdateGroundContact");
        Check(!bike.IsGrounded && bike.isAirborne,"riding off world clears grounded state");
        rb.linearVelocity=Vector3.zero;bike.fWheelRb.linearVelocity=Vector3.zero;bike.rWheelRb.linearVelocity=Vector3.zero;
        bike.rawCustomAccelerationAxis=bike.customAccelerationAxis=1;Invoke(bike,"FixedUpdate");Physics.Simulate(.02f);
        Check(rb.linearVelocity.magnitude<.01f,"pedaling in air adds no thrust");
        ground.SetActive(true);ground.transform.rotation=Quaternion.Euler(15,0,0);Physics.SyncTransforms();Invoke(bike,"UpdateGroundContact");
        Check(bike.IsGrounded && Vector3.Angle(bike.GroundNormal,Vector3.up)>10,"slope contact supplies terrain normal");
        ground.transform.rotation=Quaternion.identity;
        var recovery=root.AddComponent<CycleRecovery>();recovery.enabled=false;Invoke(recovery,"Start");
        recovery.Crash();Check(recovery.IsCrashed && bike.rawCustomAccelerationAxis==0,"crash suppresses riding input");
        root.transform.position+=Vector3.one*10;rb.linearVelocity=Vector3.one*5;bike.rWheelRb.angularVelocity=Vector3.one*20;
        recovery.Respawn();Check(!recovery.IsCrashed && rb.linearVelocity==Vector3.zero && bike.rWheelRb.angularVelocity==Vector3.zero && root.transform.position.magnitude<1,"respawn resets jointed assembly and velocities");
        var cameraObject=new GameObject("Camera");cameraObject.AddComponent<Camera>();var camera=cameraObject.AddComponent<BicycleCamera>();camera.enabled=false;camera.target=root.transform;Invoke(camera,"Start");
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=root.transform.position+new Vector3(0,2,-2);wall.transform.localScale=new Vector3(5,5,.2f);Physics.SyncTransforms();camera.Snap();
        Check(camera.transform.position.z>-1.9f,"camera pulls in ahead of obstruction");
        TouchValidation.Run(bike);
        HudValidation.Run(bike);
        StuntValidation.Run(bike);
        GarageValidation.Run(bike);
        MissionValidation.Run();
        SettingsValidation.Run();
        EconomyValidation.Run();
        AdvancedValidation.Run();
        RecurringValidation.Run();
    }
}

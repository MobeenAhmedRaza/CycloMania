using System;
using Cyclomania;
using UnityEngine;
public static class AdvancedValidation
{
    static int count;
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;Debug.Log("PASS: "+name);}
    public static void Run()
    {
        var p=AdvancedProgress.FromJson("broken");Check(p.skills.Length==3 && !p.firstPerson,"advanced corrupt save defaults safely");
        p=AdvancedProgress.FromJson("{\"version\":1,\"weather\":9,\"timePreset\":-2,\"schoolCompleted\":9,\"skills\":[0,3,3]}");Check(p.weather==3 && p.timePreset==0 && p.schoolCompleted==3 && p.skills[1]==0 && p.skills[2]==0,"advanced save clamps settings and prerequisites");
        p=new AdvancedProgress();Check(!p.Learn(0,0) && !p.Learn(-1,20),"skills reject unavailable points and invalid branches");p.schoolCompleted=3;
        Check(!p.Learn(1,0) && p.Learn(0,0) && p.Learn(1,0) && p.Learn(2,0),"skill prerequisites and point spending enforced");Check(p.Available(0)==0 && p.Available(10)==2,"career tiers grant additional skill points");
        p.firstPerson=true;p=AdvancedProgress.FromJson(p.ToJson());Check(p.firstPerson && p.skills[2]==1,"skills and camera preference survive serialization");
        var lesson=new SchoolLesson();lesson.Begin(0);lesson.Tick(new Vector3(2,0,0),25,1,1,false,false);Check(!lesson.Active && !lesson.Passed,"school fails speeding");
        lesson.Begin(0);lesson.Tick(new Vector3(-1,0,0),10,1,1,false,false);Check(!lesson.Active,"school fails riding in opposite lane");
        lesson.Begin(0);for(int z=0;z<=100;z+=5)lesson.Tick(new Vector3(2,0,z),10,1,z,false,false);Check(lesson.Passed,"school right-lane lesson completes at finish");
        lesson.Begin(1);lesson.Tick(new Vector3(2,0,39),0,1,1,false,false);lesson.Tick(new Vector3(2,0,41),5,1,2,false,false);Check(!lesson.Active && !lesson.Passed,"school stop line rejects rolling through");
        lesson.Begin(1);lesson.Tick(new Vector3(2,0,38),0,1,1,false,false);lesson.Tick(new Vector3(2,0,38),0,1,2,false,false);for(int z=40;z<=95;z+=5)lesson.Tick(new Vector3(2,0,z),10,1,z,false,false);Check(lesson.Passed,"school accepts two-second full stop then finish");
        lesson.Begin(2);lesson.Tick(new Vector3(2,0,41),10,1,9,false,false);Check(!lesson.Active,"school rejects red signal crossing");
        lesson.Begin(2);lesson.Tick(new Vector3(2,0,39),0,1,9,false,false);for(int z=40;z<=95;z+=5)lesson.Tick(new Vector3(2,0,z),10,1,11,false,false);Check(lesson.Passed,"school green signal permits crossing");
        lesson.Begin(0);lesson.Tick(new Vector3(-2,0,0),30,1,1,true,true);Check(lesson.Active,"paused school ignores simulation");lesson.Tick(Vector3.zero,0,1,1,true,false);Check(!lesson.Active,"school crash interrupts attempt");
        lesson.Begin(0);lesson.Tick(new Vector3(2,0,0),0,1,1,false,false);lesson.Tick(new Vector3(2,0,95),0,1,2,false,false);Check(!lesson.Passed && !lesson.Active,"school rejects teleporting to finish");
        var go=new GameObject("Environment test");bool oldFog=RenderSettings.fog;var env=go.AddComponent<RideEnvironment>();env.Initialize();env.Configure(2,2,false);Check(env.Weather==2 && env.Hour==0 && RenderSettings.fogDensity>.01f,"night fog applies visibility settings");env.Configure(0,0,true);Check(env.Hour==12 && env.Cycle,"day cycle and noon preset apply");UnityEngine.Object.DestroyImmediate(env);Check(RenderSettings.fog==oldFog,"environment restores prior render state on teardown");UnityEngine.Object.DestroyImmediate(go);
        Debug.Log("ADVANCED_VALIDATION_SUCCESS "+count);
    }
}

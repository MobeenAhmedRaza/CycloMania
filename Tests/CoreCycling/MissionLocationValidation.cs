using System;
using System.Collections.Generic;
using Cyclomania;
using UnityEngine;
public static class MissionLocationValidation
{
    static int count;
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;Debug.Log("PASS: "+name);}
    public static void Run()
    {
        var slots=new HashSet<int>();for(int m=0;m<13;m++)for(int level=1;level<=100;level++)slots.Add(MissionLocationData.Slot(m,level,8839));
        Check(slots.Count==1300,"every mission and level maps to a distinct street start");Check(MissionLocationData.Slot(4,5,8839)==MissionLocationData.Slot(4,5,8839),"retry location is deterministic");
        var path=new[]{Vector3.zero,new Vector3(0,0,50),new Vector3(50,0,50)};
        Check(Vector3.Distance(MissionRoute.Point(path,75),new Vector3(25,0,50))<.001f,"route distance follows turns");
        float along=MissionRoute.Nearest(path,new Vector3(20,0,52),out float gap);Check(Mathf.Abs(along-70)<.001f && Mathf.Abs(gap-2)<.001f,"race progress uses curved route distance and lateral gap");
        var definition=new MissionDefinition {Id="rival_sprint",Title="Route test",Level=1,TimeLimit=100,Objectives=new[]{new MissionObjective {kind=ObjectiveKind.RivalRace,target=100,speedKmh=1,radius=6,route=path}}};
        var attempt=new MissionSession();Check(attempt.Start(definition),"curved race starts");Check(!ReferenceEquals(attempt.Definition.Objectives[0].route,path),"attempt owns an independent route snapshot");attempt.Tick(new MissionSample(),3,false);attempt.Tick(new MissionSample{Position=new Vector3(50,0,50),Grounded=true,SpeedKmh=30},10,false);Check(attempt.State==MissionState.Completed,"curved race finishes at path endpoint rather than world Z coordinate");
        attempt=new MissionSession();attempt.Start(definition);attempt.Tick(new MissionSample(),3,false);attempt.Tick(new MissionSample{Position=new Vector3(20,0,20)},1,false);Check(attempt.State==MissionState.Failed,"cutting across a block outside the route fails race");
        var zone=new MissionObjective{useZone=true,route=path};Check(zone.Contains(new Vector3(25,0,52)) && !zone.Contains(new Vector3(25,0,25)),"speed zone follows street bends");
        definition.Objectives[0].route=new[]{Vector3.zero,new Vector3(float.NaN,0,0)};Check(!new MissionSession().Start(definition),"malformed route coordinates rejected");
        var canyon=MissionCatalog.Create(MissionCatalog.CanyonJumpIndex,1);var offset=new Vector3(-1400,10,-800);foreach(var o in canyon.Objectives){o.point+=offset;o.landingPoint+=offset;}attempt=new MissionSession();attempt.Start(canyon);attempt.Tick(new MissionSample(),3,false);attempt.Tick(new MissionSample{Position=canyon.Objectives[0].point,Grounded=true},.02f,false);Check(attempt.JumpReady,"canyon bounds use relocated objective coordinates");
        Debug.Log("MISSION_LOCATION_VALIDATION_SUCCESS "+count);
    }
}

using System;
using System.Linq;
using Cyclomania;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class AuthoredLevelValidation
{
    static RideLevelLayout layout;
    static GameplayHud hud;
    static int geometryCount,frames,stage,checks,failures,runtimeErrors;
    static void Check(bool valid,string message) {
        checks++;if(!valid){failures++;Debug.LogError("LEVEL_CHECK_FAILED: "+message);}
    }
    static void Surface(Vector3 p,string message,bool clear=true) {
        var hits=Physics.RaycastAll(p+Vector3.up*.5f,Vector3.down,1.2f);
        Check(hits.Any(h=>!h.collider.isTrigger && h.normal.y>.65f),message+" ground "+p);
        if(clear)foreach(var c in Physics.OverlapBox(p+Vector3.up*1.3f,new Vector3(.6f,.85f,.6f)))
            Check(c.isTrigger || c.GetComponentInParent<SBPScripts.BicycleController>()!=null,message+" clearance: "+c.name+" "+p);
    }
    public static void Run() {
        EditorSceneManager.OpenScene("Assets/Scenes/gameplay.unity");
        layout=Object.FindFirstObjectByType<RideLevelLayout>();
        Check(layout!=null,"saved scene contains layout");
        geometryCount=layout.GetComponentsInChildren<Collider>(true).Length;
        Check(geometryCount>20,"courses exist before entering Play mode");
        Physics.SyncTransforms();
        for(int i=0;i<5;i++) {
            Surface(layout.arrivals[i].position,"arrival "+i);
            Check(layout.playableBounds.Contains(layout.arrivals[i].position),"arrival within world "+i);
        }
        Surface(layout.canyonStart.position,"canyon start");
        for(int z=0;z<=100;z+=5)Surface(layout.school.position+new Vector3(2.5f,0,z),"school "+z);
        for(int z=-340;z<=420;z+=5)Surface(layout.Point(3,new Vector3(850,0,z)),"highway "+z);
        var ascent=new[]{new Vector3(730,0,80),new Vector3(730,10,160),new Vector3(780,14,180),new Vector3(730,24,250),new Vector3(790,24,250),new Vector3(790,0,410)};
        for(int segment=0;segment<ascent.Length-1;segment++)for(int step=0;step<=30;step++)
            Surface(layout.Point(2,Vector3.Lerp(ascent[segment],ascent[segment+1],step/30f)),"climb segment "+segment);
        for(int z=0;z<=230;z+=2) {
            float height=0;
            for(int dune=0;dune<3;dune++) {
                float start=35+dune*60;
                if(z>=start && z<=start+20)height=(z-start)/20*(2+dune);
                if(z>start+20 && z<=start+40)height=(start+40-z)/20*(2+dune);
            }
            Surface(layout.Point(4,new Vector3(1000,height,z)),"dirt trail "+z);
        }
        for(int i=0;i<MissionCatalog.Count;i++)foreach(int tier in new[]{1,100}) {
            var d=layout.Place(MissionCatalog.Create(i,tier));
            foreach(var o in d.Objectives)if(o.kind==ObjectiveKind.ReachPoint || o.kind==ObjectiveKind.PickupPackage || o.kind==ObjectiveKind.DeliverPackage || o.kind==ObjectiveKind.CanyonJump) {
                Surface(o.point,d.Id+" tier "+tier);
                if(o.kind==ObjectiveKind.CanyonJump)Surface(o.landingPoint,"canyon landing");
            }
        }
        for(int i=0;i<FreestyleCatalog.Count;i++) {
            var d=layout.Place(FreestyleCatalog.Create(i,1));
            foreach(var o in d.Objectives)if(o.kind==ObjectiveKind.ReachPoint)Surface(o.point,d.Title);
        }
        if(failures>0){Debug.LogError("AUTHORED_LEVEL_VALIDATION_FAILED "+failures+" / "+checks);EditorApplication.Exit(1);return;}
        // Imported traffic uses a compute shader unavailable to the headless graphics device.
        if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)
            foreach(var traffic in Object.FindObjectsByType<ITHappy.TrafficManager>(FindObjectsSortMode.None))traffic.gameObject.SetActive(false);
        Application.logMessageReceived+=(message,trace,type)=>{if(type==LogType.Error || type==LogType.Exception)runtimeErrors++;};
        hud=Object.FindFirstObjectByType<GameplayHud>();
        hud.SettingsSaveKey=hud.AdvancedSaveKey=hud.EconomySaveKey=hud.GarageSaveKey=hud.MissionSaveKey="";
        EditorSettings.enterPlayModeOptionsEnabled=true;
        EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload|EnterPlayModeOptions.DisableSceneReload;
        EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode)EditorApplication.update+=Tick;};
        EditorApplication.EnterPlaymode();
    }
    static void Tick() {
        if(!EditorApplication.isPlaying || ++frames<60)return;frames=0;
        try {
            Check(layout.GetComponentsInChildren<Collider>(true).Length==geometryCount,"Play mode retains authored geometry");
            Check(GameObject.Find("Cyclomania riding areas")==null,"no runtime world builder");
            if(stage<5) {
                Check(hud.World.Travel(stage),"travel succeeds "+stage);
                Check(Vector3.Distance(hud.bicycle.transform.position,layout.arrivals[stage].position)<.3f,"travel uses scene arrival "+stage);
                stage++;return;
            }
            if(stage==5) {
                Check(hud.Missions.Begin(MissionCatalog.DeliveryIndex),"begin delivery");
                Check(hud.Missions.Attempt.Objective.point==layout.Point(3,new Vector3(850,0,-220)),"mission target uses map frame");
                hud.Missions.Abandon();
                typeof(GameplayHud).GetMethod("BeginSchool",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(hud,new object[]{0});
                Check(hud.School.Active,"school begins using saved course");
                Check(layout.GetComponentsInChildren<Collider>(true).Length==geometryCount,"school does not construct geometry");
                stage++;return;
            }
            Check(!RideWorld.OutsidePlayableArea(hud.bicycle.transform.position),"school stays inside map recovery bounds");
            Check(runtimeErrors==0,"no runtime errors during travel, mission start, and school");
            Debug.Log((failures==0?"AUTHORED_LEVEL_VALIDATION_SUCCESS ":"AUTHORED_LEVEL_VALIDATION_FAILED ")+checks);
            EditorApplication.Exit(failures==0?0:1);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}

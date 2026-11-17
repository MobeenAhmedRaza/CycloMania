using System;
using System.Collections.Generic;
using System.Reflection;
using Cyclomania;
using SBPScripts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
public static class LocationSceneValidation
{
    static GameplayHud hud;static int count,frames,errors;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;}
    static void Invoke(object target,string method)=>target.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,null);
    static void Surface(Vector3 point,string name){Check(Physics.Raycast(point+Vector3.up*2,Vector3.down,out var hit,4,~0,QueryTriggerInteraction.Ignore) && Mathf.Abs(hit.point.y-point.y)<.4f,name+" has supporting terrain at "+point);}
    public static void Run()
    {
        try{
            EditorSceneManager.OpenScene("Assets/Scenes/gameplay.unity");Physics.SyncTransforms();var layout=UnityEngine.Object.FindFirstObjectByType<RideLevelLayout>();
            var go=new GameObject("Location validation");var locations=go.AddComponent<MissionLocations>();locations.Initialize();var seen=new HashSet<Vector3>();
            for(int mission=0;mission<MissionCatalog.Count;mission++)for(int level=1;level<=100;level++){
                var d=MissionCatalog.Create(mission,level);Check(locations.Place(d,mission,layout),"placement available "+mission+"/"+level);Check(new MissionSession().Start(d),"placed mission valid");
                Check(seen.Add(d.spawn),"mission/level spawn is unique "+mission+"/"+level);Check(layout.playableBounds.Contains(d.spawn),"spawn stays inside map");Surface(d.spawn,"spawn "+mission+"/"+level);
                foreach(var o in d.Objectives)if(o.kind==ObjectiveKind.ReachPoint || o.kind==ObjectiveKind.PickupPackage || o.kind==ObjectiveKind.DeliverPackage || o.kind==ObjectiveKind.CanyonJump){Surface(o.point,"objective "+mission+"/"+level);if(o.kind==ObjectiveKind.CanyonJump)Surface(o.landingPoint,"canyon landing");}
                if(level==1 || level==100){var again=MissionCatalog.Create(mission,level);locations.Place(again,mission,layout);Check(again.spawn==d.spawn,"retry retains same location");}
            }
            UnityEngine.Object.DestroyImmediate(go);Debug.Log("LOCATION_PLACEMENT_SUCCESS "+count);
            foreach(var component in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))if(component.GetType().FullName=="ITHappy.TrafficManager")component.gameObject.SetActive(false);
            hud=UnityEngine.Object.FindFirstObjectByType<GameplayHud>();hud.SettingsSaveKey=hud.GarageSaveKey=hud.EconomySaveKey=hud.AdvancedSaveKey=hud.MissionSaveKey="";
            EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload|EnterPlayModeOptions.DisableSceneReload;
            EditorApplication.playModeStateChanged+=state=>{if(state!=PlayModeStateChange.EnteredPlayMode)return;Application.logMessageReceived+=(m,t,type)=>{if(type==LogType.Exception || type==LogType.Error || type==LogType.Assert)errors++;};EditorApplication.update+=Tick;};EditorApplication.EnterPlaymode();
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying || ++frames<60)return;EditorApplication.update-=Tick;
        try{
            var bike=hud.bicycle;var hb=GameObject.Find("Handbrake").GetComponent<MobileButtonHandler>();Check(bike.mobileControls.handbrake==hb,"scene bicycle uses dedicated handbrake");
            var view=UnityEngine.Object.FindFirstObjectByType<BicycleCamera>();var lens=view.GetComponent<Camera>();Check(!lens.useOcclusionCulling && lens.rect==new Rect(0,0,1,1),"map camera uses the full viewport without old baked visibility");
            hb.OnPointerDown(new PointerEventData(EventSystem.current){pointerId=77,button=PointerEventData.InputButton.Left});Invoke(bike,"ApplyCustomInput");Check(bike.HandbrakeHeld,"actual touch button applies handbrake");
            hud.Pause();Check(!bike.HandbrakeHeld && !hb.IsPressed,"pause releases handbrake");hud.Resume();
            for(int i=0;i<MissionCatalog.Count;i++){
                Check(hud.Missions.Begin(i),"scene mission starts "+i);var first=hdSpawn();Check(Vector3.Distance(bike.transform.position,first)<.3f,"mission respawns at selected location "+i);Invoke(hud.Missions,"Update");
                Check(Vector3.Distance(view.transform.position,bike.transform.position+Vector3.up*view.lookAtHeight)>1,"mission-start camera keeps a usable viewing distance "+i);hud.Missions.Abandon();
                Check(hud.Missions.Begin(i) && hdSpawn()==first,"retry uses same scene route "+i);hud.Missions.Abandon();hud.Missions.Career.completed[i]=1;
                Check(hud.Missions.Begin(i) && hdSpawn()!=first,"next level uses different scene location "+i);hud.Missions.Abandon();
            }
            Check(errors==0,"no runtime errors from handbrake or relocated missions");Debug.Log("LOCATION_SCENE_VALIDATION_SUCCESS "+count);EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static Vector3 hdSpawn()=>hud.Missions.Attempt.Definition.spawn;
}

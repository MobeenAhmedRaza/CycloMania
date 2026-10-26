using System;
using System.Reflection;
using Cyclomania;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class WorldSceneValidation
{
    static GameplayHud hud;
    static int frame,index,errors,passed;
    static void Check(bool valid,string name) { if(!valid)throw new Exception(name);passed++;Debug.Log("PASS: "+name); }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/gameplay.unity");
        hud=UnityEngine.Object.FindFirstObjectByType<GameplayHud>();hud.SettingsSaveKey="";hud.AdvancedSaveKey="";hud.EconomySaveKey="";hud.GarageSaveKey="";hud.MissionSaveKey="";
        EditorSettings.enterPlayModeOptionsEnabled=true;
        EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload|EnterPlayModeOptions.DisableSceneReload;
        EditorApplication.playModeStateChanged+=state=> {
            if(state!=PlayModeStateChange.EnteredPlayMode)return;
            Application.logMessageReceived+=(m,t,type)=> {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;};
            EditorApplication.update+=Tick;
        };
        EditorApplication.EnterPlaymode();
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying)return;
        try {
            if(++frame<45)return;frame=0;
            Check(errors==0,"no runtime errors in full city for destination "+index);
            if(index==0) {
                Check(hud.World!=null && RideWorld.Destinations.Length==5,"gameplay scene enables all five riding areas");
                Check(RideWorld.OutsidePlayableArea(new Vector3(2000,0,0)) && RideWorld.OutsidePlayableArea(new Vector3(float.NaN,0,0)) && !RideWorld.OutsidePlayableArea(new Vector3(1000,4,200)),"world bounds reject invalid positions and retain trails");
                Check(!hud.World.Travel(-1) && !hud.World.Travel(5),"invalid destinations cannot teleport rider");
            }
            if(index<5) {
                hud.Pause();hud.ShowWorld();
                var page=(Transform)typeof(GameplayHud).GetField("page",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hud);
                float distance=hud.Session.DistanceMeters;int score=hud.bicycle.GetComponent<BicycleStunts>().Scoring.Score;
                page.Find("Travel"+index).GetComponent<Button>().onClick.Invoke();
                var location=RideWorld.Destinations[index];var recovery=hud.bicycle.GetComponent<CycleRecovery>();
                Check(!hud.IsPaused && Vector3.Distance(hud.bicycle.transform.position,location.Position)<.3f,"world travel button reaches "+location.Name);
                Check(hud.Session.DistanceMeters==distance && hud.bicycle.GetComponent<BicycleStunts>().Scoring.Score==score,"travel preserves trip and banked score "+index);
                Check(recovery.CheckpointPosition==location.Position,"travel updates safe recovery point "+index);
                // Every arrival must have a solid surface and no solid obstacle in the rider footprint.
                Check(Physics.Raycast(location.Position+Vector3.up*.1f,Vector3.down,out var hit,1) && hit.normal.y>.9f,"arrival has flat ground "+index);
                foreach(var collider in Physics.OverlapBox(location.Position+Vector3.up*1.3f,new Vector3(.6f,.9f,.6f),Quaternion.identity))
                    Check(collider.isTrigger || collider.transform.IsChildOf(hud.bicycle.transform),"arrival footprint clear "+index+" "+collider.name);
                index++;return;
            }
            if(index==6) {
                Check(hud.Garage.selected==1 && hud.equippedBicycleName=="BMX","garage equips another bicycle after world travel (selected="+hud.Garage.selected+", name="+hud.equippedBicycleName+")");
                Check(hud.bicycle.GetComponent<CycleRecovery>().CheckpointPosition==RideWorld.Destinations[4].Position,"garage swap retains destination checkpoint");
                Check(Vector3.Distance(hud.bicycle.transform.position,RideWorld.Destinations[4].Position)<1,"garage swap stays in visited destination");
                Debug.Log("WORLD_SCENE_VALIDATION_SUCCESS "+passed);EditorApplication.Exit(0);return;
            }
            // Actual ramp heights and normal, plus the high bridge surface.
            Check(Physics.Raycast(new Vector3(625,10,-134),Vector3.down,out var ramp,20) && Mathf.Abs(ramp.point.y-1.25f)<.1f && ramp.normal.y>.8f,"takeoff mesh provides sloped collision");
            Check(Physics.Raycast(new Vector3(760,30,250),Vector3.down,out var bridge,40) && Mathf.Abs(bridge.point.y-24)<.1f,"mountain bridge is rideable at summit height");
            var last=hud.bicycle.GetComponent<CycleRecovery>();hud.bicycle.transform.position=new Vector3(2000,-50,0);
            typeof(RideWorld).GetField("nextCheck",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(hud.World,0f);
            typeof(RideWorld).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(hud.World,null);
            Check(Vector3.Distance(hud.bicycle.transform.position,RideWorld.Destinations[4].Position)<.3f,"out-of-world recovery returns to last destination");
            Check(!last.SetCheckpoint(new Vector3(float.NaN,0,0),0),"invalid checkpoint rejected");
            Check(hud.garagePrefabs!=null && hud.garagePrefabs.Length==5,"scene garage catalog serialized");
            for(int i=0;i<5;i++) Check(hud.garagePrefabs[i]!=null,"scene bicycle prefab reference "+i);
            hud.Garage.Earn(5000);hud.ShowGarage();
            var garage=(Transform)typeof(GameplayHud).GetField("page",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hud);
            garage.Find("Next").GetComponent<Button>().onClick.Invoke();
            garage=(Transform)typeof(GameplayHud).GetField("page",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hud);
            garage.Find("Equip").GetComponent<Button>().onClick.Invoke();index=6;
        } catch(Exception e) {Debug.LogException(e);EditorApplication.Exit(1);}
    }
}

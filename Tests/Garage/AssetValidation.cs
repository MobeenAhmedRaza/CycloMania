using System;
using System.Reflection;
using Cyclomania;
using SBPScripts;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class AssetValidation
{
    static GameplayHud hud;
    static int frame, index, errors;
    static string[] names={"Biycycle Standard","Bmx","Mountain Bike MTBRider2","TrialBikeMTBRider2","Fatbike"};
    static Transform Page => (Transform)typeof(GameplayHud).GetField("page",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hud);
    public static void Run()
    {
        EditorSettings.enterPlayModeOptionsEnabled=true;
        EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload|EnterPlayModeOptions.DisableSceneReload;
        EditorApplication.playModeStateChanged+=state=> {
            if(state!=PlayModeStateChange.EnteredPlayMode) return;
            Application.logMessageReceived+=(message,trace,type)=> { if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert) errors++; };
            var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.transform.position=new Vector3(0,-.5f,0);ground.transform.localScale=new Vector3(100,1,100);
            var prefabs=new GameObject[5];for(int i=0;i<5;i++) prefabs[i]=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Simple Bicycle Physics/Prefabs/"+names[i]+".prefab");
            var bike=UnityEngine.Object.Instantiate(prefabs[0]).GetComponent<BicycleController>();bike.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            bike.gameObject.AddComponent<CycleRecovery>();bike.gameObject.AddComponent<BicycleStunts>();
            var camera=new GameObject("Ride camera",typeof(Camera),typeof(BicycleCamera));camera.GetComponent<BicycleCamera>().target=bike.transform;
            hud=new GameObject("HUD",typeof(RectTransform),typeof(Canvas)).AddComponent<GameplayHud>();hud.SettingsSaveKey="";hud.AdvancedSaveKey="";hud.EconomySaveKey="";hud.GarageSaveKey="";hud.bicycle=bike;hud.garagePrefabs=prefabs;
            EditorApplication.update+=Tick;
        };
        EditorApplication.EnterPlaymode();
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying) return;
        try
        {
            frame++;
            if(frame==30)
            {
                if(errors>0) throw new Exception("Runtime errors while initializing "+names[index]);
                if(hud.bicycle.rb==null || hud.bicycle.GetComponent<MeshRenderer>()==null) throw new Exception("Missing bicycle rig/frame");
                hud.Garage.Earn(10000);hud.ShowGarage();
                Page.Find("Paint").GetComponent<Button>().onClick.Invoke();
                Page.Find("Upgrade0").GetComponent<Button>().onClick.Invoke();
                hud.Resume();hud.bicycle.rawCustomAccelerationAxis=hud.bicycle.customAccelerationAxis=1;
            }
            if(frame<70) return;
            if(errors>0) throw new Exception("Runtime errors riding "+names[index]);
            hud.ResetCurrentBicycle();
            if(UnityEngine.Object.FindFirstObjectByType<BicycleCamera>().target!=hud.bicycle.transform) throw new Exception("Camera lost equipped target");
            Debug.Log("PASS: real prefab equip/initialize/paint/upgrade/ride/reset "+names[index]);
            index++;
            if(index==5) { Debug.Log("GARAGE_ASSET_VALIDATION_SUCCESS 5");EditorApplication.Exit(0);return; }
            hud.ShowGarage();Page.Find("Next").GetComponent<Button>().onClick.Invoke();Page.Find("Equip").GetComponent<Button>().onClick.Invoke();
            frame=0;
        }
        catch(Exception exception) {Debug.LogException(exception);EditorApplication.Exit(1);}
    }
}

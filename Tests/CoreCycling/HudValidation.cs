using System;
using System.Reflection;
using Cyclomania;
using SBPScripts;
using UnityEngine;
using UnityEngine.UI;

public static class HudValidation
{
    static int count;
    static void Check(bool pass,string name) { if(!pass) throw new Exception(name); count++; Debug.Log("PASS: "+name); }
    static void Invoke(object target,string method) => target.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(target,null);
    public static void Run(BicycleController bicycle)
    {
        var session=new RideSession();
        session.RecordTravel(new Vector3(3,20,4),2,true);
        Check(Mathf.Approximately(session.DistanceMeters,10),"distance measures horizontal travel");
        session.RecordTravel(Vector3.one,10,false);
        session.RecordTravel(Vector3.one,float.NaN,true);
        Check(Mathf.Approximately(session.DistanceMeters,10),"paused/crashed/invalid travel does not add distance");
        session.SetCurrency(-10,15); session.SetObjective("Delivery","Reach the marker",2);
        Check(session.Coins==0 && session.Gems==15 && session.ObjectiveProgress==1,"HUD balances and objective progress clamp safely");
        session.Restart();
        Check(session.DistanceMeters==0 && session.Gems==15 && session.ObjectiveProgress==-1,"restart clears trip without spending currency");

        var root=new GameObject("HUD test",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var controls=new GameObject("Touch controls test");
        var hud=root.AddComponent<GameplayHud>(); hud.SettingsSaveKey="";hud.AdvancedSaveKey="";hud.EconomySaveKey=""; hud.bicycle=bicycle; hud.touchControls=controls;
        Invoke(hud,"Start");
        Check(root.transform.Find("Gameplay HUD/Minimap/Map mask/Map image").GetComponent<RawImage>().texture!=null,"minimap texture is wired into HUD");
        Check(root.transform.Find("Gameplay HUD/Speed panel/Speed").GetComponent<Text>().text.Contains("KM/H"),"speed display includes units");
        Time.timeScale=.5f; AudioListener.pause=false;
        root.transform.Find("Gameplay HUD/Pause").GetComponent<Button>().onClick.Invoke();
        Check(hud.IsPaused && Time.timeScale==0 && AudioListener.pause && !controls.activeSelf,"pause button freezes time and audio and hides controls");
        hud.ShowGarage(); Check(hud.CurrentPage=="Garage" && Time.timeScale==0,"garage entry keeps ride paused");
        hud.ShowSettings();
        var slider=root.transform.Find("Pause overlay/Safe content/Settings/SoundFX").GetComponent<Slider>();
        float volume=AudioListener.volume;slider.value=.3f;
        Check(hud.CurrentPage=="Settings" && Mathf.Approximately(hud.Settings.Preferences.effects,.3f),"settings slider adjusts sound effects separately");
        AudioListener.volume=volume;
        hud.Resume();
        Check(!hud.IsPaused && Time.timeScale==.5f && !AudioListener.pause && controls.activeSelf,"resume restores prior time audio and controls");
        controls.SetActive(false);AudioListener.pause=true;hud.Pause();hud.Resume();
        Check(!controls.activeSelf && AudioListener.pause,"resume preserves previously disabled controls and paused audio");
        controls.SetActive(true);AudioListener.pause=false;Time.timeScale=1;
        hud.Session.RecordTravel(Vector3.forward*5,10,true);hud.Session.SetCurrency(10,2);
        hud.Pause();hud.RestartRide();
        Check(!hud.IsPaused && hud.Session.DistanceMeters==0 && hud.Session.Coins==10 && bicycle.rb.linearVelocity==Vector3.zero,"restart resets ride and bicycle while keeping balance");
        typeof(GameplayHud).GetMethod("OnApplicationPause",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(hud,new object[]{true});
        Check(hud.IsPaused,"backgrounding pauses the ride");
        hud.enabled=false;
        Check(Time.timeScale==1 && !AudioListener.pause,"disabling HUD releases pause state");
        UnityEngine.Object.Destroy(root);UnityEngine.Object.Destroy(controls);
        Debug.Log("HUD_VALIDATION_SUCCESS "+count);
    }
}

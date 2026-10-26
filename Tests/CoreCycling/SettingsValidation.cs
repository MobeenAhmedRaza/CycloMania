using System;
using System.Reflection;
using Cyclomania;
using UnityEngine;
public static class SettingsValidation
{
    static int count;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;Debug.Log("PASS: "+message);}
    static void Invoke(object o,string name)=>o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,null);
    public static void Run()
    {
        var p=GamePreferences.FromJson("broken");Check(p.effects==1 && p.music==.35f && p.graphics==1,"corrupt settings recover safe defaults");
        p=GamePreferences.FromJson("{\"version\":1,\"effects\":-2,\"music\":3,\"graphics\":99}");Check(p.effects==0 && p.music==1 && p.graphics==3,"settings values are clamped");
        Check(GamePreferences.FromJson("{\"version\":99}").graphics==1 && GamePreferences.Volume(float.NaN,.35f)==.35f,"unsupported versions and nonfinite audio recover defaults");
        Check(GamePreferences.Resolution(2796,1290,3).x==2560 && GamePreferences.Resolution(1280,720,3)==new Vector2Int(1280,720),"2K caps long edge without upscaling smaller displays");
        Check(GamePreferences.Resolution(1290,2796,0).y==1280,"portrait resolution preserves orientation and budget");
        string key="Cyclomania.Test.Settings."+Guid.NewGuid();var root=new GameObject("Settings test");var service=root.AddComponent<GameSettings>();
        try {
            service.Initialize(key);Check(service.Music.clip!=null && service.Music.loop && service.Music.ignoreListenerPause,"music loop exists and remains available while settings are paused");
            var audioObject=new GameObject("Effects fixture");audioObject.transform.SetParent(root.transform);var fx=audioObject.AddComponent<AudioSource>();fx.volume=.8f;
            var channel=audioObject.AddComponent<SettingsAudioChannel>();channel.Bind(service);
            float musicBefore=service.Music.volume;service.SetEffects(.25f);Check(Mathf.Abs(fx.volume-.2f)<.001f && service.Music.volume==musicBefore,"effects slider scales effects without changing music");
            for(int i=0;i<10;i++){Invoke(channel,"Update");Invoke(channel,"LateUpdate");}Check(Mathf.Abs(fx.volume-.2f)<.001f,"audio scaling does not compound across frames");
            Invoke(channel,"Update");fx.volume=.4f;Invoke(channel,"LateUpdate");Check(Mathf.Abs(fx.volume-.1f)<.001f,"vendor fade volume is respected before effects scaling");
            service.SetEffects(0);Check(fx.volume==0,"effects can mute fully");service.SetEffects(1);Check(Mathf.Abs(fx.volume-.4f)<.001f,"unmuting restores authored effects volume");
            service.SetMusic(0);Check(service.Music.volume==0 && Mathf.Abs(fx.volume-.4f)<.001f,"music mute leaves effects unchanged");service.SetMusic(.6f);
            var second=audioObject.AddComponent<AudioSource>();second.volume=.5f;Invoke(service,"Scan");service.SetEffects(.5f);Check(Mathf.Abs(second.volume-.25f)<.001f,"multiple audio sources on one object are independently routed");
            service.SetGraphics(0);Check(Application.targetFrameRate==30 && QualitySettings.shadows==ShadowQuality.Disable && QualitySettings.antiAliasing==0,"Low preset applies lighter graphics and 30 FPS target");
            service.SetGraphics(2);Check(Application.targetFrameRate==60 && QualitySettings.antiAliasing==4 && QualitySettings.shadows==ShadowQuality.All,"High preset applies 60 FPS target and enhanced graphics");
            service.Flush();p=GamePreferences.FromJson(PlayerPrefs.GetString(key));Check(p.effects==.5f && p.music==.6f && p.graphics==2,"settings save round trip preserves independent volumes and quality");
            var reloadRoot=new GameObject("Reload settings");var reload=reloadRoot.AddComponent<GameSettings>();reload.Initialize(key);
            Check(reload.Preferences.effects==.5f && reload.Preferences.music==.6f && reload.Preferences.graphics==2,"new settings service loads saved values on initialization");UnityEngine.Object.DestroyImmediate(reloadRoot);
            service.SetMusic(.4f);service.GetType().GetMethod("OnApplicationPause",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(service,new object[]{true});
            Check(GamePreferences.FromJson(PlayerPrefs.GetString(key)).music==.4f && !service.Music.isPlaying,"background pause flushes pending settings and pauses music");
            service.GetType().GetMethod("OnApplicationFocus",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(service,new object[]{true});Check(!service.Music.isPlaying,"focus alone cannot resume music while app remains paused");
            service.GetType().GetMethod("OnApplicationPause",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(service,new object[]{false});
            channel.enabled=false;Check(Mathf.Abs(fx.volume-.4f)<.001f,"removing audio routing restores authored volume");
        } finally {UnityEngine.Object.DestroyImmediate(root);PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();}
        Debug.Log("SETTINGS_VALIDATION_SUCCESS "+count);
    }
}

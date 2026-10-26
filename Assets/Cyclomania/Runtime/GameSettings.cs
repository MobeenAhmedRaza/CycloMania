using System;
using UnityEngine;
namespace Cyclomania
{
    public sealed class GameSettings:MonoBehaviour
    {
        public GamePreferences Preferences {get;private set;}=new GamePreferences();
        public string SaveNotice {get;private set;}="";
        public AudioSource Music {get;private set;}
        private string key;
        private bool dirty,landscape,appPaused;
        private bool hasFocus=true;
        private float saveAt,scanAt;
        private int nativeLong,nativeShort;
        private AudioClip generatedMusic;
        public void Initialize(string saveKey,AudioClip musicClip=null)
        {
            key=saveKey;Preferences=GamePreferences.FromJson(string.IsNullOrEmpty(key)?"":PlayerPrefs.GetString(key,""));
            nativeLong=Mathf.Max(Mathf.Max(Screen.width,Screen.height),Mathf.Max(Screen.currentResolution.width,Screen.currentResolution.height));
            nativeShort=Mathf.Max(Mathf.Min(Screen.width,Screen.height),Mathf.Min(Screen.currentResolution.width,Screen.currentResolution.height));AudioListener.volume=1;
            ApplyGraphics();
            var go=new GameObject("Cyclomania background music");go.transform.SetParent(transform,false);Music=go.AddComponent<AudioSource>();
            Music.loop=true;Music.playOnAwake=false;Music.spatialBlend=0;Music.volume=.12f;Music.ignoreListenerPause=true;
            if(musicClip==null) {
                const int rate=22050;var samples=new float[rate*8];
                for(int i=0;i<samples.Length;i++) {float t=(float)i/rate;float envelope=.5f-.5f*Mathf.Cos(2*Mathf.PI*t/8);
                    samples[i]=envelope*(Mathf.Sin(2*Mathf.PI*130.875f*t)+.5f*Mathf.Sin(2*Mathf.PI*164.875f*t)+.35f*Mathf.Sin(2*Mathf.PI*196*t))*.12f;}
                generatedMusic=AudioClip.Create("Cyclomania ambient loop",samples.Length,1,rate,false);generatedMusic.SetData(samples,0);musicClip=generatedMusic;
            }
            Music.clip=musicClip;Music.gameObject.AddComponent<SettingsAudioChannel>().Bind(this,true);Scan();Music.Play();
        }
        private void Scan()
        {
            foreach(var source in FindObjectsByType<AudioSource>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                {
                    SettingsAudioChannel match=null;
                    foreach(var channel in source.GetComponents<SettingsAudioChannel>())if(channel.Source==source){match=channel;break;}
                    if(match==null)source.gameObject.AddComponent<SettingsAudioChannel>().Bind(this,false,source);
                    else if(match.Owner==null)match.Bind(this,match.music,source);
                }
        }
        public void SetEffects(float value){Preferences.effects=GamePreferences.Volume(value,1);Changed();}
        public void SetMusic(float value){Preferences.music=GamePreferences.Volume(value,.35f);Changed();}
        public void SetGraphics(int value){Preferences.graphics=Mathf.Clamp(value,0,3);ApplyGraphics();Changed();}
        private void Changed()
        {
            foreach(var channel in FindObjectsByType<SettingsAudioChannel>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(channel.Owner==this)channel.Apply();
            dirty=true;saveAt=Time.unscaledTime+.4f;
        }
        private void Update()
        {
            if(dirty && Time.unscaledTime>=saveAt)Flush();
            if(Time.unscaledTime>=scanAt){scanAt=Time.unscaledTime+1;Scan();}
            if(landscape!=(Screen.width>=Screen.height))ApplyResolution();
        }
        public void Flush()
        {
            if(!dirty)return;dirty=false;if(string.IsNullOrEmpty(key))return;
            try {PlayerPrefs.SetString(key,JsonUtility.ToJson(Preferences));PlayerPrefs.Save();SaveNotice="";}
            catch(Exception e){SaveNotice="Settings could not be saved. Change a setting to retry.";Debug.LogWarning("Settings save failed: "+e.Message);}
        }
        private void ApplyGraphics()
        {
            int preset=Preferences.graphics;
            int level=Mathf.Min(new[]{0,2,4,5}[preset],QualitySettings.names.Length-1);
            if(level>=0)QualitySettings.SetQualityLevel(level,true);
            QualitySettings.vSyncCount=0;Application.targetFrameRate=preset==0?30:60;
            QualitySettings.shadows=preset==0?ShadowQuality.Disable:preset==1?ShadowQuality.HardOnly:ShadowQuality.All;
            QualitySettings.shadowResolution=preset<2?ShadowResolution.Low:preset==2?ShadowResolution.Medium:ShadowResolution.High;
            QualitySettings.shadowDistance=new[]{0f,40f,75f,100f}[preset];QualitySettings.antiAliasing=preset==0?0:preset==1?2:4;
            QualitySettings.globalTextureMipmapLimit=preset==0?1:0;QualitySettings.lodBias=new[]{.6f,1f,1.5f,2f}[preset];
            ApplyResolution();
        }
        private void ApplyResolution()
        {
            landscape=Screen.width>=Screen.height;
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            var size=GamePreferences.Resolution(landscape?nativeLong:nativeShort,landscape?nativeShort:nativeLong,Preferences.graphics);
            Screen.SetResolution(size.x,size.y,FullScreenMode.FullScreenWindow);
#endif
        }
        private void OnApplicationPause(bool paused){appPaused=paused;if(paused)Flush();RefreshMusicPlayback();}
        private void OnApplicationFocus(bool focused){hasFocus=focused;if(!focused)Flush();RefreshMusicPlayback();}
        private void RefreshMusicPlayback(){if(Music==null)return;if(appPaused || !hasFocus)Music.Pause();else Music.UnPause();}
        private void OnApplicationQuit(){Flush();}
        private void OnDisable(){Flush();}
        private void OnDestroy(){Flush();foreach(var channel in FindObjectsByType<SettingsAudioChannel>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(channel.Owner==this){channel.enabled=false;Destroy(channel);}if(Music!=null)Destroy(Music.gameObject);if(generatedMusic!=null)Destroy(generatedMusic);}
    }
}

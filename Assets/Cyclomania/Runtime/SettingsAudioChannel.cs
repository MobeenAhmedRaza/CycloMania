using UnityEngine;
namespace Cyclomania
{
    // Restore authored volume before vendor audio scripts calculate fades, then scale once.
    [DefaultExecutionOrder(-10000),RequireComponent(typeof(AudioSource))]
    public sealed class SettingsAudioChannel:MonoBehaviour
    {
        public bool music;
        private AudioSource source;
        private GameSettings owner;
        public AudioSource Source=>source;
        public GameSettings Owner=>owner;
        private float raw,applied;
        private bool scaled;
        public void Bind(GameSettings settings,bool isMusic=false,AudioSource target=null){source=target!=null?target:GetComponent<AudioSource>();owner=settings;music=isMusic;raw=source.volume;scaled=false;Apply();}
        private void Update(){Restore();}
        private void LateUpdate(){Apply();}
        private void Restore(){if(source==null || !scaled)return;if(Mathf.Approximately(source.volume,applied))source.volume=raw;scaled=false;}
        public void Apply()
        {
            if(source==null || owner==null)return;
            Restore();raw=source.volume;applied=raw*(music?owner.Preferences.music:owner.Preferences.effects);source.volume=applied;scaled=true;
        }
        private void OnDisable(){Restore();}
        private void OnDestroy(){Restore();}
    }
}

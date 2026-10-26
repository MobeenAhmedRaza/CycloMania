using System;
using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using UnityEngine.Apple.ReplayKit;
#endif
namespace Cyclomania
{
    public sealed class RideReplay:MonoBehaviour
    {
        public string Notice {get;private set;}="Record a ride, then preview and share the video.";
        public bool Supported {
            get {
#if UNITY_IOS && !UNITY_EDITOR
                return ReplayKit.APIAvailable;
#else
                return false;
#endif
            }
        }
        public bool Recording {get {
#if UNITY_IOS && !UNITY_EDITOR
            return ReplayKit.isRecording;
#else
            return false;
#endif
        }}
        public bool Available {get {
#if UNITY_IOS && !UNITY_EDITOR
            return ReplayKit.recordingAvailable;
#else
            return false;
#endif
        }}
        public void StartRecording()
        {
#if UNITY_IOS && !UNITY_EDITOR
            try{Notice=ReplayKit.StartRecording(false,false)?"Recording. Resume your ride; stop in Advanced.":"Recording could not start: "+ReplayKit.lastError;}catch(Exception e){Notice=e.Message;}
#else
            Notice="Video recording and sharing require a supported iPhone or iPad.";
#endif
        }
        public void StopRecording()
        {
#if UNITY_IOS && !UNITY_EDITOR
            try{Notice=ReplayKit.StopRecording()?"Clip ready. Preview to save or share.":"Unable to stop recording: "+ReplayKit.lastError;}catch(Exception e){Notice=e.Message;}
#endif
        }
        public void Preview()
        {
#if UNITY_IOS && !UNITY_EDITOR
            try{if(!ReplayKit.Preview())Notice="Preview unavailable: "+ReplayKit.lastError;}catch(Exception e){Notice=e.Message;}
#endif
        }
        public void Discard()
        {
#if UNITY_IOS && !UNITY_EDITOR
            if(!Recording)ReplayKit.Discard();
#endif
            Notice="Clip discarded.";
        }
        void OnApplicationPause(bool paused){if(paused && Recording)StopRecording();}
        void OnDestroy(){if(Recording)StopRecording();}
    }
}

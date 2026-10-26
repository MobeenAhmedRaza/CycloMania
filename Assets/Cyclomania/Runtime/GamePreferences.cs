using System;
using UnityEngine;
namespace Cyclomania
{
    [Serializable] public sealed class GamePreferences
    {
        public int version=1, graphics=1;
        public float effects=1, music=.35f;
        public static GamePreferences FromJson(string json)
        {
            GamePreferences p;
            try {p=string.IsNullOrEmpty(json)?new GamePreferences():JsonUtility.FromJson<GamePreferences>(json);}
            catch(ArgumentException){p=new GamePreferences();}
            if(p==null || p.version!=1)p=new GamePreferences();
            p.effects=Volume(p.effects,1);p.music=Volume(p.music,.35f);p.graphics=Mathf.Clamp(p.graphics,0,3);return p;
        }
        public static float Volume(float value,float fallback)=>float.IsNaN(value)||float.IsInfinity(value)?fallback:Mathf.Clamp01(value);
        public static Vector2Int Resolution(int width,int height,int preset)
        {
            int limit=new[]{1280,1600,1920,2560}[Mathf.Clamp(preset,0,3)];
            float scale=Mathf.Min(1,(float)limit/Mathf.Max(1,Mathf.Max(width,height)));
            return new Vector2Int(Mathf.Max(1,Mathf.RoundToInt(width*scale)),Mathf.Max(1,Mathf.RoundToInt(height*scale)));
        }
    }
}

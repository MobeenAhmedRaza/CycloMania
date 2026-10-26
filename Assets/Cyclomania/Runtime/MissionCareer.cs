using System;
using UnityEngine;
namespace Cyclomania
{
    [Serializable]
    public sealed class MissionCareer
    {
        public int version=1, points;
        public int[] completed=new int[MissionCatalog.Count];
        public float[] bestSeconds=new float[MissionCatalog.Count];
        public int NextLevel(int index) => Mathf.Min(100,completed[index]+1);
        // Each tier earns its reward once. Replaying the final tier cannot farm rewards.
        public bool Record(int index,int level,float seconds,int reward)
        {
            if(index<0 || index>=completed.Length || level!=NextLevel(index) || completed[index]>=100 || seconds<0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return false;
            completed[index]=level;
            if(bestSeconds[index]<=0 || seconds<bestSeconds[index]) bestSeconds[index]=seconds;
            points=(int)Math.Min(int.MaxValue,(long)points+Math.Max(0,reward));return true;
        }
        public string ToJson()=>JsonUtility.ToJson(this);
        public static MissionCareer FromJson(string json,int count=MissionCatalog.Count)
        {
            MissionCareer data;
            try { data=string.IsNullOrEmpty(json)?new MissionCareer():JsonUtility.FromJson<MissionCareer>(json); }
            catch(ArgumentException) { data=new MissionCareer(); }
            if(data==null || data.version!=1) data=new MissionCareer();
            var levels=new int[count];var best=new float[count];
            for(int i=0;i<levels.Length;i++) {
                if(data.completed!=null && i<data.completed.Length) levels[i]=Mathf.Clamp(data.completed[i],0,100);
                if(data.bestSeconds!=null && i<data.bestSeconds.Length && !float.IsNaN(data.bestSeconds[i]) && !float.IsInfinity(data.bestSeconds[i])) best[i]=Mathf.Max(0,data.bestSeconds[i]);
            }
            data.completed=levels;data.bestSeconds=best;data.points=Math.Max(0,data.points);return data;
        }
    }
}

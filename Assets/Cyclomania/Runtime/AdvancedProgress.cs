using System;
using UnityEngine;
namespace Cyclomania
{
    [Serializable] public sealed class AdvancedProgress
    {
        public int version=1, weather, timePreset, schoolCompleted;
        public bool firstPerson, cycleTime;
        public int[] skills=new int[3];
        public int Available(int careerTiers) => Mathf.Max(0,schoolCompleted+Mathf.Max(0,careerTiers)/5-skills[0]-skills[1]-skills[2]);
        public bool Learn(int index,int careerTiers)
        {
            if(index<0 || index>=3 || skills[index]>=3 || Available(careerTiers)==0 || (index>0 && skills[index-1]==0))return false;
            skills[index]++;return true;
        }
        public string ToJson()=>JsonUtility.ToJson(this);
        public static AdvancedProgress FromJson(string json)
        {
            AdvancedProgress p=null;try{if(!string.IsNullOrEmpty(json))p=JsonUtility.FromJson<AdvancedProgress>(json);}catch(ArgumentException){}
            if(p==null || p.version!=1)p=new AdvancedProgress();
            p.weather=Mathf.Clamp(p.weather,0,3);p.timePreset=Mathf.Clamp(p.timePreset,0,3);p.schoolCompleted=Mathf.Clamp(p.schoolCompleted,0,3);
            var a=new int[3];if(p.skills!=null)for(int i=0;i<Math.Min(3,p.skills.Length);i++)a[i]=Mathf.Clamp(p.skills[i],0,3);p.skills=a;
            if(a[0]==0)a[1]=a[2]=0;else if(a[1]==0)a[2]=0;return p;
        }
    }
    public sealed class SchoolLesson
    {
        public int Lesson {get;private set;}
        public bool Active {get;private set;}
        public bool Passed {get;private set;}
        public string Notice {get;private set;}="";
        public float StopTime {get;private set;}
        private Vector3 previous;private bool sampled,stopped;
        public void Begin(int lesson){Lesson=Mathf.Clamp(lesson,0,2);Active=true;Passed=false;StopTime=0;sampled=stopped=false;Notice=Lesson==0?"Keep right; stay under 20 KM/H. Reach the finish.":Lesson==1?"Stop inside the red box for 2 seconds, then continue.":"Obey the signal: stop behind the line until green, then finish.";}
        public void End(string reason){Active=false;Notice=reason;}
        public void Tick(Vector3 local,float kmh,float dt,float elapsed,bool crashed,bool paused)
        {
            if(!Active || paused || dt<=0)return;
            if(crashed){End("Lesson failed: crash/reset. Retry from Advanced.");return;}
            if(!float.IsFinite(local.sqrMagnitude) || !float.IsFinite(kmh))return;
            if(sampled && Vector3.Distance(previous,local)>15){End("Lesson interrupted: moved off course.");return;}previous=local;sampled=true;
            if(local.x<0 || local.x>5 || local.z< -5 || local.z>105 || kmh>21){End("Lesson failed: keep in the right lane and below 20 KM/H.");return;}
            if(Lesson==1 && !stopped){StopTime=local.z>=36 && local.z<=40 && kmh<1?StopTime+dt:0;if(StopTime>=2)stopped=true;if(local.z>40 && !stopped){End("Lesson failed: stop for 2 seconds before the line.");return;}}
            if(Lesson==2 && local.z>40 && !stopped){if(elapsed<10){End("Lesson failed: crossed a red signal.");return;}stopped=true;}
            if(local.z>=95){Active=false;Passed=true;Notice="Lesson passed. Skill point earned on first completion.";}
        }
    }
}

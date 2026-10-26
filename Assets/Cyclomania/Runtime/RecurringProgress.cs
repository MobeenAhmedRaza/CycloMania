using System;
using UnityEngine;
namespace Cyclomania
{
    // All dates are UTC day ordinals. Weeks begin Monday (0001-01-01 was Monday).
    [Serializable] public sealed class RecurringProgress
    {
        public long day=-1,week=-1,season=-1;
        public float[] daily=new float[3],weekly=new float[3];
        public int dailyClaims,weeklyClaims,seasonXP,freeClaims,bonusClaims;
        public float eventProgress;
        public bool eventClaimed,bonusPass;
        public static readonly int[] DailyTargets={1000,500,2},WeeklyTargets={10000,5000,10};
        public static readonly string[] ActivityNames={"Ride metres","Bank stunt points","Complete new mission tiers"};
        public int EventKind=>(int)(Math.Max(0,week)%3);
        public int EventTarget=>new[]{5000,2500,5}[EventKind];
        public int Level=>Math.Min(10,seasonXP/250);
        public bool Refresh(long today)
        {
            if(today<0 || today<day)return false;
            if(today>day){day=today;daily=new float[3];dailyClaims=0;}
            if(today/7>week){week=today/7;weekly=new float[3];weeklyClaims=0;eventProgress=0;eventClaimed=false;}
            if(today/28>season){season=today/28;seasonXP=freeClaims=bonusClaims=0;bonusPass=false;}
            return true;
        }
        public void Add(int kind,float amount,long today)
        {
            if(kind<0 || kind>2 || !float.IsFinite(amount) || amount<=0 || !Refresh(today))return;
            daily[kind]=Mathf.Min(DailyTargets[kind],daily[kind]+amount);weekly[kind]=Mathf.Min(WeeklyTargets[kind],weekly[kind]+amount);
            if(kind==EventKind)eventProgress=Mathf.Min(EventTarget,eventProgress+amount);
        }
        public bool CanClaim(bool weeklyGoal,int index)=>index>=0 && index<3 && (((weeklyGoal?weeklyClaims:dailyClaims)&(1<<index))==0) && (weeklyGoal?weekly[index]>=WeeklyTargets[index]:daily[index]>=DailyTargets[index]);
        public void AddXP(int value){if(value>0)seasonXP=(int)Math.Min(2500,(long)seasonXP+value);}
        public void Normalize()
        {
            day=Math.Max(-1,day);week=Math.Max(-1,week);season=Math.Max(-1,season);
            daily=Clean(daily,DailyTargets);weekly=Clean(weekly,WeeklyTargets);
            dailyClaims&=7;weeklyClaims&=7;freeClaims&=1023;bonusClaims&=1023;seasonXP=Mathf.Clamp(seasonXP,0,2500);
            eventProgress=float.IsFinite(eventProgress)?Mathf.Clamp(eventProgress,0,EventTarget):0;
        }
        static float[] Clean(float[] values,int[] caps){var result=new float[3];if(values!=null)for(int i=0;i<Math.Min(3,values.Length);i++)result[i]=float.IsFinite(values[i])?Mathf.Clamp(values[i],0,caps[i]):0;return result;}
    }
}

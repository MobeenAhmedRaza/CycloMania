using System;
using UnityEngine;

namespace Cyclomania
{
    // Currency and purchased entitlements share one save: a purchase cannot be saved as only a debit.
    [Serializable]
    public sealed class GameEconomy
    {
        public int version=1, coins, gems, streak, stuntRemainder;
        public long lastDay;
        public int[] career=new int[MissionCatalog.Count], freestyle=new int[FreestyleCatalog.Count];
        public int[] bikes=new int[5], drive=new int[5], brakes=new int[5], handling=new int[5];
        public bool neonPaint, sunsetPaint, violetPaint;
        public RecurringProgress recurring=new RecurringProgress();
        public static readonly int[] BikePrices={0,500,1500,3000,5000};
        public static int UpgradePrice(int tier)=>tier==1?300:tier==2?1000:2500;
        public void BankStunt(int score)
        {
            if(score<=0)return;
            long total=(long)score+stuntRemainder;
            Credit((int)(total/10),0);stuntRemainder=(int)(total%10);
        }
        public bool RewardMission(bool free,int index,int level,int reward)
        {
            var levels=free?freestyle:career;
            if(index<0 || index>=levels.Length || level<1 || level>100 || level<=levels[index] || reward<=0)return false;
            levels[index]=level;Credit(reward,level%5==0?2:0);return true;
        }
        void Credit(int c,int g){coins=(int)Math.Min(int.MaxValue,(long)coins+c);gems=(int)Math.Min(int.MaxValue,(long)gems+g);}
        public static long Day(DateTime utc)=>utc.ToUniversalTime().Date.Ticks/TimeSpan.TicksPerDay;
        public bool ClaimDaily(long day)
        {
            if(day<=lastDay || day<recurring.day)return false;
            streak=day==lastDay+1?streak%7+1:1;lastDay=day;
            Credit(100*streak,streak==7?5:0);if(recurring.Refresh(day))recurring.AddXP(25);return true;
        }
        public bool BuyBike(GarageProgress garage,int index)
        {
            if(index<=0 || index>=bikes.Length || garage.IsUnlocked(index) || bikes[index]>0 || coins<BikePrices[index])return false;
            coins-=BikePrices[index];bikes[index]=1;return true;
        }
        public bool BuyUpgrade(GarageProgress garage,int index,int category)
        {
            if(!garage.IsUnlocked(index) || category<0 || category>2)return false;
            var actual=category==0?garage.drive:category==1?garage.brakes:garage.handling;
            var bought=category==0?drive:category==1?brakes:handling;
            int tier=Math.Max(actual[index],bought[index])+1;
            if(tier>3 || coins<UpgradePrice(tier))return false;
            coins-=UpgradePrice(tier);bought[index]=tier;return true;
        }
        public bool BuyPaint(){if(neonPaint || gems<5)return false;gems-=5;neonPaint=true;return true;}
        public bool ClaimChallenge(bool weekly,int index,long day)
        {
            if(!recurring.Refresh(day) || !recurring.CanClaim(weekly,index))return false;
            if(weekly){recurring.weeklyClaims|=1<<index;Credit(1000,2);recurring.AddXP(300);}
            else{recurring.dailyClaims|=1<<index;Credit(200,0);recurring.AddXP(100);}return true;
        }
        public bool ClaimEvent(long day)
        {
            if(!recurring.Refresh(day) || recurring.eventClaimed || recurring.eventProgress<recurring.EventTarget)return false;
            recurring.eventClaimed=true;Credit(1500,3);recurring.AddXP(500);return true;
        }
        public bool BuySeasonPass(long day)
        {
            if(!recurring.Refresh(day) || recurring.bonusPass || gems<25)return false;
            gems-=25;recurring.bonusPass=true;return true;
        }
        public bool ClaimSeason(int tier,bool bonus,long day)
        {
            if(tier<1 || tier>10 || !recurring.Refresh(day) || tier>recurring.Level || (bonus && !recurring.bonusPass))return false;
            int flag=1<<(tier-1);if(((bonus?recurring.bonusClaims:recurring.freeClaims)&flag)!=0)return false;
            if(bonus){recurring.bonusClaims|=flag;Credit(500,1);if(tier==10)violetPaint=true;}
            else{recurring.freeClaims|=flag;Credit(250,tier%5==0?2:0);}return true;
        }
        public bool BuySunsetPaint(){if(sunsetPaint || gems<10)return false;gems-=10;sunsetPaint=true;return true;}
        public bool OwnsPaint(int index)=>index>=0 && (index<5 || index==5 && neonPaint || index==6 && sunsetPaint || index==7 && violetPaint);
        public void Apply(GarageProgress garage)
        {
            for(int i=0;i<5;i++) {garage.purchased[i]=bikes[i]>0;garage.drive[i]=Math.Max(garage.drive[i],drive[i]);garage.brakes[i]=Math.Max(garage.brakes[i],brakes[i]);garage.handling[i]=Math.Max(garage.handling[i],handling[i]);}
        }
        public string ToJson()=>JsonUtility.ToJson(this);
        public static GameEconomy FromJson(string json)
        {
            GameEconomy e=null;
            try {if(!string.IsNullOrEmpty(json))e=JsonUtility.FromJson<GameEconomy>(json);}catch(ArgumentException){}
            if(e==null || e.version!=1)e=new GameEconomy();
            e.coins=Math.Max(0,e.coins);e.gems=Math.Max(0,e.gems);e.streak=Mathf.Clamp(e.streak,0,7);e.lastDay=Math.Max(0,e.lastDay);e.stuntRemainder=Mathf.Clamp(e.stuntRemainder,0,9);
            e.career=Normalize(e.career,MissionCatalog.Count,100);e.freestyle=Normalize(e.freestyle,FreestyleCatalog.Count,100);
            e.bikes=Normalize(e.bikes,5,1);e.drive=Normalize(e.drive,5,3);e.brakes=Normalize(e.brakes,5,3);e.handling=Normalize(e.handling,5,3);if(e.recurring==null)e.recurring=new RecurringProgress();e.recurring.Normalize();return e;
        }
        static int[] Normalize(int[] a,int count,int max){var result=new int[count];if(a!=null)for(int i=0;i<Math.Min(a.Length,count);i++)result[i]=Mathf.Clamp(a[i],0,max);return result;}
    }
}

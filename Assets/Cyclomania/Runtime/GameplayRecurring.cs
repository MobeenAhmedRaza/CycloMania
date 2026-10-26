using System;
using UnityEngine;
namespace Cyclomania
{
    public sealed partial class GameplayHud
    {
        private bool weeklyChallenges;
        private int seasonPage;
        private float recurringDistance,recurringPending,recurringSaveAt;
        private long recurringPendingDay=-1;
        private static long Today=>GameEconomy.Day(DateTime.UtcNow);
        private void TickRecurring()
        {
            float distanceDelta=Session.DistanceMeters-recurringDistance;recurringDistance=Session.DistanceMeters;
            long today=Today;
            if(recurringPendingDay!=today){FlushRecurring();if(recurringPending<=0)recurringPendingDay=today;}
            if(distanceDelta>0 && float.IsFinite(distanceDelta))recurringPending+=distanceDelta;
            if(Time.unscaledTime>=recurringSaveAt){FlushRecurring();recurringSaveAt=Time.unscaledTime+5;}
            if(CurrentPage=="Recurring" || CurrentPage=="Season" || CurrentPage=="Events"){
                if(Economy.recurring.day<today && TransactEconomy(e=>e.recurring.Refresh(today))) {
                    if(CurrentPage=="Season")ShowSeason();else if(CurrentPage=="Events")ShowEvents();else ShowRecurring();
                }
            }
        }
        private void FlushRecurring()
        {
            if(recurringPending<=0)return;
            float amount=recurringPending;long day=recurringPendingDay;
            if(TransactEconomy(e=>{e.recurring.Add(0,amount,day);return true;}))recurringPending=0;
        }
        private void PrepareRecurring(){FlushRecurring();if(Economy.recurring.day<Today)TransactEconomy(e=>e.recurring.Refresh(Today));}
        private string ContentNotice=>string.IsNullOrEmpty(economyNotice)?"Rewards expire at reset. Dates use UTC. Claim completed goals before then.":economyNotice;
        private static string DateLabel(long day){try{return new DateTime(day*TimeSpan.TicksPerDay,DateTimeKind.Utc).ToString("MMM d");}catch(ArgumentOutOfRangeException){return "next period";}}
        private void ShowRecurring()
        {
            if(!initialized)return;if(!IsPaused)Pause();PrepareRecurring();NewPage("Recurring",weeklyChallenges?"WEEKLY CHALLENGES":"DAILY CHALLENGES");
            var r=Economy.recurring;var values=weeklyChallenges?r.weekly:r.daily;var targets=weeklyChallenges?RecurringProgress.WeeklyTargets:RecurringProgress.DailyTargets;
            Label(page,"Reset",$"Resets {DateLabel(weeklyChallenges?(r.week+1)*7:r.day+1)} · 00:00 UTC",20,new Vector2(0,150),new Vector2(1000,35));
            for(int i=0;i<3;i++){
                int index=i;int mask=weeklyChallenges?r.weeklyClaims:r.dailyClaims;bool claimed=(mask&(1<<i))!=0;
                Label(page,"Goal"+i,$"{RecurringProgress.ActivityNames[i]}: {values[i]:0} / {targets[i]}\n"+(weeklyChallenges?"1,000 coins + 2 gems + 300 season XP":"200 coins + 100 season XP"),21,new Vector2(-170,70-i*85),new Vector2(650,75));
                ActionButton(page,"Claim"+i,claimed?"CLAIMED":"CLAIM",new Vector2(340,70-i*85),()=>{TransactEconomy(e=>e.ClaimChallenge(weeklyChallenges,index,Today));ShowRecurring();},new Vector2(240,55)).interactable=r.CanClaim(weeklyChallenges,i);
            }
            Label(page,"Notice",ContentNotice,17,new Vector2(0,-170),new Vector2(1000,45));
            ActionButton(page,"Back","SHOP",new Vector2(-405,-240),ShowShop,new Vector2(230,55));
            ActionButton(page,"Period",weeklyChallenges?"DAILY":"WEEKLY",new Vector2(-135,-240),()=>{weeklyChallenges=!weeklyChallenges;ShowRecurring();},new Vector2(230,55));
            ActionButton(page,"Events","EVENTS",new Vector2(135,-240),ShowEvents,new Vector2(230,55));
            ActionButton(page,"Season","SEASON PASS",new Vector2(405,-240),ShowSeason,new Vector2(230,55));
        }
        private void ShowEvents()
        {
            PrepareRecurring();NewPage("Events","EVENTS & COSMETICS");var r=Economy.recurring;
            Label(page,"Event",new[]{"EXPLORER WEEK","STUNT FESTIVAL","CAREER RALLY"}[r.EventKind]+$" · Ends {DateLabel((r.week+1)*7)} UTC",24,new Vector2(0,145),new Vector2(1000,45));
            Label(page,"Balance",$"GEMS {Economy.gems}",18,new Vector2(0,110),new Vector2(1000,25));
            Label(page,"Progress",$"{RecurringProgress.ActivityNames[r.EventKind]}: {r.eventProgress:0} / {r.EventTarget}\n1,500 coins + 3 gems + 500 season XP",22,new Vector2(-170,70),new Vector2(650,75));
            ActionButton(page,"ClaimEvent",r.eventClaimed?"CLAIMED":"CLAIM",new Vector2(340,70),()=>{TransactEconomy(e=>e.ClaimEvent(Today));ShowEvents();},new Vector2(240,55)).interactable=!r.eventClaimed && r.eventProgress>=r.EventTarget;
            ActionButton(page,"Sunset",Economy.sunsetPaint?"SUNSET PAINT OWNED":"SUNSET PAINT · 10 GEMS",new Vector2(0,-15),()=>{TransactEconomy(e=>e.BuySunsetPaint());ShowEvents();},new Vector2(700,55)).interactable=!Economy.sunsetPaint && Economy.gems>=10;
            Label(page,"Cosmetics","Equip owned paints in Garage. Violet paint: bonus season tier 10.",19,new Vector2(0,-70),new Vector2(1000,40));
            var ad=ActionButton(page,"RewardedAd","REWARDED ADS UNAVAILABLE",new Vector2(0,-125),()=>{},new Vector2(700,50));ad.interactable=false;
            Label(page,"Notice",string.IsNullOrEmpty(economyNotice)?"Optional ads are disabled. No ad rewards or purchases use real money.":economyNotice,17,new Vector2(0,-180),new Vector2(1000,40));
            ActionButton(page,"Back","BACK",new Vector2(-310,-245),ShowRecurring,new Vector2(280,50));ActionButton(page,"Garage","GARAGE",new Vector2(310,-245),ShowGarage,new Vector2(280,50));
        }
        private void ShowSeason()
        {
            PrepareRecurring();NewPage("Season","28-DAY SEASON PASS");var r=Economy.recurring;
            Label(page,"Progress",$"{r.seasonXP} / 2,500 XP · Tier {r.Level}/10 · Gems {Economy.gems} · Ends {DateLabel((r.season+1)*28)} UTC",21,new Vector2(0,150),new Vector2(1000,35));
            for(int row=0;row<5;row++){
                int tier=seasonPage*5+row+1;float y=95-row*60;int bit=1<<(tier-1);
                Label(page,"Tier"+tier,$"TIER {tier}\n{tier*250} XP",19,new Vector2(-400,y),new Vector2(170,55));
                var free=ActionButton(page,"Free"+tier,(r.freeClaims&bit)!=0?"CLAIMED":$"250 COINS"+(tier%5==0?" + 2 GEMS":""),new Vector2(-80,y),()=>{TransactEconomy(e=>e.ClaimSeason(tier,false,Today));ShowSeason();},new Vector2(400,50));free.interactable=tier<=r.Level && (r.freeClaims&bit)==0;
                var bonus=ActionButton(page,"Bonus"+tier,(r.bonusClaims&bit)!=0?"CLAIMED":tier==10?"500C + 1G + VIOLET":"BONUS: 500C + 1G",new Vector2(330,y),()=>{TransactEconomy(e=>e.ClaimSeason(tier,true,Today));ShowSeason();},new Vector2(380,50));bonus.interactable=r.bonusPass && tier<=r.Level && (r.bonusClaims&bit)==0;
            }
            Label(page,"Notice",string.IsNullOrEmpty(economyNotice)?"XP: login 25 · daily goal 100 · weekly goal 300 · event 500. Bonus lasts this season.":economyNotice,17,new Vector2(0,-195),new Vector2(1040,35));
            ActionButton(page,"Back","BACK",new Vector2(-400,-245),ShowRecurring,new Vector2(220,50));
            ActionButton(page,"BuyPass",r.bonusPass?"BONUS OWNED":"BONUS PASS · 25 GEMS",new Vector2(-40,-245),()=>{TransactEconomy(e=>e.BuySeasonPass(Today));ShowSeason();},new Vector2(450,50)).interactable=!r.bonusPass && Economy.gems>=25;
            ActionButton(page,"Next",seasonPage==0?"TIERS 6–10":"TIERS 1–5",new Vector2(390,-245),()=>{seasonPage=1-seasonPage;ShowSeason();},new Vector2(250,50));
        }
    }
}

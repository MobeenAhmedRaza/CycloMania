using System;
using Cyclomania;
using UnityEngine;
public static class RecurringValidation
{
    static int count;
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;Debug.Log("PASS: "+name);}
    public static void Run()
    {
        const long day=700000;
        var e=GameEconomy.FromJson("{\"version\":1,\"coins\":123,\"gems\":10}");Check(e.coins==123 && e.recurring!=null && e.recurring.daily.Length==3,"old economy save migrates recurring content without losing balances");
        e.recurring.Add(0,1000,day);Check(e.recurring.CanClaim(false,0) && !e.recurring.CanClaim(false,1),"daily distance goal tracks only matching activity");
        int before=e.coins;Check(e.ClaimChallenge(false,0,day) && e.coins==before+200 && e.recurring.seasonXP==100,"daily claim grants currency and season XP together");Check(!e.ClaimChallenge(false,0,day),"daily reward cannot be claimed twice");
        e=GameEconomy.FromJson(e.ToJson());Check(!e.ClaimChallenge(false,0,day),"daily claim survives reload");
        e.recurring.Add(1,5000,day);Check(e.ClaimChallenge(true,1,day) && e.gems==12,"weekly stunt goal grants rare gems");
        e.recurring.Add(2,10,day);Check(e.ClaimChallenge(false,2,day) && e.ClaimChallenge(true,2,day),"mission activity drives separate daily and weekly goals");
        e.recurring.Add(0,float.NaN,day);e.recurring.Add(0,-1000,day);Check(e.recurring.daily[0]==1000,"invalid activity cannot corrupt progress");
        float weekly=e.recurring.weekly[0];e.recurring.Refresh(day+1);Check(e.recurring.dailyClaims==0 && e.recurring.daily[0]==0 && e.recurring.weekly[0]==weekly,"new UTC day resets daily while preserving same-week progress");
        Check(!e.recurring.Refresh(day) && !e.ClaimChallenge(false,0,day),"clock rollback cannot reopen earlier goals");
        e.recurring.Add(1,5000,day+1);e.recurring.Refresh((e.recurring.week+1)*7);Check(e.recurring.weekly[1]==0 && e.recurring.weeklyClaims==0 && !e.recurring.eventClaimed,"Monday rollover clears weekly goals and event");
        long current=e.recurring.day;int kind=e.recurring.EventKind;e.recurring.Add(kind,e.recurring.EventTarget,current);before=e.coins;Check(e.ClaimEvent(current) && e.coins==before+1500 && !e.ClaimEvent(current),"rotating event grants reward once");
        int oldKind=e.recurring.EventKind;e.recurring.Refresh((e.recurring.week+1)*7);Check(e.recurring.EventKind==(oldKind+1)%3,"event type rotates each week");
        e.gems=24;current=e.recurring.day;Check(!e.BuySeasonPass(current) && e.gems==24,"unaffordable season pass leaves balance intact");e.gems=30;Check(e.BuySeasonPass(current) && e.gems==5 && !e.BuySeasonPass(current),"season pass charges exactly once");
        e.recurring.seasonXP=249;Check(!e.ClaimSeason(1,false,current),"locked season tier cannot be claimed");e.recurring.AddXP(1);Check(e.ClaimSeason(1,false,current) && e.ClaimSeason(1,true,current),"earned tier permits separate free and bonus claims");Check(!e.ClaimSeason(1,true,current),"bonus tier cannot be claimed twice");
        e.recurring.AddXP(9999);Check(e.recurring.Level==10 && e.ClaimSeason(10,true,current) && e.violetPaint,"final bonus tier unlocks persistent violet cosmetic");
        e=GameEconomy.FromJson(e.ToJson());Check(e.violetPaint && !e.ClaimSeason(10,true,current),"season cosmetic and claim mask survive reload");
        e.recurring.Refresh((e.recurring.season+1)*28);Check(!e.recurring.bonusPass && e.recurring.seasonXP==0 && e.recurring.freeClaims==0 && e.violetPaint,"new season expires pass and XP but preserves earned cosmetic");
        e.gems=10;Check(e.BuySunsetPaint() && e.gems==0 && !e.BuySunsetPaint() && e.OwnsPaint(6) && !e.OwnsPaint(5),"cosmetics purchase and ownership are independent");
        e=GameEconomy.FromJson(e.ToJson());Check(e.sunsetPaint && e.OwnsPaint(7) && !e.OwnsPaint(8),"owned cosmetic selection persists with bounds checks");
        e=new GameEconomy();Check(e.ClaimDaily(day) && e.recurring.seasonXP==25 && !e.ClaimDaily(day),"existing daily login awards season XP exactly once");
        e=GameEconomy.FromJson("{\"version\":1,\"recurring\":{\"daily\":null,\"weekly\":[-1],\"seasonXP\":999999,\"freeClaims\":65535}}");Check(e.recurring.daily.Length==3 && e.recurring.weekly[0]==0 && e.recurring.seasonXP==2500 && e.recurring.freeClaims==1023,"malformed recurring arrays masks and XP normalize safely");
        e=new GameEconomy();e.recurring.Refresh(day+1);Check(!e.ClaimDaily(day),"login cannot reward a date older than observed activity");
        Debug.Log("RECURRING_VALIDATION_SUCCESS "+count);
    }
}

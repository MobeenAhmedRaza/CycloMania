using System;
using Cyclomania;
using UnityEngine;
public static class EconomyValidation
{
    static int count;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;Debug.Log("PASS: "+message);}
    public static void Run()
    {
        var e=GameEconomy.FromJson("broken");Check(e.coins==0 && e.gems==0,"wallet recovers corrupt save");
        e=GameEconomy.FromJson("{\"version\":1,\"coins\":-1,\"gems\":-4,\"bikes\":null}");Check(e.coins==0 && e.bikes.Length==5,"wallet normalizes balances and arrays");
        e.BankStunt(9);Check(e.coins==0 && e.stuntRemainder==9,"small stunt awards retain remainder");e.BankStunt(12);Check(e.coins==2 && e.stuntRemainder==1,"banked stunts convert to coins");e.BankStunt(-10);Check(e.coins==2,"negative stunt scores cannot debit wallet");
        Check(e.RewardMission(false,0,5,500) && e.gems==2,"milestone mission gives rare gems");Check(!e.RewardMission(false,0,5,500) && !e.RewardMission(false,0,4,500),"replayed tiers cannot reward twice");Check(e.RewardMission(true,0,5,500),"Freestyle reward ledger is independent");
        e=GameEconomy.FromJson(e.ToJson());Check(!e.RewardMission(false,0,5,500),"reward ledger survives reload");
        Check(e.ClaimDaily(100) && !e.ClaimDaily(100) && !e.ClaimDaily(99),"daily reward prevents same-day and clock-rollback claims");for(int d=101;d<=106;d++)e.ClaimDaily(d);Check(e.streak==7 && e.gems==9,"seven-day streak awards gems");e.ClaimDaily(108);Check(e.streak==1,"missed day resets streak");
        var g=new GarageProgress();e.coins=499;Check(!e.BuyBike(g,1) && e.coins==499,"unaffordable bicycle leaves balance unchanged");e.coins=2000;Check(e.BuyBike(g,1) && e.coins==1500,"bicycle purchase debits exact price");e.Apply(g);Check(g.IsUnlocked(1) && !e.BuyBike(g,1),"purchased bicycle unlocks without duplicate charge");
        Check(e.BuyUpgrade(g,1,0) && e.coins==1200,"upgrade purchase uses current tier price");e.Apply(g);g.selected=1;g.SetPaint(1,2);
        e=GameEconomy.FromJson(e.ToJson());g=GarageProgress.FromJson(g.ToJson(),e);Check(g.selected==1 && g.drive[1]==1 && g.paint[1]==2,"paid bicycle selection upgrade and paint survive restart");
        Check(!e.BuyUpgrade(g,2,0) && !e.BuyUpgrade(g,1,9),"locked bicycles and invalid upgrade categories reject purchase");
        Check(e.BuyPaint() && e.gems==4 && !e.BuyPaint(),"gem cosmetic purchased once");
        e.coins=int.MaxValue;e.BankStunt(int.MaxValue);Check(e.coins==int.MaxValue,"currency overflow saturates safely");
        var legacy=new GarageProgress();legacy.Earn(5000);legacy.Upgrade(4,0);legacy.Select(4);legacy=GarageProgress.FromJson(legacy.ToJson(),new GameEconomy());Check(legacy.selected==4 && legacy.drive[4]==1,"existing point-earned bicycle and upgrades retained");
        Debug.Log("ECONOMY_VALIDATION_SUCCESS "+count);
    }
}

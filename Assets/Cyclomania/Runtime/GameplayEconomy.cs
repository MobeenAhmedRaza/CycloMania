using System;
using UnityEngine;

namespace Cyclomania
{
    public sealed partial class GameplayHud
    {
        [NonSerialized] public string EconomySaveKey="Cyclomania.Economy.v1";
        public GameEconomy Economy {get;private set;}=new GameEconomy();
        private string economyNotice="";
        private void InitializeEconomy()
        {
            Economy=GameEconomy.FromJson(string.IsNullOrEmpty(EconomySaveKey)?"":PlayerPrefs.GetString(EconomySaveKey,""));
            Economy.Apply(Garage);Session.SetCurrency(Economy.coins,Economy.gems);
        }
        // Commit the complete proposed transaction before exposing balances or entitlements.
        private bool TransactEconomy(Func<GameEconomy,bool> change)
        {
            var next=GameEconomy.FromJson(Economy.ToJson());if(!change(next))return false;
            try {if(!string.IsNullOrEmpty(EconomySaveKey)){PlayerPrefs.SetString(EconomySaveKey,next.ToJson());PlayerPrefs.Save();}}
            catch(Exception e){economyNotice="Could not save purchase/reward. Try again.";Debug.LogWarning("Economy save failed: "+e.Message);return false;}
            Economy=next;Economy.Apply(Garage);Session.SetCurrency(Economy.coins,Economy.gems);economyNotice="";return true;
        }
        public void RewardMission(bool free,int index,int level,int reward)
        {TransactEconomy(e=>{if(!e.RewardMission(free,index,level,reward))return false;e.recurring.Add(2,1,GameEconomy.Day(DateTime.UtcNow));return true;});}
        private void ShowShop()
        {
            if(!initialized)return;if(!IsPaused)Pause();NewPage("Shop","CYCLE SHOP");
            Label(page,"Balance",$"COINS {Economy.coins}    GEMS {Economy.gems}",28,new Vector2(0,150),new Vector2(1000,40));
            var daily=ActionButton(page,"Daily","DAILY REWARD",new Vector2(-320,85),()=>{TransactEconomy(e=>e.ClaimDaily(GameEconomy.Day(DateTime.UtcNow)));ShowShop();},new Vector2(300,55));
            daily.interactable=GameEconomy.Day(DateTime.UtcNow)>Economy.lastDay;
            Label(page,"DailyInfo",$"Streak: {Economy.streak}/7 · 100–700 coins/day · Day 7: 5 gems",18,new Vector2(185,85),new Vector2(650,40));
            var spec=GarageProgress.Catalog[browsing];
            ActionButton(page,"Previous","<",new Vector2(-470,20),()=>{browsing=(browsing+4)%5;ShowShop();},new Vector2(70,50));
            Label(page,"Bicycle",spec.Name,27,new Vector2(0,20),new Vector2(750,40));
            ActionButton(page,"Next",">",new Vector2(470,20),()=>{browsing=(browsing+1)%5;ShowShop();},new Vector2(70,50));
            var buy=ActionButton(page,"BuyBike",Garage.IsUnlocked(browsing)?"BICYCLE OWNED":$"BUY · {GameEconomy.BikePrices[browsing]} COINS",new Vector2(-260,-45),()=>{TransactEconomy(e=>e.BuyBike(Garage,browsing));ShowShop();},new Vector2(440,55));
            buy.interactable=garageReady && !changingBicycle && !Garage.IsUnlocked(browsing) && Economy.coins>=GameEconomy.BikePrices[browsing];
            var paint=ActionButton(page,"BuyPaint",Economy.neonPaint?"NEON PAINT OWNED":"NEON PAINT · 5 GEMS",new Vector2(260,-45),()=>{TransactEconomy(e=>e.BuyPaint());ShowShop();},new Vector2(440,55));paint.interactable=!Economy.neonPaint && Economy.gems>=5;
            string[] names={"DRIVE","BRAKES","HANDLING"};int[] levels={Garage.drive[browsing],Garage.brakes[browsing],Garage.handling[browsing]};
            for(int i=0;i<3;i++) {int c=i,tier=levels[i]+1;var b=ActionButton(page,"BuyUpgrade"+i,names[i]+(tier>3?" · MAX":$" · {GameEconomy.UpgradePrice(tier)} COINS"),new Vector2((i-1)*330,-115),()=>{if(TransactEconomy(e=>e.BuyUpgrade(Garage,browsing,c)) && browsing==Garage.selected)Garage.Apply(bicycle,browsing);ApplyAdvanced();ShowShop();},new Vector2(315,55));b.interactable=garageReady && !changingBicycle && Garage.IsUnlocked(browsing) && tier<=3 && Economy.coins>=GameEconomy.UpgradePrice(tier);}
            Label(page,"Info",string.IsNullOrEmpty(economyNotice)?"Bank 10 stunt points = 1 coin. New mission tiers earn coins; every 5th tier earns 2 gems.\nPoint-earned garage unlocks remain free. Select purchased neon paint in the garage.":economyNotice,17,new Vector2(0,-180),new Vector2(1050,55));
            ActionButton(page,"Back","BACK",new Vector2(-310,-245),ShowPausePage,new Vector2(280,55));
            ActionButton(page,"Content","CHALLENGES",new Vector2(0,-245),ShowRecurring,new Vector2(280,55));
            ActionButton(page,"Garage","GARAGE",new Vector2(310,-245),ShowGarage,new Vector2(280,55));
        }
    }
}

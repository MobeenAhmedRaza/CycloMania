using System;
using System.Collections;
using SBPScripts;
using UnityEngine;
using UnityEngine.Events;

namespace Cyclomania
{
    public sealed partial class GameplayHud
    {
        public GarageProgress Garage { get; private set; }=new GarageProgress();
        [NonSerialized] public string GarageSaveKey="Cyclomania.Garage.v1";
        private int browsing;
        private bool changingBicycle, garageReady;
        private string garageNotice="";
        private Vector3 garageSpawn;
        private Quaternion garageRotation;
        private StuntScoring garageScoring;

        private void InitializeGarage()
        {
            // Test fixtures/other scenes without a catalog remain independent of player saves.
            garageReady=garagePrefabs!=null && garagePrefabs.Length==GarageProgress.Catalog.Length;
            if(!garageReady) return;
            InitializeEconomy();
            Garage=GarageProgress.FromJson(string.IsNullOrEmpty(GarageSaveKey)?"":PlayerPrefs.GetString(GarageSaveKey,""),Economy);
            garageSpawn=bicycle.transform.position;
            garageRotation=Quaternion.Euler(0,bicycle.transform.eulerAngles.y,0);
            if(stunts!=null) { garageScoring=stunts.Scoring; garageScoring.ComboBanked+=OnGarageScore; }
            if(touchControls!=null)
                foreach(var control in touchControls.GetComponentsInChildren<MobileButtonHandler>(true))
                    for(int i=0;i<control.onPress.GetPersistentEventCount();i++)
                        if(control.onPress.GetPersistentTarget(i)==recovery)
                        { control.onPress=new UnityEvent(); control.onPress.AddListener(ResetCurrentBicycle); break; }
            if(garagePrefabs[Garage.selected]==null) Garage.Select(0);
            browsing=Garage.selected;
            if(Garage.selected==0) { Garage.Apply(bicycle,0); equippedBicycleName=GarageProgress.Catalog[0].Name; }
            else StartCoroutine(ChangeBicycle(Garage.selected));
        }
        public void ResetCurrentBicycle() { if(!changingBicycle && recovery!=null) recovery.Respawn(); }
        private void OnGarageScore(int score) { Garage.Earn(score); TransactEconomy(e=>{e.BankStunt(score);e.recurring.Add(1,score,GameEconomy.Day(DateTime.UtcNow));return score>0;}); SaveGarage(); }
        private void SaveGarage()
        {
            if(!garageReady || string.IsNullOrEmpty(GarageSaveKey)) return;
            try { PlayerPrefs.SetString(GarageSaveKey,Garage.ToJson()); PlayerPrefs.Save(); garageNotice=""; }
            catch(Exception exception) { garageNotice="Could not save setup. Try again before closing."; Debug.LogWarning("Garage save failed: "+exception.Message); }
        }
        private void CloseGarage()
        { if(garageScoring!=null) garageScoring.ComboBanked-=OnGarageScore; }
        public void ShowGarage()
        {
            if(!initialized) return;
            if(!IsPaused) Pause();
            DrawGarage();
        }
        private void DrawGarage()
        {
            NewPage("Garage","BICYCLE GARAGE");
            var spec=GarageProgress.Catalog[browsing];
            bool unlocked=Garage.IsUnlocked(browsing);
            Label(page,"Progress",$"LIFETIME STUNT POINTS  {Garage.points}   |   EQUIPPED: {equippedBicycleName}",22,new Vector2(0,150),new Vector2(1040,35));
            ActionButton(page,"Previous","<",new Vector2(-470,85),()=>Browse(-1),new Vector2(80,65)).interactable=!changingBicycle;
            ActionButton(page,"Next",">",new Vector2(470,85),()=>Browse(1),new Vector2(80,65)).interactable=!changingBicycle;
            Label(page,"Bicycle",spec.Name.ToUpperInvariant(),32,new Vector2(0,95),new Vector2(800,45));
            Label(page,"Category",spec.Description,21,new Vector2(0,55),new Vector2(800,35));
            Label(page,"Stats",$"CRUISE / SPRINT  {Garage.SprintSpeed(browsing)*1.8f:0.#} / {Garage.SprintSpeed(browsing)*3.6f:0.#} KM/H    |    BRAKING  {Garage.BrakePower(browsing):0.#} m/s²\nDRIVE  {Garage.DriveForce(browsing):0} N    |    STEERING  {Garage.Steering(browsing):0.#}°",21,new Vector2(0,0),new Vector2(1040,65));
            string[] names={"DRIVETRAIN","BRAKES","HANDLING"};
            int[] levels={Garage.drive[browsing],Garage.brakes[browsing],Garage.handling[browsing]};
            for(int i=0;i<3;i++)
            {
                int category=i,level=levels[i];
                string detail=level==3?"MAX":Garage.points>=Garage.TierRequirement(level+1)?"UPGRADE":$"{Garage.TierRequirement(level+1)} PTS";
                var button=ActionButton(page,"Upgrade"+i,$"{names[i]} {level}/3\n{detail}",new Vector2((i-1)*330,-80),()=>UpgradeBicycle(category),new Vector2(310,75));
                button.interactable=garageReady && unlocked && !changingBicycle && level<3 && Garage.points>=Garage.TierRequirement(level+1);
            }
            var paint=ActionButton(page,"Paint","PAINT: "+GarageProgress.PaintNames[Garage.paint[browsing]],new Vector2(-270,-165),PaintBicycle,new Vector2(420,65));
            paint.interactable=garageReady && unlocked && !changingBicycle;
            var equip=ActionButton(page,"Equip",changingBicycle?"PREPARING...":!unlocked?$"UNLOCK AT {spec.Unlock} PTS":browsing==Garage.selected?"EQUIPPED":"EQUIP AT SPAWN",new Vector2(270,-165),EquipBicycle,new Vector2(420,65));
            equip.interactable=garageReady && unlocked && browsing!=Garage.selected && !changingBicycle;
            ActionButton(page,"Back","BACK",new Vector2(-370,-240),ShowPausePage,new Vector2(220,55)).interactable=!changingBicycle;
            Label(page,"Hint",string.IsNullOrEmpty(garageNotice)?"Bank stunts to unlock bikes and upgrade tiers.\nPoints are lifetime progress; upgrades do not spend them.":garageNotice,17,new Vector2(0,-240),new Vector2(500,50));
            ActionButton(page,"Ride","RESUME",new Vector2(370,-240),Resume,new Vector2(220,55)).interactable=!changingBicycle;
        }
        private void Browse(int direction) { browsing=(browsing+direction+GarageProgress.Catalog.Length)%GarageProgress.Catalog.Length; DrawGarage(); }
        private void UpgradeBicycle(int category)
        {
            if(changingBicycle || !Garage.Upgrade(browsing,category)) return;
            if(browsing==Garage.selected) Garage.Apply(bicycle,browsing);
            ApplyAdvanced();
            SaveGarage(); DrawGarage();
        }
        private void PaintBicycle()
        {
            if(changingBicycle)return;
            int next=(Garage.paint[browsing]+1)%GarageProgress.PaintNames.Length;
            while(!Economy.OwnsPaint(next))next=(next+1)%GarageProgress.PaintNames.Length;
            if(!Garage.SetPaint(browsing,next))return;
            if(browsing==Garage.selected) Garage.Apply(bicycle,browsing);
            ApplyAdvanced();
            SaveGarage(); DrawGarage();
        }
        private void EquipBicycle()
        { if(!changingBicycle && garageReady && Garage.IsUnlocked(browsing)) StartCoroutine(ChangeBicycle(browsing)); }
        private IEnumerator ChangeBicycle(int index)
        {
            if(garagePrefabs[index]==null || garagePrefabs[index].GetComponent<BicycleController>()==null)
            { garageNotice="Bicycle asset unavailable."; if(CurrentPage=="Garage") DrawGarage(); yield break; }
            Missions?.Interrupt("Changing bicycles ended this attempt.");
            changingBicycle=true;
            var old=bicycle;
            if(recovery!=null && recovery.IsReady) { garageSpawn=recovery.CheckpointPosition; garageRotation=Quaternion.Euler(0,recovery.CheckpointHeading,0); }
            old.ClearInput();
            var controls=old.mobileControls;
            old.gameObject.SetActive(false); // Legacy rider scripts must find only the newly equipped bicycle.
            var instance=Instantiate(garagePrefabs[index],garageSpawn,garageRotation);
            bicycle=instance.GetComponent<BicycleController>();
            bicycle.mobileControls=controls;
            Garage.Apply(bicycle,index);
            recovery=instance.GetComponent<CycleRecovery>();
            if(recovery==null) recovery=instance.AddComponent<CycleRecovery>();
            stunts=instance.GetComponent<BicycleStunts>();
            if(stunts==null) stunts=instance.AddComponent<BicycleStunts>();
            if(garageScoring!=null) stunts.UseScoring(garageScoring);
            foreach(var camera in FindObjectsByType<BicycleCamera>(FindObjectsSortMode.None)) camera.SetTarget(bicycle.transform);
            foreach(var map in hud.GetComponentsInChildren<RideMinimap>()) map.SetTarget(bicycle.transform);
            Garage.Select(index); ApplyAdvanced(); equippedBicycleName=GarageProgress.Catalog[index].Name;
            Destroy(old.gameObject);
            if(CurrentPage=="Garage") DrawGarage();
            // Start initializes the complete jointed rig even while the garage freezes game time.
            yield return null;
            bicycle.ClearInput();
            changingBicycle=false;
            SaveGarage();
            if(CurrentPage=="Garage") DrawGarage();
        }
    }
}

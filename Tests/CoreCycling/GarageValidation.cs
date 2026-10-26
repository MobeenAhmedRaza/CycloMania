using System;
using System.Collections;
using System.Reflection;
using Cyclomania;
using SBPScripts;
using UnityEngine;
using UnityEngine.UI;

public static class GarageValidation
{
    static int count;
    static void Check(bool pass,string name) { if(!pass) throw new Exception(name); count++; Debug.Log("PASS: "+name); }
    static object Invoke(object target,string name,params object[] args) => target.GetType().GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(target,args);
    static Transform Page(GameplayHud hud) => (Transform)typeof(GameplayHud).GetField("page",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(hud);
    public static void Run(BicycleController bike)
    {
        var data=new GarageProgress();
        Check(data.IsUnlocked(0) && !data.Select(1) && data.selected==0,"new garage owns City and rejects locked bicycle selection");
        Check(!data.Upgrade(0,0) && !data.SetPaint(1,2),"locked upgrades and cosmetics cannot be applied");
        data.Earn(-1);data.Earn(299);Check(!data.Upgrade(0,0),"upgrade tier boundary rejects insufficient lifetime score");
        data.Earn(1);Check(data.Upgrade(0,0) && !data.Upgrade(0,0) && data.points==300,"first upgrade unlocks at 300 without spending progress");
        data.Earn(4700);Check(data.Select(4),"banked stunt progression unlocks final bicycle");
        data.SetPaint(4,2);data.Upgrade(4,1);
        var restored=GarageProgress.FromJson(data.ToJson());
        Check(restored.selected==4 && restored.paint[4]==2 && restored.brakes[4]==1 && restored.points==5000,"garage save round trip retains selection cosmetics upgrades and unlocks");
        restored.Upgrade(4,1);restored.Upgrade(4,1);
        Check(!restored.Upgrade(4,1) && restored.brakes[4]==3,"upgrade cap prevents exceeding tier three");
        Check(!restored.Select(-1) && !restored.SetPaint(9,0) && !restored.Upgrade(0,7),"invalid garage operations are rejected");
        var corrupt=GarageProgress.FromJson("{\"version\":1,\"points\":-99,\"selected\":99,\"drive\":[99],\"paint\":null}");
        Check(corrupt.selected==0 && corrupt.points==0 && corrupt.drive.Length==5 && corrupt.drive[0]==0 && corrupt.paint.Length==5,"malformed save fields normalize without granting unearned equipment");
        Check(GarageProgress.FromJson("not json").selected==0 && GarageProgress.FromJson("{\"version\":99}").points==0,"corrupt and unsupported save versions recover defaults");
        restored.Earn(int.MaxValue);Check(restored.points==int.MaxValue,"lifetime points saturate without overflow");
        data.Apply(bike,0);float citySpeed=bike.topSpeed,citySteer=bike.steerAngle.Evaluate(0);
        data.Apply(bike,1);Check(bike.topSpeed<citySpeed && bike.steerAngle.Evaluate(0)>citySteer && bike.bunnyHopStrength==6,"BMX changes speed steering and hop handling");
        data.Upgrade(1,0);data.Apply(bike,1);float power=bike.accelerationCurve.Evaluate(1);data.Apply(bike,1);
        Check(Mathf.Approximately(power,257.6f) && bike.accelerationCurve.Evaluate(1)==power,"drivetrain upgrade affects actual force and does not compound on reapply");
        var frame=bike.gameObject.AddComponent<MeshRenderer>();data.SetPaint(1,2);data.Apply(bike,1);var block=new MaterialPropertyBlock();frame.GetPropertyBlock(block);
        Check(block.GetColor("_BaseColor")==GarageProgress.Paints[2],"paint changes frame property block");
        data.SetPaint(1,0);data.Apply(bike,1);frame.GetPropertyBlock(block);Check(block.isEmpty,"stock paint restores original material appearance");

        var root=new GameObject("Garage HUD test",typeof(RectTransform),typeof(Canvas));
        var hud=root.AddComponent<GameplayHud>();hud.SettingsSaveKey="";hud.AdvancedSaveKey="";hud.EconomySaveKey="";hud.GarageSaveKey="";hud.bicycle=bike;
        // The synthetic bicycle substitutes for prefab geometry; real prefab wiring is checked separately.
        hud.garagePrefabs=new[]{bike.gameObject,bike.gameObject,bike.gameObject,bike.gameObject,bike.gameObject};
        Invoke(hud,"Start");hud.Pause();hud.ShowGarage();
        hud.Garage.Earn(5000);
        Page(hud).Find("Next").GetComponent<Button>().onClick.Invoke();
        Check(hud.CurrentPage=="Garage" && Page(hud).Find("Bicycle").GetComponent<Text>().text=="BMX","garage next button browses bicycle catalog");
        Page(hud).Find("Upgrade0").GetComponent<Button>().onClick.Invoke();
        Check(hud.Garage.drive[1]==1 && hud.Garage.selected==0,"garage upgrades selected card without changing equipped bicycle");
        float distance=hud.Session.DistanceMeters;
        var routine=(IEnumerator)Invoke(hud,"ChangeBicycle",1);Check(routine.MoveNext(),"equip waits for new rig initialization");
        var next=hud.bicycle;Invoke(next,"Start");Invoke(next.GetComponent<CycleRecovery>(),"Start");Invoke(next.GetComponent<BicycleStunts>(),"Start");routine.MoveNext();
        Check(next!=bike && !bike.gameObject.activeSelf && hud.Garage.selected==1 && hud.equippedBicycleName=="BMX","equip replaces active bicycle and updates saved selection");
        Check(next.mobileControls==bike.mobileControls && Time.timeScale==0 && hud.Session.DistanceMeters==distance,"equip preserves touch bindings pause and trip");
        Check(next.GetComponent<BicycleStunts>().Scoring==bike.GetComponent<BicycleStunts>().Scoring,"equip retains session scoring instance");
        hud.ResetCurrentBicycle();Check(next.rb.linearVelocity==Vector3.zero,"reset addresses newly equipped recovery");
        hud.enabled=false;Check(Time.timeScale==1,"garage teardown restores time");
        UnityEngine.Object.Destroy(root);UnityEngine.Object.Destroy(next.gameObject);
        Debug.Log("GARAGE_VALIDATION_SUCCESS "+count);
    }
}

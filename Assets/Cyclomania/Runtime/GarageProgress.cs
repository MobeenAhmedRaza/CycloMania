using System;
using UnityEngine;

namespace Cyclomania
{
    public sealed class BicycleSpec
    {
        public readonly string Name, Description;
        public readonly int Unlock;
        public readonly float Speed, Drive, Brake, Steering, Hop;
        public BicycleSpec(string name,string description,int unlock,float speed,float drive,float brake,float steering,float hop)
        { Name=name; Description=description; Unlock=unlock; Speed=speed; Drive=drive; Brake=brake; Steering=steering; Hop=hop; }
    }

    // Stable indices are part of save version 1. Append new bicycles; do not reorder.
    [Serializable]
    public sealed class GarageProgress
    {
        public static readonly BicycleSpec[] Catalog={
            new BicycleSpec("City Bicycle","Balanced everyday riding",0,12.5f,200,7,25,5),
            new BicycleSpec("BMX","Quick steering and stronger hops",500,11,230,7.5f,30,6),
            new BicycleSpec("Mountain Bicycle","Strong drive and confident braking",1500,12,250,9,24,5.5f),
            new BicycleSpec("Trials Bicycle","Precise low-speed stunt control",3000,9,260,10,34,6.5f),
            new BicycleSpec("Fatbike","Steady steering and relaxed cruising",5000,10,220,8,20,4.5f)};
        public static readonly string[] PaintNames={"STOCK","CYAN","RED","GOLD","WHITE","NEON","SUNSET","VIOLET"};
        public static readonly Color[] Paints={Color.white,new Color(.05f,.8f,.95f),new Color(.9f,.08f,.1f),new Color(1,.65f,.08f),Color.white,new Color(.5f,1,.05f),new Color(1,.25f,.05f),new Color(.6f,.15f,1)};
        public int version=1, points, selected;
        public int[] drive=new int[5], brakes=new int[5], handling=new int[5], paint=new int[5];
        [NonSerialized] public bool[] purchased=new bool[5];
        public bool IsUnlocked(int index) => index>=0 && index<Catalog.Length && (purchased[index] || points>=Catalog[index].Unlock);
        public int TierRequirement(int level) => level==1 ? 300 : level==2 ? 1000 : 2500;
        public bool Select(int index) { if(!IsUnlocked(index)) return false; selected=index; return true; }
        public void Earn(int amount) { if(amount>0) points=(int)Math.Min(int.MaxValue,(long)points+amount); }
        public bool Upgrade(int index,int category)
        {
            if(!IsUnlocked(index) || category<0 || category>2) return false;
            int[] levels=category==0?drive:category==1?brakes:handling;
            if(levels[index]>=3 || points<TierRequirement(levels[index]+1)) return false;
            levels[index]++; return true;
        }
        public bool SetPaint(int index,int value)
        { if(!IsUnlocked(index) || value<0 || value>=PaintNames.Length) return false; paint[index]=value; return true; }
        public float SprintSpeed(int index) => Catalog[index].Speed*3f*(1+.06f*drive[index]);
        public float DriveForce(int index) => Catalog[index].Drive*(1+.12f*drive[index]);
        public float BrakePower(int index) => Catalog[index].Brake*3f*(1+.15f*brakes[index]);
        public float Steering(int index) => Catalog[index].Steering*(1+.04f*handling[index]);
        public string ToJson() => JsonUtility.ToJson(this);
        public static GarageProgress FromJson(string json,GameEconomy economy=null)
        {
            GarageProgress data;
            try { data=string.IsNullOrEmpty(json)?new GarageProgress():JsonUtility.FromJson<GarageProgress>(json); }
            catch(ArgumentException) { data=new GarageProgress(); }
            if(data==null || data.version!=1) data=new GarageProgress();
            data.points=Math.Max(0,data.points);
            data.drive=Normalize(data.drive,3); data.brakes=Normalize(data.brakes,3);
            data.handling=Normalize(data.handling,3); data.paint=Normalize(data.paint,PaintNames.Length-1);
            economy?.Apply(data);
            for(int i=0;i<Catalog.Length;i++)
            {
                if(!data.IsUnlocked(i)) { data.drive[i]=data.brakes[i]=data.handling[i]=data.paint[i]=0; continue; }
                while(data.drive[i]>0 && data.points<data.TierRequirement(data.drive[i]) && data.drive[i]>(economy?.drive[i]??0)) data.drive[i]--;
                while(data.brakes[i]>0 && data.points<data.TierRequirement(data.brakes[i]) && data.brakes[i]>(economy?.brakes[i]??0)) data.brakes[i]--;
                while(data.handling[i]>0 && data.points<data.TierRequirement(data.handling[i]) && data.handling[i]>(economy?.handling[i]??0)) data.handling[i]--;
            }
            if(!data.IsUnlocked(data.selected)) data.selected=0;
            return data;
        }
        private static int[] Normalize(int[] source,int max)
        {
            var result=new int[Catalog.Length];
            if(source!=null) for(int i=0;i<Math.Min(result.Length,source.Length);i++) result[i]=Mathf.Clamp(source[i],0,max);
            return result;
        }
        public void Apply(SBPScripts.BicycleController bike,int index)
        {
            var spec=Catalog[index];
            bike.topSpeed=SprintSpeed(index); bike.relaxedSpeed=.5f;
            bike.torque=10*(1+.12f*drive[index]);
            bike.accelerationCurve=AnimationCurve.Linear(0,0,1,DriveForce(index));
            bike.brakeDeceleration=BrakePower(index);
            bike.steerAngle=AnimationCurve.Linear(0,Steering(index),bike.topSpeed,Steering(index)*.32f);
            bike.bunnyHopStrength=spec.Hop*(1+.04f*handling[index]);
            bike.airFlipSpeed=7.5f*(1+.05f*handling[index]);
            bike.wheelieToggle=true;
            // Only tint the frame, never tires, skin, or shared material assets.
            var frame=bike.GetComponent<MeshRenderer>();
            if(frame!=null)
            {
                var block=new MaterialPropertyBlock();
                if(paint[index]!=0) { block.SetColor("_BaseColor",Paints[paint[index]]); block.SetColor("_Color",Paints[paint[index]]); }
                frame.SetPropertyBlock(block);
            }
        }
    }
}

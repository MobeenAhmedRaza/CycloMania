using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cyclomania
{
    public enum StuntKind { None, Wheelie, Stoppie, Jump }
    public struct StuntSample
    {
        public bool FrontGrounded, RearGrounded;
        public float Speed, NoseAngle, UprightDot, PitchDelta;
    }
    public struct StuntResult
    {
        public StuntKind Kind;
        public float Duration, Distance;
        public int FrontFlips, BackFlips, Points;
    }

    // Deterministic scoring from physical samples, independent of buttons and frame rate.
    public sealed class StuntScoring
    {
        public int Score { get; private set; }
        public int BestCombo { get; private set; }
        public int ComboCount { get; private set; }
        public int Multiplier => Mathf.Clamp(ComboCount,1,5);
        public int PendingScore => basePoints * Multiplier;
        public StuntKind ActiveKind { get; private set; }
        public float ActiveDuration { get; private set; }
        public float ActiveDistance { get; private set; }
        public float Airtime { get; private set; }
        public float PitchRotation { get; private set; }
        public string Message { get; private set; } = "LAND STUNTS TO SCORE";
        public event Action<StuntResult> StuntLanded;
        public event Action<int> ComboBanked;
        public event Action<int,int> ComboFinished;
        private readonly List<StuntResult> awaitingLanding = new List<StuntResult>();
        private int basePoints;
        private bool hasGround, inFlight;
        private float flightDistance, stableTime, comboGap, landingWait;
        public const float LandingSettleSeconds=.3f;
        public const float ComboWindowSeconds=2.5f;

        public void Tick(StuntSample sample,float dt)
        {
            if(dt<=0 || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            bool supported=sample.FrontGrounded || sample.RearGrounded;
            bool stable=sample.FrontGrounded && sample.RearGrounded && sample.UprightDot>=.8f;
            float distance=Mathf.Max(0,sample.Speed)*dt;
            if(supported && sample.UprightDot<.3f) { Cancel(); return; }
            if(stable) hasGround=true;

            StuntKind kind=StuntKind.None;
            if(sample.RearGrounded && !sample.FrontGrounded && sample.NoseAngle>=12 && sample.Speed>=1)
                kind=StuntKind.Wheelie;
            else if(sample.FrontGrounded && !sample.RearGrounded && sample.NoseAngle<=-10 && sample.Speed>=.5f)
                kind=StuntKind.Stoppie;
            if(kind!=ActiveKind)
            {
                FinishHold(); ActiveKind=kind; ActiveDuration=ActiveDistance=0;
            }
            if(kind!=StuntKind.None)
            {
                ActiveDuration+=dt; ActiveDistance+=distance;
                Message=kind==StuntKind.Wheelie ? "WHEELIE" : "STOPPIE";
            }

            if(!supported && hasGround)
            {
                inFlight=true; Airtime+=dt; flightDistance+=distance;
                PitchRotation+=sample.PitchDelta;
                Message=PitchRotation<=-360 ? "BACKFLIP" : PitchRotation>=360 ? "FRONTFLIP" : "AIRTIME";
                landingWait=0;
            }
            stableTime=stable ? stableTime+dt : 0;
            if(inFlight && supported)
            {
                landingWait+=dt;
                if(landingWait>2 && !stable) { Cancel(); return; }
            }
            if(stableTime>=LandingSettleSeconds)
            {
                if(inFlight)
                {
                    if(Airtime>=.25f)
                    {
                        int flips=Mathf.FloorToInt(Mathf.Abs(PitchRotation)/360f);
                        Queue(new StuntResult {Kind=StuntKind.Jump,Duration=Airtime,Distance=flightDistance,
                            FrontFlips=PitchRotation>0 ? flips : 0,BackFlips=PitchRotation<0 ? flips : 0,
                            Points=100+Mathf.RoundToInt(Airtime*100+flightDistance*5)+flips*500});
                    }
                    inFlight=false; Airtime=flightDistance=PitchRotation=landingWait=0;
                }
                if(awaitingLanding.Count>0)
                {
                    // Copy and clear before dispatch so listeners can safely reset a run.
                    var landed=awaitingLanding.ToArray(); awaitingLanding.Clear();
                    Message="CLEAN LANDING";
                    foreach(var result in landed) StuntLanded?.Invoke(result);
                }
            }
            bool performing=kind!=StuntKind.None || inFlight || awaitingLanding.Count>0;
            if(performing || !stable) comboGap=0;
            else if(basePoints>0)
            {
                comboGap+=dt;
                if(comboGap>=ComboWindowSeconds)
                {
                    int points=PendingScore,segments=ComboCount; Score+=points; BestCombo=Mathf.Max(BestCombo,points);
                    basePoints=ComboCount=0; comboGap=0;
                    Message="BANKED +"+points;
                    ComboBanked?.Invoke(points);
                    ComboFinished?.Invoke(points,segments);
                }
            }
        }
        private void FinishHold()
        {
            if(ActiveKind==StuntKind.None || ActiveDuration<.5f) return;
            Queue(new StuntResult {Kind=ActiveKind,Duration=ActiveDuration,Distance=ActiveDistance,
                Points=Mathf.RoundToInt(ActiveDuration*100+ActiveDistance*10)});
        }
        private void Queue(StuntResult result)
        {
            awaitingLanding.Add(result); basePoints+=result.Points; ComboCount++; comboGap=0;
        }
        public void Cancel()
        {
            bool lost=basePoints>0 || inFlight || ActiveKind!=StuntKind.None;
            basePoints=ComboCount=0; awaitingLanding.Clear();
            ActiveKind=StuntKind.None; ActiveDuration=ActiveDistance=Airtime=PitchRotation=0;
            flightDistance=stableTime=comboGap=landingWait=0; inFlight=hasGround=false;
            if(lost) Message="COMBO LOST";
        }
        public void ResetSession()
        {
            Cancel(); Score=BestCombo=0; Message="LAND STUNTS TO SCORE";
        }
    }
}

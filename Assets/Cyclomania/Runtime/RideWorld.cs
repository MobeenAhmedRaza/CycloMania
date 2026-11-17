using System;
using SBPScripts;
using UnityEngine;

namespace Cyclomania
{
    public sealed class RideDestination
    {
        public readonly string Name, Description;
        public readonly Vector3 Position;
        public readonly float Heading;
        public RideDestination(string name,string description,Vector3 position,float heading)
        { Name=name; Description=description; Position=position; Heading=heading; }
    }

    // Runtime travel controller. All riding surfaces belong to the authored scene.
    public sealed class RideWorld : MonoBehaviour
    {
        public static readonly RideDestination[] Destinations={
            new RideDestination("City Centre","Explore the streets and practise urban riding.",new Vector3(76.64f,0,-172.52f),-107.393f),
            new RideDestination("Industrial Yard","Ramps, jumps and a raised balance line.",new Vector3(620,0,-170),0),
            new RideDestination("Mountain Roads","Climb switchbacks and cross the high bridge.",new Vector3(730,0,80),0),
            new RideDestination("Highway","Long straights for bicycle sprint practice.",new Vector3(850,0,-240),0),
            new RideDestination("Desert Trails","Rolling dirt ramps and open trail riding.",new Vector3(1000,0,0),0)};
        public string Area { get; private set; }="City Centre";
        public int LastDestination { get; private set; }
        private GameplayHud hud;
        public static RideLevelLayout Layout { get; private set; }
        private float nextCheck;
        public void Initialize(GameplayHud owner)
        {
            hud=owner;
            Layout=FindFirstObjectByType<RideLevelLayout>();
            if(Layout==null) { Debug.LogError("The gameplay scene needs an authored RideLevelLayout."); return; }
            for(int i=0;i<Destinations.Length;i++) {
                var arrival=Layout.arrivals[i];
                Destinations[i]=new RideDestination(Destinations[i].Name,Destinations[i].Description,arrival.position,arrival.eulerAngles.y);
            }
        }
        public bool Travel(int index)
        {
            if(hud==null || Layout==null || index<0 || index>=Destinations.Length) return false;
            var recovery=hud.bicycle!=null?hud.bicycle.GetComponent<CycleRecovery>():null;
            if(recovery==null || !recovery.IsReady) return false;
            hud.Missions?.Interrupt("World travel ended this attempt.");
            var target=Destinations[index];
            recovery.SetCheckpoint(target.Position,target.Heading);recovery.Respawn();
            LastDestination=index;Area=target.Name;return true;
        }
        public static bool OutsidePlayableArea(Vector3 position)
        { return !Finite(position.x) || !Finite(position.y) || !Finite(position.z) ||
            (Layout!=null ? !Layout.playableBounds.Contains(position) : position.x< -450 || position.x>1150 || position.z< -620 || position.z>1000 || position.y< -15); }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private void Update()
        {
            if(hud==null || hud.IsPaused || hud.bicycle==null || Time.unscaledTime<nextCheck) return;
            nextCheck=Time.unscaledTime+.25f;
            var bike=hud.bicycle;var recovery=bike.GetComponent<CycleRecovery>();
            if(recovery==null || !recovery.IsReady) return;
            if(OutsidePlayableArea(bike.transform.position)) {recovery.Respawn();return;}
            Vector3 position=bike.transform.position;
            Area="City Streets";
            if(Layout!=null)for(int zone=1;zone<Destinations.Length;zone++)
                if(Layout.Contains(zone,position))Area=Destinations[zone].Name;
            for(int i=0;i<Destinations.Length;i++)
            {
                float distance=Vector3.Distance(bike.transform.position,Destinations[i].Position);
                if(distance<65) Area=Destinations[i].Name;
                if(distance<8 && bike.IsGrounded && !recovery.IsCrashed && Vector3.Dot(bike.transform.up,Vector3.up)>.8f)
                {LastDestination=i;recovery.SetCheckpoint(Destinations[i].Position,Destinations[i].Heading);}
            }
        }
        private void OnDestroy() { Layout=null; }
    }
}

using UnityEngine;

namespace Cyclomania
{
    // Scene-owned coordinates shared by the course geometry, missions, and recovery.
    // Course frames translate the original mission coordinates; keep rotation/scale at identity.
    public sealed class RideLevelLayout : MonoBehaviour
    {
        public Transform[] courseFrames = new Transform[5];
        public Transform[] arrivals = new Transform[5];
        public Transform school;
        public Transform canyonStart;
        public Bounds playableBounds;
        public Bounds[] freestyleBounds = new Bounds[5];

        public Vector3 Point(int zone, Vector3 original) => original + courseFrames[zone].position;
        public bool Contains(int zone, Vector3 point) => zone>=0 && zone<freestyleBounds.Length &&
            freestyleBounds[zone].Contains(point-courseFrames[zone].position);

        public MissionDefinition Place(MissionDefinition definition)
        {
            // Catalogs return a fresh definition, so relocating it cannot affect another attempt.
            foreach(var objective in definition.Objectives) {
                objective.point=Point(definition.Destination,objective.point);
                objective.landingPoint=Point(definition.Destination,objective.landingPoint);
            }
            return definition;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color=Color.cyan;Gizmos.DrawWireCube(playableBounds.center,playableBounds.size);
            for(int i=0;i<arrivals.Length;i++)if(arrivals[i]!=null) {
                Gizmos.DrawWireSphere(arrivals[i].position,2);
                Gizmos.DrawLine(arrivals[i].position,arrivals[i].position+arrivals[i].forward*5);
            }
        }
    }
}

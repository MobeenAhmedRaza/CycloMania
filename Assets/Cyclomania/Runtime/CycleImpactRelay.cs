using UnityEngine;

namespace Cyclomania
{
    public sealed class CycleImpactRelay : MonoBehaviour
    {
        public CycleRecovery Owner { private get; set; }
        private void OnCollisionEnter(Collision collision) { if (Owner != null) Owner.ReportImpact(collision); }
    }
}

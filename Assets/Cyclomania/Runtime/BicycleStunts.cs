using SBPScripts;
using UnityEngine;

namespace Cyclomania
{
    [DefaultExecutionOrder(50), DisallowMultipleComponent, RequireComponent(typeof(BicycleController))]
    public sealed class BicycleStunts : MonoBehaviour
    {
        public StuntScoring Scoring { get; private set; } = new StuntScoring();
        private BicycleController bicycle;
        private CycleRecovery recovery;
        private Quaternion lastRotation;
        private void Start()
        {
            bicycle=GetComponent<BicycleController>(); recovery=GetComponent<CycleRecovery>();
            lastRotation=transform.rotation;
            if(recovery!=null) { recovery.Crashed+=Cancel; recovery.Respawned+=Cancel; }
        }
        private void FixedUpdate()
        {
            if(bicycle==null || bicycle.rb==null) return;
            Quaternion current=bicycle.rb.rotation;
            Quaternion delta=Quaternion.Inverse(lastRotation)*current;
            delta.ToAngleAxis(out float angle,out Vector3 axis);
            if(angle>180) angle-=360;
            lastRotation=current;
            if(!bicycle.enabled || bicycle.IsSuspended || (recovery!=null && recovery.IsCrashed)) return;
            float pitchDelta=axis.sqrMagnitude>.001f && !float.IsInfinity(axis.x) ? angle*axis.x : 0;
            var sample=new StuntSample {
                FrontGrounded=bicycle.FrontGrounded, RearGrounded=bicycle.RearGrounded,
                Speed=Vector3.ProjectOnPlane(bicycle.rb.linearVelocity,Vector3.up).magnitude,
                NoseAngle=Mathf.Asin(Mathf.Clamp(Vector3.Dot(current*Vector3.forward,bicycle.GroundNormal),-1,1))*Mathf.Rad2Deg,
                UprightDot=Vector3.Dot(current*Vector3.up,bicycle.GroundNormal),PitchDelta=pitchDelta};
            Scoring.Tick(sample,Time.fixedDeltaTime);
        }
        public void UseScoring(StuntScoring scoring) { Scoring=scoring ?? new StuntScoring(); Cancel(); }
        public void Cancel() { Scoring.Cancel(); lastRotation=transform.rotation; }
        public void RestartSession() { Scoring.ResetSession(); lastRotation=transform.rotation; }
        private void OnDisable() { Cancel(); }
        private void OnDestroy()
        {
            if(recovery!=null) { recovery.Crashed-=Cancel; recovery.Respawned-=Cancel; }
        }
    }
}

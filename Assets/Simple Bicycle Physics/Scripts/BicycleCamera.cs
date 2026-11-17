using Cyclomania;
using UnityEngine;

namespace SBPScripts
{
    [RequireComponent(typeof(Camera))]
    public class BicycleCamera : MonoBehaviour
    {
        public Transform target;
        public bool stuntCamera;
        public bool FirstPerson {get;private set;}
        private Renderer[] riderRenderers;
        private bool[] previousHidden;
        public void SetFirstPerson(bool value){if(FirstPerson==value)return;RestoreRider();FirstPerson=value;if(value)HideRider();Snap();}
        private void HideRider(){if(target==null)return;riderRenderers=target.GetComponentsInChildren<SkinnedMeshRenderer>();previousHidden=new bool[riderRenderers.Length];for(int i=0;i<riderRenderers.Length;i++){previousHidden[i]=riderRenderers[i].forceRenderingOff;riderRenderers[i].forceRenderingOff=true;}}
        private void RestoreRider(){if(riderRenderers==null)return;for(int i=0;i<riderRenderers.Length;i++)if(riderRenderers[i]!=null)riderRenderers[i].forceRenderingOff=previousHidden[i];riderRenderers=null;}
        public float distance = 4.5f;
        public float height = 3f;
        public float heightDamping = 8f;
        public float lookAtHeight = 1.7f;
        public float rotationSnapTime = 0.3f;
        [Min(0.05f)] public float collisionRadius = 0.3f;
        public LayerMask obstacleLayers = ~0;
        private static readonly float[] escapeAngles={45f,-45f,90f,-90f,135f,-135f,180f};
        private readonly RaycastHit[] hits = new RaycastHit[64];
        private float yaw, yawVelocity;
        private Vector3 focus;
        private CycleRecovery recovery;

        private void Start()
        {
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                var bicycle = FindFirstObjectByType<BicycleController>();
                if (bicycle != null) target = bicycle.transform;
            }
            if (target == null) { enabled = false; return; }
            GetComponent<Camera>().nearClipPlane = 0.1f;
            // The imported map replaced the geometry used by the old scene bake.
            GetComponent<Camera>().useOcclusionCulling = false;
            SetTarget(target);
        }

        public void SetTarget(Transform next)
        {
            if(recovery!=null) recovery.Respawned-=Snap;
            RestoreRider();target=next;if(FirstPerson)HideRider();
            recovery=target!=null?target.GetComponent<CycleRecovery>():null;
            if(recovery!=null) recovery.Respawned+=Snap;
            Snap();
        }

        public void Snap()
        {
            if (target == null) return;
            yaw = target.eulerAngles.y;
            yawVelocity = 0;
            focus = target.position + Vector3.up * lookAtHeight;
            PositionCamera();
        }

        private void LateUpdate()
        {
            if (target == null || Time.timeScale == 0) return;
            yaw = Mathf.SmoothDampAngle(yaw, target.eulerAngles.y, ref yawVelocity, rotationSnapTime);
            focus = Vector3.Lerp(focus, target.position + Vector3.up * lookAtHeight,
                1f - Mathf.Exp(-heightDamping * Time.deltaTime));
            PositionCamera();
        }

        private void PositionCamera()
        {
            if(FirstPerson) {
                var heading=Quaternion.Euler(0,yaw,0);
                transform.position=target.position+Vector3.up*1.65f+heading*Vector3.forward*.4f;
                transform.rotation=heading;return;
            }
            Vector3 offset = Quaternion.Euler(0, yaw, 0) * new Vector3(0, height - lookAtHeight, -distance);
            float length = offset.magnitude;
            if (length < 0.01f) return;
            Vector3 direction = offset / length;
            float allowedDistance = Clearance(direction,length);
            // Tight corners should offer a side view instead of collapsing to a few centimetres.
            if(allowedDistance<1.5f){
                foreach(float angle in escapeAngles){
                    Vector3 candidate=Quaternion.AngleAxis(angle,Vector3.up)*(offset/length);
                    float clear=Clearance(candidate,length);
                    if(clear>allowedDistance+.1f){direction=candidate;allowedDistance=clear;}
                    if(allowedDistance>=length*.8f)break;
                }
            }
            transform.position = focus + direction * allowedDistance;
            transform.rotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);
        }
        private float Clearance(Vector3 direction,float length)
        {
            int count=Physics.SphereCastNonAlloc(focus,collisionRadius,direction,hits,length,obstacleLayers,QueryTriggerInteraction.Ignore);
            float result=length;
            for(int i=0;i<count;i++){
                if(hits[i].transform.IsChildOf(target))continue;
                result=Mathf.Min(result,Mathf.Max(.05f,hits[i].distance-.05f));
            }
            return result;
        }

        private void OnDestroy() { RestoreRider(); if (recovery != null) recovery.Respawned -= Snap; }
    }
}

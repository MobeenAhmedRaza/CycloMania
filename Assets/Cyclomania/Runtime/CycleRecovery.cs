using System;
using SBPScripts;
using UnityEngine;

namespace Cyclomania
{
    [DisallowMultipleComponent, RequireComponent(typeof(BicycleController))]
    public sealed class CycleRecovery : MonoBehaviour
    {
        [Min(1)] public float crashImpactSpeed = 9f;
        [Min(0.1f)] public float recoveryDelay = 2f;
        public bool IsReady => bodies!=null;
        public Vector3 CheckpointPosition => spawnPosition;
        public float CheckpointHeading => spawnRotation.eulerAngles.y;
        public bool SetCheckpoint(Vector3 position,float heading)
        {
            if(float.IsNaN(position.sqrMagnitude) || float.IsInfinity(position.sqrMagnitude) || float.IsNaN(heading) || float.IsInfinity(heading)) return false;
            spawnPosition=position;spawnRotation=Quaternion.Euler(0,heading,0);return true;
        }
        public bool IsCrashed { get; private set; }
        public event Action Crashed;
        public event Action Respawned;
        private BicycleController bicycle;
        private Rigidbody[] bodies;
        private Vector3[] localPositions;
        private Quaternion[] localRotations;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private float recoveryTime, protectionTime, tippedTime;

        private void Start()
        {
            bicycle = GetComponent<BicycleController>();
            spawnPosition = transform.position;
            spawnRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
            bodies = GetComponentsInChildren<Rigidbody>(true);
            localPositions = new Vector3[bodies.Length];
            localRotations = new Quaternion[bodies.Length];
            for (int i = 0; i < bodies.Length; i++)
            {
                localPositions[i] = Quaternion.Inverse(spawnRotation) * (bodies[i].position - spawnPosition);
                localRotations[i] = Quaternion.Inverse(spawnRotation) * bodies[i].rotation;
                var relay = bodies[i].gameObject.AddComponent<CycleImpactRelay>();
                relay.Owner = this;
            }
            // This component owns recovery; don't also run the asset's ragdoll/dismount flow.
            var legacyStatus = GetComponent<BicycleStatus>();
            if (legacyStatus != null) { legacyStatus.StopAllCoroutines(); legacyStatus.enabled = false; }
            protectionTime = 1.5f;
        }

        private void Update()
        {
            if (Time.timeScale == 0) return;
            if (Input.GetKeyDown(KeyCode.R)) Respawn();
        }

        private void FixedUpdate()
        {
            protectionTime = Mathf.Max(0, protectionTime - Time.fixedDeltaTime);
            if (IsCrashed)
            {
                recoveryTime -= Time.fixedDeltaTime;
                if (recoveryTime <= 0) Respawn();
                return;
            }
            if (transform.position.y < spawnPosition.y - 40f) { Crash(); return; }
            bool tipped = Vector3.Dot(transform.up, Vector3.up) < 0.3f;
            tippedTime = tipped && bicycle.IsGrounded ? tippedTime + Time.fixedDeltaTime : 0;
            if (tippedTime > 0.6f) Crash();
        }

        public void ReportImpact(Collision collision)
        {
            if (IsCrashed || protectionTime > 0 || collision.transform.IsChildOf(transform)) return;
            float closingSpeed = 0;
            for (int i = 0; i < collision.contactCount; i++)
            {
                Vector3 normal = collision.GetContact(i).normal;
                // A frame-first upside-down landing is a crash even at low impact speed.
                if (normal.y > .35f && Vector3.Dot(bicycle.rb != null ? bicycle.rb.rotation * Vector3.up : transform.up, normal) < .25f) { Crash(); return; }
                closingSpeed = Mathf.Max(closingSpeed, Mathf.Abs(Vector3.Dot(collision.relativeVelocity, normal)));
            }
            if (closingSpeed >= crashImpactSpeed) Crash();
        }

        public void Crash()
        {
            if (IsCrashed || bicycle == null) return;
            IsCrashed = true;
            recoveryTime = recoveryDelay;
            bicycle.SetSuspended(true);
            Crashed?.Invoke();
        }

        // Reset the entire jointed assembly, not only the frame.
        // Public entry point for Part 3's reset button and later mission restarts.
        public void Respawn()
        {
            if (bodies == null) return;
            for (int i = 0; i < bodies.Length; i++) bodies[i].isKinematic = true;
            Vector3 position = spawnPosition + Vector3.up * 0.15f;
            transform.SetPositionAndRotation(position, spawnRotation);
            for (int i = 0; i < bodies.Length; i++)
            {
                bodies[i].position = position + spawnRotation * localPositions[i];
                bodies[i].rotation = spawnRotation * localRotations[i];
            }
            Physics.SyncTransforms();
            for (int i = 0; i < bodies.Length; i++)
            {
                bodies[i].isKinematic = false;
                bodies[i].linearVelocity = Vector3.zero;
                bodies[i].angularVelocity = Vector3.zero;
                bodies[i].WakeUp();
            }
            IsCrashed = false;
            protectionTime = 1.5f;
            tippedTime = 0;
            bicycle.SetSuspended(false);
            Respawned?.Invoke();
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Please use using SBPScripts; directive to refer to or append the SBP library
namespace SBPScripts
{
    // Cycle Geometry Class - Holds Gameobjects pertaining to the specific bicycle
    [System.Serializable]
    public class CycleGeometry
    {
        public GameObject handles, lowerFork, fWheelVisual, RWheel, crank, lPedal, rPedal, fGear, rGear;
    }
    //Pedal Adjustments Class - Manipulates pedals and their positioning.  
    [System.Serializable]
    public class PedalAdjustments
    {
        public float crankRadius;
        public Vector3 lPedalOffset, rPedalOffset;
        public float pedalingSpeed;
    }
    // Wheel Friction Settings Class - Uses Physics Materials and Physics functions to control the 
    // static / dynamic slipping of the wheels 
    [System.Serializable]
    public class WheelFrictionSettings
    {
        public PhysicsMaterial fPhysicMaterial, rPhysicMaterial;
        public Vector2 fFriction, rFriction;
    }
    // Way Point System Class - Replay Ghosting system
    [System.Serializable]
    public class WayPointSystem
    {
        public enum RecordingState { DoNothing, Record, Playback };
        public RecordingState recordingState = RecordingState.DoNothing;
        [Range(1, 10)]
        public int frameIncrement;
        [HideInInspector]
        public List<Vector3> bicyclePositionTransform;
        [HideInInspector]
        public List<Quaternion> bicycleRotationTransform;
        [HideInInspector]
        public List<Vector2Int> movementInstructionSet;
        [HideInInspector]
        public List<bool> sprintInstructionSet;
        [HideInInspector]
        public List<int> bHopInstructionSet;
    }
    [System.Serializable]
    public class AirTimeSettings
    {
        public bool freestyle;
        public float airTimeRotationSensitivity;
        [Range(0.5f, 10)]
        public float heightThreshold;
        public float groundSnapSensitivity;
    }

    [System.Serializable]
    public class MobileControls
    {
        public MobileButtonHandler forward;
        public MobileButtonHandler backward;
        public MobileButtonHandler right;
        public MobileButtonHandler left;
        [Space]
        public MobileButtonHandler sprint;
        public MobileButtonHandler bHop;
        [HideInInspector]
        public int prevBHopInput;
        public MobileButtonHandler wheelie;

    }
    public class BicycleController : MonoBehaviour
    {
        public CycleGeometry cycleGeometry;
        public GameObject fPhysicsWheel, rPhysicsWheel;
        public WheelFrictionSettings wheelFrictionSettings;
        // Curve of Power Exerted over Input time by the cyclist
        // This class sets the physics materials on to the
        // tires of the bicycle. F Friction pertains to the front tire friction and R Friction to
        // the rear. They are of the Vector2 type. X field edits the static friction
        // information and Y edits the dynamic friction. Please keep the values over 0.5.
        // For more information, please read the commented scripts.
        public AnimationCurve accelerationCurve;
        [Tooltip("Steer Angle over Speed")]
        public AnimationCurve steerAngle;
        public float axisAngle;
        // Defines the leaning curve of the bicycle
        public AnimationCurve leanCurve;
        // The slider refers to the ratio of Relaxed mode to Top Speed. 
        // Torque is a physics based function which acts as the actual wheel driving force.
        public float torque, topSpeed;
        [Range(0.1f, 0.9f)]
        [Tooltip("Ratio of Relaxed mode to Top Speed")]
        public float relaxedSpeed;
        public float reversingSpeed;
        public Vector3 centerOfMassOffset;
        [HideInInspector]
        public bool isReversing, isAirborne, stuntMode;
        // Controls Cycle sway from left to right.
        // The degree of cycle waddling side to side upon pedaling.
        // Higher values correspond to higher waddling. This property also affects
        // character IK. 

        [Range(0, 8)]
        public float oscillationAmount;
        // Following the natural movement of a cyclist, the
        // oscillation of the cycle from side to side also affects the steering to a certain
        // extent. This value refers to the counter steer upon cycle oscillation. Higher
        // values correspond to a higher percentage of the oscillation being transferred
        // to the steering handles. 

        [Range(0, 1)]
        public float oscillationAffectSteerRatio;
        float oscillationSteerEffect;
        [HideInInspector]
        public float cycleOscillation;
        [HideInInspector]
        public Rigidbody rb, fWheelRb, rWheelRb;
        float turnAngle;
        float xQuat, zQuat;
        [HideInInspector]
        public float crankSpeed, crankCurrentQuat, crankLastQuat, restingCrank;
        public PedalAdjustments pedalAdjustments;
        [HideInInspector]
        public float turnLeanAmount;
        RaycastHit hit;
        [HideInInspector]
        public float customSteerAxis, customLeanAxis, customAccelerationAxis, rawCustomAccelerationAxis;
        bool isRaw, sprint;
        [HideInInspector]
        public bool wheelieInput;
        [HideInInspector]
        public float wheeliePower;
        public bool wheelieToggle;
        [HideInInspector]
        public int bunnyHopInputState;
        [HideInInspector]
        public float currentTopSpeed, pickUpSpeed;
        Quaternion initialLowerForkLocalRotaion, initialHandlesRotation;
        ConfigurableJoint fPhysicsWheelConfigJoint, rPhysicsWheelConfigJoint;
        // Ground Conformity refers to vehicles that do not need a gyroscopic force to keep them upright.
        // For non-gyroscopic wheel systems like the tricycle,
        // enabling ground conformity ensures that the tricycle is not always upright and
        // follows the curvature of the terrain. 
        public bool groundConformity;
        RaycastHit hitGround;
        Vector3 theRay;
        float groundZ;
        JointDrive fDrive, rYDrive, rZDrive;
        // Attempts to Reduce/eliminate bouncing of the bicycle after a fall impact 
        public bool inelasticCollision;
        [HideInInspector]
        public Vector3 lastVelocity, deceleration, lastDeceleration;
        int impactFrames;
        bool isBunnyHopping;
        [HideInInspector]
        public float bunnyHopAmount;
        // The upward force the rider can bunny hop with. 
        public float bunnyHopStrength;
        public WayPointSystem wayPointSystem;
        public AirTimeSettings airTimeSettings;
        public MobileControls mobileControls;

        [Header("Cyclomania core riding")]
        [Min(0)] public float brakeDeceleration = 7f;
        [Min(0)] public float rollingResistance = 0.12f;
        public bool IsGrounded { get; private set; }
        public bool FrontGrounded { get; private set; }
        public bool RearGrounded => rearGrounded;
        public bool IsSuspended => suspended;
        [Header("Stunt assistance")]
        public float wheelieAngle = 35f;
        public float stoppieAngle = 28f;
        public float airFlipSpeed = 7.5f;
        public float balanceAssist = 8f;
        private float queuedHop;
        private bool chargingHop;
        public Vector3 GroundNormal { get; private set; } = Vector3.up;
        public float SpeedKmh => rb == null ? 0f : rb.linearVelocity.magnitude * 3.6f;
        private readonly RaycastHit[] groundHits = new RaycastHit[32];
        private SphereCollider frontCollider, rearCollider;
        private bool rearGrounded;
        private bool suspended;
        private int hopCancellationVersion;

        void Awake()
        {
            transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
        }

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            rb.maxAngularVelocity = 20f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            frontCollider = fPhysicsWheel.GetComponent<SphereCollider>();
            rearCollider = rPhysicsWheel.GetComponent<SphereCollider>();

            fWheelRb = fPhysicsWheel.GetComponent<Rigidbody>();
            fWheelRb.maxAngularVelocity = Mathf.Infinity;

            rWheelRb = rPhysicsWheel.GetComponent<Rigidbody>();
            rWheelRb.maxAngularVelocity = Mathf.Infinity;

            currentTopSpeed = topSpeed * relaxedSpeed;
            // Materials belong to this bicycle, not to the imported shared asset.
            wheelFrictionSettings.fPhysicMaterial = Instantiate(wheelFrictionSettings.fPhysicMaterial);
            wheelFrictionSettings.rPhysicMaterial = Instantiate(wheelFrictionSettings.rPhysicMaterial);
            frontCollider.sharedMaterial = wheelFrictionSettings.fPhysicMaterial;
            rearCollider.sharedMaterial = wheelFrictionSettings.rPhysicMaterial;
            fWheelRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rWheelRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            initialHandlesRotation = cycleGeometry.handles.transform.localRotation;
            initialLowerForkLocalRotaion = cycleGeometry.lowerFork.transform.localRotation;

            fPhysicsWheelConfigJoint = fPhysicsWheel.GetComponent<ConfigurableJoint>();
            rPhysicsWheelConfigJoint = rPhysicsWheel.GetComponent<ConfigurableJoint>();

            //Recording is set to 0 to remove the recording previous data if not set to playback
            if (wayPointSystem.recordingState == WayPointSystem.RecordingState.Record || wayPointSystem.recordingState == WayPointSystem.RecordingState.DoNothing)
            {
                wayPointSystem.bicyclePositionTransform.Clear();
                wayPointSystem.bicycleRotationTransform.Clear();
                wayPointSystem.movementInstructionSet.Clear();
                wayPointSystem.sprintInstructionSet.Clear();
                wayPointSystem.bHopInstructionSet.Clear();
            }

            //Mobile Controls
            // Unity's GameObject.Find(string s) is implemented for convinience of the developer. 
            // You may choose to drag the Objects in the inspector for referencing.
            if (mobileControls.forward == null) mobileControls.forward = GameObject.Find("Forward")?.GetComponent<MobileButtonHandler>();
            if (mobileControls.backward == null) mobileControls.backward = GameObject.Find("Backward")?.GetComponent<MobileButtonHandler>();
            if (mobileControls.left == null) mobileControls.left = GameObject.Find("Left")?.GetComponent<MobileButtonHandler>();
            if (mobileControls.right == null) mobileControls.right = GameObject.Find("Right")?.GetComponent<MobileButtonHandler>();
            if (mobileControls.sprint == null) mobileControls.sprint = GameObject.Find("Sprint")?.GetComponent<MobileButtonHandler>();
            if (mobileControls.bHop == null) mobileControls.bHop = GameObject.Find("BHop")?.GetComponent<MobileButtonHandler>();
            if (mobileControls.wheelie == null) mobileControls.wheelie = GameObject.Find("Wheelie")?.GetComponent<MobileButtonHandler>();
        }

        void FixedUpdate()
        {

            if (suspended) return;
            UpdateGroundContact();
            Vector3 physicsForward = rb.rotation * Vector3.forward;
            Vector3 physicsRight = rb.rotation * Vector3.right;
            if (queuedHop > 0)
            {
                if (IsGrounded)
                {
                    Vector3 lift = Vector3.up * queuedHop * bunnyHopStrength;
                    rb.AddForce(lift, ForceMode.VelocityChange);
                    fWheelRb.AddForce(lift, ForceMode.VelocityChange);
                    rWheelRb.AddForce(lift, ForceMode.VelocityChange);
                }
                queuedHop = 0;
            }
            // Steering stays aligned with the bicycle on slopes.
            fWheelRb.MoveRotation(rb.rotation * Quaternion.Euler(0,
                customSteerAxis * steerAngle.Evaluate(rb.linearVelocity.magnitude) + oscillationSteerEffect, 0));
            fPhysicsWheelConfigJoint.axis = new Vector3(1, 0, 0);

            //Power Control. Wheel Torque + Acceleration curves

            //cache rb velocity
            float currentSpeed = rb.linearVelocity.magnitude;

            if (!sprint)
                currentTopSpeed = Mathf.Lerp(currentTopSpeed, topSpeed * relaxedSpeed, Time.deltaTime);
            else
                currentTopSpeed = Mathf.Lerp(currentTopSpeed, topSpeed, Time.deltaTime);

            bool braking = rawCustomAccelerationAxis < 0;
            Vector3 travel = Vector3.ProjectOnPlane(physicsForward, GroundNormal).normalized;
            if (!braking && rearGrounded && currentSpeed < currentTopSpeed && rawCustomAccelerationAxis > 0)
            {
                rWheelRb.AddTorque(physicsRight * torque * Mathf.Max(0, customAccelerationAxis));
                if (!isBunnyHopping)
                    rb.AddForce(travel * accelerationCurve.Evaluate(Mathf.Max(0, customAccelerationAxis)));
            }
            isReversing = Vector3.Dot(rb.linearVelocity, physicsForward) < -0.1f;
            if (IsGrounded)
            {
                float resistance = braking ? brakeDeceleration : rollingResistance;
                ApplyResistance(rb, resistance);
                ApplyResistance(fWheelRb, resistance);
                ApplyResistance(rWheelRb, resistance);
                if (braking)
                {
                    fWheelRb.angularVelocity = Vector3.MoveTowards(fWheelRb.angularVelocity, Vector3.zero, 100f * Time.fixedDeltaTime);
                    rWheelRb.angularVelocity = Vector3.MoveTowards(rWheelRb.angularVelocity, Vector3.zero, 100f * Time.fixedDeltaTime);
                }
            }

            // Center of Mass handling
            if (stuntMode)
                rb.centerOfMass = GetComponent<BoxCollider>().center;
            else
                rb.centerOfMass = Vector3.zero + centerOfMassOffset;

            //Handles
            cycleGeometry.handles.transform.localRotation = Quaternion.Euler(0, customSteerAxis * steerAngle.Evaluate(currentSpeed) + oscillationSteerEffect * 5, 0) * initialHandlesRotation;

            //LowerFork
            cycleGeometry.lowerFork.transform.localRotation = Quaternion.Euler(0, customSteerAxis * steerAngle.Evaluate(currentSpeed) + oscillationSteerEffect * 5, customSteerAxis * -axisAngle) * initialLowerForkLocalRotaion;

            //FWheelVisual
            xQuat = Mathf.Sin(Mathf.Deg2Rad * (transform.rotation.eulerAngles.y));
            zQuat = Mathf.Cos(Mathf.Deg2Rad * (transform.rotation.eulerAngles.y));
            cycleGeometry.fWheelVisual.transform.rotation = Quaternion.Euler(xQuat * (customSteerAxis * -axisAngle), customSteerAxis * steerAngle.Evaluate(currentSpeed) + oscillationSteerEffect * 5, zQuat * (customSteerAxis * -axisAngle));
            cycleGeometry.fWheelVisual.transform.GetChild(0).transform.localRotation = cycleGeometry.RWheel.transform.rotation;

            //Crank
            crankCurrentQuat = cycleGeometry.RWheel.transform.rotation.eulerAngles.x;
            if (customAccelerationAxis > 0 && !isAirborne && !isBunnyHopping)
            {
                crankSpeed += Mathf.Sqrt(customAccelerationAxis * Mathf.Abs(Mathf.DeltaAngle(crankCurrentQuat, crankLastQuat) * pedalAdjustments.pedalingSpeed));
                crankSpeed %= 360;
            }
            else if (Mathf.Floor(crankSpeed) > restingCrank)
                crankSpeed += -6;
            else if (Mathf.Floor(crankSpeed) < restingCrank)
                crankSpeed = Mathf.Lerp(crankSpeed, restingCrank, Time.deltaTime * 5);

            crankLastQuat = crankCurrentQuat;
            cycleGeometry.crank.transform.localRotation = Quaternion.Euler(crankSpeed, 0, 0);

            //Pedals
            cycleGeometry.lPedal.transform.localPosition = pedalAdjustments.lPedalOffset + new Vector3(0, Mathf.Cos(Mathf.Deg2Rad * (crankSpeed + 180)) * pedalAdjustments.crankRadius, Mathf.Sin(Mathf.Deg2Rad * (crankSpeed + 180)) * pedalAdjustments.crankRadius);
            cycleGeometry.rPedal.transform.localPosition = pedalAdjustments.rPedalOffset + new Vector3(0, Mathf.Cos(Mathf.Deg2Rad * (crankSpeed)) * pedalAdjustments.crankRadius, Mathf.Sin(Mathf.Deg2Rad * (crankSpeed)) * pedalAdjustments.crankRadius);

            //FGear
            if (cycleGeometry.fGear != null)
                cycleGeometry.fGear.transform.rotation = cycleGeometry.crank.transform.rotation;
            //RGear
            if (cycleGeometry.rGear != null)
                cycleGeometry.rGear.transform.rotation = rPhysicsWheel.transform.rotation;

            //CycleOscillation
            if ((sprint && currentSpeed > 5 && isReversing == false) || isAirborne || isBunnyHopping)
                pickUpSpeed += Time.deltaTime * 2;
            else
                pickUpSpeed -= Time.deltaTime * 2;

            pickUpSpeed = Mathf.Clamp(pickUpSpeed, 0.1f, 1);

            cycleOscillation = -Mathf.Sin(Mathf.Deg2Rad * (crankSpeed + 90)) * (oscillationAmount * (Mathf.Clamp(currentTopSpeed / Mathf.Max(currentSpeed, 0.1f), 1f, 1.5f))) * pickUpSpeed;
            turnLeanAmount = -leanCurve.Evaluate(customLeanAxis) * Mathf.Clamp(currentSpeed * 0.1f, 0, 1);
            oscillationSteerEffect = cycleOscillation * Mathf.Clamp01(customAccelerationAxis) * (oscillationAffectSteerRatio * (Mathf.Clamp(topSpeed / Mathf.Max(currentSpeed, 0.1f), 1f, 1.5f)));

            //FrictionSettings
            wheelFrictionSettings.fPhysicMaterial.staticFriction = wheelFrictionSettings.fFriction.x;
            wheelFrictionSettings.fPhysicMaterial.dynamicFriction = wheelFrictionSettings.fFriction.y;
            wheelFrictionSettings.rPhysicMaterial.staticFriction = wheelFrictionSettings.rFriction.x;
            wheelFrictionSettings.rPhysicMaterial.dynamicFriction = wheelFrictionSettings.rFriction.y;

            if (Physics.Raycast(fPhysicsWheel.transform.position, Vector3.down, out hit, Mathf.Infinity))
                if (hit.distance < 0.5f)
                {
                    Vector3 velf = fPhysicsWheel.transform.InverseTransformDirection(fWheelRb.linearVelocity);
                    velf.x *= Mathf.Clamp01(1 / (wheelFrictionSettings.fFriction.x + wheelFrictionSettings.fFriction.y));
                    fWheelRb.linearVelocity = fPhysicsWheel.transform.TransformDirection(velf);
                }
            if (Physics.Raycast(rPhysicsWheel.transform.position, Vector3.down, out hit, Mathf.Infinity))
                if (hit.distance < 0.5f)
                {
                    Vector3 velr = rPhysicsWheel.transform.InverseTransformDirection(rWheelRb.linearVelocity);
                    velr.x *= Mathf.Clamp01(1 / (wheelFrictionSettings.rFriction.x + wheelFrictionSettings.rFriction.y));
                    rWheelRb.linearVelocity = rPhysicsWheel.transform.TransformDirection(velr);
                }

            //Impact sensing
            deceleration = (fWheelRb.linearVelocity - lastVelocity) / Time.fixedDeltaTime;
            lastVelocity = fWheelRb.linearVelocity;
            impactFrames--;
            impactFrames = Mathf.Clamp(impactFrames, 0, 15);
            if (deceleration.y > 200 && lastDeceleration.y < -1)
                impactFrames = 30;

            lastDeceleration = deceleration;

            if (impactFrames > 0 && inelasticCollision)
            {
                fWheelRb.linearVelocity = new Vector3(fWheelRb.linearVelocity.x, -Mathf.Abs(fWheelRb.linearVelocity.y), fWheelRb.linearVelocity.z);
                rWheelRb.linearVelocity = new Vector3(rWheelRb.linearVelocity.x, -Mathf.Abs(rWheelRb.linearVelocity.y), rWheelRb.linearVelocity.z);
            }

            // Gravity and joint contacts handle pitch on slopes and free flight.
            // Assisted balance only operates while a wheel touches rideable ground.
            stuntMode = isAirborne && airTimeSettings.freestyle;
            restingCrank = isAirborne || isBunnyHopping ? 100 : 10;
            bool stuntInput = wheelieToggle && wheelieInput && rawCustomAccelerationAxis != 0;
            if (IsGrounded && (!stuntInput || currentSpeed <= .5f) && !isBunnyHopping)
            {
                Vector3 forward = Vector3.ProjectOnPlane(physicsForward, GroundNormal);
                if (forward.sqrMagnitude > 0.01f)
                {
                    Quaternion balanced = Quaternion.LookRotation(forward, GroundNormal)
                        * Quaternion.AngleAxis(turnLeanAmount + cycleOscillation, Vector3.forward);
                    rb.MoveRotation(ApplyGroundSteering(Quaternion.Slerp(rb.rotation, balanced, 1f - Mathf.Exp(-balanceAssist * Time.fixedDeltaTime))));
                }
            }

            // Assisted pitch for grounded one-wheel tricks; free pitch control in the air.
            if (stuntInput && IsGrounded && currentSpeed > .5f)
            {
                float targetPitch = rawCustomAccelerationAxis > 0 ? -wheelieAngle : stoppieAngle;
                Vector3 forward = Vector3.ProjectOnPlane(physicsForward, GroundNormal).normalized;
                if (forward.sqrMagnitude > .01f)
                {
                    Quaternion target = Quaternion.LookRotation(forward, GroundNormal) * Quaternion.Euler(targetPitch,0,turnLeanAmount);
                    Quaternion error = target * Quaternion.Inverse(rb.rotation);
                    error.ToAngleAxis(out float angle, out Vector3 axis);
                    if (angle > 180) angle -= 360;
                    Vector3 correction = axis * (angle * Mathf.Deg2Rad * 40f) - rb.angularVelocity * 8f;
                    rb.AddTorque(Vector3.ClampMagnitude(correction,40f),ForceMode.Acceleration);
                }
                wheeliePower = rawCustomAccelerationAxis > 0 ? 150 : 0;
                rb.angularDamping = 1;
            }
            else if (stuntInput && !IsGrounded)
            {
                float desiredRate = -Mathf.Sign(rawCustomAccelerationAxis) * airFlipSpeed;
                float rate = Vector3.Dot(rb.angularVelocity,physicsRight);
                rb.AddTorque(physicsRight * Mathf.Clamp((desiredRate-rate)*10,-60,60),ForceMode.Acceleration);
                rb.angularDamping = .15f;
                wheeliePower = 0;
            }
            else { rb.angularDamping = 1; wheeliePower = 0; }


        }
        void Update()
        {
            if (suspended || Time.timeScale == 0) { ClearInput(); return; }
            ApplyCustomInput();

            // Charge on the ground; consume the impulse once in FixedUpdate for all three bodies.
            if (bunnyHopInputState == 1 && IsGrounded) chargingHop = true;
            if (bunnyHopInputState == 1 && chargingHop)
            {
                isBunnyHopping = true;
                bunnyHopAmount = Mathf.Clamp01(bunnyHopAmount + Time.deltaTime * 2f);
            }
            if (bunnyHopInputState == -1)
            {
                if (chargingHop && IsGrounded) queuedHop = Mathf.Max(.25f,bunnyHopAmount);
                chargingHop = isBunnyHopping = false;
                bunnyHopAmount = 0;
            }

        }
        float GroundConformity(bool toggle)
        {
            if (toggle)
            {
                groundZ = transform.rotation.eulerAngles.z;
            }
            return groundZ;

        }

void ApplyCustomInput()
        {
            if (wayPointSystem.recordingState == WayPointSystem.RecordingState.DoNothing || wayPointSystem.recordingState == WayPointSystem.RecordingState.Record)
            {
                float steer = Input.GetAxisRaw("Horizontal");
                float pedal = Input.GetAxisRaw("Vertical");
                float touchSteer = Button(mobileControls.right) + Button(mobileControls.left);
                float touchPedal = Button(mobileControls.forward) + Button(mobileControls.backward);
                if (touchSteer != 0) steer = touchSteer;
                if (touchPedal != 0) pedal = touchPedal;
                // Braking wins if pedal and brake are held together.
                if (Button(mobileControls.backward) < 0 || Input.GetAxisRaw("Vertical") < 0) pedal = -1;
                rawCustomAccelerationAxis = Mathf.Clamp(pedal, -1, 1);
                customAccelerationAxis = Mathf.MoveTowards(customAccelerationAxis, rawCustomAccelerationAxis, Time.deltaTime * 2f);
                customSteerAxis = Mathf.MoveTowards(customSteerAxis, Mathf.Clamp(steer, -1, 1), Time.deltaTime * 12f);
                customLeanAxis = Mathf.MoveTowards(customLeanAxis, Mathf.Clamp(steer, -1, 1), Time.deltaTime * 2f);
                sprint = Input.GetKey(KeyCode.LeftShift) || Button(mobileControls.sprint) > 0;
                wheelieInput = Input.GetKey(KeyCode.LeftControl) || Button(mobileControls.wheelie) > 0;
                if (mobileControls.bHop != null && hopCancellationVersion != mobileControls.bHop.CancellationVersion)
                {
                    hopCancellationVersion = mobileControls.bHop.CancellationVersion;
                    mobileControls.prevBHopInput = 0;
                    isBunnyHopping = chargingHop = false;
                    queuedHop = bunnyHopAmount = 0;
                }
                int hop = Input.GetKey(KeyCode.Space) || Button(mobileControls.bHop) > 0 ? 1 : 0;
                bunnyHopInputState = mobileControls.prevBHopInput == 1 && hop == 0 ? -1 : hop;
                mobileControls.prevBHopInput = hop;

                //Record
                if (wayPointSystem.recordingState == WayPointSystem.RecordingState.Record)
                {
                    if (Time.frameCount % wayPointSystem.frameIncrement == 0)
                    {
                        wayPointSystem.bicyclePositionTransform.Add(new Vector3(Mathf.Round(transform.position.x * 100f) * 0.01f, Mathf.Round(transform.position.y * 100f) * 0.01f, Mathf.Round(transform.position.z * 100f) * 0.01f));
                        wayPointSystem.bicycleRotationTransform.Add(transform.rotation);
                        wayPointSystem.movementInstructionSet.Add(new Vector2Int((int)Input.GetAxisRaw("Horizontal"), (int)Input.GetAxisRaw("Vertical")));
                        wayPointSystem.sprintInstructionSet.Add(sprint);
                        wayPointSystem.bHopInstructionSet.Add(bunnyHopInputState);
                    }
                }
            }

            else
            {
                if (wayPointSystem.recordingState == WayPointSystem.RecordingState.Playback)
                {
                    if (wayPointSystem.movementInstructionSet.Count - 1 > Time.frameCount / wayPointSystem.frameIncrement)
                    {
                        transform.position = Vector3.Lerp(transform.position, wayPointSystem.bicyclePositionTransform[Time.frameCount / wayPointSystem.frameIncrement], Time.deltaTime * wayPointSystem.frameIncrement);
                        transform.rotation = Quaternion.Lerp(transform.rotation, wayPointSystem.bicycleRotationTransform[Time.frameCount / wayPointSystem.frameIncrement], Time.deltaTime * wayPointSystem.frameIncrement);
                        WayPointInput(wayPointSystem.movementInstructionSet[Time.frameCount / wayPointSystem.frameIncrement].x, ref customSteerAxis, 5, 5, false);
                        WayPointInput(wayPointSystem.movementInstructionSet[Time.frameCount / wayPointSystem.frameIncrement].y, ref customAccelerationAxis, 1, 1, false);
                        WayPointInput(wayPointSystem.movementInstructionSet[Time.frameCount / wayPointSystem.frameIncrement].x, ref customLeanAxis, 1, 1, false);
                        WayPointInput(wayPointSystem.movementInstructionSet[Time.frameCount / wayPointSystem.frameIncrement].y, ref rawCustomAccelerationAxis, 1, 1, true);
                        sprint = wayPointSystem.sprintInstructionSet[Time.frameCount / wayPointSystem.frameIncrement];
                        bunnyHopInputState = wayPointSystem.bHopInstructionSet[Time.frameCount / wayPointSystem.frameIncrement];
                    }
                }
            }
        }

        private static int Button(MobileButtonHandler button) => button == null ? 0 : button.buttonPressed;

        private void UpdateGroundContact()
        {
            bool front = ProbeWheel(frontCollider, out Vector3 fn);
            FrontGrounded = front;
            rearGrounded = ProbeWheel(rearCollider, out Vector3 rn);
            IsGrounded = front || rearGrounded;
            GroundNormal = front && rearGrounded ? (fn + rn).normalized : front ? fn : rearGrounded ? rn : Vector3.up;
            isAirborne = !IsGrounded;
        }

        private bool ProbeWheel(SphereCollider wheel, out Vector3 normal)
        {
            normal = Vector3.up;
            if (wheel == null || !wheel.enabled) return false;
            float radius = wheel.radius * Mathf.Max(Mathf.Abs(wheel.transform.lossyScale.x), Mathf.Abs(wheel.transform.lossyScale.y), Mathf.Abs(wheel.transform.lossyScale.z));
            // Rigidbody poses are current in physics ticks; interpolated Transform poses can lag after a reset.
            Rigidbody wheelBody = wheel.attachedRigidbody;
            Vector3 center = wheelBody != null
                ? wheelBody.position + wheelBody.rotation * Vector3.Scale(wheel.center, wheel.transform.lossyScale)
                : wheel.transform.TransformPoint(wheel.center);
            Vector3 origin = center + Vector3.up * 0.1f;
            int count = Physics.RaycastNonAlloc(origin, Vector3.down, groundHits, radius + 0.22f, ~0, QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var contact = groundHits[i];
                if (contact.transform.IsChildOf(transform) || contact.normal.y < 0.35f || contact.distance >= nearest) continue;
                nearest = contact.distance;
                normal = contact.normal;
            }
            return !float.IsPositiveInfinity(nearest);
        }

        // Arcade ground grip: steer the heading and velocity together without adding speed.
        // Called only from grounded balance, never during free flight or pitch tricks.
        private Quaternion ApplyGroundSteering(Quaternion balanced)
        {
            Vector3 planar=Vector3.ProjectOnPlane(rb.linearVelocity,GroundNormal);
            float speed=planar.magnitude;
            if(speed<.15f)return balanced;
            float wheelbase=Mathf.Max(.8f,Vector3.Distance(fWheelRb.position,rWheelRb.position));
            float angle=Mathf.Clamp(steerAngle.Evaluate(speed),0,45)*Mathf.Clamp(customSteerAxis,-1,1)*Mathf.Deg2Rad;
            float rate=speed/wheelbase*Mathf.Tan(angle)*Mathf.Rad2Deg;
            float limit=Mathf.Lerp(100,55,Mathf.Clamp01(speed/Mathf.Max(1,topSpeed)));
            float sign=Vector3.Dot(planar,rb.rotation*Vector3.forward)<0?-1:1;
            Quaternion turn=Quaternion.AngleAxis(Mathf.Clamp(rate,-limit,limit)*sign*Time.fixedDeltaTime,GroundNormal);
            Quaternion result=turn*balanced;
            Vector3 heading=Vector3.ProjectOnPlane(result*Vector3.forward,GroundNormal).normalized*sign;
            SteerVelocity(rb,heading);SteerVelocity(fWheelRb,heading);SteerVelocity(rWheelRb,heading);
            // Contact torque must not leave an uncontrolled yaw after steering is released.
            rb.angularVelocity-=GroundNormal*Vector3.Dot(rb.angularVelocity,GroundNormal);
            return result;
        }
        private void SteerVelocity(Rigidbody body,Vector3 heading)
        {
            Vector3 normal=Vector3.Project(body.linearVelocity,GroundNormal);
            Vector3 planar=body.linearVelocity-normal;
            if(planar.sqrMagnitude<.0001f)return;
            Vector3 direction=Vector3.Slerp(planar.normalized,heading,1-Mathf.Exp(-18*Time.fixedDeltaTime)).normalized;
            body.linearVelocity=normal+direction*planar.magnitude;
        }
        private void ApplyResistance(Rigidbody body, float decelerationRate)
        {
            Vector3 velocity = Vector3.ProjectOnPlane(body.linearVelocity, GroundNormal);
            // Never overshoot through zero into reverse propulsion.
            float delta = Mathf.Min(velocity.magnitude, decelerationRate * Time.fixedDeltaTime);
            body.AddForce(-velocity.normalized * delta, ForceMode.VelocityChange);
        }

        public void ClearInput()
        {
            customAccelerationAxis = rawCustomAccelerationAxis = customSteerAxis = customLeanAxis = 0;
            wheelieInput = sprint = false;
            wheeliePower = bunnyHopAmount = queuedHop = 0;
            chargingHop = isBunnyHopping = false;
            bunnyHopInputState = mobileControls.prevBHopInput = 0;
            foreach (var button in new[] { mobileControls.forward, mobileControls.backward, mobileControls.left,
                mobileControls.right, mobileControls.sprint, mobileControls.bHop, mobileControls.wheelie })
                if (button != null) button.ReleaseAll();
        }

        public void SetSuspended(bool value)
        {
            suspended = value;
            StopAllCoroutines();
            ClearInput();
            isBunnyHopping = false;
            impactFrames = 0;
            lastVelocity = lastDeceleration = deceleration = Vector3.zero;
            if (!value) { UpdateGroundContact(); currentTopSpeed = topSpeed * relaxedSpeed; }
        }

        private void OnApplicationFocus(bool focused) { if (!focused) ClearInput(); }
        private void OnApplicationPause(bool paused) { if (paused) ClearInput(); }
        private void OnDestroy()
        {
            if (frontCollider == null) return; // Start never ran.
            Destroy(wheelFrictionSettings.fPhysicMaterial);
            Destroy(wheelFrictionSettings.rPhysicMaterial);
        }

        //Input Manager Controls
        float CustomInput(string name, ref float axis, float sensitivity, float gravity, bool isRaw)
        {
            var r = Input.GetAxisRaw(name);
            var s = sensitivity;
            var g = gravity;
            var t = Time.unscaledDeltaTime;

            if (isRaw)
                axis = r;
            else
            {
                if (r != 0)
                    axis = Mathf.Clamp(axis + r * s * t, -1f, 1f);
                else
                    axis = Mathf.Clamp01(Mathf.Abs(axis) - g * t) * Mathf.Sign(axis);
            }

            return axis;
        }

        float WayPointInput(float instruction, ref float axis, float sensitivity, float gravity, bool isRaw)
        {
            var r = instruction;
            var s = sensitivity;
            var g = gravity;
            var t = Time.unscaledDeltaTime;

            if (isRaw)
                axis = r;
            else
            {
                if (r != 0)
                    axis = Mathf.Clamp(axis + r * s * t, -1f, 1f);
                else
                    axis = Mathf.Clamp01(Mathf.Abs(axis) - g * t) * Mathf.Sign(axis);
            }

            return axis;
        }

        float MobileInput(float instruction, ref float axis, float sensitivity, float gravity, bool isRaw)
        {
            var r = instruction * 2;
            var s = sensitivity;
            var g = gravity;
            var t = Time.unscaledDeltaTime;

            if (isRaw)
                axis = r;
            else
            {
                if (r != 0)
                    axis = Mathf.Clamp(axis + r * s * t, -1f, 1f);
                else
                    axis = Mathf.Clamp01(Mathf.Abs(axis) - g * t) * Mathf.Sign(axis);
            }

            return axis;
        }

        IEnumerator DelayBunnyHop()
        {
            yield return new WaitForSeconds(0.5f);
            isBunnyHopping = false;
            yield return null;
        }

    }
}
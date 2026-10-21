using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace CollisionLab
{
    public enum SimulationState
    {
        Ready,
        InMotion,
        Collided
    }

    /// <summary>
    /// Controls the 3D interactive collision-physics laboratory for CGVR.
    /// Manages launch parameters, Rigidbody masses, coefficient of restitution,
    /// dynamic velocity vectors, collision manifold visualization, and vector momentum conservation analysis.
    /// </summary>
    public class CollisionLabManager : MonoBehaviour
    {
        public static CollisionLabManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<CollisionLabManager>();
                }
                return _instance;
            }
            private set => _instance = value;
        }
        private static CollisionLabManager _instance;

        /// <summary>
        /// Callback invoked immediately prior to Launch or Reset, allowing VR grab components
        /// to cleanly release held objects before physics velocity application or state reset.
        /// </summary>
        public static Action ReleaseHeldObjectsHandler;

        [Header("Physical Demonstration Objects")]
        [SerializeField] private CollisionObject sphereObject;
        [SerializeField] private CollisionObject cubeObject;
        [SerializeField] private Rigidbody sphereRigidbody;
        [SerializeField] private Rigidbody cubeRigidbody;

        [Header("3D Physics Visualizers")]
        [SerializeField] private PhysicsVectorVisualizer sphereVectorVisualizer;
        [SerializeField] private PhysicsVectorVisualizer cubeVectorVisualizer;
        [SerializeField] private ContactManifoldVisualizer contactVisualizer;

        [Header("Launch & Physics Parameters")]
        [SerializeField] private float launchVelocity = 8.0f;
        [SerializeField] private Vector3 launchDirection = new Vector3(1f, 0f, 0f);
        [SerializeField] private float sphereMass = 1.0f;
        [SerializeField] private float cubeMass = 1.0f;
        [SerializeField] private float restitution = 0.85f;

        [Header("Input Mode Configuration")]
        [Tooltip("Enable desktop keyboard shortcuts. Automatically disabled in VR scenes to prevent conflicts with XR Device Simulator keys.")]
        [SerializeField] private bool enableKeyboardShortcuts = true;

        [Header("Physics Configuration UI Sliders & Controls")]
        [SerializeField] private Slider speedSlider;
        [SerializeField] private Text speedValueText;
        [SerializeField] private Button speedDownButton;
        [SerializeField] private Button speedUpButton;

        [SerializeField] private Slider sphereMassSlider;
        [SerializeField] private Text sphereMassValueText;
        [SerializeField] private Button sphereMassDownButton;
        [SerializeField] private Button sphereMassUpButton;

        [SerializeField] private Slider cubeMassSlider;
        [SerializeField] private Text cubeMassValueText;
        [SerializeField] private Button cubeMassDownButton;
        [SerializeField] private Button cubeMassUpButton;

        [SerializeField] private Slider restitutionSlider;
        [SerializeField] private Text restitutionValueText;
        [SerializeField] private Button restitutionDownButton;
        [SerializeField] private Button restitutionUpButton;

        [Header("Physics Presets")]
        [SerializeField] private Button presetEqualButton;
        [SerializeField] private Button presetHeavyImpactorButton;
        [SerializeField] private Button presetHeavyTargetButton;
        [SerializeField] private Button presetElasticButton;
        [SerializeField] private Button presetInelasticButton;

        [Header("Telemetry UI Text Elements")]
        [SerializeField] private Text stateText;
        [SerializeField] private Text impactCountText;
        [SerializeField] private Text relativeVelocityText;
        [SerializeField] private Text impulseText;
        [SerializeField] private Text contactPointText;
        [SerializeField] private Text contactNormalText;
        [SerializeField] private Text momentumBeforeText;
        [SerializeField] private Text momentumAfterText;
        [SerializeField] private Text momentumErrorText;
        [SerializeField] private Text kineticEnergyText;
        [SerializeField] private Text measuredRestitutionText;

        [Header("UI Buttons & Panels")]
        [SerializeField] private Button launchButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Button telemetryButton;
        [SerializeField] private GameObject telemetryPanel;
        [SerializeField] private GameObject configPanel;

        public SimulationState CurrentState => _currentState;
        private SimulationState _currentState = SimulationState.Ready;
        private int _impactCount = 0;
        private bool _isVRScene = false;

        // Cached pre-impact physics velocities
        private Vector3 _preImpactSphereVel = Vector3.zero;
        private Vector3 _preImpactCubeVel = Vector3.zero;
        private Vector3 _lastFixedSphereVel = Vector3.zero;
        private Vector3 _lastFixedCubeVel = Vector3.zero;

        // Per-physics-event deduplication without artificial cooldowns
        private readonly Dictionary<int, int> _lastPhysicsTickByPair = new Dictionary<int, int>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            ConfigureVRSceneIntegration();
            // Wire buttons
            if (launchButton != null) launchButton.onClick.AddListener(LaunchSphere);
            if (resetButton != null) resetButton.onClick.AddListener(ResetSimulation);
            if (backButton != null) backButton.onClick.AddListener(ReturnToMainScene);
            if (telemetryButton != null) telemetryButton.onClick.AddListener(ToggleTelemetry);

            if (speedDownButton != null) speedDownButton.onClick.AddListener(() => AdjustSpeed(-1.0f));
            if (speedUpButton != null) speedUpButton.onClick.AddListener(() => AdjustSpeed(1.0f));

            if (sphereMassDownButton != null) sphereMassDownButton.onClick.AddListener(() => AdjustSphereMass(-0.5f));
            if (sphereMassUpButton != null) sphereMassUpButton.onClick.AddListener(() => AdjustSphereMass(0.5f));

            if (cubeMassDownButton != null) cubeMassDownButton.onClick.AddListener(() => AdjustCubeMass(-0.5f));
            if (cubeMassUpButton != null) cubeMassUpButton.onClick.AddListener(() => AdjustCubeMass(0.5f));

            if (restitutionDownButton != null) restitutionDownButton.onClick.AddListener(() => AdjustRestitution(-0.1f));
            if (restitutionUpButton != null) restitutionUpButton.onClick.AddListener(() => AdjustRestitution(0.1f));

            if (presetEqualButton != null) presetEqualButton.onClick.AddListener(() => ApplyPreset("Equal"));
            if (presetHeavyImpactorButton != null) presetHeavyImpactorButton.onClick.AddListener(() => ApplyPreset("HeavyImpactor"));
            if (presetHeavyTargetButton != null) presetHeavyTargetButton.onClick.AddListener(() => ApplyPreset("HeavyTarget"));
            if (presetElasticButton != null) presetElasticButton.onClick.AddListener(() => ApplyPreset("Elastic"));
            if (presetInelasticButton != null) presetInelasticButton.onClick.AddListener(() => ApplyPreset("Inelastic"));

            // Wire sliders
            SetupSliders();

            // Apply initial physics parameters
            ApplyPhysicsConfiguration();

            // Ensure visualizer references
            EnsureVisualizers();

            ResetSimulation();
        }

        
        /// <summary>
        /// Detects whether an XROrigin is present in the active scene.
        /// If present, disables conflicting desktop keyboard shortcuts (Space/R/T/C) that collide
        /// with XR Device Simulator controls, binds the XR Camera to the World-Space Canvas,
        /// and disables redundant 2D GraphicRaycaster components so TrackedDeviceGraphicRaycaster
        /// handles 3D controller ray interactions cleanly.
        /// </summary>
        private void ConfigureVRSceneIntegration()
        {
            var xrOrigin = FindObjectOfType<XROrigin>();
            if (xrOrigin == null)
            {
                _isVRScene = false;
                return;
            }

            _isVRScene = true;
            enableKeyboardShortcuts = false;

            Camera xrCamera = xrOrigin.Camera != null ? xrOrigin.Camera : Camera.main;

            Canvas targetCanvas = null;
            if (launchButton != null)
            {
                targetCanvas = launchButton.GetComponentInParent<Canvas>();
            }
            if (targetCanvas == null)
            {
                var vrCanvasGO = GameObject.Find("VR_WorldSpace_Canvas");
                if (vrCanvasGO != null) targetCanvas = vrCanvasGO.GetComponent<Canvas>();
            }

            if (targetCanvas != null && targetCanvas.renderMode == RenderMode.WorldSpace)
            {
                if (targetCanvas.worldCamera == null && xrCamera != null)
                {
                    targetCanvas.worldCamera = xrCamera;
                }

                var trackedRaycaster = targetCanvas.GetComponent<TrackedDeviceGraphicRaycaster>();
                var standardRaycaster = targetCanvas.GetComponent<GraphicRaycaster>();

                if (trackedRaycaster != null && standardRaycaster != null)
                {
                    standardRaycaster.enabled = false;
                }
            }
        }

        private void SetupSliders()
        {
            if (speedSlider != null)
            {
                speedSlider.minValue = 2.0f;
                speedSlider.maxValue = 20.0f;
                speedSlider.value = launchVelocity;
                speedSlider.onValueChanged.AddListener(OnSpeedSliderChanged);
                OnSpeedSliderChanged(launchVelocity);
            }

            if (sphereMassSlider != null)
            {
                sphereMassSlider.minValue = 0.2f;
                sphereMassSlider.maxValue = 5.0f;
                sphereMassSlider.value = sphereMass;
                sphereMassSlider.onValueChanged.AddListener(OnSphereMassSliderChanged);
                OnSphereMassSliderChanged(sphereMass);
            }

            if (cubeMassSlider != null)
            {
                cubeMassSlider.minValue = 0.2f;
                cubeMassSlider.maxValue = 5.0f;
                cubeMassSlider.value = cubeMass;
                cubeMassSlider.onValueChanged.AddListener(OnCubeMassSliderChanged);
                OnCubeMassSliderChanged(cubeMass);
            }

            if (restitutionSlider != null)
            {
                restitutionSlider.minValue = 0.0f;
                restitutionSlider.maxValue = 1.0f;
                restitutionSlider.value = restitution;
                restitutionSlider.onValueChanged.AddListener(OnRestitutionSliderChanged);
                OnRestitutionSliderChanged(restitution);
            }
        }

        private void EnsureVisualizers()
        {
            if (sphereVectorVisualizer == null && sphereObject != null)
            {
                sphereVectorVisualizer = sphereObject.GetComponent<PhysicsVectorVisualizer>();
                if (sphereVectorVisualizer == null)
                {
                    sphereVectorVisualizer = sphereObject.gameObject.AddComponent<PhysicsVectorVisualizer>();
                    sphereVectorVisualizer.VectorColor = new Color(0.0f, 0.9f, 1.0f, 1.0f); // Bright Cyan
                }
            }

            if (cubeVectorVisualizer == null && cubeObject != null)
            {
                cubeVectorVisualizer = cubeObject.GetComponent<PhysicsVectorVisualizer>();
                if (cubeVectorVisualizer == null)
                {
                    cubeVectorVisualizer = cubeObject.gameObject.AddComponent<PhysicsVectorVisualizer>();
                    cubeVectorVisualizer.VectorColor = new Color(1.0f, 0.85f, 0.2f, 1.0f); // Bright Amber
                }
            }

            if (contactVisualizer == null)
            {
                contactVisualizer = FindObjectOfType<ContactManifoldVisualizer>();
                if (contactVisualizer == null)
                {
                    var go = new GameObject("ContactManifoldVisualizer");
                    contactVisualizer = go.AddComponent<ContactManifoldVisualizer>();
                }
            }
        }

        private void OnSpeedSliderChanged(float val)
        {
            launchVelocity = val;
            if (speedValueText != null)
            {
                speedValueText.text = $"{val:F1} m/s";
            }
        }

        private void OnSphereMassSliderChanged(float val)
        {
            sphereMass = val;
            if (sphereMassValueText != null)
            {
                sphereMassValueText.text = $"{val:F1} kg";
            }
            if (sphereObject != null) sphereObject.SetMass(val);
        }

        private void OnCubeMassSliderChanged(float val)
        {
            cubeMass = val;
            if (cubeMassValueText != null)
            {
                cubeMassValueText.text = $"{val:F1} kg";
            }
            if (cubeObject != null) cubeObject.SetMass(val);
        }

        private void OnRestitutionSliderChanged(float val)
        {
            restitution = val;
            if (restitutionValueText != null)
            {
                restitutionValueText.text = $"{val:F2}";
            }
            if (sphereObject != null) sphereObject.SetBounciness(val);
            if (cubeObject != null) cubeObject.SetBounciness(val);
        }

        public void AdjustSpeed(float delta) => SetSpeed(Mathf.Clamp(launchVelocity + delta, 2.0f, 20.0f));
        public void AdjustSphereMass(float delta) => SetSphereMass(Mathf.Clamp(sphereMass + delta, 0.2f, 5.0f));
        public void AdjustCubeMass(float delta) => SetCubeMass(Mathf.Clamp(cubeMass + delta, 0.2f, 5.0f));
        public void AdjustRestitution(float delta) => SetRestitution(Mathf.Clamp01(restitution + delta));

        public void SetSpeed(float val)
        {
            launchVelocity = val;
            if (speedSlider != null) speedSlider.value = val;
            if (speedValueText != null) speedValueText.text = $"{val:F1} m/s";
        }

        public void SetSphereMass(float val)
        {
            sphereMass = val;
            if (sphereMassSlider != null) sphereMassSlider.value = val;
            if (sphereMassValueText != null) sphereMassValueText.text = $"{val:F1} kg";
            if (sphereObject != null) sphereObject.SetMass(val);
        }

        public void SetCubeMass(float val)
        {
            cubeMass = val;
            if (cubeMassSlider != null) cubeMassSlider.value = val;
            if (cubeMassValueText != null) cubeMassValueText.text = $"{val:F1} kg";
            if (cubeObject != null) cubeObject.SetMass(val);
        }

        public void SetRestitution(float val)
        {
            restitution = val;
            if (restitutionSlider != null) restitutionSlider.value = val;
            if (restitutionValueText != null) restitutionValueText.text = $"{val:F2}";
            if (sphereObject != null) sphereObject.SetBounciness(val);
            if (cubeObject != null) cubeObject.SetBounciness(val);
        }

        public void ApplyPreset(string presetName)
        {
            switch (presetName)
            {
                case "Equal":
                    SetSphereMass(1.0f);
                    SetCubeMass(1.0f);
                    break;
                case "HeavyImpactor":
                    SetSphereMass(3.0f);
                    SetCubeMass(1.0f);
                    break;
                case "HeavyTarget":
                    SetSphereMass(1.0f);
                    SetCubeMass(3.0f);
                    break;
                case "Elastic":
                    SetRestitution(1.0f);
                    break;
                case "Inelastic":
                    SetRestitution(0.0f);
                    break;
            }
        }

        private void ApplyPhysicsConfiguration()
        {
            if (sphereObject != null)
            {
                sphereObject.SetMass(sphereMass);
                sphereObject.SetBounciness(restitution);
            }

            if (cubeObject != null)
            {
                cubeObject.SetMass(cubeMass);
                cubeObject.SetBounciness(restitution);
            }
        }

        private void FixedUpdate()
        {
            // Sample pre-collision velocities each physics step during motion
            if (_currentState == SimulationState.InMotion)
            {
                if (sphereRigidbody != null) _lastFixedSphereVel = sphereRigidbody.velocity;
                if (cubeRigidbody != null) _lastFixedCubeVel = cubeRigidbody.velocity;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ReturnToMainScene();
                return;
            }

            if (!enableKeyboardShortcuts || _isVRScene)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                LaunchSphere();
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetSimulation();
            }

            if (Input.GetKeyDown(KeyCode.T) || Input.GetKeyDown(KeyCode.C))
            {
                ToggleTelemetry();
            }
        }

        /// <summary>
        /// Launches the sphere with a direct linear velocity impulse.
        /// This ensures the initial launch speed is strictly independent of the sphere's mass.
        /// </summary>
        public void LaunchSphere()
        {
            if (sphereRigidbody == null) return;

            // Cleanly release any held objects prior to launch
            ReleaseHeldObjectsHandler?.Invoke();

            ApplyPhysicsConfiguration();

            // Clear any lingering velocities
            sphereRigidbody.velocity = Vector3.zero;
            sphereRigidbody.angularVelocity = Vector3.zero;
            if (cubeRigidbody != null)
            {
                cubeRigidbody.velocity = Vector3.zero;
                cubeRigidbody.angularVelocity = Vector3.zero;
            }

            if (contactVisualizer != null) contactVisualizer.Clear();

            // Direct linear velocity assignment guarantees mass-independence
            var dir = launchDirection.normalized;
            sphereRigidbody.velocity = dir * launchVelocity;

            _lastFixedSphereVel = sphereRigidbody.velocity;
            _lastFixedCubeVel = Vector3.zero;
            _preImpactSphereVel = _lastFixedSphereVel;
            _preImpactCubeVel = Vector3.zero;

            _currentState = SimulationState.InMotion;
            UpdateTelemetryState();
        }

        /// <summary>
        /// Callback triggered by CollisionObject on physical contact.
        /// Deduplicates multi-body callbacks using pair-hash and physics step deduplication,
        /// avoiding artificial cooldowns while guaranteeing exactly one count per physical collision.
        /// </summary>
        public void ReportCollision(CollisionObject source, Collision collision)
        {
            if (source == null || collision == null || collision.gameObject == null) return;

            int idA = source.gameObject.GetInstanceID();
            int idB = collision.gameObject.GetInstanceID();

            int minId = idA < idB ? idA : idB;
            int maxId = idA < idB ? idB : idA;
            int pairKey = unchecked((minId * 397) ^ maxId);

            int currentStep = Time.frameCount;
            if (_lastPhysicsTickByPair.TryGetValue(pairKey, out int lastStep) && lastStep == currentStep)
            {
                return;
            }

            _lastPhysicsTickByPair[pairKey] = currentStep;
            _impactCount++;
            _currentState = SimulationState.Collided;

            // Capture pre-impact velocities from the physics step before impact
            var v1i = _lastFixedSphereVel.sqrMagnitude > 0.01f ? _lastFixedSphereVel : launchDirection.normalized * launchVelocity;
            var v2i = _lastFixedCubeVel;

            // Extract contact manifold
            var contactPoint = Vector3.zero;
            var contactNormal = Vector3.zero;
            if (collision.contactCount > 0)
            {
                var contact = collision.GetContact(0);
                contactPoint = contact.point;
                contactNormal = contact.normal;
            }

            // Extract PhysX relative velocity & impulse
            var relVel = collision.relativeVelocity.magnitude;
            var impulse = collision.impulse.magnitude;

            // Visual contact marker & normal line
            if (contactVisualizer != null)
            {
                contactVisualizer.ShowContact(contactPoint, contactNormal);
            }

            // Sample post-impact velocities across the collision resolution
            StartCoroutine(CapturePostCollisionData(v1i, v2i, relVel, impulse, contactPoint, contactNormal));
        }

        private IEnumerator CapturePostCollisionData(Vector3 v1i, Vector3 v2i, float relVel, float impulse, Vector3 contactPoint, Vector3 contactNormal)
        {
            // Wait for FixedUpdate to ensure PhysX collision solver has fully integrated post-impact velocities
            yield return new WaitForFixedUpdate();

            var v1f = (sphereRigidbody != null) ? sphereRigidbody.velocity : Vector3.zero;
            var v2f = (cubeRigidbody != null) ? cubeRigidbody.velocity : Vector3.zero;

            // 1. Vector Momentum Conservation Analysis
            // p = m1*v1 + m2*v2
            var pInitial = sphereMass * v1i + cubeMass * v2i;
            var pFinal = sphereMass * v1f + cubeMass * v2f;
            var pDelta = pFinal - pInitial;

            float pInitMag = pInitial.magnitude;
            float pErrorPercent = (pInitMag > 0.001f) ? (pDelta.magnitude / pInitMag) * 100f : 0f;

            // 2. Kinetic Energy Analysis
            // KE = 0.5 * m * v^2
            float keInitial = 0.5f * sphereMass * v1i.sqrMagnitude + 0.5f * cubeMass * v2i.sqrMagnitude;
            float keFinal = 0.5f * sphereMass * v1f.sqrMagnitude + 0.5f * cubeMass * v2f.sqrMagnitude;
            float deltaKE = keFinal - keInitial;
            float keRetainedPercent = (keInitial > 0.001f) ? (keFinal / keInitial) * 100f : 0f;

            // 3. Coefficient of Restitution along Contact Normal
            // e = - (v1f - v2f) · n / ((v1i - v2i) · n)
            var norm = contactNormal.normalized;
            if (norm.sqrMagnitude < 0.001f) norm = Vector3.right;

            // Approach relative speed along normal
            float approachSpeed = Vector3.Dot(v1i - v2i, -norm);
            if (approachSpeed < 0.001f) approachSpeed = Vector3.Dot(v1i - v2i, norm);
            approachSpeed = Mathf.Abs(approachSpeed);

            // Separation relative speed along normal
            float separationSpeed = Vector3.Dot(v2f - v1f, -norm);
            if (separationSpeed < 0.001f) separationSpeed = Vector3.Dot(v2f - v1f, norm);
            separationSpeed = Mathf.Abs(separationSpeed);

            float measuredRestitution = (approachSpeed > 0.05f) ? (separationSpeed / approachSpeed) : restitution;
            measuredRestitution = Mathf.Clamp(measuredRestitution, 0f, 1.2f);

            // Update UI telemetry displays
            UpdateTelemetryCollision(relVel, impulse, contactPoint, contactNormal, pInitial, pFinal, pErrorPercent, keInitial, keFinal, deltaKE, keRetainedPercent, measuredRestitution);
        }

        /// <summary>
        /// Resets positions, rotations, linear/angular velocities, colors, and telemetry.
        /// Preserves the user-selected slider configuration values.
        /// </summary>
        public void ResetSimulation()
        {
            // Cleanly release any held objects prior to reset
            ReleaseHeldObjectsHandler?.Invoke();
            _currentState = SimulationState.Ready;
            _impactCount = 0;
            _lastPhysicsTickByPair.Clear();

            _lastFixedSphereVel = Vector3.zero;
            _lastFixedCubeVel = Vector3.zero;
            _preImpactSphereVel = Vector3.zero;
            _preImpactCubeVel = Vector3.zero;

            if (sphereObject != null) sphereObject.ResetToInitialState();
            if (cubeObject != null) cubeObject.ResetToInitialState();

            if (sphereRigidbody != null)
            {
                sphereRigidbody.velocity = Vector3.zero;
                sphereRigidbody.angularVelocity = Vector3.zero;
            }

            if (cubeRigidbody != null)
            {
                cubeRigidbody.velocity = Vector3.zero;
                cubeRigidbody.angularVelocity = Vector3.zero;
            }

            if (sphereVectorVisualizer != null) sphereVectorVisualizer.ClearVector();
            if (cubeVectorVisualizer != null) cubeVectorVisualizer.ClearVector();
            if (contactVisualizer != null) contactVisualizer.Clear();

            // Reapply current slider configuration
            ApplyPhysicsConfiguration();

            UpdateTelemetryReset();
        }

        public void ReturnToMainScene()
        {
            SceneManager.LoadScene("MainScene");
        }

        public void ToggleTelemetry()
        {
            if (telemetryPanel != null)
            {
                telemetryPanel.SetActive(!telemetryPanel.activeSelf);
            }
        }

        private void UpdateTelemetryState()
        {
            if (stateText != null)
            {
                stateText.text = $"<color=#00E5FF>{_currentState.ToString().ToUpper()}</color>";
            }
        }

        private void UpdateTelemetryCollision(float relVel, float impulse, Vector3 contactPoint, Vector3 contactNormal,
            Vector3 pInitial, Vector3 pFinal, float pErrorPercent, float keInitial, float keFinal, float deltaKE, float keRetainedPercent, float measuredRestitution)
        {
            if (stateText != null)
            {
                stateText.text = $"<color=#00E676>{_currentState.ToString().ToUpper()}</color>";
            }

            if (impactCountText != null)
            {
                impactCountText.text = _impactCount.ToString();
            }

            if (relativeVelocityText != null)
            {
                relativeVelocityText.text = $"{relVel:F2} m/s";
            }

            if (impulseText != null)
            {
                impulseText.text = $"{impulse:F2} N·s";
            }

            if (contactPointText != null)
            {
                contactPointText.text = $"({contactPoint.x:F2}, {contactPoint.y:F2}, {contactPoint.z:F2})";
            }

            if (contactNormalText != null)
            {
                contactNormalText.text = $"({contactNormal.x:F2}, {contactNormal.y:F2}, {contactNormal.z:F2})";
            }

            if (momentumBeforeText != null)
            {
                momentumBeforeText.text = $"{pInitial.x:F2} kg·m/s";
            }

            if (momentumAfterText != null)
            {
                momentumAfterText.text = $"{pFinal.x:F2} kg·m/s";
            }

            if (momentumErrorText != null)
            {
                var color = pErrorPercent < 3.0f ? "#00E676" : "#FFAB00";
                momentumErrorText.text = $"<color={color}>Δp: {pErrorPercent:F1}%</color>";
            }

            if (kineticEnergyText != null)
            {
                kineticEnergyText.text = $"{keInitial:F1}J → {keFinal:F1}J ({deltaKE:+0.0;-0.0}J, {keRetainedPercent:F0}%)";
            }

            if (measuredRestitutionText != null)
            {
                measuredRestitutionText.text = $"e = {measuredRestitution:F2} (target: {restitution:F2})";
            }
        }

        private void UpdateTelemetryReset()
        {
            if (stateText != null)
            {
                stateText.text = $"<color=#B0BEC5>{_currentState.ToString().ToUpper()}</color>";
            }

            if (impactCountText != null)
            {
                impactCountText.text = "0";
            }

            if (relativeVelocityText != null)
            {
                relativeVelocityText.text = "-- m/s";
            }

            if (impulseText != null)
            {
                impulseText.text = "-- N·s";
            }

            if (contactPointText != null)
            {
                contactPointText.text = "(--, --, --)";
            }

            if (contactNormalText != null)
            {
                contactNormalText.text = "(--, --, --)";
            }

            if (momentumBeforeText != null)
            {
                momentumBeforeText.text = "-- kg·m/s";
            }

            if (momentumAfterText != null)
            {
                momentumAfterText.text = "-- kg·m/s";
            }

            if (momentumErrorText != null)
            {
                momentumErrorText.text = "--";
            }

            if (kineticEnergyText != null)
            {
                kineticEnergyText.text = "-- J";
            }

            if (measuredRestitutionText != null)
            {
                measuredRestitutionText.text = $"e = -- (target: {restitution:F2})";
            }
        }
    }
}

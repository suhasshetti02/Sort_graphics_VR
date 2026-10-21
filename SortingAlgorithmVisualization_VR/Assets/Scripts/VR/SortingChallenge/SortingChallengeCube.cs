using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VR.SortingChallenge
{
    /// <summary>
    /// Component for each of the 5 numbered draggable cubes in the VR Sorting Challenge.
    /// Uses Kinematic grab movement with smooth following and an active collision constraint
    /// preventing the cube's bottom from passing below the tabletop surface during manipulation.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(XRGrabInteractable))]
    public class SortingChallengeCube : MonoBehaviour
    {
        [Header("Cube Identity")]
        [Tooltip("The integer value of this cube (1 to 5).")]
        [SerializeField] private int cubeValue = 1;

        [Header("Staging / Safe Position")]
        [SerializeField] private Vector3 stagingPosition;
        [SerializeField] private Quaternion stagingRotation = Quaternion.identity;

        [Header("Current Placement")]
        [SerializeField] private SortingSlot currentSlot;

        [Header("Table Surface Collision Constraint")]
        [Tooltip("Reference to the TableTop BoxCollider to prevent kinematic penetration.")]
        [SerializeField] private BoxCollider tableCollider;
        [SerializeField] private float tableHorizontalMargin = 0.04f;

        private Rigidbody _rigidbody;
        private XRGrabInteractable _grabInteractable;
        private BoxCollider _boxCollider;
        private bool _isGrabbed;
        private Vector3 _lastLoggedPos;
        private float _grabDiagTimer;

        public int CubeValue
        {
            get => cubeValue;
            set => cubeValue = value;
        }

        public Vector3 StagingPosition
        {
            get => stagingPosition;
            set => stagingPosition = value;
        }

        public Quaternion StagingRotation
        {
            get => stagingRotation;
            set => stagingRotation = value;
        }

        public SortingSlot CurrentSlot => currentSlot;
        public bool IsGrabbed => _isGrabbed;
        public BoxCollider CubeCollider => _boxCollider;
        public BoxCollider TableCollider
        {
            get => tableCollider;
            set => tableCollider = value;
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _grabInteractable = GetComponent<XRGrabInteractable>();
            _boxCollider = GetComponent<BoxCollider>();

            if (stagingPosition == Vector3.zero)
            {
                stagingPosition = transform.position;
                stagingRotation = transform.rotation;
            }

            ResolveTableCollider();
            ConfigurePhysics();
            ConfigureGrabInteractable();

            // Ensure TableSurfaceGrabTransformer is attached to constrain held target pose directly in XRI pipeline
            if (!TryGetComponent<TableSurfaceGrabTransformer>(out var transformer))
            {
                transformer = gameObject.AddComponent<TableSurfaceGrabTransformer>();
                transformer.TableCollider = tableCollider;
            }
        }

        private void Start()
        {
            ResolveTableCollider();
        }

        private void ResolveTableCollider()
        {
            if (tableCollider == null)
            {
                var tableTop = GameObject.Find("TableTop");
                if (tableTop != null)
                {
                    tableCollider = tableTop.GetComponent<BoxCollider>();
                }
            }
        }

        private void OnEnable()
        {
            if (_grabInteractable != null)
            {
                _grabInteractable.firstHoverEntered.AddListener(OnHoverEntered);
                _grabInteractable.lastHoverExited.AddListener(OnHoverExited);
                _grabInteractable.selectEntered.AddListener(OnGrabbed);
                _grabInteractable.selectExited.AddListener(OnReleased);
            }
        }

        private void OnDisable()
        {
            if (_grabInteractable != null)
            {
                _grabInteractable.firstHoverEntered.RemoveListener(OnHoverEntered);
                _grabInteractable.lastHoverExited.RemoveListener(OnHoverExited);
                _grabInteractable.selectEntered.RemoveListener(OnGrabbed);
                _grabInteractable.selectExited.RemoveListener(OnReleased);
            }
        }

        private void ConfigurePhysics()
        {
            if (_rigidbody == null) return;

            _rigidbody.mass = 1.0f;
            _rigidbody.drag = 5.0f;
            _rigidbody.angularDrag = 5.0f;
            _rigidbody.useGravity = false;
            _rigidbody.isKinematic = true;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private void ConfigureGrabInteractable()
        {
            if (_grabInteractable == null) return;

            _grabInteractable.movementType = XRBaseInteractable.MovementType.Kinematic;
            _grabInteractable.smoothPosition = true;
            _grabInteractable.smoothPositionAmount = 14f;
            _grabInteractable.smoothRotation = true;
            _grabInteractable.smoothRotationAmount = 14f;
            _grabInteractable.useDynamicAttach = true;
            _grabInteractable.trackPosition = true;
            _grabInteractable.trackRotation = true;
            _grabInteractable.throwOnDetach = false;
            _grabInteractable.retainTransformParent = false;
            _grabInteractable.interactionLayers = 1; // Default layer
        }

        private void Update()
        {
            if (_isGrabbed)
            {
                _grabDiagTimer += Time.deltaTime;
                if (Vector3.Distance(transform.position, _lastLoggedPos) > 0.05f || _grabDiagTimer > 1.0f)
                {
                    _lastLoggedPos = transform.position;
                    _grabDiagTimer = 0f;
                    Debug.Log($"[GrabDiag:HeldMove] '{name}' (Val={cubeValue}) following controller at WorldPos=({transform.position.x:F3}, {transform.position.y:F3}, {transform.position.z:F3})");
                }
            }
        }

        // Note: Real-time grab constraint is handled natively within XRI by TableSurfaceGrabTransformer.
        // This prevents fighting with XRGrabInteractable's internal kinematic MovePosition and interpolation.

        /// <summary>
        /// Prevents the cube's bottom from passing below the tabletop surface during manipulation.
        /// Calculates the cube's actual lowest point from its world BoxCollider bounds.
        /// </summary>
        public void ConstrainAboveTabletop()
        {
            ResolveTableCollider();

            float tableTopY = 0.80f;
            Bounds tableBounds;

            if (tableCollider != null && tableCollider.enabled)
            {
                tableBounds = tableCollider.bounds;
                tableTopY = tableBounds.max.y;
            }
            else
            {
                tableBounds = new Bounds(new Vector3(0f, 0.76f, 1.25f), new Vector3(1.60f, 0.08f, 0.80f));
                tableTopY = 0.80f;
            }

            Physics.SyncTransforms();

            if (_boxCollider == null)
            {
                _boxCollider = GetComponent<BoxCollider>();
            }

            Bounds cubeBounds = _boxCollider != null 
                ? _boxCollider.bounds 
                : new Bounds(transform.position, new Vector3(0.18f, 0.18f, 0.18f));

            // In Edit Mode or before PhysX internal tick, ensure bounds center aligns with current transform
            if (_boxCollider != null && Mathf.Abs(cubeBounds.center.y - transform.position.y) > 0.02f)
            {
                Vector3 center = transform.TransformPoint(_boxCollider.center);
                Vector3 size = Vector3.Scale(_boxCollider.size, transform.lossyScale);
                cubeBounds = new Bounds(center, new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z)));
            }

            // Check if any part of the cube horizontally overlaps the tabletop (with buffer)
            bool isOverTable = (cubeBounds.max.x >= tableBounds.min.x - tableHorizontalMargin &&
                                cubeBounds.min.x <= tableBounds.max.x + tableHorizontalMargin &&
                                cubeBounds.max.z >= tableBounds.min.z - tableHorizontalMargin &&
                                cubeBounds.min.z <= tableBounds.max.z + tableHorizontalMargin);

            if (isOverTable)
            {
                float cubeBottomY = cubeBounds.min.y;
                float penetration = tableTopY - cubeBottomY;

                if (penetration > 0f)
                {
                    Vector3 pos = transform.position;
                    pos.y += penetration;
                    transform.position = pos;

                    if (_rigidbody != null)
                    {
                        _rigidbody.position = pos;
                        _rigidbody.velocity = Vector3.zero;
                        _rigidbody.angularVelocity = Vector3.zero;
                    }

                    Physics.SyncTransforms();
                }
            }
        }

        private void OnHoverEntered(HoverEnterEventArgs args)
        {
            string interactorName = args.interactorObject != null ? args.interactorObject.transform.name : "Unknown";
            Debug.Log($"[GrabDiag:HoverEnter] '{name}' (Val={cubeValue}) HOVERED by '{interactorName}'. InteractionLayers={_grabInteractable?.interactionLayers.value}");
        }

        private void OnHoverExited(HoverExitEventArgs args)
        {
            string interactorName = args.interactorObject != null ? args.interactorObject.transform.name : "Unknown";
            Debug.Log($"[GrabDiag:HoverExit] '{name}' (Val={cubeValue}) UNHOVERED by '{interactorName}'.");
        }

        private void OnGrabbed(SelectEnterEventArgs args)
        {
            _isGrabbed = true;
            _lastLoggedPos = transform.position;
            _grabDiagTimer = 0f;

            string interactorName = args.interactorObject != null ? args.interactorObject.transform.name : "Unknown";
            int transformerCount = _grabInteractable != null ? _grabInteractable.singleGrabTransformersCount : 0;
            Debug.Log($"[GrabDiag:SelectEnter] '{name}' (Val={cubeValue}) GRABBED by '{interactorName}'! IsKinematic={(_rigidbody != null ? _rigidbody.isKinematic.ToString() : "null")}, Pos=({transform.position.x:F3}, {transform.position.y:F3}, {transform.position.z:F3}), TransformersRegistered={transformerCount}");

            // If picking up from a slot, immediately vacate that slot
            if (currentSlot != null)
            {
                if (currentSlot.CurrentCube == this)
                {
                    currentSlot.ClearSlot();
                }
                currentSlot = null;
            }

            if (SortingChallengeManager.Instance != null)
            {
                SortingChallengeManager.Instance.OnCubePickedUp(this);
            }
        }

        private void OnReleased(SelectExitEventArgs args)
        {
            _isGrabbed = false;
            string interactorName = args.interactorObject != null ? args.interactorObject.transform.name : "Unknown";
            Debug.Log($"[GrabDiag:SelectExit] '{name}' (Val={cubeValue}) RELEASED by '{interactorName}' at Pos=({transform.position.x:F3}, {transform.position.y:F3}, {transform.position.z:F3})");

            // Guarantee the cube cannot be released below the tabletop surface
            ConstrainAboveTabletop();

            if (SortingChallengeManager.Instance != null)
            {
                SortingChallengeManager.Instance.OnCubeReleased(this);
            }
        }

        public void ClearCurrentSlotReference()
        {
            currentSlot = null;
        }

        public void SnapToSlot(SortingSlot slot)
        {
            if (currentSlot != null && currentSlot != slot)
            {
                if (currentSlot.CurrentCube == this)
                {
                    currentSlot.ClearSlot();
                }
                currentSlot = null;
            }

            currentSlot = slot;
            slot.PlaceCube(this);

            transform.position = slot.SnapTransform.position;
            transform.rotation = slot.SnapTransform.rotation;

            // Enforce tabletop support
            ConstrainAboveTabletop();

            if (_rigidbody != null)
            {
                _rigidbody.velocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
                _rigidbody.isKinematic = true;
            }
        }

        public void ReturnToStaging()
        {
            if (currentSlot != null)
            {
                if (currentSlot.CurrentCube == this)
                {
                    currentSlot.ClearSlot();
                }
                currentSlot = null;
            }

            transform.position = stagingPosition;
            transform.rotation = stagingRotation;

            ConstrainAboveTabletop();

            if (_rigidbody != null)
            {
                _rigidbody.velocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
                _rigidbody.isKinematic = true;
            }
        }

        public void SetKinematic(bool kinematic)
        {
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = kinematic;
                if (kinematic)
                {
                    _rigidbody.velocity = Vector3.zero;
                    _rigidbody.angularVelocity = Vector3.zero;
                }
            }
        }
    }
}

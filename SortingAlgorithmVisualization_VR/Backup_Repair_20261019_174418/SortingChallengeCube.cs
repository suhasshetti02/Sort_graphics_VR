using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VR.SortingChallenge
{
    /// <summary>
    /// Component for each of the 5 numbered draggable cubes in the VR Sorting Challenge.
    /// Uses Kinematic grab movement with smooth following to eliminate sudden jumps,
    /// jitter, or physics fighting.
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

        private Rigidbody _rigidbody;
        private XRGrabInteractable _grabInteractable;
        private bool _isGrabbed;

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

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _grabInteractable = GetComponent<XRGrabInteractable>();

            if (stagingPosition == Vector3.zero)
            {
                stagingPosition = transform.position;
                stagingRotation = transform.rotation;
            }

            ConfigurePhysics();
            ConfigureGrabInteractable();
        }

        private void OnEnable()
        {
            if (_grabInteractable != null)
            {
                _grabInteractable.selectEntered.AddListener(OnGrabbed);
                _grabInteractable.selectExited.AddListener(OnReleased);
            }
        }

        private void OnDisable()
        {
            if (_grabInteractable != null)
            {
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

        private void OnGrabbed(SelectEnterEventArgs args)
        {
            _isGrabbed = true;

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

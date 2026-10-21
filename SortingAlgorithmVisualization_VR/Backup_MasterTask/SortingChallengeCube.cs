using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VR.SortingChallenge
{
    /// <summary>
    /// Component for each of the 5 numbered draggable cubes in the VR Sorting Challenge.
    /// Manages value, staging positions, grab/release events, and safe placement.
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

        private void ConfigureGrabInteractable()
        {
            if (_grabInteractable == null) return;

            // Instantaneous or Kinematic ensures responsive, jitter-free grabbing in simulator and VR
            _grabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;
            _grabInteractable.throwOnDetach = false;
            _grabInteractable.retainTransformParent = false;
        }

        private void OnGrabbed(SelectEnterEventArgs args)
        {
            _isGrabbed = true;

            // If picking up from a slot, immediately vacate that slot
            if (currentSlot != null)
            {
                currentSlot.ClearSlot();
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

        public void SnapToSlot(SortingSlot slot)
        {
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
                currentSlot.ClearSlot();
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
            }
        }
    }
}

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

namespace VR.SortingChallenge
{
    /// <summary>
    /// Custom XRI Grab Transformer that derives from XRGeneralGrabTransformer.
    /// Executes full interactor tracking (moving, rotating, following controller) via base.Process,
    /// then clamps the target pose so the held cube cannot penetrate below the tabletop.
    /// </summary>
    [DisallowMultipleComponent]
    public class TableSurfaceGrabTransformer : XRGeneralGrabTransformer
    {
        [Header("Table Surface Support")]
        [SerializeField] private BoxCollider tableCollider;
        [SerializeField] private float tableHorizontalMargin = 0.05f;

        private BoxCollider _cubeCollider;
        private bool _hasLoggedProcess;

        protected override RegistrationMode registrationMode => RegistrationMode.SingleAndMultiple;

        public BoxCollider TableCollider
        {
            get => tableCollider;
            set => tableCollider = value;
        }

        private void Awake()
        {
            _cubeCollider = GetComponent<BoxCollider>();
            ResolveTableCollider();
        }

        public void ResolveTableCollider()
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

        public override void OnGrab(XRGrabInteractable grabInteractable)
        {
            base.OnGrab(grabInteractable);
            _hasLoggedProcess = false;
        }

        public override void Process(XRGrabInteractable grabInteractable, XRInteractionUpdateOrder.UpdatePhase updatePhase, ref Pose targetPose, ref Vector3 localScale)
        {
            // 1. Let XRGeneralGrabTransformer calculate targetPose from the controller / ray interactor
            base.Process(grabInteractable, updatePhase, ref targetPose, ref localScale);

            // 2. Clamp targetPose above the tabletop surface during dynamic, fixed, or render updates
            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic || 
                updatePhase == XRInteractionUpdateOrder.UpdatePhase.Fixed ||
                updatePhase == XRInteractionUpdateOrder.UpdatePhase.OnBeforeRender)
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

                if (_cubeCollider == null)
                {
                    _cubeCollider = GetComponent<BoxCollider>();
                }

                float halfHeight = 0.09f;
                if (_cubeCollider != null)
                {
                    halfHeight = _cubeCollider.size.y * Mathf.Abs(transform.lossyScale.y) * 0.5f;
                }

                float minCenterY = tableTopY + halfHeight;

                // Check if targetPose is horizontally over the table
                bool isOverTable = (targetPose.position.x >= tableBounds.min.x - tableHorizontalMargin &&
                                    targetPose.position.x <= tableBounds.max.x + tableHorizontalMargin &&
                                    targetPose.position.z >= tableBounds.min.z - tableHorizontalMargin &&
                                    targetPose.position.z <= tableBounds.max.z + tableHorizontalMargin);

                if (isOverTable && targetPose.position.y < minCenterY)
                {
                    targetPose.position = new Vector3(targetPose.position.x, minCenterY, targetPose.position.z);
                }

                if (!_hasLoggedProcess && grabInteractable.isSelected && grabInteractable.interactorsSelecting.Count > 0)
                {
                    _hasLoggedProcess = true;
                    Debug.Log($"[GrabDiag:TransformerProcess] TableSurfaceGrabTransformer active on '{gameObject.name}'! Interactor='{grabInteractable.interactorsSelecting[0].transform.name}', Computed TargetPose=({targetPose.position.x:F3}, {targetPose.position.y:F3}, {targetPose.position.z:F3})");
                }
            }
        }
    }
}

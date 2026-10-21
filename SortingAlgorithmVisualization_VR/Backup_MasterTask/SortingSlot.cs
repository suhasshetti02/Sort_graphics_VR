using UnityEngine;

namespace VR.SortingChallenge
{
    public enum SlotBorderVisualState
    {
        Normal,
        Occupied,
        Success
    }

    /// <summary>
    /// Represents one of the five target placement slots on the tabletop.
    /// Tracks occupancy and updates its visual border frame.
    /// </summary>
    public class SortingSlot : MonoBehaviour
    {
        [Header("Slot Configuration")]
        [Tooltip("The 1-based index of this slot from left to right (1 to 5).")]
        [SerializeField] private int slotIndex = 1;

        [Tooltip("Exact transform where the cube will snap when placed in this slot.")]
        [SerializeField] private Transform snapTransform;

        [Header("State Tracking")]
        [SerializeField] private SortingChallengeCube currentCube;

        [Header("Visual Border Frame")]
        [SerializeField] private Renderer borderRenderer;
        [SerializeField] private Color normalColor = new Color(0.2f, 0.7f, 1.0f, 0.75f);
        [SerializeField] private Color occupiedColor = new Color(0.95f, 0.65f, 0.15f, 0.9f);
        [SerializeField] private Color successColor = new Color(0.1f, 0.95f, 0.35f, 1.0f);

        private Material _borderMaterial;

        public int SlotIndex => slotIndex;
        public Transform SnapTransform => snapTransform != null ? snapTransform : transform;
        public SortingChallengeCube CurrentCube => currentCube;
        public bool IsOccupied => currentCube != null;

        private void Awake()
        {
            if (snapTransform == null)
            {
                snapTransform = transform;
            }

            if (borderRenderer != null)
            {
                _borderMaterial = Application.isPlaying ? borderRenderer.material : borderRenderer.sharedMaterial;
                SetBorderState(SlotBorderVisualState.Normal);
            }
        }

        public void PlaceCube(SortingChallengeCube cube)
        {
            currentCube = cube;
            SetBorderState(SlotBorderVisualState.Occupied);
        }

        public void ClearSlot()
        {
            currentCube = null;
            SetBorderState(SlotBorderVisualState.Normal);
        }

        public void SetBorderState(SlotBorderVisualState state)
        {
            if (_borderMaterial == null && borderRenderer != null)
            {
                _borderMaterial = Application.isPlaying ? borderRenderer.material : borderRenderer.sharedMaterial;
            }

            if (_borderMaterial == null) return;

            switch (state)
            {
                case SlotBorderVisualState.Normal:
                    _borderMaterial.color = normalColor;
                    if (_borderMaterial.HasProperty("_EmissionColor"))
                    {
                        _borderMaterial.SetColor("_EmissionColor", normalColor * 0.4f);
                    }
                    break;

                case SlotBorderVisualState.Occupied:
                    _borderMaterial.color = occupiedColor;
                    if (_borderMaterial.HasProperty("_EmissionColor"))
                    {
                        _borderMaterial.SetColor("_EmissionColor", occupiedColor * 0.5f);
                    }
                    break;

                case SlotBorderVisualState.Success:
                    _borderMaterial.color = successColor;
                    if (_borderMaterial.HasProperty("_EmissionColor"))
                    {
                        _borderMaterial.SetColor("_EmissionColor", successColor * 0.8f);
                    }
                    break;
            }
        }
    }
}

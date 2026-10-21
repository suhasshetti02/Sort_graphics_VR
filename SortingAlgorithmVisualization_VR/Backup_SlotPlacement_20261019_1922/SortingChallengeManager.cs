using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VR.SortingChallenge
{
    /// <summary>
    /// Master manager for the VR Sorting Challenge.
    /// Manages slot tracking, real-time validation, feedback messages,
    /// celebration effects, reshuffling, reset, and scene navigation.
    /// </summary>
    public class SortingChallengeManager : MonoBehaviour
    {
        public static SortingChallengeManager Instance { get; private set; }

        [Header("Game Elements")]
        [Tooltip("The 5 target slots arranged left to right.")]
        [SerializeField] private SortingSlot[] slots = new SortingSlot[5];

        [Tooltip("The 5 numbered cubes.")]
        [SerializeField] private SortingChallengeCube[] cubes = new SortingChallengeCube[5];

        [Tooltip("Legacy fallback snap radius.")]
        [SerializeField] private float snapRadius = 0.38f;

        [Header("Placement Geometry Settings")]
        [Tooltip("Maximum horizontal (XZ) distance from slot snap anchor to accept placement.")]
        [SerializeField] private float maxHorizontalDistance = 0.22f;

        [Tooltip("Minimum vertical difference (cube Y - slot Y) to accept placement.")]
        [SerializeField] private float minVerticalDifference = -0.08f;

        [Tooltip("Maximum vertical difference (cube Y - slot Y) to accept placement.")]
        [SerializeField] private float maxVerticalDifference = 0.50f;

        [Tooltip("Enable detailed diagnostic logging for every cube release.")]
        [SerializeField] private bool enableReleaseDiagnostics = true;

        [Header("Staging Positions (Back Row of Table)")]
        [SerializeField] private Transform[] stagingSlots = new Transform[5];

        [Header("UI Feedback")]
        [SerializeField] private Text statusTitleText;
        [SerializeField] private Text feedbackMessageText;
        [SerializeField] private Text slotProgressText;
        [SerializeField] private Image statusPanelBackground;
        [SerializeField] private GameObject successBannerObject;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button backToMenuButton;

        [Header("3D Status Mesh Elements")]
        [SerializeField] private TextMesh statusTitleTextMesh;
        [SerializeField] private TextMesh feedbackMessageTextMesh;
        [SerializeField] private TextMesh slotProgressTextMesh;
        [SerializeField] private GameObject successBannerMesh;

        [Header("Visual Colors")]
        [SerializeField] private Color normalPanelColor = new Color(0.06f, 0.12f, 0.22f, 0.94f);
        [SerializeField] private Color successPanelColor = new Color(0.04f, 0.35f, 0.18f, 0.95f);

        [Header("Celebration FX")]
        [SerializeField] private ParticleSystem celebrationParticles;

        [Header("Game State")]
        [SerializeField] private bool isCompleted = false;

        public bool IsCompleted => isCompleted;
        public SortingSlot[] Slots => slots;
        public SortingChallengeCube[] Cubes => cubes;
        public float SnapRadius => snapRadius;
        public float MaxHorizontalDistance { get => maxHorizontalDistance; set => maxHorizontalDistance = value; }
        public float MinVerticalDifference { get => minVerticalDifference; set => minVerticalDifference = value; }
        public float MaxVerticalDifference { get => maxVerticalDifference; set => maxVerticalDifference = value; }
        public bool EnableReleaseDiagnostics { get => enableReleaseDiagnostics; set => enableReleaseDiagnostics = value; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            SetupButtonListeners();
            InitializeInitialLayout();
            UpdateGameState();
            EnsureCanvasCameras();
        }

        private void EnsureCanvasCameras()
        {
            var cam = Camera.main;
            if (cam == null) cam = FindObjectOfType<Camera>();
            if (cam != null)
            {
                foreach (var c in FindObjectsOfType<Canvas>())
                {
                    if (c.renderMode == RenderMode.WorldSpace && c.worldCamera == null)
                    {
                        c.worldCamera = cam;
                    }
                }
            }
        }

        private void SetupButtonListeners()
        {
            if (playAgainButton != null)
            {
                playAgainButton.onClick.RemoveAllListeners();
                playAgainButton.onClick.AddListener(PlayAgain);
            }

            if (resetButton != null)
            {
                resetButton.onClick.RemoveAllListeners();
                resetButton.onClick.AddListener(ResetChallenge);
            }

            if (backToMenuButton != null)
            {
                backToMenuButton.onClick.RemoveAllListeners();
                backToMenuButton.onClick.AddListener(BackToMainMenu);
            }
        }

        /// <summary>
        /// Initial layout places cubes in an unsorted configuration: [3, 1, 5, 2, 4].
        /// </summary>
        public void InitializeInitialLayout()
        {
            isCompleted = false;
            int[] initialOrder = new int[] { 3, 1, 5, 2, 4 };

            // Clear any occupied slots
            foreach (var slot in slots)
            {
                if (slot != null) slot.ClearSlot();
            }

            // Map cubes by value
            var cubeByValue = new Dictionary<int, SortingChallengeCube>();
            foreach (var cube in cubes)
            {
                if (cube != null) cubeByValue[cube.CubeValue] = cube;
            }

            // Assign each cube to its staging position
            for (int i = 0; i < 5; i++)
            {
                int val = initialOrder[i];
                if (cubeByValue.TryGetValue(val, out var cube))
                {
                    Vector3 stagePos = (stagingSlots != null && i < stagingSlots.Length && stagingSlots[i] != null)
                        ? stagingSlots[i].position
                        : new Vector3(-0.52f + (i * 0.26f), 0.895f, 1.45f);

                    Quaternion stageRot = (stagingSlots != null && i < stagingSlots.Length && stagingSlots[i] != null)
                        ? stagingSlots[i].rotation
                        : Quaternion.identity;

                    cube.StagingPosition = stagePos;
                    cube.StagingRotation = stageRot;
                    cube.ReturnToStaging();
                }
            }

            if (celebrationParticles != null)
            {
                celebrationParticles.Stop();
                celebrationParticles.Clear();
            }
        }

        /// <summary>
        /// Resets the challenge back to the original unsorted arrangement.
        /// </summary>
        public void ResetChallenge()
        {
            InitializeInitialLayout();
            UpdateGameState();
        }

        public void OnCubePickedUp(SortingChallengeCube cube)
        {
            cube.SetKinematic(true);
            UpdateGameState();
        }

        public void OnCubeReleased(SortingChallengeCube cube)
        {
            if (cube == null) return;

            // Enforce tabletop support constraint before evaluating placement
            cube.ConstrainAboveTabletop();

            Vector3 cubePos = cube.transform.position;
            Vector2 cubeXZ = new Vector2(cubePos.x, cubePos.z);
            BoxCollider cubeCol = cube.CubeCollider != null ? cube.CubeCollider : cube.GetComponent<BoxCollider>();

            // Find TableTop collider and world bounds
            BoxCollider tableCol = cube.TableCollider;
            if (tableCol == null)
            {
                var tableObj = GameObject.Find("TableTop");
                if (tableObj != null) tableCol = tableObj.GetComponent<BoxCollider>();
            }

            SortingSlot nearestSlot = null;
            float closestHorizDist = float.MaxValue;
            System.Text.StringBuilder diagLog = null;

            if (enableReleaseDiagnostics)
            {
                diagLog = new System.Text.StringBuilder();
                diagLog.AppendLine($"[CubePlacement Diagnostics] Released '{cube.gameObject.name}' (Value={cube.CubeValue})");
                diagLog.AppendLine($"  Cube Position: ({cubePos.x:F4}, {cubePos.y:F4}, {cubePos.z:F4})");
                if (cubeCol != null)
                {
                    diagLog.AppendLine($"  Cube Collider Bounds: center=({cubeCol.bounds.center.x:F3}, {cubeCol.bounds.center.y:F3}, {cubeCol.bounds.center.z:F3}), size=({cubeCol.bounds.size.x:F3}, {cubeCol.bounds.size.y:F3}, {cubeCol.bounds.size.z:F3}), bottomY={cubeCol.bounds.min.y:F4}");
                }
                if (tableCol != null)
                {
                    diagLog.AppendLine($"  TableTop World Bounds: min=({tableCol.bounds.min.x:F3}, {tableCol.bounds.min.y:F3}, {tableCol.bounds.min.z:F3}), max=({tableCol.bounds.max.x:F3}, {tableCol.bounds.max.y:F3}, {tableCol.bounds.max.z:F3}), topY={tableCol.bounds.max.y:F4}");
                }
                diagLog.AppendLine($"  Capture Zone: maxHorizDist={maxHorizontalDistance:F3}m, vertRange=[{minVerticalDifference:F3}m, {maxVerticalDifference:F3}m]");
                diagLog.AppendLine($"  Candidate Slots Evaluation:");
            }

            foreach (var slot in slots)
            {
                if (slot == null) continue;

                Vector3 slotWorldPos = slot.transform.position;
                Vector3 snapPos = slot.SnapTransform.position;
                Vector2 snapXZ = new Vector2(snapPos.x, snapPos.z);
                float horizDist = Vector2.Distance(cubeXZ, snapXZ);
                float vertDiff = cubePos.y - snapPos.y;

                bool isOccupied = slot.IsOccupied;
                bool horizOk = horizDist <= maxHorizontalDistance;
                bool vertOk = vertDiff >= minVerticalDifference && vertDiff <= maxVerticalDifference;
                bool eligible = !isOccupied && horizOk && vertOk;

                if (enableReleaseDiagnostics && diagLog != null)
                {
                    string status = "";
                    if (isOccupied) status += "Already Occupied; ";
                    if (!horizOk) status += $"HorizDist {horizDist:F3}m > {maxHorizontalDistance:F3}m; ";
                    if (!vertOk) status += $"VertDiff {vertDiff:F3}m outside [{minVerticalDifference:F2}, {maxVerticalDifference:F2}]; ";
                    if (eligible) status = "ELIGIBLE CANDIDATE";

                    diagLog.AppendLine($"    Slot '{slot.gameObject.name}' (Index={slot.SlotIndex}) worldPos=({slotWorldPos.x:F3}, {slotWorldPos.y:F3}, {slotWorldPos.z:F3}) snapCenter=({snapPos.x:F3}, {snapPos.y:F3}, {snapPos.z:F3}): horizDist={horizDist:F3}m, vertDiff={vertDiff:F3}m -> {status}");
                }

                if (eligible)
                {
                    if (horizDist < closestHorizDist)
                    {
                        closestHorizDist = horizDist;
                        nearestSlot = slot;
                    }
                }
            }

            if (nearestSlot != null)
            {
                if (enableReleaseDiagnostics && diagLog != null)
                {
                    diagLog.AppendLine($"  -> FINAL DECISION: SNAPPED into '{nearestSlot.gameObject.name}' (Nearest eligible empty slot, horizDist={closestHorizDist:F3}m).");
                    Debug.Log(diagLog.ToString());
                }

                // Snap cleanly into the nearest eligible empty slot
                cube.SnapToSlot(nearestSlot);
            }
            else
            {
                if (enableReleaseDiagnostics && diagLog != null)
                {
                    diagLog.AppendLine("  -> FINAL DECISION: RETURN TO STAGING (No eligible empty slot within capture criteria).");
                    Debug.Log(diagLog.ToString());
                }

                // Dropped outside any valid target zone: return cleanly to safe staging rack
                cube.ReturnToStaging();
            }

            UpdateGameState();
        }

        /// <summary>
        /// Validates sorting state and updates UI feedback immediately.
        /// </summary>
        public void UpdateGameState()
        {
            int occupiedCount = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null && slots[i].IsOccupied)
                {
                    occupiedCount++;
                }
            }

            string progStr = $"{occupiedCount} / 5 SLOTS OCCUPIED";
            if (slotProgressText != null) slotProgressText.text = progStr;
            if (slotProgressTextMesh != null) slotProgressTextMesh.text = progStr;

            if (occupiedCount == 5)
            {
                // Sort slots by slotIndex so left-to-right evaluation is strictly guaranteed
                var sortedSlots = new SortingSlot[5];
                System.Array.Copy(slots, sortedSlots, 5);
                System.Array.Sort(sortedSlots, (a, b) => a.SlotIndex.CompareTo(b.SlotIndex));

                bool strictlySorted = true;
                for (int i = 0; i < 5; i++)
                {
                    if (sortedSlots[i] == null || sortedSlots[i].CurrentCube == null || sortedSlots[i].CurrentCube.CubeValue != (i + 1))
                    {
                        strictlySorted = false;
                        break;
                    }
                }

                if (strictlySorted)
                {
                    TriggerSuccess();
                }
                else
                {
                    TriggerIncorrectState();
                }
            }
            else
            {
                TriggerPartialState(occupiedCount);
            }
        }

        private void TriggerSuccess()
        {
            if (isCompleted) return; // Prevent repeated triggers
            isCompleted = true;

            string title = "SUCCESS! SORTING COMPLETED";
            string feed = "1 -> 2 -> 3 -> 4 -> 5\nAll cubes arranged in perfect ascending order!";

            if (statusTitleText != null) statusTitleText.text = title;
            if (statusTitleTextMesh != null) statusTitleTextMesh.text = title;

            if (feedbackMessageText != null) feedbackMessageText.text = feed;
            if (feedbackMessageTextMesh != null) feedbackMessageTextMesh.text = feed;

            if (statusPanelBackground != null) statusPanelBackground.color = successPanelColor;

            if (successBannerObject != null) successBannerObject.SetActive(true);
            if (successBannerMesh != null) successBannerMesh.SetActive(true);

            // Change all slot borders to green
            foreach (var slot in slots)
            {
                if (slot != null)
                {
                    slot.SetBorderState(SlotBorderVisualState.Success);
                }
            }

            // Trigger celebration burst once
            if (celebrationParticles != null)
            {
                celebrationParticles.Play();
            }
        }

        private void TriggerIncorrectState()
        {
            isCompleted = false;

            string title = "SORTING INCOMPLETE";
            string feed = "Not sorted yet. Arrange cubes in ascending order (1 to 5).";

            if (statusTitleText != null) statusTitleText.text = title;
            if (statusTitleTextMesh != null) statusTitleTextMesh.text = title;

            if (feedbackMessageText != null) feedbackMessageText.text = feed;
            if (feedbackMessageTextMesh != null) feedbackMessageTextMesh.text = feed;

            if (statusPanelBackground != null) statusPanelBackground.color = normalPanelColor;

            if (successBannerObject != null) successBannerObject.SetActive(false);
            if (successBannerMesh != null) successBannerMesh.SetActive(false);

            // Set slots back to occupied state
            foreach (var slot in slots)
            {
                if (slot != null && slot.IsOccupied)
                {
                    slot.SetBorderState(SlotBorderVisualState.Occupied);
                }
            }
        }

        private void TriggerPartialState(int occupiedCount)
        {
            isCompleted = false;

            string title = "VR SORTING CHALLENGE";
            string feed = (occupiedCount == 0)
                ? "Pick up the numbered cubes and arrange them from smallest to largest."
                : $"{occupiedCount}/5 slots filled. Place all cubes in ascending order.";

            if (statusTitleText != null) statusTitleText.text = title;
            if (statusTitleTextMesh != null) statusTitleTextMesh.text = title;

            if (feedbackMessageText != null) feedbackMessageText.text = feed;
            if (feedbackMessageTextMesh != null) feedbackMessageTextMesh.text = feed;

            if (statusPanelBackground != null) statusPanelBackground.color = normalPanelColor;

            if (successBannerObject != null) successBannerObject.SetActive(false);
            if (successBannerMesh != null) successBannerMesh.SetActive(false);

            foreach (var slot in slots)
            {
                if (slot != null)
                {
                    slot.SetBorderState(slot.IsOccupied ? SlotBorderVisualState.Occupied : SlotBorderVisualState.Normal);
                }
            }
        }

        /// <summary>
        /// Clears all slots and shuffles cubes to a fresh unsorted order.
        /// </summary>
        public void PlayAgain()
        {
            isCompleted = false;

            // Clear slots
            foreach (var slot in slots)
            {
                if (slot != null) slot.ClearSlot();
            }

            // Create a randomized order that is guaranteed NOT already sorted (1,2,3,4,5)
            List<int> order = new List<int> { 1, 2, 3, 4, 5 };
            bool isSorted = true;
            while (isSorted)
            {
                // Fisher-Yates shuffle
                for (int i = order.Count - 1; i > 0; i--)
                {
                    int rnd = Random.Range(0, i + 1);
                    int tmp = order[i];
                    order[i] = order[rnd];
                    order[rnd] = tmp;
                }

                // Check if accidentally sorted
                isSorted = true;
                for (int i = 0; i < 5; i++)
                {
                    if (order[i] != i + 1)
                    {
                        isSorted = false;
                        break;
                    }
                }
            }

            var cubeByValue = new Dictionary<int, SortingChallengeCube>();
            foreach (var cube in cubes)
            {
                if (cube != null) cubeByValue[cube.CubeValue] = cube;
            }

            for (int i = 0; i < 5; i++)
            {
                int val = order[i];
                if (cubeByValue.TryGetValue(val, out var cube))
                {
                    Vector3 stagePos = (stagingSlots != null && i < stagingSlots.Length && stagingSlots[i] != null)
                        ? stagingSlots[i].position
                        : new Vector3(-0.52f + (i * 0.26f), 0.895f, 1.45f);

                    Quaternion stageRot = (stagingSlots != null && i < stagingSlots.Length && stagingSlots[i] != null)
                        ? stagingSlots[i].rotation
                        : Quaternion.identity;

                    cube.StagingPosition = stagePos;
                    cube.StagingRotation = stageRot;
                    cube.ReturnToStaging();
                }
            }

            if (celebrationParticles != null)
            {
                celebrationParticles.Stop();
                celebrationParticles.Clear();
            }

            UpdateGameState();
        }

        public void BackToMainMenu()
        {
            SceneManager.LoadScene("MainScene");
        }
    }
}

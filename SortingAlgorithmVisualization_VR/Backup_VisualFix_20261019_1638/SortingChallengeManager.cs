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

        [Tooltip("Maximum distance from slot center to trigger snapping upon release.")]
        [SerializeField] private float snapRadius = 0.38f;

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
                        : new Vector3(-0.76f + (i * 0.38f), 0.88f, 1.30f);

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
            // Find nearest empty slot within snapRadius
            SortingSlot nearestSlot = null;
            float closestDist = snapRadius;

            foreach (var slot in slots)
            {
                if (slot != null && !slot.IsOccupied)
                {
                    float dist = Vector3.Distance(cube.transform.position, slot.SnapTransform.position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        nearestSlot = slot;
                    }
                }
            }

            if (nearestSlot != null)
            {
                // Snap cleanly into the empty slot
                cube.SnapToSlot(nearestSlot);
            }
            else
            {
                // Dropped outside any valid empty slot: return to safe staging position on the table
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

            if (slotProgressText != null)
            {
                slotProgressText.text = $"{occupiedCount} / 5 SLOTS OCCUPIED";
            }

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

            if (statusTitleText != null) statusTitleText.text = "SUCCESS! SORTING COMPLETED";
            if (feedbackMessageText != null) feedbackMessageText.text = "1 -> 2 -> 3 -> 4 -> 5\nAll cubes arranged in perfect ascending order!";
            if (statusPanelBackground != null) statusPanelBackground.color = successPanelColor;

            if (successBannerObject != null) successBannerObject.SetActive(true);

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

            if (statusTitleText != null) statusTitleText.text = "SORTING INCOMPLETE";
            if (feedbackMessageText != null) feedbackMessageText.text = "Not sorted yet. Arrange cubes in ascending order (1 to 5).";
            if (statusPanelBackground != null) statusPanelBackground.color = normalPanelColor;

            if (successBannerObject != null) successBannerObject.SetActive(false);

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

            if (statusTitleText != null) statusTitleText.text = "VR SORTING CHALLENGE";

            if (feedbackMessageText != null)
            {
                if (occupiedCount == 0)
                {
                    feedbackMessageText.text = "Pick up the numbered cubes and arrange them from smallest to largest.";
                }
                else
                {
                    feedbackMessageText.text = $"{occupiedCount}/5 slots filled. Place all cubes in ascending order.";
                }
            }

            if (statusPanelBackground != null) statusPanelBackground.color = normalPanelColor;
            if (successBannerObject != null) successBannerObject.SetActive(false);

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
                        : new Vector3(-0.76f + (i * 0.38f), 0.88f, 1.30f);

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

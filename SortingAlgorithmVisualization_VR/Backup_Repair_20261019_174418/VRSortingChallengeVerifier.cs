using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using VR.SortingChallenge;

public static class VRSortingChallengeVerifier
{
    private static int _passedCount = 0;
    private static int _failedCount = 0;

    private static void Assert(bool condition, string message)
    {
        if (condition)
        {
            Debug.Log($"[CHALLENGE PASS] {message}");
            _passedCount++;
        }
        else
        {
            Debug.LogError($"[CHALLENGE FAIL] {message}");
            _failedCount++;
        }
    }

    public static void RunAllVerifications()
    {
        _passedCount = 0;
        _failedCount = 0;

        Debug.Log("===============================================================================");
        Debug.Log("STARTING VR SORTING CHALLENGE COMPREHENSIVE VERIFICATION SUITE");
        Debug.Log("===============================================================================");

        VerifySceneAndBuildSettings();
        VerifyCameraAndEnvironment();
        VerifyTableAndSlots();
        VerifyCubesConfiguration();
        VerifySortingValidationLogic();
        VerifySafeDropAndReset();
        VerifyMainSceneNavigation();
        VerifyOriginalProjectUntouched();

        Debug.Log("===============================================================================");
        Debug.Log($"CHALLENGE VERIFICATION SUMMARY: {_passedCount} PASSED, {_failedCount} FAILED.");
        Debug.Log("===============================================================================");

        if (_failedCount > 0)
        {
            EditorApplication.Exit(1);
        }
        else
        {
            EditorApplication.Exit(0);
        }
    }

    private static void VerifySceneAndBuildSettings()
    {
        Debug.Log("--- 1. Testing Scene Presence and Build Settings ---");
        string scenePath = "Assets/Scenes/VRSortingChallenge.unity";
        Assert(File.Exists(scenePath), $"VRSortingChallenge.unity scene exists on disk at {scenePath}.");

        bool inBuild = false;
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (s.path == scenePath && s.enabled)
            {
                inBuild = true;
                break;
            }
        }
        Assert(inBuild, "VRSortingChallenge.unity is registered and enabled in EditorBuildSettings.");
    }

    private static void VerifyCameraAndEnvironment()
    {
        Debug.Log("--- 2. Testing Camera Framing and Environment Hygiene ---");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/VRSortingChallenge.unity", OpenSceneMode.Single);

        // XR Rig and Camera Framing
        var rig = GameObject.Find("XR Interaction Setup");
        Assert(rig != null, "XR Interaction Setup GameObject exists in scene.");
        if (rig != null)
        {
            Assert(Mathf.Abs(rig.transform.position.z - (-0.45f)) < 0.10f, $"XR Origin is placed comfortably at Z = -0.45 (actual: {rig.transform.position.z}).");
            Assert(Mathf.Abs(rig.transform.eulerAngles.x - 18f) < 3.0f, $"XR Origin pitch is angled downward by ~18 deg (actual: {rig.transform.eulerAngles.x:F1} deg) to frame tabletop.");
        }

        // Table
        var table = GameObject.Find("SortingTable");
        Assert(table != null, "SortingTable GameObject exists.");
        var tableTop = GameObject.Find("TableTop");
        Assert(tableTop != null && tableTop.GetComponent<BoxCollider>() != null, "TableTop exists and possesses a BoxCollider for surface support.");

        // Collider Hygiene: decorative elements must not have colliders to prevent physics/grab conflicts
        var trim = GameObject.Find("TableTrim");
        if (trim != null)
        {
            Assert(trim.GetComponent<Collider>() == null, "TableTrim collider removed to prevent interference.");
        }

        var leg1 = GameObject.Find("TableLeg_1");
        if (leg1 != null)
        {
            Assert(leg1.GetComponent<Collider>() == null, "TableLeg collider removed to prevent ray/grab interference.");
        }

        // In-Scene Floating UI Elements
        var statusCanvas = GameObject.Find("GameStatus_Canvas");
        Assert(statusCanvas != null, "GameStatus_Canvas exists.");
        if (statusCanvas != null)
        {
            Assert(statusCanvas.transform.position.y > 1.3f && statusCanvas.transform.position.y < 1.7f, $"GameStatus_Canvas is positioned at comfortable eye level (actual Y: {statusCanvas.transform.position.y}).");
        }

        var guidePanel = GameObject.Find("VR_Controls_Guide");
        Assert(guidePanel != null, "VR_Controls_Guide panel exists on the left of the table.");
    }

    private static void VerifyTableAndSlots()
    {
        Debug.Log("--- 3. Testing Target Slots and Rack Structure ---");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/VRSortingChallenge.unity", OpenSceneMode.Single);

        var slots = GameObject.FindObjectsOfType<SortingSlot>();
        Assert(slots.Length == 5, $"Found exactly 5 SortingSlot components (found {slots.Length}).");

        var seenIndices = new System.Collections.Generic.HashSet<int>();
        float prevX = -999f;
        bool leftToRight = true;

        // Sort slots by slotIndex
        System.Array.Sort(slots, (a, b) => a.SlotIndex.CompareTo(b.SlotIndex));

        foreach (var slot in slots)
        {
            Assert(slot.SlotIndex >= 1 && slot.SlotIndex <= 5, $"Slot index {slot.SlotIndex} is within 1..5.");
            seenIndices.Add(slot.SlotIndex);
            Assert(slot.SnapTransform != null, $"Slot {slot.SlotIndex} has a configured SnapTransform.");

            if (slot.transform.position.x <= prevX)
            {
                leftToRight = false;
            }
            prevX = slot.transform.position.x;

            // Verify slot pad and border have no rogue colliders
            var pad = slot.transform.Find("SlotPad");
            if (pad != null)
            {
                Assert(pad.GetComponent<Collider>() == null, $"Slot {slot.SlotIndex} SlotPad has no conflicting collider.");
            }
            var border = slot.transform.Find("SlotBorder");
            if (border != null)
            {
                Assert(border.GetComponent<Collider>() == null, $"Slot {slot.SlotIndex} SlotBorder has no conflicting collider.");
            }
        }
        Assert(seenIndices.Count == 5, "All 5 slots have unique indices 1 through 5.");
        Assert(leftToRight, "Slots are placed in strict left-to-right order across the tabletop.");
    }

    private static void VerifyCubesConfiguration()
    {
        Debug.Log("--- 4. Testing 5 Numbered Cubes Physics & Grabbing Configuration ---");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/VRSortingChallenge.unity", OpenSceneMode.Single);

        var cubes = GameObject.FindObjectsOfType<SortingChallengeCube>();
        Assert(cubes.Length == 5, $"Found exactly 5 SortingChallengeCube components (found {cubes.Length}).");

        var values = new System.Collections.Generic.HashSet<int>();
        foreach (var c in cubes)
        {
            Assert(c.CubeValue >= 1 && c.CubeValue <= 5, $"Cube {c.name} has valid value {c.CubeValue}.");
            values.Add(c.CubeValue);

            var rb = c.GetComponent<Rigidbody>();
            Assert(rb != null, $"Cube {c.name} has Rigidbody.");
            if (rb != null)
            {
                Assert(rb.isKinematic, $"Cube {c.name} Rigidbody is Kinematic to prevent jitter/teleportation.");
                Assert(!rb.useGravity, $"Cube {c.name} Rigidbody useGravity is false.");
            }

            var col = c.GetComponent<BoxCollider>();
            Assert(col != null, $"Cube {c.name} has BoxCollider.");

            var grab = c.GetComponent<XRGrabInteractable>();
            Assert(grab != null, $"Cube {c.name} has XRGrabInteractable.");
            if (grab != null)
            {
                Assert(grab.movementType == XRBaseInteractable.MovementType.Kinematic, $"Cube {c.name} grab movementType is Kinematic (eliminates sudden jumps).");
                Assert(grab.smoothPosition, $"Cube {c.name} smoothPosition is enabled.");
                Assert(grab.smoothRotation, $"Cube {c.name} smoothRotation is enabled.");
                Assert(grab.interactionLayers.value == 1, $"Cube {c.name} interaction layer is 1 (Default).");
            }

            // Check face labels
            var labels = c.GetComponentsInChildren<Text>(true);
            Assert(labels.Length >= 4, $"Cube {c.name} has multi-face number labels (found {labels.Length} text labels).");
        }
        Assert(values.Count == 5, "All 5 cubes have distinct values 1 to 5.");
    }

    private static void VerifySortingValidationLogic()
    {
        Debug.Log("--- 5. Testing Sorting Validation Rules (1 < 2 < 3 < 4 < 5) ---");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/VRSortingChallenge.unity", OpenSceneMode.Single);

        var mgr = GameObject.FindObjectOfType<SortingChallengeManager>();
        Assert(mgr != null, "SortingChallengeManager is present in scene.");

        // Initial state must be unsorted
        mgr.InitializeInitialLayout();
        mgr.UpdateGameState();
        Assert(!mgr.IsCompleted, "Initial unsorted layout does NOT trigger success.");

        var cube1 = FindCubeByValue(mgr, 1);
        var cube2 = FindCubeByValue(mgr, 2);
        var cube3 = FindCubeByValue(mgr, 3);
        var cube4 = FindCubeByValue(mgr, 4);
        var cube5 = FindCubeByValue(mgr, 5);

        // Sort slots by index
        var slots = new SortingSlot[5];
        System.Array.Copy(mgr.Slots, slots, 5);
        System.Array.Sort(slots, (a, b) => a.SlotIndex.CompareTo(b.SlotIndex));

        // Test 1: Partial placement (3 cubes placed)
        cube1.SnapToSlot(slots[0]); // Slot 1 = 1
        cube2.SnapToSlot(slots[1]); // Slot 2 = 2
        cube3.SnapToSlot(slots[2]); // Slot 3 = 3
        mgr.UpdateGameState();
        Assert(!mgr.IsCompleted, "Partial placement (3/5) does NOT trigger success.");

        // Test 2: Inverted order (5, 4, 3, 2, 1)
        cube5.SnapToSlot(slots[0]); // Slot 1 = 5
        cube4.SnapToSlot(slots[1]); // Slot 2 = 4
        cube3.SnapToSlot(slots[2]); // Slot 3 = 3
        cube2.SnapToSlot(slots[3]); // Slot 4 = 2
        cube1.SnapToSlot(slots[4]); // Slot 5 = 1
        mgr.UpdateGameState();
        Assert(!mgr.IsCompleted, "Inverted order (5, 4, 3, 2, 1) does NOT trigger success.");

        // Test 3: Full placement with small swap (1, 3, 2, 4, 5)
        cube1.SnapToSlot(slots[0]); // Slot 1 = 1
        cube3.SnapToSlot(slots[1]); // Slot 2 = 3 (Wrong)
        cube2.SnapToSlot(slots[2]); // Slot 3 = 2 (Wrong)
        cube4.SnapToSlot(slots[3]); // Slot 4 = 4
        cube5.SnapToSlot(slots[4]); // Slot 5 = 5
        mgr.UpdateGameState();
        Assert(!mgr.IsCompleted, "Full placement with wrong order (1, 3, 2, 4, 5) does NOT trigger success.");

        // Test 4: Perfect ascending order (1, 2, 3, 4, 5)
        cube1.SnapToSlot(slots[0]); // Slot 1 = 1
        cube2.SnapToSlot(slots[1]); // Slot 2 = 2
        cube3.SnapToSlot(slots[2]); // Slot 3 = 3
        cube4.SnapToSlot(slots[3]); // Slot 4 = 4
        cube5.SnapToSlot(slots[4]); // Slot 5 = 5
        mgr.UpdateGameState();
        Assert(mgr.IsCompleted, "Strict ascending order (1, 2, 3, 4, 5) TRIGGERS SUCCESS!");

        // Test 5: Re-trigger idempotence
        mgr.UpdateGameState();
        Assert(mgr.IsCompleted, "Subsequent state check maintains success state cleanly.");
    }

    private static void VerifySafeDropAndReset()
    {
        Debug.Log("--- 6. Testing Safe Drop, ResetChallenge, and PlayAgain ---");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/VRSortingChallenge.unity", OpenSceneMode.Single);
        var mgr = GameObject.FindObjectOfType<SortingChallengeManager>();

        // Test Safe Return: Cube dropped far outside slots
        var cube1 = FindCubeByValue(mgr, 1);
        Vector3 originalStage = cube1.StagingPosition;
        cube1.transform.position = new Vector3(10f, -5f, 10f); // dropped far off table
        mgr.OnCubeReleased(cube1);
        Assert(Vector3.Distance(cube1.transform.position, originalStage) < 0.05f, "Cube dropped outside slots returned safely to staging position.");

        // Test ResetChallenge: Clears board and returns to initial [3, 1, 5, 2, 4] layout
        mgr.ResetChallenge();
        Assert(!mgr.IsCompleted, "ResetChallenge resets IsCompleted to false.");

        int occupied = 0;
        foreach (var s in mgr.Slots)
        {
            if (s.IsOccupied) occupied++;
        }
        Assert(occupied == 0, "ResetChallenge clears all 5 target slots.");

        // Test PlayAgain: Clears slots and guarantees a fresh unsorted permutation
        mgr.PlayAgain();
        Assert(!mgr.IsCompleted, "PlayAgain resets IsCompleted to false.");

        occupied = 0;
        foreach (var s in mgr.Slots)
        {
            if (s.IsOccupied) occupied++;
        }
        Assert(occupied == 0, "PlayAgain leaves all 5 target slots empty.");
    }

    private static void VerifyMainSceneNavigation()
    {
        Debug.Log("--- 7. Testing MainScene Navigation Integration ---");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity", OpenSceneMode.Single);

        var btn = GameObject.Find("BtnVRSortingChallenge");
        Assert(btn != null, "MainScene contains BtnVRSortingChallenge button.");

        var nav = btn.GetComponent<UI.SceneNavigator>();
        Assert(nav != null, "BtnVRSortingChallenge has SceneNavigator component.");

        var so = new SerializedObject(nav);
        string target = so.FindProperty("targetScene").stringValue;
        Assert(target == "VRSortingChallenge", $"SceneNavigator targetScene is 'VRSortingChallenge' (actual: {target}).");

        // Verify original CollisionLab button is intact
        var colBtn = GameObject.Find("BtnCollisionLab");
        Assert(colBtn != null, "Original BtnCollisionLab button in MainScene remains intact.");
    }

    private static void VerifyOriginalProjectUntouched()
    {
        Debug.Log("--- 8. Testing Original Desktop Project Intact ---");
        string originalBuildPath = "C:/Users/sande/OneDrive/Desktop/CGVR/SortingAlgorithmVisualization/Builds/SortLab3D-Windows/SortLab3D.exe";
        Assert(File.Exists(originalBuildPath), $"Original desktop build exists at {originalBuildPath}.");
    }

    private static SortingChallengeCube FindCubeByValue(SortingChallengeManager mgr, int val)
    {
        foreach (var c in mgr.Cubes)
        {
            if (c != null && c.CubeValue == val) return c;
        }
        return null;
    }
}

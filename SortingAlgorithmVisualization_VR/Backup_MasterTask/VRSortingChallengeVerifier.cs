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
        Debug.Log("STARTING VR SORTING CHALLENGE AUTOMATED VERIFICATION SUITE");
        Debug.Log("===============================================================================");

        VerifySceneAndBuildSettings();
        VerifyTableAndSlots();
        VerifyCubesConfiguration();
        VerifySortingValidationLogic();
        VerifyPlayAgainAndSafeReturn();
        VerifyMainSceneNavigation();
        VerifyOriginalProjectUntouched();

        Debug.Log("===============================================================================");
        Debug.Log($"CHALLENGE VERIFICATION RESULTS: {_passedCount} PASSED, {_failedCount} FAILED.");
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

    private static void VerifyTableAndSlots()
    {
        Debug.Log("--- 2. Testing Table and Target Slots ---");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/VRSortingChallenge.unity", OpenSceneMode.Single);

        var table = GameObject.Find("SortingTable");
        Assert(table != null, "SortingTable GameObject exists.");

        var slots = GameObject.FindObjectsOfType<SortingSlot>();
        Assert(slots.Length == 5, $"Found exactly 5 SortingSlot components in scene (found {slots.Length}).");

        // Verify distinct positions and 1..5 indices
        var seenIndices = new System.Collections.Generic.HashSet<int>();
        foreach (var slot in slots)
        {
            Assert(slot.SlotIndex >= 1 && slot.SlotIndex <= 5, $"Slot index {slot.SlotIndex} is in range 1..5.");
            seenIndices.Add(slot.SlotIndex);
            Assert(slot.SnapTransform != null, $"Slot {slot.SlotIndex} has a configured SnapTransform.");
        }
        Assert(seenIndices.Count == 5, "All 5 slots have unique indices 1 to 5.");
    }

    private static void VerifyCubesConfiguration()
    {
        Debug.Log("--- 3. Testing 5 Numbered Cubes Setup ---");
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

            var col = c.GetComponent<BoxCollider>();
            Assert(col != null, $"Cube {c.name} has BoxCollider.");

            var grab = c.GetComponent<XRGrabInteractable>();
            Assert(grab != null, $"Cube {c.name} has XRGrabInteractable.");
            Assert(grab.interactionLayers.value == 1, $"Cube {c.name} interaction layer is 1 (Default).");
        }
        Assert(values.Count == 5, "All 5 cubes have distinct values 1 to 5.");
    }

    private static void VerifySortingValidationLogic()
    {
        Debug.Log("--- 4. Testing Sorting Validation (Ascending Order 1..5) ---");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/VRSortingChallenge.unity", OpenSceneMode.Single);

        var mgr = GameObject.FindObjectOfType<SortingChallengeManager>();
        Assert(mgr != null, "SortingChallengeManager is present in scene.");

        // Initial state must be unsorted
        mgr.InitializeInitialLayout();
        mgr.UpdateGameState();
        Assert(!mgr.IsCompleted, "Initial unsorted layout does NOT trigger success.");

        // Test 1: Partial placement (3 cubes placed)
        var cube1 = FindCubeByValue(mgr, 1);
        var cube2 = FindCubeByValue(mgr, 2);
        var cube3 = FindCubeByValue(mgr, 3);
        var cube4 = FindCubeByValue(mgr, 4);
        var cube5 = FindCubeByValue(mgr, 5);

        cube1.SnapToSlot(mgr.Slots[0]); // Slot 1 = 1
        cube2.SnapToSlot(mgr.Slots[1]); // Slot 2 = 2
        cube3.SnapToSlot(mgr.Slots[2]); // Slot 3 = 3
        mgr.UpdateGameState();
        Assert(!mgr.IsCompleted, "Partial placement (3/5) does NOT trigger success.");

        // Test 2: Full placement in WRONG order (1, 3, 2, 4, 5)
        cube1.SnapToSlot(mgr.Slots[0]); // Slot 1 = 1
        cube3.SnapToSlot(mgr.Slots[1]); // Slot 2 = 3 (Wrong)
        cube2.SnapToSlot(mgr.Slots[2]); // Slot 3 = 2 (Wrong)
        cube4.SnapToSlot(mgr.Slots[3]); // Slot 4 = 4
        cube5.SnapToSlot(mgr.Slots[4]); // Slot 5 = 5
        mgr.UpdateGameState();
        Assert(!mgr.IsCompleted, "Full placement with wrong order (1,3,2,4,5) does NOT trigger success.");

        // Test 3: Full placement in PERFECT order (1, 2, 3, 4, 5)
        cube1.SnapToSlot(mgr.Slots[0]); // Slot 1 = 1
        cube2.SnapToSlot(mgr.Slots[1]); // Slot 2 = 2
        cube3.SnapToSlot(mgr.Slots[2]); // Slot 3 = 3
        cube4.SnapToSlot(mgr.Slots[3]); // Slot 4 = 4
        cube5.SnapToSlot(mgr.Slots[4]); // Slot 5 = 5
        mgr.UpdateGameState();
        Assert(mgr.IsCompleted, "Strict ascending order (1, 2, 3, 4, 5) TRIGGERS SUCCESS!");
    }

    private static void VerifyPlayAgainAndSafeReturn()
    {
        Debug.Log("--- 5. Testing Play Again and Safe Drop Reset ---");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/VRSortingChallenge.unity", OpenSceneMode.Single);
        var mgr = GameObject.FindObjectOfType<SortingChallengeManager>();

        // Test Safe Return: Cube dropped far outside slots
        var cube1 = FindCubeByValue(mgr, 1);
        Vector3 originalStage = cube1.StagingPosition;
        cube1.transform.position = new Vector3(10f, -5f, 10f); // dropped far off table
        mgr.OnCubeReleased(cube1);
        Assert(Vector3.Distance(cube1.transform.position, originalStage) < 0.05f, "Cube dropped outside slots returned safely to staging position.");

        // Test Play Again: Resets board, clears slots, sets fresh unsorted state
        mgr.PlayAgain();
        Assert(!mgr.IsCompleted, "PlayAgain resets IsCompleted to false.");

        int occupied = 0;
        foreach (var s in mgr.Slots)
        {
            if (s.IsOccupied) occupied++;
        }
        Assert(occupied == 0, "PlayAgain clears all 5 target slots.");
    }

    private static void VerifyMainSceneNavigation()
    {
        Debug.Log("--- 6. Testing MainScene Navigation Integration ---");
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
        Debug.Log("--- 7. Testing Original Desktop Project Intact ---");
        string originalBuildPath = "C:/Users/sande/OneDrive/Desktop/CGVR/SortingAlgorithmVisualization/Builds/SortLab3D-Windows/SortLab3D.exe";
        Assert(File.Exists(originalBuildPath), $"Original desktop build exists at {originalBuildPath}.");
    }

    private static SortingChallengeCube FindCubeByValue(SortingChallengeManager mgr, int val)
    {
        foreach (var c in mgr.Cubes)
        {
            if (c.CubeValue == val) return c;
        }
        return null;
    }
}

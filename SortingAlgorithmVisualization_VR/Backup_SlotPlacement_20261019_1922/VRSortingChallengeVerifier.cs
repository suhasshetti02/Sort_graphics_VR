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

    [MenuItem("SortLab VR/Run VR Sorting Challenge Verifier")]
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
        VerifyDeterministicPlacement();
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
        Debug.Log("--- 2. Testing Camera Framing, Canvas Positioning, and Lighting ---");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/VRSortingChallenge.unity", OpenSceneMode.Single);

        var cam = Camera.main;
        if (cam == null) cam = Object.FindObjectOfType<Camera>();
        Assert(cam != null, "Main Camera exists in scene.");

        // XR Rig and Camera Framing
        var rig = GameObject.Find("XR Interaction Setup");
        Assert(rig != null, "XR Interaction Setup GameObject exists in scene.");
        if (rig != null)
        {
            Assert(Mathf.Abs(rig.transform.position.z - (-0.45f)) < 0.10f, $"XR Origin is placed at Z = -0.45 (actual: {rig.transform.position.z}).");
            Assert(Mathf.Abs(rig.transform.eulerAngles.x - 18f) < 3.0f, $"XR Origin pitch is angled downward by ~18 deg (actual: {rig.transform.eulerAngles.x:F1} deg).");
        }

        // Table
        var table = GameObject.Find("SortingTable");
        Assert(table != null, "SortingTable GameObject exists.");
        var tableTop = GameObject.Find("TableTop");
        Assert(tableTop != null && tableTop.GetComponent<BoxCollider>() != null, "TableTop exists and possesses a BoxCollider for surface support.");
        if (tableTop != null)
        {
            var tableCol = tableTop.GetComponent<BoxCollider>();
            Assert(tableCol != null && tableCol.enabled && !tableCol.isTrigger, "TableTop BoxCollider is enabled and non-trigger.");
            float topY = tableCol.bounds.max.y;
            Assert(Mathf.Abs(topY - 0.8000f) < 0.015f, $"TableTop top surface Y is approximately 0.80m (actual: {topY:F4}m).");
        }

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

        // GameStatus_Canvas in camera frame
        var statusCanvas = GameObject.Find("GameStatus_Canvas");
        Assert(statusCanvas != null, "GameStatus_Canvas exists.");
        if (statusCanvas != null && cam != null)
        {
            Assert(statusCanvas.transform.position.y >= 1.15f && statusCanvas.transform.position.y <= 1.45f, $"GameStatus_Canvas is positioned at comfortable eye level (actual Y: {statusCanvas.transform.position.y:F2}).");
            Vector3 vp = cam.WorldToViewportPoint(statusCanvas.transform.position);
            Assert(vp.z > 0 && vp.x >= 0.2f && vp.x <= 0.8f && vp.y >= 0.5f && vp.y <= 0.95f, $"GameStatus_Canvas is positioned well within Camera Viewport (viewport: ({vp.x:F2}, {vp.y:F2}, {vp.z:F2})).");
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
            if (col != null)
            {
                Assert(col.enabled && !col.isTrigger, $"Cube {c.name} BoxCollider is enabled and non-trigger.");
                float worldHeight = col.bounds.size.y > 0 ? col.bounds.size.y : col.size.y * c.transform.lossyScale.y;
                Assert(Mathf.Abs(worldHeight - 0.18f) < 0.02f, $"Cube {c.name} world height is ~0.18m (actual: {worldHeight:F3}m).");
            }

            var grab = c.GetComponent<XRGrabInteractable>();
            Assert(grab != null, $"Cube {c.name} has XRGrabInteractable.");
            if (grab != null)
            {
                Assert(grab.movementType == XRBaseInteractable.MovementType.Kinematic, $"Cube {c.name} grab movementType is Kinematic.");
                Assert(grab.smoothPosition, $"Cube {c.name} smoothPosition is enabled.");
                Assert(grab.smoothRotation, $"Cube {c.name} smoothRotation is enabled.");
                Assert(grab.interactionLayers.value == 1, $"Cube {c.name} interaction layer is 1 (Default).");
            }

            // Verify Front Face Badge is firmly attached to cube
            var frontFace = c.transform.Find("FaceNumber_Front");
            Assert(frontFace != null, $"Cube {c.name} has child FaceNumber_Front firmly attached.");
            if (frontFace != null)
            {
                Assert(frontFace.parent == c.transform, $"FaceNumber_Front is parented directly to {c.name}.");
                var badgeQuad = frontFace.Find("BadgeQuad");
                Assert(badgeQuad != null, $"Cube {c.name} has BadgeQuad on front face.");
                if (badgeQuad != null)
                {
                    Assert(badgeQuad.localScale.x <= 0.60f && badgeQuad.localScale.y <= 0.60f, $"Cube {c.name} BadgeQuad is appropriately sized (scale: {badgeQuad.localScale.x:F2}) to keep colored body visible.");
                }

                var text3D = frontFace.Find("Text3D");
                Assert(text3D != null, $"Cube {c.name} has Text3D component on front face.");
                if (text3D != null)
                {
                    var tm = text3D.GetComponent<TextMesh>();
                    Assert(tm != null && tm.text == c.CubeValue.ToString(), $"Cube {c.name} TextMesh displays value '{c.CubeValue}'.");
                }
            }
        }
        Assert(values.Count == 5, "All 5 cubes have unique values 1 through 5.");
    }

    private static void VerifyDeterministicPlacement()
    {
        Debug.Log("--- 5. Testing Deterministic Placement (Horizontal, Vertical, Nearest Slot, Relocation) ---");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/VRSortingChallenge.unity", OpenSceneMode.Single);
        var mgr = GameObject.FindObjectOfType<SortingChallengeManager>();
        Assert(mgr != null, "SortingChallengeManager exists in scene.");

        var slots = new SortingSlot[5];
        System.Array.Copy(mgr.Slots, slots, 5);
        System.Array.Sort(slots, (a, b) => a.SlotIndex.CompareTo(b.SlotIndex));

        var cube1 = FindCubeByValue(mgr, 1);
        var cube2 = FindCubeByValue(mgr, 2);
        Assert(cube1 != null && cube2 != null, "Cubes 1 and 2 found.");

        mgr.ResetChallenge();

        // Test 0: Verify SnapTransform rest height is above tabletop surface
        foreach (var s in slots)
        {
            float snapY = s.SnapTransform.position.y;
            Assert(snapY >= 0.88f && snapY <= 0.92f, $"Slot {s.SlotIndex} SnapTransform Y={snapY:F4} centers cube properly above tabletop (Y=0.80).");
        }

        // Test Table Penetration Clamp: When held cube is dragged below tabletop (e.g. Y = 0.626m as reported in user console),
        // ConstrainAboveTabletop() must clamp world Y so bottom of cube rests at or above tabletop topY (0.80m).
        cube1.transform.position = new Vector3(-0.52f, 0.626f, 1.02f);
        cube1.ConstrainAboveTabletop();
        Assert(cube1.transform.position.y >= 0.885f, $"Held cube pushed underground (Y=0.626) is constrained upward to tabletop surface (actual Y: {cube1.transform.position.y:F4}).");
        Assert(cube1.CubeCollider.bounds.min.y >= 0.795f, $"Held cube bottom bounds ({cube1.CubeCollider.bounds.min.y:F4}) do not penetrate tabletop surface (0.80m).");

        // Test A: Placement within horizontal capture radius and resting vertical tolerance
        Vector3 slot1Snap = slots[0].SnapTransform.position;
        cube1.transform.position = new Vector3(slot1Snap.x + 0.05f, slot1Snap.y + 0.08f, slot1Snap.z);
        mgr.OnCubeReleased(cube1);
        Assert(slots[0].IsOccupied && slots[0].CurrentCube == cube1, "Cube 1 successfully snapped into Slot 1 when released within valid horizontal capture zone.");
        Assert(cube1.CurrentSlot == slots[0], "Cube 1 currentSlot bidirectionally references Slot 1.");

        // Test B: Placement with generous vertical height (holding cube 0.28m above slot)
        Vector3 slot2Snap = slots[1].SnapTransform.position;
        cube2.transform.position = new Vector3(slot2Snap.x, slot2Snap.y + 0.28f, slot2Snap.z);
        mgr.OnCubeReleased(cube2);
        Assert(slots[1].IsOccupied && slots[1].CurrentCube == cube2, "Cube 2 successfully snapped into Slot 2 when released high (0.28m) above target slot.");

        // Test C: Nearest-slot selection between Slot 3 (X=0.0) and Slot 4 (X=0.26)
        var cube3 = FindCubeByValue(mgr, 3);
        Vector3 slot3Snap = slots[2].SnapTransform.position;
        // Position cube slightly closer to Slot 3 (X=0.06m)
        cube3.transform.position = new Vector3(slot3Snap.x + 0.06f, slot3Snap.y + 0.10f, slot3Snap.z);
        mgr.OnCubeReleased(cube3);
        Assert(slots[2].IsOccupied && slots[2].CurrentCube == cube3, "Cube 3 snapped to nearest eligible Slot 3 rather than ambiguous adjacent Slot 4.");

        // Test D: Dropping outside horizontal capture radius returns safely to staging
        var cube4 = FindCubeByValue(mgr, 4);
        Vector3 origStaging4 = cube4.StagingPosition;
        cube4.transform.position = new Vector3(0.95f, 1.20f, 1.02f); // Way to the right of Slot 5 (X=0.52)
        mgr.OnCubeReleased(cube4);
        Assert(cube4.CurrentSlot == null, "Cube 4 released outside target zones does NOT occupy any slot.");
        Assert(Vector3.Distance(cube4.transform.position, origStaging4) < 0.05f, "Cube 4 released outside target zones returned safely to staging rack.");

        // Test E: Relocating cube from Slot 1 to Slot 5
        cube1.transform.position = new Vector3(slots[4].SnapTransform.position.x, slots[4].SnapTransform.position.y + 0.10f, slots[4].SnapTransform.position.z);
        mgr.OnCubeReleased(cube1);
        Assert(!slots[0].IsOccupied && slots[0].CurrentCube == null, "Slot 1 was vacated when Cube 1 was relocated.");
        Assert(slots[4].IsOccupied && slots[4].CurrentCube == cube1, "Slot 5 was occupied by Cube 1.");
        Assert(cube1.CurrentSlot == slots[4], "Cube 1 currentSlot references Slot 5.");

        // Reset for remaining tests
        mgr.ResetChallenge();
    }

    private static void VerifySortingValidationLogic()
    {
        Debug.Log("--- 6. Testing Sorting Validation Logic & UI State Changes ---");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/VRSortingChallenge.unity", OpenSceneMode.Single);
        var mgr = GameObject.FindObjectOfType<SortingChallengeManager>();

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
        Debug.Log("--- 7. Testing Safe Drop, ResetChallenge, and PlayAgain ---");
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

        // Verify initial staging order is strictly [3, 1, 5, 2, 4]
        int[] expectedOrder = new int[] { 3, 1, 5, 2, 4 };
        for (int i = 0; i < 5; i++)
        {
            var c = FindCubeByValue(mgr, expectedOrder[i]);
            float expectedX = -0.52f + (i * 0.26f);
            Assert(Mathf.Abs(c.transform.position.x - expectedX) < 0.05f, $"Staging slot {i + 1} contains Cube {expectedOrder[i]} at X={expectedX:F2}.");
        }

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
        Debug.Log("--- 8. Testing MainScene Navigation Integration ---");
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
        Debug.Log("--- 9. Testing Original Desktop Project Intact ---");
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

    private static void Assert(bool condition, string message)
    {
        if (condition)
        {
            _passedCount++;
            Debug.Log($"[PASS] {message}");
        }
        else
        {
            _failedCount++;
            Debug.LogError($"[FAIL] {message}");
        }
    }
}

using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Transformers;
using VR.SortingChallenge;

public static class TestGrabPipeline
{
    public static void RunTest()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/VRSortingChallenge.unity", OpenSceneMode.Single);
        Debug.Log("================ TEST COMPLETE GRAB PIPELINE EXECUTION ================");

        var mgr = UnityEngine.Object.FindObjectOfType<SortingChallengeManager>();
        var interactionMgr = UnityEngine.Object.FindObjectOfType<XRInteractionManager>();
        Assert(mgr != null, "SortingChallengeManager found.");
        Assert(interactionMgr != null, "XRInteractionManager found.");

        var cube1 = mgr.Cubes[0]; // Cube 1
        var cube2 = mgr.Cubes[1]; // Cube 2
        Assert(cube1 != null && cube2 != null, "Cubes 1 and 2 found.");

        var grab1 = cube1.GetComponent<XRGrabInteractable>();
        var grab2 = cube2.GetComponent<XRGrabInteractable>();
        Assert(grab1 != null && grab2 != null, "Cubes have XRGrabInteractable.");

        var tableTransformer1 = cube1.GetComponent<TableSurfaceGrabTransformer>();
        var tableTransformer2 = cube2.GetComponent<TableSurfaceGrabTransformer>();
        Assert(tableTransformer1 != null && tableTransformer2 != null, "Cubes have TableSurfaceGrabTransformer.");
        Assert(typeof(XRGeneralGrabTransformer).IsAssignableFrom(typeof(TableSurfaceGrabTransformer)), 
            "TableSurfaceGrabTransformer inherits from XRGeneralGrabTransformer.");

        // Find Right and Left Controller Ray Interactors
        XRRayInteractor rightRay = null;
        XRRayInteractor leftRay = null;
        foreach (var ray in UnityEngine.Object.FindObjectsOfType<XRRayInteractor>())
        {
            if (ray.transform.parent != null && ray.transform.parent.name.Contains("Right") && !ray.name.Contains("Teleport"))
            {
                rightRay = ray;
            }
            if (ray.transform.parent != null && ray.transform.parent.name.Contains("Left") && !ray.name.Contains("Teleport"))
            {
                leftRay = ray;
            }
        }
        Assert(rightRay != null, "Right Controller Ray Interactor found.");
        Assert(leftRay != null, "Left Controller Ray Interactor found.");

        // Initialize internal state via reflection
        var awakeFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        typeof(SortingChallengeManager).GetMethod("Awake", awakeFlags)?.Invoke(mgr, null);
        typeof(SortingChallengeCube).GetMethod("Awake", awakeFlags)?.Invoke(cube1, null);
        typeof(SortingChallengeCube).GetMethod("OnEnable", awakeFlags)?.Invoke(cube1, null);
        typeof(SortingChallengeCube).GetMethod("Awake", awakeFlags)?.Invoke(cube2, null);
        typeof(SortingChallengeCube).GetMethod("OnEnable", awakeFlags)?.Invoke(cube2, null);
        typeof(XRGrabInteractable).GetMethod("Awake", awakeFlags)?.Invoke(grab1, null);
        typeof(XRGrabInteractable).GetMethod("OnEnable", awakeFlags)?.Invoke(grab1, null);
        typeof(XRGrabInteractable).GetMethod("Awake", awakeFlags)?.Invoke(grab2, null);
        typeof(XRGrabInteractable).GetMethod("OnEnable", awakeFlags)?.Invoke(grab2, null);
        typeof(XRRayInteractor).GetMethod("Awake", awakeFlags)?.Invoke(rightRay, null);
        typeof(XRRayInteractor).GetMethod("OnEnable", awakeFlags)?.Invoke(rightRay, null);
        typeof(XRRayInteractor).GetMethod("Awake", awakeFlags)?.Invoke(leftRay, null);
        typeof(XRRayInteractor).GetMethod("OnEnable", awakeFlags)?.Invoke(leftRay, null);
        typeof(XRInteractionManager).GetMethod("Awake", awakeFlags)?.Invoke(interactionMgr, null);
        tableTransformer1?.ResolveTableCollider();
        tableTransformer2?.ResolveTableCollider();

        grab1.attachEaseInTime = 0f;
        grab1.smoothPosition = false;
        grab1.smoothRotation = false;
        grab2.attachEaseInTime = 0f;
        grab2.smoothPosition = false;
        grab2.smoothRotation = false;

        // ===================== TEST 1: RIGHT CONTROLLER GRAB & MOVE =====================
        Debug.Log("--- TEST 1: Right Controller Grabbing Cube 1 ---");
        interactionMgr.HoverEnter((IXRHoverInteractor)rightRay, (IXRHoverInteractable)grab1);
        Assert(grab1.isHovered, "Cube 1 is hovered by Right Ray Interactor.");

        Vector3 initialPos1 = cube1.transform.position;
        interactionMgr.SelectEnter((IXRSelectInteractor)rightRay, (IXRSelectInteractable)grab1);
        Assert(grab1.isSelected, "XRInteractionManager selected Cube 1.");
        Assert(cube1.IsGrabbed, "Cube 1 IsGrabbed is true.");

        var rightAttach = rightRay.GetAttachTransform(grab1);
        Vector3 rightMoveDelta = new Vector3(0.20f, 0.30f, 0.10f);
        Transform rightController = rightRay.transform.parent != null ? rightRay.transform.parent : rightRay.transform;
        rightController.position += rightMoveDelta;
        if (rightAttach != null) rightAttach.position += rightMoveDelta;
        Physics.SyncTransforms();

        grab1.ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase.Dynamic);
        grab1.ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase.Fixed);

        Vector3 targetPose1 = grab1.GetTargetPose().position;
        Assert(targetPose1 != initialPos1, $"Right Hand: Grab target pose moved with controller (initial: {initialPos1}, target: {targetPose1}).");
        cube1.transform.position = targetPose1;

        interactionMgr.SelectExit((IXRSelectInteractor)rightRay, (IXRSelectInteractable)grab1);
        Assert(!grab1.isSelected && !cube1.IsGrabbed, "Right Hand: Cube 1 unselected on release.");
        Assert(Vector3.Distance(cube1.transform.position, cube1.StagingPosition) < 0.05f, "Right Hand: Cube 1 safely returned to staging.");

        // ===================== TEST 2: LEFT CONTROLLER GRAB & MOVE =====================
        Debug.Log("--- TEST 2: Left Controller Grabbing Cube 2 ---");
        interactionMgr.HoverEnter((IXRHoverInteractor)leftRay, (IXRHoverInteractable)grab2);
        Assert(grab2.isHovered, "Cube 2 is hovered by Left Ray Interactor.");

        Vector3 initialPos2 = cube2.transform.position;
        interactionMgr.SelectEnter((IXRSelectInteractor)leftRay, (IXRSelectInteractable)grab2);
        Assert(grab2.isSelected, "XRInteractionManager selected Cube 2.");
        Assert(cube2.IsGrabbed, "Cube 2 IsGrabbed is true.");

        var leftAttach = leftRay.GetAttachTransform(grab2);
        Vector3 leftMoveDelta = new Vector3(-0.20f, 0.25f, 0.15f);
        Transform leftController = leftRay.transform.parent != null ? leftRay.transform.parent : leftRay.transform;
        leftController.position += leftMoveDelta;
        if (leftAttach != null) leftAttach.position += leftMoveDelta;
        Physics.SyncTransforms();

        grab2.ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase.Dynamic);
        grab2.ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase.Fixed);

        Vector3 targetPose2 = grab2.GetTargetPose().position;
        Assert(targetPose2 != initialPos2, $"Left Hand: Grab target pose moved with controller (initial: {initialPos2}, target: {targetPose2}).");
        cube2.transform.position = targetPose2;

        interactionMgr.SelectExit((IXRSelectInteractor)leftRay, (IXRSelectInteractable)grab2);
        Assert(!grab2.isSelected && !cube2.IsGrabbed, "Left Hand: Cube 2 unselected on release.");
        Assert(Vector3.Distance(cube2.transform.position, cube2.StagingPosition) < 0.05f, "Left Hand: Cube 2 safely returned to staging.");

        // ===================== TEST 3: TABLE SURFACE CONSTRAINT =====================
        Debug.Log("--- TEST 3: Table Surface Penetration Constraint ---");
        interactionMgr.SelectEnter((IXRSelectInteractor)rightRay, (IXRSelectInteractable)grab1);
        // Attempt to move controller below tabletop (tabletop is at Y=0.80)
        rightController.position = new Vector3(0f, 0.30f, 1.25f);
        if (rightAttach != null) rightAttach.position = rightController.position;
        Physics.SyncTransforms();

        grab1.ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase.Dynamic);
        Vector3 constrainedPose = grab1.GetTargetPose().position;
        Assert(constrainedPose.y >= 0.88f, $"Table Constraint: Target pose Y is constrained above table surface (Y={constrainedPose.y:F3} >= 0.88).");
        interactionMgr.SelectExit((IXRSelectInteractor)rightRay, (IXRSelectInteractable)grab1);

        // ===================== TEST 4: SLOT SNAP & PLACEMENT =====================
        Debug.Log("--- TEST 4: Slot Snapping & Placement ---");
        var targetSlot0 = mgr.Slots[0];
        Assert(!targetSlot0.IsOccupied, "Slot 0 is initially empty.");
        // Position cube right above slot 0
        cube1.transform.position = targetSlot0.transform.position + new Vector3(0.02f, 0.05f, 0.01f);
        mgr.OnCubeReleased(cube1);
        Assert(targetSlot0.IsOccupied, "Slot 0 is occupied after valid drop.");
        Assert(targetSlot0.CurrentCube == cube1, "Slot 0 occupant is Cube 1.");
        Assert(cube1.CurrentSlot == targetSlot0, "Cube 1 CurrentSlot points to Slot 0.");
        Assert(Vector3.Distance(cube1.transform.position, targetSlot0.SnapTransform.position) < 0.02f, "Cube 1 center snapped accurately to slot snap anchor.");

        Debug.Log("================ COMPLETE GRAB PIPELINE TEST FINISHED ================");
    }

    private static void Assert(bool condition, string msg)
    {
        if (condition) Debug.Log($"[PASS] {msg}");
        else Debug.LogError($"[FAIL] {msg}");
    }
}

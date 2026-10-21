using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Unity.XR.CoreUtils;
using CollisionLab;
using CollisionLab.VR;

public static class Stage9LocomotionVerifier
{
    private static int _passCount = 0;
    private static int _failCount = 0;

    private static void Assert(bool condition, string message)
    {
        if (condition)
        {
            Debug.Log($"[STAGE 9 PASS] {message}");
            _passCount++;
        }
        else
        {
            Debug.LogError($"[STAGE 9 FAIL] {message}");
            _failCount++;
        }
    }

    public static void RunAllVerifications()
    {
        _passCount = 0;
        _failCount = 0;

        Debug.Log("===============================================================================");
        Debug.Log(">>> RUNNING STAGE 9 VR LOCOMOTION & NAVIGATION VERIFICATION SUITE <<<");
        Debug.Log("===============================================================================");

        VerifyTeleportationSetup();
        VerifySnapTurnSetup();
        VerifyRayAndTeleportLayerSegregation();
        VerifyVRControlsHelpPanel();
        VerifyPhysicsAndGrabbingIntact();

        Debug.Log("===============================================================================");
        Debug.Log($"STAGE 9 VERIFICATION RESULTS: {_passCount} PASSED, {_failCount} FAILED.");
        Debug.Log("===============================================================================");

        if (_failCount > 0)
        {
            EditorApplication.Exit(1);
        }
    }

    private static void VerifyTeleportationSetup()
    {
        Debug.Log("--- Testing Teleportation Area & Destination Reticle ---");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/CollisionLab_VR.unity", OpenSceneMode.Single);
        Assert(scene.IsValid(), "CollisionLab_VR.unity loaded successfully.");

        var floor = GameObject.Find("Floor");
        Assert(floor != null, "Floor object exists in scene.");

        var teleArea = floor.GetComponent<TeleportationArea>();
        Assert(teleArea != null, "Floor has TeleportationArea component attached.");
        Assert(teleArea.customReticle != null, "TeleportationArea has customReticle assigned.");
        Assert(teleArea.customReticle.name.Contains("Reticle"), $"Custom reticle is valid reticle prefab ({teleArea.customReticle.name}).");

        // Interaction layer must have bit 31 (Teleport)
        bool hasTeleportLayer = (teleArea.interactionLayers.value & (1 << 31)) != 0;
        Assert(hasTeleportLayer, "Floor TeleportationArea has dedicated Teleport interaction layer (bit 31).");

        // TeleportationProvider presence
        var teleProvider = GameObject.FindObjectOfType<TeleportationProvider>();
        Assert(teleProvider != null, "TeleportationProvider exists in the locomotion system.");

        // Invalid surfaces check (Walls and Canvas must not be teleport areas)
        var northWall = GameObject.Find("NorthWall");
        Assert(northWall != null && northWall.GetComponent<TeleportationArea>() == null, "NorthWall cannot be teleported to.");
        var southWall = GameObject.Find("SouthWall");
        Assert(southWall != null && southWall.GetComponent<TeleportationArea>() == null, "SouthWall cannot be teleported to.");
        var eastWall = GameObject.Find("EastWall");
        Assert(eastWall != null && eastWall.GetComponent<TeleportationArea>() == null, "EastWall cannot be teleported to.");
        var westWall = GameObject.Find("WestWall");
        Assert(westWall != null && westWall.GetComponent<TeleportationArea>() == null, "WestWall cannot be teleported to.");
    }

    private static void VerifySnapTurnSetup()
    {
        Debug.Log("--- Testing Snap Turn Configuration ---");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/CollisionLab_VR.unity", OpenSceneMode.Single);

        var snapTurn = GameObject.FindObjectOfType<ActionBasedSnapTurnProvider>();
        Assert(snapTurn != null, "ActionBasedSnapTurnProvider exists in the scene.");
        Assert(Mathf.Approximately(snapTurn.turnAmount, 45f), $"Snap turn amount is 45 degrees ({snapTurn.turnAmount}).");
        Assert(snapTurn.debounceTime >= 0.4f, $"Debounce time is configured to prevent repeated turns ({snapTurn.debounceTime}s).");
        Assert(snapTurn.enableTurnLeftRight, "Snap turn left and right is enabled.");
    }

    private static void VerifyRayAndTeleportLayerSegregation()
    {
        Debug.Log("--- Testing Ray Interactor & Teleport Layer Segregation ---");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/CollisionLab_VR.unity", OpenSceneMode.Single);

        var rayInteractors = GameObject.FindObjectsOfType<XRRayInteractor>();
        Assert(rayInteractors.Length >= 2, $"Found {rayInteractors.Length} ray interactors.");

        // Separate ray interactors for UI/manipulation vs teleportation
        int uiOrGrabRays = 0;
        int teleportRays = 0;

        foreach (var r in rayInteractors)
        {
            if ((r.interactionLayers.value & (1 << 31)) != 0)
            {
                teleportRays++;
            }
            if ((r.interactionLayers.value & 1) != 0)
            {
                uiOrGrabRays++;
            }
        }

        Assert(teleportRays >= 2, $"Found {teleportRays} Teleport Interactor rays on controllers.");
        Assert(uiOrGrabRays >= 2, $"Found {uiOrGrabRays} UI/Grab Interactor rays on controllers.");

        // Verify Floor does not have Default layer (bit 0) in TeleportationArea
        var floor = GameObject.Find("Floor");
        var teleArea = floor.GetComponent<TeleportationArea>();
        bool floorHasDefaultLayer = (teleArea.interactionLayers.value & 1) != 0;
        Assert(!floorHasDefaultLayer, "Floor TeleportationArea excludes Default layer (bit 0), preventing UI/Teleport ray conflicts.");
    }

    private static void VerifyVRControlsHelpPanel()
    {
        Debug.Log("--- Testing VR CONTROLS World-Space Help Panel ---");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/CollisionLab_VR.unity", OpenSceneMode.Single);

        var helpCanvasGO = GameObject.Find("VR_Controls_Canvas");
        Assert(helpCanvasGO != null, "VR_Controls_Canvas exists in the scene.");

        var canvas = helpCanvasGO.GetComponent<Canvas>();
        Assert(canvas != null && canvas.renderMode == RenderMode.WorldSpace, "VR_Controls_Canvas renderMode is WorldSpace.");

        var rt = helpCanvasGO.GetComponent<RectTransform>();
        Assert(rt.localScale.x > 0.001f && rt.localScale.x < 0.005f, $"VR_Controls_Canvas scale is readable ({rt.localScale.x}).");

        var raycaster = helpCanvasGO.GetComponent<TrackedDeviceGraphicRaycaster>();
        Assert(raycaster != null, "VR_Controls_Canvas has TrackedDeviceGraphicRaycaster.");

        // Check key text elements
        var texts = helpCanvasGO.GetComponentsInChildren<Text>(true);
        Assert(texts.Length >= 8, $"VR_Controls_Canvas contains comprehensive instructional texts ({texts.Length} text elements found).");

        bool hasTeleportGuide = false;
        bool hasSnapTurnGuide = false;
        bool hasGrabbingGuide = false;
        bool hasHeadLookGuide = false;

        foreach (var t in texts)
        {
            if (t.text.Contains("TELEPORT")) hasTeleportGuide = true;
            if (t.text.Contains("SNAP TURN")) hasSnapTurnGuide = true;
            if (t.text.Contains("GRABBING")) hasGrabbingGuide = true;
            if (t.text.Contains("HEAD")) hasHeadLookGuide = true;
        }

        Assert(hasTeleportGuide, "Help panel includes Teleport Locomotion instructions.");
        Assert(hasSnapTurnGuide, "Help panel includes Snap Turn instructions.");
        Assert(hasGrabbingGuide, "Help panel includes Physics Grabbing instructions.");
        Assert(hasHeadLookGuide, "Help panel includes Head Look & Walk instructions.");
    }

    private static void VerifyPhysicsAndGrabbingIntact()
    {
        Debug.Log("--- Testing Preservation of Physics & Grabbing ---");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/CollisionLab_VR.unity", OpenSceneMode.Single);

        var sphere = GameObject.Find("Sphere");
        Assert(sphere != null && sphere.GetComponent<VRCollisionObjectGrab>() != null, "Sphere has VRCollisionObjectGrab intact.");

        var cube = GameObject.Find("Cube");
        Assert(cube != null && cube.GetComponent<VRCollisionObjectGrab>() != null, "Cube has VRCollisionObjectGrab intact.");

        var calibCube = GameObject.Find("VRCalibrationCube");
        Assert(calibCube != null && calibCube.GetComponent<XRGrabInteractable>() != null, "VRCalibrationCube has XRGrabInteractable intact.");

        var mgr = GameObject.FindObjectOfType<CollisionLabManager>();
        Assert(mgr != null, "CollisionLabManager is present and intact.");

        var menuCanvas = GameObject.Find("VR_WorldSpace_Canvas");
        Assert(menuCanvas != null, "World-Space Physics Menu Canvas is present and intact.");
    }
}

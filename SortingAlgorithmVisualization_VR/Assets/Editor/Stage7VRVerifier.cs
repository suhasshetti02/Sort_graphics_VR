using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using Unity.XR.CoreUtils;
using CollisionLab;
using CollisionLab.VR;
using UI;

public static class Stage7VRVerifier
{
    private static int _passCount = 0;
    private static int _failCount = 0;

    private static void Assert(bool condition, string message)
    {
        if (condition)
        {
            Debug.Log($"[STAGE 7 PASS] {message}");
            _passCount++;
        }
        else
        {
            Debug.LogError($"[STAGE 7 FAIL] {message}");
            _failCount++;
        }
    }

    public static void RunAllVerifications()
    {
        _passCount = 0;
        _failCount = 0;

        Debug.Log("===============================================================================");
        Debug.Log(">>> RUNNING STAGE 7 VR PROTOTYPE & COMPATIBILITY VERIFICATION SUITE <<<");
        Debug.Log("===============================================================================");

        VerifyBuildSettings();
        VerifyMainScene();
        VerifyDesktopCollisionLab();
        VerifyVRCollisionLab();
        VerifyPhysicsAndTelemetryInVR();

        Debug.Log("===============================================================================");
        Debug.Log($"STAGE 7 VERIFICATION RESULTS: {_passCount} PASSED, {_failCount} FAILED.");
        Debug.Log("===============================================================================");

        if (_failCount > 0)
        {
            EditorApplication.Exit(1);
        }
    }

    private static void VerifyBuildSettings()
    {
        Debug.Log("--- Checking Build Settings ---");
        var scenes = EditorBuildSettings.scenes;
        Assert(scenes.Length >= 3, $"Build settings should have at least 3 scenes, found: {scenes.Length}");
        Assert(scenes[0].path == "Assets/Scenes/MainScene.unity", $"Scene 0 must be MainScene.unity, found: {scenes[0].path}");
        Assert(scenes[1].path == "Assets/Scenes/CollisionLab.unity", $"Scene 1 must be CollisionLab.unity, found: {scenes[1].path}");
        Assert(scenes[2].path == "Assets/Scenes/CollisionLab_VR.unity", $"Scene 2 must be CollisionLab_VR.unity, found: {scenes[2].path}");
        Assert(scenes[0].enabled && scenes[1].enabled && scenes[2].enabled, "All three scenes in Build Settings must be enabled.");
    }

    private static void VerifyMainScene()
    {
        Debug.Log("--- Testing MainScene.unity ---");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity", OpenSceneMode.Single);
        Assert(scene.IsValid(), "MainScene.unity opened successfully.");

        var gm = GameObject.FindObjectOfType<GameManager>();
        Assert(gm != null, "GameManager is present in MainScene.");

        var cam = Camera.main;
        Assert(cam != null, "Main Camera is present in MainScene.");

        var playBar = GameObject.FindObjectOfType<PlayBar>();
        Assert(playBar != null, "PlayBar is present in MainScene.");
    }

    private static void VerifyDesktopCollisionLab()
    {
        Debug.Log("--- Testing Desktop CollisionLab.unity ---");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/CollisionLab.unity", OpenSceneMode.Single);
        Assert(scene.IsValid(), "CollisionLab.unity opened successfully.");

        var mgr = GameObject.FindObjectOfType<CollisionLabManager>();
        Assert(mgr != null, "CollisionLabManager is present in desktop CollisionLab.");

        var sphere = GameObject.Find("Sphere");
        var cube = GameObject.Find("Cube");
        Assert(sphere != null && cube != null, "Sphere and Cube are present in desktop CollisionLab.");

        var canvas = GameObject.Find("Canvas");
        Assert(canvas != null, "Desktop Canvas is present in CollisionLab.");
        var cComp = canvas.GetComponent<Canvas>();
        Assert(cComp.renderMode != RenderMode.WorldSpace, "Desktop Canvas is Screen Space (intact).");
    }

    private static void VerifyVRCollisionLab()
    {
        Debug.Log("--- Testing CollisionLab_VR.unity ---");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/CollisionLab_VR.unity", OpenSceneMode.Single);
        Assert(scene.IsValid(), "CollisionLab_VR.unity opened successfully.");

        // 1. XR Interaction Setup & Origin
        var xrSetup = GameObject.Find("XR Interaction Setup");
        Assert(xrSetup != null, "XR Interaction Setup is present in scene.");

        var origin = GameObject.FindObjectOfType<XROrigin>();
        Assert(origin != null, "XROrigin is present.");
        Assert(origin.Camera != null, "XROrigin has XR Camera attached.");

        var xrManager = GameObject.FindObjectOfType<XRInteractionManager>();
        Assert(xrManager != null, "XRInteractionManager is present.");

        // 2. Controller Interactors
        var rayInteractors = GameObject.FindObjectsOfType<XRRayInteractor>();
        Assert(rayInteractors.Length >= 2, $"Found {rayInteractors.Length} XRRayInteractors (Left & Right controllers).");

        var directInteractors = GameObject.FindObjectsOfType<XRDirectInteractor>();
        Assert(directInteractors.Length >= 2, $"Found {directInteractors.Length} XRDirectInteractors (Left & Right controllers).");

        // 3. XR Device Simulator
        var sim = GameObject.FindObjectOfType<XRDeviceSimulator>();
        Assert(sim != null, "XRDeviceSimulator is present for keyboard/mouse simulation.");

        // 4. Locomotion / Teleportation
        var floor = GameObject.Find("Floor");
        Assert(floor != null, "Floor object exists.");
        var teleArea = floor.GetComponent<TeleportationArea>();
        Assert(teleArea != null, "Floor has TeleportationArea component for comfortable teleport locomotion.");

        var teleProvider = GameObject.FindObjectOfType<TeleportationProvider>();
        Assert(teleProvider != null, "TeleportationProvider is present in locomotion system.");

        var snapTurn = GameObject.FindObjectOfType<ActionBasedSnapTurnProvider>();
        Assert(snapTurn != null, "ActionBasedSnapTurnProvider is present for snap-turn navigation.");

        // 5. Grabbable Object
        var grabCube = GameObject.Find("VRCalibrationCube");
        Assert(grabCube != null, "VRCalibrationCube is present on pedestal.");
        var grabInteractable = grabCube.GetComponent<XRGrabInteractable>();
        Assert(grabInteractable != null, "VRCalibrationCube has XRGrabInteractable.");
        Assert(grabCube.GetComponent<Rigidbody>() != null, "VRCalibrationCube has Rigidbody.");

        // 6. World-Space Canvas
        var vrCanvas = GameObject.Find("VR_WorldSpace_Canvas");
        Assert(vrCanvas != null, "VR_WorldSpace_Canvas is present.");
        var canvasComp = vrCanvas.GetComponent<Canvas>();
        Assert(canvasComp != null && canvasComp.renderMode == RenderMode.WorldSpace, "Canvas renderMode is WorldSpace.");

        var raycaster = vrCanvas.GetComponent<TrackedDeviceGraphicRaycaster>();
        Assert(raycaster != null, "Canvas has TrackedDeviceGraphicRaycaster for VR pointer rays.");

        var eventSystem = GameObject.FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        Assert(eventSystem != null, "EventSystem is present.");
        var xrUIModule = eventSystem.GetComponent<XRUIInputModule>();
        Assert(xrUIModule != null, "XRUIInputModule is present on EventSystem.");

        // 7. Guarded XR Bootstrap
        var bootstrap = GameObject.FindObjectOfType<XRSceneBootstrap>();
        Assert(bootstrap != null, "XRSceneBootstrap is present for guarded XR lifecycle.");

        // 8. CollisionLabManager & Wiring
        var mgr = GameObject.FindObjectOfType<CollisionLabManager>();
        Assert(mgr != null, "CollisionLabManager is present in VR scene.");

        var so = new SerializedObject(mgr);
        Assert(so.FindProperty("sphereObject").objectReferenceValue != null, "CollisionLabManager.sphereObject is wired.");
        Assert(so.FindProperty("cubeObject").objectReferenceValue != null, "CollisionLabManager.cubeObject is wired.");
        Assert(so.FindProperty("launchButton").objectReferenceValue != null, "CollisionLabManager.launchButton is wired.");
        Assert(so.FindProperty("resetButton").objectReferenceValue != null, "CollisionLabManager.resetButton is wired.");
        Assert(so.FindProperty("speedSlider").objectReferenceValue != null, "CollisionLabManager.speedSlider is wired.");
        Assert(so.FindProperty("sphereMassSlider").objectReferenceValue != null, "CollisionLabManager.sphereMassSlider is wired.");
        Assert(so.FindProperty("cubeMassSlider").objectReferenceValue != null, "CollisionLabManager.cubeMassSlider is wired.");
        Assert(so.FindProperty("restitutionSlider").objectReferenceValue != null, "CollisionLabManager.restitutionSlider is wired.");
        Assert(so.FindProperty("presetEqualButton").objectReferenceValue != null, "CollisionLabManager.presetEqualButton is wired.");
        Assert(so.FindProperty("stateText").objectReferenceValue != null, "CollisionLabManager.stateText is wired.");
        Assert(so.FindProperty("impulseText").objectReferenceValue != null, "CollisionLabManager.impulseText is wired.");
        Assert(so.FindProperty("momentumErrorText").objectReferenceValue != null, "CollisionLabManager.momentumErrorText is wired.");
    }

    private static void VerifyPhysicsAndTelemetryInVR()
    {
        Debug.Log("--- Testing Physics & Telemetry In CollisionLab_VR ---");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/CollisionLab_VR.unity", OpenSceneMode.Single);

        var mgr = GameObject.FindObjectOfType<CollisionLabManager>();
        var sphereObj = GameObject.Find("Sphere");
        var cubeObj = GameObject.Find("Cube");

        var sphereVec = sphereObj.GetComponent<PhysicsVectorVisualizer>();
        Assert(sphereVec != null, "Sphere has PhysicsVectorVisualizer.");

        var cubeVec = cubeObj.GetComponent<PhysicsVectorVisualizer>();
        Assert(cubeVec != null, "Cube has PhysicsVectorVisualizer.");

        var manifold = GameObject.FindObjectOfType<ContactManifoldVisualizer>();
        Assert(manifold != null, "ContactManifoldVisualizer is present for collision point/normal.");

        // Test Launch and Reset methods invocation
        mgr.ResetSimulation();
        Assert(mgr.CurrentState == SimulationState.Ready, "Simulation state is Ready after ResetSimulation().");

        mgr.LaunchSphere();
        Assert(mgr.CurrentState == SimulationState.InMotion, "Simulation state is InMotion after LaunchSphere().");

        var sphereRb = sphereObj.GetComponent<Rigidbody>();
        Assert(sphereRb.velocity.magnitude > 0.01f, $"Sphere velocity is active after launch ({sphereRb.velocity.magnitude:F2} m/s).");

        mgr.ResetSimulation();
        Assert(mgr.CurrentState == SimulationState.Ready, "Simulation state returned to Ready after reset.");
        Assert(sphereRb.velocity.magnitude < 0.001f, "Sphere velocity is zeroed after reset.");
    }

    public static void InitializeOpenXRSettings()
    {
        try
        {
            var openxrEditorAsm = Assembly.Load("Unity.XR.OpenXR.Editor");
            if (openxrEditorAsm != null)
            {
                var pkgSettingsType = openxrEditorAsm.GetType("UnityEditor.XR.OpenXR.OpenXRPackageSettings");
                if (pkgSettingsType != null)
                {
                    var getOrCreate = pkgSettingsType.GetMethod("GetOrCreateInstance", BindingFlags.Public | BindingFlags.Static);
                    if (getOrCreate != null)
                    {
                        var instance = getOrCreate.Invoke(null, null);
                        Debug.Log("[InitializeOpenXRSettings] Initialized OpenXRPackageSettings: " + (instance != null));
                        AssetDatabase.SaveAssets();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[InitializeOpenXRSettings] Note: " + ex.Message);
        }
    }

    public static void BuildStandalonePlayer()
    {
        Debug.Log("===============================================================================");
        Debug.Log(">>> BUILDING STANDALONE WINDOWS PLAYER (NO HEADSET ATTACHED TEST) <<<");
        Debug.Log("===============================================================================");

        // Pre-initialize OpenXR settings so BuildPlayer doesn't fail with 'not yet loaded'
        InitializeOpenXRSettings();

        string buildDir = "Builds/SortLab3D-VR-Windows";
        Directory.CreateDirectory(buildDir);
        string exePath = Path.Combine(buildDir, "SortLab3D.exe");

        var scenes = new string[]
        {
            "Assets/Scenes/MainScene.unity",
            "Assets/Scenes/CollisionLab.unity",
            "Assets/Scenes/CollisionLab_VR.unity"
        };

        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = exePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var summary = report.summary;

        Debug.Log($"Build Result: {summary.result}");
        Debug.Log($"Total Errors: {summary.totalErrors}");
        Debug.Log($"Total Warnings: {summary.totalWarnings}");
        Debug.Log($"Total Size: {summary.totalSize} bytes");

        if (summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.LogError($"[STAGE 7 BUILD FAIL] Build failed with {summary.totalErrors} errors.");
            EditorApplication.Exit(1);
        }
        else
        {
            Debug.Log($"[STAGE 7 BUILD PASS] Standalone Windows build succeeded cleanly at: {exePath}");
        }
    }
}

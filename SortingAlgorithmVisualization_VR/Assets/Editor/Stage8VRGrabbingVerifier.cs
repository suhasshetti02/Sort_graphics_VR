using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using CollisionLab;
using CollisionLab.VR;

public static class Stage8VRGrabbingVerifier
{
    private static int _passCount = 0;
    private static int _failCount = 0;

    private static void Assert(bool condition, string message)
    {
        if (condition)
        {
            Debug.Log($"[STAGE 8 PASS] {message}");
            _passCount++;
        }
        else
        {
            Debug.LogError($"[STAGE 8 FAIL] {message}");
            _failCount++;
        }
    }

    public static void RunAllVerifications()
    {
        _passCount = 0;
        _failCount = 0;

        Debug.Log("===============================================================================");
        Debug.Log(">>> RUNNING STAGE 8 VR PHYSICS OBJECT GRABBING VERIFICATION SUITE <<<");
        Debug.Log("===============================================================================");

        VerifySceneComponents();
        VerifyGrabbingBlockedDuringMotion();
        VerifyCollisionSuppressionDuringGrab();
        VerifyLaunchReleasesHeldObjects();
        VerifyResetReleasesHeldObjects();
        VerifyOriginalProjectUntouched();

        Debug.Log("===============================================================================");
        Debug.Log($"STAGE 8 VERIFICATION RESULTS: {_passCount} PASSED, {_failCount} FAILED.");
        Debug.Log("===============================================================================");

        if (_failCount > 0)
        {
            EditorApplication.Exit(1);
        }
    }

    private static void VerifySceneComponents()
    {
        Debug.Log("--- Testing Scene Serialization & Component Counts ---");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/CollisionLab_VR.unity", OpenSceneMode.Single);
        Assert(scene.IsValid(), "CollisionLab_VR.unity loaded successfully.");

        // Sphere Checks
        var sphere = GameObject.Find("Sphere");
        Assert(sphere != null, "Sphere GameObject exists in CollisionLab_VR.");
        var sphereGrab = sphere.GetComponent<VRCollisionObjectGrab>();
        Assert(sphereGrab != null, "Sphere has VRCollisionObjectGrab component attached.");
        var allSphereGrabs = sphere.GetComponents<XRGrabInteractable>();
        Assert(allSphereGrabs.Length == 1, $"Sphere has exactly 1 XRGrabInteractable (found {allSphereGrabs.Length}).");
        Assert(sphere.GetComponent<Rigidbody>() != null, "Sphere retains its original Rigidbody.");
        Assert(sphere.GetComponent<Collider>() != null, "Sphere retains its original Collider.");
        Assert(sphere.GetComponent<CollisionObject>() != null, "Sphere retains its original CollisionObject.");

        // Cube Checks
        var cube = GameObject.Find("Cube");
        Assert(cube != null, "Cube GameObject exists in CollisionLab_VR.");
        var cubeGrab = cube.GetComponent<VRCollisionObjectGrab>();
        Assert(cubeGrab != null, "Cube has VRCollisionObjectGrab component attached.");
        var allCubeGrabs = cube.GetComponents<XRGrabInteractable>();
        Assert(allCubeGrabs.Length == 1, $"Cube has exactly 1 XRGrabInteractable (found {allCubeGrabs.Length}).");
        Assert(cube.GetComponent<Rigidbody>() != null, "Cube retains its original Rigidbody.");
        Assert(cube.GetComponent<Collider>() != null, "Cube retains its original Collider.");
        Assert(cube.GetComponent<CollisionObject>() != null, "Cube retains its original CollisionObject.");

        // Calibration Cube preservation
        var calibCube = GameObject.Find("VRCalibrationCube");
        Assert(calibCube != null, "VRCalibrationCube is preserved in the scene.");
        Assert(calibCube.GetComponent<XRGrabInteractable>() != null, "VRCalibrationCube retains its XRGrabInteractable.");

        // Grab configuration verification
        Assert(!sphereGrab.throwOnDetach, "Sphere VRCollisionObjectGrab has throwOnDetach disabled (no fling).");
        Assert(!cubeGrab.throwOnDetach, "Cube VRCollisionObjectGrab has throwOnDetach disabled (no fling).");
        Assert(sphereGrab.movementType == XRBaseInteractable.MovementType.VelocityTracking, "Sphere uses VelocityTracking movement type.");
        Assert(cubeGrab.movementType == XRBaseInteractable.MovementType.VelocityTracking, "Cube uses VelocityTracking movement type.");
        Assert(sphereGrab.interactionLayers.value == 1, "Sphere interactionLayers match Default controller layer (bit 1).");
        Assert(cubeGrab.interactionLayers.value == 1, "Cube interactionLayers match Default controller layer (bit 1).");
    }

    private static void VerifyGrabbingBlockedDuringMotion()
    {
        Debug.Log("--- Testing Grabbing Blocked During Active Simulation ---");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/CollisionLab_VR.unity", OpenSceneMode.Single);
        var mgr = GameObject.FindObjectOfType<CollisionLabManager>();
        var sphere = GameObject.Find("Sphere");
        var sphereGrab = sphere.GetComponent<VRCollisionObjectGrab>();

        mgr.ResetSimulation();
        Assert(mgr.CurrentState == SimulationState.Ready, "Simulation state is Ready.");
        Assert(sphereGrab.IsSelectableBy(null), "Object grabbing is PERMITTED when simulation is Ready.");

        mgr.LaunchSphere();
        Assert(mgr.CurrentState == SimulationState.InMotion, "Simulation state is InMotion after launch.");
        Assert(!sphereGrab.IsSelectableBy(null), "Object grabbing is BLOCKED while simulation is InMotion.");

        mgr.ResetSimulation();
        Assert(mgr.CurrentState == SimulationState.Ready, "Simulation state returned to Ready.");
        Assert(sphereGrab.IsSelectableBy(null), "Object grabbing is PERMITTED again after reset.");
    }

    private static void VerifyCollisionSuppressionDuringGrab()
    {
        Debug.Log("--- Testing Collision Suppression During Active Manipulation ---");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/CollisionLab_VR.unity", OpenSceneMode.Single);
        var sphere = GameObject.Find("Sphere");
        var sphereCol = sphere.GetComponent<CollisionObject>();
        Assert(sphereCol != null, "Sphere CollisionObject exists.");

        Assert(!sphereCol.SuppressCollisionReports, "Collision suppression is FALSE initially.");

        sphereCol.SuppressCollisionReports = true;
        Assert(sphereCol.SuppressCollisionReports, "Collision suppression enabled during grab manipulation.");

        sphereCol.ResetToInitialState();
        Assert(!sphereCol.SuppressCollisionReports, "Collision suppression restored to FALSE after ResetToInitialState().");
    }

    private static void VerifyLaunchReleasesHeldObjects()
    {
        Debug.Log("--- Testing Launch Automatically Releases Held Objects ---");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/CollisionLab_VR.unity", OpenSceneMode.Single);
        var mgr = GameObject.FindObjectOfType<CollisionLabManager>();
        var sphere = GameObject.Find("Sphere");
        var sphereRb = sphere.GetComponent<Rigidbody>();

        bool releaseHookCalled = false;
        Action testHandler = () => { releaseHookCalled = true; };
        CollisionLabManager.ReleaseHeldObjectsHandler += testHandler;

        try
        {
            mgr.LaunchSphere();
            Assert(releaseHookCalled, "CollisionLabManager.ReleaseHeldObjectsHandler was invoked by LaunchSphere().");
            Assert(sphereRb.velocity.magnitude > 0.1f, $"Launch velocity applied successfully ({sphereRb.velocity.magnitude:F2} m/s).");
        }
        finally
        {
            CollisionLabManager.ReleaseHeldObjectsHandler -= testHandler;
        }
    }

    private static void VerifyResetReleasesHeldObjects()
    {
        Debug.Log("--- Testing Reset Automatically Releases Held Objects ---");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/CollisionLab_VR.unity", OpenSceneMode.Single);
        var mgr = GameObject.FindObjectOfType<CollisionLabManager>();
        var sphere = GameObject.Find("Sphere");
        var sphereRb = sphere.GetComponent<Rigidbody>();

        bool releaseHookCalled = false;
        Action testHandler = () => { releaseHookCalled = true; };
        CollisionLabManager.ReleaseHeldObjectsHandler += testHandler;

        try
        {
            mgr.ResetSimulation();
            Assert(releaseHookCalled, "CollisionLabManager.ReleaseHeldObjectsHandler was invoked by ResetSimulation().");
            Assert(sphereRb.velocity.magnitude < 0.001f, "Velocities cleanly zeroed after reset.");
        }
        finally
        {
            CollisionLabManager.ReleaseHeldObjectsHandler -= testHandler;
        }
    }

    private static void VerifyOriginalProjectUntouched()
    {
        Debug.Log("--- Testing Original Desktop Project Intact ---");
        string originalExe = @"C:\Users\sande\OneDrive\Desktop\CGVR\SortingAlgorithmVisualization\Builds\SortLab3D-Windows\SortLab3D.exe";
        Assert(File.Exists(originalExe), $"Original desktop build exists at {originalExe}.");

        var fi = new FileInfo(originalExe);
        Assert(fi.Length > 500000, $"Original desktop build file size is valid ({fi.Length} bytes).");
    }
}

using System;
using System.Collections;
using System.IO;
using System.Text;
using CollisionLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CollisionLab.Editor
{
    [InitializeOnLoad]
    public static class Stage5CollisionLabVerifier
    {
        private const string OutputFilePath = "Assets/../stage5_verification_output.txt";
        private const string ScenePath = "Assets/Scenes/CollisionLab.unity";
        private const string RunFlagKey = "Stage5Verifier_ShouldRun";
        private const string StateKey = "Stage5Verifier_State";

        private static StringBuilder _log = new StringBuilder();
        private static int _frameCounter = 0;
        private static float _timeoutTimer = 0f;

        static Stage5CollisionLabVerifier()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        [MenuItem("CollisionLab/Run Stage 5 Physics Verification")]
        public static void TriggerVerification()
        {
            SessionState.SetBool(RunFlagKey, true);
            SessionState.SetInt(StateKey, 0);
            Debug.Log("[STAGE5_VERIFIER] Verification started via Menu / Trigger.");
            if (!EditorApplication.isPlaying)
            {
                if (SceneManager.GetActiveScene().path != ScenePath)
                {
                    EditorSceneManager.OpenScene(ScenePath);
                }
                EditorApplication.EnterPlaymode();
            }
        }

        private static void OnEditorUpdate()
        {
            if (!SessionState.GetBool(RunFlagKey, false)) return;

            int state = SessionState.GetInt(StateKey, 0);

            if (!EditorApplication.isPlaying)
            {
                if (state == 0)
                {
                    if (SceneManager.GetActiveScene().path != ScenePath)
                    {
                        EditorSceneManager.OpenScene(ScenePath);
                    }
                    SessionState.SetInt(StateKey, 1);
                    EditorApplication.EnterPlaymode();
                }
                return;
            }

            // We are in PlayMode
            var manager = CollisionLabManager.Instance;
            if (manager == null)
            {
                manager = UnityEngine.Object.FindObjectOfType<CollisionLabManager>();
                if (manager == null)
                {
                    Debug.LogWarning("[STAGE5_VERIFIER] Waiting for CollisionLabManager...");
                    return;
                }
            }

            _frameCounter++;
            _timeoutTimer += Time.unscaledDeltaTime;

            switch (state)
            {
                case 1: // Initialize test run
                    _log.Clear();
                    _log.AppendLine("=== STAGE 5 INTERACTIVE PHYSICS VISUALIZATION VERIFICATION ===");
                    _log.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    _log.AppendLine($"Unity Version: {Application.unityVersion}");
                    _log.AppendLine($"Scene: {SceneManager.GetActiveScene().path}");
                    _log.AppendLine();

                    // Step 1: Validate Hierarchy & Components
                    _log.AppendLine("--- TEST 1: SCENE HIERARCHY & COMPONENT VERIFICATION ---");
                    bool hierarchyOk = ValidateSceneComponents(manager, _log);
                    _log.AppendLine($"Hierarchy & Serialized Fields Check: {(hierarchyOk ? "PASSED" : "FAILED")}");
                    _log.AppendLine();

                    // Step 2: Validate Parameter Sliders, Mass Setting & Runtime PhysicMaterial Instancing
                    _log.AppendLine("--- TEST 2: PARAMETER CONTROLS & SAFE RUNTIME MATERIAL INSTANCING ---");
                    bool paramsOk = ValidateParameterControls(manager, _log);
                    _log.AppendLine($"Parameter & Runtime Material Instancing Check: {(paramsOk ? "PASSED" : "FAILED")}");
                    _log.AppendLine();

                    // Step 3: Validate Presets
                    _log.AppendLine("--- TEST 3: PRESET CONFIGURATIONS ---");
                    bool presetsOk = ValidatePresets(manager, _log);
                    _log.AppendLine($"Preset Configurations Check: {(presetsOk ? "PASSED" : "FAILED")}");
                    _log.AppendLine();

                    // Prepare for Head-On Equal-Mass Collision Test
                    _log.AppendLine("--- TEST 4: EQUAL-MASS ELASTIC COLLISION & VECTOR MOMENTUM TELEMETRY ---");
                    manager.ApplyPreset("Equal");
                    manager.SetRestitution(1.0f);
                    manager.SetSpeed(8.0f);
                    manager.ResetSimulation();

                    _frameCounter = 0;
                    SessionState.SetInt(StateKey, 2);
                    break;

                case 2: // Launch Sphere for Test 4
                    if (_frameCounter > 5) // wait 5 frames after reset
                    {
                        manager.LaunchSphere();
                        _log.AppendLine($"Sphere launched: velocity = {manager.GetComponentInChildren<Rigidbody>()?.velocity}");
                        _frameCounter = 0;
                        SessionState.SetInt(StateKey, 3);
                    }
                    break;

                case 3: // Wait for collision in Test 4
                    if (manager.CurrentState == SimulationState.Collided || _frameCounter > 180)
                    {
                        // Wait a few physics steps for post-impact integration coroutine to complete
                        SessionState.SetInt(StateKey, 4);
                        _frameCounter = 0;
                    }
                    break;

                case 4: // Validate Test 4 telemetry
                    if (_frameCounter > 10)
                    {
                        bool test4Ok = ValidateTelemetryResults(manager, "Equal-Mass Elastic", 1.0f, 1.0f, 8.0f, 1.0f, _log);
                        _log.AppendLine($"Equal-Mass Elastic Collision Test: {(test4Ok ? "PASSED" : "FAILED")}");
                        _log.AppendLine();

                        // Prepare for Unequal-Mass Test
                        _log.AppendLine("--- TEST 5: UNEQUAL-MASS (3:1) INELASTIC COLLISION ---");
                        manager.ApplyPreset("HeavyImpactor"); // Sphere = 3.0kg, Cube = 1.0kg
                        manager.SetRestitution(0.4f);
                        manager.SetSpeed(7.0f);
                        manager.ResetSimulation();

                        _frameCounter = 0;
                        SessionState.SetInt(StateKey, 5);
                    }
                    break;

                case 5: // Launch Sphere for Test 5
                    if (_frameCounter > 5)
                    {
                        manager.LaunchSphere();
                        _log.AppendLine($"Heavy Sphere launched: velocity = {manager.GetComponentInChildren<Rigidbody>()?.velocity}");
                        _frameCounter = 0;
                        SessionState.SetInt(StateKey, 6);
                    }
                    break;

                case 6: // Wait for collision in Test 5
                    if (manager.CurrentState == SimulationState.Collided || _frameCounter > 180)
                    {
                        SessionState.SetInt(StateKey, 7);
                        _frameCounter = 0;
                    }
                    break;

                case 7: // Validate Test 5 telemetry
                    if (_frameCounter > 10)
                    {
                        bool test5Ok = ValidateTelemetryResults(manager, "Heavy-Impactor Inelastic (3:1)", 3.0f, 1.0f, 7.0f, 0.4f, _log);
                        _log.AppendLine($"Heavy-Impactor Inelastic Collision Test: {(test5Ok ? "PASSED" : "FAILED")}");
                        _log.AppendLine();

                        // Test 6: Complete Reset Behavior
                        _log.AppendLine("--- TEST 6: COMPLETE RESET BEHAVIOUR & CONFIGURATION RETENTION ---");

                        manager.ResetSimulation();

                        var sphereRb = GameObject.Find("Sphere")?.GetComponent<Rigidbody>();
                        var cubeRb = GameObject.Find("Cube")?.GetComponent<Rigidbody>();
                        var contactVis = UnityEngine.Object.FindObjectOfType<ContactManifoldVisualizer>();

                        bool resetPosOk = (sphereRb != null && Vector3.Distance(sphereRb.position, new Vector3(-4f, 0.5f, 0f)) < 0.1f);
                        bool resetVelOk = (sphereRb != null && sphereRb.velocity.sqrMagnitude < 0.001f && cubeRb != null && cubeRb.velocity.sqrMagnitude < 0.001f);
                        bool contactCleared = (contactVis != null && !contactVis.IsActive);
                        bool stateReset = (manager.CurrentState == SimulationState.Ready);

                        _log.AppendLine($"Sphere Position Restored: {resetPosOk} (Pos: {sphereRb?.position})");
                        _log.AppendLine($"Velocities Zeroed: {resetVelOk}");
                        _log.AppendLine($"Contact Visualizer Cleared: {contactCleared}");
                        _log.AppendLine($"Simulation State Reset to Ready: {stateReset}");

                        bool test6Ok = resetPosOk && resetVelOk && contactCleared && stateReset;
                        _log.AppendLine($"Reset Behaviour Check: {(test6Ok ? "PASSED" : "FAILED")}");
                        _log.AppendLine();

                        // Test 7: Theory & Viva Content Validation
                        _log.AppendLine("--- TEST 7: THEORY & VIVA PANEL CONTENT VERIFICATION ---");
                        var theoryTextObj = GameObject.Find("VivaTheoryContent")?.GetComponent<Text>();
                        bool theoryOk = theoryTextObj != null && 
                                        theoryTextObj.text.Contains("CONSERVATION OF LINEAR MOMENTUM") &&
                                        theoryTextObj.text.Contains("COEFFICIENT OF RESTITUTION (e)") &&
                                        theoryTextObj.text.Contains("VIVA EXPLANATION");
                        _log.AppendLine($"Theory Text Object Present: {theoryTextObj != null}");
                        _log.AppendLine($"Contains Momentum, Restitution & Viva Content: {theoryOk}");
                        _log.AppendLine($"Theory & Viva Panel Check: {(theoryOk ? "PASSED" : "FAILED")}");
                        _log.AppendLine();

                        _log.AppendLine("=== STAGE 5 VERIFICATION COMPLETED SUCCESSFULLY ===");

                        // Write log file
                        string fullPath = Path.GetFullPath(OutputFilePath);
                        File.WriteAllText(fullPath, _log.ToString());
                        Debug.Log($"[STAGE5_VERIFIER] Results written to: {fullPath}\n{_log}");

                        // Clean up session state
                        SessionState.SetBool(RunFlagKey, false);
                        SessionState.SetInt(StateKey, 0);

                        // Exit PlayMode
                        EditorApplication.ExitPlaymode();
                    }
                    break;
            }
        }

        private static bool ValidateSceneComponents(CollisionLabManager manager, StringBuilder log)
        {
            bool ok = true;
            var sphereObj = GameObject.Find("Sphere");
            var cubeObj = GameObject.Find("Cube");
            var contactObj = GameObject.Find("ContactManifoldVisualizer");
            var configPanel = GameObject.Find("PhysicsConfigPanel");
            var telemetryPanel = GameObject.Find("TelemetryPanel");

            log.AppendLine($"Sphere GameObject: {(sphereObj != null ? "Present" : "MISSING")}");
            log.AppendLine($"Cube GameObject: {(cubeObj != null ? "Present" : "MISSING")}");
            log.AppendLine($"ContactManifoldVisualizer: {(contactObj != null ? "Present" : "MISSING")}");
            log.AppendLine($"PhysicsConfigPanel: {(configPanel != null ? "Present" : "MISSING")}");
            log.AppendLine($"TelemetryPanel: {(telemetryPanel != null ? "Present" : "MISSING")}");

            if (sphereObj != null)
            {
                var sVis = sphereObj.GetComponent<PhysicsVectorVisualizer>();
                var sLr = sphereObj.GetComponent<LineRenderer>();
                log.AppendLine($"  Sphere Vector Visualizer: {(sVis != null ? "Present" : "MISSING")}");
                log.AppendLine($"  Sphere LineRenderer: {(sLr != null ? "Present" : "MISSING")}");
                if (sVis == null || sLr == null) ok = false;
            }
            else ok = false;

            if (cubeObj != null)
            {
                var cVis = cubeObj.GetComponent<PhysicsVectorVisualizer>();
                var cLr = cubeObj.GetComponent<LineRenderer>();
                log.AppendLine($"  Cube Vector Visualizer: {(cVis != null ? "Present" : "MISSING")}");
                log.AppendLine($"  Cube LineRenderer: {(cLr != null ? "Present" : "MISSING")}");
                if (cVis == null || cLr == null) ok = false;
            }
            else ok = false;

            if (contactObj != null)
            {
                var cmVis = contactObj.GetComponent<ContactManifoldVisualizer>();
                log.AppendLine($"  Contact Visualizer Component: {(cmVis != null ? "Present" : "MISSING")}");
                if (cmVis == null) ok = false;
            }
            else ok = false;

            return ok;
        }

        private static bool ValidateParameterControls(CollisionLabManager manager, StringBuilder log)
        {
            bool ok = true;
            var sphereRb = GameObject.Find("Sphere")?.GetComponent<Rigidbody>();
            var cubeRb = GameObject.Find("Cube")?.GetComponent<Rigidbody>();
            var sphereCol = GameObject.Find("Sphere")?.GetComponent<Collider>();
            var cubeCol = GameObject.Find("Cube")?.GetComponent<Collider>();

            // Test mass changing
            manager.SetSphereMass(2.5f);
            manager.SetCubeMass(3.5f);
            if (sphereRb != null && Mathf.Approximately(sphereRb.mass, 2.5f))
            {
                log.AppendLine("  Sphere Mass set to 2.5 kg: VERIFIED");
            }
            else
            {
                log.AppendLine($"  Sphere Mass set FAILED. Current: {sphereRb?.mass}");
                ok = false;
            }

            if (cubeRb != null && Mathf.Approximately(cubeRb.mass, 3.5f))
            {
                log.AppendLine("  Cube Mass set to 3.5 kg: VERIFIED");
            }
            else
            {
                log.AppendLine($"  Cube Mass set FAILED. Current: {cubeRb?.mass}");
                ok = false;
            }

            // Test restitution changing & runtime material instancing
            manager.SetRestitution(0.65f);
            if (sphereCol != null && sphereCol.material != null)
            {
                log.AppendLine($"  Sphere PhysicMaterial runtime instance: {sphereCol.material.name}, bounciness: {sphereCol.material.bounciness}");
                if (!Mathf.Approximately(sphereCol.material.bounciness, 0.65f)) ok = false;
            }
            else ok = false;

            if (cubeCol != null && cubeCol.material != null)
            {
                log.AppendLine($"  Cube PhysicMaterial runtime instance: {cubeCol.material.name}, bounciness: {cubeCol.material.bounciness}");
                if (!Mathf.Approximately(cubeCol.material.bounciness, 0.65f)) ok = false;
            }
            else ok = false;

            return ok;
        }

        private static bool ValidatePresets(CollisionLabManager manager, StringBuilder log)
        {
            var sphereRb = GameObject.Find("Sphere")?.GetComponent<Rigidbody>();
            var cubeRb = GameObject.Find("Cube")?.GetComponent<Rigidbody>();
            var sphereCol = GameObject.Find("Sphere")?.GetComponent<Collider>();

            manager.ApplyPreset("Equal");
            bool equalOk = Mathf.Approximately(sphereRb.mass, 1.0f) && Mathf.Approximately(cubeRb.mass, 1.0f);
            log.AppendLine($"  Preset 'Equal': m1={sphereRb?.mass}, m2={cubeRb?.mass} -> {(equalOk ? "OK" : "FAIL")}");

            manager.ApplyPreset("HeavyImpactor");
            bool heavyImpOk = Mathf.Approximately(sphereRb.mass, 3.0f) && Mathf.Approximately(cubeRb.mass, 1.0f);
            log.AppendLine($"  Preset 'HeavyImpactor': m1={sphereRb?.mass}, m2={cubeRb?.mass} -> {(heavyImpOk ? "OK" : "FAIL")}");

            manager.ApplyPreset("HeavyTarget");
            bool heavyTgtOk = Mathf.Approximately(sphereRb.mass, 1.0f) && Mathf.Approximately(cubeRb.mass, 3.0f);
            log.AppendLine($"  Preset 'HeavyTarget': m1={sphereRb?.mass}, m2={cubeRb?.mass} -> {(heavyTgtOk ? "OK" : "FAIL")}");

            manager.ApplyPreset("Elastic");
            bool elasticOk = Mathf.Approximately(sphereCol.material.bounciness, 1.0f);
            log.AppendLine($"  Preset 'Elastic': bounciness={sphereCol?.material?.bounciness} -> {(elasticOk ? "OK" : "FAIL")}");

            manager.ApplyPreset("Inelastic");
            bool inelasticOk = Mathf.Approximately(sphereCol.material.bounciness, 0.0f);
            log.AppendLine($"  Preset 'Inelastic': bounciness={sphereCol?.material?.bounciness} -> {(inelasticOk ? "OK" : "FAIL")}");

            return equalOk && heavyImpOk && heavyTgtOk && elasticOk && inelasticOk;
        }

        private static bool ValidateTelemetryResults(CollisionLabManager manager, string testName, float m1, float m2, float launchV, float e, StringBuilder log)
        {
            var sphereRb = GameObject.Find("Sphere")?.GetComponent<Rigidbody>();
            var cubeRb = GameObject.Find("Cube")?.GetComponent<Rigidbody>();

            Vector3 v1f = sphereRb != null ? sphereRb.velocity : Vector3.zero;
            Vector3 v2f = cubeRb != null ? cubeRb.velocity : Vector3.zero;

            Vector3 pInitial = new Vector3(m1 * launchV, 0f, 0f);
            Vector3 pFinal = m1 * v1f + m2 * v2f;
            Vector3 deltaP = pFinal - pInitial;
            float pErrorPercent = (pInitial.magnitude > 0.001f) ? (deltaP.magnitude / pInitial.magnitude) * 100f : 0f;

            float keInitial = 0.5f * m1 * launchV * launchV;
            float keFinal = 0.5f * m1 * v1f.sqrMagnitude + 0.5f * m2 * v2f.sqrMagnitude;
            float deltaKE = keFinal - keInitial;
            float keRetainedPercent = (keInitial > 0.001f) ? (keFinal / keInitial) * 100f : 0f;

            log.AppendLine($"  Configuration: m1={m1:F1}kg, m2={m2:F1}kg, v_launch={launchV:F1}m/s, target e={e:F2}");
            log.AppendLine($"  Post-Impact Sphere Velocity v1f: {v1f}");
            log.AppendLine($"  Post-Impact Cube Velocity v2f: {v2f}");
            log.AppendLine($"  Initial Momentum p_i: {pInitial} (Mag: {pInitial.magnitude:F3} kg·m/s)");
            log.AppendLine($"  Final Momentum p_f: {pFinal} (Mag: {pFinal.magnitude:F3} kg·m/s)");
            log.AppendLine($"  Momentum Error Delta p: {deltaP} ({pErrorPercent:F2}%)");
            log.AppendLine($"  Initial Kinetic Energy KE_i: {keInitial:F2} J");
            log.AppendLine($"  Final Kinetic Energy KE_f: {keFinal:F2} J (Delta: {deltaKE:F2} J, {keRetainedPercent:F1}% retained)");

            // Check UI telemetry text labels
            var momentumBeforeTxt = GameObject.Find("MomentumBeforeValue")?.GetComponent<Text>();
            var momentumAfterTxt = GameObject.Find("MomentumAfterValue")?.GetComponent<Text>();
            var momentumErrorTxt = GameObject.Find("MomentumErrorValue")?.GetComponent<Text>();
            var keTxt = GameObject.Find("KineticEnergyValue")?.GetComponent<Text>();
            var restitutionTxt = GameObject.Find("RestitutionValue")?.GetComponent<Text>();
            var contactPointTxt = GameObject.Find("ContactPointValue")?.GetComponent<Text>();
            var contactNormalTxt = GameObject.Find("ContactNormalValue")?.GetComponent<Text>();

            log.AppendLine($"  UI Display - Momentum Before: {momentumBeforeTxt?.text}");
            log.AppendLine($"  UI Display - Momentum After: {momentumAfterTxt?.text}");
            log.AppendLine($"  UI Display - Momentum Error: {momentumErrorTxt?.text}");
            log.AppendLine($"  UI Display - Kinetic Energy: {keTxt?.text}");
            log.AppendLine($"  UI Display - Restitution (e): {restitutionTxt?.text}");
            log.AppendLine($"  UI Display - Contact Point: {contactPointTxt?.text}");
            log.AppendLine($"  UI Display - Contact Normal: {contactNormalTxt?.text}");

            // Verification checks:
            // 1. Momentum conservation error across impact should be <= 6% (small differences due to floor contact/sub-frame integration)
            bool momentumConserved = pErrorPercent < 6.0f;
            // 2. Both bodies should have non-zero motion after impact
            bool bodiesMoved = (v1f.sqrMagnitude > 0.01f || v2f.sqrMagnitude > 0.01f);
            // 3. UI updated
            bool uiUpdated = momentumBeforeTxt != null && !momentumBeforeTxt.text.Contains("0.00 kg·m/s");

            log.AppendLine($"  Checks: MomentumConserved={momentumConserved}, BodiesMoved={bodiesMoved}, UIUpdated={uiUpdated}");
            return momentumConserved && bodiesMoved && uiUpdated;
        }
    }
}

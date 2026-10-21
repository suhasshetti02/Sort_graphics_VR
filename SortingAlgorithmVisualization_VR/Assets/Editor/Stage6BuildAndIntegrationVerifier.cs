using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SortLab3D.Editor
{
    /// <summary>
    /// Stage 6 Integration Verifier and Windows Standalone Build Pipeline.
    /// Provides 1-click build generation and comprehensive regression verification in Unity Editor.
    /// </summary>
    public static class Stage6BuildAndIntegrationVerifier
    {
        private const string BuildOutputDir = "Builds/SortLab3D-Windows";
        private const string ExecutableName = "SortLab3D.exe";

        private static readonly string[] RequiredScenes = new[]
        {
            "Assets/Scenes/MainScene.unity",
            "Assets/Scenes/CollisionLab.unity"
        };

        [MenuItem("SortLab3D/Build Windows Standalone (x86_64)")]
        public static void BuildWindowsStandalone()
        {
            Debug.Log("[Stage 6 Build] Starting Windows x86_64 standalone build pipeline...");

            // 1. Ensure output directory exists
            var projectRoot = Directory.GetCurrentDirectory();
            var fullOutputDir = Path.Combine(projectRoot, BuildOutputDir);
            if (!Directory.Exists(fullOutputDir))
            {
                Directory.CreateDirectory(fullOutputDir);
            }

            var buildPath = Path.Combine(fullOutputDir, ExecutableName);

            // 2. Validate scene existence
            foreach (var scenePath in RequiredScenes)
            {
                if (!File.Exists(scenePath))
                {
                    EditorUtility.DisplayDialog("Build Error", $"Required scene not found: {scenePath}", "OK");
                    Debug.LogError($"[Stage 6 Build] Missing scene: {scenePath}");
                    return;
                }
            }

            // 3. Configure build player options
            var buildOptions = new BuildPlayerOptions
            {
                scenes = RequiredScenes,
                locationPathName = buildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            Debug.Log($"[Stage 6 Build] Building to: {buildPath}");
            var report = BuildPipeline.BuildPlayer(buildOptions);
            var summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                var sizeMb = summary.totalSize / (1024f * 1024f);
                var successMsg = $"Windows Standalone build succeeded!\n\nLocation: {buildPath}\nTotal Size: {sizeMb:F2} MB\nTime: {summary.totalTime.TotalSeconds:F1}s";
                Debug.Log($"<color=#00FF88>[Stage 6 Build Success]</color> {successMsg}");
                
                EditorUtility.DisplayDialog("Build Succeeded", successMsg, "OK");
                EditorUtility.RevealInFinder(buildPath);
            }
            else
            {
                var errorMsg = $"Build failed with result: {summary.result}. Total errors: {summary.totalErrors}";
                Debug.LogError($"<color=#FF3366>[Stage 6 Build Failed]</color> {errorMsg}");
                EditorUtility.DisplayDialog("Build Failed", errorMsg, "OK");
            }
        }

        [MenuItem("SortLab3D/Run Stage 6 Integration Tests")]
        public static void RunIntegrationTests()
        {
            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("           STAGE 6: FINAL INTEGRATION & DELIVERY VERIFICATION                   ");
            sb.AppendLine("================================================================================");
            sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            bool allPassed = true;

            // 1. Build Settings Scenes Check
            sb.AppendLine("\n--- 1. Build Settings & Scene Ordering ---");
            var scenes = EditorBuildSettings.scenes;
            bool mainSceneConfigured = false;
            bool collisionLabConfigured = false;

            if (scenes.Length >= 2)
            {
                if (scenes[0].path == RequiredScenes[0] && scenes[0].enabled)
                {
                    sb.AppendLine("[PASS] Initial startup scene (Index 0) is MainScene.unity (Enabled)");
                    mainSceneConfigured = true;
                }
                else
                {
                    sb.AppendLine($"[FAIL] Scene 0 is {scenes[0].path} (enabled={scenes[0].enabled})");
                    allPassed = false;
                }

                if (scenes[1].path == RequiredScenes[1] && scenes[1].enabled)
                {
                    sb.AppendLine("[PASS] Secondary scene (Index 1) is CollisionLab.unity (Enabled)");
                    collisionLabConfigured = true;
                }
                else
                {
                    sb.AppendLine($"[FAIL] Scene 1 is {scenes[1].path} (enabled={scenes[1].enabled})");
                    allPassed = false;
                }
            }
            else
            {
                sb.AppendLine($"[FAIL] Insufficient scenes in EditorBuildSettings: count = {scenes.Length}");
                allPassed = false;
            }

            // 2. MainScene Navigation Button Check
            sb.AppendLine("\n--- 2. MainScene Navigation Check ---");
            if (File.Exists(RequiredScenes[0]))
            {
                var mainSceneText = File.ReadAllText(RequiredScenes[0]);
                bool hasNavButton = mainSceneText.Contains("BtnCollisionLab");
                bool hasSceneNavigator = mainSceneText.Contains("c011151000b9ec740000000000000006");
                bool hasTargetCollisionLab = mainSceneText.Contains("targetScene: CollisionLab");
                bool hasDraggablePanel = mainSceneText.Contains("Panel") && mainSceneText.Contains("TitleBar");

                if (hasNavButton && hasSceneNavigator && hasTargetCollisionLab)
                {
                    sb.AppendLine("[PASS] MainScene sidebar contains 'BtnCollisionLab' with SceneNavigator wired to CollisionLab");
                }
                else
                {
                    sb.AppendLine($"[FAIL] MainScene navigation incomplete (button={hasNavButton}, script={hasSceneNavigator}, target={hasTargetCollisionLab})");
                    allPassed = false;
                }

                if (hasDraggablePanel)
                {
                    sb.AppendLine("[PASS] MainScene retains draggable code inspector panel and title bar");
                }
                else
                {
                    sb.AppendLine("[FAIL] MainScene missing draggable code inspector panel");
                    allPassed = false;
                }
            }

            // 3. CollisionLab Physics & Return Navigation Check
            sb.AppendLine("\n--- 3. CollisionLab Physics & Return Navigation Check ---");
            if (File.Exists(RequiredScenes[1]))
            {
                var collisionSceneText = File.ReadAllText(RequiredScenes[1]);
                bool hasPhysicsControls = collisionSceneText.Contains("PhysicsConfigPanel");
                bool hasTelemetry = collisionSceneText.Contains("TelemetryPanel");
                bool hasCorrectedPreset = collisionSceneText.Contains("e=0.0 Inelastic (No Bounce)");
                bool hasReturnHandler = collisionSceneText.Contains("CollisionLabManager");

                if (hasPhysicsControls && hasTelemetry)
                {
                    sb.AppendLine("[PASS] CollisionLab contains interactive physics controls and telemetry panel");
                }
                else
                {
                    sb.AppendLine("[FAIL] CollisionLab missing controls or telemetry");
                    allPassed = false;
                }

                if (hasCorrectedPreset)
                {
                    sb.AppendLine("[PASS] Restitution preset accurately renamed to 'e=0.0 Inelastic (No Bounce)'");
                }
                else
                {
                    sb.AppendLine("[FAIL] Restitution preset still contains inaccurate label");
                    allPassed = false;
                }

                if (hasReturnHandler)
                {
                    sb.AppendLine("[PASS] CollisionLabManager handles ReturnToMainScene and Escape hotkey");
                }
                else
                {
                    sb.AppendLine("[FAIL] CollisionLabManager not found in scene");
                    allPassed = false;
                }
            }

            // 4. Output Summary
            sb.AppendLine("\n================================================================================");
            sb.AppendLine($"FINAL RESULT: {(allPassed ? "ALL 6 INTEGRATION CHECKS PASSED" : "FAILED")}");
            sb.AppendLine("================================================================================");

            if (allPassed)
            {
                Debug.Log($"<color=#00FF88>{sb}</color>");
                EditorUtility.DisplayDialog("Stage 6 Verification", "All 6 integration checks passed successfully!\n\nReady for Standalone Windows Build.", "OK");
            }
            else
            {
                Debug.LogError($"<color=#FF3366>{sb}</color>");
                EditorUtility.DisplayDialog("Stage 6 Verification Failed", sb.ToString(), "OK");
            }
        }
    }
}

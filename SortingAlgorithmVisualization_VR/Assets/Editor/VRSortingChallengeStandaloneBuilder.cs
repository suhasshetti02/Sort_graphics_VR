using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class VRSortingChallengeStandaloneBuilder
{
    public const string BuildDirectory = "Builds/VRSortingChallenge-Windows";
    public const string ExecutableName = "VRSortingChallenge.exe";
    public static readonly string FullExecutablePath = Path.Combine(BuildDirectory, ExecutableName);

    public static readonly string[] StandaloneScenes = new string[]
    {
        "Assets/Scenes/VRSortingChallenge.unity", // Index 0: Starts immediately in tabletop game!
        "Assets/Scenes/MainScene.unity",          // Index 1: Back to main menu & return
        "Assets/Scenes/CollisionLab.unity",       // Index 2: Full navigation compatibility
        "Assets/Scenes/CollisionLab_VR.unity"     // Index 3: VR laboratory
    };

    [MenuItem("SortLab VR/Build Dedicated VR Sorting Challenge Player")]
    public static void BuildStandalonePlayer()
    {
        Debug.Log("===============================================================================");
        Debug.Log("STARTING DEDICATED VR SORTING CHALLENGE WINDOWS 64-BIT STANDALONE BUILD");
        Debug.Log("===============================================================================");

        // 1. Verify all scenes exist
        for (int i = 0; i < StandaloneScenes.Length; i++)
        {
            string scenePath = StandaloneScenes[i];
            if (!File.Exists(scenePath))
            {
                string errMsg = $"Required scene not found: {scenePath}";
                Debug.LogError($"[VRSortingChallengeStandaloneBuilder] {errMsg}");
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Build Error", errMsg, "OK");
                }
                EditorApplication.Exit(1);
                return;
            }
            Debug.Log($"[Scene Config] Index {i}: '{scenePath}'");
        }

        // 2. Ensure output directory
        string fullOutputDir = Path.GetFullPath(BuildDirectory);
        if (!Directory.Exists(fullOutputDir))
        {
            Directory.CreateDirectory(fullOutputDir);
            Debug.Log($"[VRSortingChallengeStandaloneBuilder] Created directory: {fullOutputDir}");
        }

        string fullExePath = Path.GetFullPath(FullExecutablePath);
        Debug.Log($"[VRSortingChallengeStandaloneBuilder] Destination executable: {fullExePath}");

        // 3. Configure BuildPlayerOptions
        var options = new BuildPlayerOptions
        {
            scenes = StandaloneScenes,
            locationPathName = fullExePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        // 4. Execute Build
        DateTime startTime = DateTime.Now;
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            float sizeMb = summary.totalSize / (1024f * 1024f);
            TimeSpan duration = DateTime.Now - startTime;
            Debug.Log("===============================================================================");
            Debug.Log($"[BUILD SUCCEEDED] Windows Standalone player built successfully!");
            Debug.Log($"Output Path: {fullExePath}");
            Debug.Log($"Total Size: {sizeMb:F2} MB");
            Debug.Log($"Build Time: {duration.TotalSeconds:F1}s");
            Debug.Log($"Initial Startup Scene (Index 0): {StandaloneScenes[0]}");
            Debug.Log("===============================================================================");

            if (!Application.isBatchMode)
            {
                EditorUtility.RevealInFinder(fullExePath);
            }
            else
            {
                EditorApplication.Exit(0);
            }
        }
        else
        {
            Debug.LogError("===============================================================================");
            Debug.LogError($"[BUILD FAILED] Result: {summary.result}, Errors: {summary.totalErrors}");
            Debug.LogError("===============================================================================");
            EditorApplication.Exit(1);
        }
    }
}

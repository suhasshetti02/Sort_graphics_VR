using UnityEditor;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.XR.Management;

public static class XRConfigHelper
{
    public static void InspectXRSettings()
    {
        var settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
        if (settings == null)
        {
            Debug.Log("[XRConfigHelper] Standalone XRGeneralSettings is null. Attempting creation...");
            XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
        }
        else
        {
            Debug.Log($"[XRConfigHelper] Standalone XRGeneralSettings found. InitOnStart: {settings.InitManagerOnStart}");
        }
    }
}

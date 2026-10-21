using System.Collections;
using UnityEngine;
using UnityEngine.XR.Management;

namespace CollisionLab.VR
{
    /// <summary>
    /// Guarded XR bootstrap for VR scenes.
    /// Ensures XR subsystems are initialized when entering CollisionLab_VR
    /// without requiring XR initialization at application startup, keeping desktop scenes pure.
    /// Safely handles running in Editor with XR Device Simulator when no physical headset is present.
    /// </summary>
    public class XRSceneBootstrap : MonoBehaviour
    {
        [Header("XR Lifecycle Configuration")]
        [SerializeField] private bool initializeOnStart = true;
        [SerializeField] private bool deinitializeOnDestroy = true;

        private void Start()
        {
            if (initializeOnStart)
            {
                StartCoroutine(InitializeXRCoroutine());
            }
        }

        private IEnumerator InitializeXRCoroutine()
        {
            var xrSettings = XRGeneralSettings.Instance;
            if (xrSettings == null || xrSettings.Manager == null)
            {
                Debug.Log("[XRSceneBootstrap] XRGeneralSettings not configured. Proceeding in standalone/simulated mode.");
                yield break;
            }

            var manager = xrSettings.Manager;
            if (!manager.isInitializationComplete)
            {
                Debug.Log("[XRSceneBootstrap] Initializing XR Loader...");
                yield return manager.InitializeLoader();
            }

            if (manager.activeLoader != null)
            {
                Debug.Log($"[XRSceneBootstrap] Active XR Loader: {manager.activeLoader.name}. Starting XR Subsystems...");
                manager.StartSubsystems();
            }
            else
            {
                Debug.Log("[XRSceneBootstrap] No physical XR loader active. Running with XR Device Simulator and mock input.");
            }
        }

        private void OnDestroy()
        {
            if (deinitializeOnDestroy)
            {
                var xrSettings = XRGeneralSettings.Instance;
                if (xrSettings != null && xrSettings.Manager != null && xrSettings.Manager.isInitializationComplete)
                {
                    Debug.Log("[XRSceneBootstrap] Stopping and deinitializing XR subsystems upon scene exit...");
                    xrSettings.Manager.StopSubsystems();
                    xrSettings.Manager.DeinitializeLoader();
                }
            }
        }
    }
}

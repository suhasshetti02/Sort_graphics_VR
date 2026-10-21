// Copyright (c) 2021 Dylan Cheng & SortLab 3D Contributors
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Lightweight component for seamless scene navigation between MainScene and CollisionLab.
    /// Handles UI button clicks and optional keyboard shortcuts without modifying GameManager.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class SceneNavigator : MonoBehaviour
    {
        [Header("Scene Navigation")]
        [Tooltip("The name of the target scene to load (must be included in Build Settings).")]
        [SerializeField] private string targetScene = "CollisionLab";

        [Header("Optional Hotkey")]
        [Tooltip("Optional keyboard shortcut to trigger navigation (e.g. F2). Set to None to disable.")]
        [SerializeField] private KeyCode navigationHotkey = KeyCode.F2;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_button != null)
            {
                _button.onClick.AddListener(OnNavigateClicked);
            }
        }

        private void Update()
        {
            if (navigationHotkey != KeyCode.None && Input.GetKeyDown(navigationHotkey))
            {
                OnNavigateClicked();
            }
        }

        public void OnNavigateClicked()
        {
            if (string.IsNullOrEmpty(targetScene))
            {
                Debug.LogWarning("[SceneNavigator] Target scene name is not specified.");
                return;
            }

            // Load the target scene cleanly
            SceneManager.LoadScene(targetScene);
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnNavigateClicked);
            }
        }
    }
}

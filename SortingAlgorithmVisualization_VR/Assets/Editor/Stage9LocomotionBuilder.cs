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

public static class Stage9LocomotionBuilder
{
    [MenuItem("VR/Build Stage 9 Locomotion & Help Panel")]
    public static void BuildLocomotionAndHelpPanel()
    {
        Debug.Log("[Stage9LocomotionBuilder] Starting Locomotion & Help Panel Build...");

        string scenePath = "Assets/Scenes/CollisionLab_VR.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError($"[Stage9LocomotionBuilder] Failed to open scene: {scenePath}");
            return;
        }

        // 1. Configure Floor TeleportationArea and Reticle
        ConfigureFloorTeleportation();

        // 2. Configure Snap Turn Provider settings
        ConfigureSnapTurn();

        // 3. Create or Refresh World-Space VR CONTROLS Help Panel
        BuildVRControlsHelpPanel();

        // 4. Save Scene
        bool saved = EditorSceneManager.SaveScene(scene);
        if (saved)
        {
            Debug.Log("[Stage9LocomotionBuilder] Successfully saved CollisionLab_VR.unity with locomotion and controls panel!");
        }
        else
        {
            Debug.LogError("[Stage9LocomotionBuilder] Failed to save scene!");
        }
    }

    private static void ConfigureFloorTeleportation()
    {
        var floor = GameObject.Find("Floor");
        if (floor == null)
        {
            Debug.LogError("[Stage9LocomotionBuilder] Floor object not found!");
            return;
        }

        var teleArea = floor.GetComponent<TeleportationArea>();
        if (teleArea == null)
        {
            teleArea = floor.AddComponent<TeleportationArea>();
        }

        // Dedicated Teleport interaction layer (bit 31: 2147483648)
        // Matches the Teleport Interactor on Left & Right controllers,
        // and keeps the UI/Grab rays from hovering or fighting for the floor!
        teleArea.interactionLayers = 1 << 31; // Teleport layer

        // Load XRI Directional Teleport Reticle
        string reticlePath = "Assets/Samples/XR Interaction Toolkit/2.6.3/Starter Assets/Prefabs/Teleport/Directional Teleport Reticle.prefab";
        var reticlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(reticlePath);
        if (reticlePrefab != null)
        {
            teleArea.customReticle = reticlePrefab;
            Debug.Log("[Stage9LocomotionBuilder] Assigned Directional Teleport Reticle to Floor TeleportationArea.");
        }
        else
        {
            Debug.LogWarning($"[Stage9LocomotionBuilder] Could not find reticle at {reticlePath}");
        }

        teleArea.teleportTrigger = BaseTeleportationInteractable.TeleportTrigger.OnSelectExited;
        teleArea.matchOrientation = MatchOrientation.TargetUpAndForward;

        Debug.Log("[Stage9LocomotionBuilder] Configured Floor TeleportationArea successfully.");
    }

    private static void ConfigureSnapTurn()
    {
        var snapTurn = GameObject.FindObjectOfType<ActionBasedSnapTurnProvider>();
        if (snapTurn != null)
        {
            snapTurn.turnAmount = 45f;
            snapTurn.debounceTime = 0.5f; // Prevents rapid repeated turns on held key/button
            snapTurn.enableTurnLeftRight = true;
            snapTurn.enableTurnAround = true;
            Debug.Log("[Stage9LocomotionBuilder] Configured ActionBasedSnapTurnProvider (45 deg, 0.5s debounce).");
        }
    }

    private static void BuildVRControlsHelpPanel()
    {
        var existingCanvas = GameObject.Find("VR_Controls_Canvas");
        if (existingCanvas != null)
        {
            UnityEngine.Object.DestroyImmediate(existingCanvas);
        }

        var canvasGO = new GameObject("VR_Controls_Canvas");
        canvasGO.transform.position = new Vector3(2.3f, 1.4f, -2.8f);
        canvasGO.transform.rotation = Quaternion.Euler(0f, -26f, 0f);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var rt = canvasGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(1000f, 750f);
        rt.localScale = new Vector3(0.0018f, 0.0018f, 0.0018f);

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        canvasGO.AddComponent<TrackedDeviceGraphicRaycaster>();

        var xrOrigin = GameObject.FindObjectOfType<XROrigin>();
        if (xrOrigin != null && xrOrigin.Camera != null)
        {
            canvas.worldCamera = xrOrigin.Camera;
        }

        Font defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // Main Background Panel
        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.sizeDelta = Vector2.zero;
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.06f, 0.08f, 0.12f, 0.95f); // Dark slate

        // Header
        var headerGO = new GameObject("Header");
        headerGO.transform.SetParent(bgGO.transform, false);
        var hRT = headerGO.AddComponent<RectTransform>();
        hRT.anchorMin = new Vector2(0f, 0.88f);
        hRT.anchorMax = new Vector2(1f, 1f);
        hRT.sizeDelta = Vector2.zero;

        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(headerGO.transform, false);
        var titleRT = titleGO.AddComponent<RectTransform>();
        titleRT.anchorMin = Vector2.zero;
        titleRT.anchorMax = Vector2.one;
        titleRT.sizeDelta = Vector2.zero;
        var titleText = titleGO.AddComponent<Text>();
        titleText.font = defaultFont;
        titleText.text = "VR CONTROLS & NAVIGATION GUIDE";
        titleText.fontSize = 28;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(0.22f, 0.74f, 0.97f); // Cyan accent

        // Subtitle
        var subGO = new GameObject("Subtitle");
        subGO.transform.SetParent(bgGO.transform, false);
        var subRT = subGO.AddComponent<RectTransform>();
        subRT.anchoredPosition = new Vector2(0f, 260f);
        subRT.sizeDelta = new Vector2(900f, 25f);
        var subText = subGO.AddComponent<Text>();
        subText.font = defaultFont;
        subText.text = "XR Device Simulator & Controller Quick Reference for CollisionLab";
        subText.fontSize = 15;
        subText.alignment = TextAnchor.MiddleCenter;
        subText.color = new Color(0.6f, 0.65f, 0.75f);

        // 4 Section Cards Grid
        float colLeftX = -230f;
        float colRightX = 230f;
        float rowTopY = 120f;
        float rowBottomY = -140f;
        Vector2 cardSize = new Vector2(440f, 230f);

        // Card 1: Head & Look Controls
        CreateGuideCard(bgGO.transform, "Card_HeadLook", "1. HEAD LOOK & ROOM WALK", new Color(0.95f, 0.65f, 0.15f), new Vector2(colLeftX, rowTopY), cardSize, defaultFont, new string[]
        {
            "Hold Right Mouse Button + Move",
            "Rotate simulated head / view angle",
            "W / A / S / D Keys",
            "Walk / translate head around laboratory",
            "Mouse Scroll Wheel",
            "Move head up / down in elevation"
        });

        // Card 2: Controller Pointers & Aiming
        CreateGuideCard(bgGO.transform, "Card_Controllers", "2. CONTROLLER POINTERS", new Color(0.95f, 0.65f, 0.15f), new Vector2(colRightX, rowTopY), cardSize, defaultFont, new string[]
        {
            "Spacebar (Toggle)",
            "Active control of Right Controller ray",
            "Left Shift (Toggle)",
            "Active control of Left Controller ray",
            "Move Mouse",
            "Aim pointer ray at objects or UI buttons"
        });

        // Card 3: Teleport Locomotion & Snap Turn
        CreateGuideCard(bgGO.transform, "Card_Locomotion", "3. TELEPORT & SNAP TURN", new Color(0.1f, 0.8f, 0.5f), new Vector2(colLeftX, rowBottomY), cardSize, defaultFont, new string[]
        {
            "Thumbstick Forward / MMB Drag",
            "Aim teleport ray at floor surface",
            "Directional Torus Reticle",
            "Visual indicator highlights landing target",
            "Release Stick / MMB",
            "Instantly teleport to target location",
            "Thumbstick Left / Right",
            "Discrete 45-degree snap turn"
        });

        // Card 4: Physics Grabbing & Menu Navigation
        CreateGuideCard(bgGO.transform, "Card_PhysicsMenu", "4. GRABBING & SIMULATION", new Color(0.22f, 0.74f, 0.97f), new Vector2(colRightX, rowBottomY), cardSize, defaultFont, new string[]
        {
            "Aim Ray at Object + Press G",
            "Grab & reposition Sphere, Cube or Calib Cube",
            "Release G Key",
            "Drop cleanly under gravity (no fling)",
            "Aim Ray at Floating Menu + LMB",
            "Click LAUNCH, RESET, Sliders & Presets",
            "Esc Key or 'MAIN MENU' Button",
            "Return to Desktop Sorting Visualizer"
        });

        Debug.Log("[Stage9LocomotionBuilder] Built VR Controls Help Panel successfully.");
    }

    private static void CreateGuideCard(Transform parent, string name, string title, Color accentColor, Vector2 pos, Vector2 size, Font font, string[] items)
    {
        var cardGO = new GameObject(name);
        cardGO.transform.SetParent(parent, false);
        var rt = cardGO.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var img = cardGO.AddComponent<Image>();
        img.color = new Color(0.03f, 0.04f, 0.07f, 0.85f); // Dark inset

        // Title
        var titleGO = new GameObject("CardTitle");
        titleGO.transform.SetParent(cardGO.transform, false);
        var tRT = titleGO.AddComponent<RectTransform>();
        tRT.anchoredPosition = new Vector2(0f, 95f);
        tRT.sizeDelta = new Vector2(size.x - 24f, 28f);
        var tText = titleGO.AddComponent<Text>();
        tText.font = font;
        tText.text = title;
        tText.fontSize = 15;
        tText.fontStyle = FontStyle.Bold;
        tText.alignment = TextAnchor.MiddleLeft;
        tText.color = accentColor;

        // Content items
        float startY = 65f;
        float stepY = 25f;

        for (int i = 0; i < items.Length; i += 2)
        {
            string key = items[i];
            string desc = (i + 1 < items.Length) ? items[i + 1] : "";

            var rowGO = new GameObject("Row_" + (i / 2));
            rowGO.transform.SetParent(cardGO.transform, false);
            var rRT = rowGO.AddComponent<RectTransform>();
            rRT.anchoredPosition = new Vector2(0f, startY - (i / 2) * stepY * 1.7f);
            rRT.sizeDelta = new Vector2(size.x - 24f, 40f);

            // Key badge
            var keyGO = new GameObject("Key");
            keyGO.transform.SetParent(rowGO.transform, false);
            var kRT = keyGO.AddComponent<RectTransform>();
            kRT.anchorMin = new Vector2(0f, 0.5f);
            kRT.anchorMax = new Vector2(1f, 1f);
            kRT.sizeDelta = Vector2.zero;
            var kText = keyGO.AddComponent<Text>();
            kText.font = font;
            kText.text = "• " + key;
            kText.fontSize = 13;
            kText.fontStyle = FontStyle.Bold;
            kText.alignment = TextAnchor.MiddleLeft;
            kText.color = Color.white;

            // Desc
            var descGO = new GameObject("Desc");
            descGO.transform.SetParent(rowGO.transform, false);
            var dRT = descGO.AddComponent<RectTransform>();
            dRT.anchorMin = new Vector2(0f, 0f);
            dRT.anchorMax = new Vector2(1f, 0.5f);
            dRT.sizeDelta = Vector2.zero;
            var dText = descGO.AddComponent<Text>();
            dText.font = font;
            dText.text = "    " + desc;
            dText.fontSize = 12;
            dText.alignment = TextAnchor.MiddleLeft;
            dText.color = new Color(0.7f, 0.75f, 0.85f);
        }
    }
}

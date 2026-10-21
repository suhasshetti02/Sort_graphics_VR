using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Unity.XR.CoreUtils;
using CollisionLab;
using CollisionLab.VR;

public static class VRSceneBuilder
{
    [MenuItem("VR/Build CollisionLab_VR Scene")]
    public static void BuildVRScene()
    {
        Debug.Log("[VRSceneBuilder] Starting VR Scene Construction...");

        // 1. Open CollisionLab.unity as base
        string sourceScenePath = "Assets/Scenes/CollisionLab.unity";
        string targetScenePath = "Assets/Scenes/CollisionLab_VR.unity";

        var scene = EditorSceneManager.OpenScene(sourceScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError($"[VRSceneBuilder] Failed to open base scene: {sourceScenePath}");
            return;
        }

        // 2. Remove desktop Main Camera and desktop Canvas & EventSystem
        var desktopCam = GameObject.Find("Main Camera");
        if (desktopCam != null)
        {
            Debug.Log("[VRSceneBuilder] Removing desktop camera...");
            UnityEngine.Object.DestroyImmediate(desktopCam);
        }

        var desktopCanvas = GameObject.Find("Canvas");
        if (desktopCanvas != null)
        {
            Debug.Log("[VRSceneBuilder] Removing desktop canvas...");
            UnityEngine.Object.DestroyImmediate(desktopCanvas);
        }

        var desktopEventSystem = GameObject.Find("EventSystem");
        if (desktopEventSystem != null)
        {
            Debug.Log("[VRSceneBuilder] Removing desktop EventSystem...");
            UnityEngine.Object.DestroyImmediate(desktopEventSystem);
        }

        // 3. Configure Floor with TeleportationArea
        var floor = GameObject.Find("Floor");
        if (floor != null)
        {
            if (floor.GetComponent<TeleportationArea>() == null)
            {
                Debug.Log("[VRSceneBuilder] Adding TeleportationArea to Floor...");
                floor.AddComponent<TeleportationArea>();
            }
        }

        // 4. Instantiate XR Interaction Setup prefab
        string xrSetupPrefabPath = "Assets/Samples/XR Interaction Toolkit/2.6.3/Starter Assets/Prefabs/XR Interaction Setup.prefab";
        var xrSetupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(xrSetupPrefabPath);
        if (xrSetupPrefab == null)
        {
            Debug.LogError($"[VRSceneBuilder] Failed to load XR Interaction Setup prefab at {xrSetupPrefabPath}");
            return;
        }

        var xrSetupInstance = (GameObject)PrefabUtility.InstantiatePrefab(xrSetupPrefab);
        xrSetupInstance.name = "XR Interaction Setup";
        xrSetupInstance.transform.position = new Vector3(0f, 0f, -4.5f);
        xrSetupInstance.transform.rotation = Quaternion.identity;
        Debug.Log("[VRSceneBuilder] Instantiated XR Interaction Setup at (0, 0, -4.5)");

        // 5. Instantiate XR Device Simulator prefab
        string simPrefabPath = "Assets/Samples/XR Interaction Toolkit/2.6.3/XR Device Simulator/XR Device Simulator.prefab";
        var simPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(simPrefabPath);
        if (simPrefab != null)
        {
            var simInstance = (GameObject)PrefabUtility.InstantiatePrefab(simPrefab);
            simInstance.name = "XR Device Simulator";
            Debug.Log("[VRSceneBuilder] Instantiated XR Device Simulator.");
        }
        else
        {
            Debug.LogWarning($"[VRSceneBuilder] XR Device Simulator prefab not found at {simPrefabPath}");
        }

        // 6. Create Grabbable Test Pedestal and Cube
        CreateGrabbableTestObject();

        // 7. Create World-Space Control Canvas & UI
        var vrCanvasGO = CreateWorldSpaceCanvas(out var uiRefs);

        // 8. Add XRSceneBootstrap
        var bootstrapGO = new GameObject("XRSceneBootstrap");
        bootstrapGO.AddComponent<XRSceneBootstrap>();

        // 9. Configure CollisionLabManager
        WireCollisionLabManager(uiRefs);

        // 10. Save as CollisionLab_VR.unity
        bool saved = EditorSceneManager.SaveScene(scene, targetScenePath);
        if (saved)
        {
            Debug.Log($"[VRSceneBuilder] Successfully saved VR scene to {targetScenePath}");
        }
        else
        {
            Debug.LogError($"[VRSceneBuilder] Failed to save VR scene to {targetScenePath}");
            return;
        }

        // 11. Update EditorBuildSettings
        UpdateBuildSettings(targetScenePath);

        Debug.Log("[VRSceneBuilder] Build completed successfully!");
    }

    private static void CreateGrabbableTestObject()
    {
        // Pedestal
        var pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pedestal.name = "CalibrationPedestal";
        pedestal.transform.position = new Vector3(1.6f, 0.4f, -3.2f);
        pedestal.transform.localScale = new Vector3(0.5f, 0.4f, 0.5f);
        var pedRenderer = pedestal.GetComponent<Renderer>();
        if (pedRenderer != null)
        {
            pedRenderer.material.color = new Color(0.18f, 0.22f, 0.28f); // Dark Slate
        }

        // Grabbable Cube
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "VRCalibrationCube";
        cube.transform.position = new Vector3(1.6f, 0.95f, -3.2f);
        cube.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);

        var rb = cube.GetComponent<Rigidbody>();
        if (rb == null) rb = cube.AddComponent<Rigidbody>();
        rb.mass = 0.5f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        var grab = cube.AddComponent<XRGrabInteractable>();
        grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
        grab.throwOnDetach = true;

        var cubeRenderer = cube.GetComponent<Renderer>();
        if (cubeRenderer != null)
        {
            cubeRenderer.material.color = new Color(0.95f, 0.7f, 0.1f); // Vibrant Gold
        }

        Debug.Log("[VRSceneBuilder] Created Grabbable Calibration Cube on Pedestal.");
    }

    public class VRUIRefs
    {
        public Button launchBtn;
        public Button resetBtn;
        public Button backBtn;

        public Slider speedSlider;
        public Text speedText;
        public Button speedDown;
        public Button speedUp;

        public Slider sphereMassSlider;
        public Text sphereMassText;
        public Button sphereMassDown;
        public Button sphereMassUp;

        public Slider cubeMassSlider;
        public Text cubeMassText;
        public Button cubeMassDown;
        public Button cubeMassUp;

        public Slider restitutionSlider;
        public Text restitutionText;
        public Button restitutionDown;
        public Button restitutionUp;

        public Button presetEqual;
        public Button presetHeavyImpactor;
        public Button presetHeavyTarget;
        public Button presetElastic;
        public Button presetInelastic;

        public Text stateText;
        public Text impactCountText;
        public Text relVelText;
        public Text impulseText;
        public Text contactPointText;
        public Text contactNormalText;
        public Text momentumBeforeText;
        public Text momentumAfterText;
        public Text momentumErrorText;
        public Text kineticEnergyText;
        public Text measuredRestitutionText;
    }

    private static GameObject CreateWorldSpaceCanvas(out VRUIRefs refs)
    {
        refs = new VRUIRefs();

        var canvasGO = new GameObject("VR_WorldSpace_Canvas");
        canvasGO.transform.position = new Vector3(-2.1f, 1.35f, -2.6f);
        canvasGO.transform.rotation = Quaternion.Euler(0f, 26f, 0f);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var rt = canvasGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(1050f, 750f);
        rt.localScale = new Vector3(0.0018f, 0.0018f, 0.0018f);

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        canvasGO.AddComponent<TrackedDeviceGraphicRaycaster>();

        Font defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // Background Panel
        var bgGO = new GameObject("BackgroundPanel");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.sizeDelta = Vector2.zero;
        var bgImage = bgGO.AddComponent<Image>();
        bgImage.color = new Color(0.06f, 0.08f, 0.12f, 0.95f); // Sleek modern dark blue-gray

        // Header Panel
        var headerGO = new GameObject("Header");
        headerGO.transform.SetParent(bgGO.transform, false);
        var headerRT = headerGO.AddComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0f, 0.88f);
        headerRT.anchorMax = new Vector2(1f, 1f);
        headerRT.sizeDelta = Vector2.zero;

        var titleGO = new GameObject("TitleText");
        titleGO.transform.SetParent(headerGO.transform, false);
        var titleRT = titleGO.AddComponent<RectTransform>();
        titleRT.anchorMin = Vector2.zero;
        titleRT.anchorMax = Vector2.one;
        titleRT.sizeDelta = Vector2.zero;
        var titleText = titleGO.AddComponent<Text>();
        titleText.font = defaultFont;
        titleText.text = "VR COLLISION PHYSICS LABORATORY";
        titleText.fontSize = 28;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(0.22f, 0.74f, 0.97f); // Vibrant Cyan

        // Main Actions Row (Launch, Reset, Desktop)
        var actionsGO = new GameObject("ActionsRow");
        actionsGO.transform.SetParent(bgGO.transform, false);
        var actionsRT = actionsGO.AddComponent<RectTransform>();
        actionsRT.anchorMin = new Vector2(0.05f, 0.77f);
        actionsRT.anchorMax = new Vector2(0.95f, 0.87f);
        actionsRT.sizeDelta = Vector2.zero;

        refs.launchBtn = CreateButton(actionsGO.transform, "LaunchBtn", "LAUNCH SIMULATION", new Vector2(-310f, 0f), new Vector2(280f, 50f), new Color(0.06f, 0.72f, 0.5f), defaultFont, 20);
        refs.resetBtn = CreateButton(actionsGO.transform, "ResetBtn", "RESET SIMULATION", new Vector2(0f, 0f), new Vector2(280f, 50f), new Color(0.93f, 0.27f, 0.27f), defaultFont, 20);
        refs.backBtn = CreateButton(actionsGO.transform, "BackBtn", "MAIN MENU", new Vector2(310f, 0f), new Vector2(280f, 50f), new Color(0.39f, 0.4f, 0.95f), defaultFont, 20);

        // Left Column: Parameters & Presets
        var leftColGO = new GameObject("ParametersColumn");
        leftColGO.transform.SetParent(bgGO.transform, false);
        var leftRT = leftColGO.AddComponent<RectTransform>();
        leftRT.anchorMin = new Vector2(0.04f, 0.04f);
        leftRT.anchorMax = new Vector2(0.51f, 0.75f);
        leftRT.sizeDelta = Vector2.zero;

        // Sliders
        float yPos = 210f;
        float ySpacing = 72f;

        CreateSliderRow(leftColGO.transform, "Speed", "Launch Speed", yPos, defaultFont, out refs.speedSlider, out refs.speedText, out refs.speedDown, out refs.speedUp);
        yPos -= ySpacing;
        CreateSliderRow(leftColGO.transform, "SphereMass", "Sphere Mass", yPos, defaultFont, out refs.sphereMassSlider, out refs.sphereMassText, out refs.sphereMassDown, out refs.sphereMassUp);
        yPos -= ySpacing;
        CreateSliderRow(leftColGO.transform, "CubeMass", "Cube Mass", yPos, defaultFont, out refs.cubeMassSlider, out refs.cubeMassText, out refs.cubeMassDown, out refs.cubeMassUp);
        yPos -= ySpacing;
        CreateSliderRow(leftColGO.transform, "Restitution", "Restitution (e)", yPos, defaultFont, out refs.restitutionSlider, out refs.restitutionText, out refs.restitutionDown, out refs.restitutionUp);

        // Presets Label & Row
        var presetLabel = CreateText(leftColGO.transform, "PresetLabel", "Physics Collision Presets:", new Vector2(0f, -85f), new Vector2(460f, 25f), defaultFont, 16, FontStyle.Bold, new Color(0.9f, 0.9f, 0.95f), TextAnchor.MiddleLeft);

        var presetsRowGO = new GameObject("PresetsRow");
        presetsRowGO.transform.SetParent(leftColGO.transform, false);
        var prRT = presetsRowGO.AddComponent<RectTransform>();
        prRT.anchoredPosition = new Vector2(0f, -130f);
        prRT.sizeDelta = new Vector2(460f, 40f);

        refs.presetEqual = CreateButton(presetsRowGO.transform, "PresetEqual", "Equal (1:1)", new Vector2(-180f, 0f), new Vector2(85f, 36f), new Color(0.2f, 0.25f, 0.35f), defaultFont, 12);
        refs.presetHeavyImpactor = CreateButton(presetsRowGO.transform, "PresetHeavyImpactor", "Heavy (3:1)", new Vector2(-90f, 0f), new Vector2(85f, 36f), new Color(0.2f, 0.25f, 0.35f), defaultFont, 12);
        refs.presetHeavyTarget = CreateButton(presetsRowGO.transform, "PresetHeavyTarget", "Target (1:3)", new Vector2(0f, 0f), new Vector2(85f, 36f), new Color(0.2f, 0.25f, 0.35f), defaultFont, 12);
        refs.presetElastic = CreateButton(presetsRowGO.transform, "PresetElastic", "Elastic (e=1)", new Vector2(90f, 0f), new Vector2(85f, 36f), new Color(0.2f, 0.25f, 0.35f), defaultFont, 12);
        refs.presetInelastic = CreateButton(presetsRowGO.transform, "PresetInelastic", "Inelastic (e=0)", new Vector2(180f, 0f), new Vector2(85f, 36f), new Color(0.2f, 0.25f, 0.35f), defaultFont, 12);

        // Right Column: Telemetry & Conservation
        var rightColGO = new GameObject("TelemetryColumn");
        rightColGO.transform.SetParent(bgGO.transform, false);
        var rightRT = rightColGO.AddComponent<RectTransform>();
        rightRT.anchorMin = new Vector2(0.53f, 0.04f);
        rightRT.anchorMax = new Vector2(0.96f, 0.75f);
        rightRT.sizeDelta = Vector2.zero;

        var telemBg = rightColGO.AddComponent<Image>();
        telemBg.color = new Color(0.03f, 0.04f, 0.07f, 0.85f);

        CreateText(rightColGO.transform, "TelemTitle", "REAL-TIME TELEMETRY & CONSERVATION", new Vector2(0f, 220f), new Vector2(420f, 30f), defaultFont, 18, FontStyle.Bold, new Color(0.38f, 0.85f, 0.98f), TextAnchor.MiddleCenter);

        float tY = 180f;
        float tStep = 36f;

        refs.stateText = CreateTelemetryLine(rightColGO.transform, "StateLine", "Simulation State:", "Ready", tY, defaultFont);
        tY -= tStep;
        refs.impactCountText = CreateTelemetryLine(rightColGO.transform, "ImpactCountLine", "Impact Count:", "0", tY, defaultFont);
        tY -= tStep;
        refs.relVelText = CreateTelemetryLine(rightColGO.transform, "RelVelLine", "Relative Velocity:", "8.00 m/s", tY, defaultFont);
        tY -= tStep;
        refs.impulseText = CreateTelemetryLine(rightColGO.transform, "ImpulseLine", "Calculated Impulse:", "0.00 Ns", tY, defaultFont);
        tY -= tStep;
        refs.contactPointText = CreateTelemetryLine(rightColGO.transform, "ContactPointLine", "Contact Point:", "(0.00, 0.00, 0.00)", tY, defaultFont);
        tY -= tStep;
        refs.contactNormalText = CreateTelemetryLine(rightColGO.transform, "ContactNormalLine", "Contact Normal:", "(0.00, 0.00, 0.00)", tY, defaultFont);
        tY -= tStep;
        refs.momentumBeforeText = CreateTelemetryLine(rightColGO.transform, "MomBeforeLine", "Momentum Initial (P1+P2):", "8.00 kg*m/s", tY, defaultFont);
        tY -= tStep;
        refs.momentumAfterText = CreateTelemetryLine(rightColGO.transform, "MomAfterLine", "Momentum Final (P1'+P2'):", "8.00 kg*m/s", tY, defaultFont);
        tY -= tStep;
        refs.momentumErrorText = CreateTelemetryLine(rightColGO.transform, "MomErrorLine", "Conservation Error (ΔP):", "0.00%", tY, defaultFont);
        tY -= tStep;
        refs.kineticEnergyText = CreateTelemetryLine(rightColGO.transform, "KELossLine", "Kinetic Energy Loss (ΔKE):", "0.00 J", tY, defaultFont);
        tY -= tStep;
        refs.measuredRestitutionText = CreateTelemetryLine(rightColGO.transform, "MeasRestLine", "Measured Restitution (e):", "--", tY, defaultFont);

        return canvasGO;
    }

    private static Text CreateTelemetryLine(Transform parent, string name, string label, string defaultValue, float yPos, Font font)
    {
        var lineGO = new GameObject(name);
        lineGO.transform.SetParent(parent, false);
        var rt = lineGO.AddComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0f, yPos);
        rt.sizeDelta = new Vector2(410f, 30f);

        // Label
        var lblGO = new GameObject("Label");
        lblGO.transform.SetParent(lineGO.transform, false);
        var lblRT = lblGO.AddComponent<RectTransform>();
        lblRT.anchorMin = new Vector2(0f, 0f);
        lblRT.anchorMax = new Vector2(0.55f, 1f);
        lblRT.sizeDelta = Vector2.zero;
        var lblText = lblGO.AddComponent<Text>();
        lblText.font = font;
        lblText.text = label;
        lblText.fontSize = 13;
        lblText.alignment = TextAnchor.MiddleLeft;
        lblText.color = new Color(0.7f, 0.75f, 0.85f);

        // Value
        var valGO = new GameObject("Value");
        valGO.transform.SetParent(lineGO.transform, false);
        var valRT = valGO.AddComponent<RectTransform>();
        valRT.anchorMin = new Vector2(0.55f, 0f);
        valRT.anchorMax = new Vector2(1f, 1f);
        valRT.sizeDelta = Vector2.zero;
        var valText = valGO.AddComponent<Text>();
        valText.font = font;
        valText.text = defaultValue;
        valText.fontSize = 13;
        valText.fontStyle = FontStyle.Bold;
        valText.alignment = TextAnchor.MiddleRight;
        valText.color = new Color(0.38f, 0.85f, 0.98f); // Cyan accent

        return valText;
    }

    private static void CreateSliderRow(Transform parent, string id, string label, float yPos, Font font, out Slider slider, out Text valueText, out Button downBtn, out Button upBtn)
    {
        var rowGO = new GameObject(id + "Row");
        rowGO.transform.SetParent(parent, false);
        var rt = rowGO.AddComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0f, yPos);
        rt.sizeDelta = new Vector2(460f, 55f);

        // Label
        var lbl = CreateText(rowGO.transform, "Label", label, new Vector2(-120f, 15f), new Vector2(200f, 25f), font, 15, FontStyle.Bold, new Color(0.85f, 0.9f, 0.95f), TextAnchor.MiddleLeft);

        // Value Text
        valueText = CreateText(rowGO.transform, "ValText", "--", new Vector2(160f, 15f), new Vector2(120f, 25f), font, 15, FontStyle.Bold, new Color(0.38f, 0.85f, 0.98f), TextAnchor.MiddleRight);

        // Down Button [-]
        downBtn = CreateButton(rowGO.transform, "DownBtn", "−", new Vector2(-190f, -15f), new Vector2(40f, 32f), new Color(0.2f, 0.25f, 0.35f), font, 18);

        // Up Button [+]
        upBtn = CreateButton(rowGO.transform, "UpBtn", "+", new Vector2(190f, -15f), new Vector2(40f, 32f), new Color(0.2f, 0.25f, 0.35f), font, 18);

        // Slider
        var sliderGO = new GameObject("Slider");
        sliderGO.transform.SetParent(rowGO.transform, false);
        var sRT = sliderGO.AddComponent<RectTransform>();
        sRT.anchoredPosition = new Vector2(0f, -15f);
        sRT.sizeDelta = new Vector2(310f, 26f);

        slider = sliderGO.AddComponent<Slider>();

        // Slider background
        var sBg = new GameObject("Background");
        sBg.transform.SetParent(sliderGO.transform, false);
        var sBgRT = sBg.AddComponent<RectTransform>();
        sBgRT.anchorMin = new Vector2(0f, 0.35f);
        sBgRT.anchorMax = new Vector2(1f, 0.65f);
        sBgRT.sizeDelta = Vector2.zero;
        var sBgImg = sBg.AddComponent<Image>();
        sBgImg.color = new Color(0.12f, 0.16f, 0.22f);

        // Slider Fill Area
        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGO.transform, false);
        var faRT = fillArea.AddComponent<RectTransform>();
        faRT.anchorMin = new Vector2(0f, 0.35f);
        faRT.anchorMax = new Vector2(1f, 0.65f);
        faRT.sizeDelta = Vector2.zero;

        var fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fillRT = fill.AddComponent<RectTransform>();
        fillRT.sizeDelta = Vector2.zero;
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.22f, 0.74f, 0.97f);

        slider.fillRect = fillRT;

        // Slider Handle Slide Area
        var handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderGO.transform, false);
        var haRT = handleArea.AddComponent<RectTransform>();
        haRT.anchorMin = Vector2.zero;
        haRT.anchorMax = Vector2.one;
        haRT.sizeDelta = Vector2.zero;

        var handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        var hRT = handle.AddComponent<RectTransform>();
        hRT.sizeDelta = new Vector2(24f, 24f);
        var hImg = handle.AddComponent<Image>();
        hImg.color = Color.white;

        slider.handleRect = hRT;
        slider.targetGraphic = hImg;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color bgColor, Font font, int fontSize)
    {
        var btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent, false);
        var rt = btnGO.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var img = btnGO.AddComponent<Image>();
        img.color = bgColor;

        var btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;

        var colors = btn.colors;
        colors.normalColor = bgColor;
        colors.highlightedColor = bgColor * 1.25f;
        colors.pressedColor = bgColor * 0.8f;
        btn.colors = colors;

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(btnGO.transform, false);
        var textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;

        var text = textGO.AddComponent<Text>();
        text.font = font;
        text.text = label;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        return btn;
    }

    private static Text CreateText(Transform parent, string name, string content, Vector2 pos, Vector2 size, Font font, int fontSize, FontStyle style, Color color, TextAnchor align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var text = go.AddComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = align;

        return text;
    }

    private static void WireCollisionLabManager(VRUIRefs refs)
    {
        var managerGO = GameObject.Find("CollisionLabManager");
        if (managerGO == null)
        {
            managerGO = new GameObject("CollisionLabManager");
        }

        var mgr = managerGO.GetComponent<CollisionLabManager>();
        if (mgr == null)
        {
            mgr = managerGO.AddComponent<CollisionLabManager>();
        }

        var so = new SerializedObject(mgr);

        // Physical demonstration objects
        var sphere = GameObject.Find("Sphere");
        var cube = GameObject.Find("Cube");

        if (sphere != null)
        {
            so.FindProperty("sphereObject").objectReferenceValue = sphere.GetComponent<CollisionObject>();
            so.FindProperty("sphereRigidbody").objectReferenceValue = sphere.GetComponent<Rigidbody>();
            so.FindProperty("sphereVectorVisualizer").objectReferenceValue = sphere.GetComponent<PhysicsVectorVisualizer>();
        }

        if (cube != null)
        {
            so.FindProperty("cubeObject").objectReferenceValue = cube.GetComponent<CollisionObject>();
            so.FindProperty("cubeRigidbody").objectReferenceValue = cube.GetComponent<Rigidbody>();
            so.FindProperty("cubeVectorVisualizer").objectReferenceValue = cube.GetComponent<PhysicsVectorVisualizer>();
        }

        var contactVisualizer = GameObject.FindObjectOfType<ContactManifoldVisualizer>();
        if (contactVisualizer != null)
        {
            so.FindProperty("contactVisualizer").objectReferenceValue = contactVisualizer;
        }

        // Sliders & controls
        so.FindProperty("speedSlider").objectReferenceValue = refs.speedSlider;
        so.FindProperty("speedValueText").objectReferenceValue = refs.speedText;
        so.FindProperty("speedDownButton").objectReferenceValue = refs.speedDown;
        so.FindProperty("speedUpButton").objectReferenceValue = refs.speedUp;

        so.FindProperty("sphereMassSlider").objectReferenceValue = refs.sphereMassSlider;
        so.FindProperty("sphereMassValueText").objectReferenceValue = refs.sphereMassText;
        so.FindProperty("sphereMassDownButton").objectReferenceValue = refs.sphereMassDown;
        so.FindProperty("sphereMassUpButton").objectReferenceValue = refs.sphereMassUp;

        so.FindProperty("cubeMassSlider").objectReferenceValue = refs.cubeMassSlider;
        so.FindProperty("cubeMassValueText").objectReferenceValue = refs.cubeMassText;
        so.FindProperty("cubeMassDownButton").objectReferenceValue = refs.cubeMassDown;
        so.FindProperty("cubeMassUpButton").objectReferenceValue = refs.cubeMassUp;

        so.FindProperty("restitutionSlider").objectReferenceValue = refs.restitutionSlider;
        so.FindProperty("restitutionValueText").objectReferenceValue = refs.restitutionText;
        so.FindProperty("restitutionDownButton").objectReferenceValue = refs.restitutionDown;
        so.FindProperty("restitutionUpButton").objectReferenceValue = refs.restitutionUp;

        // Presets
        so.FindProperty("presetEqualButton").objectReferenceValue = refs.presetEqual;
        so.FindProperty("presetHeavyImpactorButton").objectReferenceValue = refs.presetHeavyImpactor;
        so.FindProperty("presetHeavyTargetButton").objectReferenceValue = refs.presetHeavyTarget;
        so.FindProperty("presetElasticButton").objectReferenceValue = refs.presetElastic;
        so.FindProperty("presetInelasticButton").objectReferenceValue = refs.presetInelastic;

        // Telemetry texts
        so.FindProperty("stateText").objectReferenceValue = refs.stateText;
        so.FindProperty("impactCountText").objectReferenceValue = refs.impactCountText;
        so.FindProperty("relativeVelocityText").objectReferenceValue = refs.relVelText;
        so.FindProperty("impulseText").objectReferenceValue = refs.impulseText;
        so.FindProperty("contactPointText").objectReferenceValue = refs.contactPointText;
        so.FindProperty("contactNormalText").objectReferenceValue = refs.contactNormalText;
        so.FindProperty("momentumBeforeText").objectReferenceValue = refs.momentumBeforeText;
        so.FindProperty("momentumAfterText").objectReferenceValue = refs.momentumAfterText;
        so.FindProperty("momentumErrorText").objectReferenceValue = refs.momentumErrorText;
        so.FindProperty("kineticEnergyText").objectReferenceValue = refs.kineticEnergyText;
        so.FindProperty("measuredRestitutionText").objectReferenceValue = refs.measuredRestitutionText;

        // Buttons
        so.FindProperty("launchButton").objectReferenceValue = refs.launchBtn;
        so.FindProperty("resetButton").objectReferenceValue = refs.resetBtn;
        so.FindProperty("backButton").objectReferenceValue = refs.backBtn;

        so.ApplyModifiedProperties();
        Debug.Log("[VRSceneBuilder] Successfully wired CollisionLabManager properties via SerializedObject.");
    }

    private static void UpdateBuildSettings(string vrScenePath)
    {
        var existingScenes = EditorBuildSettings.scenes;
        var sceneList = new List<EditorBuildSettingsScene>();

        bool hasMain = false;
        bool hasCollisionDesktop = false;
        bool hasVR = false;

        foreach (var s in existingScenes)
        {
            if (s.path == "Assets/Scenes/MainScene.unity")
            {
                hasMain = true;
                sceneList.Add(s);
            }
            else if (s.path == "Assets/Scenes/CollisionLab.unity")
            {
                hasCollisionDesktop = true;
                sceneList.Add(s);
            }
            else if (s.path == vrScenePath)
            {
                hasVR = true;
            }
        }

        if (!hasMain)
        {
            sceneList.Insert(0, new EditorBuildSettingsScene("Assets/Scenes/MainScene.unity", true));
        }

        if (!hasCollisionDesktop)
        {
            sceneList.Add(new EditorBuildSettingsScene("Assets/Scenes/CollisionLab.unity", true));
        }

        sceneList.Add(new EditorBuildSettingsScene(vrScenePath, true));

        EditorBuildSettings.scenes = sceneList.ToArray();

        Debug.Log("[VRSceneBuilder] Updated EditorBuildSettings. Total scenes: " + EditorBuildSettings.scenes.Length);
        for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
        {
            Debug.Log($"  [{i}] {EditorBuildSettings.scenes[i].path} (Enabled: {EditorBuildSettings.scenes[i].enabled})");
        }
    }
}

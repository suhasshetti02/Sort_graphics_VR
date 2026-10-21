using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
using VR.SortingChallenge;
using CollisionLab.VR;

public static class VRSortingChallengeSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/VRSortingChallenge.unity";
    private const string XrSetupPrefabPath = "Assets/Samples/XR Interaction Toolkit/2.6.3/Starter Assets/Prefabs/XR Interaction Setup.prefab";
    private const string SimPrefabPath = "Assets/Samples/XR Interaction Toolkit/2.6.3/XR Device Simulator/XR Device Simulator.prefab";

    [MenuItem("SortLab VR/Build VR Sorting Challenge Scene")]
    public static void BuildScene()
    {
        Debug.Log("[VRSortingChallengeSceneBuilder] Starting scene build...");

        // Create new empty scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Lighting & Environment
        CreateLightingAndEnvironment();

        // 2. Table and Target Slots
        var tableObj = CreateTabletop();
        var slots = CreateTargetSlots(tableObj.transform);
        var stagingPoints = CreateStagingRack(tableObj.transform);

        // 3. Five Numbered Cubes
        var cubes = CreateNumberedCubes(stagingPoints);

        // 4. In-Game World-Space UI Banner (Status & Game controls)
        var uiElements = CreateWorldSpaceUI();

        // 5. In-Game World-Space Controls Guide Panel (Authoritative Simulator Bindings)
        CreateControlsGuidePanel();

        // 6. Celebration Particles
        var celebrationFX = CreateCelebrationParticles();

        // 7. XR Rig & Device Simulator (Comfortably framed at 15 deg pitch down)
        CreateXRRigAndSimulator();

        // 8. Sorting Challenge Master Manager
        var mgrObj = new GameObject("SortingChallengeManager");
        var mgr = mgrObj.AddComponent<SortingChallengeManager>();

        // Wire serialized fields on manager using SerializedObject
        WireManager(mgr, slots, cubes, stagingPoints, uiElements, celebrationFX);

        // 9. Save the scene
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"[VRSortingChallengeSceneBuilder] Scene saved to {ScenePath}");

        // 10. Update Build Settings
        UpdateBuildSettings();

        // 11. Add Navigation Button in MainScene
        AddNavigationButtonInMainScene();

        Debug.Log("[VRSortingChallengeSceneBuilder] Scene build completed successfully!");
    }

    private static void CreateLightingAndEnvironment()
    {
        // Directional Light
        var lightObj = new GameObject("Directional Light");
        var light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(0.98f, 0.98f, 1.0f);
        light.intensity = 1.15f;
        light.shadows = LightShadows.Soft;
        lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // Flat ambient light
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.28f, 0.32f, 0.40f);

        // Floor
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.position = new Vector3(0f, -0.1f, 1.5f);
        floor.transform.localScale = new Vector3(18f, 0.2f, 18f);

        var floorMat = new Material(Shader.Find("Standard"));
        floorMat.color = new Color(0.08f, 0.1f, 0.14f);
        floorMat.SetFloat("_Glossiness", 0.35f);
        floor.GetComponent<Renderer>().material = floorMat;

        // Subtle back wall
        var backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backWall.name = "BackWall";
        backWall.transform.position = new Vector3(0f, 4f, 5.5f);
        backWall.transform.localScale = new Vector3(20f, 8f, 0.5f);
        var wallMat = new Material(Shader.Find("Standard"));
        wallMat.color = new Color(0.05f, 0.07f, 0.11f);
        backWall.GetComponent<Renderer>().material = wallMat;
    }

    private static GameObject CreateTabletop()
    {
        var tableParent = new GameObject("SortingTable");
        tableParent.transform.position = Vector3.zero;

        // Table Top: 2.2m wide, 1.1m deep, 0.08m thick, center at (0, 0.76, 1.15)
        // Top surface is exactly at Y = 0.80m, convenient for standing XR interaction
        var top = GameObject.CreatePrimitive(PrimitiveType.Cube);
        top.name = "TableTop";
        top.transform.SetParent(tableParent.transform);
        top.transform.position = new Vector3(0f, 0.76f, 1.15f);
        top.transform.localScale = new Vector3(2.2f, 0.08f, 1.1f);

        var topMat = new Material(Shader.Find("Standard"));
        topMat.color = new Color(0.12f, 0.15f, 0.22f); // Sleek tech dark slate
        topMat.SetFloat("_Glossiness", 0.55f);
        top.GetComponent<Renderer>().material = topMat;

        // Table Frame Accent Trim (no collider needed - remove to prevent interference)
        var trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
        trim.name = "TableTrim";
        trim.transform.SetParent(tableParent.transform);
        trim.transform.position = new Vector3(0f, 0.71f, 1.15f);
        trim.transform.localScale = new Vector3(2.24f, 0.04f, 1.14f);
        Object.DestroyImmediate(trim.GetComponent<Collider>());

        var trimMat = new Material(Shader.Find("Standard"));
        trimMat.color = new Color(0.02f, 0.65f, 0.78f); // Cyan accent trim
        trim.GetComponent<Renderer>().material = trimMat;

        // 4 Legs (no colliders needed)
        Vector3[] legOffsets = new Vector3[]
        {
            new Vector3(-1.0f, 0.36f, 0.65f),
            new Vector3( 1.0f, 0.36f, 0.65f),
            new Vector3(-1.0f, 0.36f, 1.65f),
            new Vector3( 1.0f, 0.36f, 1.65f),
        };

        var legMat = new Material(Shader.Find("Standard"));
        legMat.color = new Color(0.1f, 0.11f, 0.14f);
        legMat.SetFloat("_Glossiness", 0.4f);

        for (int i = 0; i < legOffsets.Length; i++)
        {
            var leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leg.name = $"TableLeg_{i + 1}";
            leg.transform.SetParent(tableParent.transform);
            leg.transform.position = legOffsets[i];
            leg.transform.localScale = new Vector3(0.10f, 0.36f, 0.10f);
            Object.DestroyImmediate(leg.GetComponent<Collider>());
            leg.GetComponent<Renderer>().material = legMat;
        }

        return tableParent;
    }

    private static SortingSlot[] CreateTargetSlots(Transform tableParent)
    {
        var slotsParent = new GameObject("TargetSlots");
        slotsParent.transform.SetParent(tableParent);

        var slots = new SortingSlot[5];
        float startX = -0.76f;
        float spacingX = 0.38f;
        float slotZ = 0.82f;
        float slotY = 0.805f; // Just resting on table top (y=0.80)

        for (int i = 0; i < 5; i++)
        {
            int index = i + 1;
            float posX = startX + (i * spacingX);
            var slotObj = new GameObject($"Slot_{index}");
            slotObj.transform.SetParent(slotsParent.transform);
            slotObj.transform.position = new Vector3(posX, slotY, slotZ);

            // Slot Pad (Square base, remove collider so it doesn't fight cubes or rays)
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = "SlotPad";
            pad.transform.SetParent(slotObj.transform);
            pad.transform.localPosition = Vector3.zero;
            pad.transform.localScale = new Vector3(0.26f, 0.004f, 0.26f);
            Object.DestroyImmediate(pad.GetComponent<Collider>());

            var padMat = new Material(Shader.Find("Standard"));
            padMat.color = new Color(0.08f, 0.12f, 0.18f);
            pad.GetComponent<Renderer>().material = padMat;

            // Slot Border Frame (Emissive outline, remove collider)
            var border = GameObject.CreatePrimitive(PrimitiveType.Cube);
            border.name = "SlotBorder";
            border.transform.SetParent(slotObj.transform);
            border.transform.localPosition = new Vector3(0f, 0.003f, 0f);
            border.transform.localScale = new Vector3(0.27f, 0.006f, 0.27f);
            Object.DestroyImmediate(border.GetComponent<Collider>());

            var borderMat = new Material(Shader.Find("Standard"));
            borderMat.color = new Color(0.2f, 0.7f, 1f, 0.8f);
            borderMat.EnableKeyword("_EMISSION");
            borderMat.SetColor("_EmissionColor", new Color(0.2f, 0.7f, 1f) * 0.4f);
            border.GetComponent<Renderer>().material = borderMat;

            // Snap Transform (Center where the cube rests: cube height = 0.20, half height = 0.10)
            var snapPoint = new GameObject("SnapPoint");
            snapPoint.transform.SetParent(slotObj.transform);
            snapPoint.transform.localPosition = new Vector3(0f, 0.105f, 0f);
            snapPoint.transform.localRotation = Quaternion.identity;

            // Slot Label Canvas in front of the slot (SLOT 1, SLOT 2, ...)
            var labelCanvasObj = new GameObject("SlotLabelCanvas");
            labelCanvasObj.transform.SetParent(slotObj.transform);
            labelCanvasObj.transform.localPosition = new Vector3(0f, 0.008f, -0.17f);
            labelCanvasObj.transform.localRotation = Quaternion.Euler(70f, 0f, 0f); // Angled up towards player eye

            var canvas = labelCanvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = labelCanvasObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160, 40);
            rect.localScale = Vector3.one * 0.0016f;

            var textObj = new GameObject("LabelText");
            textObj.transform.SetParent(labelCanvasObj.transform);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            textRect.localPosition = Vector3.zero;

            var text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.text = $"SLOT {index}";
            text.fontSize = 26;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.35f, 0.85f, 1.0f);

            // SortingSlot Component
            var sortingSlot = slotObj.AddComponent<SortingSlot>();
            var so = new SerializedObject(sortingSlot);
            so.FindProperty("slotIndex").intValue = index;
            so.FindProperty("snapTransform").objectReferenceValue = snapPoint.transform;
            so.FindProperty("borderRenderer").objectReferenceValue = border.GetComponent<Renderer>();
            so.ApplyModifiedProperties();

            slots[i] = sortingSlot;
        }

        return slots;
    }

    private static Transform[] CreateStagingRack(Transform tableParent)
    {
        var rackParent = new GameObject("StagingRack");
        rackParent.transform.SetParent(tableParent);

        var stagingPoints = new Transform[5];
        float startX = -0.76f;
        float spacingX = 0.38f;
        float stageZ = 1.30f;
        float stageY = 0.805f;

        for (int i = 0; i < 5; i++)
        {
            float posX = startX + (i * spacingX);

            // Staging Stand Visual (Square base, remove collider to prevent raycast/physics issues)
            var stand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stand.name = $"StagingStand_{i + 1}";
            stand.transform.SetParent(rackParent.transform);
            stand.transform.position = new Vector3(posX, stageY, stageZ);
            stand.transform.localScale = new Vector3(0.24f, 0.004f, 0.24f);
            Object.DestroyImmediate(stand.GetComponent<Collider>());

            var standMat = new Material(Shader.Find("Standard"));
            standMat.color = new Color(0.14f, 0.16f, 0.22f);
            stand.GetComponent<Renderer>().material = standMat;

            // Staging Point Transform (clean unscaled child of rackParent for exact positioning)
            var point = new GameObject($"StagingPoint_{i + 1}");
            point.transform.SetParent(rackParent.transform);
            point.transform.position = new Vector3(posX, 0.91f, stageZ); // Tabletop 0.80 + 0.005 + half cube 0.10 + small clearance
            point.transform.rotation = Quaternion.identity;

            stagingPoints[i] = point.transform;
        }

        return stagingPoints;
    }

    private static SortingChallengeCube[] CreateNumberedCubes(Transform[] stagingPoints)
    {
        var cubesParent = new GameObject("NumberedCubes");

        Color[] cubeColors = new Color[]
        {
            new Color(0.92f, 0.22f, 0.35f), // 1: Vibrant Coral Red
            new Color(0.95f, 0.60f, 0.12f), // 2: Warm Amber Gold
            new Color(0.08f, 0.72f, 0.65f), // 3: Emerald Cyan
            new Color(0.18f, 0.52f, 0.95f), // 4: Royal Blue
            new Color(0.58f, 0.28f, 0.95f)  // 5: Rich Violet
        };

        // Initial unsorted order: 3, 1, 5, 2, 4
        int[] initialOrder = new int[] { 3, 1, 5, 2, 4 };

        var cubes = new SortingChallengeCube[5];

        for (int i = 0; i < 5; i++)
        {
            int val = i + 1; // Cube value 1..5
            var cubeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubeObj.name = $"SortCube_{val}";
            cubeObj.transform.SetParent(cubesParent.transform);
            cubeObj.transform.localScale = new Vector3(0.20f, 0.20f, 0.20f);

            // Material
            var mat = new Material(Shader.Find("Standard"));
            mat.color = cubeColors[i];
            mat.SetFloat("_Glossiness", 0.6f);
            cubeObj.GetComponent<Renderer>().material = mat;

            // Rigidbody & Collider
            var rb = cubeObj.GetComponent<Rigidbody>();
            if (rb == null) rb = cubeObj.AddComponent<Rigidbody>();
            rb.mass = 1.0f;
            rb.drag = 5.0f;
            rb.angularDrag = 5.0f;
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            var col = cubeObj.GetComponent<BoxCollider>();
            col.size = Vector3.one;

            // XRGrabInteractable - Kinematic movement with smoothing
            var grab = cubeObj.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.Kinematic;
            grab.smoothPosition = true;
            grab.smoothPositionAmount = 14f;
            grab.smoothRotation = true;
            grab.smoothRotationAmount = 14f;
            grab.useDynamicAttach = true;
            grab.trackPosition = true;
            grab.trackRotation = true;
            grab.throwOnDetach = false;
            grab.retainTransformParent = false;
            grab.interactionLayers = 1; // Default layer

            // Bold Number Labels on ALL visible faces
            float halfExtent = 0.101f;
            // Front Face (-Z)
            CreateCubeFaceLabel(cubeObj.transform, new Vector3(0f, 0f, -halfExtent), Quaternion.identity, val.ToString());
            // Top Face (+Y)
            CreateCubeFaceLabel(cubeObj.transform, new Vector3(0f, halfExtent, 0f), Quaternion.Euler(90f, 0f, 0f), val.ToString());
            // Left Face (-X)
            CreateCubeFaceLabel(cubeObj.transform, new Vector3(-halfExtent, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), val.ToString());
            // Right Face (+X)
            CreateCubeFaceLabel(cubeObj.transform, new Vector3(halfExtent, 0f, 0f), Quaternion.Euler(0f, -90f, 0f), val.ToString());
            // Back Face (+Z)
            CreateCubeFaceLabel(cubeObj.transform, new Vector3(0f, 0f, halfExtent), Quaternion.Euler(0f, 180f, 0f), val.ToString());

            // SortingChallengeCube component
            var challengeCube = cubeObj.AddComponent<SortingChallengeCube>();
            challengeCube.CubeValue = val;

            cubes[i] = challengeCube;
        }

        // Position each cube according to the initial unsorted order [3, 1, 5, 2, 4]
        for (int slotIdx = 0; slotIdx < 5; slotIdx++)
        {
            int val = initialOrder[slotIdx];
            var cube = cubes[val - 1]; // 1-indexed value
            Vector3 targetPos = stagingPoints[slotIdx].position;
            cube.StagingPosition = targetPos;
            cube.StagingRotation = Quaternion.identity;
            cube.transform.position = targetPos;
            cube.transform.rotation = Quaternion.identity;
        }

        return cubes;
    }

    private static void CreateCubeFaceLabel(Transform cubeTransform, Vector3 localPos, Quaternion localRot, string textValue)
    {
        var labelCanvas = new GameObject("FaceLabel");
        labelCanvas.transform.SetParent(cubeTransform);
        labelCanvas.transform.localPosition = localPos;
        labelCanvas.transform.localRotation = localRot;

        var canvas = labelCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rect = labelCanvas.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(100, 100);
        rect.localScale = Vector3.one * 0.0018f;

        var textObj = new GameObject("NumberText");
        textObj.transform.SetParent(labelCanvas.transform);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        textRect.localPosition = Vector3.zero;

        var text = textObj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.text = textValue;
        text.fontSize = 72;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
    }

    private struct UIElements
    {
        public Text statusTitle;
        public Text feedbackMessage;
        public Text progressText;
        public Image backgroundPanel;
        public GameObject successBanner;
        public Button playAgainBtn;
        public Button resetBtn;
        public Button backMenuBtn;
    }

    private static UIElements CreateWorldSpaceUI()
    {
        // Positioned directly above the back edge of the table, perfectly framed in player view
        var canvasObj = new GameObject("GameStatus_Canvas");
        canvasObj.transform.position = new Vector3(0f, 1.42f, 1.65f);
        canvasObj.transform.rotation = Quaternion.Euler(16f, 0f, 0f); // Tilted gently down towards player eye

        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rect = canvasObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(760, 420);
        rect.localScale = Vector3.one * 0.0018f;

        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<TrackedDeviceGraphicRaycaster>();

        // Background Panel
        var bgObj = new GameObject("BackgroundPanel");
        bgObj.transform.SetParent(canvasObj.transform);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        bgRect.localPosition = Vector3.zero;

        var bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.06f, 0.12f, 0.22f, 0.94f);

        // Header Title
        var titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(bgObj.transform);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.05f, 0.78f);
        titleRect.anchorMax = new Vector2(0.95f, 0.96f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        var titleText = titleObj.AddComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        titleText.text = "VR SORTING CHALLENGE";
        titleText.fontSize = 32;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(0.25f, 0.85f, 1f);

        // Progress Text
        var progObj = new GameObject("ProgressText");
        progObj.transform.SetParent(bgObj.transform);
        var progRect = progObj.AddComponent<RectTransform>();
        progRect.anchorMin = new Vector2(0.05f, 0.64f);
        progRect.anchorMax = new Vector2(0.95f, 0.78f);
        progRect.offsetMin = Vector2.zero;
        progRect.offsetMax = Vector2.zero;

        var progText = progObj.AddComponent<Text>();
        progText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        progText.text = "0 / 5 SLOTS OCCUPIED";
        progText.fontSize = 20;
        progText.fontStyle = FontStyle.Bold;
        progText.alignment = TextAnchor.MiddleCenter;
        progText.color = new Color(0.95f, 0.75f, 0.2f);

        // Feedback / Instructions Text
        var feedObj = new GameObject("FeedbackText");
        feedObj.transform.SetParent(bgObj.transform);
        var feedRect = feedObj.AddComponent<RectTransform>();
        feedRect.anchorMin = new Vector2(0.06f, 0.34f);
        feedRect.anchorMax = new Vector2(0.94f, 0.62f);
        feedRect.offsetMin = Vector2.zero;
        feedRect.offsetMax = Vector2.zero;

        var feedText = feedObj.AddComponent<Text>();
        feedText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        feedText.text = "Pick up the numbered cubes and arrange them from smallest to largest.";
        feedText.fontSize = 22;
        feedText.alignment = TextAnchor.MiddleCenter;
        feedText.color = Color.white;

        // Success Banner Box
        var bannerObj = new GameObject("SuccessBanner");
        bannerObj.transform.SetParent(bgObj.transform);
        var bannerRect = bannerObj.AddComponent<RectTransform>();
        bannerRect.anchorMin = new Vector2(0.06f, 0.34f);
        bannerRect.anchorMax = new Vector2(0.94f, 0.62f);
        bannerRect.offsetMin = Vector2.zero;
        bannerRect.offsetMax = Vector2.zero;

        var bannerImg = bannerObj.AddComponent<Image>();
        bannerImg.color = new Color(0.1f, 0.65f, 0.25f, 0.95f);
        bannerObj.SetActive(false); // Initially hidden

        var bannerTextObj = new GameObject("BannerText");
        bannerTextObj.transform.SetParent(bannerObj.transform);
        var btRect = bannerTextObj.AddComponent<RectTransform>();
        btRect.anchorMin = Vector2.zero;
        btRect.anchorMax = Vector2.one;
        btRect.offsetMin = Vector2.zero;
        btRect.offsetMax = Vector2.zero;
        var bText = bannerTextObj.AddComponent<Text>();
        bText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        bText.text = "SUCCESS! SORTING COMPLETED\n1 -> 2 -> 3 -> 4 -> 5";
        bText.fontSize = 24;
        bText.fontStyle = FontStyle.Bold;
        bText.alignment = TextAnchor.MiddleCenter;
        bText.color = Color.white;

        // Button Container
        // 1. Play Again Button
        var playBtnObj = CreateButton(bgObj.transform, "PlayAgainButton", "PLAY AGAIN", new Vector2(0.05f, 0.08f), new Vector2(0.33f, 0.26f), new Color(0.08f, 0.6f, 0.4f));

        // 2. Reset Button
        var resetBtnObj = CreateButton(bgObj.transform, "ResetButton", "RESET", new Vector2(0.37f, 0.08f), new Vector2(0.63f, 0.26f), new Color(0.85f, 0.45f, 0.1f));

        // 3. Back to Main Menu Button
        var backBtnObj = CreateButton(bgObj.transform, "BackToMenuButton", "MAIN MENU", new Vector2(0.67f, 0.08f), new Vector2(0.95f, 0.26f), new Color(0.2f, 0.32f, 0.5f));

        return new UIElements
        {
            statusTitle = titleText,
            feedbackMessage = feedText,
            progressText = progText,
            backgroundPanel = bgImg,
            successBanner = bannerObj,
            playAgainBtn = playBtnObj.GetComponent<Button>(),
            resetBtn = resetBtnObj.GetComponent<Button>(),
            backMenuBtn = backBtnObj.GetComponent<Button>()
        };
    }

    private static void CreateControlsGuidePanel()
    {
        // Floating controls guide on the left of the table, angled inward toward player
        var guideObj = new GameObject("VR_Controls_Guide");
        guideObj.transform.position = new Vector3(-1.45f, 1.25f, 1.15f);
        guideObj.transform.rotation = Quaternion.Euler(10f, 35f, 0f);

        var canvas = guideObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rect = guideObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(580, 460);
        rect.localScale = Vector3.one * 0.0018f;

        guideObj.AddComponent<CanvasScaler>();

        // Background
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(guideObj.transform);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        var bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.05f, 0.09f, 0.16f, 0.94f);

        // Title
        var titleObj = new GameObject("GuideTitle");
        titleObj.transform.SetParent(bgObj.transform);
        var tRect = titleObj.AddComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0.05f, 0.85f);
        tRect.anchorMax = new Vector2(0.95f, 0.98f);
        tRect.offsetMin = Vector2.zero;
        tRect.offsetMax = Vector2.zero;

        var title = titleObj.AddComponent<Text>();
        title.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        title.text = "VR SIMULATOR CONTROLS";
        title.fontSize = 24;
        title.fontStyle = FontStyle.Bold;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = new Color(0.25f, 0.85f, 1.0f);

        // Body Text
        var bodyObj = new GameObject("GuideBody");
        bodyObj.transform.SetParent(bgObj.transform);
        var bRect = bodyObj.AddComponent<RectTransform>();
        bRect.anchorMin = new Vector2(0.06f, 0.04f);
        bRect.anchorMax = new Vector2(0.94f, 0.84f);
        bRect.offsetMin = Vector2.zero;
        bRect.offsetMax = Vector2.zero;

        var body = bodyObj.AddComponent<Text>();
        body.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        body.fontSize = 17;
        body.lineSpacing = 1.15f;
        body.color = Color.white;
        body.text =
            "\u2022 Look Around: Hold Right Mouse Button + move mouse\n" +
            "\u2022 Right Controller: Hold Space (or press Y to toggle)\n" +
            "\u2022 Left Controller: Hold Left Shift (or press T to toggle)\n" +
            "\u2022 Move Hand: Space + Mouse / W,A,S,D,Q,E\n" +
            "\u2022 Rotate Hand: Hold Left Ctrl + move mouse\n" +
            "\u2022 Grab / Release Cube: Press G (Grip)\n" +
            "\u2022 UI Click / Trigger: Left Mouse Button\n" +
            "\u2022 Reset Device Position: Press V\n\n" +
            "\u25b6 Place cubes 1..5 in order into Slots 1..5 to win!\n" +
            "\u25b6 Dropping outside slots safely returns cube to rack.";
    }

    private static GameObject CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Color btnColor)
    {
        var btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent);
        var rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var img = btnObj.AddComponent<Image>();
        img.color = btnColor;

        var btn = btnObj.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = btnColor;
        colors.highlightedColor = btnColor * 1.25f;
        colors.pressedColor = btnColor * 0.8f;
        btn.colors = colors;

        // Add BoxCollider for XR Ray Interactor clicking
        var col = btnObj.AddComponent<BoxCollider>();
        col.size = new Vector3(rect.rect.width, rect.rect.height, 1f);

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform);
        var tRect = textObj.AddComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero;
        tRect.anchorMax = Vector2.one;
        tRect.offsetMin = Vector2.zero;
        tRect.offsetMax = Vector2.zero;

        var text = textObj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.text = label;
        text.fontSize = 20;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        return btnObj;
    }

    private static ParticleSystem CreateCelebrationParticles()
    {
        var fxObj = new GameObject("CelebrationParticles");
        fxObj.transform.position = new Vector3(0f, 1.4f, 1.1f);

        var ps = fxObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 1.5f;
        main.startLifetime = 1.8f;
        main.startSpeed = 3.5f;
        main.startSize = 0.08f;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.2f, 0.9f, 1f), new Color(1f, 0.85f, 0.2f));
        main.gravityModifier = 0.5f;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 60) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 45f;
        shape.radius = 0.3f;

        return ps;
    }

    private static void CreateXRRigAndSimulator()
    {
        // XR Interaction Setup Prefab
        var xrSetupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(XrSetupPrefabPath);
        if (xrSetupPrefab != null)
        {
            var rigInstance = (GameObject)PrefabUtility.InstantiatePrefab(xrSetupPrefab);
            rigInstance.name = "XR Interaction Setup";
            // Position player comfortably in front of the table: (0, 0, -0.30)
            // Downward pitch of 15 deg frames the tabletop, slots, cubes, and UI banner squarely in center view!
            rigInstance.transform.position = new Vector3(0f, 0f, -0.30f);
            rigInstance.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
        }

        // XR Device Simulator Prefab
        var simPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SimPrefabPath);
        if (simPrefab != null)
        {
            var simInstance = (GameObject)PrefabUtility.InstantiatePrefab(simPrefab);
            simInstance.name = "XR Device Simulator";
        }

        // XR Bootstrap
        var bootObj = new GameObject("XRSceneBootstrap");
        bootObj.AddComponent<XRSceneBootstrap>();
    }

    private static void WireManager(SortingChallengeManager mgr, SortingSlot[] slots, SortingChallengeCube[] cubes, Transform[] stagingPoints, UIElements ui, ParticleSystem fx)
    {
        var so = new SerializedObject(mgr);

        var slotsProp = so.FindProperty("slots");
        slotsProp.arraySize = slots.Length;
        for (int i = 0; i < slots.Length; i++)
        {
            slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
        }

        var cubesProp = so.FindProperty("cubes");
        cubesProp.arraySize = cubes.Length;
        for (int i = 0; i < cubes.Length; i++)
        {
            cubesProp.GetArrayElementAtIndex(i).objectReferenceValue = cubes[i];
        }

        var stagingProp = so.FindProperty("stagingSlots");
        stagingProp.arraySize = stagingPoints.Length;
        for (int i = 0; i < stagingPoints.Length; i++)
        {
            stagingProp.GetArrayElementAtIndex(i).objectReferenceValue = stagingPoints[i];
        }

        so.FindProperty("statusTitleText").objectReferenceValue = ui.statusTitle;
        so.FindProperty("feedbackMessageText").objectReferenceValue = ui.feedbackMessage;
        so.FindProperty("slotProgressText").objectReferenceValue = ui.progressText;
        so.FindProperty("statusPanelBackground").objectReferenceValue = ui.backgroundPanel;
        so.FindProperty("successBannerObject").objectReferenceValue = ui.successBanner;
        so.FindProperty("playAgainButton").objectReferenceValue = ui.playAgainBtn;
        so.FindProperty("resetButton").objectReferenceValue = ui.resetBtn;
        so.FindProperty("backToMenuButton").objectReferenceValue = ui.backMenuBtn;
        so.FindProperty("celebrationParticles").objectReferenceValue = fx;

        so.ApplyModifiedProperties();
    }

    private static void UpdateBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        bool alreadyAdded = false;
        foreach (var s in scenes)
        {
            if (s.path == ScenePath)
            {
                alreadyAdded = true;
                break;
            }
        }

        if (!alreadyAdded)
        {
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"[VRSortingChallengeSceneBuilder] Added {ScenePath} to EditorBuildSettings.");
        }
    }

    private static void AddNavigationButtonInMainScene()
    {
        string mainScenePath = "Assets/Scenes/MainScene.unity";
        if (!File.Exists(mainScenePath))
        {
            Debug.LogWarning($"[VRSortingChallengeSceneBuilder] MainScene not found at {mainScenePath}");
            return;
        }

        var mainScene = EditorSceneManager.OpenScene(mainScenePath, OpenSceneMode.Single);

        // Check if button already exists
        var existingBtn = GameObject.Find("BtnVRSortingChallenge");
        if (existingBtn != null)
        {
            Debug.Log("[VRSortingChallengeSceneBuilder] BtnVRSortingChallenge already exists in MainScene.");
            return;
        }

        var collisionBtn = GameObject.Find("BtnCollisionLab");
        if (collisionBtn == null)
        {
            Debug.LogWarning("[VRSortingChallengeSceneBuilder] BtnCollisionLab not found in MainScene. Cannot clone button position.");
            return;
        }

        // Duplicate BtnCollisionLab to create BtnVRSortingChallenge
        var newBtn = Object.Instantiate(collisionBtn, collisionBtn.transform.parent);
        newBtn.name = "BtnVRSortingChallenge";

        // Position it below or to the side of BtnCollisionLab
        var rect = newBtn.GetComponent<RectTransform>();
        var origRect = collisionBtn.GetComponent<RectTransform>();
        if (rect != null && origRect != null)
        {
            rect.anchoredPosition = new Vector2(origRect.anchoredPosition.x + 210f, origRect.anchoredPosition.y);
        }

        // Update button text
        var text = newBtn.GetComponentInChildren<Text>();
        if (text != null)
        {
            text.text = "VR SORT CHALLENGE";
        }

        // Update SceneNavigator targetScene
        var nav = newBtn.GetComponent<UI.SceneNavigator>();
        if (nav != null)
        {
            var so = new SerializedObject(nav);
            var targetProp = so.FindProperty("targetScene");
            if (targetProp != null)
            {
                targetProp.stringValue = "VRSortingChallenge";
            }
            so.ApplyModifiedProperties();
        }

        EditorSceneManager.SaveScene(mainScene);
        Debug.Log("[VRSortingChallengeSceneBuilder] Created and saved BtnVRSortingChallenge in MainScene.");
    }
}

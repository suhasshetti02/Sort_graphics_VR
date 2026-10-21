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
    private const string MatFolder = "Assets/Materials/VRChallenge";
    private const string ArtifactScreenshotPath = "C:/Users/sande/.gemini/antigravity-ide/brain/23ade3bd-d523-4f34-9fb5-37a2a9669722/game_view_screenshot.png";

    [MenuItem("SortLab VR/Build VR Sorting Challenge Scene")]
    public static void BuildScene()
    {
        Debug.Log("[VRSortingChallengeSceneBuilder] Starting scene build with clean XR Rig, vibrant cubes, and readable labels...");

        EnsureMaterialFolder();

        // Create new empty scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Lighting & Environment
        CreateLightingAndEnvironment();

        // 2. Table and Target Slots (Compact 1.6m width, 0.8m depth, centered at (0, 0.76, 1.25))
        var tableObj = CreateTabletop();
        var slots = CreateTargetSlots(tableObj.transform);
        var stagingPoints = CreateStagingRack(tableObj.transform);

        // 3. Five Numbered Cubes (0.18m cubes with permanent materials, dark slate badges, bold 3D numbers & UI labels)
        var cubes = CreateNumberedCubes(stagingPoints);

        // 4. In-Game World-Space UI Banner (Status & Game controls)
        var uiElements = CreateWorldSpaceUI();

        // 5. In-Game World-Space Controls Guide Panel (Compact beside table)
        CreateControlsGuidePanel();

        // 6. Celebration Particles
        var celebrationFX = CreateCelebrationParticles();

        // 7. XR Rig & Device Simulator (Clean uncorrupted camera rig at (0, 0, -0.45), pitch = 18 deg)
        CreateXRRigAndSimulator();

        // 8. Sorting Challenge Master Manager
        var mgrObj = new GameObject("SortingChallengeManager");
        var mgr = mgrObj.AddComponent<SortingChallengeManager>();

        // Wire serialized fields on manager using SerializedObject
        WireManager(mgr, slots, cubes, stagingPoints, uiElements, celebrationFX);

        // 9. Save the scene & assets
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"[VRSortingChallengeSceneBuilder] Scene saved to {ScenePath}");

        // 10. Update Build Settings
        UpdateBuildSettings();

        // 11. Add Navigation Button in MainScene
        AddNavigationButtonInMainScene();

        // 12. Render and save diagnostic screenshot from camera view
        CaptureScenePreviewScreenshot();

        Debug.Log("[VRSortingChallengeSceneBuilder] Scene build completed successfully!");
    }

    private static void EnsureMaterialFolder()
    {
        if (!Directory.Exists(MatFolder))
        {
            Directory.CreateDirectory(MatFolder);
            AssetDatabase.Refresh();
        }
    }

    private static Material GetOrCreateMaterial(string name, Color color, float glossiness, Color? emission = null)
    {
        EnsureMaterialFolder();
        string path = $"{MatFolder}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.color = color;
        mat.SetFloat("_Glossiness", glossiness);
        if (emission.HasValue)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission.Value);
        }
        else
        {
            mat.DisableKeyword("_EMISSION");
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void CreateLightingAndEnvironment()
    {
        // Directional Key Light
        var lightObj = new GameObject("Directional Light");
        var light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1.0f, 0.98f, 0.95f);
        light.intensity = 1.05f;
        light.shadows = LightShadows.None;
        lightObj.transform.rotation = Quaternion.Euler(45f, -25f, 0f);

        // Soft Fill Light from right-front to illuminate all faces evenly
        var fillObj = new GameObject("FillLight");
        var fill = fillObj.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = new Color(0.75f, 0.85f, 1.0f);
        fill.intensity = 0.40f;
        fill.shadows = LightShadows.None;
        fillObj.transform.rotation = Quaternion.Euler(30f, 40f, 0f);

        // Ambient lighting
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.40f, 0.44f, 0.52f);

        // Floor (Matte, no specular hotspot)
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.position = new Vector3(0f, -0.1f, 1.5f);
        floor.transform.localScale = new Vector3(18f, 0.2f, 18f);
        floor.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("Mat_Floor", new Color(0.08f, 0.10f, 0.14f), 0.0f);

        // Subtle back wall
        var backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backWall.name = "BackWall";
        backWall.transform.position = new Vector3(0f, 4f, 5.5f);
        backWall.transform.localScale = new Vector3(20f, 8f, 0.5f);
        backWall.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("Mat_BackWall", new Color(0.06f, 0.08f, 0.12f), 0.0f);
    }

    private static GameObject CreateTabletop()
    {
        var tableParent = new GameObject("SortingTable");
        tableParent.transform.position = Vector3.zero;

        // Table Top: 1.6m wide, 0.8m deep, 0.08m thick, center at (0, 0.76, 1.25)
        var top = GameObject.CreatePrimitive(PrimitiveType.Cube);
        top.name = "TableTop";
        top.transform.SetParent(tableParent.transform);
        top.transform.position = new Vector3(0f, 0.76f, 1.25f);
        top.transform.localScale = new Vector3(1.60f, 0.08f, 0.80f);
        top.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("Mat_TableTop", new Color(0.14f, 0.17f, 0.24f), 0.20f);

        // Table Frame Accent Trim (no collider)
        var trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
        trim.name = "TableTrim";
        trim.transform.SetParent(tableParent.transform);
        trim.transform.position = new Vector3(0f, 0.71f, 1.25f);
        trim.transform.localScale = new Vector3(1.64f, 0.04f, 0.84f);
        Object.DestroyImmediate(trim.GetComponent<Collider>());
        trim.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("Mat_TableTrim", new Color(0.05f, 0.75f, 0.90f), 0.45f);

        // 4 Legs (no colliders)
        Vector3[] legOffsets = new Vector3[]
        {
            new Vector3(-0.72f, 0.36f, 0.90f),
            new Vector3( 0.72f, 0.36f, 0.90f),
            new Vector3(-0.72f, 0.36f, 1.60f),
            new Vector3( 0.72f, 0.36f, 1.60f),
        };

        var matLeg = GetOrCreateMaterial("Mat_TableLeg", new Color(0.12f, 0.13f, 0.16f), 0.25f);
        for (int i = 0; i < legOffsets.Length; i++)
        {
            var leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leg.name = $"TableLeg_{i + 1}";
            leg.transform.SetParent(tableParent.transform);
            leg.transform.position = legOffsets[i];
            leg.transform.localScale = new Vector3(0.08f, 0.36f, 0.08f);
            Object.DestroyImmediate(leg.GetComponent<Collider>());
            leg.GetComponent<Renderer>().sharedMaterial = matLeg;
        }

        return tableParent;
    }

    private static SortingSlot[] CreateTargetSlots(Transform tableParent)
    {
        var slotsParent = new GameObject("TargetSlots");
        slotsParent.transform.SetParent(tableParent);

        var slots = new SortingSlot[5];
        float startX = -0.52f;
        float spacingX = 0.26f;
        float slotZ = 1.02f;
        float slotY = 0.805f;

        var matPad = GetOrCreateMaterial("Mat_SlotPad", new Color(0.08f, 0.12f, 0.18f), 0.2f);
        var matBorder = GetOrCreateMaterial("Mat_SlotBorder", new Color(0.2f, 0.7f, 1f, 0.8f), 0.5f, new Color(0.2f, 0.7f, 1f) * 0.4f);
        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        Material fontMat = font != null ? font.material : null;

        for (int i = 0; i < 5; i++)
        {
            int index = i + 1;
            float posX = startX + (i * spacingX);
            var slotObj = new GameObject($"Slot_{index}");
            slotObj.transform.SetParent(slotsParent.transform);
            slotObj.transform.position = new Vector3(posX, slotY, slotZ);

            // Slot Pad (Square base, no collider)
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = "SlotPad";
            pad.transform.SetParent(slotObj.transform);
            pad.transform.localPosition = Vector3.zero;
            pad.transform.localScale = new Vector3(0.22f, 0.004f, 0.22f);
            Object.DestroyImmediate(pad.GetComponent<Collider>());
            pad.GetComponent<Renderer>().sharedMaterial = matPad;

            // Slot Border Frame (Emissive outline, no collider)
            var border = GameObject.CreatePrimitive(PrimitiveType.Cube);
            border.name = "SlotBorder";
            border.transform.SetParent(slotObj.transform);
            border.transform.localPosition = new Vector3(0f, 0.003f, 0f);
            border.transform.localScale = new Vector3(0.23f, 0.006f, 0.23f);
            Object.DestroyImmediate(border.GetComponent<Collider>());
            border.GetComponent<Renderer>().sharedMaterial = matBorder;

            // Snap Transform (Center where the cube rests)
            var snapPoint = new GameObject("SnapPoint");
            snapPoint.transform.SetParent(slotObj.transform);
            snapPoint.transform.localPosition = new Vector3(0f, 0.095f, 0f);
            snapPoint.transform.localRotation = Quaternion.identity;

            // 3D Slot Label on tabletop in front of slot ("SLOT 1", "SLOT 2", ...)
            var slotLabelObj = new GameObject("SlotLabel3D");
            slotLabelObj.transform.SetParent(slotObj.transform);
            slotLabelObj.transform.localPosition = new Vector3(0f, 0.012f, -0.15f);
            slotLabelObj.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
            slotLabelObj.transform.localScale = Vector3.one * 0.016f;

            var tm = slotLabelObj.AddComponent<TextMesh>();
            tm.font = font;
            tm.text = $"SLOT {index}";
            tm.fontSize = 32;
            tm.fontStyle = FontStyle.Bold;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = new Color(0.30f, 0.85f, 1.0f);
            var tmMr = slotLabelObj.GetComponent<MeshRenderer>();
            if (fontMat != null) tmMr.sharedMaterial = fontMat;

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
        float startX = -0.52f;
        float spacingX = 0.26f;
        float stageZ = 1.45f;
        float stageY = 0.805f;

        var matStand = GetOrCreateMaterial("Mat_StagingStand", new Color(0.14f, 0.16f, 0.22f), 0.2f);

        for (int i = 0; i < 5; i++)
        {
            float posX = startX + (i * spacingX);

            // Staging Stand Visual
            var stand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stand.name = $"StagingStand_{i + 1}";
            stand.transform.SetParent(rackParent.transform);
            stand.transform.position = new Vector3(posX, stageY, stageZ);
            stand.transform.localScale = new Vector3(0.22f, 0.004f, 0.22f);
            Object.DestroyImmediate(stand.GetComponent<Collider>());
            stand.GetComponent<Renderer>().sharedMaterial = matStand;

            // Staging Point Transform
            var point = new GameObject($"StagingPoint_{i + 1}");
            point.transform.SetParent(rackParent.transform);
            point.transform.position = new Vector3(posX, 0.895f, stageZ);
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
            new Color(0.92f, 0.20f, 0.30f), // 1: Vibrant Coral Red
            new Color(0.96f, 0.60f, 0.10f), // 2: Warm Amber Gold
            new Color(0.08f, 0.75f, 0.40f), // 3: Emerald Jade Green
            new Color(0.12f, 0.45f, 0.95f), // 4: Royal Blue
            new Color(0.65f, 0.20f, 0.90f)  // 5: Rich Violet
        };

        Material[] cubeMats = new Material[5];
        for (int i = 0; i < 5; i++)
        {
            cubeMats[i] = GetOrCreateMaterial(
                $"Mat_Cube_{i + 1}",
                cubeColors[i],
                0.25f,
                cubeColors[i] * 0.35f
            );
        }

        var matBadge = GetOrCreateMaterial("Mat_Badge", new Color(0.04f, 0.06f, 0.10f), 0.1f);
        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        Material fontMat = font != null ? font.material : null;

        // Initial unsorted order: 3, 1, 5, 2, 4
        int[] initialOrder = new int[] { 3, 1, 5, 2, 4 };
        var cubes = new SortingChallengeCube[5];

        for (int i = 0; i < 5; i++)
        {
            int val = i + 1;
            var cubeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubeObj.name = $"SortCube_{val}";
            cubeObj.transform.SetParent(cubesParent.transform);
            cubeObj.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);
            cubeObj.GetComponent<Renderer>().sharedMaterial = cubeMats[i];

            // Rigidbody
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

            // XRGrabInteractable
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
            grab.interactionLayers = 1;

            // 5 Visible Face Labels with dark obsidian slate badge, bold 3D TextMesh, and UI Text
            CreateFaceLabel(cubeObj.transform, "Front", new Vector3(0f, 0f, -0.505f), Quaternion.Euler(0f, 0f, 0f), val.ToString(), font, fontMat, matBadge);
            CreateFaceLabel(cubeObj.transform, "Top", new Vector3(0f, 0.505f, 0f), Quaternion.Euler(90f, 0f, 0f), val.ToString(), font, fontMat, matBadge);
            CreateFaceLabel(cubeObj.transform, "Left", new Vector3(-0.505f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), val.ToString(), font, fontMat, matBadge);
            CreateFaceLabel(cubeObj.transform, "Right", new Vector3(0.505f, 0f, 0f), Quaternion.Euler(0f, -90f, 0f), val.ToString(), font, fontMat, matBadge);
            CreateFaceLabel(cubeObj.transform, "Back", new Vector3(0f, 0f, 0.505f), Quaternion.Euler(0f, 180f, 0f), val.ToString(), font, fontMat, matBadge);

            // SortingChallengeCube component
            var challengeCube = cubeObj.AddComponent<SortingChallengeCube>();
            challengeCube.CubeValue = val;

            cubes[i] = challengeCube;
        }

        // Position each cube according to initial unsorted order [3, 1, 5, 2, 4]
        for (int slotIdx = 0; slotIdx < 5; slotIdx++)
        {
            int val = initialOrder[slotIdx];
            var cube = cubes[val - 1];
            Vector3 targetPos = stagingPoints[slotIdx].position;
            cube.StagingPosition = targetPos;
            cube.StagingRotation = Quaternion.identity;
            cube.transform.position = targetPos;
            cube.transform.rotation = Quaternion.identity;
        }

        return cubes;
    }

    private static void CreateFaceLabel(Transform parent, string faceName, Vector3 localPos, Quaternion localRot, string numberText, Font font, Material fontMat, Material badgeMat)
    {
        var labelParent = new GameObject($"FaceNumber_{faceName}");
        labelParent.transform.SetParent(parent);
        labelParent.transform.localPosition = localPos;
        labelParent.transform.localRotation = localRot;
        labelParent.transform.localScale = Vector3.one;

        // 1. Dark Slate Badge Quad (0.76 x 0.76 of face)
        var badge = GameObject.CreatePrimitive(PrimitiveType.Quad);
        badge.name = "BadgeQuad";
        badge.transform.SetParent(labelParent.transform);
        badge.transform.localPosition = Vector3.zero;
        badge.transform.localRotation = Quaternion.identity;
        badge.transform.localScale = new Vector3(0.76f, 0.76f, 1f);
        Object.DestroyImmediate(badge.GetComponent<Collider>());
        badge.GetComponent<Renderer>().sharedMaterial = badgeMat;

        // 2. Bold White 3D TextMesh (in front of badge)
        var textObj = new GameObject("Text3D");
        textObj.transform.SetParent(labelParent.transform);
        textObj.transform.localPosition = new Vector3(0f, 0f, -0.012f);
        textObj.transform.localRotation = Quaternion.identity;
        textObj.transform.localScale = Vector3.one * 0.085f;

        var tm = textObj.AddComponent<TextMesh>();
        tm.font = font;
        tm.text = numberText;
        tm.fontSize = 72;
        tm.fontStyle = FontStyle.Bold;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = Color.white;

        var mr = textObj.GetComponent<MeshRenderer>();
        if (fontMat != null)
        {
            mr.sharedMaterial = fontMat;
        }

        // 3. Child UI Text for UI verification compatibility
        var canvasObj = new GameObject("FaceCanvas");
        canvasObj.transform.SetParent(labelParent.transform);
        canvasObj.transform.localPosition = new Vector3(0f, 0f, -0.015f);
        canvasObj.transform.localRotation = Quaternion.identity;
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rect = canvasObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(100, 100);
        rect.localScale = Vector3.one * 0.0076f;

        var uiTextObj = new GameObject("Text");
        uiTextObj.transform.SetParent(canvasObj.transform);
        var tRect = uiTextObj.AddComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero;
        tRect.anchorMax = Vector2.one;
        tRect.offsetMin = Vector2.zero;
        tRect.offsetMax = Vector2.zero;
        var uiText = uiTextObj.AddComponent<Text>();
        uiText.font = font;
        uiText.text = numberText;
        uiText.fontSize = 72;
        uiText.fontStyle = FontStyle.Bold;
        uiText.alignment = TextAnchor.MiddleCenter;
        uiText.color = Color.white;
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
        var canvasObj = new GameObject("GameStatus_Canvas");
        canvasObj.transform.position = new Vector3(0f, 1.48f, 1.82f);
        canvasObj.transform.rotation = Quaternion.Euler(3.5f, 0f, 0f);

        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rect = canvasObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(680, 360);
        rect.localScale = Vector3.one * 0.0016f;

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
        titleText.fontSize = 30;
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
        feedText.fontSize = 20;
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
        bannerObj.SetActive(false);

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
        bText.fontSize = 22;
        bText.fontStyle = FontStyle.Bold;
        bText.alignment = TextAnchor.MiddleCenter;
        bText.color = Color.white;

        // Buttons
        var playBtnObj = CreateButton(bgObj.transform, "PlayAgainButton", "PLAY AGAIN", new Vector2(0.05f, 0.08f), new Vector2(0.33f, 0.26f), new Color(0.08f, 0.6f, 0.4f));
        var resetBtnObj = CreateButton(bgObj.transform, "ResetButton", "RESET", new Vector2(0.37f, 0.08f), new Vector2(0.63f, 0.26f), new Color(0.85f, 0.45f, 0.1f));
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
        var guideObj = new GameObject("VR_Controls_Guide");
        guideObj.transform.position = new Vector3(-1.08f, 1.15f, 1.30f);
        guideObj.transform.rotation = Quaternion.Euler(10f, 32f, 0f);

        var canvas = guideObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rect = guideObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(460, 380);
        rect.localScale = Vector3.one * 0.0014f;

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
        title.fontSize = 22;
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
        body.fontSize = 15;
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
            "\u25b6 Arrange cubes 1..5 in order into Slots 1..5 to win!\n" +
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
        text.fontSize = 18;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        return btnObj;
    }

    private static ParticleSystem CreateCelebrationParticles()
    {
        var fxObj = new GameObject("CelebrationParticles");
        fxObj.transform.position = new Vector3(0f, 1.4f, 1.25f);

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

            // Unpack prefab to guarantee zero inherited skew or corrupt rotations
            PrefabUtility.UnpackPrefabInstance(rigInstance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            // Cleanly reset any corrupted internal transforms
            var xrRig = rigInstance.transform.Find("XR Origin (XR Rig)");
            if (xrRig != null)
            {
                xrRig.localPosition = Vector3.zero;
                xrRig.localRotation = Quaternion.identity;

                var camOffset = xrRig.Find("Camera Offset");
                if (camOffset != null)
                {
                    camOffset.localPosition = new Vector3(0f, 1.36f, 0f);
                    camOffset.localRotation = Quaternion.identity;

                    var mainCam = camOffset.Find("Main Camera");
                    if (mainCam != null)
                    {
                        mainCam.localPosition = Vector3.zero;
                        mainCam.localRotation = Quaternion.identity;

                        // Disable TunnelingVignette if present
                        var vignette = mainCam.Find("TunnelingVignette");
                        if (vignette != null)
                        {
                            vignette.gameObject.SetActive(false);
                        }
                    }
                }
            }

            // Set XR Origin at (0, 0, -0.45) with clean 18 deg downward pitch
            rigInstance.transform.position = new Vector3(0f, 0f, -0.45f);
            rigInstance.transform.rotation = Quaternion.Euler(18f, 0f, 0f);

            // Disable foreground Poke Visuals that obstruct the camera
            foreach (var r in rigInstance.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (r.gameObject.name == "Poke Point" || r.gameObject.name == "Cylinder")
                {
                    r.enabled = false;
                }
            }
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

        var newBtn = Object.Instantiate(collisionBtn, collisionBtn.transform.parent);
        newBtn.name = "BtnVRSortingChallenge";

        var rect = newBtn.GetComponent<RectTransform>();
        var origRect = collisionBtn.GetComponent<RectTransform>();
        if (rect != null && origRect != null)
        {
            rect.anchoredPosition = new Vector2(origRect.anchoredPosition.x + 210f, origRect.anchoredPosition.y);
        }

        var text = newBtn.GetComponentInChildren<Text>();
        if (text != null)
        {
            text.text = "VR SORT CHALLENGE";
        }

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

    public static void CaptureScenePreviewScreenshot()
    {
        if (EditorSceneManager.GetActiveScene().path != ScenePath)
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        var cam = Camera.main;
        if (cam == null) cam = Object.FindObjectOfType<Camera>();
        if (cam == null) return;

        foreach (var c in Object.FindObjectsOfType<Canvas>())
        {
            if (c.renderMode == RenderMode.WorldSpace)
            {
                c.worldCamera = cam;
            }
        }
        Canvas.ForceUpdateCanvases();

        int width = 1920;
        int height = 1080;
        var rt = new RenderTexture(width, height, 24);
        var prevRT = RenderTexture.active;
        var prevTarget = cam.targetTexture;

        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        byte[] pngBytes = tex.EncodeToPNG();
        string outPath = "Assets/game_view_screenshot.png";
        File.WriteAllBytes(outPath, pngBytes);
        File.WriteAllBytes(ArtifactScreenshotPath, pngBytes);
        Debug.Log($"[VRSortingChallengeSceneBuilder] Captured game preview screenshot to {outPath} & {ArtifactScreenshotPath} ({pngBytes.Length} bytes)");

        cam.targetTexture = prevTarget;
        RenderTexture.active = prevRT;
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(rt);
    }
}

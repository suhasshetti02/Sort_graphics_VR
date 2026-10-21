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

        // 2. Table and Target Slots (1.6m width, 0.8m depth, centered at (0, 0.76, 1.25))
        var tableObj = CreateTabletop();
        var slots = CreateTargetSlots(tableObj.transform);
        var stagingPoints = CreateStagingRack(tableObj.transform);

        // 3. Five Numbered Cubes (0.18m cubes with vibrant luminous materials, dark slate badges, bold numbers)
        var cubes = CreateNumberedCubes(stagingPoints);

        // 4. In-Game World-Space UI Banner (Status & Game controls, positioned comfortably in camera frame)
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
        // Directional Key Light: Angled from behind/above camera into scene (+Z)
        var lightObj = new GameObject("Directional Light");
        var light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1.0f, 0.98f, 0.95f);
        light.intensity = 0.95f;
        light.shadows = LightShadows.None;
        lightObj.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

        // Soft Fill Light
        var fillObj = new GameObject("FillLight");
        var fill = fillObj.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = new Color(0.85f, 0.90f, 1.0f);
        fill.intensity = 0.30f;
        fill.shadows = LightShadows.None;
        fillObj.transform.rotation = Quaternion.Euler(30f, 40f, 0f);

        // Flat Ambient lighting
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.25f, 0.28f, 0.35f);
        RenderSettings.fog = false;

        // Floor (Matte dark navy floor)
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.position = new Vector3(0f, -0.1f, 1.5f);
        floor.transform.localScale = new Vector3(18f, 0.2f, 18f);
        floor.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("Mat_Floor", new Color(0.08f, 0.10f, 0.14f), 0.0f);

        // Back wall
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
        top.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("Mat_TableTop", new Color(0.12f, 0.15f, 0.20f), 0.05f);

        // Table Frame Accent Trim (no collider)
        var trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
        trim.name = "TableTrim";
        trim.transform.SetParent(tableParent.transform);
        trim.transform.position = new Vector3(0f, 0.71f, 1.25f);
        trim.transform.localScale = new Vector3(1.64f, 0.04f, 0.84f);
        Object.DestroyImmediate(trim.GetComponent<Collider>());
        trim.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("Mat_TableTrim", new Color(0.05f, 0.65f, 0.85f), 0.15f);

        // 4 Legs (no colliders)
        Vector3[] legOffsets = new Vector3[]
        {
            new Vector3(-0.72f, 0.36f, 0.90f),
            new Vector3( 0.72f, 0.36f, 0.90f),
            new Vector3(-0.72f, 0.36f, 1.60f),
            new Vector3( 0.72f, 0.36f, 1.60f),
        };

        var matLeg = GetOrCreateMaterial("Mat_TableLeg", new Color(0.10f, 0.12f, 0.16f), 0.15f);
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

        var matPad = GetOrCreateMaterial("Mat_SlotPad", new Color(0.08f, 0.11f, 0.16f), 0.05f);
        var matBorder = GetOrCreateMaterial("Mat_SlotBorder", new Color(0.2f, 0.7f, 1.0f, 0.85f), 0.20f, new Color(0.2f, 0.7f, 1.0f) * 0.4f);
        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        Material fontMat = font != null ? font.material : null;

        for (int i = 0; i < 5; i++)
        {
            int index = i + 1;
            float posX = startX + (i * spacingX);
            var slotObj = new GameObject($"Slot_{index}");
            slotObj.transform.SetParent(slotsParent.transform);
            slotObj.transform.position = new Vector3(posX, slotY, slotZ);

            // Slot Border Frame (Emissive base outline, no collider)
            var border = GameObject.CreatePrimitive(PrimitiveType.Cube);
            border.name = "SlotBorder";
            border.transform.SetParent(slotObj.transform, false);
            border.transform.localPosition = new Vector3(0f, 0.002f, 0f);
            border.transform.localScale = new Vector3(0.23f, 0.004f, 0.23f);
            Object.DestroyImmediate(border.GetComponent<Collider>());
            border.GetComponent<Renderer>().sharedMaterial = matBorder;

            // Slot Pad (Dark square center inset resting on top of border frame, no collider)
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = "SlotPad";
            pad.transform.SetParent(slotObj.transform, false);
            pad.transform.localPosition = new Vector3(0f, 0.004f, 0f);
            pad.transform.localScale = new Vector3(0.19f, 0.005f, 0.19f);
            Object.DestroyImmediate(pad.GetComponent<Collider>());
            pad.GetComponent<Renderer>().sharedMaterial = matPad;

            // Snap Transform (Center where resting 0.18m cube rests; cube center at Y = 0.895m)
            var snapPoint = new GameObject("SnapPoint");
            snapPoint.transform.SetParent(slotObj.transform);
            snapPoint.transform.localPosition = new Vector3(0f, 0.090f, 0f);
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
            tm.color = new Color(0.25f, 0.80f, 1.0f);
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

        var matStand = GetOrCreateMaterial("Mat_StagingStand", new Color(0.10f, 0.13f, 0.18f), 0.05f);

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

            // Staging Point Transform (Resting cube center at Y = 0.895m)
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

        // Vibrant, luminous cube colors with distinct hues and 1.2x emissive radiance
        Color[] cubeColors = new Color[]
        {
            new Color(0.95f, 0.20f, 0.30f), // 1: Coral Red
            new Color(1.00f, 0.65f, 0.10f), // 2: Warm Amber Gold
            new Color(0.10f, 0.85f, 0.40f), // 3: Emerald Jade Green
            new Color(0.15f, 0.55f, 1.00f), // 4: Royal Blue
            new Color(0.75f, 0.25f, 1.00f)  // 5: Rich Violet
        };

        Material[] cubeMats = new Material[5];
        for (int i = 0; i < 5; i++)
        {
            cubeMats[i] = GetOrCreateMaterial(
                $"Mat_Cube_{i + 1}",
                cubeColors[i],
                0.10f,
                cubeColors[i] * 1.20f // Highly vibrant luminous emission so bodies glow beautifully
            );
        }

        // Dark charcoal slate badge material
        var matBadge = GetOrCreateMaterial("Mat_Badge", new Color(0.06f, 0.08f, 0.12f), 0.05f);
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

            // Attach Face Badges (Front, Top, Back):
            CreateFaceLabel(cubeObj.transform, "Front", new Vector3(0f, 0f, -0.502f), Quaternion.identity, val.ToString(), font, fontMat, matBadge, true);
            CreateFaceLabel(cubeObj.transform, "Top", new Vector3(0f, 0.502f, 0f), Quaternion.Euler(90f, 0f, 0f), val.ToString(), font, fontMat, matBadge, false);
            // Back face label removed to prevent GUI/Text Shader depth bleedthrough

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

    private static void CreateFaceLabel(Transform parent, string faceName, Vector3 localPos, Quaternion localRot, string numberText, Font font, Material fontMat, Material badgeMat, bool includeCanvas)
    {
        var labelParent = new GameObject($"FaceNumber_{faceName}");
        labelParent.transform.SetParent(parent, false);
        labelParent.transform.localPosition = localPos;
        labelParent.transform.localRotation = localRot;
        labelParent.transform.localScale = Vector3.one;

        // 1. Dark Slate Badge Quad (scaled to 0.50 x 0.50 so the vibrant colored cube body is prominently visible around it)
        var badge = GameObject.CreatePrimitive(PrimitiveType.Quad);
        badge.name = "BadgeQuad";
        badge.transform.SetParent(labelParent.transform, false);
        badge.transform.localPosition = Vector3.zero;
        badge.transform.localRotation = Quaternion.identity;
        badge.transform.localScale = new Vector3(0.50f, 0.50f, 1f);
        Object.DestroyImmediate(badge.GetComponent<Collider>());
        badge.GetComponent<Renderer>().sharedMaterial = badgeMat;

        // 2. Bold White 3D TextMesh (in front of badge)
        var textObj = new GameObject("Text3D");
        textObj.transform.SetParent(labelParent.transform, false);
        textObj.transform.localPosition = new Vector3(0f, 0f, -0.005f);
        textObj.transform.localRotation = Quaternion.identity;
        textObj.transform.localScale = Vector3.one * 0.070f;

        var tm = textObj.AddComponent<TextMesh>();
        tm.font = font;
        tm.text = numberText;
        tm.fontSize = 68;
        tm.fontStyle = FontStyle.Bold;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = Color.white;

        var mr = textObj.GetComponent<MeshRenderer>();
        if (fontMat != null)
        {
            mr.sharedMaterial = fontMat;
        }

        // Only single bold 3D TextMesh and badge to eliminate any duplicate numbers or ghosting
    }

    public struct UIComponentGroup
    {
        public Text statusTitle;
        public Text feedbackMessage;
        public Text progressText;
        public Image backgroundPanel;
        public GameObject successBanner;
        public Button playAgainBtn;
        public Button resetBtn;
        public Button backMenuBtn;

        public TextMesh titleTextMesh;
        public TextMesh feedbackTextMesh;
        public TextMesh progressTextMesh;
        public GameObject successBannerMesh;
    }

    private static UIComponentGroup CreateWorldSpaceUI()
    {
        // 1. Root 3D Panel Object at (0, 1.24, 1.76) angled 14 deg downward (scale 1.0)
        var panelObj = new GameObject("GameStatus_Panel");
        panelObj.transform.position = new Vector3(0f, 1.24f, 1.76f);
        panelObj.transform.rotation = Quaternion.Euler(14f, 0f, 0f);

        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        Material fontMat = font != null ? font.material : null;

        // 3D Background Mesh: 0.84m wide, 0.44m tall
        var bgMesh = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bgMesh.name = "BackgroundMesh";
        bgMesh.transform.SetParent(panelObj.transform, false);
        bgMesh.transform.localPosition = new Vector3(0f, 0f, 0.002f);
        bgMesh.transform.localRotation = Quaternion.identity;
        bgMesh.transform.localScale = new Vector3(0.84f, 0.44f, 1f);
        Object.DestroyImmediate(bgMesh.GetComponent<Collider>());
        bgMesh.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("Mat_StatusBG", new Color(0.06f, 0.12f, 0.22f), 0.10f, new Color(0.04f, 0.08f, 0.16f));

        // 3D Title TextMesh
        var title3DObj = new GameObject("Title3D");
        title3DObj.transform.SetParent(panelObj.transform, false);
        title3DObj.transform.localPosition = new Vector3(0f, 0.14f, -0.005f);
        title3DObj.transform.localRotation = Quaternion.identity;
        title3DObj.transform.localScale = Vector3.one * 0.016f;
        var tmTitle = title3DObj.AddComponent<TextMesh>();
        tmTitle.font = font;
        tmTitle.text = "VR SORTING CHALLENGE";
        tmTitle.fontSize = 32;
        tmTitle.fontStyle = FontStyle.Bold;
        tmTitle.alignment = TextAlignment.Center;
        tmTitle.anchor = TextAnchor.MiddleCenter;
        tmTitle.color = new Color(0.25f, 0.85f, 1.0f);
        if (fontMat != null) title3DObj.GetComponent<MeshRenderer>().sharedMaterial = fontMat;

        // 3D Progress TextMesh
        var prog3DObj = new GameObject("Progress3D");
        prog3DObj.transform.SetParent(panelObj.transform, false);
        prog3DObj.transform.localPosition = new Vector3(0f, 0.07f, -0.005f);
        prog3DObj.transform.localRotation = Quaternion.identity;
        prog3DObj.transform.localScale = Vector3.one * 0.014f;
        var tmProg = prog3DObj.AddComponent<TextMesh>();
        tmProg.font = font;
        tmProg.text = "0 / 5 SLOTS OCCUPIED";
        tmProg.fontSize = 26;
        tmProg.fontStyle = FontStyle.Bold;
        tmProg.alignment = TextAlignment.Center;
        tmProg.anchor = TextAnchor.MiddleCenter;
        tmProg.color = new Color(0.95f, 0.75f, 0.20f);
        if (fontMat != null) prog3DObj.GetComponent<MeshRenderer>().sharedMaterial = fontMat;

        // 3D Feedback TextMesh
        var feed3DObj = new GameObject("Feedback3D");
        feed3DObj.transform.SetParent(panelObj.transform, false);
        feed3DObj.transform.localPosition = new Vector3(0f, -0.01f, -0.005f);
        feed3DObj.transform.localRotation = Quaternion.identity;
        feed3DObj.transform.localScale = Vector3.one * 0.011f;
        var tmFeed = feed3DObj.AddComponent<TextMesh>();
        tmFeed.font = font;
        tmFeed.text = "Pick up the numbered cubes and arrange them from smallest to largest.";
        tmFeed.fontSize = 22;
        tmFeed.alignment = TextAlignment.Center;
        tmFeed.anchor = TextAnchor.MiddleCenter;
        tmFeed.color = Color.white;
        if (fontMat != null) feed3DObj.GetComponent<MeshRenderer>().sharedMaterial = fontMat;

        // 3D Success Banner
        var bannerMeshObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bannerMeshObj.name = "SuccessBannerMesh";
        bannerMeshObj.transform.SetParent(panelObj.transform, false);
        bannerMeshObj.transform.localPosition = new Vector3(0f, -0.01f, -0.006f);
        bannerMeshObj.transform.localRotation = Quaternion.identity;
        bannerMeshObj.transform.localScale = new Vector3(0.76f, 0.12f, 1f);
        Object.DestroyImmediate(bannerMeshObj.GetComponent<Collider>());
        bannerMeshObj.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("Mat_BannerGreen", new Color(0.1f, 0.65f, 0.25f), 0.2f, new Color(0.1f, 0.65f, 0.25f) * 0.5f);
        bannerMeshObj.SetActive(false);

        // 2. Child Canvas (Named GameStatus_Canvas for backwards compatibility with tests and UI Event Camera)
        var canvasObj = new GameObject("GameStatus_Canvas");
        canvasObj.transform.SetParent(panelObj.transform, false);
        canvasObj.transform.localPosition = Vector3.zero;
        canvasObj.transform.localRotation = Quaternion.identity;
        canvasObj.transform.localScale = Vector3.one * 0.00135f;

        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rect = canvasObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(620, 320);

        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<TrackedDeviceGraphicRaycaster>();

        // UI Background Panel
        var bgObj = new GameObject("BackgroundPanel");
        bgObj.transform.SetParent(canvasObj.transform, false);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        bgRect.localPosition = Vector3.zero;
        bgRect.localScale = Vector3.one;
        var bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.06f, 0.12f, 0.22f, 0.0f);

        // UI Title Text
        var titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(bgObj.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.05f, 0.78f);
        titleRect.anchorMax = new Vector2(0.95f, 0.96f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        var titleText = titleObj.AddComponent<Text>();
        titleText.font = font;
        titleText.text = "VR SORTING CHALLENGE";
        titleText.fontSize = 28;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.clear;

        // UI Progress Text
        var progObj = new GameObject("ProgressText");
        progObj.transform.SetParent(bgObj.transform, false);
        var progRect = progObj.AddComponent<RectTransform>();
        progRect.anchorMin = new Vector2(0.05f, 0.64f);
        progRect.anchorMax = new Vector2(0.95f, 0.78f);
        progRect.offsetMin = Vector2.zero;
        progRect.offsetMax = Vector2.zero;
        var progText = progObj.AddComponent<Text>();
        progText.font = font;
        progText.text = "0 / 5 SLOTS OCCUPIED";
        progText.fontSize = 20;
        progText.fontStyle = FontStyle.Bold;
        progText.alignment = TextAnchor.MiddleCenter;
        progText.color = Color.clear;

        // UI Feedback Text
        var feedObj = new GameObject("FeedbackText");
        feedObj.transform.SetParent(bgObj.transform, false);
        var feedRect = feedObj.AddComponent<RectTransform>();
        feedRect.anchorMin = new Vector2(0.06f, 0.34f);
        feedRect.anchorMax = new Vector2(0.94f, 0.62f);
        feedRect.offsetMin = Vector2.zero;
        feedRect.offsetMax = Vector2.zero;
        var feedText = feedObj.AddComponent<Text>();
        feedText.font = font;
        feedText.text = "Pick up the numbered cubes and arrange them from smallest to largest.";
        feedText.fontSize = 18;
        feedText.alignment = TextAnchor.MiddleCenter;
        feedText.color = Color.clear;

        // UI Success Banner
        var bannerObj = new GameObject("SuccessBanner");
        bannerObj.transform.SetParent(bgObj.transform, false);
        var bannerRect = bannerObj.AddComponent<RectTransform>();
        bannerRect.anchorMin = new Vector2(0.06f, 0.34f);
        bannerRect.anchorMax = new Vector2(0.94f, 0.62f);
        bannerRect.offsetMin = Vector2.zero;
        bannerRect.offsetMax = Vector2.zero;
        var bannerImg = bannerObj.AddComponent<Image>();
        bannerImg.color = new Color(0.1f, 0.65f, 0.25f, 0.95f);
        bannerObj.SetActive(false);

        // Buttons
        var playBtnObj = CreateButton(panelObj.transform, bgObj.transform, "PlayAgainButton", "PLAY AGAIN", new Vector2(0.05f, 0.08f), new Vector2(0.33f, 0.26f), new Vector3(-0.25f, -0.13f, -0.005f), new Color(0.08f, 0.6f, 0.4f), font, fontMat);
        var resetBtnObj = CreateButton(panelObj.transform, bgObj.transform, "ResetButton", "RESET", new Vector2(0.37f, 0.08f), new Vector2(0.63f, 0.26f), new Vector3(0.0f, -0.13f, -0.005f), new Color(0.85f, 0.45f, 0.1f), font, fontMat);
        var backBtnObj = CreateButton(panelObj.transform, bgObj.transform, "BackToMenuButton", "MAIN MENU", new Vector2(0.67f, 0.08f), new Vector2(0.95f, 0.26f), new Vector3(0.25f, -0.13f, -0.005f), new Color(0.2f, 0.32f, 0.5f), font, fontMat);

        return new UIComponentGroup
        {
            statusTitle = titleText,
            feedbackMessage = feedText,
            progressText = progText,
            backgroundPanel = bgImg,
            successBanner = bannerObj,
            playAgainBtn = playBtnObj.GetComponent<Button>(),
            resetBtn = resetBtnObj.GetComponent<Button>(),
            backMenuBtn = backBtnObj.GetComponent<Button>(),

            titleTextMesh = tmTitle,
            feedbackTextMesh = tmFeed,
            progressTextMesh = tmProg,
            successBannerMesh = bannerMeshObj
        };
    }

    private static void CreateControlsGuidePanel()
    {
        var guideObj = new GameObject("VR_Controls_Guide");
        guideObj.transform.position = new Vector3(-0.95f, 1.15f, 1.35f);
        guideObj.transform.rotation = Quaternion.Euler(12f, 30f, 0f);

        var canvas = guideObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rect = guideObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(460, 380);
        rect.localScale = Vector3.one * 0.0013f;

        guideObj.AddComponent<CanvasScaler>();

        // Background
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(guideObj.transform, false);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        var bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.05f, 0.09f, 0.16f, 0.94f);

        // Title
        var titleObj = new GameObject("GuideTitle");
        titleObj.transform.SetParent(bgObj.transform, false);
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
        bodyObj.transform.SetParent(bgObj.transform, false);
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
            "• Look Around: Hold Right Mouse Button + move mouse\n" +
            "• Right Controller: Hold Space (or press Y to toggle)\n" +
            "• Left Controller: Hold Left Shift (or press T to toggle)\n" +
            "• Move Hand: Space + Mouse / W,A,S,D,Q,E\n" +
            "• Rotate Hand: Hold Left Ctrl + move mouse\n" +
            "• Grab / Release Cube: Press G (Grip)\n" +
            "• UI Click / Trigger: Left Mouse Button\n" +
            "• Reset Device Position: Press V\n\n" +
            "▶ Arrange cubes 1..5 in order into Slots 1..5 to win!\n" +
            "▶ Dropping outside slots safely returns cube to rack.";
    }

    private static GameObject CreateButton(Transform panelRoot, Transform uiParent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector3 local3DPos, Color btnColor, Font font, Material fontMat)
    {
        // 3D Quad for button surface: 0.22m wide, 0.075m tall
        var btnMesh = GameObject.CreatePrimitive(PrimitiveType.Quad);
        btnMesh.name = $"{name}_Mesh";
        btnMesh.transform.SetParent(panelRoot, false);
        btnMesh.transform.localPosition = local3DPos;
        btnMesh.transform.localRotation = Quaternion.identity;
        btnMesh.transform.localScale = new Vector3(0.22f, 0.075f, 1f);
        Object.DestroyImmediate(btnMesh.GetComponent<Collider>());
        btnMesh.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial($"Mat_Btn_{name}", btnColor, 0.20f, btnColor * 0.40f);

        // 3D Label
        var lbl3D = new GameObject("Label3D");
        lbl3D.transform.SetParent(btnMesh.transform, false);
        lbl3D.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        lbl3D.transform.localRotation = Quaternion.identity;
        lbl3D.transform.localScale = Vector3.one * 0.06f;
        var tm = lbl3D.AddComponent<TextMesh>();
        tm.font = font;
        tm.text = label;
        tm.fontSize = 24;
        tm.fontStyle = FontStyle.Bold;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = Color.white;
        if (fontMat != null) lbl3D.GetComponent<MeshRenderer>().sharedMaterial = fontMat;

        // UI Button GameObject (child of UI canvas)
        var btnObj = new GameObject(name);
        btnObj.transform.SetParent(uiParent, false);
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
        colors.pressedColor = btnColor * 0.85f;
        btn.colors = colors;

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        var tRect = textObj.AddComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero;
        tRect.anchorMax = Vector2.one;
        tRect.offsetMin = Vector2.zero;
        tRect.offsetMax = Vector2.zero;

        var text = textObj.AddComponent<Text>();
        text.font = font;
        text.text = label;
        text.fontSize = 16;
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

                        var camComp = mainCam.GetComponent<Camera>();
                        if (camComp != null)
                        {
                            camComp.clearFlags = CameraClearFlags.SolidColor;
                            camComp.backgroundColor = new Color(0.06f, 0.08f, 0.12f);
                        }

                        // Destroy full-screen TunnelingVignette to completely eliminate camera obstruction / foggy wash
                        var vignette = mainCam.Find("TunnelingVignette");
                        if (vignette != null)
                        {
                            foreach (var r in vignette.GetComponentsInChildren<Renderer>(true))
                            {
                                r.enabled = false;
                            }
                            Object.DestroyImmediate(vignette.gameObject);
                        }
                    }

                    // Scale down controller visual models to prevent foreground obstruction while keeping interactors 100% active
                    foreach (var ctrlName in new string[] { "Left Controller", "Right Controller" })
                    {
                        var ctrlT = camOffset.Find(ctrlName);
                        if (ctrlT != null)
                        {
                            var ctrl = ctrlT.GetComponent<ActionBasedController>();
                            if (ctrl != null)
                            {
                                var modelRoot = new GameObject("ModelRoot");
                                modelRoot.transform.SetParent(ctrlT);
                                modelRoot.transform.localPosition = new Vector3(0f, -0.05f, -0.04f);
                                modelRoot.transform.localRotation = Quaternion.identity;
                                modelRoot.transform.localScale = new Vector3(0.50f, 0.50f, 0.50f);

                                ctrl.modelParent = modelRoot.transform;
                            }
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

    private static void WireManager(SortingChallengeManager mgr, SortingSlot[] slots, SortingChallengeCube[] cubes, Transform[] stagingPoints, UIComponentGroup ui, ParticleSystem fx)
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

        so.FindProperty("maxHorizontalDistance").floatValue = 0.22f;
        so.FindProperty("minVerticalDifference").floatValue = -0.15f;
        so.FindProperty("maxVerticalDifference").floatValue = 0.50f;
        so.FindProperty("enableReleaseDiagnostics").boolValue = true;

        so.FindProperty("statusTitleText").objectReferenceValue = ui.statusTitle;
        so.FindProperty("feedbackMessageText").objectReferenceValue = ui.feedbackMessage;
        so.FindProperty("slotProgressText").objectReferenceValue = ui.progressText;
        so.FindProperty("statusPanelBackground").objectReferenceValue = ui.backgroundPanel;
        so.FindProperty("successBannerObject").objectReferenceValue = ui.successBanner;
        so.FindProperty("playAgainButton").objectReferenceValue = ui.playAgainBtn;
        so.FindProperty("resetButton").objectReferenceValue = ui.resetBtn;
        so.FindProperty("backToMenuButton").objectReferenceValue = ui.backMenuBtn;
        so.FindProperty("celebrationParticles").objectReferenceValue = fx;

        so.FindProperty("statusTitleTextMesh").objectReferenceValue = ui.titleTextMesh;
        so.FindProperty("feedbackMessageTextMesh").objectReferenceValue = ui.feedbackTextMesh;
        so.FindProperty("slotProgressTextMesh").objectReferenceValue = ui.progressTextMesh;
        so.FindProperty("successBannerMesh").objectReferenceValue = ui.successBannerMesh;

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

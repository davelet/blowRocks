#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor tool: one-click scene setup for blowRocks.
/// Menu: blowRocks → Setup Scene
/// </summary>
public class BlowRocksSetup
{
    [MenuItem("blowRocks/Setup Scene")]
    public static void SetupScene()
    {
        // Setup Chinese font first (needed for localization)
        ChineseFontSetup.SetupChineseFont();

        CreateSprites();

        // ============================================
        //  1. Clean up existing scene objects
        // ============================================
        var rootObjects = new System.Collections.Generic.List<GameObject>();
        foreach (var go in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            try { if (go != null && go.parent == null) rootObjects.Add(go.gameObject); }
            catch { }
        }
        foreach (var go in rootObjects)
        {
            if (go != null) Object.DestroyImmediate(go);
        }

        // ============================================
        //  2. Configure Camera
        // ============================================
        var cam = Camera.main;
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
        }
        cam.orthographic = true;
        cam.orthographicSize = 6;
        cam.backgroundColor = new Color(0.039f, 0.039f, 0.07f);
        cam.transform.position = new Vector3(0, 0, -10);
        cam.clearFlags = CameraClearFlags.SolidColor;

        // ============================================
        //  3. Create Player
        // ============================================
        var player = CreateSquareSprite("Player", new Color(0f, 0.8f, 1f));
        player.transform.position = Vector3.zero;
        player.transform.localScale = new Vector3(0.6f, 0.8f, 1f);
        player.tag = "Player";

        var rb2d = player.AddComponent<Rigidbody2D>();
        rb2d.gravityScale = 0;
        rb2d.drag = 0;

        var playerCol = player.AddComponent<BoxCollider2D>();
        playerCol.size = new Vector2(1f, 1.2f);

        var playerCtrl = player.AddComponent<PlayerController>();

        // Create fire point
        var firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(player.transform);
        firePoint.transform.localPosition = new Vector3(0, 0.6f, 0);

        // ============================================
        //  4. Create Bullet Prefab
        // ============================================
        var bullet = CreateCircleSprite("Bullet", Color.yellow);
        bullet.transform.localScale = Vector3.one * 0.15f;

        var bulletRb = bullet.AddComponent<Rigidbody2D>();
        bulletRb.gravityScale = 0;

        var bulletCol = bullet.AddComponent<CircleCollider2D>();
        bulletCol.isTrigger = true;
        bulletCol.radius = 0.5f;

        bullet.AddComponent<Bullet>();
        bullet.GetComponent<SpriteRenderer>().sortingOrder = 10;

        // Save as prefab
        EnsureFolder("Assets/Prefabs/Weapons");
        var bulletPrefab = SavePrefab(bullet, "Assets/Prefabs/Weapons/Bullet.prefab");

        // ============================================
        //  5. Create Asteroid Prefabs
        // ============================================
        EnsureFolder("Assets/Prefabs/Asteroids");

        // Ensure "Asteroid" tag exists
        AddTagIfMissing("Asteroid");

        // Large asteroid
        var astLarge = CreateRockSprite("Asteroid_Large", new Color(0.55f, 0.55f, 0.55f));
        astLarge.transform.localScale = Vector3.one * 1.6f;
        SetupAsteroid(astLarge);
        var largePrefab = SavePrefab(astLarge, "Assets/Prefabs/Asteroids/Asteroid_Large.prefab");

        // Medium asteroid
        var astMed = CreateRockSprite("Asteroid_Medium", new Color(0.65f, 0.65f, 0.65f));
        astMed.transform.localScale = Vector3.one * 1.0f;
        SetupAsteroid(astMed);
        var medPrefab = SavePrefab(astMed, "Assets/Prefabs/Asteroids/Asteroid_Medium.prefab");

        // Small asteroid
        var astSmall = CreateRockSprite("Asteroid_Small", new Color(0.75f, 0.75f, 0.75f));
        astSmall.transform.localScale = Vector3.one * 0.5f;
        SetupAsteroid(astSmall);
        var smallPrefab = SavePrefab(astSmall, "Assets/Prefabs/Asteroids/Asteroid_Small.prefab");

        // Link splits: Large → Medium, Medium → Small
        SetAsteroidSplit(largePrefab, medPrefab);
        SetAsteroidSplit(medPrefab, smallPrefab);

        // ============================================
        //  6. Create GameManager
        // ============================================
        var gmObj = new GameObject("GameManager");
        var gm = gmObj.AddComponent<GameManager>();
        var spawner = gmObj.AddComponent<AsteroidSpawner>();

        // Link references via SerializedObject
        var gmSO = new SerializedObject(gm);
        gmSO.FindProperty("player").objectReferenceValue = player.GetComponent<PlayerController>();
        gmSO.FindProperty("spawner").objectReferenceValue = spawner;
        gmSO.ApplyModifiedProperties();

        var spawnerSO = new SerializedObject(spawner);
        var prefabsArray = spawnerSO.FindProperty("asteroidPrefabs");
        prefabsArray.arraySize = 3;
        prefabsArray.GetArrayElementAtIndex(0).objectReferenceValue = largePrefab;
        prefabsArray.GetArrayElementAtIndex(1).objectReferenceValue = medPrefab;
        prefabsArray.GetArrayElementAtIndex(2).objectReferenceValue = smallPrefab;
        spawnerSO.ApplyModifiedProperties();

        // Link PlayerController bullet prefab & fire point
        var pcSO = new SerializedObject(playerCtrl);
        pcSO.FindProperty("bulletPrefab").objectReferenceValue = bulletPrefab;
        pcSO.FindProperty("firePoint").objectReferenceValue = firePoint.transform;
        pcSO.ApplyModifiedProperties();

        // ============================================
        //  6b. Add VFX, StarField, and Localization
        // ============================================
        gmObj.AddComponent<VFX>();
        gmObj.AddComponent<SFX>();
        gmObj.AddComponent<StarField>();

        // Localization Manager
        var locGo = new GameObject("LocalizationManager");
        locGo.AddComponent<LocalizationManager>();

        // ============================================
        //  7. Create UI Canvas
        // ============================================
        CreateGameUI(gm);

        // ============================================
        //  8. Create ScreenBoundary helper
        // ============================================
        gmObj.AddComponent<ScreenBoundary>();

        // ============================================
        //  9. Save scene and add to build settings
        // ============================================
        EnsureFolder("Assets/Scenes");
        var scenePath = "Assets/Scenes/MainScene.unity";
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene(),
            scenePath
        );
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[] {
            new EditorBuildSettingsScene(scenePath, true)
        };

        Debug.Log("=== blowRocks scene setup complete! Press Play to test. ===");
        EditorUtility.DisplayDialog("blowRocks", "Scene setup complete!\n\nPress Play to test the game.", "OK");
    }

    // ================================================
    //  Helper methods
    // ================================================

    static Sprite squareSprite;
    static Sprite circleSprite;
    static Sprite rockSprite;

    static void CreateSprites()
    {
        EnsureFolder("Assets/Sprites");

        // --- Ship: sleek arrow/ship shape ---
        squareSprite = CreateAndSaveSprite("Assets/Sprites/Ship.png", 128, 128, (x, y, s) => {
            float cx = s / 2f, cy = s / 2f;
            float dx = x - cx, dy = y - cy;
            // Triangle ship pointing up
            float shipTop = s * 0.42f;
            float shipBottom = -s * 0.3f;
            float shipHalfW = s * 0.22f;
            // Body: triangle from top to bottom
            if (dy >= shipBottom && dy <= shipTop)
            {
                float t = (dy - shipBottom) / (shipTop - shipBottom);
                float halfW = Mathf.Lerp(shipHalfW * 0.6f, shipHalfW * 0.1f, t);
                if (Mathf.Abs(dx) <= halfW)
                {
                    // Edge glow
                    float edgeDist = 1f - Mathf.Abs(dx) / halfW;
                    float brightness = Mathf.Clamp01(edgeDist * 1.5f);
                    return new Color(0.6f, 0.85f, 1f, brightness);
                }
            }
            // Wings
            float wingY = shipBottom + (shipTop - shipBottom) * 0.25f;
            if (dy >= shipBottom && dy <= wingY)
            {
                float wt = (dy - shipBottom) / (wingY - shipBottom);
                float wingW = Mathf.Lerp(shipHalfW * 1.4f, shipHalfW * 0.8f, wt);
                if (Mathf.Abs(dx) > shipHalfW * 0.4f && Mathf.Abs(dx) <= wingW)
                {
                    float edgeDist = 1f - Mathf.Abs(Mathf.Abs(dx) - shipHalfW * 0.9f) / (wingW * 0.5f);
                    float brightness = Mathf.Clamp01(edgeDist * 1.2f);
                    return new Color(0.3f, 0.7f, 1f, brightness * 0.7f);
                }
            }
            // Engine glow at bottom
            float engineDist = Mathf.Sqrt(dx * dx + (dy - shipBottom) * (dy - shipBottom));
            if (engineDist < s * 0.15f)
            {
                float glow = 1f - engineDist / (s * 0.15f);
                return new Color(1f, 0.6f, 0.2f, glow * 0.8f);
            }
            return Color.clear;
        });

        // --- Bullet: glowing projectile ---
        circleSprite = CreateAndSaveSprite("Assets/Sprites/Bullet.png", 64, 64, (x, y, s) => {
            float cx = s / 2f, cy = s / 2f;
            float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
            float maxR = s * 0.4f;
            if (dist > maxR) return Color.clear;
            float t = 1f - dist / maxR;
            // Inner bright core, outer glow
            Color core = new Color(1f, 1f, 0.6f, 1f);
            Color glow = new Color(1f, 0.8f, 0.2f, 0.6f);
            return Color.Lerp(glow, core, t * t);
        });

        // --- App Icon: ship on starfield ---
        CreateAndSaveIcon("Assets/Sprites/AppIcon.png", 1024, 1024);

        // --- Rock: detailed asteroid with shading and craters ---
        rockSprite = CreateAndSaveSprite("Assets/Sprites/Rock.png", 128, 128, (x, y, s) => {
            float center = s / 2f;
            int sides = 10;
            float[] radii = new float[sides];
            var rng = new System.Random(42);
            for (int i = 0; i < sides; i++)
                radii[i] = (s / 2f - 6) * (float)(0.72 + rng.NextDouble() * 0.28);
            float dx = x - center, dy = y - center;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            float angle = Mathf.Atan2(dy, dx);
            if (angle < 0) angle += Mathf.PI * 2;
            float sectorAngle = (Mathf.PI * 2) / sides;
            int idx = Mathf.FloorToInt(angle / sectorAngle);
            float t = (angle - idx * sectorAngle) / sectorAngle;
            float r0 = radii[idx % sides];
            float r1 = radii[(idx + 1) % sides];
            float r = Mathf.Lerp(r0, r1, t);
            if (dist > r) return Color.clear;

            // Base color with shading (light from top-left) - lighter to stand out
            float nx = dx / r, ny = dy / r;
            float light = Mathf.Clamp01(nx * -0.4f + ny * 0.5f + 0.6f);
            Color baseColor = new Color(0.75f * light, 0.7f * light, 0.65f * light, 1f);

            // Crater 1
            float c1dist = Vector2.Distance(new Vector2(x, y), new Vector2(center + r * 0.2f, center - r * 0.15f));
            if (c1dist < r * 0.22f)
            {
                float ct = c1dist / (r * 0.22f);
                baseColor = Color.Lerp(new Color(0.25f, 0.24f, 0.23f, 1f), baseColor, ct);
            }
            // Crater 2
            float c2dist = Vector2.Distance(new Vector2(x, y), new Vector2(center - r * 0.25f, center + r * 0.2f));
            if (c2dist < r * 0.15f)
            {
                float ct = c2dist / (r * 0.15f);
                baseColor = Color.Lerp(new Color(0.22f, 0.21f, 0.2f, 1f), baseColor, ct);
            }
            // Edge highlight
            float edgeFade = Mathf.Clamp01((r - dist) / (r * 0.12f));
            baseColor.a = edgeFade;
            return baseColor;
        });
    }

    static Sprite CreateAndSaveSprite(string path, int w, int h, System.Func<int, int, int, Color> pixelFunc)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                pixels[y * w + x] = pixelFunc(x, y, w);
        tex.SetPixels(pixels);
        tex.Apply();
        var png = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, png);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        importer.spritePixelsPerUnit = w / 2;
        importer.textureType = TextureImporterType.Sprite;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void CreateAndSaveIcon(string path, int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var rng = new System.Random(7);
        float cx = w / 2f, cy = h / 2f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx, dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                Color c;

                // Starfield background
                float noise = (float)(rng.NextDouble());
                if (noise > 0.997f)
                    c = new Color(1f, 1f, 1f, 0.9f);
                else if (noise > 0.993f)
                    c = new Color(0.8f, 0.85f, 1f, 0.6f);
                else
                    c = new Color(0.05f, 0.05f, 0.12f, 1f);

                // Ship body (triangle pointing up)
                float shipTop = h * 0.37f;
                float shipBottom = -h * 0.22f;
                float shipHalfW = w * 0.18f;
                if (dy >= shipBottom && dy <= shipTop)
                {
                    float t = (dy - shipBottom) / (shipTop - shipBottom);
                    float halfW = Mathf.Lerp(shipHalfW * 0.7f, shipHalfW * 0.08f, t);
                    if (Mathf.Abs(dx) <= halfW)
                    {
                        float edgeDist = 1f - Mathf.Abs(dx) / halfW;
                        float brightness = Mathf.Clamp01(edgeDist * 1.5f);
                        c = Color.Lerp(c, new Color(0.4f, 0.8f, 1f, 1f), brightness);
                    }
                }

                // Engine glow
                float engineDist = Mathf.Sqrt(dx * dx + (dy - shipBottom) * (dy - shipBottom));
                if (engineDist < w * 0.12f)
                {
                    float glow = 1f - engineDist / (w * 0.12f);
                    c = Color.Lerp(c, new Color(1f, 0.6f, 0.2f, 1f), glow * 0.8f);
                }

                // Rounded corners (mask to circle-ish rounded rect)
                float cornerR = w * 0.18f;
                float halfSide = w / 2f;
                float cornerCx = Mathf.Sign(dx) * (halfSide - cornerR);
                float cornerCy = Mathf.Sign(dy) * (halfSide - cornerR);
                if (Mathf.Abs(dx) > halfSide - cornerR && Mathf.Abs(dy) > halfSide - cornerR)
                {
                    float cDist = Mathf.Sqrt((dx - cornerCx) * (dx - cornerCx) + (dy - cornerCy) * (dy - cornerCy));
                    if (cDist > cornerR)
                        c.a *= Mathf.Clamp01((cornerR + 2f - cDist) / 2f);
                }

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        var png = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, png);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        importer.textureType = TextureImporterType.Default;
        importer.isReadable = true;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 1024;
        importer.SaveAndReimport();
    }

    static GameObject CreateSquareSprite(string name, Color color)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = squareSprite;
        sr.color = color;
        return go;
    }

    static GameObject CreateCircleSprite(string name, Color color)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = circleSprite;
        sr.color = color;
        return go;
    }

    static GameObject CreateRockSprite(string name, Color color)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = rockSprite;
        sr.color = color;
        return go;
    }

    static void SetupAsteroid(GameObject go)
    {
        go.tag = "Asteroid";

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.45f;

        go.AddComponent<Asteroid>();
    }

    static void SetAsteroidSplit(GameObject prefab, GameObject smallerPrefab)
    {
        var so = new SerializedObject(prefab.GetComponent<Asteroid>());
        so.FindProperty("smallerAsteroidPrefab").objectReferenceValue = smallerPrefab;
        so.ApplyModifiedProperties();
    }

    static GameObject SavePrefab(GameObject go, string path)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            var parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }

    static void AddTagIfMissing(string tag)
    {
        var tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]
        );
        var tags = tagManager.FindProperty("tags");

        for (int i = 0; i < tags.arraySize; i++)
        {
            if (tags.GetArrayElementAtIndex(i).stringValue == tag)
                return; // already exists
        }

        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedProperties();
    }

    static void CreateGameUI(GameManager gm)
    {
        // Canvas
        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();

        // Score Text
        var scoreGo = new GameObject("ScoreText");
        scoreGo.transform.SetParent(canvasGo.transform, false);
        var scoreRect = scoreGo.AddComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(0, 1);
        scoreRect.anchorMax = new Vector2(0, 1);
        scoreRect.pivot = new Vector2(0, 1);
        scoreRect.anchoredPosition = new Vector2(20, -20);
        scoreRect.sizeDelta = new Vector2(300, 40);
        var scoreText = scoreGo.AddComponent<TextMeshProUGUI>();
        scoreText.text = "Score: 0";
        scoreText.fontSize = 24;
        scoreText.color = Color.white;

        // Lives Text
        var livesGo = new GameObject("LivesText");
        livesGo.transform.SetParent(canvasGo.transform, false);
        var livesRect = livesGo.AddComponent<RectTransform>();
        livesRect.anchorMin = new Vector2(1, 1);
        livesRect.anchorMax = new Vector2(1, 1);
        livesRect.pivot = new Vector2(1, 1);
        livesRect.anchoredPosition = new Vector2(-20, -20);
        livesRect.sizeDelta = new Vector2(300, 40);
        var livesText = livesGo.AddComponent<TextMeshProUGUI>();
        livesText.text = "Lives: ♥ ♥ ♥";
        livesText.fontSize = 24;
        livesText.color = new Color(1f, 0.3f, 0.3f);
        livesText.alignment = TextAlignmentOptions.MidlineRight;

        // Game Over Panel
        var gameOverGo = new GameObject("GameOverPanel");
        gameOverGo.transform.SetParent(canvasGo.transform, false);
        var panelRect = gameOverGo.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        var panelImg = gameOverGo.AddComponent<Image>();
        panelImg.color = new Color(0, 0, 0, 0.7f);

        var finalScoreGo = new GameObject("FinalScoreText");
        finalScoreGo.transform.SetParent(gameOverGo.transform, false);
        var fsRect = finalScoreGo.AddComponent<RectTransform>();
        fsRect.anchorMin = new Vector2(0.5f, 0.5f);
        fsRect.anchorMax = new Vector2(0.5f, 0.5f);
        fsRect.sizeDelta = new Vector2(400, 60);
        fsRect.anchoredPosition = new Vector2(0, 30);
        var fsText = finalScoreGo.AddComponent<TextMeshProUGUI>();
        fsText.text = "GAME OVER";
        fsText.fontSize = 48;
        fsText.color = new Color(1f, 0.3f, 0.3f);
        fsText.alignment = TextAlignmentOptions.Center;

        // Restart Button
        var btnGo = new GameObject("RestartButton");
        btnGo.transform.SetParent(gameOverGo.transform, false);
        var btnRect = btnGo.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.sizeDelta = new Vector2(200, 50);
        btnRect.anchoredPosition = new Vector2(0, -40);
        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.6f, 1f);
        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = btnImg;

        var btnTextGo = new GameObject("Text");
        btnTextGo.transform.SetParent(btnGo.transform, false);
        var btnTextRect = btnTextGo.AddComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.sizeDelta = Vector2.zero;
        var btnText = btnTextGo.AddComponent<TextMeshProUGUI>();
        btnText.text = "Restart";
        btnText.fontSize = 24;
        btnText.color = Color.white;
        btnText.alignment = TextAlignmentOptions.Center;

        // GameUI script
        var uiGo = new GameObject("GameUI");
        var gameUI = uiGo.AddComponent<GameUI>();

        var uiSO = new SerializedObject(gameUI);
        uiSO.FindProperty("scoreText").objectReferenceValue = scoreText;
        uiSO.FindProperty("livesText").objectReferenceValue = livesText;
        uiSO.FindProperty("gameOverPanel").objectReferenceValue = gameOverGo;
        uiSO.FindProperty("finalScoreText").objectReferenceValue = fsText;
        uiSO.FindProperty("restartButton").objectReferenceValue = btn;
        uiSO.ApplyModifiedProperties();

        // ============================================
        //  Pause Menu
        // ============================================
        var pauseGo = new GameObject("PausePanel");
        pauseGo.transform.SetParent(canvasGo.transform, false);
        var pauseRect = pauseGo.AddComponent<RectTransform>();
        pauseRect.anchorMin = Vector2.zero;
        pauseRect.anchorMax = Vector2.one;
        pauseRect.sizeDelta = Vector2.zero;
        var pauseImg = pauseGo.AddComponent<Image>();
        pauseImg.color = new Color(0, 0, 0, 0.75f);

        // Pause title
        var pauseTitleGo = new GameObject("PauseTitle");
        pauseTitleGo.transform.SetParent(pauseGo.transform, false);
        var ptRect = pauseTitleGo.AddComponent<RectTransform>();
        ptRect.anchorMin = new Vector2(0.5f, 0.5f);
        ptRect.anchorMax = new Vector2(0.5f, 0.5f);
        ptRect.sizeDelta = new Vector2(400, 60);
        ptRect.anchoredPosition = new Vector2(0, 100);
        var ptText = pauseTitleGo.AddComponent<TextMeshProUGUI>();
        ptText.text = "PAUSED";
        ptText.fontSize = 48;
        ptText.color = new Color(0.4f, 0.8f, 1f);
        ptText.alignment = TextAlignmentOptions.Center;

        // Button container
        var btnContainer = new GameObject("ButtonContainer");
        btnContainer.transform.SetParent(pauseGo.transform, false);
        var bcRect = btnContainer.AddComponent<RectTransform>();
        bcRect.anchorMin = new Vector2(0.5f, 0.5f);
        bcRect.anchorMax = new Vector2(0.5f, 0.5f);
        bcRect.sizeDelta = new Vector2(250, 280);
        bcRect.anchoredPosition = new Vector2(0, -20);

        // Resume button
        var resumeBtn = CreatePauseButton(btnContainer.transform, "Resume", new Vector2(0, 60), new Color(0.2f, 0.7f, 0.3f));

        // Settings button
        var settingsBtn = CreatePauseButton(btnContainer.transform, "Settings", new Vector2(0, 0), new Color(0.3f, 0.5f, 0.8f));

        // Quit button
        var quitBtn = CreatePauseButton(btnContainer.transform, "Quit", new Vector2(0, -60), new Color(0.8f, 0.2f, 0.2f));

        // Shrink button container (was 280 for 4 buttons, now 220 for 3)
        bcRect.sizeDelta = new Vector2(250, 220);

        // ============================================
        //  Settings Panel (inside pause)
        // ============================================
        var settingsGo = new GameObject("SettingsPanel");
        settingsGo.transform.SetParent(canvasGo.transform, false);
        var sRect = settingsGo.AddComponent<RectTransform>();
        sRect.anchorMin = Vector2.zero;
        sRect.anchorMax = Vector2.one;
        sRect.sizeDelta = Vector2.zero;
        var sImg = settingsGo.AddComponent<Image>();
        sImg.color = new Color(0, 0, 0, 0.85f);

        // Settings title
        var sTitleGo = new GameObject("SettingsTitle");
        sTitleGo.transform.SetParent(settingsGo.transform, false);
        var stRect = sTitleGo.AddComponent<RectTransform>();
        stRect.anchorMin = new Vector2(0.5f, 0.5f);
        stRect.anchorMax = new Vector2(0.5f, 0.5f);
        stRect.sizeDelta = new Vector2(400, 60);
        stRect.anchoredPosition = new Vector2(0, 80);
        var stText = sTitleGo.AddComponent<TextMeshProUGUI>();
        stText.text = "SETTINGS";
        stText.fontSize = 40;
        stText.color = new Color(0.4f, 0.8f, 1f);
        stText.alignment = TextAlignmentOptions.Center;

        // Volume label
        var volLabelGo = new GameObject("VolumeLabel");
        volLabelGo.transform.SetParent(settingsGo.transform, false);
        var vlRect = volLabelGo.AddComponent<RectTransform>();
        vlRect.anchorMin = new Vector2(0.5f, 0.5f);
        vlRect.anchorMax = new Vector2(0.5f, 0.5f);
        vlRect.sizeDelta = new Vector2(200, 30);
        vlRect.anchoredPosition = new Vector2(-80, 20);
        var vlText = volLabelGo.AddComponent<TextMeshProUGUI>();
        vlText.text = "Volume";
        vlText.fontSize = 22;
        vlText.color = Color.white;
        vlText.alignment = TextAlignmentOptions.MidlineLeft;

        // Volume slider
        var sliderGo = new GameObject("VolumeSlider");
        sliderGo.transform.SetParent(settingsGo.transform, false);
        var slRect = sliderGo.AddComponent<RectTransform>();
        slRect.anchorMin = new Vector2(0.5f, 0.5f);
        slRect.anchorMax = new Vector2(0.5f, 0.5f);
        slRect.sizeDelta = new Vector2(180, 20);
        slRect.anchoredPosition = new Vector2(10, 20);
        var slider = sliderGo.AddComponent<Slider>();
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = AudioListener.volume;

        // Slider background
        var slBg = new GameObject("Background");
        slBg.transform.SetParent(sliderGo.transform, false);
        var slBgRect = slBg.AddComponent<RectTransform>();
        slBgRect.anchorMin = Vector2.zero;
        slBgRect.anchorMax = Vector2.one;
        slBgRect.sizeDelta = Vector2.zero;
        slBg.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f);

        // Slider fill area
        var slFillArea = new GameObject("Fill Area");
        slFillArea.transform.SetParent(sliderGo.transform, false);
        var slFillAreaRect = slFillArea.AddComponent<RectTransform>();
        slFillAreaRect.anchorMin = new Vector2(0, 0.25f);
        slFillAreaRect.anchorMax = new Vector2(1, 0.75f);
        slFillAreaRect.sizeDelta = Vector2.zero;

        // Slider fill
        var slFill = new GameObject("Fill");
        slFill.transform.SetParent(slFillArea.transform, false);
        var slFillRect = slFill.AddComponent<RectTransform>();
        slFillRect.anchorMin = Vector2.zero;
        slFillRect.anchorMax = Vector2.one;
        slFillRect.sizeDelta = Vector2.zero;
        slFill.AddComponent<Image>().color = new Color(0.4f, 0.8f, 1f);
        slider.fillRect = slFillRect;

        // Slider handle area
        var slHandleArea = new GameObject("Handle Slide Area");
        slHandleArea.transform.SetParent(sliderGo.transform, false);
        var slHandleAreaRect = slHandleArea.AddComponent<RectTransform>();
        slHandleAreaRect.anchorMin = Vector2.zero;
        slHandleAreaRect.anchorMax = Vector2.one;
        slHandleAreaRect.sizeDelta = Vector2.zero;

        // Slider handle
        var slHandle = new GameObject("Handle");
        slHandle.transform.SetParent(slHandleArea.transform, false);
        var slHandleRect = slHandle.AddComponent<RectTransform>();
        slHandleRect.sizeDelta = new Vector2(20, 20);
        slHandle.AddComponent<Image>().color = Color.white;
        slider.handleRect = slHandleRect;

        // Volume value label
        var volValGo = new GameObject("VolumeValue");
        volValGo.transform.SetParent(settingsGo.transform, false);
        var vvRect = volValGo.AddComponent<RectTransform>();
        vvRect.anchorMin = new Vector2(0.5f, 0.5f);
        vvRect.anchorMax = new Vector2(0.5f, 0.5f);
        vvRect.sizeDelta = new Vector2(60, 30);
        vvRect.anchoredPosition = new Vector2(155, 20);
        var vvText = volValGo.AddComponent<TextMeshProUGUI>();
        vvText.text = Mathf.RoundToInt(AudioListener.volume * 100) + "%";
        vvText.fontSize = 22;
        vvText.color = Color.white;
        vvText.alignment = TextAlignmentOptions.MidlineLeft;

        // Fullscreen toggle button in settings
        var fsLabelGo = new GameObject("FullscreenLabel");
        fsLabelGo.transform.SetParent(settingsGo.transform, false);
        var fsLabelRect = fsLabelGo.AddComponent<RectTransform>();
        fsLabelRect.anchorMin = new Vector2(0.5f, 0.5f);
        fsLabelRect.anchorMax = new Vector2(0.5f, 0.5f);
        fsLabelRect.sizeDelta = new Vector2(200, 30);
        fsLabelRect.anchoredPosition = new Vector2(-80, -35);
        var fsLabelText = fsLabelGo.AddComponent<TextMeshProUGUI>();
        fsLabelText.text = "Fullscreen";
        fsLabelText.fontSize = 22;
        fsLabelText.color = Color.white;
        fsLabelText.alignment = TextAlignmentOptions.MidlineLeft;

        var fsBtn = CreatePauseButton(settingsGo.transform, "Off", new Vector2(80, -35), new Color(0.3f, 0.5f, 0.8f));
        fsBtn.gameObject.name = "FullscreenButton";
        var fsBtnText = fsBtn.GetComponentInChildren<TextMeshProUGUI>();
        if (fsBtnText != null)
        {
            var cjkFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/NotoSansSC SDF.asset");
            if (cjkFont != null) fsBtnText.font = cjkFont;
        }

        // Language toggle button in settings
        var langBtn = CreatePauseButton(settingsGo.transform, "中文/EN", new Vector2(0, -85), new Color(0.6f, 0.4f, 0.8f));
        langBtn.gameObject.name = "LangButton";
        var langBtnText = langBtn.GetComponentInChildren<TextMeshProUGUI>();
        if (langBtnText != null)
        {
            var cjkFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/NotoSansSC SDF.asset");
            if (cjkFont != null) langBtnText.font = cjkFont;
        }

        // Back button in settings
        var backBtn = CreatePauseButton(settingsGo.transform, "Back", new Vector2(0, -135), new Color(0.5f, 0.5f, 0.5f));

        // PauseMenu script
        var pauseMenuGo = new GameObject("PauseMenu");
        var pauseMenu = pauseMenuGo.AddComponent<PauseMenu>();

        // Find all text references
        // Note: buttons are inside ButtonContainer, so paths must include it
        var pauseTitleText = pauseGo.transform.Find("PauseTitle")?.GetComponent<TMPro.TMP_Text>();
        var resumeText = pauseGo.transform.Find("ButtonContainer/ResumeButton/Text")?.GetComponent<TMPro.TMP_Text>();
        var settingsText = pauseGo.transform.Find("ButtonContainer/SettingsButton/Text")?.GetComponent<TMPro.TMP_Text>();
        var quitText = pauseGo.transform.Find("ButtonContainer/QuitButton/Text")?.GetComponent<TMPro.TMP_Text>();
        var settingsTitleText = settingsGo.transform.Find("SettingsTitle")?.GetComponent<TMPro.TMP_Text>();
        var backText = settingsGo.transform.Find("BackButton/Text")?.GetComponent<TMPro.TMP_Text>();
        var langButtonText = settingsGo.transform.Find("LangButton/Text")?.GetComponent<TMPro.TMP_Text>();
        var fullscreenLabelText = settingsGo.transform.Find("FullscreenLabel")?.GetComponent<TMPro.TMP_Text>();
        var fullscreenButtonText = settingsGo.transform.Find("FullscreenButton/Text")?.GetComponent<TMPro.TMP_Text>();

        var pmSO = new SerializedObject(pauseMenu);
        pmSO.FindProperty("pausePanel").objectReferenceValue = pauseGo;
        pmSO.FindProperty("settingsPanel").objectReferenceValue = settingsGo;
        pmSO.FindProperty("volumeSlider").objectReferenceValue = slider;
        pmSO.FindProperty("volumeLabelText").objectReferenceValue = vlText;
        pmSO.FindProperty("volumeValueText").objectReferenceValue = vvText;
        pmSO.FindProperty("pauseTitleText").objectReferenceValue = pauseTitleText;
        pmSO.FindProperty("resumeText").objectReferenceValue = resumeText;
        pmSO.FindProperty("settingsText").objectReferenceValue = settingsText;
        pmSO.FindProperty("quitText").objectReferenceValue = quitText;
        pmSO.FindProperty("settingsTitleText").objectReferenceValue = settingsTitleText;
        pmSO.FindProperty("backText").objectReferenceValue = backText;
        pmSO.FindProperty("langButtonText").objectReferenceValue = langButtonText;
        pmSO.FindProperty("fullscreenLabelText").objectReferenceValue = fullscreenLabelText;
        pmSO.FindProperty("fullscreenButtonText").objectReferenceValue = fullscreenButtonText;
        pmSO.ApplyModifiedProperties();

        slider.onValueChanged.AddListener(v => { vvText.text = Mathf.RoundToInt(v * 100) + "%"; });

        // EventSystem
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    static UnityEngine.UI.Button CreatePauseButton(Transform parent, string text, Vector2 pos, Color color)
    {
        var btnGo = new GameObject(text + "Button");
        btnGo.transform.SetParent(parent, false);
        var btnRect = btnGo.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.sizeDelta = new Vector2(220, 45);
        btnRect.anchoredPosition = pos;
        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = color;
        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = btnImg;

        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(btnGo.transform, false);
        var txtRect = txtGo.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;
        var txt = txtGo.AddComponent<TextMeshProUGUI>();
        txt.text = text;
        txt.fontSize = 22;
        txt.color = Color.white;
        txt.alignment = TextAlignmentOptions.Center;
        return btn;
    }
}
#endif

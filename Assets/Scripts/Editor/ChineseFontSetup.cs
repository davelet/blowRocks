#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;
using TMPro.EditorUtilities;
using System.IO;
using System.Net;

/// <summary>
/// Editor tool: creates a Chinese (CJK) font asset and adds it as a TMP fallback.
/// Called by BlowRocksSetup.cs, no separate menu needed.
/// </summary>
public static class ChineseFontSetup
{
    private const string CJKFontPath = "Assets/Fonts/NotoSansSC-Regular.ttf";
    private const string CJKFontAssetPath = "Assets/Fonts/NotoSansSC SDF.asset";
    private const string FallbackAssetPath = "Assets/Fonts/NotoSansSC Fallback.asset";

    public static void SetupChineseFont()
    {
        // Step 1: Get the TTF font file
        if (!EnsureCJKFont())
        {
            Debug.LogWarning("[ChineseFont] Could not find or download a Chinese font.");
            return;
        }

        // Step 2: Create TMP font asset
        CreateTMPFontAsset();

        // Step 3: Set as default TMP font fallback
        SetAsFallback();

        AssetDatabase.Refresh();
        Debug.Log("[ChineseFont] Chinese font setup complete.");
    }

    static bool EnsureCJKFont()
    {
        // Already exists?
        if (File.Exists(CJKFontPath)) return true;

        EnsureFolder("Assets/Fonts");

        // Try system fonts (macOS)
        string[] macPaths = new[] {
            "/System/Library/Fonts/PingFang.ttc",
            "/System/Library/Fonts/STHeiti Medium.ttc",
            "/System/Library/Fonts/Hiragino Sans GB.ttc",
            "/Library/Fonts/Arial Unicode.ttf",
        };

        foreach (var sysPath in macPaths)
        {
            if (File.Exists(sysPath))
            {
                File.Copy(sysPath, CJKFontPath, true);
                AssetDatabase.ImportAsset(CJKFontPath, ImportAssetOptions.ForceUpdate);
                Debug.Log($"[ChineseFont] Copied system font from: {sysPath}");
                return true;
            }
        }

        // Try downloading Noto Sans SC (Google Fonts, open-source)
        Debug.Log("[ChineseFont] No system CJK font found, attempting download...");
        try
        {
            string url = "https://github.com/google/fonts/raw/main/ofl/notosanssc/NotoSansSC%5Bwght%5D.ttf";
            using (var client = new WebClient())
            {
                client.DownloadFile(url, CJKFontPath);
            }
            AssetDatabase.ImportAsset(CJKFontPath, ImportAssetOptions.ForceUpdate);
            Debug.Log("[ChineseFont] Downloaded Noto Sans SC from Google Fonts.");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[ChineseFont] Download failed: {ex.Message}");
        }

        return false;
    }

    static void CreateTMPFontAsset()
    {
        // Load the source font
        var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(CJKFontPath);
        if (sourceFont == null)
        {
            Debug.LogError("[ChineseFont] Could not load font at: " + CJKFontPath);
            return;
        }

        // Force delete existing assets to regenerate cleanly
        if (File.Exists(CJKFontAssetPath))
        {
            AssetDatabase.DeleteAsset(CJKFontAssetPath);
            Debug.Log("[ChineseFont] Deleted old font asset for regeneration.");
        }
        if (File.Exists(FallbackAssetPath))
        {
            AssetDatabase.DeleteAsset(FallbackAssetPath);
            Debug.Log("[ChineseFont] Deleted old fallback asset for regeneration.");
        }
        AssetDatabase.Refresh();

        // Create the font asset using TMP's built-in method
        var fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont);

        if (fontAsset == null)
        {
            Debug.LogError("[ChineseFont] Failed to create TMP_FontAsset.");
            return;
        }

        // Configure the font asset
        fontAsset.material.SetFloat(ShaderUtilities.ID_GradientScale, 9f);

        // Save main font asset first
        AssetDatabase.CreateAsset(fontAsset, CJKFontAssetPath);

        // Save material and atlas texture as sub-assets so they get included in builds
        if (fontAsset.material != null)
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);

        if (fontAsset.atlasTextures != null)
        {
            for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
            {
                if (fontAsset.atlasTextures[i] != null)
                    AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[i], fontAsset);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[ChineseFont] Created TMP font asset at: " + CJKFontAssetPath);

        // Also create a fallback asset
        CreateFallbackAsset(sourceFont);
    }

    static void CreateFallbackAsset(Font sourceFont)
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackAssetPath);
        if (existing != null) return;

        // Create a smaller fallback with common CJK characters
        var fallbackAsset = TMP_FontAsset.CreateFontAsset(sourceFont);
        if (fallbackAsset == null) return;

        AssetDatabase.CreateAsset(fallbackAsset, FallbackAssetPath);

        // Save material and atlas texture as sub-assets
        if (fallbackAsset.material != null)
            AssetDatabase.AddObjectToAsset(fallbackAsset.material, fallbackAsset);

        if (fallbackAsset.atlasTextures != null)
        {
            for (int i = 0; i < fallbackAsset.atlasTextures.Length; i++)
            {
                if (fallbackAsset.atlasTextures[i] != null)
                    AssetDatabase.AddObjectToAsset(fallbackAsset.atlasTextures[i], fallbackAsset);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[ChineseFont] Created fallback font asset.");
    }

    static void SetAsFallback()
    {
        // defaultFontAsset is a static property on TMP_Settings
        var defaultFont = TMP_Settings.defaultFontAsset;
        if (defaultFont == null)
        {
            Debug.LogWarning("[ChineseFont] No default font asset configured in TMP Settings.");
            return;
        }

        var cjkFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CJKFontAssetPath);
        if (cjkFont == null) return;

        // Check if already in fallbacks
        var fallbacks = defaultFont.fallbackFontAssetTable;
        if (fallbacks != null && fallbacks.Contains(cjkFont))
        {
            Debug.Log("[ChineseFont] CJK font already in fallback list.");
            return;
        }

        // Add as fallback
        if (fallbacks == null)
            defaultFont.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { cjkFont };
        else
            fallbacks.Add(cjkFont);

        EditorUtility.SetDirty(defaultFont);
        AssetDatabase.SaveAssets();
        Debug.Log("[ChineseFont] Added CJK font as fallback to default TMP font.");
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
}
#endif
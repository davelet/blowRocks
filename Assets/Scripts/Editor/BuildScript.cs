#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using Debug = UnityEngine.Debug;

public class BuildScript
{
    [MenuItem("blowRocks/Build macOS")]
    public static void BuildMacOS()
    {
        string buildPath = "Builds/blowRocks.app";

        // Ensure output directory exists
        Directory.CreateDirectory("Builds");

        // Set app icon in PlayerSettings (fallback)
        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/AppIcon.png");
        if (icon != null)
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone, new[] { icon });

        // Get all enabled scenes
        var scenes = new System.Collections.Generic.List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
                scenes.Add(scene.path);
        }

        if (scenes.Count == 0)
        {
            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(activeScene.path))
                scenes.Add(activeScene.path);
            else
                scenes.Add("Assets/Scenes/MainScene.unity");
        }

        Debug.Log("Building to: " + Path.GetFullPath(buildPath));
        Debug.Log("Scenes: " + string.Join(", ", scenes));

        var options = new BuildPlayerOptions
        {
            scenes = scenes.ToArray(),
            locationPathName = buildPath,
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("Build succeeded! Size: " + (report.summary.totalSize / 1024 / 1024) + " MB");

            // Inject macOS icon into .app bundle
            InjectMacOSIcon(buildPath);

            EditorUtility.DisplayDialog("blowRocks", "Build succeeded!\n\n" + Path.GetFullPath(buildPath), "OK");
            EditorUtility.RevealInFinder(Path.GetFullPath(buildPath));
        }
        else
        {
            Debug.LogError("Build failed: " + report.summary.result);
            EditorUtility.DisplayDialog("blowRocks", "Build failed! Check Console for errors.", "OK");
        }
    }

    static void InjectMacOSIcon(string appPath)
    {
        string srcPng = Path.GetFullPath("Assets/Sprites/AppIcon.png");
        if (!File.Exists(srcPng))
        {
            UnityEngine.Debug.LogWarning("AppIcon.png not found, skipping icon injection.");
            return;
        }

        string iconsetDir = Path.Combine(Path.GetTempPath(), "blowRocks_icon.iconset");
        string icnsPath = Path.Combine(Path.GetTempPath(), "blowRocks.icns");

        try
        {
            // Create iconset directory
            if (Directory.Exists(iconsetDir))
                Directory.Delete(iconsetDir, true);
            Directory.CreateDirectory(iconsetDir);

            // Generate all required icon sizes using sips
            int[] sizes = { 16, 32, 128, 256, 512 };
            foreach (int s in sizes)
            {
                RunProcess("sips", $"-z {s} {s} \"{srcPng}\" --out \"{iconsetDir}/icon_{s}x{s}.png\"");
                RunProcess("sips", $"-z {s * 2} {s * 2} \"{srcPng}\" --out \"{iconsetDir}/icon_{s}x{s}@2x.png\"");
            }

            // Convert iconset to icns
            RunProcess("iconutil", $"-c icns \"{iconsetDir}\" -o \"{icnsPath}\"");

            if (!File.Exists(icnsPath))
            {
                UnityEngine.Debug.LogError("Failed to generate .icns file.");
                return;
            }

            // Copy icns into app bundle
            string resourcesDir = Path.Combine(appPath, "Contents", "Resources");
            Directory.CreateDirectory(resourcesDir);

            // Find the product name from Info.plist or use default
            string productName = PlayerSettings.productName;
            string destIcns = Path.Combine(resourcesDir, productName + ".icns");
            File.Copy(icnsPath, destIcns, true);

            // Update Info.plist to reference the icon
            string infoPlist = Path.Combine(appPath, "Contents", "Info.plist");
            if (File.Exists(infoPlist))
            {
                string plist = File.ReadAllText(infoPlist);
                string iconKey = "<key>CFBundleIconFile</key>";
                string iconValue = $"<string>{productName}.icns</string>";

                if (plist.Contains("CFBundleIconFile"))
                {
                    // Replace existing icon entry
                    plist = System.Text.RegularExpressions.Regex.Replace(
                        plist,
                        @"<key>CFBundleIconFile</key>\s*<string>[^<]*</string>",
                        $"{iconKey}\n\t{iconValue}");
                }
                else
                {
                    // Insert before closing </dict>
                    plist = plist.Replace("</dict>", $"\t{iconKey}\n\t{iconValue}\n</dict>");
                }
                File.WriteAllText(infoPlist, plist);
            }

            UnityEngine.Debug.Log("macOS icon injected: " + destIcns);
        }
        finally
        {
            // Cleanup temp files
            try { if (Directory.Exists(iconsetDir)) Directory.Delete(iconsetDir, true); } catch { }
            try { if (File.Exists(icnsPath)) File.Delete(icnsPath); } catch { }
        }
    }

    static void RunProcess(string fileName, string arguments)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true
        };
        using (var proc = System.Diagnostics.Process.Start(psi))
        {
            proc.WaitForExit();
            if (proc.ExitCode != 0)
                UnityEngine.Debug.LogWarning($"{fileName} failed: {proc.StandardError.ReadToEnd()}");
        }
    }
}
#endif

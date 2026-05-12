#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public class BuildScript
{
    [MenuItem("blowRocks/Build macOS")]
    public static void BuildMacOS()
    {
        string buildPath = "Builds/blowRocks.app";

        // Ensure output directory exists
        Directory.CreateDirectory("Builds");

        // Get all enabled scenes
        var scenes = new System.Collections.Generic.List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
                scenes.Add(scene.path);
        }

        // If no scenes in build settings, use the current scene
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
            EditorUtility.DisplayDialog("blowRocks", "Build succeeded!\n\n" + Path.GetFullPath(buildPath), "OK");
            // Open the build folder
            EditorUtility.RevealInFinder(Path.GetFullPath(buildPath));
        }
        else
        {
            Debug.LogError("Build failed: " + report.summary.result);
            EditorUtility.DisplayDialog("blowRocks", "Build failed! Check Console for errors.", "OK");
        }
    }
}
#endif

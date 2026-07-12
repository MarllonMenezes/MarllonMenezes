#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AlbaWorld.Editor;

public static class BuildTools
{
    [MenuItem("Alba World/Build Android AAB")]
    public static void BuildAndroidAab()
    {
        ProjectSetup.EnsureDemoScene();
        Directory.CreateDirectory("Builds");
        EditorUserBuildSettings.buildAppBundle = true;
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Main.unity" },
            locationPathName = "Builds/AlbaWorld.aab",
            target = BuildTarget.Android,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException($"Android build failed: {report.summary.result}");
        Debug.Log($"AAB created at {Path.GetFullPath("Builds/AlbaWorld.aab")}");
    }
}
#endif

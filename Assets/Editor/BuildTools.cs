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
        BuildAndroidPackage(appBundle: true, path: "Builds/AlbaWorld.aab", options: BuildOptions.None);
    }

    [MenuItem("Alba World/Build Android APK (local test)")]
    public static void BuildAndroidApk()
    {
        BuildAndroidPackage(appBundle: false, path: "Builds/AlbaWorld.apk", options: BuildOptions.Development);
    }

    private static void BuildAndroidPackage(bool appBundle, string path, BuildOptions options)
    {
        ProjectSetup.EnsureDemoScene();
        Directory.CreateDirectory("Builds");
        EditorUserBuildSettings.buildAppBundle = appBundle;
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Main.unity" },
            locationPathName = path,
            target = BuildTarget.Android,
            options = options
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException($"Android build failed: {report.summary.result}");
        Debug.Log($"Android package created at {Path.GetFullPath(path)}");
    }
}
#endif

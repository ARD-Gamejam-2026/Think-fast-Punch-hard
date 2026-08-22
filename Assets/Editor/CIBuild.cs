using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Command-line entry point for CI builds, e.g.:
/// Unity -batchmode -quit -buildTarget WebGL -executeMethod CIBuild.BuildWebGL
/// </summary>
public static class CIBuild
{
    public static void BuildWebGL()
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            // Same output layout as game-ci/unity-builder, so the publish
            // job works identically with either build path.
            locationPathName = "build/WebGL/WebGL",
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        });

        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"WebGL build failed: {report.summary.result}");
            EditorApplication.Exit(1);
        }
    }
}

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FarmASU.Editor
{
    public static class BuildScript
    {
        [MenuItem("FarmASU/Build Windows Development Player")]
        public static void BuildWindowsDevelopmentPlayer()
        {
            string buildPath = "Builds/Windows/FarmASU.exe";
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Main/MainScene.unity" },
                locationPathName = buildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };

            Debug.Log($"[BuildScript] Starting Windows Build to {buildPath}...");
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildScript] Build Succeeded! Size: {summary.totalSize} bytes in {summary.totalTime.TotalSeconds:F1} seconds.");
            }
            else if (summary.result == BuildResult.Failed)
            {
                Debug.LogError($"[BuildScript] Build Failed with {summary.totalErrors} errors.");
            }
        }
    }
}
#endif

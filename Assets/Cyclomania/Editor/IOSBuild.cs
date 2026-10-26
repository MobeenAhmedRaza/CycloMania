using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Cyclomania.Editor
{
    public static class IOSBuild
    {
        public const string GameplayScene = "Assets/Scenes/gameplay.unity";

        [MenuItem("Cyclomania/iOS/Export Xcode Project")]
        public static void Export()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
                throw new BuildFailedException("Install iOS Build Support for this Unity version through Unity Hub, then retry.");

            string output = EditorUtility.SaveFolderPanel("Export Cyclomania Xcode project", "", "Cyclomania-iOS");
            if (string.IsNullOrEmpty(output)) return;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { GameplayScene },
                locationPathName = output,
                target = BuildTarget.iOS,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("iOS export failed: " + report.summary.result);
            Debug.Log("Cyclomania Xcode project exported to " + output);
        }
    }
}

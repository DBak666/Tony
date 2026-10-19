using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
public static class GMBuildValidation
{
    public static void Build()
    {
        var result = BuildPipeline.BuildPlayer(new[] { "Assets/Scenes/Main.unity" }, "Builds/GMValidation/AmazeGO.exe", BuildTarget.StandaloneWindows64, BuildOptions.Development);
        if (result.summary.result != BuildResult.Succeeded) throw new Exception("GM validation build failed");
    }
}

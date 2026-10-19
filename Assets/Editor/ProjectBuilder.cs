using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using AmazeRebuild;

public static class ProjectBuilder
{
    [MenuItem("Amaze GO/Create or Open Main Scene")]
    public static void CreateScene()
    {
        Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera", typeof(Camera)); camera.tag = "MainCamera";
        camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
        camera.GetComponent<Camera>().backgroundColor = new Color32(31, 36, 43, 255);
        camera.AddComponent<AudioListener>();
        new GameObject("Amaze GO - Game", typeof(AmazeGame));
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true) };
        PlayerSettings.companyName = "LocalStudy"; PlayerSettings.productName = "Amaze GO Rebuild";
        PlayerSettings.defaultScreenWidth = 750; PlayerSettings.defaultScreenHeight = 1334;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed; PlayerSettings.resizableWindow = true;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.colorSpace = ColorSpace.Gamma;
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.localstudy.amazerebuild");
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
        PlayerSettings.runInBackground = true;
        AssetDatabase.SaveAssets();
    }
    [MenuItem("Amaze GO/Validate All Levels")]
    public static void Validate()
    {
        var catalog = JsonUtility.FromJson<LevelCatalog>(Resources.Load<TextAsset>("catalog").text);
        int passed = 0, moves = 0;
        foreach (var entry in catalog.levels)
        {
            var data = LevelData.Load(entry.number); var model = new BoardModel(data);
            foreach (int index in data.Solution)
            {
                if (!model.TryRemove(index)) throw new Exception("Solution failed on level " + entry.number + ", arrow " + index);
                moves++;
            }
            if (model.Remaining != 0) throw new Exception("Unsolved level " + entry.number);
            passed++;
        }
        var test = new LevelData { XSize = 5, YSize = 5, Arrows = new[] { new ArrowData { Points = new[] { 12, 11 } }, new ArrowData { Points = new[] { 18, 8 } } } };
        var board = new BoardModel(test);
        if (board.CanExit(0) || board.TryRemove(0)) throw new Exception("Expected interior segment blocking");
        if (!board.TryRemove(1) || !board.CanExit(0) || !board.TryRemove(0)) throw new Exception("Dependency removal failed");
        if (board.TryRemove(0) || board.Remaining != 0) throw new Exception("Duplicate removal failed");
        bool invalidRejected = false;
        try { BoardModel.Expand(new[] { 0, 6 }, 5, 5); } catch (ArgumentException) { invalidRejected = true; }
        if (!invalidRejected) throw new Exception("Diagonal segment accepted");
        Directory.CreateDirectory("QA");
        File.WriteAllText("QA/level-validation.txt", "PASS: " + passed + " levels; " + moves + " valid moves; segment occupancy; blocked move; dependency removal; duplicate removal; invalid data rejection.\n");
        Debug.Log("VALIDATION PASS: " + passed + " levels; " + moves + " moves");
    }
    public static void BuildWindows()
    {
        CreateScene(); Validate(); Directory.CreateDirectory("Builds");
        var result = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, "Builds/AmazeGO.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
        if (result.summary.result != BuildResult.Succeeded) throw new Exception("Build failed: " + result.summary.result);
        Debug.Log("BUILD PASS");
    }
}

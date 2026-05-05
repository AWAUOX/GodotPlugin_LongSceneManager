using Godot;
using System;
using LongSceneManagerCs;

public partial class TestScene1Cs : Node2D
{
    private const string MAIN_SCENE_PATH = "res://demo_test_scene_manager/main_scene.tscn";
    private const string TEST_SCENE_2_PATH = "res://demo_test_scene_manager/test_scene_2.tscn";

    private Button _buttonMain;
    private Button _buttonScene2;
    private Button _buttonBack;
    private Label _labelInfo;

    private bool _isFirstEnter = true;

    public override void _Ready()
    {
        GD.Print("=== Test Scene 1 Loaded (C#) 测试场景1加载完成 (C#) ===");

        _buttonMain = GetNode<Button>("MarginContainer/VBoxContainer/HBoxContainer/VBoxContainer/Button_Main");
        _buttonScene2 = GetNode<Button>("MarginContainer/VBoxContainer/HBoxContainer/VBoxContainer/Button_Scene2");
        _buttonBack = GetNode<Button>("MarginContainer/VBoxContainer/HBoxContainer/VBoxContainer/Button_Back");
        _labelInfo = GetNode<Label>("MarginContainer/VBoxContainer/HBoxContainer/Label_Info");

        _buttonMain.Pressed += OnMainPressed;
        _buttonScene2.Pressed += OnScene2Pressed;
        _buttonBack.Pressed += OnBackPressed;

        UpdateInfoLabel();
        _isFirstEnter = false;

        LongSceneManagerCs.Instance.SceneSwitchStarted += OnSceneSwitchStarted;
        LongSceneManagerCs.Instance.SceneSwitchCompleted += OnSceneSwitchCompleted;
    }

    public override void _EnterTree()
    {
        if (!_isFirstEnter)
        {
            UpdateInfoLabel();
        }
    }

    public override void _Process(double delta)
    {
        UpdateInfoLabel();
    }

    private void UpdateInfoLabel()
    {
        var cacheInfo = LongSceneManagerCs.Instance.GetCacheInfo();

        var instanceCache = (Godot.Collections.Dictionary)cacheInfo["instance_cache"];
        var tempPreloadCache = (Godot.Collections.Dictionary)cacheInfo["temp_preload_cache"];
        var fixedPreloadCache = (Godot.Collections.Dictionary)cacheInfo["fixed_preload_cache"];
        var preloadStates = (Godot.Collections.Dictionary)cacheInfo["preload_states"];

        var cachedScenes = (Godot.Collections.Array<Godot.Collections.Dictionary>)instanceCache["scenes"];
        var instancePaths = new System.Collections.Generic.List<string>();
        foreach (var s in cachedScenes)
        {
            instancePaths.Add((string)s["path"]);
        }
        var instanceList = instancePaths.Count > 0 ? string.Join("\n", instancePaths) : "（empty）";

        var tempScenes = (Godot.Collections.Array<string>)tempPreloadCache["scenes"];
        var tempList = tempScenes.Count > 0 ? string.Join("\n", tempScenes) : "（empty）";

        var fixedScenes = (Godot.Collections.Array<string>)fixedPreloadCache["scenes"];
        var fixedList = fixedScenes.Count > 0 ? string.Join("\n", fixedScenes) : "（empty）";

        var states = (Godot.Collections.Array<Godot.Collections.Dictionary>)preloadStates["states"];
        var statesList = new System.Collections.Generic.List<string>();
        foreach (var s in states)
        {
            statesList.Add($"{s["path"]} [{(int)s["state"]}]");
        }
        var preloadStatesStr = statesList.Count > 0 ? string.Join("\n", statesList) : "（empty）";

        _labelInfo.Text = $@"
Current Scene: {(string)cacheInfo["current_scene"]}
Previous Scene: {(string)cacheInfo["previous_scene"]}

[Instance Scene Cache] Count: {instanceCache["size"]}/{instanceCache["max_size"]}
Scene List:
{instanceList}

[Temporary Preload Cache] Count: {tempPreloadCache["size"]}/{tempPreloadCache["max_size"]}
Resource List:
{tempList}

[Fixed Preload Cache] Count: {fixedPreloadCache["size"]}/{fixedPreloadCache["max_size"]}
Resource List:
{fixedList}

[Preload States] Count: {preloadStates["size"]}
States:
{preloadStatesStr}
";
    }

    private void OnMainPressed()
    {
        GD.Print("Switch back to main scene 切换回主场景");
        LongSceneManagerCs.Instance.SwitchScene(MAIN_SCENE_PATH, LongSceneManagerCs.LoadMethod.BothPreloadFirst, true, "");
    }

    private void OnScene2Pressed()
    {
        GD.Print("Switch to scene 2 切换到场景2");
        LongSceneManagerCs.Instance.SwitchScene(TEST_SCENE_2_PATH, LongSceneManagerCs.LoadMethod.BothPreloadFirst, true, "");
    }

    private void OnBackPressed()
    {
        GD.Print("Back to main scene (no transition) 返回主场景（无过渡效果）");
        LongSceneManagerCs.Instance.SwitchScene(MAIN_SCENE_PATH, LongSceneManagerCs.LoadMethod.BothPreloadFirst, true, "no_transition");
    }

    private void OnSceneSwitchStarted(string fromScene, string toScene)
    {
        GD.Print($"Scene 1 - switch started (C#) 场景1 - 切换开始 (C#): {fromScene} -> {toScene}");
    }

    private void OnSceneSwitchCompleted(string scenePath)
    {
        GD.Print($"Scene 1 - switch completed (C#) 场景1 - 切换完成 (C#): {scenePath}");
    }
}
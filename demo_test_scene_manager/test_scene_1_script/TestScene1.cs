using Godot;
using System;

public partial class TestScene1 : Node2D
{
	private const string MAIN_SCENE_PATH = "res://demo_test_scene_manager/main_scene.tscn";
	private const string TEST_SCENE_2_PATH = "res://demo_test_scene_manager/test_scene_2.tscn";

	[Export]
	private Button _buttonMain;
	[Export]
	private Button _buttonScene2;
	[Export]
	private Button _buttonBack;
	[Export]
	private Label _labelInfo;

	private bool _isFirstEnter = true;

	public override void _Ready()
	{
		GD.Print("=== Test Scene 1 Loaded 测试场景1加载完成 ===");

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
		if (!GodotObject.IsInstanceValid(this) || !IsInsideTree() || _labelInfo == null || !GodotObject.IsInstanceValid(_labelInfo))
		{
			return;
		}

		var cacheInfo = LongSceneManagerCs.Instance.GetCacheInfo();

		var instanceCacheDict = (Godot.Collections.Dictionary)cacheInfo["instance_cache"];
		var tempPreloadCacheDict = (Godot.Collections.Dictionary)cacheInfo["temp_preload_cache"];
		var fixedPreloadCacheDict = (Godot.Collections.Dictionary)cacheInfo["fixed_preload_cache"];
		var preloadStatesDict = (Godot.Collections.Dictionary)cacheInfo["preload_states"];

		var instancePaths = new Godot.Collections.Array();
		foreach (var s in (Godot.Collections.Array)instanceCacheDict["scenes"])
		{
			var dict = (Godot.Collections.Dictionary)s;
			instancePaths.Add(dict["path"]);
		}
		string instanceList = string.Join("\n", instancePaths) ?? "（empty）";

		string preloadList = string.Join("\n", (Godot.Collections.Array)tempPreloadCacheDict["scenes"]) ?? "（empty）";

		string permanentPreloadList = string.Join("\n", (Godot.Collections.Array)fixedPreloadCacheDict["scenes"]) ?? "（empty）";

		var preloadStatesList = new Godot.Collections.Array();
		foreach (var s in (Godot.Collections.Array)preloadStatesDict["states"])
		{
			var dict = (Godot.Collections.Dictionary)s;
			preloadStatesList.Add($"{dict["path"]} [{dict["state"]}]");
		}
		string preloadStatesStr = string.Join("\n", preloadStatesList) ?? "（empty）";

		_labelInfo.Text = $"""
Current Scene: {cacheInfo["current_scene"]}
Previous Scene: {cacheInfo["previous_scene"]}

[Instance Scene Cache] Count: {instanceCacheDict["size"]}/{instanceCacheDict["max_size"]}
Scene List:
{instanceList}

[Temporary Preload Cache] Count: {tempPreloadCacheDict["size"]}/{tempPreloadCacheDict["max_size"]}
Resource List:
{preloadList}

[Fixed Preload Cache] Count: {fixedPreloadCacheDict["size"]}/{fixedPreloadCacheDict["max_size"]}
Resource List:
{permanentPreloadList}

[Preload States] Count: {preloadStatesDict["size"]}
States:
{preloadStatesStr}
""";
	}

	private async void OnMainPressed()
	{
		GD.Print("Switch back to main scene 切换回主场景");
		await LongSceneManagerCs.Instance.SwitchScene(MAIN_SCENE_PATH, LongSceneManagerCs.LoadMethod.BothPreloadFirst, true, "");
	}

	private async void OnScene2Pressed()
	{
		GD.Print("Switch to scene 2 切换到场景2");
		await LongSceneManagerCs.Instance.SwitchScene(TEST_SCENE_2_PATH, LongSceneManagerCs.LoadMethod.BothPreloadFirst, true, "");
	}

	private async void OnBackPressed()
	{
		GD.Print("Back to main scene (no transition) 返回主场景（无过渡效果）");
		await LongSceneManagerCs.Instance.SwitchScene(MAIN_SCENE_PATH, LongSceneManagerCs.LoadMethod.BothPreloadFirst, true, "no_transition");
	}

	private void OnSceneSwitchStarted(string fromScene, string toScene)
	{
		GD.Print($"Scene 1 - switch started 场景1 - 切换开始: {fromScene} -> {toScene}");
	}

	private void OnSceneSwitchCompleted(string scenePath)
	{
		GD.Print($"Scene 1 - switch completed 场景1 - 切换完成: {scenePath}");
	}
}

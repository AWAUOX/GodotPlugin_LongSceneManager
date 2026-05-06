using Godot;
using System;

public partial class MainScene : Control
{
	private const string TEST_SCENE_1_PATH = "res://demo_test_scene_manager/test_scene_1.tscn";
	private const string TEST_SCENE_2_PATH = "res://demo_test_scene_manager/test_scene_2.tscn";

	[Export]
	private Button _buttonScene1;
	[Export]
	private Button _buttonScene2;
	[Export]
	private Button _buttonPreload1;
	[Export]
	private Button _buttonPreload2;
	[Export]
	private Button _buttonClearCache;
	[Export]
	private Label _labelInfo;

	private bool _isFirstEnter = true;

	public override void _Ready()
	{
		GD.Print("=== Main Scene Loaded 主场景加载完成 ===");

		_buttonScene1.Pressed += OnScene1Pressed;
		_buttonScene2.Pressed += OnScene2Pressed;
		_buttonPreload1.Pressed += OnPreload1Pressed;
		_buttonPreload2.Pressed += OnPreload2Pressed;
		_buttonClearCache.Pressed += OnClearAllCachePressed;

		UpdateInfoLabel();
		_isFirstEnter = false;

		LongSceneManagerCs.Instance.SceneSwitchStarted += OnSceneSwitchStarted;
		LongSceneManagerCs.Instance.SceneSwitchCompleted += OnSceneSwitchCompleted;
		LongSceneManagerCs.Instance.SceneCached += OnSceneCached;
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

	private async void OnScene1Pressed()
	{
		GD.Print("Switch to scene 1 切换到场景1");
		await LongSceneManagerCs.Instance.SwitchScene(TEST_SCENE_1_PATH, LongSceneManagerCs.LoadMethod.PreloadCache, true, "");
	}

	private async void OnScene2Pressed()
	{
		GD.Print("Switch to scene 2 切换到场景2");
		await LongSceneManagerCs.Instance.SwitchScene(TEST_SCENE_2_PATH, LongSceneManagerCs.LoadMethod.PreloadCache, true, "");
	}

	private void OnPreload1Pressed()
	{
		GD.Print("Preload scene 1 预加载场景1");
		LongSceneManagerCs.Instance.PreloadScene(TEST_SCENE_1_PATH);
		UpdateInfoLabel();
	}

	private void OnPreload2Pressed()
	{
		GD.Print("Preload scene 2 预加载场景2");
		LongSceneManagerCs.Instance.PreloadScene(TEST_SCENE_2_PATH);
		UpdateInfoLabel();
	}

	private void OnClearAllCachePressed()
	{
		GD.Print("Clear all cache 清空所有缓存");
		LongSceneManagerCs.Instance.ClearAllCache();
		UpdateInfoLabel();
	}

	private void OnSceneSwitchStarted(string fromScene, string toScene)
	{
		GD.Print($"Main - switch started 主场景 - 切换开始: {fromScene} -> {toScene}");
	}

	private void OnSceneSwitchCompleted(string scenePath)
	{
		GD.Print($"Main - switch completed 主场景 - 切换完成: {scenePath}");
	}

	private void OnSceneCached(string scenePath)
	{
		GD.Print($"Main - scene cached 主场景 - 场景已缓存: {scenePath}");
		if (!GodotObject.IsInstanceValid(this) || !IsInsideTree() || _labelInfo == null || !GodotObject.IsInstanceValid(_labelInfo))
		{
			return;
		}
		UpdateInfoLabel();
	}
}

using Godot;
using System;

public partial class TestScene2 : Control
{
	private const string MAIN_SCENE_PATH = "res://demo_test_scene_manager/main_scene.tscn";
	private const string TEST_SCENE_1_PATH = "res://demo_test_scene_manager/test_scene_1.tscn";
	private const string TEST_SCENE_3_PATH = "res://demo_test_scene_manager/test_scene_3.tscn";
	private const string TEST_SCENE_4_PATH = "res://demo_test_scene_manager/test_scene_4.tscn";
	private const string TEST_SCENE_5_PATH = "res://demo_test_scene_manager/test_scene_5.tscn";
	private const string CUSTOM_LOADING_SCENE = "res://demo_test_scene_manager/custom_load_screen/custom_load_screen.tscn";

	[Export]
	private Button _buttonMain;
	[Export]
	private Button _buttonScene1;
	[Export]
	private Button _preloadAll;
	[Export]
	private Button _cancelAllPreload;
	[Export]
	private Button _cancelPreloadScene3;
	[Export]
	private Button _buttonPreloadScene3;
	[Export]
	private Button _buttonSwitchScene3WithPreload;
	[Export]
	private Button _buttonSwitchScene3Direct;
	[Export]
	private Button _buttonRemovePreloadScene3;
	[Export]
	private Button _buttonRemoveCachedScene3;
	[Export]
	private Button _buttonPreloadScene4;
	[Export]
	private Button _buttonSwitchScene4WithPreload;
	[Export]
	private Button _buttonSwitchScene4Direct;
	[Export]
	private Button _buttonRemovePreloadScene4;
	[Export]
	private Button _buttonRemoveCachedScene4;
	[Export]
	private Button _buttonPreloadScene5;
	[Export]
	private Button _buttonSwitchScene5WithPreload;
	[Export]
	private Button _buttonSwitchScene5Direct;
	[Export]
	private Button _buttonRemovePreloadScene5;
	[Export]
	private Button _buttonRemoveCachedScene5;
	[Export]
	private Button _loadScene3WithSceneCache;
	[Export]
	private Button _loadScene4WithSceneCache;
	[Export]
	private Button _loadScene5WithSceneCache;
	[Export]
	private Button _sceneToTemp;
	[Export]
	private Button _sceneTofixed;
	[Export]
	private Button _clearTempCache;
	[Export]
	private Button _clearfixedCache;
	[Export]
	private Button _clearinstanceCache;
	[Export]
	private Button _removeTempResource4;
	[Export]
	private Button _removeFixResource4;
	[Export]
	private Button _removeCachedScene4;
	[Export]
	private Label _labelInfo;
	[Export]
	private ProgressBar _progressBarPreloadScene3;
	[Export]
	private ProgressBar _progressBarPreloadScene4;
	[Export]
	private ProgressBar _progressBarPreloadScene5;

	private bool _isFirstEnter = true;

	public override void _Ready()
	{
		GD.Print("=== Test Scene 2 Loaded 测试场景2加载完成 ===");
		SetProcess(false);

		_buttonMain.Pressed += OnMainPressed;
		_buttonScene1.Pressed += OnScene1Pressed;
		_preloadAll.Pressed += OnPreloadAllPressed;
		_cancelAllPreload.Pressed += OnCancelAllPreloadPressed;
		_cancelPreloadScene3.Pressed += OnCancelPreloadScene3Pressed;
		_buttonPreloadScene3.Pressed += OnPreloadScene3Pressed;
		_buttonPreloadScene4.Pressed += OnPreloadScene4Pressed;
		_buttonPreloadScene5.Pressed += OnPreloadScene5Pressed;
		_buttonSwitchScene3WithPreload.Pressed += OnSwitchScene3WithPreloadPressed;
		_buttonSwitchScene3Direct.Pressed += OnSwitchScene3DirectPressed;
		_buttonSwitchScene4WithPreload.Pressed += OnSwitchScene4WithPreloadPressed;
		_buttonSwitchScene4Direct.Pressed += OnSwitchScene4DirectPressed;
		_buttonSwitchScene5WithPreload.Pressed += OnSwitchScene5WithPreloadPressed;
		_buttonSwitchScene5Direct.Pressed += OnSwitchScene5DirectPressed;
		_buttonRemovePreloadScene3.Pressed += OnRemovePreloadScene3Pressed;
		_buttonRemoveCachedScene3.Pressed += OnRemoveCachedScene3Pressed;
		_buttonRemovePreloadScene4.Pressed += OnRemovePreloadScene4Pressed;
		_buttonRemoveCachedScene4.Pressed += OnRemoveCachedScene4Pressed;
		_buttonRemovePreloadScene5.Pressed += OnRemovePreloadScene5Pressed;
		_buttonRemoveCachedScene5.Pressed += OnRemoveCachedScene5Pressed;
		_loadScene3WithSceneCache.Pressed += OnLoadScene3WithSceneCachePressed;
		_loadScene4WithSceneCache.Pressed += OnLoadScene4WithSceneCachePressed;
		_loadScene5WithSceneCache.Pressed += OnLoadScene5WithSceneCachePressed;
		_sceneToTemp.Pressed += OnSceneToTempPressed;
		_sceneTofixed.Pressed += OnSceneToFixedPressed;
		_clearTempCache.Pressed += OnClearTempCachePressed;
		_clearfixedCache.Pressed += OnClearFixedCachePressed;
		_clearinstanceCache.Pressed += OnClearInstanceCachePressed;
		_removeTempResource4.Pressed += OnRemoveTempResource4Pressed;
		_removeFixResource4.Pressed += OnRemoveFixResource4Pressed;
		_removeCachedScene4.Pressed += OnRemoveCachedScene4Pressed;

		_isFirstEnter = false;
		UpdateInfo();

		LongSceneManagerCs.Instance.SceneSwitchStarted += OnSceneSwitchStarted;
		LongSceneManagerCs.Instance.SceneSwitchCompleted += OnSceneSwitchCompleted;
	}

	public override void _EnterTree()
	{
		SetProcess(false);
		if (!_isFirstEnter)
		{
			UpdateInfo();
		}
	}

	public override void _Process(double delta)
	{
		if (!GodotObject.IsInstanceValid(this) || !IsInsideTree() || _progressBarPreloadScene3 == null || !GodotObject.IsInstanceValid(_progressBarPreloadScene3) ||
			_progressBarPreloadScene4 == null || !GodotObject.IsInstanceValid(_progressBarPreloadScene4) ||
			_progressBarPreloadScene5 == null || !GodotObject.IsInstanceValid(_progressBarPreloadScene5))
		{
			return;
		}

		UpdateInfo();

		float scene3Progress = LongSceneManagerCs.Instance.GetLoadingProgress(TEST_SCENE_3_PATH);
		float scene4Progress = LongSceneManagerCs.Instance.GetLoadingProgress(TEST_SCENE_4_PATH);
		float scene5Progress = LongSceneManagerCs.Instance.GetLoadingProgress(TEST_SCENE_5_PATH);

		if (scene3Progress > 0 && scene3Progress < 1.0f)
		{
			_progressBarPreloadScene3.Value = scene3Progress * 100;
		}
		else if (scene3Progress >= 1.0f)
		{
			_progressBarPreloadScene3.Value = 100;
		}
		else
		{
			_progressBarPreloadScene3.Value = 0;
		}

		if (scene4Progress > 0 && scene4Progress < 1.0f)
		{
			_progressBarPreloadScene4.Value = scene4Progress * 100;
		}
		else if (scene4Progress >= 1.0f)
		{
			_progressBarPreloadScene4.Value = 100;
		}
		else
		{
			_progressBarPreloadScene4.Value = 0;
		}

		if (scene5Progress > 0 && scene5Progress < 1.0f)
		{
			_progressBarPreloadScene5.Value = scene5Progress * 100;
		}
		else if (scene5Progress >= 1.0f)
		{
			_progressBarPreloadScene5.Value = 100;
		}
		else
		{
			_progressBarPreloadScene5.Value = 0;
		}
	}

	private void UpdateInfo()
	{
		if (!GodotObject.IsInstanceValid(this) || !IsInsideTree() || _labelInfo == null || !GodotObject.IsInstanceValid(_labelInfo) ||
			_progressBarPreloadScene3 == null || !GodotObject.IsInstanceValid(_progressBarPreloadScene3) ||
			_progressBarPreloadScene4 == null || !GodotObject.IsInstanceValid(_progressBarPreloadScene4) ||
			_progressBarPreloadScene5 == null || !GodotObject.IsInstanceValid(_progressBarPreloadScene5))
		{
			return;
		}

		_progressBarPreloadScene3.Value = 0;
		_progressBarPreloadScene4.Value = 0;
		_progressBarPreloadScene5.Value = 0;

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

	private async void OnScene1Pressed()
	{
		GD.Print("Switch to scene 1 切换到场景1");
		await LongSceneManagerCs.Instance.SwitchScene(TEST_SCENE_1_PATH, LongSceneManagerCs.LoadMethod.BothPreloadFirst, true, "");
	}

	private void OnPreloadScene3Pressed()
	{
		GD.Print("Preload scene 3 预加载场景3");
		SetProcess(true);
		LongSceneManagerCs.Instance.PreloadScene(TEST_SCENE_3_PATH);
		UpdateInfo();
	}

	private void OnPreloadScene4Pressed()
	{
		GD.Print("Preload scene 4 预加载场景4");
		SetProcess(true);
		LongSceneManagerCs.Instance.PreloadScene(TEST_SCENE_4_PATH);
		UpdateInfo();
	}

	private void OnPreloadScene5Pressed()
	{
		GD.Print("Preload scene 5 预加载场景5");
		SetProcess(true);
		LongSceneManagerCs.Instance.PreloadScene(TEST_SCENE_5_PATH);
		UpdateInfo();
	}

	private async void OnSwitchScene3WithPreloadPressed()
	{
		GD.Print("Switch scene 3 with preload 使用预加载切换场景3");
		await LongSceneManagerCs.Instance.SwitchScene(TEST_SCENE_3_PATH, LongSceneManagerCs.LoadMethod.BothPreloadFirst, true, "");
	}

	private async void OnSwitchScene3DirectPressed()
	{
		GD.Print("Direct load scene 3 直接加载场景3");
		await LongSceneManagerCs.Instance.SwitchScene(TEST_SCENE_3_PATH, LongSceneManagerCs.LoadMethod.Direct, true, CUSTOM_LOADING_SCENE);
	}

	private async void OnSwitchScene4WithPreloadPressed()
	{
		GD.Print("Switch scene 4 with preload 使用预加载切换场景4");
		await LongSceneManagerCs.Instance.SwitchScene(TEST_SCENE_4_PATH, LongSceneManagerCs.LoadMethod.BothPreloadFirst, true, "");
	}

	private async void OnSwitchScene4DirectPressed()
	{
		GD.Print("Direct load scene 4 直接加载场景4");
		await LongSceneManagerCs.Instance.SwitchScene(TEST_SCENE_4_PATH, LongSceneManagerCs.LoadMethod.Direct, true, CUSTOM_LOADING_SCENE);
	}

	private async void OnSwitchScene5WithPreloadPressed()
	{
		GD.Print("Switch scene 5 with preload 使用预加载切换场景5");
		await LongSceneManagerCs.Instance.SwitchScene(TEST_SCENE_5_PATH, 3, true, "");
	}

	private async void OnSwitchScene5DirectPressed()
	{
		GD.Print("Direct load scene 5 直接加载场景5");
		await LongSceneManagerCs.Instance.SwitchScene(TEST_SCENE_5_PATH, "Direct", true, CUSTOM_LOADING_SCENE);
	}

	private void OnSceneSwitchStarted(string fromScene, string toScene)
	{
		GD.Print($"Scene2-switch start 场景2-切换开始: {fromScene} -> {toScene}");
	}

	private void OnSceneSwitchCompleted(string scenePath)
	{
		GD.Print($"Scene2-switch completed 场景2-切换完成: {scenePath}");
	}

	private void OnRemovePreloadScene3Pressed()
	{
		GD.Print("Remove preload resource scene3 移除预加载资源场景3");
		LongSceneManagerCs.Instance.RemoveTempResource(TEST_SCENE_3_PATH);
		UpdateInfo();
	}

	private void OnRemoveCachedScene3Pressed()
	{
		GD.Print("Remove instate scene3 移除实例化缓存场景3");
		LongSceneManagerCs.Instance.RemoveCachedScene(TEST_SCENE_3_PATH);
		UpdateInfo();
	}

	private void OnRemovePreloadScene4Pressed()
	{
		GD.Print("Remove preload resource scene4 移除预加载资源场景4");
		LongSceneManagerCs.Instance.RemoveTempResource(TEST_SCENE_4_PATH);
		UpdateInfo();
	}

	private void OnRemoveCachedScene4Pressed()
	{
		GD.Print("Remove instate scene4 移除实例化缓存场景4");
		LongSceneManagerCs.Instance.RemoveCachedScene(TEST_SCENE_4_PATH);
		UpdateInfo();
	}

	private void OnRemovePreloadScene5Pressed()
	{
		GD.Print("Remove preload resource scene5 移除预加载资源场景5");
		LongSceneManagerCs.Instance.RemoveTempResource(TEST_SCENE_5_PATH);
		UpdateInfo();
	}

	private void OnRemoveCachedScene5Pressed()
	{
		GD.Print("Remove instate scene5 移除实例化缓存场景5");
		LongSceneManagerCs.Instance.RemoveCachedScene(TEST_SCENE_5_PATH);
		UpdateInfo();
	}

	private void OnPreloadAllPressed()
	{
		SetProcess(true);
		string[] scenes = new string[] { TEST_SCENE_3_PATH, TEST_SCENE_4_PATH, TEST_SCENE_5_PATH };
		LongSceneManagerCs.Instance.PreloadScenes(scenes);
		UpdateInfo();
	}

	private void OnCancelAllPreloadPressed()
	{
		SetProcess(true);
		LongSceneManagerCs.Instance.CancelAllPreloading();
		UpdateInfo();
	}

	private void OnCancelPreloadScene3Pressed()
	{
		SetProcess(true);
		LongSceneManagerCs.Instance.CancelPreloadingScene(TEST_SCENE_3_PATH);
		UpdateInfo();
	}

	private void OnSceneToTempPressed()
	{
		GD.Print("Move scene 5 from fixed to temp cache 将场景5从固定缓存移至临时缓存");
		LongSceneManagerCs.Instance.MoveToTemp(TEST_SCENE_5_PATH);
		UpdateInfo();
	}

	private void OnSceneToFixedPressed()
	{
		GD.Print("Move scene 5 from temp to fixed cache 将场景5从临时缓存移至固定缓存");
		LongSceneManagerCs.Instance.MoveToFixed(TEST_SCENE_5_PATH);
		UpdateInfo();
	}

	private void OnClearTempCachePressed()
	{
		GD.Print("Clear temp preload cache 清除临时预加载缓存");
		LongSceneManagerCs.Instance.ClearTempPreloadCache();
		UpdateInfo();
	}

	private void OnClearFixedCachePressed()
	{
		GD.Print("Clear fixed preload cache 清除固定预加载缓存");
		LongSceneManagerCs.Instance.ClearFixedCache();
		UpdateInfo();
	}

	private void OnClearInstanceCachePressed()
	{
		GD.Print("Clear instance cache 清除实例化场景缓存");
		LongSceneManagerCs.Instance.ClearInstanceCache();
		UpdateInfo();
	}

	private void OnRemoveTempResource4Pressed()
	{
		GD.Print("Remove temp resource scene 4 移除临时预加载资源场景4");
		LongSceneManagerCs.Instance.RemoveTempResource(TEST_SCENE_4_PATH);
		UpdateInfo();
	}

	private void OnRemoveFixResource4Pressed()
	{
		GD.Print("Remove fixed resource scene 4 移除固定预加载资源场景4");
		LongSceneManagerCs.Instance.RemoveFixedResource(TEST_SCENE_4_PATH);
		UpdateInfo();
	}

	private async void OnLoadScene3WithSceneCachePressed()
	{
		await LongSceneManagerCs.Instance.SwitchScene(TEST_SCENE_3_PATH, LongSceneManagerCs.LoadMethod.SceneCache, true, "");
	}

	private async void OnLoadScene4WithSceneCachePressed()
	{
		await LongSceneManagerCs.Instance.SwitchScene(TEST_SCENE_4_PATH, LongSceneManagerCs.LoadMethod.SceneCache, true, "");
	}

	private async void OnLoadScene5WithSceneCachePressed()
	{
		await LongSceneManagerCs.Instance.SwitchScene(TEST_SCENE_5_PATH, LongSceneManagerCs.LoadMethod.SceneCache, true, "");
	}
}

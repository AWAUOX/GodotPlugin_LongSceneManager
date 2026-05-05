using Godot;
using System;
using LongSceneManagerCs;

public partial class MainSceneCs : Control
{
	// Scene path constants 场景路径常量
	private const string TEST_SCENE_1_PATH = "res://demo_test_scene_manager/test_scene_1.tscn";
	private const string TEST_SCENE_2_PATH = "res://demo_test_scene_manager/test_scene_2.tscn";

	private Button buttonScene1;
	private Button buttonScene2;
	private Button buttonPreload1;
	private Button buttonPreload2;
	private Button buttonClearCache;
	private Label labelInfo;

	private LongSceneManagerCs.LongSceneManagerCs sceneManager; // Cache SceneManager reference 缓存 SceneManager 引用
	private bool isFirstEnter = true;

	public override void _Ready()
	{
		GD.Print("=== Main Scene Loaded (C#) 主场景加载完成 (C#) ===");

		// Get node references (consistent with GDScript paths) 获取节点引用（与GDScript路径一致）
		buttonScene1 = GetNode<Button>("MarginContainer/VBoxContainer/HBoxContainer2/HBoxContainer/Button_Scene1");
		buttonScene2 = GetNode<Button>("MarginContainer/VBoxContainer/HBoxContainer2/HBoxContainer/Button_Scene2");
		buttonPreload1 = GetNode<Button>("MarginContainer/VBoxContainer/HBoxContainer2/HBoxContainer/Button_Preload1");
		buttonPreload2 = GetNode<Button>("MarginContainer/VBoxContainer/HBoxContainer2/HBoxContainer/Button_Preload2");
		buttonClearCache = GetNode<Button>("MarginContainer/VBoxContainer/HBoxContainer2/HBoxContainer/Button_ClearCache");
		labelInfo = GetNode<Label>("MarginContainer/VBoxContainer/HBoxContainer2/ScrollContainer/VBoxContainer/Label_Info");

		// Connect button signals 连接按钮信号
		buttonScene1.Pressed += OnScene1Pressed;
		buttonScene2.Pressed += OnScene2Pressed;
		buttonPreload1.Pressed += OnPreload1Pressed;
		buttonPreload2.Pressed += OnPreload2Pressed;
		buttonClearCache.Pressed += OnClearCachePressed;

		// Update info label 更新信息标签
		UpdateInfoLabel();
		isFirstEnter = false;

		// Get and cache SceneManager instance 获取并缓存 SceneManager 实例
		sceneManager = GetNode<LongSceneManagerCs.LongSceneManagerCs>("/root/LongSceneManagerCs");

		// Connect SceneManager signals 连接SceneManager信号
		sceneManager.Connect("SceneSwitchStarted", Callable.From((string fromScene, string toScene) => OnSceneSwitchStarted(fromScene, toScene)));
		sceneManager.Connect("SceneSwitchCompleted", Callable.From((string scenePath) => OnSceneSwitchCompleted(scenePath)));
		sceneManager.Connect("SceneCached", Callable.From((string scenePath) => OnSceneCached(scenePath)));
		sceneManager.Connect("ScenePreloadCompleted", Callable.From((string scenePath) => OnScenePreloadCompleted(scenePath)));
	}

	public override void _EnterTree()
	{
		if (!isFirstEnter)
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
		var manager = sceneManager;
		if (manager == null)
		{
			labelInfo.Text = "Scene manager not found 场景管理器未找到";
			return;
		}
		
		var cacheInfo = manager.GetCacheInfo();
		
		// Get nested dictionaries 获取嵌套字典
		var instanceCache = (Godot.Collections.Dictionary)cacheInfo["instance_cache"];
		var preloadCache = (Godot.Collections.Dictionary)cacheInfo["preload_cache"];
		
		var instanceAccessOrder = (Godot.Collections.Array)instanceCache["access_order"];
		
		labelInfo.Text = string.Format(@"
Current Scene: Main Scene (C#)
Previous Scene: {0}

[Instance Scene Cache] Count: {1}/{2}
Scene List:
{3}

[Preloaded Resource Cache] Count: {4}/{5}",
			manager.GetPreviousScenePath(),
			instanceCache["size"],
			instanceCache["max_size"],
			string.Join("\n", instanceAccessOrder),
			preloadCache["size"],
			preloadCache["max_size"]);
	}

	private void OnScene1Pressed()
	{
		// Switch to scene 1 (using default loading screen) 切换到场景1（使用默认加载屏幕）
		GD.Print("Switch to scene 1 (C# Interface) 切换到场景1 (C# Interface)");
		sceneManager.SwitchSceneGD(TEST_SCENE_1_PATH, true, "");
	}

	private void OnScene2Pressed()
	{
		// Switch to scene 2 (using default loading screen) 切换到场景2（使用默认加载屏幕）
		GD.Print("Switch to scene 2 (C# Interface) 切换到场景2 (C# Interface)");
		sceneManager.SwitchSceneGD(TEST_SCENE_2_PATH, true, "");
	}

	private void OnPreload1Pressed()
	{
		// Preload scene 1 预加载场景1
		GD.Print("Preload scene 1 (C# Interface) 预加载场景1 (C# Interface)");
		sceneManager.PreloadSceneGD(TEST_SCENE_1_PATH);
	}

	private void OnPreload2Pressed()
	{
		// Preload scene 2 预加载场景2
		GD.Print("Preload scene 2 (C# Interface) 预加载场景2 (C# Interface)");
		sceneManager.PreloadSceneGD(TEST_SCENE_2_PATH);
	}

	private void OnClearCachePressed()
	{
		// Clear cache 清空缓存
		GD.Print("Clear cache (C# Interface) 清空缓存 (C# Interface)");
		sceneManager.ClearCache();
		UpdateInfoLabel();
	}

	private void OnSceneSwitchStarted(string fromScene, string toScene)
	{
		GD.Print($"Scene switch started (C# Interface) 场景切换开始 (C# Interface): {fromScene} -> {toScene}");
	}

	private void OnSceneSwitchCompleted(string scenePath)
	{
		GD.Print($"Scene switch completed (C# Interface) 场景切换完成 (C# Interface): {scenePath}");
	}

	private void OnSceneCached(string scenePath)
	{
		GD.Print($"Scene cached (C# Interface) 场景已缓存 (C# Interface): {scenePath}");
		UpdateInfoLabel();
	}

	private void OnScenePreloadCompleted(string scenePath)
	{
		GD.Print($"Scene preload completed (C# Interface) 场景预加载完成 (C# Interface): {scenePath}");
		UpdateInfoLabel();
	}
}

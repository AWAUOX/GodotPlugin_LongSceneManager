using Godot;
using System;
using LongSceneManagerCs;

public partial class MainSceneCs : Control
{
	// 场景路径常量
	private const string TEST_SCENE_1_PATH = "res://demo_test_scene_manager/test_scene_1.tscn";
	private const string TEST_SCENE_2_PATH = "res://demo_test_scene_manager/test_scene_2.tscn";

	private Button buttonScene1;
	private Button buttonScene2;
	private Button buttonPreload1;
	private Button buttonPreload2;
	private Button buttonClearCache;
	private Label labelInfo;

	private LongSceneManagerCs.LongSceneManagerCs sceneManager; // 缓存 SceneManager 引用
	private bool isFirstEnter = true;

	public override void _Ready()
	{
		GD.Print("=== Main Scene Loaded (C# Interface) ===");

		// 获取节点引用
		buttonScene1 = GetNode<Button>("VBoxContainer/Button_Scene1");
		buttonScene2 = GetNode<Button>("VBoxContainer/Button_Scene2");
		buttonPreload1 = GetNode<Button>("VBoxContainer/Button_Preload1");
		buttonPreload2 = GetNode<Button>("VBoxContainer/Button_Preload2");
		buttonClearCache = GetNode<Button>("VBoxContainer/Button_ClearCache");
		labelInfo = GetNode<Label>("VBoxContainer/Label_Info");

		// 连接按钮信号
		buttonScene1.Pressed += OnScene1Pressed;
		buttonScene2.Pressed += OnScene2Pressed;
		buttonPreload1.Pressed += OnPreload1Pressed;
		buttonPreload2.Pressed += OnPreload2Pressed;
		buttonClearCache.Pressed += OnClearCachePressed;

		// 更新信息标签
		UpdateInfoLabel();
		isFirstEnter = false;

		// 获取并缓存 SceneManager 实例
		sceneManager = GetNode<LongSceneManagerCs.LongSceneManagerCs>("/root/LongSceneManagerCs");

		// 连接SceneManager信号
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
		// 使用已缓存的场景管理器实例，避免在场景切换过程中使用GetNode
		LongSceneManagerCs.LongSceneManagerCs manager = sceneManager;
		
		// 检查manager是否为空，避免空引用异常
		if (manager == null)
		{
			labelInfo.Text = "场景管理器未找到";
			return;
		}
		
		var cacheInfo = manager.GetCacheInfo();

		labelInfo.Text = string.Format(@"
当前场景: Main Scene (C# Interface)
上一个场景: {0}
缓存实例场景数: {1}/{2}
缓存最大数值: {3}
缓存实例场景列表: {4}
预加载资源缓存数量: {5}
预加载缓存最大数值: {6}",
			manager.GetPreviousScenePath(),
			cacheInfo["instance_cache_size"],
			cacheInfo["max_size"],
			cacheInfo["max_size"],
			string.Join(",\n ", (string[])cacheInfo["access_order"]),
			((Godot.Collections.Array)cacheInfo["preload_resource_cache"]).Count,
			cacheInfo["max_preload_resource_cache_size"]);
	}

	private void OnScene1Pressed()
	{
		// 切换到场景1（使用默认加载屏幕）
		GD.Print("切换到场景1 (C# Interface)");
		sceneManager.SwitchSceneGD(TEST_SCENE_1_PATH, true, "");
	}

	private void OnScene2Pressed()
	{
		// 切换到场景2（使用默认加载屏幕）
		GD.Print("切换到场景2 (C# Interface)");
		sceneManager.SwitchSceneGD(TEST_SCENE_2_PATH, true, "");
	}

	private void OnPreload1Pressed()
	{
		// 预加载场景1
		GD.Print("预加载场景1 (C# Interface)");
		sceneManager.PreloadSceneGD(TEST_SCENE_1_PATH);
	}

	private void OnPreload2Pressed()
	{
		// 预加载场景2
		GD.Print("预加载场景2 (C# Interface)");
		sceneManager.PreloadSceneGD(TEST_SCENE_2_PATH);
	}

	private void OnClearCachePressed()
	{
		// 清空缓存
		GD.Print("清空缓存 (C# Interface)");
		sceneManager.ClearCache();
		UpdateInfoLabel();
	}

	private void OnSceneSwitchStarted(string fromScene, string toScene)
	{
		GD.Print($"场景切换开始 (C# Interface): {fromScene} -> {toScene}");
	}

	private void OnSceneSwitchCompleted(string scenePath)
	{
		GD.Print($"场景切换完成 (C# Interface): {scenePath}");
	}

	private void OnSceneCached(string scenePath)
	{
		GD.Print($"场景已缓存 (C# Interface): {scenePath}");
		UpdateInfoLabel();
	}

	private void OnScenePreloadCompleted(string scenePath)
	{
		GD.Print($"场景预加载完成 (C# Interface): {scenePath}");
		UpdateInfoLabel();
	}
}
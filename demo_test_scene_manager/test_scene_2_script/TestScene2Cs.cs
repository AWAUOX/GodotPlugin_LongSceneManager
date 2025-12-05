using Godot;
using System;
using LongSceneManagerCs;

public partial class TestScene2Cs : Node2D
{
	private const string MAIN_SCENE_PATH = "res://demo_test_scene_manager/main_scene.tscn";
	private const string TEST_SCENE_1_PATH = "res://demo_test_scene_manager/test_scene_1.tscn";

	private Button buttonMain;
	private Button buttonScene1;
	private Button buttonPreloadMain;
	private Label labelInfo;
	private ProgressBar progressBar;

	private bool isFirstEnter = true;

	public override void _Ready()
	{
		GD.Print("=== Test Scene 2 Loaded (C# Interface) ===");
		SetProcess(false);

		// 获取节点引用
		buttonMain = GetNode<Button>("VBoxContainer/Button_Main");
		buttonScene1 = GetNode<Button>("VBoxContainer/Button_Scene1");
		buttonPreloadMain = GetNode<Button>("VBoxContainer/Button_PreloadMain");
		labelInfo = GetNode<Label>("VBoxContainer/Label_Info");
		progressBar = GetNode<ProgressBar>("ProgressBar");

		// 连接按钮信号
		buttonMain.Pressed += OnMainPressed;
		buttonScene1.Pressed += OnScene1Pressed;
		buttonPreloadMain.Pressed += OnPreloadMainPressed;

		isFirstEnter = false;

		// 更新信息
		UpdateInfo();

		// 连接SceneManager信号
		LongSceneManagerCs.LongSceneManagerCs manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
		manager.Connect("SceneSwitchStarted", Callable.From((string fromScene, string toScene) => OnSceneSwitchStarted(fromScene, toScene)));
		manager.Connect("SceneSwitchCompleted", Callable.From((string scenePath) => OnSceneSwitchCompleted(scenePath)));
		manager.Connect("ScenePreloadCompleted", Callable.From((string scenePath) => OnScenePreloadCompleted(scenePath)));
	}

	public override void _EnterTree()
	{
		SetProcess(false);
		if (!isFirstEnter)
		{
			UpdateInfo();
		}
	}

	public override void _Process(double delta)
	{
		UpdateInfo();

		// 每帧更新预加载进度
		LongSceneManagerCs.LongSceneManagerCs manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
		var progress = manager.GetLoadingProgress(MAIN_SCENE_PATH);
		progressBar.Value = progress * 100;

		if (progress < 1.0 && progress > 0)
		{
			labelInfo.Text = $"预加载主场景进度: {Mathf.Round(progress * 100)}%";
		}
	}

	private void UpdateInfo()
	{
		// 获取缓存信息
		LongSceneManagerCs.LongSceneManagerCs manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
		var cacheInfo = manager.GetCacheInfo();
		progressBar.Value = 0;

		labelInfo.Text = string.Format(@"
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
			cacheInfo["preload_cache_size"],
			cacheInfo["max_preload_resource_cache_size"]);
	}

	private void OnMainPressed()
	{
		// 切换回主场景
		GD.Print("切换回主场景 (C# Interface)");
		LongSceneManagerCs.LongSceneManagerCs manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
		manager.SwitchSceneGD(MAIN_SCENE_PATH, true, "");
	}

	private void OnScene1Pressed()
	{
		// 切换到场景1
		GD.Print("切换到场景1 (C# Interface)");
		LongSceneManagerCs.LongSceneManagerCs manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
		manager.SwitchSceneGD(TEST_SCENE_1_PATH, true, "");
	}

	private void OnPreloadMainPressed()
	{
		// 预加载主场景
		GD.Print("预加载主场景 (C# Interface)");
		SetProcess(true);
		LongSceneManagerCs.LongSceneManagerCs manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
		manager.PreloadSceneGD(MAIN_SCENE_PATH);
	}

	private void OnSceneSwitchStarted(string fromScene, string toScene)
	{
		GD.Print($"场景2 - 切换开始 (C# Interface): {fromScene} -> {toScene}");
	}

	private void OnSceneSwitchCompleted(string scenePath)
	{
		GD.Print($"场景2 - 切换完成 (C# Interface): {scenePath}");
	}

	private void OnScenePreloadCompleted(string scenePath)
	{
		GD.Print($"场景预加载完成 (C# Interface): {scenePath}");
		UpdateInfo();
	}
}
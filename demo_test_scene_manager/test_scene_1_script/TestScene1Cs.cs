using Godot;
using System;
using LongSceneManagerCs;

public partial class TestScene1Cs : Node2D
{
	private const string MAIN_SCENE_PATH = "res://demo_test_scene_manager/main_scene.tscn";
	private const string TEST_SCENE_2_PATH = "res://demo_test_scene_manager/test_scene_2.tscn";

	private Button buttonMain;
	private Button buttonScene2;
	private Button buttonBack;
	private Label labelInfo;

	private bool isFirstEnter = true;

	public override void _Ready()
	{
		GD.Print("=== Test Scene 1 Loaded (C# Interface) ===");

		// 获取节点引用
		buttonMain = GetNode<Button>("VBoxContainer/Button_Main");
		buttonScene2 = GetNode<Button>("VBoxContainer/Button_Scene2");
		buttonBack = GetNode<Button>("VBoxContainer/Button_Back");
		labelInfo = GetNode<Label>("VBoxContainer/Label_Info");

		// 连接按钮信号
		buttonMain.Pressed += OnMainPressed;
		buttonScene2.Pressed += OnScene2Pressed;
		buttonBack.Pressed += OnBackPressed;

		// 更新信息标签
		UpdateInfoLabel();
		isFirstEnter = false;

		// 连接SceneManager信号
		LongSceneManagerCs.LongSceneManagerCs manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
		manager.Connect("SceneSwitchStarted", Callable.From((string fromScene, string toScene) => OnSceneSwitchStarted(fromScene, toScene)));
		manager.Connect("SceneSwitchCompleted", Callable.From((string scenePath) => OnSceneSwitchCompleted(scenePath)));
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
		// 获取缓存信息
		LongSceneManagerCs.LongSceneManagerCs manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
		var cacheInfo = manager.GetCacheInfo();

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
			((string[])cacheInfo["preload_resource_cache"]).Length,
			cacheInfo["max_preload_resource_cache_size"]);
	}

	private void OnMainPressed()
	{
		// 切换回主场景
		GD.Print("切换回主场景 (C# Interface)");
		LongSceneManagerCs.LongSceneManagerCs manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
		manager.SwitchSceneGD(MAIN_SCENE_PATH, true, "");
	}

	private void OnScene2Pressed()
	{
		// 切换到场景2
		GD.Print("切换到场景2 (C# Interface)");
		LongSceneManagerCs.LongSceneManagerCs manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
		manager.SwitchSceneGD(TEST_SCENE_2_PATH, true, "");
	}

	private void OnBackPressed()
	{
		// 返回按钮（特殊测试：无过渡效果）
		GD.Print("返回主场景（无过渡效果）(C# Interface)");
		LongSceneManagerCs.LongSceneManagerCs manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
		manager.SwitchSceneGD(MAIN_SCENE_PATH, true, "no_transition");
	}

	private void OnSceneSwitchStarted(string fromScene, string toScene)
	{
		GD.Print($"场景1 - 切换开始 (C# Interface): {fromScene} -> {toScene}");
	}

	private void OnSceneSwitchCompleted(string scenePath)
	{
		GD.Print($"场景1 - 切换完成 (C# Interface): {scenePath}");
	}
}
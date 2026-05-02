using Godot;
using System;

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
		GD.Print("=== Test Scene 1 Loaded (C#) ===");

		// 获取节点引用（与GDScript路径一致）
		buttonMain = GetNode<Button>("MarginContainer/VBoxContainer/HBoxContainer/VBoxContainer/Button_Main");
		buttonScene2 = GetNode<Button>("MarginContainer/VBoxContainer/HBoxContainer/VBoxContainer/Button_Scene2");
		buttonBack = GetNode<Button>("MarginContainer/VBoxContainer/HBoxContainer/VBoxContainer/Button_Back");
		labelInfo = GetNode<Label>("MarginContainer/VBoxContainer/HBoxContainer/Label_Info");

		// 连接按钮信号
		buttonMain.Pressed += OnMainPressed;
		buttonScene2.Pressed += OnScene2Pressed;
		buttonBack.Pressed += OnBackPressed;

		// 更新信息标签
		UpdateInfoLabel();
		isFirstEnter = false;

		// 连接SceneManager信号
		var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
		manager.SceneSwitchStarted += OnSceneSwitchStarted;
		manager.SceneSwitchCompleted += OnSceneSwitchCompleted;
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
		var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
		var cacheInfo = manager.GetCacheInfo();

		// 获取嵌套字典
		var instanceCache = (Godot.Collections.Dictionary)cacheInfo["instance_cache"];
		var preloadCache = (Godot.Collections.Dictionary)cacheInfo["preload_cache"];

		// 处理实例化场景缓存列表
		var cachedScenes = (Godot.Collections.Array<Godot.Collections.Dictionary>)instanceCache["scenes"];
		var instancePaths = new System.Collections.Generic.List<string>();
		foreach (var s in cachedScenes)
		{
			instancePaths.Add((string)s["path"]);
		}
		var instanceList = instancePaths.Count > 0 ? string.Join("\n", instancePaths) : "（empty）";

		// 处理预加载资源缓存列表
		var preloadPaths = (Godot.Collections.Array<string>)preloadCache["scenes"];
		var preloadList = preloadPaths.Count > 0 ? string.Join("\n", preloadPaths) : "（empty）";

		labelInfo.Text = string.Format(@"
Current Scene: {0}
Previous Scene: {1}

[Instance Scene Cache] Count: {2}/{3}
Scene List:
{4}

[Preloaded Resource Cache] Count: {5}/{6}
Resource List:
{7}
",
			cacheInfo["current_scene"],
			cacheInfo["previous_scene"],
			instanceCache["size"],
			instanceCache["max_size"],
			instanceList,
			preloadCache["size"],
			preloadCache["max_size"],
			preloadList);
	}

	private void OnMainPressed()
	{
		// 切换回主场景
		GD.Print("切换回主场景 (C#)");
		var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
		manager.SwitchSceneGD(MAIN_SCENE_PATH, true, "");
	}

	private void OnScene2Pressed()
	{
		// 切换到场景2
		GD.Print("切换到场景2 (C#)");
		var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
		manager.SwitchSceneGD(TEST_SCENE_2_PATH, true, "");
	}

	private void OnBackPressed()
	{
		// 返回按钮（特殊测试：无过渡效果）
		GD.Print("返回主场景（无过渡效果）(C#)");
		var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
		manager.SwitchSceneGD(MAIN_SCENE_PATH, true, "no_transition");
	}

	private void OnSceneSwitchStarted(string fromScene, string toScene)
	{
		GD.Print($"场景1 - 切换开始 (C#): {fromScene} -> {toScene}");
	}

	private void OnSceneSwitchCompleted(string scenePath)
	{
		GD.Print($"场景1 - 切换完成 (C#): {scenePath}");
	}
}

using Godot;
using System;
using System.Linq;

public partial class TestScene2Cs : Control
{
	private const string MAIN_SCENE_PATH = "res://demo_test_scene_manager/main_scene.tscn";
	private const string TEST_SCENE_1_PATH = "res://demo_test_scene_manager/test_scene_1.tscn";
	private const string TEST_SCENE_3_PATH = "res://demo_test_scene_manager/test_scene_3.tscn";
	private const string TEST_SCENE_4_PATH = "res://demo_test_scene_manager/test_scene_4.tscn";
	private const string TEST_SCENE_5_PATH = "res://demo_test_scene_manager/test_scene_5.tscn";

	private Button buttonMain;
	private Button buttonScene1;
	private Button buttonPreloadScene3;
	private Button buttonSwitchScene3WithPreload;
	private Button buttonSwitchScene3Direct;
	private Button buttonPreloadScene4;
	private Button buttonSwitchScene4WithPreload;
	private Button buttonSwitchScene4Direct;
	private Button buttonPreloadScene5;
	private Button buttonSwitchScene5WithPreload;
	private Button buttonSwitchScene5Direct;
	private Button buttonRemovePreloadScene3;
	private Button buttonRemoveCachedScene3;
	private Button buttonRemovePreloadScene4;
	private Button buttonRemoveCachedScene4;
	private Button buttonRemovePreloadScene5;
	private Button buttonRemoveCachedScene5;

	private Label labelInfo;
	private ProgressBar progressBarPreloadScene3;
	private ProgressBar progressBarPreloadScene4;
	private ProgressBar progressBarPreloadScene5;

	private bool isFirstEnter = true;

	public override void _Ready()
	{
		GD.Print("=== Test Scene 2 Loaded (C#) ===");
		SetProcess(false);

		// 获取节点引用
		buttonMain = GetNode<Button>("VBoxContainer/HBoxContainer/VBoxContainer/HBoxContainer/Button_Main");
		buttonScene1 = GetNode<Button>("VBoxContainer/HBoxContainer/VBoxContainer/HBoxContainer/Button_Scene1");
		buttonPreloadScene3 = GetNode<Button>("VBoxContainer/HBoxContainer/VBoxContainer/scene3/Button_PreloadScene3");
		buttonSwitchScene3WithPreload = GetNode<Button>("VBoxContainer/HBoxContainer/VBoxContainer/scene3/Button_SwitchScene3WithPreload");
		buttonSwitchScene3Direct = GetNode<Button>("VBoxContainer/HBoxContainer/VBoxContainer/scene3/Button_SwitchScene3Direct");
		buttonPreloadScene4 = GetNode<Button>("VBoxContainer/HBoxContainer/VBoxContainer/scene4/Button_PreloadScene4");
		buttonSwitchScene4WithPreload = GetNode<Button>("VBoxContainer/HBoxContainer/VBoxContainer/scene4/Button_SwitchScene4WithPreload");
		buttonSwitchScene4Direct = GetNode<Button>("VBoxContainer/HBoxContainer/VBoxContainer/scene4/Button_SwitchScene4Direct");
		buttonPreloadScene5 = GetNode<Button>("VBoxContainer/HBoxContainer/VBoxContainer/Scene5/Button_PreloadScene5");
		buttonSwitchScene5WithPreload = GetNode<Button>("VBoxContainer/HBoxContainer/VBoxContainer/Scene5/Button_SwitchScene5WithPreload");
		buttonSwitchScene5Direct = GetNode<Button>("VBoxContainer/HBoxContainer/VBoxContainer/Scene5/Button_SwitchScene5Direct");
		buttonRemovePreloadScene3 = GetNode<Button>("VBoxContainer/HBoxContainer/VBoxContainer/Scene3RemoveCache/RemoveScene3Resource");
		buttonRemoveCachedScene3 = GetNode<Button>("VBoxContainer/HBoxContainer/VBoxContainer/Scene3RemoveCache/RemoveScene3Instance");
		buttonRemovePreloadScene4 = GetNode<Button>("VBoxContainer/HBoxContainer/VBoxContainer/Scene4RemoveCache/RemoveScene4Resource");
		buttonRemoveCachedScene4 = GetNode<Button>("VBoxContainer/HBoxContainer/VBoxContainer/Scene4RemoveCache/RemoveScene4Instance");
		buttonRemovePreloadScene5 = GetNode<Button>("VBoxContainer/HBoxContainer/VBoxContainer/Scene5RemoveCache/RemoveScene5Resource");
		buttonRemoveCachedScene5 = GetNode<Button>("VBoxContainer/HBoxContainer/VBoxContainer/Scene5RemoveCache/RemoveScene5Instance");

		labelInfo = GetNode<Label>("VBoxContainer/HBoxContainer/VBoxContainer2/Label_Info");
		progressBarPreloadScene3 = GetNode<ProgressBar>("VBoxContainer/VBoxContainer/scene3/ProgressBar_PreloadScene3");
		progressBarPreloadScene4 = GetNode<ProgressBar>("VBoxContainer/VBoxContainer/scene4/ProgressBar_PreloadScene4");
		progressBarPreloadScene5 = GetNode<ProgressBar>("VBoxContainer/VBoxContainer/scene5/ProgressBar_PreloadScene5");

		// 连接所有按钮信号
		buttonMain.Pressed += OnMainPressed;
		buttonScene1.Pressed += OnScene1Pressed;
		buttonPreloadScene3.Pressed += OnPreloadScene3Pressed;
		buttonPreloadScene4.Pressed += OnPreloadScene4Pressed;
		buttonPreloadScene5.Pressed += OnPreloadScene5Pressed;
		buttonSwitchScene3WithPreload.Pressed += OnSwitchScene3WithPreloadPressed;
		buttonSwitchScene3Direct.Pressed += OnSwitchScene3DirectPressed;
		buttonSwitchScene4WithPreload.Pressed += OnSwitchScene4WithPreloadPressed;
		buttonSwitchScene4Direct.Pressed += OnSwitchScene4DirectPressed;
		buttonSwitchScene5WithPreload.Pressed += OnSwitchScene5WithPreloadPressed;
		buttonSwitchScene5Direct.Pressed += OnSwitchScene5DirectPressed;
		buttonRemovePreloadScene3.Pressed += OnRemovePreloadScene3Pressed;
		buttonRemoveCachedScene3.Pressed += OnRemoveCachedScene3Pressed;
		buttonRemovePreloadScene4.Pressed += OnRemovePreloadScene4Pressed;
		buttonRemoveCachedScene4.Pressed += OnRemoveCachedScene4Pressed;
		buttonRemovePreloadScene5.Pressed += OnRemovePreloadScene5Pressed;
		buttonRemoveCachedScene5.Pressed += OnRemoveCachedScene5Pressed;

		isFirstEnter = false;
		UpdateInfo();

		// 连接SceneManager信号
		var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
		manager.SceneSwitchStarted += OnSceneSwitchStarted;
		manager.SceneSwitchCompleted += OnSceneSwitchCompleted;
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

		var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");

		// 更新三个场景的预加载进度
		float scene3Progress = manager.GetLoadingProgress(TEST_SCENE_3_PATH);
		float scene4Progress = manager.GetLoadingProgress(TEST_SCENE_4_PATH);
		float scene5Progress = manager.GetLoadingProgress(TEST_SCENE_5_PATH);

		// 场景3进度
		if (scene3Progress > 0 && scene3Progress < 1.0f)
		{
			progressBarPreloadScene3.Value = scene3Progress * 100;
		}
		else if (scene3Progress >= 1.0f)
		{
			progressBarPreloadScene3.Value = 100;
		}
		else
		{
			progressBarPreloadScene3.Value = 0;
		}

		// 场景4进度
		if (scene4Progress > 0 && scene4Progress < 1.0f)
		{
			progressBarPreloadScene4.Value = scene4Progress * 100;
		}
		else if (scene4Progress >= 1.0f)
		{
			progressBarPreloadScene4.Value = 100;
		}
		else
		{
			progressBarPreloadScene4.Value = 0;
		}

		// 场景5进度
		if (scene5Progress > 0 && scene5Progress < 1.0f)
		{
			progressBarPreloadScene5.Value = scene5Progress * 100;
		}
		else if (scene5Progress >= 1.0f)
		{
			progressBarPreloadScene5.Value = 100;
		}
		else
		{
			progressBarPreloadScene5.Value = 0;
		}
	}

	private void UpdateInfo()
	{
		var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
		var cacheInfo = manager.GetCacheInfo();

		progressBarPreloadScene3.Value = 0;
		progressBarPreloadScene4.Value = 0;
		progressBarPreloadScene5.Value = 0;

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

	// 原有切换函数
	private void OnMainPressed()
	{
		GD.Print("切换回主场景 (C#)");
		var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
		manager.SwitchSceneGD(MAIN_SCENE_PATH, true, "");
	}

	private void OnScene1Pressed()
	{
		GD.Print("切换到场景1 (C#)");
		var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
		manager.SwitchSceneGD(TEST_SCENE_1_PATH, true, "");
	}

	// 预加载函数
	private void OnPreloadScene3Pressed()
	{
		GD.Print("预加载场景3 (C#)");
		SetProcess(true);
		var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
		manager.PreloadSceneGD(TEST_SCENE_3_PATH);
		UpdateInfo();
	}

	private void OnPreloadScene4Pressed()
	{
		GD.Print("预加载场景4 (C#)");
		SetProcess(true);
		var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
		manager.PreloadSceneGD(TEST_SCENE_4_PATH);
		UpdateInfo();
	}

	private void OnPreloadScene5Pressed()
	{
		GD.Print("预加载场景5 (C#)");
		SetProcess(true);
		var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
		manager.PreloadSceneGD(TEST_SCENE_5_PATH);
		UpdateInfo();
	}

	// scene3切换函数
	private void OnSwitchScene3WithPreloadPressed()
	{
		GD.Print("使用预加载切换场景3 (C#)");
		var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
		manager.SwitchSceneGD(TEST_SCENE_3_PATH, true, "");
	}

	private void OnSwitchScene3DirectPressed()
	{
		GD.Print("直接加载场景3 (C#)");
		var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
		manager.SwitchSceneGD(TEST_SCENE_3_PATH, true, "");
	}

	// scene4切换函数
	private void OnSwitchScene4WithPreloadPressed()
	{
		GD.Print("使用预加载切换场景4 (C#)");
		var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
		manager.SwitchSceneGD(TEST_SCENE_4_PATH, true, "");
	}

	private void OnSwitchScene4DirectPressed()
	{
		GD.Print("直接加载场景4 (C#)");
		var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
		manager.SwitchSceneGD(TEST_SCENE_4_PATH, true, "");
	}

	// scene5切换函数
	private void OnSwitchScene5WithPreloadPressed()
	{
		GD.Print("使用预加载切换场景5 (C#)");
		var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
		manager.SwitchSceneGD(TEST_SCENE_5_PATH, true, "");
	}

	private void OnSwitchScene5DirectPressed()
	{
		GD.Print("直接加载场景5 (C#)");
		var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
		manager.SwitchSceneGD(TEST_SCENE_5_PATH, true, "");
	}

	private void OnSceneSwitchStarted(string fromScene, string toScene)
	{
		GD.Print($"场景2 - 切换开始 (C#): {fromScene} -> {toScene}");
	}

	private void OnSceneSwitchCompleted(string scenePath)
	{
		GD.Print($"场景2 - 切换完成 (C#): {scenePath}");
	}

	// 移除缓存功能
	private void OnRemovePreloadScene3Pressed()
	{
		GD.Print("移除预加载资源场景3 (C#)");
		var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
		manager.RemovePreloadedResource(TEST_SCENE_3_PATH);
		UpdateInfo();
	}

	private void OnRemoveCachedScene3Pressed()
	{
		GD.Print("移除实例化缓存场景3 (C#)");
		var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
		manager.RemoveCachedScene(TEST_SCENE_3_PATH);
		UpdateInfo();
	}

	private void OnRemovePreloadScene4Pressed()
	{
		GD.Print("移除预加载资源场景4 (C#)");
		var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
		manager.RemovePreloadedResource(TEST_SCENE_4_PATH);
		UpdateInfo();
	}

	private void OnRemoveCachedScene4Pressed()
	{
		GD.Print("移除实例化缓存场景4 (C#)");
		var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
		manager.RemoveCachedScene(TEST_SCENE_4_PATH);
		UpdateInfo();
	}

	private void OnRemovePreloadScene5Pressed()
	{
		GD.Print("移除预加载资源场景5 (C#)");
		var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
		manager.RemovePreloadedResource(TEST_SCENE_5_PATH);
		UpdateInfo();
	}

	private void OnRemoveCachedScene5Pressed()
	{
		GD.Print("移除实例化缓存场景5 (C#)");
		var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
		manager.RemoveCachedScene(TEST_SCENE_5_PATH);
		UpdateInfo();
	}
}

using Godot;
using System;

public partial class TestScene4 : Node3D
{
	private const string TEST_SCENE_2_PATH = "res://demo_test_scene_manager/test_scene_2.tscn";

	public override void _Ready()
	{
		GD.Print("=== Test Scene 4 Loaded 场景4已加载完成 ===");
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Q)
		{
			GD.Print("press Q to return scene2 按下Q键，返回场景2");
			_ = LongSceneManagerCs.Instance.SwitchScene(TEST_SCENE_2_PATH, LongSceneManagerCs.LoadMethod.BothPreloadFirst, true, "");
		}
	}
}

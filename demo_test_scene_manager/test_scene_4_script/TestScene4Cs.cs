using Godot;
using System;

public partial class TestScene4Cs : Node3D
{
	private const string TEST_SCENE_2_PATH = "res://demo_test_scene_manager/test_scene_2.tscn";

	public override void _Ready()
	{
		GD.Print("=== Test Scene 4 Loaded (C#) 测试场景4加载完成 (C#) ===");
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Q)
		{
			GD.Print("Press Q key, back to scene 2 (C#) 按下Q键，返回场景2 (C#)");
			var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
			manager.SwitchSceneGD(TEST_SCENE_2_PATH, true, "");
		}
	}
}

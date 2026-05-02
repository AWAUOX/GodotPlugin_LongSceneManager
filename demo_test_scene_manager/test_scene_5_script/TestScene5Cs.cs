using Godot;
using System;

public partial class TestScene5Cs : Node3D
{
	private const string TEST_SCENE_2_PATH = "res://demo_test_scene_manager/test_scene_2.tscn";

	public override void _Ready()
	{
		GD.Print("=== Test Scene 5 Loaded (C#) ===");
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Q)
		{
			GD.Print("按下Q键，返回场景2 (C#)");
			var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
			manager.SwitchSceneGD(TEST_SCENE_2_PATH, true, "");
		}
	}
}

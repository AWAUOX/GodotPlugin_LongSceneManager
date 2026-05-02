using Godot;
using System;

public partial class TestScene3Cs : Node3D
{
	private const string TEST_SCENE_2_PATH = "res://demo_test_scene_manager/test_scene_2.tscn";

	private Label timerLabel;
	private Timer timer;

	public override void _Ready()
	{
		GD.Print("=== Test Scene 3 Loaded (C#) ===");

		// 获取节点引用
		timerLabel = GetNode<Label>("Camera3D/CanvasLayer/VBoxContainer/HBoxContainer/TimerLabel");
		timer = GetNode<Timer>("Camera3D/CanvasLayer/VBoxContainer/HBoxContainer/TimerLabel/Timer");
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed)
		{
			if (keyEvent.Keycode == Key.Q)
			{
				GD.Print("按下Q键，返回场景2 (C#)");
				var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
				manager.SwitchSceneGD(TEST_SCENE_2_PATH, true, "");
			}

			if (keyEvent.Keycode == Key.E)
			{
				GD.Print("按下E键 (C#)");
				timer.Start();
			}
		}
	}

	public override void _Process(double delta)
	{
		// 更新计时器显示
		if (!timer.IsStopped())
		{
			timerLabel.Text = $"{timer.TimeLeft:F1}";
		}
		else
		{
			timerLabel.Text = "N/A";
		}
	}
}

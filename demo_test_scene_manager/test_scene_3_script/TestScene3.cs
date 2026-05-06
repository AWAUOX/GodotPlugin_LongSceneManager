using Godot;
using System;

public partial class TestScene3 : Node3D
{
	private const string TEST_SCENE_2_PATH = "res://demo_test_scene_manager/test_scene_2.tscn";
	private const string TEST_SCENE_3_PATH = "res://demo_test_scene_manager/test_scene_3.tscn";

	[Export]
	private Label _timerLabel;
	[Export]
	private Timer _timer;
	[Export]
	private Label _sceneSize;

	public override void _Ready()
	{
		GD.Print("=== Test Scene 3 Loaded 测试场景3加载完成 ===");

		_timerLabel.Text = "N/A";
		_sceneSize.Text = LongSceneManagerCs.Instance.GetResourceFileSizeFormatted(TEST_SCENE_3_PATH);
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed)
		{
			if (keyEvent.Keycode == Key.Q)
			{
				GD.Print("Press Q key, back to scene 2 按下Q键，返回场景2");
				_ = LongSceneManagerCs.Instance.SwitchScene(TEST_SCENE_2_PATH, LongSceneManagerCs.LoadMethod.BothPreloadFirst, true, "");
			}
			else if (keyEvent.Keycode == Key.E)
			{
				GD.Print("Press E key 按下E键");
				_timer.Start();
			}
		}
	}

	public override void _Process(double delta)
	{
		_timerLabel.Text = _timer.IsStopped() ? "N/A" : $"{_timer.TimeLeft:F1}";
	}
}

extends Node3D

const TEST_SCENE_2_PATH = "res://demo_test_scene_manager/test_scene_2.tscn"

@onready var timer_label: Label = $Camera3D/CanvasLayer/VBoxContainer/HBoxContainer/TimerLabel
@onready var timer: Timer = $Camera3D/CanvasLayer/VBoxContainer/HBoxContainer/TimerLabel/Timer



func _ready() -> void:
	print("=== Test Scene 3 Loaded 测试场景3加载完成 ===")

func _input(event: InputEvent) -> void:
	if event is InputEventKey and event.pressed and event.keycode == KEY_Q:
		print("Press Q key, back to scene 2 按下Q键，返回场景2")
		await LongSceneManager.switch_scene(TEST_SCENE_2_PATH, LongSceneManager.LoadMethod.BOTH_PRELOAD_FIRST, true, "")
	
	if event is InputEventKey and event.pressed and event.keycode == KEY_E:
		print("Press E key 按下E键")
		timer.start()
		
func _process(delta: float) -> void:
	timer_label.text = "%.1f" % timer.time_left if !timer.is_stopped() else "N/A"

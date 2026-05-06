extends Node3D

const TEST_SCENE_2_PATH = "res://demo_test_scene_manager/test_scene_2.tscn"

func _ready() -> void:
	print("=== Test Scene 4 Loaded 场景4已加载完成 ===")

func _input(event: InputEvent) -> void:
	if event is InputEventKey and event.pressed and event.keycode == KEY_Q:
		print("press Q to return scene2 按下Q键，返回场景2")
		await LongSceneManager.switch_scene(TEST_SCENE_2_PATH, LongSceneManager.LoadMethod.BOTH_PRELOAD_FIRST, true, "")


# Original GDScript version code 原始 GDScript 版本代码
# ========================
extends Node2D

const MAIN_SCENE_PATH = "res://demo_test_scene_manager/main_scene.tscn"
const TEST_SCENE_2_PATH = "res://demo_test_scene_manager/test_scene_2.tscn"

var is_first_enter:bool = true

@onready var button_main: Button = $MarginContainer/VBoxContainer/HBoxContainer/VBoxContainer/Button_Main
@onready var button_scene2: Button = $MarginContainer/VBoxContainer/HBoxContainer/VBoxContainer/Button_Scene2
@onready var button_back: Button = $MarginContainer/VBoxContainer/HBoxContainer/VBoxContainer/Button_Back
@onready var label_info: Label = $MarginContainer/VBoxContainer/HBoxContainer/Label_Info




func _ready():
	print("=== Test Scene 1 Loaded 测试场景1加载完成 ===")
	
	# Connect button signals 连接按钮信号
	button_main.pressed.connect(_on_main_pressed)
	button_scene2.pressed.connect(_on_scene2_pressed)
	button_back.pressed.connect(_on_back_pressed)
	
	# Update info label 更新信息标签
	_update_info_label()
	is_first_enter = false
	
	# Connect SceneManager signals 连接SceneManager信号
	LongSceneManager.scene_switch_started.connect(_on_scene_switch_started)
	LongSceneManager.scene_switch_completed.connect(_on_scene_switch_completed)
	
	
func _enter_tree() -> void:
	if not is_first_enter:
		_update_info_label()
		
func _process(delta: float) -> void:
	_update_info_label()

func _update_info_label():
	var cache_info = LongSceneManager.get_cache_info()

	# Process instance scene cache list 处理实例化场景缓存列表
	var instance_paths = []
	for s in cache_info.instance_cache.scenes:
		instance_paths.append(s.path)
	var instance_list = "\n".join(instance_paths) if not instance_paths.is_empty() else "（empty）"

	# Process preload resource cache list 处理预加载资源缓存列表
	var preload_list = "\n".join(cache_info.preload_cache.scenes) if not cache_info.preload_cache.scenes.is_empty() else "（empty）"

	label_info.text = """
Current Scene: {current}
Previous Scene: {previous}

[Instance Scene Cache] Count: {instance_count}/{instance_max}
Scene List:
{instance_list}

[Preloaded Resource Cache] Count: {preload_count}/{preload_max}
Resource List:
{preload_list}
""".format({
		"current": cache_info.current_scene,
		"previous": cache_info.previous_scene,
		"instance_count": cache_info.instance_cache.size,
		"instance_max": cache_info.instance_cache.max_size,
		"instance_list": instance_list,
		"preload_count": cache_info.preload_cache.size,
		"preload_max": cache_info.preload_cache.max_size,
		"preload_list": preload_list
	})
	

func _on_main_pressed():
	#"""Switch back to main scene 切换回主场景"""
	print("Switch back to main scene 切换回主场景")
	await LongSceneManager.switch_scene(MAIN_SCENE_PATH, LongSceneManager.LoadMethod.BOTH_PRELOAD_FIRST, true, "")

func _on_scene2_pressed():
	#"""Switch to scene 2 切换到场景2"""
	print("Switch to scene 2 切换到场景2")
	await LongSceneManager.switch_scene(TEST_SCENE_2_PATH, LongSceneManager.LoadMethod.BOTH_PRELOAD_FIRST, true, "")

func _on_back_pressed():
	#"""Back button (special test: no transition effect) 返回按钮（特殊测试：无过渡效果）"""
	print("Back to main scene (no transition) 返回主场景（无过渡效果）")
	await LongSceneManager.switch_scene(MAIN_SCENE_PATH, LongSceneManager.LoadMethod.BOTH_PRELOAD_FIRST, true, "no_transition")

func _on_scene_switch_started(from_scene: String, to_scene: String):
	print("Scene 1 - switch started 场景1 - 切换开始: ", from_scene, " -> ", to_scene)

func _on_scene_switch_completed(scene_path: String):
	print("Scene 1 - switch completed 场景1 - 切换完成: ", scene_path)

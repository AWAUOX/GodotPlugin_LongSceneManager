# Main scene script - used for testing scene switching 主场景脚本 - 用于测试场景切换

extends Control

	# Scene path constants 场景路径常量
const TEST_SCENE_1_PATH = "res://demo_test_scene_manager/test_scene_1.tscn"
const TEST_SCENE_2_PATH = "res://demo_test_scene_manager/test_scene_2.tscn"


@onready var button_scene_1: Button = $MarginContainer/VBoxContainer/HBoxContainer2/HBoxContainer/Button_Scene1
@onready var button_scene_2: Button = $MarginContainer/VBoxContainer/HBoxContainer2/HBoxContainer/Button_Scene2
@onready var button_preload_1: Button = $MarginContainer/VBoxContainer/HBoxContainer2/HBoxContainer/Button_Preload1
@onready var button_preload_2: Button = $MarginContainer/VBoxContainer/HBoxContainer2/HBoxContainer/Button_Preload2
@onready var button_clear_cache: Button = $MarginContainer/VBoxContainer/HBoxContainer2/HBoxContainer/Button_ClearCache
@onready var label_info: Label = $MarginContainer/VBoxContainer/HBoxContainer2/ScrollContainer/VBoxContainer/Label_Info




var is_first_enter:bool = true

func _ready():
	print("=== Main Scene Loaded 主场景加载完成 ===")
	
	# Connect button signals 连接按钮信号
	button_scene_1.pressed.connect(_on_scene1_pressed)
	button_scene_2.pressed.connect(_on_scene2_pressed)
	button_preload_1.pressed.connect(_on_preload1_pressed)
	button_preload_2.pressed.connect(_on_preload2_pressed)
	button_clear_cache.pressed.connect(_on_clear_cache_pressed)
	
	# Update info label 更新信息标签
	_update_info_label()
	is_first_enter = false
	
	# Connect SceneManager signals 连接SceneManager信号
	LongSceneManager.scene_switch_started.connect(_on_scene_switch_started)
	LongSceneManager.scene_switch_completed.connect(_on_scene_switch_completed)
	LongSceneManager.scene_cached.connect(_on_scene_cached)
	
func _enter_tree() -> void:
	if not is_first_enter:
		_update_info_label()
		
func _process(delta:float) -> void:
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

	# Process permanent preload resource cache list 处理永久预加载资源缓存列表
	var permanent_preload_list = "\n".join(cache_info.permanent_preload_cache.scenes) if not cache_info.permanent_preload_cache.scenes.is_empty() else "（empty）"

	label_info.text = """
Current Scene: {current}
Previous Scene: {previous}

[Instance Scene Cache] Count: {instance_count}/{instance_max}
Scene List:
{instance_list}

[Preloaded Resource Cache] Count: {preload_count}/{preload_max}
Resource List:
{preload_list}

[Permanent Preload Cache] Count: {permanent_count}/{permanent_max}
Resource List:
{permanent_preload_list}
""".format({
		"current": cache_info.current_scene,
		"previous": cache_info.previous_scene,
		"instance_count": cache_info.instance_cache.size,
		"instance_max": cache_info.instance_cache.max_size,
		"instance_list": instance_list,
		"preload_count": cache_info.preload_cache.size,
		"preload_max": cache_info.preload_cache.max_size,
		"preload_list": preload_list,
		"permanent_count": cache_info.permanent_preload_cache.size,
		"permanent_max": cache_info.permanent_preload_cache.max_size,
		"permanent_preload_list": permanent_preload_list
	})

func _on_scene1_pressed():
	##"Switch to scene 1 (using default loading screen) 切换到场景1（使用默认加载屏幕）"""
	print("Switch to scene 1 切换到场景1")
	await LongSceneManager.switch_scene(TEST_SCENE_1_PATH,LongSceneManager.LoadMethod.PRELOAD_CACHE, true, "")

func _on_scene2_pressed():
	##"Switch to scene 2 (using default loading screen) 切换到场景2（使用默认加载屏幕）"""
	print("Switch to scene 2 切换到场景2")
	await LongSceneManager.switch_scene(TEST_SCENE_2_PATH,LongSceneManager.LoadMethod.PRELOAD_CACHE, true, "")

func _on_preload1_pressed():
	##"Preload scene 1 预加载场景1"""
	print("Preload scene 1 预加载场景1")
	LongSceneManager.preload_scene(TEST_SCENE_1_PATH)
	#LongSceneManager.print_debug_info()
	_update_info_label()

func _on_preload2_pressed():
	##"Preload scene 2 预加载场景2"""
	print("Preload scene 2 预加载场景2")
	LongSceneManager.preload_scene(TEST_SCENE_2_PATH)
	#LongSceneManager.print_debug_info()
	_update_info_label()

func _on_clear_cache_pressed():
	##"Clear cache 清空缓存"""
	print("Clear cache 清空缓存")
	LongSceneManager.clear_cache()
	#LongSceneManager.print_debug_info()
	_update_info_label()

func _on_scene_switch_started(from_scene: String, to_scene: String):
	print("Scene switch started 场景切换开始: ", from_scene, " -> ", to_scene)

func _on_scene_switch_completed(scene_path: String):
	print("Scene switch completed 场景切换完成: ", scene_path)

func _on_scene_cached(scene_path: String):
	print("Scene cached 场景已缓存: ", scene_path)
	_update_info_label()

extends Control

# Keep all scene path constants 保留所有场景路径常量
const MAIN_SCENE_PATH = "res://demo_test_scene_manager/main_scene.tscn"
const TEST_SCENE_1_PATH = "res://demo_test_scene_manager/test_scene_1.tscn"
const TEST_SCENE_3_PATH = "res://demo_test_scene_manager/test_scene_3.tscn"
const TEST_SCENE_4_PATH = "res://demo_test_scene_manager/test_scene_4.tscn"
const TEST_SCENE_5_PATH = "res://demo_test_scene_manager/test_scene_5.tscn"
const CUSTOM_LOADING_SCENE = "res://demo_test_scene_manager/custom_load_screen/custom_load_screen.tscn"

# Button declarations (including new scene4/scene5 switch buttons) 按钮声明（含新增的scene4/scene5切换按钮）
@onready var button_main: Button = $VBoxContainer/HBoxContainer/VBoxContainer/HBoxContainer/Button_Main
@onready var button_scene1: Button = $VBoxContainer/HBoxContainer/VBoxContainer/HBoxContainer/Button_Scene1


@onready var button_preload_scene_3: Button = $VBoxContainer/HBoxContainer/VBoxContainer/scene3/Button_PreloadScene3
@onready var button_switch_scene_3_with_preload: Button = $VBoxContainer/HBoxContainer/VBoxContainer/scene3/Button_SwitchScene3WithPreload
@onready var button_switch_scene_3_direct: Button = $VBoxContainer/HBoxContainer/VBoxContainer/scene3/Button_SwitchScene3Direct
@onready var button_preload_scene_4: Button = $VBoxContainer/HBoxContainer/VBoxContainer/scene4/Button_PreloadScene4
@onready var button_switch_scene_4_with_preload: Button = $VBoxContainer/HBoxContainer/VBoxContainer/scene4/Button_SwitchScene4WithPreload
@onready var button_switch_scene_4_direct: Button = $VBoxContainer/HBoxContainer/VBoxContainer/scene4/Button_SwitchScene4Direct
@onready var button_preload_scene_5: Button = $VBoxContainer/HBoxContainer/VBoxContainer/Scene5/Button_PreloadScene5
@onready var button_switch_scene_5_with_preload: Button = $VBoxContainer/HBoxContainer/VBoxContainer/Scene5/Button_SwitchScene5WithPreload
@onready var button_switch_scene_5_direct: Button = $VBoxContainer/HBoxContainer/VBoxContainer/Scene5/Button_SwitchScene5Direct

@onready var button_remove_preload_scene_3: Button = $VBoxContainer/HBoxContainer/VBoxContainer/Scene3RemoveCache/RemoveScene3Resource
@onready var button_remove_cached_scene_3: Button = $VBoxContainer/HBoxContainer/VBoxContainer/Scene3RemoveCache/RemoveScene3Instance
@onready var button_remove_preload_scene_4: Button = $VBoxContainer/HBoxContainer/VBoxContainer/Scene4RemoveCache/RemoveScene4Resource
@onready var button_remove_cached_scene_4: Button = $VBoxContainer/HBoxContainer/VBoxContainer/Scene4RemoveCache/RemoveScene4Instance
@onready var button_remove_preload_scene_5: Button = $VBoxContainer/HBoxContainer/VBoxContainer/Scene5RemoveCache/RemoveScene5Resource
@onready var button_remove_cached_scene_5: Button = $VBoxContainer/HBoxContainer/VBoxContainer/Scene5RemoveCache/RemoveScene5Instance




@onready var label_info: Label = $VBoxContainer/HBoxContainer/VBoxContainer2/Label_Info
@onready var progress_bar_preload_scene_3: ProgressBar = $VBoxContainer/MarginContainer/VBoxContainer/scene3/ProgressBar_PreloadScene3
@onready var progress_bar_preload_scene_4: ProgressBar = $VBoxContainer/MarginContainer/VBoxContainer/scene4/ProgressBar_PreloadScene4
@onready var progress_bar_preload_scene_5: ProgressBar = $VBoxContainer/MarginContainer/VBoxContainer/scene5/ProgressBar_PreloadScene5

var is_first_enter:bool = true

func _ready():
	print("=== Test Scene 2 Loaded 测试场景2加载完成 ===")
	set_process(false)
	# Connect all button signals (including scene4/scene5 switch buttons) 连接所有按钮信号（补scene4/scene5切换按钮连接）
	button_main.pressed.connect(_on_main_pressed)
	button_scene1.pressed.connect(_on_scene1_pressed)
	button_preload_scene_3.pressed.connect(_on_preload_scene_3_pressed)
	button_preload_scene_4.pressed.connect(_on_preload_scene_4_pressed)
	button_preload_scene_5.pressed.connect(_on_preload_scene_5_pressed)
	button_switch_scene_3_with_preload.pressed.connect(_on_switch_scene_3_with_preload_pressed)
	button_switch_scene_3_direct.pressed.connect(_on_switch_scene_3_direct_pressed)
	# New scene4/scene5 switch button connections 新增scene4/scene5切换按钮连接
	button_switch_scene_4_with_preload.pressed.connect(_on_switch_scene_4_with_preload_pressed)
	button_switch_scene_4_direct.pressed.connect(_on_switch_scene_4_direct_pressed)
	button_switch_scene_5_with_preload.pressed.connect(_on_switch_scene_5_with_preload_pressed)
	button_switch_scene_5_direct.pressed.connect(_on_switch_scene_5_direct_pressed)
	# Connect remove cache button signals 连接移除缓存按钮信号
	button_remove_preload_scene_3.pressed.connect(_on_remove_preload_scene_3_pressed)
	button_remove_cached_scene_3.pressed.connect(_on_remove_cached_scene_3_pressed)
	button_remove_preload_scene_4.pressed.connect(_on_remove_preload_scene_4_pressed)
	button_remove_cached_scene_4.pressed.connect(_on_remove_cached_scene_4_pressed)
	button_remove_preload_scene_5.pressed.connect(_on_remove_preload_scene_5_pressed)
	button_remove_cached_scene_5.pressed.connect(_on_remove_cached_scene_5_pressed)
	is_first_enter = false
	_update_info()
	
	# 连接SceneManager信号
	LongSceneManager.scene_switch_started.connect(_on_scene_switch_started)
	LongSceneManager.scene_switch_completed.connect(_on_scene_switch_completed)

func _enter_tree() -> void:
	set_process(false)
	if not is_first_enter:
		_update_info()

func _process(delta):
	_update_info()
	# Update preload progress for three scenes 更新三个场景的预加载进度
	var scene3_progress = LongSceneManager.get_loading_progress(TEST_SCENE_3_PATH)
	var scene4_progress = LongSceneManager.get_loading_progress(TEST_SCENE_4_PATH)
	var scene5_progress = LongSceneManager.get_loading_progress(TEST_SCENE_5_PATH)
	
	# Scene 3 progress 场景3进度
	if scene3_progress > 0 and scene3_progress < 1.0:
		progress_bar_preload_scene_3.value = scene3_progress * 100
		#label_info.text = "Preload scene 3 progress: 预加载场景3进度: " + str(round(scene3_progress * 100)) + "%"
	elif scene3_progress >= 1.0:
		progress_bar_preload_scene_3.value = 100
		#label_info.text = "Scene 3 preload completed 场景3预加载完成"
	else:
		progress_bar_preload_scene_3.value = 0
	
	# Scene 4 progress 场景4进度
	if scene4_progress > 0 and scene4_progress < 1.0:
		progress_bar_preload_scene_4.value = scene4_progress * 100
		#label_info.text = "Preload scene 4 progress: 预加载场景4进度: " + str(round(scene4_progress * 100)) + "%"
	elif scene4_progress >= 1.0:
		progress_bar_preload_scene_4.value = 100
		#label_info.text = "Scene 4 preload completed 场景4预加载完成"
	else:
		progress_bar_preload_scene_4.value = 0
	
	# Scene 5 progress 场景5进度
	if scene5_progress > 0 and scene5_progress < 1.0:
		progress_bar_preload_scene_5.value = scene5_progress * 100
		#label_info.text = "Preload scene 5 progress: 预加载场景5进度: " + str(round(scene5_progress * 100)) + "%"
	elif scene5_progress >= 1.0:
		progress_bar_preload_scene_5.value = 100
		#label_info.text = "Scene 5 preload completed 场景5预加载完成"
	else:
		progress_bar_preload_scene_5.value = 0

func _update_info():
	var cache_info = LongSceneManager.get_cache_info()
	progress_bar_preload_scene_3.value = 0
	progress_bar_preload_scene_4.value = 0
	progress_bar_preload_scene_5.value = 0

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

# Original switch functions 原有切换函数
func _on_main_pressed():
	print("Switch back to main scene 切换回主场景")
	await LongSceneManager.switch_scene(MAIN_SCENE_PATH, LongSceneManager.LoadMethod.BOTH_PRELOAD_FIRST, true, "")

func _on_scene1_pressed():
	print("Switch to scene 1 切换到场景1")
	await LongSceneManager.switch_scene(TEST_SCENE_1_PATH, LongSceneManager.LoadMethod.BOTH_PRELOAD_FIRST, true, "")

# Preload functions (original) 预加载函数（原有）
func _on_preload_scene_3_pressed():
	print("Preload scene 3 预加载场景3")
	set_process(true)
	LongSceneManager.preload_scene(TEST_SCENE_3_PATH)
	_update_info()

func _on_preload_scene_4_pressed():
	print("Preload scene 4 预加载场景4")
	set_process(true)
	LongSceneManager.preload_scene(TEST_SCENE_4_PATH)
	_update_info()

func _on_preload_scene_5_pressed():
	print("Preload scene 5 预加载场景5")
	set_process(true)
	LongSceneManager.preload_scene(TEST_SCENE_5_PATH)
	_update_info()

# scene3 switch functions (original) scene3切换函数（原有）
func _on_switch_scene_3_with_preload_pressed():
	print("Switch scene 3 with preload 使用预加载切换场景3")
	await LongSceneManager.switch_scene(TEST_SCENE_3_PATH, LongSceneManager.LoadMethod.BOTH_PRELOAD_FIRST, true, "")

func _on_switch_scene_3_direct_pressed():
	print("Direct load scene 3 直接加载场景3")
	await LongSceneManager.switch_scene(TEST_SCENE_3_PATH, LongSceneManager.LoadMethod.DIRECT, true, CUSTOM_LOADING_SCENE)

# ============== New scene4 switch functions ============== 新增scene4切换函数 ==============
func _on_switch_scene_4_with_preload_pressed():
	# Switch scene 4 with preload (same logic as scene3) 使用预加载切换场景4（和scene3逻辑完全一致）
	print("Switch scene 4 with preload 使用预加载切换场景4")
	await LongSceneManager.switch_scene(TEST_SCENE_4_PATH, LongSceneManager.LoadMethod.BOTH_PRELOAD_FIRST, true, "")

func _on_switch_scene_4_direct_pressed():
	# Direct load scene 4 (no preload) 直接加载场景4（不预加载）
	print("Direct load scene 4 直接加载场景4")
	await LongSceneManager.switch_scene(TEST_SCENE_4_PATH, LongSceneManager.LoadMethod.DIRECT, true, CUSTOM_LOADING_SCENE)

# ============== New scene5 switch functions ============== 新增scene5切换函数 ==============
func _on_switch_scene_5_with_preload_pressed():
	# Switch scene 5 with preload 使用预加载切换场景5
	print("Switch scene 5 with preload 使用预加载切换场景5")
	await LongSceneManager.switch_scene(TEST_SCENE_5_PATH, LongSceneManager.LoadMethod.BOTH_PRELOAD_FIRST, true, "")

func _on_switch_scene_5_direct_pressed():
	# Direct load scene 5 直接加载场景5
	print("Direct load scene 5 直接加载场景5")
	await LongSceneManager.switch_scene(TEST_SCENE_5_PATH, LongSceneManager.LoadMethod.DIRECT, true, CUSTOM_LOADING_SCENE)

func _on_scene_switch_started(from_scene: String, to_scene: String):
	print("Scene1-switch start 场景2-切换开始: ", from_scene, " -> ", to_scene)

func _on_scene_switch_completed(scene_path: String):
	print("Scene2-switch start 场景2-切换完成: ", scene_path)

# ==============remove cache 移除缓存功能 ==============
func _on_remove_preload_scene_3_pressed():
	print("remove preload resource scene3 移除预加载资源场景3")
	LongSceneManager.remove_preloaded_resource(TEST_SCENE_3_PATH)
	_update_info()

func _on_remove_cached_scene_3_pressed():
	print("remove instate scene3 移除实例化缓存场景3")
	LongSceneManager.remove_cached_scene(TEST_SCENE_3_PATH)
	_update_info()

func _on_remove_preload_scene_4_pressed():
	print("remove preload resource scene4 移除预加载资源场景4")
	LongSceneManager.remove_preloaded_resource(TEST_SCENE_4_PATH)
	_update_info()

func _on_remove_cached_scene_4_pressed():
	print("remove instate scene4 移除实例化缓存场景4")
	LongSceneManager.remove_cached_scene(TEST_SCENE_4_PATH)
	_update_info()

func _on_remove_preload_scene_5_pressed():
	print("remove preload resource scene5 移除预加载资源场景5")
	LongSceneManager.remove_preloaded_resource(TEST_SCENE_5_PATH)
	_update_info()

func _on_remove_cached_scene_5_pressed():
	print("remove instate scene5 移除实例化缓存场景5")
	LongSceneManager.remove_cached_scene(TEST_SCENE_5_PATH)
	_update_info()


func _on_preload_all_pressed() -> void:
	pass # Replace with function body.


func _on_cancel_all_preload_pressed() -> void:
	pass # Replace with function body.


func _on_cancel_preload_scene_3_pressed() -> void:
	pass # Replace with function body.

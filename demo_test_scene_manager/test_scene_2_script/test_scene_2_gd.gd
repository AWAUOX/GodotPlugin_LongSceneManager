extends Control

# 保留所有场景路径常量
const MAIN_SCENE_PATH = "res://demo_test_scene_manager/main_scene.tscn"
const TEST_SCENE_1_PATH = "res://demo_test_scene_manager/test_scene_1.tscn"
const TEST_SCENE_3_PATH = "res://demo_test_scene_manager/test_scene_3.tscn"
const TEST_SCENE_4_PATH = "res://demo_test_scene_manager/test_scene_4.tscn"
const TEST_SCENE_5_PATH = "res://demo_test_scene_manager/test_scene_5.tscn"

# 按钮声明（含新增的scene4/scene5切换按钮）
@onready var button_main: Button = $VBoxContainer/HBoxContainer/VBoxContainer/Button_Main
@onready var button_scene1: Button = $VBoxContainer/HBoxContainer/VBoxContainer/Button_Scene1

@onready var button_preload_scene_3: Button = $VBoxContainer/HBoxContainer/VBoxContainer/scene3/Button_PreloadScene3
@onready var button_switch_scene_3_with_preload: Button = $VBoxContainer/HBoxContainer/VBoxContainer/scene3/Button_SwitchScene3WithPreload
@onready var button_switch_scene_3_direct: Button = $VBoxContainer/HBoxContainer/VBoxContainer/scene3/Button_SwitchScene3Direct
@onready var button_preload_scene_4: Button = $VBoxContainer/HBoxContainer/VBoxContainer/scene4/Button_PreloadScene4
@onready var button_switch_scene_4_with_preload: Button = $VBoxContainer/HBoxContainer/VBoxContainer/scene4/Button_SwitchScene4WithPreload
@onready var button_switch_scene_4_direct: Button = $VBoxContainer/HBoxContainer/VBoxContainer/scene4/Button_SwitchScene4Direct
@onready var button_preload_scene_5: Button = $VBoxContainer/HBoxContainer/VBoxContainer/Scene5/Button_PreloadScene5
@onready var button_switch_scene_5_with_preload: Button = $VBoxContainer/HBoxContainer/VBoxContainer/Scene5/Button_SwitchScene5WithPreload
@onready var button_switch_scene_5_direct: Button = $VBoxContainer/HBoxContainer/VBoxContainer/Scene5/Button_SwitchScene5Direct




@onready var label: Label = $VBoxContainer/HBoxContainer/VBoxContainer2/Label
@onready var label_info: Label = $VBoxContainer/HBoxContainer/VBoxContainer2/Label_Info
@onready var progress_bar_preload_scene_3: ProgressBar = $VBoxContainer/VBoxContainer/scene3/ProgressBar_PreloadScene3
@onready var progress_bar_preload_scene_4: ProgressBar = $VBoxContainer/VBoxContainer/scene4/ProgressBar_PreloadScene4
@onready var progress_bar_preload_scene_5: ProgressBar = $VBoxContainer/VBoxContainer/scene5/ProgressBar_PreloadScene5

var is_first_enter:bool = true

func _ready():
	print("=== Test Scene 2 Loaded ===")
	set_process(false)
	# 连接所有按钮信号（补scene4/scene5切换按钮连接）
	button_main.pressed.connect(_on_main_pressed)
	button_scene1.pressed.connect(_on_scene1_pressed)
	button_preload_scene_3.pressed.connect(_on_preload_scene_3_pressed)
	button_preload_scene_4.pressed.connect(_on_preload_scene_4_pressed)
	button_preload_scene_5.pressed.connect(_on_preload_scene_5_pressed)
	button_switch_scene_3_with_preload.pressed.connect(_on_switch_scene_3_with_preload_pressed)
	button_switch_scene_3_direct.pressed.connect(_on_switch_scene_3_direct_pressed)
	# 新增scene4/scene5切换按钮连接
	button_switch_scene_4_with_preload.pressed.connect(_on_switch_scene_4_with_preload_pressed)
	button_switch_scene_4_direct.pressed.connect(_on_switch_scene_4_direct_pressed)
	button_switch_scene_5_with_preload.pressed.connect(_on_switch_scene_5_with_preload_pressed)
	button_switch_scene_5_direct.pressed.connect(_on_switch_scene_5_direct_pressed)
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
	# 更新三个场景的预加载进度
	var scene3_progress = LongSceneManager.get_loading_progress(TEST_SCENE_3_PATH)
	var scene4_progress = LongSceneManager.get_loading_progress(TEST_SCENE_4_PATH)
	var scene5_progress = LongSceneManager.get_loading_progress(TEST_SCENE_5_PATH)
	
	# 场景3进度
	if scene3_progress > 0 and scene3_progress < 1.0:
		progress_bar_preload_scene_3.value = scene3_progress * 100
		label_info.text = "预加载场景3进度: " + str(round(scene3_progress * 100)) + "%"
	elif scene3_progress >= 1.0:
		progress_bar_preload_scene_3.value = 100
		label_info.text = "场景3预加载完成"
	else:
		progress_bar_preload_scene_3.value = 0
	
	# 场景4进度
	if scene4_progress > 0 and scene4_progress < 1.0:
		progress_bar_preload_scene_4.value = scene4_progress * 100
		label_info.text = "预加载场景4进度: " + str(round(scene4_progress * 100)) + "%"
	elif scene4_progress >= 1.0:
		progress_bar_preload_scene_4.value = 100
		label_info.text = "场景4预加载完成"
	else:
		progress_bar_preload_scene_4.value = 0
	
	# 场景5进度
	if scene5_progress > 0 and scene5_progress < 1.0:
		progress_bar_preload_scene_5.value = scene5_progress * 100
		label_info.text = "预加载场景5进度: " + str(round(scene5_progress * 100)) + "%"
	elif scene5_progress >= 1.0:
		progress_bar_preload_scene_5.value = 100
		label_info.text = "场景5预加载完成"
	else:
		progress_bar_preload_scene_5.value = 0

func _update_info():
	var cache_info = LongSceneManager.get_cache_info()
	progress_bar_preload_scene_3.value = 0
	progress_bar_preload_scene_4.value = 0
	progress_bar_preload_scene_5.value = 0
	
	label_info.text = """
     上一个场景: {previous}
     缓存实例场景数: {cache_count}/{cache_max}
 	 缓存最大数值: {cache_max}
     缓存实例场景列表: {cache_list}
 	 预加载资源缓存数量: {preload_cache_size}
 	 预加载缓存最大数值: {preload_cache_max}
 	""".format({
 		"previous": LongSceneManager.get_previous_scene_path(),
 		"cache_count": cache_info.instance_cache_size,
 		"cache_max": cache_info.max_size,
 		"cache_list": ",\n ".join(cache_info.access_order),
 		"preload_cache_size": LongSceneManager.preload_resource_cache.size(),
 		"preload_cache_max": LongSceneManager.max_preload_resource_cache_size
 	})

# 原有切换函数
func _on_main_pressed():
	print("切换回主场景")
	await LongSceneManager.switch_scene(MAIN_SCENE_PATH, true, "")

func _on_scene1_pressed():
	print("切换到场景1")
	await LongSceneManager.switch_scene(TEST_SCENE_1_PATH, true, "")

# 预加载函数（原有）
func _on_preload_scene_3_pressed():
	print("预加载场景3")
	set_process(true)
	LongSceneManager.preload_scene(TEST_SCENE_3_PATH)
	_update_info()

func _on_preload_scene_4_pressed():
	print("预加载场景4")
	set_process(true)
	LongSceneManager.preload_scene(TEST_SCENE_4_PATH)
	_update_info()

func _on_preload_scene_5_pressed():
	print("预加载场景5")
	set_process(true)
	LongSceneManager.preload_scene(TEST_SCENE_5_PATH)
	_update_info()

# scene3切换函数（原有）
func _on_switch_scene_3_with_preload_pressed():
	print("使用预加载切换场景3")
	await LongSceneManager.switch_scene(TEST_SCENE_3_PATH, true, "")

func _on_switch_scene_3_direct_pressed():
	print("直接加载场景3")
	await LongSceneManager.switch_scene(TEST_SCENE_3_PATH, true, "")

# ============== 新增scene4切换函数 ==============
func _on_switch_scene_4_with_preload_pressed():
	# 使用预加载切换场景4（和scene3逻辑完全一致）
	print("使用预加载切换场景4")
	await LongSceneManager.switch_scene(TEST_SCENE_4_PATH, true, "")

func _on_switch_scene_4_direct_pressed():
	# 直接加载场景4（不预加载）
	print("直接加载场景4")
	await LongSceneManager.switch_scene(TEST_SCENE_4_PATH, true, "")

# ============== 新增scene5切换函数 ==============
func _on_switch_scene_5_with_preload_pressed():
	# 使用预加载切换场景5
	print("使用预加载切换场景5")
	await LongSceneManager.switch_scene(TEST_SCENE_5_PATH, true, "")

func _on_switch_scene_5_direct_pressed():
	# 直接加载场景5
	print("直接加载场景5")
	await LongSceneManager.switch_scene(TEST_SCENE_5_PATH, true, "")

func _on_scene_switch_started(from_scene: String, to_scene: String):
	print("场景2 - 切换开始: ", from_scene, " -> ", to_scene)

func _on_scene_switch_completed(scene_path: String):
	print("场景2 - 切换完成: ", scene_path)

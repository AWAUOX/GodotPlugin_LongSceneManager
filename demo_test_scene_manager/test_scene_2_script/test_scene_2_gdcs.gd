# 测试场景2脚本 (C#版本接口)

extends Node2D

const MAIN_SCENE_PATH = "res://demo_test_scene_manager/main_scene.tscn"
const TEST_SCENE_1_PATH = "res://demo_test_scene_manager/test_scene_1.tscn"

@onready var button_main: Button = $VBoxContainer/Button_Main
@onready var button_scene1: Button = $VBoxContainer/Button_Scene1
@onready var button_preload_main: Button = $VBoxContainer/Button_PreloadMain
@onready var label_info: Label = $VBoxContainer/Label_Info
@onready var progress_bar: ProgressBar = $ProgressBar



var is_first_enter:bool = true


func _ready():
	print("=== Test Scene 2 Loaded (C# Interface) ===")
	set_process(false)
	# 连接按钮信号
	button_main.pressed.connect(_on_main_pressed)
	button_scene1.pressed.connect(_on_scene1_pressed)
	button_preload_main.pressed.connect(_on_preload_main_pressed)
	is_first_enter = false
	# 更新信息
	_update_info()
	
	# 连接SceneManager信号
	LongSceneManagerCs.SceneSwitchStarted.connect(_on_scene_switch_started)
	LongSceneManagerCs.SceneSwitchCompleted.connect(_on_scene_switch_completed)
	LongSceneManagerCs.ScenePreloadCompleted.connect(_on_scene_preload_completed)
	
func _enter_tree() -> void:
	set_process(false)
	if not is_first_enter:
		_update_info()

func _process(delta):
	_update_info()
	#"每帧更新预加载进度"""
	var progress = LongSceneManagerCs.GetLoadingProgress(MAIN_SCENE_PATH)
	progress_bar.value = progress * 100
	
	if progress < 1.0 and progress > 0:
		label_info.text = "预加载主场景进度: " + str(round(progress * 100)) + "%"

func _update_info():
	#LongSceneManagerCs.PrintDebugInfo()
	#"更新显示信息"""
	var cache_info = LongSceneManagerCs.GetCacheInfo()
	progress_bar.value = 0
	
	label_info.text = """
    上一个场景: {previous}
    缓存实例场景数: {cache_count}/{cache_max}
	缓存最大数值: {cache_max}
    缓存实例场景列表: {cache_list}
	预加载资源缓存数量: {preload_cache_size}
	预加载缓存最大数值: {preload_cache_max}
	""".format({
		"previous": LongSceneManagerCs.GetPreviousScenePath(),
		"cache_count": cache_info.instance_cache_size,
		"cache_max": cache_info.max_size,
		"cache_list": ",\n ".join(cache_info.access_order),
		"preload_cache_size": cache_info.preload_resource_cache.size(),
		"preload_cache_max": cache_info.max_preload_resource_cache_size
	})

func _on_main_pressed():
	#"切换回主场景"""
	print("切换回主场景 (C# Interface)")
	LongSceneManagerCs.SwitchSceneGD(MAIN_SCENE_PATH, true, "")

func _on_scene1_pressed():
	#"切换到场景1"""
	print("切换到场景1 (C# Interface)")
	LongSceneManagerCs.SwitchSceneGD(TEST_SCENE_1_PATH, true, "")

func _on_preload_main_pressed():
	#"预加载主场景"""
	print("预加载主场景 (C# Interface)")
	set_process(true)
	LongSceneManagerCs.PreloadSceneGD(MAIN_SCENE_PATH)

func _on_scene_switch_started(from_scene: String, to_scene: String):
	print("场景2 - 切换开始 (C# Interface): ", from_scene, " -> ", to_scene)

func _on_scene_switch_completed(scene_path: String):
	print("场景2 - 切换完成 (C# Interface): ", scene_path)

func _on_scene_preload_completed(scene_path: String):
	print("场景预加载完成 (C# Interface): ", scene_path)
	_update_info()

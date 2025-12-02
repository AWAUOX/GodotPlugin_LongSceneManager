@tool
extends EditorPlugin

const AUTOLOAD_NAME = "LongSceneManager"

func _enable_plugin() -> void:
	# 添加全局单例
	add_autoload_singleton(
		AUTOLOAD_NAME,
        "res://addons/basic_scene_manager/autoload/scene_manager.gd"
	)

func _disable_plugin() -> void:
	# 移除全局单例
	remove_autoload_singleton(AUTOLOAD_NAME)

func _enter_tree() -> void:
	# 注册自定义节点
	add_custom_type(
		"LongSceneManager",
		"Node",
		preload("res://addons/basic_scene_manager/autoload/scene_manager.gd"),
		preload("res://icon.svg")
	)

func _exit_tree() -> void:
	# 移除自定义节点
	remove_custom_type("LongSceneManager")

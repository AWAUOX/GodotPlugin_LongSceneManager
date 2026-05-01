@tool
extends EditorPlugin

const AUTOLOAD_NAME = "LongSceneManager"

func _enable_plugin() -> void:
	add_autoload_singleton(
		AUTOLOAD_NAME,
        "res://addons/long_scene_manager/autoload/long_scene_manager.gd"
    )

func _disable_plugin() -> void:
	remove_autoload_singleton(AUTOLOAD_NAME)

func _enter_tree() -> void:
	var icon = get_editor_interface().get_editor_theme().get_icon("Node", "EditorIcons")
	add_custom_type(
		"LongSceneManager",
		"Node",
		preload("res://addons/long_scene_manager/autoload/long_scene_manager.gd"),
		icon
	)

func _exit_tree() -> void:
	remove_custom_type("LongSceneManager")

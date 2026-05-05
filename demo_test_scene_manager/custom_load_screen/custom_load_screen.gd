extends Control

signal fade_in_started
signal fade_in_completed
signal fade_out_started
signal fade_out_completed

@export_category("过渡设置")
@export var fade_in_duration: float = 0.3
@export var fade_out_duration: float = 0.3
@export var fade_in_ease: Tween.EaseType = Tween.EASE_OUT
@export var fade_in_trans: Tween.TransitionType = Tween.TRANS_QUAD
@export var fade_out_ease: Tween.EaseType = Tween.EASE_IN
@export var fade_out_trans: Tween.TransitionType = Tween.TRANS_QUAD

var tween: Tween
var is_transitioning: bool = false

var _target_scene_path: String = ""
var _is_switching: bool = false

func _ready():
	anchors_preset = Control.PRESET_FULL_RECT
	z_index = 1000
	mouse_filter = Control.MOUSE_FILTER_STOP
	
	modulate.a = 0
	visible = false
	
	var progress_bar = $ColorRect/MarginContainer/VBoxContainer/ProgressBar
	if progress_bar:
		progress_bar.value = 0
	
	set_process(false)
	
	if LongSceneManager:
		LongSceneManager.scene_switch_started.connect(_on_scene_switch_started)
		LongSceneManager.scene_switch_completed.connect(_on_scene_switch_completed)

func _get_progress_bar() -> ProgressBar:
	return $ColorRect/MarginContainer/VBoxContainer/ProgressBar

func set_progress(progress: float) -> void:
	var progress_bar = _get_progress_bar()
	if progress_bar:
		progress_bar.value = progress * 100

func update_progress(progress: float) -> void:
	set_progress(progress)

func fade_in() -> void:
	if is_transitioning:
		_stop_current_tween()
	
	is_transitioning = true
	fade_in_started.emit()
	
	visible = true
	modulate.a = 0
	
	tween = create_tween()
	tween.set_ease(fade_in_ease)
	tween.set_trans(fade_in_trans)
	tween.tween_property(self, "modulate:a", 1.0, fade_in_duration)
	
	await tween.finished
	is_transitioning = false
	fade_in_completed.emit()

func fade_out() -> void:
	if is_transitioning:
		_stop_current_tween()
	
	is_transitioning = true
	fade_out_started.emit()
	
	tween = create_tween()
	tween.set_ease(fade_out_ease)
	tween.set_trans(fade_out_trans)
	tween.tween_property(self, "modulate:a", 0.0, fade_out_duration)
	
	await tween.finished
	visible = false
	is_transitioning = false
	fade_out_completed.emit()

func _stop_current_tween() -> void:
	if tween and tween.is_valid():
		tween.kill()
	is_transitioning = false

func set_immediate_visible(visible: bool) -> void:
	_stop_current_tween()
	self.visible = visible
	modulate.a = 1.0 if visible else 0.0

func _notification(what: int) -> void:
	if what == NOTIFICATION_WM_SIZE_CHANGED:
		var color_rect = $ColorRect
		if color_rect:
			color_rect.size = get_viewport().get_visible_rect().size

func _on_scene_switch_started(from_scene: String, to_scene: String) -> void:
	_target_scene_path = to_scene
	_is_switching = true
	set_process(true)

func _process(delta):
	if _is_switching and _target_scene_path != "":
		var progress = LongSceneManager.get_loading_progress(_target_scene_path)
		set_progress(progress)

func _on_scene_switch_completed(scene_path: String) -> void:
	_is_switching = false
	_target_scene_path = ""
	set_process(false)
	set_progress(1.0)

extends CanvasLayer

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

@onready var fade_in_out: ColorRect = $FadeInOut
@onready var background_color: ColorRect = $BackgroundColor
@onready var progress_bar: ProgressBar = $BackgroundColor/MarginContainer/VBoxContainer/ProgressBar

var tween: Tween
var is_transitioning: bool = false

var _target_scene_path: String = ""
var _is_switching: bool = false

func _ready():
	layer = 1000
	follow_viewport_enabled = true
	
	# 确保FadeInOut在最上层（z_index高于BackgroundColor）
	fade_in_out.z_index = 1
	background_color.z_index = 0
	
	# 初始化：黑色层覆盖（alpha=1），背景不可见，整个CanvasLayer隐藏
	fade_in_out.modulate.a = 1.0
	fade_in_out.visible = true  # 黑色层始终可见（但CanvasLayer隐藏时看不到）
	background_color.visible = false
	
	visible = false  # 整个CanvasLayer初始隐藏
	
	if progress_bar:
		progress_bar.value = 0
	
	set_process(false)
	
	if LongSceneManager:
		LongSceneManager.scene_switch_started.connect(_on_scene_switch_started)
		LongSceneManager.scene_switch_completed.connect(_on_scene_switch_completed)

func set_progress(progress: float) -> void:
	if progress_bar:
		progress_bar.value = progress * 100

func update_progress(progress: float) -> void:
	set_progress(progress)

func fade_in() -> void:
	if is_transitioning:
		_stop_current_tween()
	
	is_transitioning = true
	fade_in_started.emit()
	
	# 显示CanvasLayer和背景内容
	visible = true
	background_color.visible = true
	
	# 黑色层从完全不透明淡出到完全透明，露出背景内容
	fade_in_out.modulate.a = 1.0
	
	tween = create_tween()
	tween.set_ease(fade_in_ease)
	tween.set_trans(fade_in_trans)
	tween.tween_property(fade_in_out, "modulate:a", 0.0, fade_in_duration)
	
	await tween.finished
	is_transitioning = false
	fade_in_completed.emit()

func fade_out() -> void:
	if is_transitioning:
		_stop_current_tween()
	
	is_transitioning = true
	fade_out_started.emit()
	
	# 黑色层从完全透明淡入到完全不透明，覆盖背景内容
	fade_in_out.modulate.a = 0.0
	
	tween = create_tween()
	tween.set_ease(fade_out_ease)
	tween.set_trans(fade_out_trans)
	tween.tween_property(fade_in_out, "modulate:a", 1.0, fade_out_duration)
	
	await tween.finished
	
	# 隐藏背景和整个CanvasLayer
	background_color.visible = false
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
	if visible:
		background_color.visible = true
		fade_in_out.modulate.a = 1.0
	else:
		background_color.visible = false
		fade_in_out.modulate.a = 0.0

func _notification(what: int) -> void:
	if what == NOTIFICATION_WM_SIZE_CHANGED:
		var size = get_viewport().get_visible_rect().size
		fade_in_out.size = size
		background_color.size = size

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

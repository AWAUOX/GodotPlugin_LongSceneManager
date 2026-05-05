# long_scene_manager.gd
extends Node

# Global scene manager plugin. 全局场景管理器插件
# Supports scene switching with custom loading screens, preloading, and LRU cache. 支持自定义加载屏幕的场景切换、预加载和LRU缓存
# Scene tree and cache separation: instances are either in scene tree or in cache. 场景树和缓存分离设计:场景实例要么在场景树中，要么在缓存中

# ==================== Constants and Enums ====================
# ==================== 常量和枚举 ====================

const DEFAULT_LOAD_SCREEN_PATH = "res://addons/long_scene_manager/ui/loading_screen/GDscript/loading_black_screen.tscn"

enum LoadState {
	NOT_LOADED,      # Not loaded. 未加载
	LOADING,         # Loading in progress. 正在加载中
	LOADED          # Loaded (resource loaded but not instantiated). 已加载（资源已加载但未实例化）
}

enum LoadMethod {
	DIRECT,              # Direct load, no cache lookup. 直接加载，不查找缓存
	PRELOAD_CACHE,       # Only check preload resource cache. 只查找预加载资源缓存
	SCENE_CACHE,         # Only check scene instance cache. 只查找场景实例化缓存
	BOTH_PRELOAD_FIRST,  # Check both caches, prioritize preload cache (default). 查找两个缓存，优先预加载缓存
	BOTH_INSTANCE_FIRST  # Check both caches, prioritize instance cache. 查找两个缓存，优先实例化缓存
}

# ==================== Signal Definitions ====================
# ==================== 信号定义 ====================

signal scene_preload_started(scene_path: String)

signal scene_preload_completed(scene_path: String)

signal scene_switch_started(from_scene: String, to_scene: String)

signal scene_switch_completed(scene_path: String)

signal scene_cached(scene_path: String)

signal scene_removed_from_cache(scene_path: String)

signal load_screen_shown(load_screen_instance: Node)

signal load_screen_hidden(load_screen_instance: Node)

signal scene_preload_failed(scene_path: String)

signal scene_switch_failed(scene_path: String)

# ==================== Exported Variables ====================
# ==================== 导出变量 ====================

@export_category("Scene Manager Global Configuration")
@export_range(1, 20) var max_cache_size: int = 4

@export_range(1, 50) var max_preload_resource_cache_size: int = 8

@export var use_async_loading: bool = true

@export var always_use_default_load_screen: bool = false

@export_range(1, 10) var instantiate_frames: int = 3

# ==================== Internal State Variables ====================
# ==================== 内部状态变量 ====================

var current_scene: Node = null

var current_scene_path: String = ""

var previous_scene_path: String = ""

var default_load_screen: Node = null

var active_load_screen: Node = null

var scene_cache: Dictionary = {}

var cache_access_order: Array = []

var preload_resource_cache: Dictionary = {}

var preload_resource_cache_access_order: Array = []

var _preload_states: Dictionary = {}

var _preload_pending_cleanup: Dictionary = {}

var _is_switching: bool = false

var _scenes_to_reset: Dictionary = {}

# Track removed scenes to prevent re-entering cache. 记录被移除的场景，防止重新进入缓存
var _removed_scenes: Dictionary = {}

# Get or create preload state for scene. 获取或创建场景的预加载状态。
func _get_preload_state(scene_path: String) -> Dictionary:
	if not _preload_states.has(scene_path):
		_preload_states[scene_path] = {"state": LoadState.NOT_LOADED, "resource": null}
	return _preload_states[scene_path]

# Clear preload state for scene. 清除场景的预加载状态。
func _clear_preload_state(scene_path: String) -> void:
	if _preload_states.has(scene_path):
		_preload_states.erase(scene_path)

# Cached scene data structure. 缓存场景数据结构
class CachedScene:
	var scene_instance: Node
	var cached_time: float

	# Initialize cached scene with instance and timestamp. 用实例和时间戳初始化缓存场景
	func _init(scene: Node):
		scene_instance = scene
		cached_time = Time.get_unix_time_from_system()

# ==================== Lifecycle Functions ====================
# ==================== 生命周期函数 ====================

# Initialize scene manager on node ready. 节点就绪时初始化场景管理器。
func _ready():
	print("[SceneManager] Scene manager singleton initialized")
	
	_init_default_load_screen()
	
	current_scene = get_tree().current_scene
	if current_scene:
		current_scene_path = current_scene.scene_file_path
		print("[SceneManager] Current scene: ", current_scene_path)
	
	print("[SceneManager] Initialization complete, max cache: ", max_cache_size)

# ==================== Initialization Functions ====================
# ==================== 初始化函数 ====================

# Initialize default loading screen. 初始化默认加载屏幕。
func _init_default_load_screen():
	print("[SceneManager] Initializing default loading screen")
	
	if ResourceLoader.exists(DEFAULT_LOAD_SCREEN_PATH):
		var load_screen_scene = load(DEFAULT_LOAD_SCREEN_PATH)
		if load_screen_scene:
			default_load_screen = load_screen_scene.instantiate()
			add_child(default_load_screen)
			
			if default_load_screen is CanvasItem:
				default_load_screen.visible = false
			elif default_load_screen.has_method("set_visible"):
				default_load_screen.set_visible(false)
			
			print("[SceneManager] Default loading screen loaded successfully")
			return
	
	print("[SceneManager] Warning: Default loading screen file does not exist, creating simple version")
	default_load_screen = _create_simple_load_screen()
	add_child(default_load_screen)
	
	if default_load_screen is CanvasItem:
		default_load_screen.visible = false
	
	print("[SceneManager] Simple loading screen creation completed")

# Create a simple loading screen as fallback. 创建简单加载屏幕作为后备。
func _create_simple_load_screen() -> Node:
	var canvas_layer = CanvasLayer.new()
	canvas_layer.name = "SimpleLoadScreen"
	canvas_layer.layer = 1000
	
	var color_rect = ColorRect.new()
	color_rect.color = Color(0, 0, 0, 1)
	color_rect.size = get_viewport().get_visible_rect().size
	color_rect.anchor_left = 0
	color_rect.anchor_top = 0
	color_rect.anchor_right = 1
	color_rect.anchor_bottom = 1
	color_rect.mouse_filter = Control.MOUSE_FILTER_STOP
	
	var label = Label.new()
	label.text = "Loading..."
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	label.add_theme_font_size_override("font_size", 32)
	label.add_theme_color_override("font_color", Color.WHITE)
	
	canvas_layer.add_child(color_rect)
	color_rect.add_child(label)
	
	label.anchor_left = 0.5
	label.anchor_top = 0.5
	label.anchor_right = 0.5
	label.anchor_bottom = 0.5
	label.position = Vector2(-50, -16)
	label.size = Vector2(100, 32)
	
	return canvas_layer

# ==================== Public API - Scene Switching ====================
# ==================== 公开API - 场景切换 ====================

# Switch to target scene with optional cache and load screen. 切换到目标场景，可选择使用缓存和加载屏幕。
func switch_scene(new_scene_path: String, load_method: LoadMethod = LoadMethod.BOTH_PRELOAD_FIRST, cache_current_scene: bool = true, load_screen_path: String = "") -> void:
	if _is_switching:
		push_warning("[SceneManager] Warning: Scene switch already in progress, ignoring request to: ", new_scene_path)
		return

	_is_switching = true
	print("[SceneManager] Start switching scene to: ", new_scene_path)

	_debug_validate_scene_tree()

	if always_use_default_load_screen:
		load_screen_path = ""
		print("[SceneManager] Force using default loading screen")

	if not ResourceLoader.exists(new_scene_path):
		push_error("[SceneManager] Error: Target scene path does not exist: ", new_scene_path)
		_is_switching = false
		scene_switch_failed.emit(new_scene_path)
		return
	
	scene_switch_started.emit(current_scene_path, new_scene_path)
	
	if current_scene_path == new_scene_path and not _scenes_to_reset.has(new_scene_path):
		print("[SceneManager] Scene already loaded: ", new_scene_path)
		_is_switching = false
		scene_switch_completed.emit(new_scene_path)
		return
	
	var load_screen_to_use = _get_load_screen_instance(load_screen_path)
	if load_screen_path != "no_transition" and not load_screen_to_use:
		push_error("[SceneManager] Error: Unable to get loading screen, switching aborted")
		_is_switching = false
		scene_switch_failed.emit(new_scene_path)
		return
	
	await _load_scene_by_method(new_scene_path, load_method, cache_current_scene, load_screen_to_use)
	
	_is_switching = false

# ==================== Public API - Preloading ====================
# ==================== 公开API - 预加载 ====================

# Preload scene resource asynchronously. 异步预加载场景资源。
func preload_scene(scene_path: String) -> void:
	if not ResourceLoader.exists(scene_path):
		push_error("[SceneManager] Error: Preload scene path does not exist: ", scene_path)
		return
	
	if preload_resource_cache.has(scene_path):
		print("[SceneManager] Scene already preloaded: ", scene_path)
		return
	
	if scene_cache.has(scene_path):
		print("[SceneManager] Scene already loaded or loading: ", scene_path)
		return
	
	var preload_state = _get_preload_state(scene_path)
	if preload_state["state"] == LoadState.LOADING or preload_state["state"] == LoadState.LOADED:
		print("[SceneManager] Scene already loaded or loading: ", scene_path)
		return
	
	print("[SceneManager] Start preloading scene: ", scene_path)
	scene_preload_started.emit(scene_path)
	
	preload_state["state"] = LoadState.LOADING
	
	_preload_background(scene_path)

# Preload multiple scenes at once. 批量预加载多个场景。
func preload_scenes(scene_paths: Array[String]) -> void:
	for path in scene_paths:
		preload_scene(path)

# Cancel scene preloading if in progress. 取消正在进行的场景预加载。
func cancel_preload_scene(scene_path: String) -> void:
	if _preload_states.has(scene_path) and _preload_states[scene_path]["state"] == LoadState.LOADING:
		_preload_pending_cleanup[scene_path] = true
		_clear_preload_state(scene_path)
		print("[SceneManager] Cancelled preload, marked for cleanup: ", scene_path)

# Cancel all scenes that are currently preloading. 取消所有正在预加载的场景。
func cancel_all_preloads() -> void:
	var to_cancel = []
	for path in _preload_states:
		if _preload_states[path]["state"] == LoadState.LOADING:
			to_cancel.append(path)
	
	for path in to_cancel:
		cancel_preload_scene(path)

# Handle background preload completion and cache. 处理后台预加载完成并缓存。
func _preload_background(scene_path: String) -> void:
	if use_async_loading:
		await _async_preload_scene(scene_path)
	else:
		_sync_preload_scene(scene_path)
	
	if not _preload_states.has(scene_path):
		print("[SceneManager] Preload was cancelled: ", scene_path)
		return
	
	var preload_state = _preload_states[scene_path]
	if preload_state["state"] != LoadState.LOADING:
		print("[SceneManager] Preload was cancelled: ", scene_path)
		return
	
	if preload_state["resource"]:
		if _preload_pending_cleanup.has(scene_path):
			_preload_pending_cleanup.erase(scene_path)
			preload_resource_cache.erase(scene_path)
			print("[SceneManager] Preload completed but cancelled by user, cleaned up: ", scene_path)
			return
		
		preload_resource_cache[scene_path] = preload_state["resource"]
		preload_resource_cache_access_order.append(scene_path)
		preload_state["state"] = LoadState.LOADED
		scene_preload_completed.emit(scene_path)
		print("[SceneManager] Preloading complete, resource cached: ", scene_path)
		
		if preload_resource_cache_access_order.size() > max_preload_resource_cache_size:
			_remove_oldest_preload_resource()
	else:
		preload_state["state"] = LoadState.NOT_LOADED
		preload_state["resource"] = null
		_clear_preload_state(scene_path)
		scene_preload_failed.emit(scene_path)
		print("[SceneManager] Preloading failed: ", scene_path)

# ==================== Public API - Cache Management ====================
# ==================== 公开API - 缓存管理 ====================

# Clear all cached scenes and preload resources. 清除所有缓存场景和预加载资源。
func clear_cache() -> void:
	print("[SceneManager] Clearing cache...")
	
	preload_resource_cache.clear()
	preload_resource_cache_access_order.clear()
	_preload_states.clear()
	print("[SceneManager] Preload resource cache cleared")
	
	var to_remove = []
	for scene_path in scene_cache:
		var cached = scene_cache[scene_path]
		if is_instance_valid(cached.scene_instance):
			_cleanup_orphaned_nodes(cached.scene_instance)
			cached.scene_instance.queue_free()
		to_remove.append(scene_path)
		scene_removed_from_cache.emit(scene_path)
	
	for scene_path in to_remove:
		scene_cache.erase(scene_path)
		var index = cache_access_order.find(scene_path)
		if index != -1:
			cache_access_order.remove_at(index)
	
	print("[SceneManager] Cache cleared")

# Remove preloaded resource from cache. 从缓存中移除预加载的资源。
func remove_preloaded_resource(scene_path: String) -> void:
	# Only cleanup preload resource cache and related states. 只清理预加载资源缓存及相关状态
	if preload_resource_cache.has(scene_path) or _preload_states.has(scene_path):
		preload_resource_cache.erase(scene_path)
		
		var index = preload_resource_cache_access_order.find(scene_path)
		if index != -1:
			preload_resource_cache_access_order.remove_at(index)
		
		# Clear preload state to prevent residue from blocking re-preload. 清除预加载状态，防止状态残留导致无法重新预加载
		_clear_preload_state(scene_path)
		
		print("[SceneManager] Removed preloaded resource: ", scene_path)
		scene_removed_from_cache.emit(scene_path)
	else:
		print("[SceneManager] Warning: Preloaded resource not found in cache: ", scene_path)
		if scene_cache.has(scene_path):
			print("[SceneManager] Hint: Scene is in instance cache. Use 'remove_cached_scene()' to remove instance cache.")

# Remove cached scene instance from cache. 从缓存中移除已缓存的场景实例。
func remove_cached_scene(scene_path: String) -> void:
	# Only cleanup instantiated scene cache and related states. 只清理实例化场景缓存及相关状态
	if scene_cache.has(scene_path):
		var cached = scene_cache[scene_path]

		if is_instance_valid(cached.scene_instance):
			_cleanup_orphaned_nodes(cached.scene_instance)
			cached.scene_instance.queue_free()

		scene_cache.erase(scene_path)

		var index = cache_access_order.find(scene_path)
		if index != -1:
			cache_access_order.remove_at(index)
		
		# Clear any residual preload state. 清除可能残留的预加载状态
		_clear_preload_state(scene_path)

		print("[SceneManager] Removed cached scene: ", scene_path)
		scene_removed_from_cache.emit(scene_path)
	else:
		print("[SceneManager] Warning: Cached scene not found in cache: ", scene_path)
		if preload_resource_cache.has(scene_path):
			print("[SceneManager] Hint: Scene is in preload resource cache. Use 'remove_preloaded_resource()' to remove preload cache.")

# Get detailed cache information for debugging. 获取详细的缓存信息用于调试。
func get_cache_info() -> Dictionary:
	# Instance cache section. 实例化场景缓存板块
	var cached_scenes = []
	for path in scene_cache:
		var cached = scene_cache[path]
		cached_scenes.append({
			"path": path,
			"cached_time": cached.cached_time,
			"instance_valid": is_instance_valid(cached.scene_instance)
		})

	# Preload resource cache section. 预加载资源缓存板块
	var preloaded_scenes = []
	for path in preload_resource_cache:
		preloaded_scenes.append(path)

	# Scenes currently preloading. 正在预加载的场景
	var preloading_scenes = []
	for path in _preload_states:
		if _preload_states[path]["state"] == LoadState.LOADING:
			preloading_scenes.append(path)

	return {
			# Basic info. 基本信息
		"current_scene": current_scene_path,
		"previous_scene": previous_scene_path,

		# Instance cache section. 实例化场景缓存板块
		"instance_cache": {
			"size": scene_cache.size(),
			"max_size": max_cache_size,
			"access_order": cache_access_order.duplicate(),
			"scenes": cached_scenes
		},

		# Preload resource cache section. 预加载资源缓存板块
		"preload_cache": {
			"size": preload_resource_cache.size(),
			"max_size": max_preload_resource_cache_size,
			"access_order": preload_resource_cache_access_order.duplicate(),
			"scenes": preloaded_scenes
		},

		# Scenes currently preloading. 正在预加载的场景
		"preloading_scenes": preloading_scenes
	}

# Check if scene is in any cache. 检查场景是否在任意缓存中。
func is_scene_cached(scene_path: String) -> bool:
	return scene_cache.has(scene_path) or preload_resource_cache.has(scene_path)

# Check if scene is currently preloading. 检查场景是否正在预加载。
func is_scene_preloading(scene_path: String) -> bool:
	return _preload_states.has(scene_path) and _preload_states[scene_path]["state"] == LoadState.LOADING

# Mark scene to be reset on next switch. 标记场景在下次切换时重置。
func mark_scene_for_reset(scene_path: String) -> void:
	_scenes_to_reset[scene_path] = true
	print("[SceneManager] Scene marked for reset on next switch: ", scene_path)

# Unmark scene from reset on next switch. 取消标记场景在下次切换时重置。
func unmark_scene_for_reset(scene_path: String) -> void:
	if _scenes_to_reset.has(scene_path):
		_scenes_to_reset.erase(scene_path)
		print("[SceneManager] Scene unmarked for reset: ", scene_path)

# Get list of scenes currently preloading. 获取当前正在预加载的场景列表。
func get_preloading_scenes() -> Array:
	var loading = []
	for path in _preload_states:
		if _preload_states[path]["state"] == LoadState.LOADING:
			loading.append(path)
	return loading

# ==================== Public API - Utility Functions ====================
# ==================== 公开API - 实用函数 ====================

# Get current active scene node. 获取当前活动场景节点。
func get_current_scene() -> Node:
	return current_scene

# Get previous scene path. 获取上一个场景路径。
func get_previous_scene_path() -> String:
	return previous_scene_path

# Get loading progress for preloading scene. 获取预加载场景的加载进度。
func get_loading_progress(scene_path: String) -> float:
	if _preload_states.has(scene_path):
		var state = _preload_states[scene_path]["state"]
		if state == LoadState.LOADING:
			var progress = []
			var status = ResourceLoader.load_threaded_get_status(scene_path, progress)
			if status == ResourceLoader.THREAD_LOAD_IN_PROGRESS and progress.size() > 0:
				return progress[0]
			return 0.0
		elif state == LoadState.LOADED:
			return 1.0
	
	return 1.0 if (scene_cache.has(scene_path) or preload_resource_cache.has(scene_path)) else 0.0

# Set maximum instance cache size and trim if needed. 设置实例缓存最大大小并在需要时修剪。
func set_max_cache_size(new_size: int) -> void:
	if new_size < 1:
		push_error("[SceneManager] Error: Cache size must be greater than 0")
		return
	
	max_cache_size = new_size
	print("[SceneManager] Setting maximum cache size: ", max_cache_size)
	
	while cache_access_order.size() > max_cache_size:
		_remove_oldest_cached_scene()

# Set maximum preload cache size and trim if needed. 设置预加载缓存最大大小并在需要时修剪。
func set_max_preload_resource_cache_size(new_size: int) -> void:
	if new_size < 1:
		push_error("[SceneManager] Error: Preload resource cache size must be greater than 0")
		return
	
	max_preload_resource_cache_size = new_size
	print("[SceneManager] Setting maximum preload resource cache size: ", max_preload_resource_cache_size)
	
	while preload_resource_cache_access_order.size() > max_preload_resource_cache_size:
		_remove_oldest_preload_resource()

# ==================== Loading Screen Management ====================
# ==================== 加载屏幕管理 ====================

# Get or create load screen instance. 获取或创建加载屏幕实例。
func _get_load_screen_instance(load_screen_path: String) -> Node:
	if load_screen_path == "":
		if default_load_screen:
			print("[SceneManager] Using default loading screen")
			return default_load_screen
		else:
			push_error("[SceneManager] Error: Default loading screen not initialized")
			return null
	elif load_screen_path == "no_transition":
		print("[SceneManager] Using no transition mode")
		return null
	else:
		if ResourceLoader.exists(load_screen_path):
			var custom_scene = load(load_screen_path)
			if custom_scene:
				var instance = custom_scene.instantiate()
				add_child(instance)
				print("[SceneManager] Using custom loading screen: ", load_screen_path)
				return instance
			else:
				print("[SceneManager] Warning: Custom loading screen failed to load, using default")
				return default_load_screen
		else:
			print("[SceneManager] Warning: Custom loading screen path does not exist, using default")
			return default_load_screen

# Show load screen with optional fade-in effect. 显示加载屏幕，可选淡入效果。
func _show_load_screen(load_screen_instance: Node) -> void:
	if not load_screen_instance:
		print("[SceneManager] No loading screen, switching directly")
		return
	
	active_load_screen = load_screen_instance
	
	if load_screen_instance is CanvasItem:
		load_screen_instance.visible = true
	elif load_screen_instance.has_method("set_visible"):
		load_screen_instance.set_visible(true)
	elif load_screen_instance.has_method("show"):
		load_screen_instance.show()
	
	if load_screen_instance.has_method("fade_in"):
		print("[SceneManager] Calling loading screen fade-in effect")
		await load_screen_instance.fade_in()
	elif load_screen_instance.has_method("show_loading"):
		await load_screen_instance.show_loading()
	
	load_screen_shown.emit(load_screen_instance)
	print("[SceneManager] Loading screen display completed")

# Hide load screen with optional fade-out effect. 隐藏加载屏幕，可选淡出效果。
func _hide_load_screen(load_screen_instance: Node) -> void:
	if not load_screen_instance:
		return
	
	if load_screen_instance.has_method("fade_out"):
		print("[SceneManager] Calling loading screen fade-out effect")
		await load_screen_instance.fade_out()
	elif load_screen_instance.has_method("hide_loading"):
		await load_screen_instance.hide_loading()
	elif load_screen_instance.has_method("hide"):
		load_screen_instance.hide()
	
	if load_screen_instance != default_load_screen:
		load_screen_instance.queue_free()
		print("[SceneManager] Cleaning up custom loading screen")
	else:
		if load_screen_instance is CanvasItem:
			load_screen_instance.visible = false
		elif load_screen_instance.has_method("set_visible"):
			load_screen_instance.set_visible(false)
	
	active_load_screen = null
	load_screen_hidden.emit(load_screen_instance)
	print("[SceneManager] Loading screen hiding completed")

func _load_scene_by_method(scene_path: String, load_method: LoadMethod, cache_current_scene: bool, load_screen_instance: Node) -> void:
	match load_method:
		LoadMethod.DIRECT:
			print("[SceneManager] Load method: DIRECT")
			await _handle_direct_load(scene_path, load_screen_instance, cache_current_scene)
		LoadMethod.PRELOAD_CACHE:
			print("[SceneManager] Load method: PRELOAD_CACHE")
			if preload_resource_cache.has(scene_path):
				print("[SceneManager] Using preload resource cache: ", scene_path)
				await _handle_preloaded_resource(scene_path, load_screen_instance, cache_current_scene)
			elif _preload_states.has(scene_path) and _preload_states[scene_path]["state"] == LoadState.LOADING:
				print("[SceneManager] Scene is preloading, waiting for completion...")
				await _handle_preloading_scene(scene_path, load_screen_instance, cache_current_scene)
			else:
				print("[SceneManager] Preload cache miss, loading directly: ", scene_path)
				await _handle_direct_load(scene_path, load_screen_instance, cache_current_scene)
		LoadMethod.SCENE_CACHE:
			print("[SceneManager] Load method: SCENE_CACHE")
			if scene_cache.has(scene_path):
				print("[SceneManager] Loading scene from instance cache: ", scene_path)
				await _handle_cached_scene(scene_path, load_screen_instance, cache_current_scene)
			else:
				print("[SceneManager] Instance cache miss, loading directly: ", scene_path)
				await _handle_direct_load(scene_path, load_screen_instance, cache_current_scene)
		LoadMethod.BOTH_PRELOAD_FIRST:
			print("[SceneManager] Load method: BOTH_PRELOAD_FIRST")
			if preload_resource_cache.has(scene_path):
				print("[SceneManager] Using preload resource cache: ", scene_path)
				await _handle_preloaded_resource(scene_path, load_screen_instance, cache_current_scene)
			elif _preload_states.has(scene_path) and _preload_states[scene_path]["state"] == LoadState.LOADING:
				print("[SceneManager] Scene is preloading, waiting for completion...")
				await _handle_preloading_scene(scene_path, load_screen_instance, cache_current_scene)
			elif scene_cache.has(scene_path):
				print("[SceneManager] Loading scene from instance cache: ", scene_path)
				await _handle_cached_scene(scene_path, load_screen_instance, cache_current_scene)
			else:
				print("[SceneManager] All caches miss, loading directly: ", scene_path)
				await _handle_direct_load(scene_path, load_screen_instance, cache_current_scene)
		LoadMethod.BOTH_INSTANCE_FIRST:
			print("[SceneManager] Load method: BOTH_INSTANCE_FIRST")
			if scene_cache.has(scene_path):
				print("[SceneManager] Loading scene from instance cache: ", scene_path)
				await _handle_cached_scene(scene_path, load_screen_instance, cache_current_scene)
			elif preload_resource_cache.has(scene_path):
				print("[SceneManager] Using preload resource cache: ", scene_path)
				await _handle_preloaded_resource(scene_path, load_screen_instance, cache_current_scene)
			elif _preload_states.has(scene_path) and _preload_states[scene_path]["state"] == LoadState.LOADING:
				print("[SceneManager] Scene is preloading, waiting for completion...")
				await _handle_preloading_scene(scene_path, load_screen_instance, cache_current_scene)
			else:
				print("[SceneManager] All caches miss, loading directly: ", scene_path)
				await _handle_direct_load(scene_path, load_screen_instance, cache_current_scene)
		_:
			push_error("[SceneManager] Error: Unknown load method")
			scene_switch_failed.emit(scene_path)

# ==================== Scene Switch Handlers ====================
# ==================== 场景切换处理函数 ====================

# Handle switch using preloaded resource. 处理使用预加载资源的场景切换。
func _handle_preloaded_resource(scene_path: String, load_screen_instance: Node, use_cache: bool) -> void:
	await _show_load_screen(load_screen_instance)

	var packed_scene = preload_resource_cache.get(scene_path)
	preload_resource_cache.erase(scene_path)

	var index = preload_resource_cache_access_order.find(scene_path)
	if index != -1:
		preload_resource_cache_access_order.remove_at(index)

	if not packed_scene:
		push_error("[SceneManager] Preload resource cache error: ", scene_path)
		await _hide_load_screen(load_screen_instance)
		scene_switch_failed.emit(scene_path)
		return

	print("[SceneManager] Instantiate preloaded resources: ", scene_path)

	var new_scene = await _instantiate_scene_deferred(packed_scene, load_screen_instance)
	if not new_scene:
		push_error("[SceneManager] Scene instantiation failed")
		await _hide_load_screen(load_screen_instance)
		scene_switch_failed.emit(scene_path)
		return

	await _perform_scene_switch(new_scene, scene_path, load_screen_instance, use_cache)

# Handle switch while scene is preloading. 处理场景正在预加载时的切换。
func _handle_preloading_scene(scene_path: String, load_screen_instance: Node, use_cache: bool) -> void:
	await _show_load_screen(load_screen_instance)

	var wait_start_time = Time.get_ticks_msec()
	while _preload_states.has(scene_path) and _preload_states[scene_path]["state"] == LoadState.LOADING:
		var progress = get_loading_progress(scene_path)

		if load_screen_instance and load_screen_instance.has_method("set_progress"):
			load_screen_instance.set_progress(progress)
		elif load_screen_instance and load_screen_instance.has_method("update_progress"):
			load_screen_instance.update_progress(progress)

		if Time.get_ticks_msec() - wait_start_time > 500:
			print("[SceneManager] Preload progress: ", progress * 100, "%")
			wait_start_time = Time.get_ticks_msec()

		await get_tree().process_frame

	print("[SceneManager] Preload waiting completed")

	var preload_state = _get_preload_state(scene_path)
	if preload_state["resource"]:
		preload_resource_cache[scene_path] = preload_state["resource"]
		preload_resource_cache_access_order.append(scene_path)
		print("[SceneManager] Preload resource cached: ", scene_path)

		if preload_resource_cache_access_order.size() > max_preload_resource_cache_size:
			_remove_oldest_preload_resource()

	_clear_preload_state(scene_path)

	await _instantiate_and_switch(scene_path, load_screen_instance, use_cache)

# Handle switch using cached scene instance. 处理使用缓存场景实例的切换。
func _handle_cached_scene(scene_path: String, load_screen_instance: Node, cache_current_scene: bool) -> void:
	await _show_load_screen(load_screen_instance)
	await _switch_to_cached_scene(scene_path, load_screen_instance, cache_current_scene)

# Handle direct scene load without cache. 处理不通过缓存的直接场景加载。
func _handle_direct_load(scene_path: String, load_screen_instance: Node, use_cache: bool) -> void:
	await _show_load_screen(load_screen_instance)
	await _load_and_switch(scene_path, load_screen_instance, use_cache)

# Instantiate scene across multiple frames to avoid stalls. 跨多帧实例化场景以避免卡顿。
func _instantiate_scene_deferred(packed_scene: PackedScene, load_screen_instance: Node = null) -> Node:
	if instantiate_frames <= 1:
		return packed_scene.instantiate()
	
	var instance = packed_scene.instantiate()
	if not instance:
		return null
	
	var children = _collect_children_recursive(instance)
	var total = children.size()
	
	if total == 0:
		return instance
	
	var frame_size = max(1, ceil(float(total) / instantiate_frames))
	
	for i in range(0, total, frame_size):
		for j in range(frame_size):
			var idx = i + j
			if idx >= total:
				break
			var child = children[idx]
			if is_instance_valid(child):
				child.set_process(false)
				child.set_physics_process(false)
				child.set_process_input(false)
				child.set_process_unhandled_input(false)
				child.set_process_unhandled_key_input(false)
		
		var processed = min(i + frame_size, total)
		
		if load_screen_instance and load_screen_instance.has_method("set_progress"):
			load_screen_instance.set_progress(float(processed) / total)
		elif load_screen_instance and load_screen_instance.has_method("update_progress"):
			load_screen_instance.update_progress(float(processed) / total)
		
		await get_tree().process_frame
	
	for child in children:
		if is_instance_valid(child):
			child.set_process(true)
			child.set_physics_process(true)
			child.set_process_input(true)
			child.set_process_unhandled_input(true)
			child.set_process_unhandled_key_input(true)
	
	return instance

# Collect all children nodes recursively. 递归收集所有子节点。
func _collect_children_recursive(root: Node) -> Array:
	var result = [root]
	var queue = [root]
	while queue.size() > 0:
		var node = queue.pop_front()
		for child in node.get_children():
			result.append(child)
			queue.append(child)
	return result

# Instantiate preloaded scene and perform switch. 实例化预加载场景并执行切换。
func _instantiate_and_switch(scene_path: String, load_screen_instance: Node, use_cache: bool) -> void:
	if not preload_resource_cache.has(scene_path):
		push_error("[SceneManager] Preloaded resource does not exist: ", scene_path)
		await _hide_load_screen(load_screen_instance)
		scene_switch_failed.emit(scene_path)
		return

	print("[SceneManager] Instantiating preloaded scene: ", scene_path)

	var packed_scene = preload_resource_cache.get(scene_path)
	# Remove from preload cache (reference correct approach from _handle_preloaded_resource). 从预加载缓存移除（参考 _handle_preloaded_resource 的正确做法）
	preload_resource_cache.erase(scene_path)
	var index = preload_resource_cache_access_order.find(scene_path)
	if index != -1:
		preload_resource_cache_access_order.remove_at(index)

	var new_scene = await _instantiate_scene_deferred(packed_scene, load_screen_instance)
	if not new_scene:
		push_error("[SceneManager] Scene instantiation failed")
		await _hide_load_screen(load_screen_instance)
		scene_switch_failed.emit(scene_path)
		return

	await _perform_scene_switch(new_scene, scene_path, load_screen_instance, use_cache)

# ==================== Core Loading and Switching Functions ====================
# ==================== 加载和切换核心函数 ====================

# Switch to scene from instance cache. 从实例缓存切换到场景。
func _switch_to_cached_scene(scene_path: String, load_screen_instance: Node, cache_current_scene: bool) -> void:
	if not scene_cache.has(scene_path):
		push_error("[SceneManager] Scene not found in cache: ", scene_path)
		await _hide_load_screen(load_screen_instance)
		return
	
	var cached = scene_cache[scene_path]
	if not is_instance_valid(cached.scene_instance):
		push_error("[SceneManager] Cached scene instance is invalid")
		scene_cache.erase(scene_path)
		var index = cache_access_order.find(scene_path)
		if index != -1:
			cache_access_order.remove_at(index)
		await _hide_load_screen(load_screen_instance)
		return

	print("[SceneManager] Using cached scene: ", scene_path)
	
	var scene_instance = cached.scene_instance
	
	scene_cache.erase(scene_path)
	var index = cache_access_order.find(scene_path)
	if index != -1:
		cache_access_order.remove_at(index)

	if scene_instance.is_inside_tree():
		scene_instance.get_parent().remove_child(scene_instance)
	
	await _perform_scene_switch(scene_instance, scene_path, load_screen_instance, cache_current_scene)

# Load scene resource and perform switch. 加载场景资源并执行切换。
func _load_and_switch(scene_path: String, load_screen_instance: Node, current_scene_use_cache: bool) -> void:
	print("[SceneManager] Loading scene: ", scene_path)

	# Use asynchronous loading to get progress 使用异步加载以获取进度
	ResourceLoader.load_threaded_request(scene_path, "", false, ResourceLoader.CACHE_MODE_IGNORE)
	
	var progress_array = []
	var status
	var load_start_time = Time.get_ticks_msec()
	
	while true:
		status = ResourceLoader.load_threaded_get_status(scene_path, progress_array)
		
		match status:
			ResourceLoader.THREAD_LOAD_IN_PROGRESS:
				var progress = progress_array[0] if progress_array.size() > 0 else 0.0
				# Update progress bar 更新进度条
				if load_screen_instance and load_screen_instance.has_method("set_progress"):
					load_screen_instance.set_progress(progress)
				elif load_screen_instance and load_screen_instance.has_method("update_progress"):
					load_screen_instance.update_progress(progress)
				
				# Log progress every 500ms 每500毫秒输出一次进度
				if Time.get_ticks_msec() - load_start_time > 500:
					print("[SceneManager] Direct load progress: ", progress * 100, "%")
					load_start_time = Time.get_ticks_msec()
				
				await get_tree().process_frame
			
			ResourceLoader.THREAD_LOAD_LOADED:
				print("[SceneManager] Direct load completed: ", scene_path)
				break
			
			ResourceLoader.THREAD_LOAD_FAILED:
				push_error("[SceneManager] Scene loading failed: ", scene_path)
				await _hide_load_screen(load_screen_instance)
				scene_switch_failed.emit(scene_path)
				return
	
	var new_scene_resource = ResourceLoader.load_threaded_get(scene_path)
	if not new_scene_resource:
		push_error("[SceneManager] Scene resource retrieval failed: ", scene_path)
		await _hide_load_screen(load_screen_instance)
		scene_switch_failed.emit(scene_path)
		return

	var new_scene = await _instantiate_scene_deferred(new_scene_resource, load_screen_instance)
	if not new_scene:
		push_error("[SceneManager] Scene instantiation failed: ", scene_path)
		await _hide_load_screen(load_screen_instance)
		scene_switch_failed.emit(scene_path)
		return

	await _perform_scene_switch(new_scene, scene_path, load_screen_instance, current_scene_use_cache)

# Core function to swap old scene with new scene. 核心函数：用新场景替换旧场景。
func _perform_scene_switch(new_scene: Node, new_scene_path: String, load_screen_instance: Node, current_scene_use_cache: bool) -> void:
	print("[SceneManager] Performing scene switch to: ", new_scene_path)

	var old_scene = current_scene
	var old_scene_path = current_scene_path

	previous_scene_path = current_scene_path
	current_scene = new_scene
	current_scene_path = new_scene_path

	if old_scene and old_scene != new_scene:
		print("[SceneManager] Removing current scene: ", old_scene.name)

		if old_scene.is_inside_tree():
			old_scene.get_parent().remove_child(old_scene)

		if _scenes_to_reset.has(old_scene_path):
			# Scene marked for reset: free instance and reload as resource to preload cache. 场景被标记为重置：释放实例，重新加载为资源保存到预加载缓存
			print("[SceneManager] Scene marked for reset, reloading as resource: ", old_scene_path)
			_cleanup_orphaned_nodes(old_scene)
			old_scene.queue_free()
			_reset_scene_as_resource(old_scene_path)
			_scenes_to_reset.erase(old_scene_path)
		elif current_scene_use_cache and old_scene_path != "" and old_scene_path != new_scene_path:
			_add_to_cache(old_scene_path, old_scene)
		else:
			_cleanup_orphaned_nodes(old_scene)
			old_scene.queue_free()
	
	print("[SceneManager] Adding new scene: ", new_scene.name)
	
	if new_scene.is_inside_tree():
		new_scene.get_parent().remove_child(new_scene)
	
	get_tree().root.add_child(new_scene)
	get_tree().current_scene = new_scene
	
	if not new_scene.is_node_ready():
		print("[SceneManager] Waiting for new scene to be ready...")
		await new_scene.ready
	
	await _hide_load_screen(load_screen_instance)
	
	_debug_validate_scene_tree()
	
	scene_switch_completed.emit(new_scene_path)
	print("[SceneManager] Scene switching completed: ", new_scene_path)

# ==================== Internal Cache Management Functions ====================
# ==================== 缓存管理内部函数 ====================

# Add scene instance to LRU cache. 将场景实例添加到LRU缓存。
func _add_to_cache(scene_path: String, scene_instance: Node) -> void:
	if scene_path == "" or not scene_instance:
		print("[SceneManager] Warning: Cannot cache empty scene or path")
		return

	if scene_cache.has(scene_path):
		print("[SceneManager] Scene already in instance cache: ", scene_path)
		var old_cached = scene_cache[scene_path]
		if is_instance_valid(old_cached.scene_instance):
			_cleanup_orphaned_nodes(old_cached.scene_instance)
			old_cached.scene_instance.queue_free()
		scene_cache.erase(scene_path)
		var index = cache_access_order.find(scene_path)
		if index != -1:
			cache_access_order.remove_at(index)

	_cleanup_orphaned_nodes(scene_instance)

	if scene_instance.is_inside_tree():
		push_error("[SceneManager] Error: Attempting to cache node still in scene tree")
		scene_instance.get_parent().remove_child(scene_instance)

	print("[SceneManager] Adding to instance cache: ", scene_path)

	var cached = CachedScene.new(scene_instance)
	scene_cache[scene_path] = cached
	cache_access_order.append(scene_path)
	scene_cached.emit(scene_path)

	if cache_access_order.size() > max_cache_size:
		_remove_oldest_cached_scene()

# Remove oldest scene from instance cache (LRU). 从实例缓存移除最旧的场景（LRU）。
func _remove_oldest_cached_scene() -> void:
	if cache_access_order.size() == 0:
		return
	
	var oldest_path = cache_access_order[0]
	cache_access_order.remove_at(0)
	
	if scene_cache.has(oldest_path):
		var cached = scene_cache[oldest_path]
		if is_instance_valid(cached.scene_instance):
			_cleanup_orphaned_nodes(cached.scene_instance)
			cached.scene_instance.queue_free()
		scene_cache.erase(oldest_path)
		scene_removed_from_cache.emit(oldest_path)
		print("[SceneManager] Removing old cache: ", oldest_path)

# Remove oldest resource from preload cache (LRU). 从预加载缓存移除最旧的资源（LRU）。
func _remove_oldest_preload_resource() -> void:
	if preload_resource_cache_access_order.size() == 0:
		return
	
	var oldest_path = preload_resource_cache_access_order[0]
	preload_resource_cache_access_order.remove_at(0)
	
	if preload_resource_cache.has(oldest_path):
		preload_resource_cache.erase(oldest_path)
		print("[SceneManager] Removing old preload resource: ", oldest_path)

# Reload scene as resource and cache it. 重新加载场景为资源并缓存。
func _reset_scene_as_resource(scene_path: String) -> void:
	print("[SceneManager] Resetting scene as resource: ", scene_path)
	
	var packed_scene = load(scene_path)
	if not packed_scene:
		push_error("[SceneManager] Failed to reload scene as resource: ", scene_path)
		return
	
	preload_resource_cache[scene_path] = packed_scene
	preload_resource_cache_access_order.append(scene_path)
	print("[SceneManager] Scene reloaded and cached as resource: ", scene_path)
	
	if preload_resource_cache_access_order.size() > max_preload_resource_cache_size:
		_remove_oldest_preload_resource()

# ==================== Internal Preload Functions ====================
# ==================== 预加载内部函数 ====================

# Asynchronously preload scene using threaded loading. 使用线程加载异步预加载场景。
func _async_preload_scene(scene_path: String) -> void:
	print("[SceneManager] Asynchronous preload: ", scene_path)
	
	var load_start_time = Time.get_ticks_msec()
	#ResourceLoader.load_threaded_request(scene_path)
	ResourceLoader.load_threaded_request(scene_path, "", false, ResourceLoader.CACHE_MODE_IGNORE)
	
	while true:
		var status = ResourceLoader.load_threaded_get_status(scene_path)
		
		match status:
			ResourceLoader.THREAD_LOAD_IN_PROGRESS:
				if Time.get_ticks_msec() - load_start_time > 500:
					var progress = []
					ResourceLoader.load_threaded_get_status(scene_path, progress)
					if progress.size() > 0:
						print("[SceneManager] Asynchronous loading progress: ", progress[0] * 100, "%")
					load_start_time = Time.get_ticks_msec()
				
				await get_tree().process_frame
			
			ResourceLoader.THREAD_LOAD_LOADED:
				var preload_state = _get_preload_state(scene_path)
				preload_state["resource"] = ResourceLoader.load_threaded_get(scene_path)
				print("[SceneManager] Asynchronous preload completed: ", scene_path)
				return
			
			ResourceLoader.THREAD_LOAD_FAILED:
				push_error("[SceneManager] Asynchronous loading failed: ", scene_path)
				var preload_state = _get_preload_state(scene_path)
				preload_state["resource"] = null
				return
			
			_:
				push_error("[SceneManager] Unknown loading status: ", status)
				var preload_state = _get_preload_state(scene_path)
				preload_state["resource"] = null
				return

# Synchronously preload scene on main thread. 在主线程同步预加载场景。
func _sync_preload_scene(scene_path: String) -> void:
	print("[SceneManager] Synchronous preload: ", scene_path)
	var preload_state = _get_preload_state(scene_path)
	preload_state["resource"] = load(scene_path)

# ==================== Orphaned Node Cleanup Functions ====================
# ==================== 孤立节点清理函数 ====================

# Recursively remove node from parent and cleanup. 递归从父节点移除节点并清理。
func _cleanup_orphaned_nodes(root_node: Node) -> void:
	if not root_node or not is_instance_valid(root_node):
		return
	
	if root_node.is_inside_tree():
		var parent = root_node.get_parent()
		if parent:
			parent.remove_child(root_node)
	
	for child in root_node.get_children():
		_cleanup_orphaned_nodes(child)

# Validate scene tree state for debugging. 验证场景树状态用于调试。
func _debug_validate_scene_tree() -> void:
	var root = get_tree().root
	var current = get_tree().current_scene
	
	print("[SceneManager] Scene tree validation - Root node child count: ", root.get_child_count())
	print("[SceneManager] Current scene: ", current.name if current else "None")
	
	for scene_path in scene_cache:
		var cached = scene_cache[scene_path]
		if is_instance_valid(cached.scene_instance) and cached.scene_instance.is_inside_tree():
			push_error("[SceneManager] Error: Cached node still in scene tree: ", scene_path)

# ==================== Signal Connection Helpers ====================
# ==================== 信号连接辅助 ====================

# Auto-connect all scene manager signals to target. 自动连接所有场景管理器信号到目标。
func connect_all_signals(target: Object) -> void:
	if not target:
		return
	
	var signals_list = get_signal_list()
	for signal_info in signals_list:
		var signal_name = signal_info["name"]
		
		var method_name = "_on_scene_manager_" + signal_name
		if target.has_method(method_name):
			connect(signal_name, Callable(target, method_name))
			print("[SceneManager] Connecting signal: ", signal_name, " -> ", method_name)

# ==================== Debug and Utility Functions ====================
# ==================== 调试和工具函数 ====================

# Print detailed debug information to console. 打印详细的调试信息到控制台。
func print_debug_info() -> void:
	print("\n=== SceneManager Debug Info ===")
	print("Current scene: ", current_scene_path if current_scene else "None")
	print("Previous scene: ", previous_scene_path)
	print("Instance cache count: ", scene_cache.size(), "/", max_cache_size)
	print("Preload resource cache count: ", preload_resource_cache.size(), "/", max_preload_resource_cache_size)
	print("Cache access order: ", cache_access_order)
	print("Preload resource cache access order: ", preload_resource_cache_access_order)
	
	var loading_scenes = []
	for path in _preload_states:
		if _preload_states[path]["state"] == LoadState.LOADING:
			loading_scenes.append(path)
	print("Scenes currently loading: ", loading_scenes if loading_scenes.size() > 0 else "None")
	
	print("Default loading screen: ", "Loaded" if default_load_screen else "Not loaded")
	print("Active loading screen: ", "Yes" if active_load_screen else "No")
	print("Using asynchronous loading: ", use_async_loading)
	print("Always use default loading screen: ", always_use_default_load_screen)
	print("===============================\n")

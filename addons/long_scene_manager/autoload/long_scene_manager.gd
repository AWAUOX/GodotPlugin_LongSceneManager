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
	LOADED,          # Preloaded (resource loaded but not instantiated). 已预加载（资源已加载但未实例化）
	INSTANTIATED,    # Instantiated and stored in instance cache. 已实例化并存入实例缓存
	CANCELLED        # Preload cancelled. 预加载已取消
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
signal scene_preload_cancelled(scene_path: String)
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
@export_range(1, 50) var max_temp_preload_resource_cache_size: int = 8
@export_range(0, 50) var max_fixed_preload_resource_cache_size: int = 4
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

var instantiate_scene_cache: Dictionary = {}
var instantiate_scene_cache_order: Array = []

var temp_preloaded_resource_cache: Dictionary = {}
var temp_preloaded_resource_cache_order: Array = []

var fixed_preload_resource_cache: Dictionary = {}
var fixed_preload_resource_cache_order: Array = []

var _preload_resource_states: Dictionary = {}
var _is_switching: bool = false

# Cached scene data structure. 缓存场景数据结构
class CachedScene:
	var scene_instance: Node
	var cached_time: float

	func _init(scene: Node):
		scene_instance = scene
		cached_time = Time.get_unix_time_from_system()

# ==================== Lifecycle Functions ====================
# ==================== 生命周期函数 ====================

func _ready():
	print("[SceneManager] Scene manager singleton initialized")
	_init_default_load_screen()
	current_scene = get_tree().current_scene
	if current_scene:
		current_scene_path = current_scene.scene_file_path
		print("[SceneManager] Current scene: ", current_scene_path)
	print("[SceneManager] Initialization complete, max cache: ", max_cache_size)

# ==================== Public API - Scene Switching ====================
# ==================== 公开API - 场景切换 ====================

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

	if current_scene_path == new_scene_path:
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

func preload_scene(scene_path: String, fixed: bool = false) -> void:
	if not ResourceLoader.exists(scene_path):
		push_error("[SceneManager] Error: Preload scene path does not exist: ", scene_path)
		return

	var resource_state = _get_preload_resource_state(scene_path)
	if resource_state["state"] == LoadState.LOADING:
		print("[SceneManager] Scene is loading: ", scene_path)
		return
	if resource_state["state"] == LoadState.LOADED:
		print("[SceneManager] Scene already preloaded: ", scene_path)
		return
	if resource_state["state"] == LoadState.INSTANTIATED:
		print("[SceneManager] Scene was instantiated, allowing re-preload: ", scene_path)
	if resource_state["state"] == LoadState.CANCELLED:
		print("[SceneManager] Scene preload was cancelled, will restart: ", scene_path)

	if temp_preloaded_resource_cache.has(scene_path):
		print("[SceneManager] Scene already in temp cache: ", scene_path)
		return
	if fixed_preload_resource_cache.has(scene_path):
		print("[SceneManager] Scene already in fixed cache: ", scene_path)
		return

	print("[SceneManager] Start preloading scene: ", scene_path, " (fixed: ", fixed, ")")
	scene_preload_started.emit(scene_path)

	resource_state["state"] = LoadState.LOADING
	resource_state["fixed"] = fixed
	resource_state["resource"] = null

	_preload_background(scene_path)

func preload_scenes(scene_paths: Array[String], fixed: bool = false) -> void:
	for path in scene_paths:
		preload_scene(path, fixed)

func cancel_preloading_scene(scene_path: String) -> void:
	if _preload_resource_states.has(scene_path):
		var state = _preload_resource_states[scene_path]
		if state["state"] == LoadState.LOADING:
			state["state"] = LoadState.CANCELLED
			print("[SceneManager] Preload cancelled: ", scene_path)
			scene_preload_cancelled.emit(scene_path)
		else:
			print("[SceneManager] Preload not in loading state: ", scene_path)
	else:
		print("[SceneManager] No preload state found: ", scene_path)

func cancel_all_preloading() -> void:
	var to_cancel = []
	for path in _preload_resource_states:
		if _preload_resource_states[path]["state"] == LoadState.LOADING:
			to_cancel.append(path)

	for path in to_cancel:
		cancel_preloading_scene(path)

# ==================== Public API - Cache Management ====================
# ==================== 公开API - 缓存管理 ====================

func clear_all_cache() -> void:
	print("[SceneManager] Clearing cache...")

	temp_preloaded_resource_cache.clear()
	temp_preloaded_resource_cache_order.clear()
	fixed_preload_resource_cache.clear()
	fixed_preload_resource_cache_order.clear()
	_preload_resource_states.clear()
	print("[SceneManager] Temp and fixed preload resource cache cleared")

	var to_remove = []
	for scene_path in instantiate_scene_cache:
		var cached = instantiate_scene_cache[scene_path]
		if is_instance_valid(cached.scene_instance):
			_cleanup_orphaned_nodes(cached.scene_instance)
			cached.scene_instance.queue_free()
		to_remove.append(scene_path)
		scene_removed_from_cache.emit(scene_path)

	for scene_path in to_remove:
		instantiate_scene_cache.erase(scene_path)
		var index = instantiate_scene_cache_order.find(scene_path)
		if index != -1:
			instantiate_scene_cache_order.remove_at(index)

	print("[SceneManager] Cache cleared")

func clear_temp_preload_cache() -> void:
	print("[SceneManager] Clearing temp preload cache...")

	var to_remove = []
	for path in _preload_resource_states:
		if _preload_resource_states[path].get("fixed", false) == false:
			to_remove.append(path)

	for path in to_remove:
		_preload_resource_states.erase(path)

	temp_preloaded_resource_cache.clear()
	temp_preloaded_resource_cache_order.clear()
	print("[SceneManager] Temp preload cache cleared")

func clear_fixed_cache() -> void:
	print("[SceneManager] Clearing fixed cache...")

	var to_remove = []
	for path in fixed_preload_resource_cache:
		to_remove.append(path)

	for path in to_remove:
		fixed_preload_resource_cache.erase(path)
		var index = fixed_preload_resource_cache_order.find(path)
		if index != -1:
			fixed_preload_resource_cache_order.remove_at(index)
		scene_removed_from_cache.emit(path)

	for path in to_remove:
		_preload_resource_states.erase(path)

	print("[SceneManager] Fixed cache cleared")

func clear_instance_cache() -> void:
	print("[SceneManager] Clearing instance cache...")

	var to_remove = []
	for scene_path in instantiate_scene_cache:
		var cached = instantiate_scene_cache[scene_path]
		if is_instance_valid(cached.scene_instance):
			_cleanup_orphaned_nodes(cached.scene_instance)
			cached.scene_instance.queue_free()
		to_remove.append(scene_path)
		scene_removed_from_cache.emit(scene_path)

	for scene_path in to_remove:
		instantiate_scene_cache.erase(scene_path)
		var index = instantiate_scene_cache_order.find(scene_path)
		if index != -1:
			instantiate_scene_cache_order.remove_at(index)

	print("[SceneManager] Instance cache cleared")

func remove_temp_resource(scene_path: String) -> void:
	if temp_preloaded_resource_cache.has(scene_path) or _preload_resource_states.has(scene_path):
		temp_preloaded_resource_cache.erase(scene_path)

		var index = temp_preloaded_resource_cache_order.find(scene_path)
		if index != -1:
			temp_preloaded_resource_cache_order.remove_at(index)

		_clear_preload_resource_state(scene_path)

		print("[SceneManager] Removed temp preloaded resource: ", scene_path)
		scene_removed_from_cache.emit(scene_path)
	else:
		print("[SceneManager] Warning: Temp preloaded resource not found: ", scene_path)
		if instantiate_scene_cache.has(scene_path):
			print("[SceneManager] Hint: Scene is in instance cache. Use 'remove_cached_scene()' instead.")

func remove_fixed_resource(scene_path: String) -> void:
	if fixed_preload_resource_cache.has(scene_path):
		fixed_preload_resource_cache.erase(scene_path)

		var index = fixed_preload_resource_cache_order.find(scene_path)
		if index != -1:
			fixed_preload_resource_cache_order.remove_at(index)

		_clear_preload_resource_state(scene_path)

		print("[SceneManager] Removed fixed preloaded resource: ", scene_path)
		scene_removed_from_cache.emit(scene_path)
	else:
		print("[SceneManager] Warning: Fixed preloaded resource not found: ", scene_path)

func remove_cached_scene(scene_path: String) -> void:
	if instantiate_scene_cache.has(scene_path):
		var cached = instantiate_scene_cache[scene_path]

		if is_instance_valid(cached.scene_instance):
			_cleanup_orphaned_nodes(cached.scene_instance)
			cached.scene_instance.queue_free()

		instantiate_scene_cache.erase(scene_path)

		var index = instantiate_scene_cache_order.find(scene_path)
		if index != -1:
			instantiate_scene_cache_order.remove_at(index)

		_clear_preload_resource_state(scene_path)

		print("[SceneManager] Removed cached scene: ", scene_path)
		scene_removed_from_cache.emit(scene_path)
	else:
		print("[SceneManager] Warning: Cached scene not found: ", scene_path)
		if temp_preloaded_resource_cache.has(scene_path):
			print("[SceneManager] Hint: Scene is in temp preload cache. Use 'remove_temp_resource()' instead.")

func move_to_fixed(scene_path: String) -> void:
	if temp_preloaded_resource_cache.has(scene_path):
		var resource = temp_preloaded_resource_cache.get(scene_path)

		if fixed_preload_resource_cache_order.size() >= max_fixed_preload_resource_cache_size and max_fixed_preload_resource_cache_size > 0:
			_remove_oldest_fixed_preload_resource()

		temp_preloaded_resource_cache.erase(scene_path)
		var index = temp_preloaded_resource_cache_order.find(scene_path)
		if index != -1:
			temp_preloaded_resource_cache_order.remove_at(index)

		fixed_preload_resource_cache[scene_path] = resource
		fixed_preload_resource_cache_order.append(scene_path)

		print("[SceneManager] Moved resource to fixed cache: ", scene_path)
	else:
		print("[SceneManager] Warning: Resource not found in temp preload cache: ", scene_path)

func move_to_temp(scene_path: String) -> void:
	if fixed_preload_resource_cache.has(scene_path):
		var resource = fixed_preload_resource_cache.get(scene_path)

		if temp_preloaded_resource_cache_order.size() >= max_temp_preload_resource_cache_size:
			_remove_oldest_temp_preload_resource()

		fixed_preload_resource_cache.erase(scene_path)
		var index = fixed_preload_resource_cache_order.find(scene_path)
		if index != -1:
			fixed_preload_resource_cache_order.remove_at(index)

		temp_preloaded_resource_cache[scene_path] = resource
		temp_preloaded_resource_cache_order.append(scene_path)

		print("[SceneManager] Moved resource to temp cache: ", scene_path)
	else:
		print("[SceneManager] Warning: Resource not found in fixed preload cache: ", scene_path)

func set_max_fixed_cache_size(new_size: int) -> void:
	if new_size < 0:
		push_error("[SceneManager] Error: Fixed cache size must be >= 0")
		return

	max_fixed_preload_resource_cache_size = new_size
	print("[SceneManager] Setting maximum fixed cache size: ", max_fixed_preload_resource_cache_size)

	while fixed_preload_resource_cache_order.size() > max_fixed_preload_resource_cache_size and max_fixed_preload_resource_cache_size > 0:
		_remove_oldest_fixed_preload_resource()

func set_max_cache_size(new_size: int) -> void:
	if new_size < 1:
		push_error("[SceneManager] Error: Cache size must be greater than 0")
		return

	max_cache_size = new_size
	print("[SceneManager] Setting maximum cache size: ", max_cache_size)

	while instantiate_scene_cache_order.size() > max_cache_size:
		_remove_oldest_cached_scene()

func set_max_temp_preload_resource_cache_size(new_size: int) -> void:
	if new_size < 1:
		push_error("[SceneManager] Error: Temp preload cache size must be greater than 0")
		return

	max_temp_preload_resource_cache_size = new_size
	print("[SceneManager] Setting maximum temp preload cache size: ", max_temp_preload_resource_cache_size)

	while temp_preloaded_resource_cache_order.size() > max_temp_preload_resource_cache_size:
		_remove_oldest_temp_preload_resource()

# ==================== Public API - Query Functions ====================
# ==================== 公开API - 查询函数 ====================

func get_cache_info() -> Dictionary:
	var cached_scenes = []
	for path in instantiate_scene_cache:
		var cached = instantiate_scene_cache[path]
		cached_scenes.append({
			"path": path,
			"cached_time": cached.cached_time,
			"instance_valid": is_instance_valid(cached.scene_instance)
		})

	var temp_preloaded_scenes = []
	for path in temp_preloaded_resource_cache:
		temp_preloaded_scenes.append(path)

	var fixed_preloaded_scenes = []
	for path in fixed_preload_resource_cache:
		fixed_preloaded_scenes.append(path)

	var preload_states_info = []
	for path in _preload_resource_states:
		var state_info = _preload_resource_states[path]
		preload_states_info.append({
			"path": path,
			"state": state_info["state"],
			"fixed": state_info.get("fixed", false),
			"has_resource": state_info["resource"] != null
		})

	return {
		"current_scene": current_scene_path,
		"previous_scene": previous_scene_path,
		"instance_cache": {
			"size": instantiate_scene_cache.size(),
			"max_size": max_cache_size,
			"access_order": instantiate_scene_cache_order.duplicate(),
			"scenes": cached_scenes
		},
		"temp_preload_cache": {
			"size": temp_preloaded_resource_cache.size(),
			"max_size": max_temp_preload_resource_cache_size,
			"access_order": temp_preloaded_resource_cache_order.duplicate(),
			"scenes": temp_preloaded_scenes
		},
		"fixed_preload_cache": {
			"size": fixed_preload_resource_cache.size(),
			"max_size": max_fixed_preload_resource_cache_size,
			"access_order": fixed_preload_resource_cache_order.duplicate(),
			"scenes": fixed_preloaded_scenes
		},
		"preload_states": {
			"size": _preload_resource_states.size(),
			"states": preload_states_info
		}
	}

func is_scene_cached(scene_path: String) -> bool:
	return instantiate_scene_cache.has(scene_path) or temp_preloaded_resource_cache.has(scene_path) or fixed_preload_resource_cache.has(scene_path)

func is_scene_preloading(scene_path: String) -> bool:
	return _preload_resource_states.has(scene_path) and _preload_resource_states[scene_path]["state"] == LoadState.LOADING

func get_preloading_scenes() -> Array:
	var loading = []
	for path in _preload_resource_states:
		if _preload_resource_states[path]["state"] == LoadState.LOADING:
			loading.append(path)
	return loading

func get_current_scene() -> Node:
	return current_scene

func get_previous_scene_path() -> String:
	return previous_scene_path

func get_loading_progress(scene_path: String) -> float:
	if _preload_resource_states.has(scene_path):
		var state = _preload_resource_states[scene_path]["state"]
		if state == LoadState.LOADING:
			var progress = []
			var status = ResourceLoader.load_threaded_get_status(scene_path, progress)
			if status == ResourceLoader.THREAD_LOAD_IN_PROGRESS and progress.size() > 0:
				return progress[0]
			return 0.0
		elif state == LoadState.LOADED:
			return 1.0

	return 1.0 if (instantiate_scene_cache.has(scene_path) or temp_preloaded_resource_cache.has(scene_path) or fixed_preload_resource_cache.has(scene_path)) else 0.0

func get_resource_file_size(scene_path: String) -> int:
	if not ResourceLoader.exists(scene_path):
		return -1

	var file = FileAccess.open(scene_path, FileAccess.READ)
	if file == null:
		return -1

	var size = file.get_length()
	file.close()
	return size

func get_resource_file_size_formatted(scene_path: String) -> String:
	var size = get_resource_file_size(scene_path)
	if size < 0:
		return "N/A"

	if size < 1024:
		return str(size) + " B"
	elif size < 1024 * 1024:
		return str(size / 1024.0) + " KB"
	elif size < 1024 * 1024 * 1024:
		return str(size / (1024.0 * 1024.0)) + " MB"
	else:
		return str(size / (1024.0 * 1024.0 * 1024.0)) + " GB"

func get_resource_info(scene_path: String) -> Dictionary:
	var info = {
		"path": scene_path,
		"exists": ResourceLoader.exists(scene_path),
		"file_size_bytes": get_resource_file_size(scene_path),
		"file_size_formatted": get_resource_file_size_formatted(scene_path),
		"in_temp_cache": temp_preloaded_resource_cache.has(scene_path),
		"in_fixed_cache": fixed_preload_resource_cache.has(scene_path),
		"in_instance_cache": instantiate_scene_cache.has(scene_path),
		"is_preloading": is_scene_preloading(scene_path),
		"loading_progress": get_loading_progress(scene_path)
	}

	if _preload_resource_states.has(scene_path):
		info["preload_state"] = _preload_resource_states[scene_path]["state"]
		info["is_fixed_preload"] = _preload_resource_states[scene_path].get("fixed", false)

	return info

func is_in_fixed_cache(scene_path: String) -> bool:
	return fixed_preload_resource_cache.has(scene_path)

# ==================== Public API - Debug ====================
# ==================== 公开API - 调试 ====================

func print_debug_info() -> void:
	print("\n=== SceneManager Debug Info ===")
	print("Current scene: ", current_scene_path if current_scene else "None")
	print("Previous scene: ", previous_scene_path)

	print("\n[Instance Cache] Count: ", instantiate_scene_cache.size(), "/", max_cache_size)
	print("  Access order: ", instantiate_scene_cache_order)
	print("  Scenes: ", instantiate_scene_cache.keys())

	print("\n[Temp Preload Cache] Count: ", temp_preloaded_resource_cache.size(), "/", max_temp_preload_resource_cache_size)
	print("  Access order: ", temp_preloaded_resource_cache_order)
	print("  Scenes: ", temp_preloaded_resource_cache.keys())

	print("\n[Fixed Preload Cache] Count: ", fixed_preload_resource_cache.size(), "/", max_fixed_preload_resource_cache_size)
	print("  Access order: ", fixed_preload_resource_cache_order)
	print("  Scenes: ", fixed_preload_resource_cache.keys())

	print("\n[Preload States] Count: ", _preload_resource_states.size())
	for path in _preload_resource_states:
		var state_info = _preload_resource_states[path]
		print("  ", path, " -> ", state_info["state"], " | fixed: ", state_info.get("fixed", false), " | has_resource: ", state_info["resource"] != null)

	print("\nDefault loading screen: ", "Loaded" if default_load_screen else "Not loaded")
	print("Active loading screen: ", "Yes" if active_load_screen else "No")
	print("Using asynchronous loading: ", use_async_loading)
	print("Always use default loading screen: ", always_use_default_load_screen)
	print("===============================\n")

# ==================== Public API - Signal Helpers ====================
# ==================== 公开API - 信号辅助 ====================

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

# ==================== Private Functions - Initialization ====================
# ==================== 私有函数 - 初始化 ====================

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

# ==================== Private Functions - Preload Resource State ====================
# ==================== 私有函数 - 预加载资源状态 ====================

func _get_preload_resource_state(scene_path: String) -> Dictionary:
	if not _preload_resource_states.has(scene_path):
		_preload_resource_states[scene_path] = {"state": LoadState.NOT_LOADED, "resource": null, "fixed": false}
	return _preload_resource_states[scene_path]

func _clear_preload_resource_state(scene_path: String) -> void:
	if _preload_resource_states.has(scene_path):
		_preload_resource_states.erase(scene_path)

# ==================== Private Functions - Preload Core ====================
# ==================== 私有函数 - 预加载核心 ====================

func _preload_background(scene_path: String) -> void:
	if use_async_loading:
		await _async_preload_scene(scene_path)
	else:
		_sync_preload_scene(scene_path)

	if not _preload_resource_states.has(scene_path):
		print("[SceneManager] Preload state cleared: ", scene_path)
		return

	var preload_state = _preload_resource_states[scene_path]

	if preload_state["state"] == LoadState.CANCELLED:
		print("[SceneManager] Preload was cancelled: ", scene_path)
		_clear_preload_resource_state(scene_path)
		return

	if preload_state["state"] != LoadState.LOADING:
		print("[SceneManager] Preload state changed unexpectedly: ", scene_path)
		return

	if not preload_state["resource"]:
		preload_state["state"] = LoadState.NOT_LOADED
		preload_state["resource"] = null
		_clear_preload_resource_state(scene_path)
		scene_preload_failed.emit(scene_path)
		print("[SceneManager] Preloading failed: ", scene_path)
		return

	var is_fixed = preload_state.get("fixed", false)

	if is_fixed:
		if fixed_preload_resource_cache_order.size() >= max_fixed_preload_resource_cache_size and max_fixed_preload_resource_cache_size > 0:
			_remove_oldest_fixed_preload_resource()
		fixed_preload_resource_cache[scene_path] = preload_state["resource"]
		fixed_preload_resource_cache_order.append(scene_path)
		print("[SceneManager] Preloading complete, fixed resource cached: ", scene_path)
	else:
		if temp_preloaded_resource_cache_order.size() >= max_temp_preload_resource_cache_size:
			_remove_oldest_temp_preload_resource()
		temp_preloaded_resource_cache[scene_path] = preload_state["resource"]
		temp_preloaded_resource_cache_order.append(scene_path)
		print("[SceneManager] Preloading complete, temp resource cached: ", scene_path)

	preload_state["state"] = LoadState.LOADED
	scene_preload_completed.emit(scene_path)

func _async_preload_scene(scene_path: String) -> void:
	print("[SceneManager] Asynchronous preload: ", scene_path)

	var load_start_time = Time.get_ticks_msec()
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
				var preload_state = _get_preload_resource_state(scene_path)
				preload_state["resource"] = ResourceLoader.load_threaded_get(scene_path)
				print("[SceneManager] Asynchronous preload completed: ", scene_path)
				return

			ResourceLoader.THREAD_LOAD_FAILED:
				push_error("[SceneManager] Asynchronous loading failed: ", scene_path)
				var preload_state = _get_preload_resource_state(scene_path)
				preload_state["resource"] = null
				return

			_:
				push_error("[SceneManager] Unknown loading status: ", status)
				var preload_state = _get_preload_resource_state(scene_path)
				preload_state["resource"] = null
				return

func _sync_preload_scene(scene_path: String) -> void:
	print("[SceneManager] Synchronous preload: ", scene_path)
	var preload_state = _get_preload_resource_state(scene_path)
	preload_state["resource"] = load(scene_path)

# ==================== Private Functions - Load Screen ====================
# ==================== 私有函数 - 加载屏幕 ====================

func _get_load_screen_instance(load_screen_path: String) -> Node:
	if load_screen_path == "" or load_screen_path == "default":
		return default_load_screen
	elif load_screen_path == "no_transition":
		return null
	else:
		if ResourceLoader.exists(load_screen_path):
			var scene = load(load_screen_path)
			if scene:
				return scene.instantiate()
		return default_load_screen

func _show_load_screen(load_screen_instance: Node) -> void:
	if not load_screen_instance:
		return

	active_load_screen = load_screen_instance

	if load_screen_instance is CanvasItem:
		load_screen_instance.visible = true
	elif load_screen_instance.has_method("show"):
		load_screen_instance.show()
	elif load_screen_instance.has_method("set_visible"):
		load_screen_instance.set_visible(true)

	load_screen_shown.emit(load_screen_instance)

func _hide_load_screen(load_screen_instance: Node) -> void:
	if not load_screen_instance:
		return

	if load_screen_instance is CanvasItem:
		load_screen_instance.visible = false
	elif load_screen_instance.has_method("hide"):
		load_screen_instance.hide()
	elif load_screen_instance.has_method("set_visible"):
		load_screen_instance.set_visible(false)

	active_load_screen = null
	load_screen_hidden.emit(load_screen_instance)

# ==================== Private Functions - Scene Loading ====================
# ==================== 私有函数 - 场景加载 ====================

func _load_scene_by_method(scene_path: String, load_method: LoadMethod, cache_current_scene: bool, load_screen_instance: Node) -> void:
	_show_load_screen(load_screen_instance)

	match load_method:
		LoadMethod.DIRECT, 0:
			if temp_preloaded_resource_cache.has(scene_path) or fixed_preload_resource_cache.has(scene_path):
				print("[SceneManager] DIRECT: resource found in preload cache, using preloaded resource")
				await _handle_preloaded_resource(scene_path, load_screen_instance, cache_current_scene)
			else:
				print("[SceneManager] DIRECT: resource not in any cache, using async loading")
				await _load_and_switch(scene_path, load_screen_instance, cache_current_scene)
		LoadMethod.PRELOAD_CACHE, 1:
			if temp_preloaded_resource_cache.has(scene_path) or fixed_preload_resource_cache.has(scene_path):
				await _handle_preloaded_resource(scene_path, load_screen_instance, cache_current_scene)
			else:
				print("[SceneManager] PRELOAD_CACHE: resource not in preload cache, falling back to direct load")
				await _load_and_switch(scene_path, load_screen_instance, cache_current_scene)
		LoadMethod.SCENE_CACHE, 2:
			if instantiate_scene_cache.has(scene_path):
				await _handle_cached_scene(scene_path, load_screen_instance, cache_current_scene)
			else:
				print("[SceneManager] SCENE_CACHE: scene not in instance cache, falling back to direct load")
				await _load_and_switch(scene_path, load_screen_instance, cache_current_scene)
		LoadMethod.BOTH_PRELOAD_FIRST, 3:
			if temp_preloaded_resource_cache.has(scene_path) or fixed_preload_resource_cache.has(scene_path):
				await _handle_preloaded_resource(scene_path, load_screen_instance, cache_current_scene)
			elif instantiate_scene_cache.has(scene_path):
				await _handle_cached_scene(scene_path, load_screen_instance, cache_current_scene)
			else:
				await _handle_preloading_scene(scene_path, load_screen_instance, cache_current_scene)
		LoadMethod.BOTH_INSTANCE_FIRST, 4:
			if instantiate_scene_cache.has(scene_path):
				await _handle_cached_scene(scene_path, load_screen_instance, cache_current_scene)
			elif temp_preloaded_resource_cache.has(scene_path) or fixed_preload_resource_cache.has(scene_path):
				await _handle_preloaded_resource(scene_path, load_screen_instance, cache_current_scene)
			else:
				await _handle_preloading_scene(scene_path, load_screen_instance, cache_current_scene)
		_:
			push_error("[SceneManager] Error: Unknown load method: ", load_method)
			await _hide_load_screen(load_screen_instance)
			scene_switch_failed.emit(scene_path)

func _handle_preloaded_resource(scene_path: String, load_screen_instance: Node, use_cache: bool) -> void:
	print("[SceneManager] Handling preloaded resource: ", scene_path)
	await _instantiate_and_switch(scene_path, load_screen_instance, use_cache)

func _handle_preloading_scene(scene_path: String, load_screen_instance: Node, use_cache: bool) -> void:
	print("[SceneManager] Handling preloading scene: ", scene_path)

	var progress_array = []
	var status
	var load_start_time = Time.get_ticks_msec()

	ResourceLoader.load_threaded_request(scene_path, "", false, ResourceLoader.CACHE_MODE_IGNORE)

	while true:
		status = ResourceLoader.load_threaded_get_status(scene_path, progress_array)

		match status:
			ResourceLoader.THREAD_LOAD_IN_PROGRESS:
				var progress = progress_array[0] if progress_array.size() > 0 else 0.0
				if load_screen_instance and load_screen_instance.has_method("set_progress"):
					load_screen_instance.set_progress(progress)
				elif load_screen_instance and load_screen_instance.has_method("update_progress"):
					load_screen_instance.update_progress(progress)

				if Time.get_ticks_msec() - load_start_time > 500:
					print("[SceneManager] Preload scene loading progress: ", progress * 100, "%")
					load_start_time = Time.get_ticks_msec()

				await get_tree().process_frame

			ResourceLoader.THREAD_LOAD_LOADED:
				print("[SceneManager] Preload scene loading completed: ", scene_path)
				break

			ResourceLoader.THREAD_LOAD_FAILED:
				push_error("[SceneManager] Scene loading failed: ", scene_path)
				await _hide_load_screen(load_screen_instance)
				scene_switch_failed.emit(scene_path)
				return

			_:
				push_error("[SceneManager] Unknown loading status: ", status)
				await _hide_load_screen(load_screen_instance)
				scene_switch_failed.emit(scene_path)
				return

	var packed_scene = ResourceLoader.load_threaded_get(scene_path)
	if not packed_scene:
		push_error("[SceneManager] Scene resource retrieval failed: ", scene_path)
		await _hide_load_screen(load_screen_instance)
		scene_switch_failed.emit(scene_path)
		return

	print("[SceneManager] Instantiating scene: ", scene_path)
	var new_scene = await _instantiate_scene_deferred(packed_scene, load_screen_instance)
	if not new_scene:
		push_error("[SceneManager] Scene instantiation failed: ", scene_path)
		await _hide_load_screen(load_screen_instance)
		scene_switch_failed.emit(scene_path)
		return

	await _perform_scene_switch(new_scene, scene_path, load_screen_instance, use_cache)

func _handle_cached_scene(scene_path: String, load_screen_instance: Node, cache_current_scene: bool) -> void:
	print("[SceneManager] Handling cached scene: ", scene_path)
	await _switch_to_cached_scene(scene_path, load_screen_instance, cache_current_scene)

func _load_and_switch(scene_path: String, load_screen_instance: Node, current_scene_use_cache: bool) -> void:
	print("[SceneManager] Loading scene: ", scene_path)

	ResourceLoader.load_threaded_request(scene_path, "", false, ResourceLoader.CACHE_MODE_IGNORE)

	var progress_array = []
	var status
	var load_start_time = Time.get_ticks_msec()

	while true:
		status = ResourceLoader.load_threaded_get_status(scene_path, progress_array)

		match status:
			ResourceLoader.THREAD_LOAD_IN_PROGRESS:
				var progress = progress_array[0] if progress_array.size() > 0 else 0.0
				if load_screen_instance and load_screen_instance.has_method("set_progress"):
					load_screen_instance.set_progress(progress)
				elif load_screen_instance and load_screen_instance.has_method("update_progress"):
					load_screen_instance.update_progress(progress)

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

# ==================== Private Functions - Instantiate ====================
# ==================== 私有函数 - 实例化 ====================

func _instantiate_scene_deferred(packed_scene: PackedScene, load_screen_instance: Node = null) -> Node:
	for i in instantiate_frames:
		await get_tree().process_frame

	var instance = packed_scene.instantiate()
	if not instance:
		push_error("[SceneManager] Scene instantiation failed")
		return null

	return instance

func _instantiate_and_switch(scene_path: String, load_screen_instance: Node, use_cache: bool) -> void:
	var packed_scene
	var from_fixed = false

	if temp_preloaded_resource_cache.has(scene_path):
		packed_scene = temp_preloaded_resource_cache.get(scene_path)
		temp_preloaded_resource_cache.erase(scene_path)
		var index = temp_preloaded_resource_cache_order.find(scene_path)
		if index != -1:
			temp_preloaded_resource_cache_order.remove_at(index)
	elif fixed_preload_resource_cache.has(scene_path):
		packed_scene = fixed_preload_resource_cache.get(scene_path)
		from_fixed = true
		print("[SceneManager] Using from fixed cache (copy mode): ", scene_path)
	else:
		push_error("[SceneManager] Preloaded resource does not exist: ", scene_path)
		await _hide_load_screen(load_screen_instance)
		scene_switch_failed.emit(scene_path)
		return

	print("[SceneManager] Instantiating preloaded scene: ", scene_path)

	var new_scene = await _instantiate_scene_deferred(packed_scene, load_screen_instance)
	if not new_scene:
		push_error("[SceneManager] Scene instantiation failed")
		await _hide_load_screen(load_screen_instance)
		scene_switch_failed.emit(scene_path)
		return

	await _perform_scene_switch(new_scene, scene_path, load_screen_instance, use_cache)

func _switch_to_cached_scene(scene_path: String, load_screen_instance: Node, cache_current_scene: bool) -> void:
	if not instantiate_scene_cache.has(scene_path):
		push_error("[SceneManager] Scene not found in cache: ", scene_path)
		await _hide_load_screen(load_screen_instance)
		return

	var cached = instantiate_scene_cache[scene_path]
	if not is_instance_valid(cached.scene_instance):
		push_error("[SceneManager] Cached scene instance is invalid")
		instantiate_scene_cache.erase(scene_path)
		var index = instantiate_scene_cache_order.find(scene_path)
		if index != -1:
			instantiate_scene_cache_order.remove_at(index)
		await _hide_load_screen(load_screen_instance)
		return

	print("[SceneManager] Using cached scene: ", scene_path)

	var scene_instance = cached.scene_instance

	instantiate_scene_cache.erase(scene_path)
	var index = instantiate_scene_cache_order.find(scene_path)
	if index != -1:
		instantiate_scene_cache_order.remove_at(index)

	if scene_instance.is_inside_tree():
		scene_instance.get_parent().remove_child(scene_instance)

	await _perform_scene_switch(scene_instance, scene_path, load_screen_instance, cache_current_scene)

# ==================== Private Functions - Scene Switch ====================
# ==================== 私有函数 - 场景切换 ====================

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

		if current_scene_use_cache and old_scene_path != "" and old_scene_path != new_scene_path:
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

# ==================== Private Functions - Cache Management ====================
# ==================== 私有函数 - 缓存管理 ====================

func _add_to_cache(scene_path: String, scene_instance: Node) -> void:
	if scene_path == "" or not scene_instance:
		print("[SceneManager] Warning: Cannot cache empty scene or path")
		return

	if instantiate_scene_cache.has(scene_path):
		print("[SceneManager] Scene already in instance cache: ", scene_path)
		var old_cached = instantiate_scene_cache[scene_path]
		if is_instance_valid(old_cached.scene_instance):
			_cleanup_orphaned_nodes(old_cached.scene_instance)
			old_cached.scene_instance.queue_free()
		instantiate_scene_cache.erase(scene_path)
		var index = instantiate_scene_cache_order.find(scene_path)
		if index != -1:
			instantiate_scene_cache_order.remove_at(index)

	_cleanup_orphaned_nodes(scene_instance)

	if scene_instance.is_inside_tree():
		push_error("[SceneManager] Error: Attempting to cache node still in scene tree")
		scene_instance.get_parent().remove_child(scene_instance)

	print("[SceneManager] Adding to instance cache: ", scene_path)

	var cached = CachedScene.new(scene_instance)
	instantiate_scene_cache[scene_path] = cached
	instantiate_scene_cache_order.append(scene_path)
	scene_cached.emit(scene_path)

	if _preload_resource_states.has(scene_path):
		_preload_resource_states[scene_path]["state"] = LoadState.INSTANTIATED

	if instantiate_scene_cache_order.size() > max_cache_size:
		_remove_oldest_cached_scene()

func _remove_oldest_cached_scene() -> void:
	if instantiate_scene_cache_order.size() == 0:
		return

	var oldest_path = instantiate_scene_cache_order[0]
	instantiate_scene_cache_order.remove_at(0)

	if instantiate_scene_cache.has(oldest_path):
		var cached = instantiate_scene_cache[oldest_path]
		if is_instance_valid(cached.scene_instance):
			_cleanup_orphaned_nodes(cached.scene_instance)
			cached.scene_instance.queue_free()
		instantiate_scene_cache.erase(oldest_path)
		scene_removed_from_cache.emit(oldest_path)
		print("[SceneManager] Removing old cache: ", oldest_path)

	if _preload_resource_states.has(oldest_path):
		_clear_preload_resource_state(oldest_path)

func _remove_oldest_temp_preload_resource() -> void:
	if temp_preloaded_resource_cache_order.size() == 0:
		return

	var oldest_path = temp_preloaded_resource_cache_order[0]
	temp_preloaded_resource_cache_order.remove_at(0)

	if temp_preloaded_resource_cache.has(oldest_path):
		temp_preloaded_resource_cache.erase(oldest_path)
		scene_removed_from_cache.emit(oldest_path)
		print("[SceneManager] Removing old temp preload resource: ", oldest_path)

func _remove_oldest_fixed_preload_resource() -> void:
	if fixed_preload_resource_cache_order.size() == 0:
		return

	var oldest_path = fixed_preload_resource_cache_order[0]
	fixed_preload_resource_cache_order.remove_at(0)

	if fixed_preload_resource_cache.has(oldest_path):
		fixed_preload_resource_cache.erase(oldest_path)
		scene_removed_from_cache.emit(oldest_path)
		print("[SceneManager] Removing oldest fixed preload resource (FIFO): ", oldest_path)

# ==================== Private Functions - Cleanup ====================
# ==================== 私有函数 - 清理 ====================

func _cleanup_orphaned_nodes(root_node: Node) -> void:
	if not root_node or not is_instance_valid(root_node):
		return

	if root_node.is_inside_tree():
		var parent = root_node.get_parent()
		if parent:
			parent.remove_child(root_node)

	for child in root_node.get_children():
		_cleanup_orphaned_nodes(child)

func _debug_validate_scene_tree() -> void:
	var root = get_tree().root
	var current = get_tree().current_scene

	print("[SceneManager] Scene tree validation - Root node child count: ", root.get_child_count())
	print("[SceneManager] Current scene: ", current.name if current else "None")

	for scene_path in instantiate_scene_cache:
		var cached = instantiate_scene_cache[scene_path]
		if is_instance_valid(cached.scene_instance) and cached.scene_instance.is_inside_tree():
			push_error("[SceneManager] Error: Cached node still in scene tree: ", scene_path)

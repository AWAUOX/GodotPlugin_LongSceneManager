# Long Scene Manager Plugin

**<font size="3">[中文文档 链接](README_中文.md)</font>**<br>
Long Scene Manager is a powerful and easy-to-use Godot 4.x scene management plugin. It is specifically designed to solve various pain points in complex scene switching and multi-scene management in game projects. The plugin provides built-in asynchronous loading technology, an intelligent three-tier caching system, and customizable loading transitions, making your game scene switching efficient, smooth, controllable, and predictable.

Whether you are an indie game developer or a commercial project with a full team, Long Scene Manager can help you:

- 🚀 **Eliminate Loading Stutters** - Fully asynchronous loading mechanism makes loading large scenes seamless
- 💾 **Smart Memory Management** - LRU/FIFO hybrid caching strategy ensures memory is used wisely
- ✨ **Ultimate User Experience** - Custom loading screens, fade in/out effects, progress bar display, turns waiting into enjoyment
- 🎯 **Flexible Scene Reuse** - Preserve scene states, switch anytime, making game flow smoother
- 🔧 **Zero Learning Curve** - Clean API design, can be integrated with just a few lines of code

> 🌟 **Special Note**: The plugin provides both GDScript and C# versions with identical functionality. No matter which language you use, you can enjoy the same features and performance!

---

<img src="./addons/long_scene_manager/image_icon/icon3.png" width = 30%>

---

## Support the Developer

If you find this plugin helpful for your project, please:

- ⭐ **Give the GitHub repository a Star**: [https://github.com/AWAUOX/GodotPlugin_LongSceneManager](https://github.com/AWAUOX/GodotPlugin_LongSceneManager)
- 🎮 **Mention this plugin and its author LongZhan in your game's credits**
- 📢 **Recommend this plugin to other Godot developers**

Your support is my motivation to keep improving!

---

## Table of Contents

- [Long Scene Manager Plugin](#long-scene-manager-plugin)
  - [Support the Developer](#support-the-developer)
  - [Table of Contents](#table-of-contents)
  - [Plugin Overview](#plugin-overview)
    - [Why Do You Need This Plugin?](#why-do-you-need-this-plugin)
    - [Core Features](#core-features)
  - [Basic Usage](#basic-usage)
    - [Installation and Setup](#installation-and-setup)
    - [Plugin Configuration (Exported Variables)](#plugin-configuration-exported-variables)
    - [GDScript Usage](#gdscript-usage)
    - [C# Usage](#c-usage)
    - [Basic Scene Switching](#basic-scene-switching)
    - [Scene Preloading](#scene-preloading)
    - [Complete Preload + Switch Flow](#complete-preload--switch-flow)
  - [Advanced Usage](#advanced-usage)
    - [Five Loading Strategies Explained](#five-loading-strategies-explained)
    - [Three-Tier Cache System](#three-tier-cache-system)
    - [Cache Management API](#cache-management-api)
    - [Signal System](#signal-system)
  - [Plugin Development Guide](#plugin-development-guide)
    - [Core Design Principles](#core-design-principles)
    - [Code Structure Analysis](#code-structure-analysis)
    - [Runtime Logic Flow](#runtime-logic-flow)
      - [Scene Switching Flow](#scene-switching-flow)
      - [Preloading Flow](#preloading-flow)
    - [Key Data Structures](#key-data-structures)
      - [LoadState Enum](#loadstate-enum)
      - [CachedScene Internal Class](#cachedscene-internal-class)
      - [Cache State Dictionary](#cache-state-dictionary)
  - [Complete API Reference](#complete-api-reference)
    - [Scene Switching](#scene-switching)
    - [Preloading](#preloading)
    - [Cache Management](#cache-management)
    - [Query](#query)
    - [Debugging](#debugging)
  - [Loading Screen Customization](#loading-screen-customization)
    - [Using Default Loading Screen](#using-default-loading-screen)
    - [Creating Custom Loading Screen](#creating-custom-loading-screen)
    - [Using Custom Loading Screen](#using-custom-loading-screen)
  - [Screenshots](#screenshots)
  - [Support the Developer](#support-the-developer-1)
    - [🌟 Give the Project a Star](#-give-the-project-a-star)
    - [📣 Recommend to Other Developers](#-recommend-to-other-developers)
    - [🏆 Credit in Your Game](#-credit-in-your-game)
    - [🐛 Feedback and Contribution](#-feedback-and-contribution)
    - [💬 Contact the Author](#-contact-the-author)

---

## Plugin Overview

### Why Do You Need This Plugin?

Godot engine's built-in `change_scene_to_file()` and `preload()` have the following issues in certain cases:

| Issue | Description |
|------|-------------|
| **Blocking Main Thread** | Large scene loading may affect game smoothness and cause stuttering |
| **Lack of Cache Management** | No built-in LRU/FIFO cache eviction strategy; switching back requires reloading |
| **No Loading Screen Support** | Cannot conveniently display custom loading screens and progress bars |
| **Memory Waste** | Cannot flexibly control which scenes stay in memory and which should be released |

### Core Features

- ✅ **Fully Asynchronous Loading** - Non-blocking main thread with progress callbacks
- ✅ **Three-Tier Cache System** - Instance cache + temp preload cache + fixed preload cache
- ✅ **Five Loading Strategies** - Flexible control over cache lookup priority
- ✅ **Customizable Loading Screens** - Supports fade in/out, progress bars, and more
- ✅ **Multi-Frame Instantiation** - Avoids stuttering when instantiating large scenes
- ✅ **Complete Signal System** - Easy monitoring and debugging

---

## Basic Usage

### Installation and Setup

1. Copy the `addons/long_scene_manager` folder into your project's `addons` folder
2. Enable the plugin in Godot:
   - Go to `Project → Project Settings → Plugins`
   - Find "Long Scene Manager" and set its status to "Enabled"
3. The plugin will automatically register as an Autoload singleton:
   - **GDScript version**: Access name is `LongSceneManager`
   - **C# version**: Access name is `LongSceneManagerCs`

> **Note**: Both versions have identical functionality. Which one to use depends on your project's language environment. The GDScript version works out of the box, while the C# version requires your project to have C# support (.NET) configured.

### Plugin Configuration (Exported Variables)

Select the `LongSceneManager` node in the editor to configure the following parameters in the inspector:

| Variable | Type | Default | Description |
|----------|------|---------|-------------|
| `max_cache_size` | int (1~20) | 4 | Maximum instance cache capacity; LRU eviction when exceeded |
| `max_temp_preload_resource_cache_size` | int (1~50) | 8 | Maximum temp preload resource cache capacity; FIFO eviction when exceeded |
| `max_fixed_preload_resource_cache_size` | int (0~50) | 4 | Maximum fixed preload resource cache capacity; FIFO eviction when exceeded |
| `use_async_loading` | bool | true | Whether to use async loading (recommended) |
| `always_use_default_load_screen` | bool | false | Whether to force using the default loading screen |
| `instantiate_frames` | int (1~10) | 3 | Number of frames to delay during scene instantiation |

### GDScript Usage

```gdscript
# Access singleton
var manager = LongSceneManager

# Scene switching
manager.switch_scene("res://scenes/level_02.tscn")

# Preloading
manager.preload_scene("res://scenes/level_02.tscn")

# Get cache info
var info = manager.get_cache_info()
```

### C# Usage

```csharp
// Access singleton
var manager = LongSceneManagerCs.Instance;

// Scene switching
await manager.SwitchScene("res://scenes/Level02.tscn", LongSceneManagerCs.LoadMethod.BothPreloadFirst, true, "");

// Preloading
manager.PreloadScene("res://scenes/Level02.tscn");

// Batch preloading
manager.PreloadScenes(new string[] { "res://scenes/Level02.tscn", "res://scenes/Level03.tscn" });

// Cancel preloading
manager.CancelPreloadingScene("res://scenes/Level03.tscn");

// Get cache info
var cacheInfo = manager.GetCacheInfo();

// Listen to signals
manager.SceneSwitchStarted += OnSceneSwitchStarted;
manager.SceneSwitchCompleted += OnSceneSwitchCompleted;

private void OnSceneSwitchStarted(string fromScene, string toScene)
{
    GD.Print($"Switch: {fromScene} -> {toScene}");
}

private void OnSceneSwitchCompleted(string scenePath)
{
    GD.Print($"Completed: {scenePath}");
}
```

### Basic Scene Switching

`switch_scene()` / `SwitchScene()` is the core scene switching function:

**GDScript:**
```gdscript
# Simplest switch (using default strategy and default loading screen)
LongSceneManager.switch_scene("res://scenes/level_02.tscn")

# Don't cache current scene
LongSceneManager.switch_scene("res://scenes/main_menu.tscn", LongSceneManager.LoadMethod.BOTH_PRELOAD_FIRST, false)

# Use SCENE_CACHE strategy (prioritize reusing instances)
await LongSceneManager.switch_scene("res://scenes/level_02.tscn", LongSceneManager.LoadMethod.SCENE_CACHE)

# No transition switch
LongSceneManager.switch_scene("res://scenes/level_02.tscn", LongSceneManager.LoadMethod.BOTH_PRELOAD_FIRST, true, "no_transition")
```

**C#:**
```csharp
// Simplest switch (using default strategy and default loading screen)
await LongSceneManagerCs.Instance.SwitchScene("res://scenes/Level02.tscn");

// Don't cache current scene
await LongSceneManagerCs.Instance.SwitchScene("res://scenes/MainMenu.tscn", LongSceneManagerCs.LoadMethod.BothPreloadFirst, false, "");

// Use SceneCache strategy (prioritize reusing instances)
await LongSceneManagerCs.Instance.SwitchScene("res://scenes/Level02.tscn", LongSceneManagerCs.LoadMethod.SceneCache, true, "");

// No transition switch
await LongSceneManagerCs.Instance.SwitchScene("res://scenes/Level02.tscn", LongSceneManagerCs.LoadMethod.BothPreloadFirst, true, "no_transition");
```

### Scene Preloading

Preloading allows you to load scene resources in the background ahead of time, so switching is nearly instantaneous when needed:

**GDScript:**
```gdscript
# Temp preloading: resource may be automatically evicted
LongSceneManager.preload_scene("res://scenes/level_02.tscn")

# Fixed preloading: resource won't be auto-evicted, can be instantiated multiple times
LongSceneManager.preload_scene("res://scenes/boss_fight.tscn", true)

# Batch preloading
LongSceneManager.preload_scenes(["res://scenes/level_02.tscn", "res://scenes/level_03.tscn"])

# Cancel preloading
LongSceneManager.cancel_preloading_scene("res://scenes/level_03.tscn")
```

**C#:**
```csharp
// Temp preloading: resource may be automatically evicted
LongSceneManagerCs.Instance.PreloadScene("res://scenes/Level02.tscn");

// Fixed preloading: resource won't be auto-evicted, can be instantiated multiple times
LongSceneManagerCs.Instance.PreloadScene("res://scenes/BossFight.tscn", true);

// Batch preloading
LongSceneManagerCs.Instance.PreloadScenes(new string[] { "res://scenes/Level02.tscn", "res://scenes/Level03.tscn" });

// Cancel preloading
LongSceneManagerCs.Instance.CancelPreloadingScene("res://scenes/Level03.tscn");

// Cancel all preloading
LongSceneManagerCs.Instance.CancelAllPreloading();
```

### Complete Preload + Switch Flow

**GDScript:**
```gdscript
# 1. After entering a level, preload the next level
func _on_level_01_ready():
    LongSceneManager.preload_scene("res://scenes/level_02.tscn")

# 2. Player reaches exit, switch scene
func _on_exit_reached():
    await LongSceneManager.switch_scene("res://scenes/level_02.tscn")
```

**C#:**
```csharp
// 1. After entering a level, preload the next level
public override void _Ready()
{
    LongSceneManagerCs.Instance.PreloadScene("res://scenes/Level02.tscn");
}

// 2. Player reaches exit, switch scene
private async void OnExitReached()
{
    await LongSceneManagerCs.Instance.SwitchScene("res://scenes/Level02.tscn");
}
```

---

## Advanced Usage

### Five Loading Strategies Explained

When switching scenes, the `load_method` parameter controls cache lookup behavior:

| Strategy | Enum Value | Lookup Order | Use Case |
|----------|-----------|--------------|----------|
| `DIRECT` | Direct load | Preload cache → direct load | Don't care about instance cache, just need the resource |
| `PRELOAD_CACHE` | Preload first | Preload cache → fallback direct load | Only use preloaded resources |
| `SCENE_CACHE` | Instance cache first | Instance cache → fallback direct load | Prioritize reusing instantiated scenes (preserve state) |
| `BOTH_PRELOAD_FIRST` | Hybrid preload first | Preload → instance → preloading → direct | **Default strategy**, recommended for most cases |
| `BOTH_INSTANCE_FIRST` | Hybrid instance first | Instance → preload → preloading → direct | Prioritize reusing scene instances (preserve state) |

> **Important**: Only when hitting the **instance cache** will an already-instantiated scene be reused. That scene will **retain its complete runtime state from before the switch**.

### Three-Tier Cache System

```
┌─────────────────────────────────────────────────────────┐
│                    LongSceneManager                      │
├─────────────────────────────────────────────────────────┤
│  Tier 1: Instance Cache                                  │
│  Stores: Complete scene Node + cache timestamp          │
│  Eviction: LRU (Least Recently Used)                    │
│  Default capacity: 4                                    │
│  Feature: Preserves complete runtime state before switch│
├─────────────────────────────────────────────────────────┤
│  Tier 2: Temp Preload Cache                             │
│  Stores: PackedScene resource                           │
│  Eviction: FIFO (First In, First Out)                  │
│  Default capacity: 8                                   │
│  Feature: Resource is removed from cache after use      │
│           (consumable)                                  │
├─────────────────────────────────────────────────────────┤
│  Tier 3: Fixed Preload Cache                             │
│  Stores: PackedScene resource                           │
│  Eviction: FIFO (First In, First Out)                  │
│  Default capacity: 4                                   │
│  Feature: Resource remains in cache after use          │
│           (persistent)                                 │
└─────────────────────────────────────────────────────────┘
```

**Core Design Principle**: A scene instance is either in the scene tree (currently active) or in the cache (inactive but retained in memory). The two are strictly separated and never coexist.

### Cache Management API

**GDScript:**
```gdscript
# Clear all cache
LongSceneManager.clear_all_cache()

# Clear specific cache
LongSceneManager.clear_temp_preload_cache()  # Temp preload cache
LongSceneManager.clear_fixed_cache()          # Fixed preload cache
LongSceneManager.clear_instance_cache()       # Instance cache

# Remove single resource
LongSceneManager.remove_temp_resource("res://scenes/level_02.tscn")
LongSceneManager.remove_fixed_resource("res://scenes/shop.tscn")
LongSceneManager.remove_cached_scene("res://scenes/level_02.tscn")

# Move resource
LongSceneManager.move_to_fixed("res://scenes/boss_fight.tscn")  # Temp → Fixed
LongSceneManager.move_to_temp("res://scenes/boss_fight.tscn")   # Fixed → Temp

# Dynamically adjust cache size
LongSceneManager.set_max_cache_size(10)
```

**C#:**
```csharp
// Clear all cache
LongSceneManagerCs.Instance.ClearAllCache();

// Clear specific cache
LongSceneManagerCs.Instance.ClearTempPreloadCache();   // Temp preload cache
LongSceneManagerCs.Instance.ClearFixedCache();          // Fixed preload cache
LongSceneManagerCs.Instance.ClearInstanceCache();     // Instance cache

// Remove single resource
LongSceneManagerCs.Instance.RemoveTempResource("res://scenes/Level02.tscn");
LongSceneManagerCs.Instance.RemoveFixedResource("res://scenes/Shop.tscn");
LongSceneManagerCs.Instance.RemoveCachedScene("res://scenes/Level02.tscn");

// Move resource
LongSceneManagerCs.Instance.MoveToFixed("res://scenes/BossFight.tscn");  // Temp → Fixed
LongSceneManagerCs.Instance.MoveToTemp("res://scenes/BossFight.tscn");   // Fixed → Temp

// Dynamically adjust cache size
LongSceneManagerCs.Instance.SetMaxCacheSize(10);
```

### Signal System

**GDScript:**
```gdscript
# Listen to scene switching events
LongSceneManager.scene_switch_started.connect(_on_switch_started)
LongSceneManager.scene_switch_completed.connect(_on_switch_completed)
LongSceneManager.scene_preload_started.connect(_on_preload_started)
LongSceneManager.scene_preload_completed.connect(_on_preload_completed)
LongSceneManager.scene_cached.connect(_on_scene_cached)

# Or use auto-connect
LongSceneManager.connect_all_signals(self)
# Then implement corresponding method: _on_scene_manager_scene_switch_started()
```

**C#:**
```csharp
// Listen to scene switching events
LongSceneManagerCs.Instance.SceneSwitchStarted += OnSceneSwitchStarted;
LongSceneManagerCs.Instance.SceneSwitchCompleted += OnSceneSwitchCompleted;
LongSceneManagerCs.Instance.ScenePreloadStarted += OnScenePreloadStarted;
LongSceneManagerCs.Instance.ScenePreloadCompleted += OnScenePreloadCompleted;
LongSceneManagerCs.Instance.SceneCached += OnSceneCached;

// Unsubscribe
LongSceneManagerCs.Instance.SceneSwitchStarted -= OnSceneSwitchStarted;
```

**Available Signals:**

| Signal | Parameters | Description |
|--------|-----------|--------------|
| `scene_preload_started` | scene_path | Preloading started |
| `scene_preload_completed` | scene_path | Preloading completed |
| `scene_preload_cancelled` | scene_path | Preloading cancelled |
| `scene_switch_started` | from_scene, to_scene | Switching started |
| `scene_switch_completed` | scene_path | Switching completed |
| `scene_cached` | scene_path | Scene cached |
| `scene_removed_from_cache` | scene_path | Scene removed from cache |
| `load_screen_shown` | instance | Loading screen shown |
| `load_screen_hidden` | instance | Loading screen hidden |
| `scene_preload_failed` | scene_path | Preloading failed |
| `scene_switch_failed` | scene_path | Switching failed |

---

## Plugin Development Guide

### Core Design Principles

1. **Strict Separation of Scene Tree and Cache**
   - A scene instance is either in the scene tree (currently active) or in the cache
   - The two never coexist, which avoids node management chaos

2. **State Machine Driven**
   - Each scene resource goes through five states: `NOT_LOADED → LOADING → LOADED → INSTANTIATED`
   - Supports `CANCELLED` state for cancelled preloading

3. **Async First**
   - Resource loading uses `ResourceLoader.load_threaded_request()`
   - Multi-frame instantiation avoids main thread stuttering

4. **Extensible Loading Screen**
   - Supports custom loading screen scenes
   - Compatibility through method name conventions

### Code Structure Analysis

```
addons/long_scene_manager/
├── autoload/
│   ├── long_scene_manager.gd       # GDScript core manager (singleton)
│   └── LongSceneManagerCs.cs       # C# core manager (singleton)
├── ui/
│   └── loading_screen/
│       ├── GDScript/               # GDScript version loading screen
│       │   └── loading_black_screen.tscn
│       └── CSharp/                 # C# version loading screen
│           └── loading_black_screen_cs.tscn
└── image_icon/
    └── icon3.png
```

**long_scene_manager.gd core code sections:**

| Section | Line Range | Description |
|---------|-----------|--------------|
| Constants and Enums | 1-50 | LoadState, LoadMethod enum definitions |
| Signal Definitions | 52-65 | All scene event signals |
| Exported Variables | 67-75 | Plugin configuration items |
| Internal State | 77-95 | Cache and scene state variables |
| Internal Class | 96-102 | CachedScene data class |
| Lifecycle | 104-115 | _ready() initialization |
| Public API - Scene Switching | 117-145 | switch_scene() |
| Public API - Preloading | 147-185 | preload_*() |
| Public API - Cache Management | 187-320 | clear_*, remove_*, move_* |
| Public API - Query | 322-420 | get_cache_info(), get_*() |
| Public API - Debugging | 422-450 | print_debug_info() |
| Private Functions - Initialization | 550-600 | _init_default_load_screen() |
| Private Functions - Preloading Core | 650-750 | _async_preload_scene() |
| Private Functions - Loading Screen | 760-830 | _show_load_screen() |
| Private Functions - Scene Loading | 850-1000 | _load_scene_by_method() |
| Private Functions - Instantiation | 1000-1100 | _instantiate_scene_deferred() |
| Private Functions - Cache Management | 1100-1200 | _add_to_cache() |

### Runtime Logic Flow

#### Scene Switching Flow

```
switch_scene() is called
    │
    ├─► Validate scene path
    │
    ├─► Emit scene_switch_started signal
    │
    ├─► Get loading screen instance
    │
    ├─► Show loading screen (_show_load_screen)
    │       │
    │       ├─► Set visibility
    │       │
    │       └─► Call fade_in() or show_loading()
    │
    ├─► Select loading strategy based on LoadMethod
    │       │
    │       ├─► DIRECT: Direct load or use preload resource
    │       │
    │       ├─► PRELOAD_CACHE: Only check preload cache
    │       │
    │       ├─► SCENE_CACHE: Only check instance cache
    │       │
    │       ├─► BOTH_PRELOAD_FIRST: preload → instance → preloading → direct
    │       │
    │       └─► BOTH_INSTANCE_FIRST: instance → preload → preloading → direct
    │
    ├─► Execute scene switch (_perform_scene_switch)
    │       │
    │       ├─► Remove old scene
    │       │
    │       ├─► If caching needed, add to instance cache
    │       │
    │       ├─► Add new scene to scene tree
    │       │
    │       └─► Wait for ready signal
    │
    ├─► Hide loading screen (_hide_load_screen)
    │       │
    │       ├─► Call fade_out() or hide_loading()
    │       │
    │       └─► Cleanup custom loading screen
    │
    └─► Emit scene_switch_completed signal
```

#### Preloading Flow

```
preload_scene() is called
    │
    ├─► Check if already in cache or loading
    │
    ├─► Create preload state (LoadState.LOADING)
    │
    ├─► Emit scene_preload_started signal
    │
    └─► Start background preload (_preload_background)
            │
            ├─► Async mode: ResourceLoader.load_threaded_request()
            │       │
            │       └─► Poll status until LOADED
            │
            └─► Sync mode: Direct load()
                    │
                    └─► Store to preload cache
                            │
                            ├─► Temp cache (FIFO eviction)
                            └─► Fixed cache (persistent)
```

### Key Data Structures

#### LoadState Enum

```gdscript
enum LoadState {
    NOT_LOADED,      # Initial state
    LOADING,         # Async loading in progress
    LOADED,          # Resource loaded (not instantiated)
    INSTANTIATED,    # Instantiated (in instance cache)
    CANCELLED        # Preloading cancelled
}
```

#### CachedScene Internal Class

```gdscript
class CachedScene:
    var scene_instance: Node   # Scene instance
    var cached_time: float     # Cache timestamp

    func _init(scene: Node):
        scene_instance = scene
        cached_time = Time.get_unix_time_from_system()
```

#### Cache State Dictionary

```gdscript
# Instance cache structure
instantiate_scene_cache = {
    "res://scenes/level_01.tscn": CachedScene,
    "res://scenes/level_02.tscn": CachedScene
}
instantiate_scene_cache_order = ["level_01", "level_02"]  # LRU order

# Preload resource cache structure
temp_preloaded_resource_cache = {
    "res://scenes/level_03.tscn": PackedScene
}
temp_preloaded_resource_cache_order = ["level_03"]  # FIFO order

fixed_preload_resource_cache = {
    "res://scenes/boss.tscn": PackedScene
}

# Preload state
_preload_resource_states = {
    "res://scenes/level_01.tscn": {
        "state": LoadState.LOADED,
        "resource": PackedScene,
        "fixed": false
    }
}
```

---

## Complete API Reference

### Scene Switching

| Function | Description |
|----------|-------------|
| `switch_scene(new_scene_path, load_method, cache_current_scene, load_screen_path)` | Switch scene |

### Preloading

| Function | Description |
|----------|-------------|
| `preload_scene(scene_path, fixed)` | Preload single scene |
| `preload_scenes(scene_paths, fixed)` | Batch preload |
| `cancel_preloading_scene(scene_path)` | Cancel single preload |
| `cancel_all_preloading()` | Cancel all preloading |

### Cache Management

| Function | Description |
|----------|-------------|
| `clear_all_cache()` | Clear all cache |
| `clear_temp_preload_cache()` | Clear temp preload cache |
| `clear_fixed_cache()` | Clear fixed preload cache |
| `clear_instance_cache()` | Clear instance cache |
| `remove_temp_resource(scene_path)` | Remove temp preload resource |
| `remove_fixed_resource(scene_path)` | Remove fixed preload resource |
| `remove_cached_scene(scene_path)` | Remove cached scene |
| `move_to_fixed(scene_path)` | Move to fixed cache |
| `move_to_temp(scene_path)` | Move to temp cache |
| `set_max_cache_size(new_size)` | Set instance cache capacity |
| `set_max_temp_preload_resource_cache_size(new_size)` | Set temp cache capacity |
| `set_max_fixed_cache_size(new_size)` | Set fixed cache capacity |

### Query

| Function | Description |
|----------|-------------|
| `get_cache_info()` | Get complete cache status |
| `is_scene_cached(scene_path)` | Check if scene is in cache |
| `is_scene_preloading(scene_path)` | Check if scene is preloading |
| `get_preloading_scenes()` | Get list of scenes being preloaded |
| `get_current_scene()` | Get current scene node |
| `get_previous_scene_path()` | Get previous scene path |
| `get_loading_progress(scene_path)` | Get loading progress (0.0~1.0) |
| `is_in_fixed_cache(scene_path)` | Check if in fixed cache |

### Debugging

| Function | Description |
|----------|-------------|
| `print_debug_info()` | Print debug info |
| `connect_all_signals(target)` | Auto-connect all signals |

---

## Loading Screen Customization

### Using Default Loading Screen

```gdscript
# Empty string or "default" uses default black screen
await LongSceneManager.switch_scene("res://scenes/level_02.tscn", LoadMethod.BOTH_PRELOAD_FIRST, true, "")
```

### Creating Custom Loading Screen

A custom loading screen scene should implement the following methods (optional, implement as needed):

```gdscript
extends CanvasLayer

@onready var progress_bar: ProgressBar = $ProgressBar

func fade_in():
    # Fade in animation when showing
    var tween = create_tween()
    tween.tween_property(self, "modulate:a", 1.0, 0.3)

func fade_out():
    # Fade out animation when hiding
    var tween = create_tween()
    tween.tween_property(self, "modulate:a", 0.0, 0.3)

func set_progress(progress: float):
    # Update progress (0.0 ~ 1.0)
    progress_bar.value = progress * 100.0
```

### Using Custom Loading Screen

```gdscript
await LongSceneManager.switch_scene(
    "res://scenes/level_02.tscn",
    LoadMethod.BOTH_PRELOAD_FIRST,
    true,
    "res://ui/my_loading_screen.tscn"  # Custom path
)
```

---

## Screenshots

The plugin comes with a demo project `demo_test_scene_manager/`, which includes:

- `main_scene.tscn` - Main interface, demonstrating basic switching functionality
- `test_scene_1.tscn` - Test scene 1
- `test_scene_2.tscn` - Test scene 2, demonstrating complete cache management functionality

Running the demo project provides a intuitive understanding of the plugin's features.

<img src="./addons/long_scene_manager/image_icon/main1.png" >
<img src="./addons/long_scene_manager/image_icon/scene2.png" >
<img src="./addons/long_scene_manager/image_icon/scene3.png" >

---

## Support the Developer

This plugin is developed and maintained by me in my spare time. If you find it helpful, please consider supporting me in the following ways:

### 🌟 Give the Project a Star

Visit the GitHub repository and click the Star button in the upper right corner. This is the simplest and most powerful way to show your support!

### 📣 Recommend to Other Developers

- Share this plugin in Godot communities, forums, and Discord groups
- Write a usage experience or tutorial
- Mention this plugin in your game development videos/livestreams

### 🏆 Credit in Your Game

Add the following to your game's credits:

```
Scene Manager Plugin - LongSceneManager by LongZhan
https://github.com/AWAUOX/GodotPlugin_LongSceneManager
```

### 🐛 Feedback and Contribution

- Found a bug? Submit an Issue on GitHub
- Have a great idea? Submit a Feature Request
- Want to contribute code? Submit a Pull Request

### 💬 Contact the Author

- GitHub: [@AWAUOX](https://github.com/AWAUOX)

The path of solo development can be lonely, and every Star from you gives me the motivation to keep going! Thank you! 🙏
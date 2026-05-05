# Long Scene Manager Plugin

Long Scene Manager 是一个 Godot 4.x 插件，旨在简化和优化场景切换过程，特别是对于需要长时间加载的复杂场景。它通过提供异步场景加载、三层缓存机制和可定制的加载界面来改善用户体验。

---

<img src="./addons/long_scene_manager/image_icon/icon3.png" width = 30%>

---

> **⚠️ 关于 C# 版本**：C# 版本已暂停更新，停留在 v1.5 版本。主要原因是 Godot 的 C# API 支持目前比较混乱，单人开发难度大。经过实际测试对比，C# 版本在性能和稳定性方面相比 GDScript 版本几乎没有优势，因此决定将精力集中在 GDScript 版本的维护上。如果你需要使用 C# 版本，可以查看仓库中的历史版本，但不再提供后续更新支持。

---

## 目录

- [为什么需要这个插件？](#为什么需要这个插件)
- [支持开发者](#支持开发者)
- [安装方法](#安装方法)
- [插件配置（导出变量）](#插件配置导出变量)
- [核心概念](#核心概念)
  - [五态状态机 (LoadState)](#五态状态机-loadstate)
  - [五种加载策略 (LoadMethod)](#五种加载策略-loadmethod)
  - [三层缓存体系](#三层缓存体系)
- [基础用法](#基础用法)
  - [场景切换](#场景切换)
  - [场景预加载](#场景预加载)
  - [加载屏幕](#加载屏幕)
- [高级用法](#高级用法)
  - [缓存管理](#缓存管理)
  - [查询与监控](#查询与监控)
  - [信号系统](#信号系统)
  - [调试工具](#调试工具)
- [完整 Demo 示例](#完整-demo-示例)
- [完整 API 参考](#完整-api-参考)
- [参考图片](#参考图片)
- [支持开发者](#支持开发者-1)

---

## 为什么需要这个插件？

Godot 引擎内置的 `change_scene_to_file()` 和 `preload()` 在某些情况下存在以下问题：

- **阻塞主线程**：大型场景加载时可能影响游戏流畅度，导致卡顿
- **缺乏缓存管理**：没有内置的 LRU/FIFO 缓存淘汰策略，切换回来的场景需要重新加载
- **无加载界面支持**：切换场景时无法方便地显示自定义加载屏幕和进度条
- **内存浪费**：无法灵活控制哪些场景保留在内存中、哪些应该释放

**本插件提供的解决方案**：

- ✅ 完全异步加载，不阻塞主线程，支持进度回调
- ✅ 三层缓存体系（实例缓存 + 临时预加载缓存 + 固定预加载缓存）
- ✅ 五种加载策略，灵活控制缓存查找优先级
- ✅ 可定制的加载屏幕（支持淡入淡出、进度条等）
- ✅ 分帧实例化，避免大场景实例化时的卡顿
- ✅ 完整的信号系统和调试工具
- ✅ 场景树与缓存严格分离，避免节点管理混乱

---

## 支持开发者

如果您觉得这个插件对您的项目有帮助，请：

1. **给 GitHub 仓库一个 Star**：[https://github.com/AWAUOX/GodotPlugin_LongSceneManager](https://github.com/AWAUOX/GodotPlugin_LongSceneManager)
2. **在游戏的感谢名单中提到本插件和作者 LongZhan**
3. **向其他 Godot 开发者推荐这个插件**

您的支持是我持续改进的动力！

---

## 安装方法

1. 将 `addons/long_scene_manager` 文件夹复制到你的项目的 `addons` 文件夹中
2. 在 Godot 中启用插件：
   - 转到 `项目 → 项目设置 → 插件`
   - 找到 "Long Scene Manager" 并将其状态设置为 "启用"
3. 插件会自动注册为 Autoload 单例，名称为 `LongSceneManager`，你可以在任何脚本中直接通过 `LongSceneManager` 访问

---

## 插件配置（导出变量）

在编辑器中选中 `LongSceneManager` 节点，可以在检查器中配置以下参数：

| 变量名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `max_cache_size` | int (1~20) | 4 | 实例缓存最大容量，超出后按 LRU 淘汰 |
| `max_temp_preload_resource_cache_size` | int (1~50) | 8 | 临时预加载资源缓存最大容量，超出后按 FIFO 淘汰 |
| `max_fixed_preload_resource_cache_size` | int (0~50) | 4 | 固定预加载资源缓存最大容量，超出后按 FIFO 淘汰 |
| `use_async_loading` | bool | true | 是否使用异步加载（推荐开启） |
| `always_use_default_load_screen` | bool | false | 是否强制使用默认加载屏幕（忽略自定义） |
| `instantiate_frames` | int (1~10) | 3 | 场景实例化时延迟的帧数，值越大越平滑但切换越慢 |

也可以在代码中动态修改这些值：

```gdscript
# 运行时修改配置
LongSceneManager.max_cache_size = 6
LongSceneManager.use_async_loading = true
LongSceneManager.instantiate_frames = 2
```

---

## 核心概念

### 五态状态机 (LoadState)

每个场景资源在管理器中经历以下状态流转：

```
NOT_LOADED ──→ LOADING ──→ LOADED ──→ INSTANTIATED
                  │                        ↑
                  └──→ CANCELLED ──────────┘
```

| 状态 | 枚举值 | 含义 |
|------|--------|------|
| `NOT_LOADED` | `LoadState.NOT_LOADED` | 未加载，初始状态 |
| `LOADING` | `LoadState.LOADING` | 正在异步加载中 |
| `LOADED` | `LoadState.LOADED` | 资源已加载为 PackedScene，存入预加载缓存 |
| `INSTANTIATED` | `LoadState.INSTANTIATED` | 已实例化为 Node，存入实例缓存 |
| `CANCELLED` | `LoadState.CANCELLED` | 预加载被取消 |

### 五种加载策略 (LoadMethod)

场景切换时，通过 `load_method` 参数控制缓存查找行为：

| 策略 | 枚举值 | 查找顺序 | 切换行为 | 适用场景 |
|------|--------|---------|---------|---------|
| `DIRECT` | `LoadMethod.DIRECT` | 预加载缓存 → 直接异步加载 | 全新场景 | 不关心实例缓存，只要资源 |
| `PRELOAD_CACHE` | `LoadMethod.PRELOAD_CACHE` | 预加载缓存 → fallback 直接加载 | 全新场景 | 只用预加载的资源 |
| `SCENE_CACHE` | `LoadMethod.SCENE_CACHE` | 实例缓存 → fallback 直接加载 | 复用缓存实例（保留状态） | 优先复用已实例化的场景 |
| `BOTH_PRELOAD_FIRST` | `LoadMethod.BOTH_PRELOAD_FIRST` | 预加载缓存 → 实例缓存 → 正在预加载中 → 直接加载 | 命中实例缓存时复用（保留状态），否则全新 | **默认策略**，推荐大多数情况使用 |
| `BOTH_INSTANCE_FIRST` | `LoadMethod.BOTH_INSTANCE_FIRST` | 实例缓存 → 预加载缓存 → 正在预加载中 → 直接加载 | 命中实例缓存时复用（保留状态），否则全新 | 优先复用场景实例（保留运行时状态） |

> **重要说明**：只有命中**实例缓存**（`SCENE_CACHE`、`BOTH_PRELOAD_FIRST`、`BOTH_INSTANCE_FIRST` 中命中实例缓存时）才会复用已实例化的场景，该场景会**保留切换前的完整运行时状态**（节点位置、变量值、动画进度等）。命中预加载缓存或直接加载时，都会创建**全新**的场景实例。`DIRECT` 和 `PRELOAD_CACHE` 策略永远不会复用实例缓存。

### 三层缓存体系

```
┌─────────────────────────────────────────────────────────┐
│                    LongSceneManager                      │
├─────────────────────────────────────────────────────────┤
│  第一层：实例缓存 (instantiate_scene_cache)               │
│  存储：CachedScene（完整场景 Node + 缓存时间戳）           │
│  淘汰策略：LRU（最近最少使用）                             │
│  默认容量：4                                             │
│  特点：缓存中的场景保留切换前的完整运行时状态，             │
│        从场景树移除后处于暂停状态，重新进入场景树时恢复      │
├─────────────────────────────────────────────────────────┤
│  第二层：临时预加载缓存 (temp_preloaded_resource_cache)    │
│  存储：PackedScene 资源                                  │
│  淘汰策略：FIFO（先进先出）                                │
│  默认容量：8                                             │
│  特点：资源被使用后会从缓存中移除（消耗型）                  │
├─────────────────────────────────────────────────────────┤
│  第三层：固定预加载缓存 (fixed_preload_resource_cache)     │
│  存储：PackedScene 资源                                  │
│  淘汰策略：FIFO（先进先出）                                │
│  默认容量：4                                             │
│  特点：资源被使用后保留在缓存中（持久型），可多次实例化       │
└─────────────────────────────────────────────────────────┘
```

**核心设计原则**：场景实例要么在场景树中（当前活跃），要么在缓存中（非活跃但保留在内存中）。两者严格分离，绝不共存。当场景从场景树移入实例缓存时，场景会**保留切换前的完整运行时状态**（节点位置、变量值、动画进度等），并处于**暂停状态**（`process_mode` 等仍保持原样，但由于不在场景树中，`_process`/`_physics_process` 不会被调用）。当该场景从实例缓存重新进入场景树时，会恢复到离开时的状态继续运行。

---

## 基础用法

### 场景切换

`switch_scene()` 是核心的场景切换函数，支持四种参数组合。

#### 函数签名

```gdscript
func switch_scene(
    new_scene_path: String,                              # 目标场景路径
    load_method: LoadMethod = LoadMethod.BOTH_PRELOAD_FIRST,  # 加载策略
    cache_current_scene: bool = true,                    # 是否缓存当前场景
    load_screen_path: String = ""                        # 加载屏幕路径
) -> void
```

#### 示例 1：最简切换（使用默认策略和默认加载屏幕）

```gdscript
# 使用默认加载策略（BOTH_PRELOAD_FIRST）和默认黑屏过渡
LongSceneManager.switch_scene("res://scenes/level_02.tscn")
```

#### 示例 2：不缓存当前场景

```gdscript
# 切换后当前场景被释放，不进入缓存
LongSceneManager.switch_scene(
    "res://scenes/main_menu.tscn",
    LoadMethod.BOTH_PRELOAD_FIRST,
    false  # 不缓存当前场景
)
```

#### 示例 3：使用 SCENE_CACHE 策略（优先复用实例）

```gdscript
# 如果 level_02 之前被缓存了，直接复用它的实例（保留运行时状态）
await LongSceneManager.switch_scene(
    "res://scenes/level_02.tscn",
    LoadMethod.SCENE_CACHE,
    true
)
print("切换完成！")
```

#### 示例 4：使用 DIRECT 策略（跳过实例缓存）

```gdscript
# 即使实例缓存中有该场景，也重新加载
await LongSceneManager.switch_scene(
    "res://scenes/level_02.tscn",
    LoadMethod.DIRECT,
    false
)
```

#### 示例 5：使用 BOTH_INSTANCE_FIRST 策略

```gdscript
# 优先查找实例缓存，找不到再查预加载缓存
await LongSceneManager.switch_scene(
    "res://scenes/level_03.tscn",
    LoadMethod.BOTH_INSTANCE_FIRST,
    true
)
```

#### 示例 6：无过渡切换

```gdscript
# 不显示任何加载屏幕，场景立即切换
LongSceneManager.switch_scene(
    "res://scenes/level_02.tscn",
    LoadMethod.BOTH_PRELOAD_FIRST,
    true,
    "no_transition"
)
```

---

### 场景预加载

预加载允许你在后台提前加载场景资源，等需要切换时几乎瞬间完成。

#### preload_scene() — 预加载单个场景

```gdscript
func preload_scene(scene_path: String, fixed: bool = false) -> void
```

```gdscript
# 临时预加载：资源可能被自动淘汰
LongSceneManager.preload_scene("res://scenes/level_02.tscn")

# 固定预加载：资源不会被自动淘汰，可多次实例化
LongSceneManager.preload_scene("res://scenes/boss_fight.tscn", true)
```

#### preload_scenes() — 批量预加载

```gdscript
func preload_scenes(scene_paths: Array[String], fixed: bool = false) -> void
```

```gdscript
# 批量预加载多个关卡
LongSceneManager.preload_scenes([
    "res://scenes/level_02.tscn",
    "res://scenes/level_03.tscn",
    "res://scenes/level_04.tscn"
])

# 批量固定预加载
LongSceneManager.preload_scenes([
    "res://scenes/shop.tscn",
    "res://scenes/inventory.tscn"
], true)
```

#### cancel_preloading_scene() — 取消单个预加载

```gdscript
func cancel_preloading_scene(scene_path: String) -> void
```

```gdscript
# 玩家改变了主意，不需要去 level_03 了
LongSceneManager.cancel_preloading_scene("res://scenes/level_03.tscn")
```

#### cancel_all_preloading() — 取消所有预加载

```gdscript
func cancel_all_preloading() -> void
```

```gdscript
# 场景切换时，取消所有正在进行的预加载
LongSceneManager.cancel_all_preloading()
```

#### 预加载 + 切换的完整流程

```gdscript
# 1. 进入关卡后，预加载下一个关卡
func _on_level_01_ready():
    LongSceneManager.preload_scene("res://scenes/level_02.tscn")
    print("后台开始预加载 level_02...")

# 2. 玩家到达出口，切换场景（此时预加载已完成，切换瞬间完成）
func _on_exit_reached():
    print("正在切换到 level_02...")
    await LongSceneManager.switch_scene("res://scenes/level_02.tscn")
    print("切换完成！")
```

---

### 加载屏幕

加载屏幕在场景切换时显示，提供视觉过渡效果。

#### 加载屏幕的三种模式

| load_screen_path 参数 | 行为 |
|----------------------|------|
| `""` 或 `"default"` | 使用默认黑屏（淡入淡出效果） |
| `"no_transition"` | 无过渡，直接切换 |
| 自定义路径（如 `"res://ui/my_loading.tscn"`） | 使用自定义加载屏幕场景 |

#### 使用默认加载屏幕

```gdscript
# 空字符串 = 使用默认黑屏
await LongSceneManager.switch_scene(
    "res://scenes/level_02.tscn",
    LoadMethod.BOTH_PRELOAD_FIRST,
    true,
    ""  # 默认加载屏幕
)
```

#### 使用自定义加载屏幕

```gdscript
# 指定自定义加载屏幕的路径
await LongSceneManager.switch_scene(
    "res://scenes/level_02.tscn",
    LoadMethod.BOTH_PRELOAD_FIRST,
    true,
    "res://ui/my_fancy_loading_screen.tscn"
)
```

#### 创建自定义加载屏幕

自定义加载屏幕场景需要实现以下方法（均为可选，按需实现）：

```gdscript
# my_fancy_loading_screen.gd
extends CanvasLayer

@onready var progress_bar: ProgressBar = $ProgressBar
@onready var tip_label: Label = $TipLabel

func fade_in():
    # 显示加载屏幕时的动画（淡入）
    modulate.a = 0.0
    var tween = create_tween()
    tween.tween_property(self, "modulate:a", 1.0, 0.3)
    await tween.finished

func fade_out():
    # 隐藏加载屏幕时的动画（淡出）
    var tween = create_tween()
    tween.tween_property(self, "modulate:a", 0.0, 0.3)
    await tween.finished

func set_progress(progress: float):
    # 更新加载进度（progress 范围 0.0 ~ 1.0）
    progress_bar.value = progress * 100.0

func update_progress(progress: float):
    # 备选进度更新方法名
    progress_bar.value = progress * 100.0

func show_loading():
    # 备选显示方法名
    visible = true

func hide_loading():
    # 备选隐藏方法名
    visible = false
```

**方法调用优先级**：
- 显示时：优先调用 `fade_in()`，其次 `show_loading()`，最后直接设置 `visible = true`
- 隐藏时：优先调用 `fade_out()`，其次 `hide_loading()`，最后直接设置 `visible = false`
- 进度更新：优先调用 `set_progress()`，其次 `update_progress()`

#### 强制使用默认加载屏幕

```gdscript
# 在编辑器中勾选 always_use_default_load_screen，或代码设置：
LongSceneManager.always_use_default_load_screen = true

# 此后无论传入什么 load_screen_path，都会使用默认黑屏
await LongSceneManager.switch_scene(
    "res://scenes/level_02.tscn",
    LoadMethod.BOTH_PRELOAD_FIRST,
    true,
    "res://ui/my_loading.tscn"  # 这个参数会被忽略
)
```

---

## 高级用法

### 缓存管理

#### clear_all_cache() — 清空所有缓存

```gdscript
func clear_all_cache() -> void
```

清空实例缓存、临时预加载缓存、固定预加载缓存和所有预加载状态。缓存中的场景实例会被 `queue_free()` 释放。

```gdscript
# 返回主菜单时，清空所有关卡缓存释放内存
func _return_to_main_menu():
    LongSceneManager.clear_all_cache()
    await LongSceneManager.switch_scene("res://scenes/main_menu.tscn", LoadMethod.DIRECT, false)
```

#### clear_temp_preload_cache() — 清空临时预加载缓存

```gdscript
func clear_temp_preload_cache() -> void
```

只清空临时预加载资源缓存，不影响固定缓存和实例缓存。

```gdscript
# 清理不再需要的临时预加载资源
LongSceneManager.clear_temp_preload_cache()
```

#### clear_fixed_cache() — 清空固定预加载缓存

```gdscript
func clear_fixed_cache() -> void
```

只清空固定预加载资源缓存。

```gdscript
# 离开商店区域后，清理商店相关的固定缓存
LongSceneManager.clear_fixed_cache()
```

#### clear_instance_cache() — 清空实例缓存

```gdscript
func clear_instance_cache() -> void
```

只清空实例缓存，缓存中的场景节点会被释放。

```gdscript
# 清理所有缓存的场景实例
LongSceneManager.clear_instance_cache()
```

#### remove_temp_resource() — 移除单个临时预加载资源

```gdscript
func remove_temp_resource(scene_path: String) -> void
```

```gdscript
# 玩家不会去 level_04 了，移除它的预加载资源
LongSceneManager.remove_temp_resource("res://scenes/level_04.tscn")
```

#### remove_fixed_resource() — 移除单个固定预加载资源

```gdscript
func remove_fixed_resource(scene_path: String) -> void
```

```gdscript
# 离开商店区域，不再需要商店场景的固定缓存
LongSceneManager.remove_fixed_resource("res://scenes/shop.tscn")
```

#### remove_cached_scene() — 移除单个缓存场景实例

```gdscript
func remove_cached_scene(scene_path: String) -> void
```

```gdscript
# 手动移除某个缓存的场景实例
LongSceneManager.remove_cached_scene("res://scenes/level_02.tscn")
```

#### move_to_fixed() — 将临时预加载资源转为固定

```gdscript
func move_to_fixed(scene_path: String) -> void
```

将资源从临时预加载缓存移动到固定预加载缓存。固定缓存中的资源不会被自动淘汰，且使用后保留。

```gdscript
# 预加载了 boss 场景，发现玩家确实要打 boss，转为固定缓存
LongSceneManager.preload_scene("res://scenes/boss_fight.tscn")
# ... 确认玩家进入 boss 区域 ...
LongSceneManager.move_to_fixed("res://scenes/boss_fight.tscn")
```

#### move_to_temp() — 将固定预加载资源转为临时

```gdscript
func move_to_temp(scene_path: String) -> void
```

将资源从固定预加载缓存移动到临时预加载缓存。临时缓存中的资源可能被自动淘汰。

```gdscript
# Boss 战结束，不再需要固定保留该场景
LongSceneManager.move_to_temp("res://scenes/boss_fight.tscn")
```

#### set_max_cache_size() — 动态设置实例缓存容量

```gdscript
func set_max_cache_size(new_size: int) -> void
```

```gdscript
# 在开放区域增加缓存容量，让更多场景保留在内存中
LongSceneManager.set_max_cache_size(10)

# 进入内存紧张的区域，减少缓存容量
LongSceneManager.set_max_cache_size(3)
```

#### set_max_temp_preload_resource_cache_size() — 动态设置临时预加载缓存容量

```gdscript
func set_max_temp_preload_resource_cache_size(new_size: int) -> void
```

```gdscript
LongSceneManager.set_max_temp_preload_resource_cache_size(20)
```

#### set_max_fixed_cache_size() — 动态设置固定预加载缓存容量

```gdscript
func set_max_fixed_cache_size(new_size: int) -> void
```

```gdscript
LongSceneManager.set_max_fixed_cache_size(6)
```

---

### 查询与监控

#### get_cache_info() — 获取完整缓存状态

```gdscript
func get_cache_info() -> Dictionary
```

返回一个包含所有缓存详细信息的字典，非常适合做调试面板。

```gdscript
var info = LongSceneManager.get_cache_info()

print("当前场景: ", info["current_scene"])
print("上一个场景: ", info["previous_scene"])

# 实例缓存信息
var instance = info["instance_cache"]
print("实例缓存: ", instance["size"], "/", instance["max_size"])
for scene in instance["scenes"]:
    print("  - ", scene["path"], " (缓存时间: ", scene["cached_time"], ")")

# 临时预加载缓存信息
var temp = info["temp_preload_cache"]
print("临时预加载缓存: ", temp["size"], "/", temp["max_size"])
for path in temp["scenes"]:
    print("  - ", path)

# 固定预加载缓存信息
var fixed = info["fixed_preload_cache"]
print("固定预加载缓存: ", fixed["size"], "/", fixed["max_size"])
for path in fixed["scenes"]:
    print("  - ", path)

# 预加载状态信息
var states = info["preload_states"]
print("预加载状态追踪: ", states["size"], " 个场景")
for s in states["states"]:
    print("  - ", s["path"], " 状态: ", s["state"], " 固定: ", s["fixed"])
```

#### is_scene_cached() — 检查场景是否在任意缓存中

```gdscript
func is_scene_cached(scene_path: String) -> bool
```

```gdscript
if LongSceneManager.is_scene_cached("res://scenes/level_02.tscn"):
    print("level_02 在缓存中，切换会很快！")
else:
    print("level_02 不在缓存中，需要加载")
```

#### is_scene_preloading() — 检查场景是否正在预加载

```gdscript
func is_scene_preloading(scene_path: String) -> bool
```

```gdscript
if LongSceneManager.is_scene_preloading("res://scenes/level_03.tscn"):
    print("level_03 正在后台加载中...")
```

#### get_preloading_scenes() — 获取所有正在预加载的场景列表

```gdscript
func get_preloading_scenes() -> Array
```

```gdscript
var loading_list = LongSceneManager.get_preloading_scenes()
if loading_list.size() > 0:
    print("当前有 ", loading_list.size(), " 个场景正在预加载：")
    for path in loading_list:
        print("  - ", path)
```

#### get_current_scene() — 获取当前活跃场景节点

```gdscript
func get_current_scene() -> Node
```

```gdscript
var scene = LongSceneManager.get_current_scene()
if scene and scene.has_method("get_level_name"):
    print("当前关卡: ", scene.get_level_name())
```

#### get_previous_scene_path() — 获取上一个场景的路径

```gdscript
func get_previous_scene_path() -> String
```

```gdscript
var prev = LongSceneManager.get_previous_scene_path()
print("上一个场景是: ", prev)
# 可以用来实现"返回上一场景"功能
```

#### get_loading_progress() — 获取场景加载进度

```gdscript
func get_loading_progress(scene_path: String) -> float
```

返回值范围 `0.0` ~ `1.0`。如果场景已在缓存中则直接返回 `1.0`。

```gdscript
# 在 _process 中轮询加载进度
func _process(_delta):
    var progress = LongSceneManager.get_loading_progress("res://scenes/level_02.tscn")
    $ProgressBar.value = progress * 100
    if progress >= 1.0:
        print("加载完成！")
        set_process(false)
```

#### get_resource_file_size() — 获取场景文件大小（字节）

```gdscript
func get_resource_file_size(scene_path: String) -> int
```

```gdscript
var size_bytes = LongSceneManager.get_resource_file_size("res://scenes/level_02.tscn")
print("场景文件大小: ", size_bytes, " 字节")
```

#### get_resource_file_size_formatted() — 获取格式化的文件大小

```gdscript
func get_resource_file_size_formatted(scene_path: String) -> String
```

```gdscript
var size_str = LongSceneManager.get_resource_file_size_formatted("res://scenes/level_02.tscn")
print("场景文件大小: ", size_str)  # 输出如 "2.5 MB"
```

#### get_resource_info() — 获取场景综合信息

```gdscript
func get_resource_info(scene_path: String) -> Dictionary
```

返回一个包含场景所有相关信息的字典。

```gdscript
var info = LongSceneManager.get_resource_info("res://scenes/level_02.tscn")

print("路径: ", info["path"])
print("文件存在: ", info["exists"])
print("文件大小: ", info["file_size_formatted"])
print("在临时缓存中: ", info["in_temp_cache"])
print("在固定缓存中: ", info["in_fixed_cache"])
print("在实例缓存中: ", info["in_instance_cache"])
print("正在预加载: ", info["is_preloading"])
print("加载进度: ", info["loading_progress"] * 100, "%")
```

#### is_in_fixed_cache() — 检查场景是否在固定缓存中

```gdscript
func is_in_fixed_cache(scene_path: String) -> bool
```

```gdscript
if LongSceneManager.is_in_fixed_cache("res://scenes/boss_fight.tscn"):
    print("Boss 场景在固定缓存中，不会被自动清理")
```

---

### 信号系统

LongSceneManager 提供了 10 个信号，覆盖场景管理的完整生命周期。

#### 信号列表

| 信号 | 参数 | 触发时机 |
|------|------|---------|
| `scene_preload_started` | `scene_path: String` | 预加载开始 |
| `scene_preload_completed` | `scene_path: String` | 预加载完成 |
| `scene_preload_cancelled` | `scene_path: String` | 预加载被取消 |
| `scene_preload_failed` | `scene_path: String` | 预加载失败 |
| `scene_switch_started` | `from_scene: String, to_scene: String` | 场景切换开始 |
| `scene_switch_completed` | `scene_path: String` | 场景切换完成 |
| `scene_switch_failed` | `scene_path: String` | 场景切换失败 |
| `scene_cached` | `scene_path: String` | 场景被加入缓存 |
| `scene_removed_from_cache` | `scene_path: String` | 场景从缓存中移除 |
| `load_screen_shown` | `load_screen_instance: Node` | 加载屏幕显示完成 |
| `load_screen_hidden` | `load_screen_instance: Node` | 加载屏幕隐藏完成 |

#### 手动连接信号

```gdscript
# 在 _ready 中连接信号
func _ready():
    LongSceneManager.scene_switch_started.connect(_on_switch_started)
    LongSceneManager.scene_switch_completed.connect(_on_switch_completed)
    LongSceneManager.scene_preload_completed.connect(_on_preload_done)

func _on_switch_started(from_scene: String, to_scene: String):
    print("正在从 ", from_scene, " 切换到 ", to_scene)
    # 暂停游戏逻辑、保存数据等

func _on_switch_completed(scene_path: String):
    print("已切换到: ", scene_path)
    # 恢复游戏逻辑、初始化新场景等

func _on_preload_done(scene_path: String):
    print("预加载完成: ", scene_path)
```

#### 使用 connect_all_signals() 批量连接

```gdscript
func connect_all_signals(target: Object) -> void
```

自动将管理器的所有信号连接到目标对象上。连接规则：信号 `scene_switch_completed` 会连接到目标对象的 `_on_scene_manager_scene_switch_completed` 方法。

```gdscript
# game_manager.gd
extends Node

func _ready():
    # 一行代码连接所有信号
    LongSceneManager.connect_all_signals(self)

func _on_scene_manager_scene_switch_started(from_scene: String, to_scene: String):
    print("场景切换: ", from_scene, " → ", to_scene)

func _on_scene_manager_scene_switch_completed(scene_path: String):
    print("场景切换完成: ", scene_path)

func _on_scene_manager_scene_preload_completed(scene_path: String):
    print("预加载完成: ", scene_path)

func _on_scene_manager_scene_preload_failed(scene_path: String):
    push_error("预加载失败: ", scene_path)

func _on_scene_manager_scene_switch_failed(scene_path: String):
    push_error("场景切换失败: ", scene_path)

func _on_scene_manager_scene_cached(scene_path: String):
    print("场景已缓存: ", scene_path)

func _on_scene_manager_scene_removed_from_cache(scene_path: String):
    print("场景已从缓存移除: ", scene_path)

func _on_scene_manager_load_screen_shown(load_screen_instance: Node):
    print("加载屏幕已显示")

func _on_scene_manager_load_screen_hidden(load_screen_instance: Node):
    print("加载屏幕已隐藏")

func _on_scene_manager_scene_preload_started(scene_path: String):
    print("开始预加载: ", scene_path)

func _on_scene_manager_scene_preload_cancelled(scene_path: String):
    print("预加载已取消: ", scene_path)
```

---

### 调试工具

#### print_debug_info() — 打印完整调试信息

```gdscript
func print_debug_info() -> void
```

打印所有缓存状态、当前场景、配置参数等到控制台。

```gdscript
# 在某个调试快捷键上绑定
func _input(event):
    if event.is_action_pressed("debug_print_cache"):
        LongSceneManager.print_debug_info()
```

输出示例：

```
=== SceneManager Debug Info ===
Current scene: res://scenes/level_01.tscn
Previous scene: res://scenes/main_menu.tscn

[Instance Cache] Count: 2/4
  Access order: ["res://scenes/main_menu.tscn", "res://scenes/level_02.tscn"]
  Scenes: ["res://scenes/main_menu.tscn", "res://scenes/level_02.tscn"]

[Temp Preload Cache] Count: 1/8
  Access order: ["res://scenes/level_03.tscn"]
  Scenes: ["res://scenes/level_03.tscn"]

[Fixed Preload Cache] Count: 1/4
  Access order: ["res://scenes/shop.tscn"]
  Scenes: ["res://scenes/shop.tscn"]

[Preload States] Count: 2
  res://scenes/level_03.tscn -> 2 | fixed: false | has_resource: true
  res://scenes/shop.tscn -> 2 | fixed: true | has_resource: true

Default loading screen: Loaded
Active loading screen: No
Using asynchronous loading: true
Always use default loading screen: false
===============================
```

---

## 完整 Demo 示例

### Demo 1：简单的多关卡游戏

```gdscript
# game_manager.gd - 挂载到 Autoload 或主场景
extends Node

var levels = [
    "res://scenes/level_01.tscn",
    "res://scenes/level_02.tscn",
    "res://scenes/level_03.tscn",
    "res://scenes/level_04.tscn",
    "res://scenes/level_05.tscn"
]

var current_level_index: int = 0

func _ready():
    # 连接信号
    LongSceneManager.connect_all_signals(self)
    # 预加载前两个关卡
    _preload_upcoming_levels()

func _preload_upcoming_levels():
    # 预加载接下来的两个关卡
    for i in range(1, 3):
        var idx = current_level_index + i
        if idx < levels.size():
            LongSceneManager.preload_scene(levels[idx])

func go_to_next_level():
    current_level_index += 1
    if current_level_index >= levels.size():
        _return_to_main_menu()
        return

    # 切换到下一关
    await LongSceneManager.switch_scene(
        levels[current_level_index],
        LoadMethod.BOTH_PRELOAD_FIRST,
        true  # 缓存当前关卡，方便玩家返回
    )
    # 继续预加载后续关卡
    _preload_upcoming_levels()

func go_to_previous_level():
    if current_level_index > 0:
        current_level_index -= 1
        await LongSceneManager.switch_scene(
            levels[current_level_index],
            LoadMethod.BOTH_INSTANCE_FIRST,  # 优先复用缓存的实例
            true
        )

func _return_to_main_menu():
    LongSceneManager.clear_all_cache()
    await LongSceneManager.switch_scene(
        "res://scenes/main_menu.tscn",
        LoadMethod.DIRECT,
        false
    )

# 信号回调
func _on_scene_manager_scene_switch_completed(scene_path: String):
    print("成功进入: ", scene_path)

func _on_scene_manager_scene_switch_failed(scene_path: String):
    push_error("切换失败: ", scene_path)
    # 可以显示错误提示给玩家
```

### Demo 2：带 Boss 战的关卡管理

```gdscript
# boss_level_manager.gd
extends Node

const BOSS_SCENE = "res://scenes/boss_fight.tscn"
const VICTORY_SCENE = "res://scenes/victory.tscn"
const GAME_OVER_SCENE = "res://scenes/game_over.tscn"

func _ready():
    LongSceneManager.connect_all_signals(self)

func prepare_boss_fight():
    print("准备 Boss 战...")

    # 固定预加载 Boss 场景（不会被自动清理）
    LongSceneManager.preload_scene(BOSS_SCENE, true)

    # 预加载胜利和失败场景
    LongSceneManager.preload_scenes([VICTORY_SCENE, GAME_OVER_SCENE])

    # 检查加载进度
    _check_boss_preload_progress()

func _check_boss_preload_progress():
    while LongSceneManager.get_loading_progress(BOSS_SCENE) < 1.0:
        var progress = LongSceneManager.get_loading_progress(BOSS_SCENE)
        print("Boss 场景加载中: ", progress * 100, "%")
        await get_tree().create_timer(0.5).timeout
    print("Boss 场景准备就绪！")

func start_boss_fight():
    await LongSceneManager.switch_scene(
        BOSS_SCENE,
        LoadMethod.BOTH_PRELOAD_FIRST,
        false  # 不缓存当前场景，Boss 战需要全新状态
    )

func on_boss_defeated():
    # Boss 战结束，不再需要固定保留
    LongSceneManager.move_to_temp(BOSS_SCENE)
    # 切换到胜利场景
    await LongSceneManager.switch_scene(VICTORY_SCENE)

func on_player_died():
    await LongSceneManager.switch_scene(GAME_OVER_SCENE)

func cleanup_boss_resources():
    LongSceneManager.remove_fixed_resource(BOSS_SCENE)
    LongSceneManager.remove_temp_resource(VICTORY_SCENE)
    LongSceneManager.remove_temp_resource(GAME_OVER_SCENE)
```

### Demo 3：带自定义加载屏幕和进度条

```gdscript
# loading_screen_manager.gd
extends Node

const CUSTOM_LOADING_SCREEN = "res://ui/fancy_loading_screen.tscn"

func switch_with_fancy_loading(target_scene: String):
    # 先预加载目标场景
    LongSceneManager.preload_scene(target_scene)

    # 使用自定义加载屏幕切换
    await LongSceneManager.switch_scene(
        target_scene,
        LoadMethod.BOTH_PRELOAD_FIRST,
        true,
        CUSTOM_LOADING_SCREEN
    )

func switch_with_progress_tracking(target_scene: String):
    # 不使用预加载，直接切换并追踪进度
    LongSceneManager.switch_scene(
        target_scene,
        LoadMethod.DIRECT,
        false,
        CUSTOM_LOADING_SCREEN
    )

    # 在另一个地方轮询进度
    _track_loading_progress(target_scene)

func _track_loading_progress(scene_path: String):
    while LongSceneManager.get_loading_progress(scene_path) < 1.0:
        var progress = LongSceneManager.get_loading_progress(scene_path)
        var size = LongSceneManager.get_resource_file_size_formatted(scene_path)
        print("加载中... ", progress * 100, "% (文件大小: ", size, ")")
        await get_tree().process_frame
    print("加载完成！")
```

### Demo 4：缓存监控调试面板

```gdscript
# debug_panel.gd - 挂载到调试 UI
extends Control

@onready var info_label: RichTextLabel = $InfoLabel

func _ready():
    # 每 2 秒刷新一次缓存信息
    var timer = get_tree().create_timer(2.0)
    timer.timeout.connect(_refresh_info)
    _refresh_info()

func _refresh_info():
    var info = LongSceneManager.get_cache_info()
    var text = "[b]=== 场景管理器状态 ===[/b]\n\n"

    text += "[b]当前场景:[/b] " + str(info["current_scene"]) + "\n"
    text += "[b]上一个场景:[/b] " + str(info["previous_scene"]) + "\n\n"

    text += "[b]实例缓存:[/b] " + str(info["instance_cache"]["size"]) + "/" + str(info["instance_cache"]["max_size"]) + "\n"
    for scene in info["instance_cache"]["scenes"]:
        var valid = "✓" if scene["instance_valid"] else "✗"
        text += "  " + valid + " " + scene["path"].get_file() + "\n"

    text += "\n[b]临时预加载缓存:[/b] " + str(info["temp_preload_cache"]["size"]) + "/" + str(info["temp_preload_cache"]["max_size"]) + "\n"
    for path in info["temp_preload_cache"]["scenes"]:
        text += "  - " + path.get_file() + "\n"

    text += "\n[b]固定预加载缓存:[/b] " + str(info["fixed_preload_cache"]["size"]) + "/" + str(info["fixed_preload_cache"]["max_size"]) + "\n"
    for path in info["fixed_preload_cache"]["scenes"]:
        text += "  - " + path.get_file() + "\n"

    text += "\n[b]预加载状态:[/b] " + str(info["preload_states"]["size"]) + " 个\n"
    for s in info["preload_states"]["states"]:
        var state_name = ["未加载", "加载中", "已加载", "已实例化", "已取消"][s["state"]]
        text += "  - " + s["path"].get_file() + ": " + state_name + "\n"

    info_label.text = text

    # 继续定时刷新
    var timer = get_tree().create_timer(2.0)
    timer.timeout.connect(_refresh_info)
```

---

## 完整 API 参考

### 场景切换

#### switch_scene()

```gdscript
func switch_scene(
    new_scene_path: String,
    load_method: LoadMethod = LoadMethod.BOTH_PRELOAD_FIRST,
    cache_current_scene: bool = true,
    load_screen_path: String = ""
) -> void
```

切换到指定场景。

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `new_scene_path` | String | (必填) | 目标场景的文件路径 |
| `load_method` | LoadMethod | `BOTH_PRELOAD_FIRST` | 加载策略，决定如何查找和使用缓存 |
| `cache_current_scene` | bool | `true` | 是否将当前场景放入实例缓存 |
| `load_screen_path` | String | `""` | 加载屏幕路径，`""`=默认，`"no_transition"`=无过渡 |

---

### 场景预加载

#### preload_scene()

```gdscript
func preload_scene(scene_path: String, fixed: bool = false) -> void
```

后台异步预加载场景资源。

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `scene_path` | String | (必填) | 场景文件路径 |
| `fixed` | bool | `false` | 是否存入固定缓存（持久保留） |

#### preload_scenes()

```gdscript
func preload_scenes(scene_paths: Array[String], fixed: bool = false) -> void
```

批量预加载多个场景。

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `scene_paths` | Array[String] | (必填) | 场景路径数组 |
| `fixed` | bool | `false` | 是否存入固定缓存 |

#### cancel_preloading_scene()

```gdscript
func cancel_preloading_scene(scene_path: String) -> void
```

取消指定场景的预加载（仅在 LOADING 状态有效）。

#### cancel_all_preloading()

```gdscript
func cancel_all_preloading() -> void
```

取消所有正在进行的预加载。

---

### 缓存管理

#### clear_all_cache()

```gdscript
func clear_all_cache() -> void
```

清空所有缓存（实例缓存 + 临时预加载缓存 + 固定预加载缓存 + 预加载状态）。

#### clear_temp_preload_cache()

```gdscript
func clear_temp_preload_cache() -> void
```

只清空临时预加载资源缓存。

#### clear_fixed_cache()

```gdscript
func clear_fixed_cache() -> void
```

只清空固定预加载资源缓存。

#### clear_instance_cache()

```gdscript
func clear_instance_cache() -> void
```

只清空实例缓存，缓存中的场景节点会被释放。

#### remove_temp_resource()

```gdscript
func remove_temp_resource(scene_path: String) -> void
```

从临时预加载缓存中移除指定场景的资源。

#### remove_fixed_resource()

```gdscript
func remove_fixed_resource(scene_path: String) -> void
```

从固定预加载缓存中移除指定场景的资源。

#### remove_cached_scene()

```gdscript
func remove_cached_scene(scene_path: String) -> void
```

从实例缓存中移除指定场景并释放其节点。

#### move_to_fixed()

```gdscript
func move_to_fixed(scene_path: String) -> void
```

将场景资源从临时预加载缓存移动到固定预加载缓存。

#### move_to_temp()

```gdscript
func move_to_temp(scene_path: String) -> void
```

将场景资源从固定预加载缓存移动到临时预加载缓存。

#### set_max_cache_size()

```gdscript
func set_max_cache_size(new_size: int) -> void
```

动态设置实例缓存的最大容量（范围 1~∞）。如果当前缓存超出新容量，会立即淘汰最旧的。

#### set_max_temp_preload_resource_cache_size()

```gdscript
func set_max_temp_preload_resource_cache_size(new_size: int) -> void
```

动态设置临时预加载资源缓存的最大容量（范围 1~∞）。

#### set_max_fixed_cache_size()

```gdscript
func set_max_fixed_cache_size(new_size: int) -> void
```

动态设置固定预加载资源缓存的最大容量（范围 0~∞）。设为 0 表示禁用固定缓存。

---

### 查询函数

#### get_cache_info()

```gdscript
func get_cache_info() -> Dictionary
```

返回完整缓存状态字典。结构见 [查询与监控](#查询与监控) 章节。

#### is_scene_cached()

```gdscript
func is_scene_cached(scene_path: String) -> bool
```

检查场景是否在任意缓存中（实例缓存、临时预加载缓存或固定预加载缓存）。

#### is_scene_preloading()

```gdscript
func is_scene_preloading(scene_path: String) -> bool
```

检查场景是否正在预加载中。

#### get_preloading_scenes()

```gdscript
func get_preloading_scenes() -> Array
```

返回所有正在预加载的场景路径数组。

#### get_current_scene()

```gdscript
func get_current_scene() -> Node
```

返回当前活跃的场景节点。

#### get_previous_scene_path()

```gdscript
func get_previous_scene_path() -> String
```

返回上一个场景的文件路径。

#### get_loading_progress()

```gdscript
func get_loading_progress(scene_path: String) -> float
```

获取场景的加载进度（0.0 ~ 1.0）。已在缓存中则直接返回 1.0。

#### get_resource_file_size()

```gdscript
func get_resource_file_size(scene_path: String) -> int
```

获取场景文件的原始字节大小。文件不存在时返回 -1。

#### get_resource_file_size_formatted()

```gdscript
func get_resource_file_size_formatted(scene_path: String) -> String
```

获取格式化后的场景文件大小（如 "2.5 MB"）。文件不存在时返回 "N/A"。

#### get_resource_info()

```gdscript
func get_resource_info(scene_path: String) -> Dictionary
```

获取场景的综合信息字典，包含文件大小、缓存位置、加载进度等。

#### is_in_fixed_cache()

```gdscript
func is_in_fixed_cache(scene_path: String) -> bool
```

检查场景是否在固定预加载缓存中。

---

### 调试

#### print_debug_info()

```gdscript
func print_debug_info() -> void
```

向控制台打印完整的调试信息，包括所有缓存状态和配置参数。

---

### 信号辅助

#### connect_all_signals()

```gdscript
func connect_all_signals(target: Object) -> void
```

将管理器的所有信号自动连接到目标对象。信号 `xxx` 会连接到目标对象的 `_on_scene_manager_xxx` 方法。

---

## 参考图片

<img src="./addons/long_scene_manager/image_icon/main1.png">

<img src="./addons/long_scene_manager/image_icon/scene22.png">

<img src="./addons/long_scene_manager/image_icon/scene3.png">

---

## 支持开发者

这个插件是我一个人用业余时间开发和维护的。如果你觉得它对你有帮助，请考虑以下方式支持我：

### 🌟 给项目一个 Star
访问 [GitHub 仓库](https://github.com/AWAUOX/GodotPlugin_LongSceneManager)，点击右上角的 Star 按钮。这是最简单也最有力的支持方式！

### 📣 向其他开发者推荐
- 在 Godot 社区、论坛、Discord 群组中分享这个插件
- 写一篇使用心得或教程
- 在你的游戏开发视频/直播中提到这个插件

### 🏆 在游戏中致谢
在你的游戏感谢名单（Credits）中加入：
> 场景管理器插件 - LongSceneManager by LongZhan
> https://github.com/AWAUOX/GodotPlugin_LongSceneManager

### 🐛 反馈与贡献
- 发现 Bug？在 GitHub 上提交 Issue
- 有好的想法？提交 Feature Request
- 想贡献代码？提交 Pull Request

### 💬 联系作者
- GitHub: [@AWAUOX](https://github.com/AWAUOX)

---

**一个人的开发之路很孤独，你的每一个 Star 都是我继续前进的动力！谢谢！** 🙏

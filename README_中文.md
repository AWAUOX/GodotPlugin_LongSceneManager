# Long Scene Manager Plugin

**<font size="3">[ENGLISH Document](README.md)</font>**<br>
Long Scene Manager 是一个功能强大、简单易用的 Godot 4.x 场景管理插件。它专为解决各类游戏项目中复杂场景切换、多场景管理等的种种痛点而设计，插件提供了自带的异步加载技术、智能三级缓存系统和可自定义加载的过渡动画，让你的游戏场景切换高效、流畅、可控制、可预测。

无论你是开发独立游戏的个人开发者，还是拥有完整团队的商业项目，Long Scene Manager 都能帮助你：

- 🚀 **消除加载卡顿** - 完全异步的加载机制，让大型场景的加载过程变得悄无声息
- 💾 **智能内存管理** - LRU/FIFO 混合缓存策略，确保内存用在刀刃上
- ✨ **极致的用户体验** - 自定义加载屏幕、淡入淡出效果、进度条显示，让等待变成享受
- 🎯 **灵活的场景复用** - 保留场景状态、随时切换，让游戏流程更加流畅
- 🔧 **零学习成本** - 简洁的 API 设计，几行代码即可接入

> 🌟 **特别提示**：插件同时提供 GDScript 和 C# 两个版本，功能完全一致。无论你使用哪种语言，都能享受到同等的功能和性能！

---

<img src="./addons/long_scene_manager/image_icon/icon3.png" width = 30%>

---

## 支持开发者

如果您觉得这个插件对您的项目有帮助，请：

- ⭐ **给 GitHub 仓库一个 Star**：[https://github.com/AWAUOX/GodotPlugin_LongSceneManager](https://github.com/AWAUOX/GodotPlugin_LongSceneManager)
- 🎮 **在游戏的感谢名单中提到本插件和作者 LongZhan**
- 📢 **向其他 Godot 开发者推荐这个插件**

您的支持是我持续改进的动力！

---

## 目录

- [Long Scene Manager Plugin](#long-scene-manager-plugin)
  - [支持开发者](#支持开发者)
  - [目录](#目录)
  - [插件概述](#插件概述)
    - [插件核心特性](#插件核心特性)
  - [基本使用](#基本使用)
    - [安装与启用](#安装与启用)
    - [插件配置（导出变量）](#插件配置导出变量)
    - [GDScript 使用](#gdscript-使用)
    - [C# 使用](#c-使用)
    - [场景切换基础](#场景切换基础)
    - [场景预加载](#场景预加载)
    - [预加载 + 切换的完整流程](#预加载--切换的完整流程)
  - [进阶使用](#进阶使用)
    - [五种加载策略详解](#五种加载策略详解)
    - [三层缓存体系](#三层缓存体系)
    - [缓存管理API](#缓存管理api)
    - [信号系统](#信号系统)
  - [插件开发指南](#插件开发指南)
    - [核心设计原则](#核心设计原则)
    - [代码结构解析](#代码结构解析)
    - [运行逻辑流程](#运行逻辑流程)
      - [场景切换流程](#场景切换流程)
      - [预加载流程](#预加载流程)
    - [关键数据结构](#关键数据结构)
      - [LoadState 枚举](#loadstate-枚举)
      - [CachedScene 内部类](#cachedscene-内部类)
      - [缓存状态字典](#缓存状态字典)
  - [完整API参考](#完整api参考)
    - [场景切换](#场景切换)
    - [预加载](#预加载)
    - [缓存管理](#缓存管理)
    - [查询](#查询)
    - [调试](#调试)
  - [加载屏幕自定义](#加载屏幕自定义)
    - [使用默认加载屏幕](#使用默认加载屏幕)
    - [创建自定义加载屏幕](#创建自定义加载屏幕)
    - [使用自定义加载屏幕](#使用自定义加载屏幕)
  - [参考图片](#参考图片)
  - [支持开发者](#支持开发者-1)
    - [🌟 给项目一个 Star](#-给项目一个-star)
    - [🏆 在游戏中致谢](#-在游戏中致谢)
    - [🐛 反馈与贡献](#-反馈与贡献)
    - [💬 联系作者](#-联系作者)

---

## 插件概述



### 插件核心特性

- ✅ **完全异步加载** - 不阻塞主线程，支持进度回调
- ✅ **三层缓存体系** - 实例缓存 + 临时预加载缓存 + 固定预加载缓存
- ✅ **五种加载策略** - 灵活控制缓存查找优先级
- ✅ **可定制加载屏幕** - 支持淡入淡出、进度条等效果
- ✅ **分帧实例化** - 避免大场景实例化时的卡顿
- ✅ **完整信号系统** - 便于监控和调试

---

## 基本使用

### 安装与启用

1. 将 `addons/long_scene_manager` 文件夹复制到你的项目的 `addons` 文件夹中
2. 在 Godot 中启用插件：
   - 转至 `项目 → 项目设置 → 插件`
   - 找到 "Long Scene Manager" 并将其状态设置为 "启用"
3. 插件会自动注册为 Autoload 单例：
   - **GDScript 版本**：访问名称为 `LongSceneManager`
   - **C# 版本**：访问名称为 `LongSceneManagerCs`

> **注意**：两个版本的功能完全一致，选择使用哪个版本取决于你的项目语言环境。GDScript 版本开箱即用，C# 版本需要你的项目配置了 C# 支持（.NET）。

### 插件配置（导出变量）

在编辑器中选中 `LongSceneManager` 节点，可以在检查器中配置以下参数：

| 变量名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `max_cache_size` | int (1~20) | 4 | 实例缓存最大容量，超出后按 LRU 淘汰 |
| `max_temp_preload_resource_cache_size` | int (1~50) | 8 | 临时预加载资源缓存最大容量，超出后按 FIFO 淘汰 |
| `max_fixed_preload_resource_cache_size` | int (0~50) | 4 | 固定预加载资源缓存最大容量，超出后按 FIFO 淘汰 |
| `use_async_loading` | bool | true | 是否使用异步加载（推荐开启） |
| `always_use_default_load_screen` | bool | false | 是否强制使用默认加载屏幕 |
| `instantiate_frames` | int (1~10) | 3 | 场景实例化时延迟的帧数 |

### GDScript 使用

```gdscript
# 访问单例
var manager = LongSceneManager

# 场景切换
manager.switch_scene("res://scenes/level_02.tscn")

# 预加载
manager.preload_scene("res://scenes/level_02.tscn")

# 获取缓存信息
var info = manager.get_cache_info()
```

### C# 使用

```csharp
// 访问单例
var manager = LongSceneManagerCs.Instance;

// 场景切换
await manager.SwitchScene("res://scenes/Level02.tscn", LongSceneManagerCs.LoadMethod.BothPreloadFirst, true, "");

// 预加载
manager.PreloadScene("res://scenes/Level02.tscn");

// 批量预加载
manager.PreloadScenes(new string[] { "res://scenes/Level02.tscn", "res://scenes/Level03.tscn" });

// 取消预加载
manager.CancelPreloadingScene("res://scenes/Level03.tscn");

// 获取缓存信息
var cacheInfo = manager.GetCacheInfo();

// 监听信号
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

### 场景切换基础

`switch_scene()` / `SwitchScene()` 是核心的场景切换函数：

**GDScript：**
```gdscript
# 最简切换（使用默认策略和默认加载屏幕）
LongSceneManager.switch_scene("res://scenes/level_02.tscn")

# 不缓存当前场景
LongSceneManager.switch_scene("res://scenes/main_menu.tscn", LongSceneManager.LoadMethod.BOTH_PRELOAD_FIRST, false)

# 使用 SCENE_CACHE 策略（优先复用实例）
await LongSceneManager.switch_scene("res://scenes/level_02.tscn", LongSceneManager.LoadMethod.SCENE_CACHE)

# 无过渡切换
LongSceneManager.switch_scene("res://scenes/level_02.tscn", LongSceneManager.LoadMethod.BOTH_PRELOAD_FIRST, true, "no_transition")
```

**C#：**
```csharp
// 最简切换（使用默认策略和默认加载屏幕）
await LongSceneManagerCs.Instance.SwitchScene("res://scenes/Level02.tscn");

// 不缓存当前场景
await LongSceneManagerCs.Instance.SwitchScene("res://scenes/MainMenu.tscn", LongSceneManagerCs.LoadMethod.BothPreloadFirst, false, "");

// 使用 SceneCache 策略（优先复用实例）
await LongSceneManagerCs.Instance.SwitchScene("res://scenes/Level02.tscn", LongSceneManagerCs.LoadMethod.SceneCache, true, "");

// 无过渡切换
await LongSceneManagerCs.Instance.SwitchScene("res://scenes/Level02.tscn", LongSceneManagerCs.LoadMethod.BothPreloadFirst, true, "no_transition");
```

### 场景预加载

预加载允许你在后台提前加载场景资源，切换时几乎瞬间完成：

**GDScript：**
```gdscript
# 临时预加载：资源可能被自动淘汰
LongSceneManager.preload_scene("res://scenes/level_02.tscn")

# 固定预加载：资源不会被自动淘汰，可多次实例化
LongSceneManager.preload_scene("res://scenes/boss_fight.tscn", true)

# 批量预加载
LongSceneManager.preload_scenes(["res://scenes/level_02.tscn", "res://scenes/level_03.tscn"])

# 取消预加载
LongSceneManager.cancel_preloading_scene("res://scenes/level_03.tscn")
```

**C#：**
```csharp
// 临时预加载：资源可能被自动淘汰
LongSceneManagerCs.Instance.PreloadScene("res://scenes/Level02.tscn");

// 固定预加载：资源不会被自动淘汰，可多次实例化
LongSceneManagerCs.Instance.PreloadScene("res://scenes/BossFight.tscn", true);

// 批量预加载
LongSceneManagerCs.Instance.PreloadScenes(new string[] { "res://scenes/Level02.tscn", "res://scenes/Level03.tscn" });

// 取消预加载
LongSceneManagerCs.Instance.CancelPreloadingScene("res://scenes/Level03.tscn");

// 取消所有预加载
LongSceneManagerCs.Instance.CancelAllPreloading();
```

### 预加载 + 切换的完整流程

**GDScript：**
```gdscript
# 1. 进入关卡后，预加载下一个关卡
func _on_level_01_ready():
    LongSceneManager.preload_scene("res://scenes/level_02.tscn")

# 2. 玩家到达出口，切换场景
func _on_exit_reached():
    await LongSceneManager.switch_scene("res://scenes/level_02.tscn")
```

**C#：**
```csharp
// 1. 进入关卡后，预加载下一个关卡
public override void _Ready()
{
    LongSceneManagerCs.Instance.PreloadScene("res://scenes/Level02.tscn");
}

// 2. 玩家到达出口，切换场景
private async void OnExitReached()
{
    await LongSceneManagerCs.Instance.SwitchScene("res://scenes/Level02.tscn");
}
```

---

## 进阶使用

### 五种加载策略详解

场景切换时，通过 `load_method` 参数控制缓存查找行为：

| 策略 | 枚举值 | 查找顺序 | 适用场景 |
|------|--------|---------|---------|
| `DIRECT` | 直接加载 | 预加载缓存 → 直接加载 | 不关心实例缓存，只要资源 |
| `PRELOAD_CACHE` | 预加载优先 | 预加载缓存 → fallback直接加载 | 只用预加载的资源 |
| `SCENE_CACHE` | 实例缓存优先 | 实例缓存 → fallback直接加载 | 优先复用已实例化的场景（保留状态） |
| `BOTH_PRELOAD_FIRST` | 混合预加载优先 | 预加载缓存 → 实例缓存 → 正在预加载 → 直接加载 | **默认策略**，推荐大多数情况 |
| `BOTH_INSTANCE_FIRST` | 混合实例优先 | 实例缓存 → 预加载缓存 → 正在预加载 → 直接加载 | 优先复用场景实例（保留状态） |

> **重要**：只有命中**实例缓存**时才会复用已实例化的场景，该场景会**保留切换前的完整运行时状态**。

### 三层缓存体系

```
┌─────────────────────────────────────────────────────────┐
│                    LongSceneManager                      │
├─────────────────────────────────────────────────────────┤
│  第一层：实例缓存 (Instance Cache)                         │
│  存储：完整场景 Node + 缓存时间戳                          │
│  淘汰策略：LRU（最近最少使用）                             │
│  默认容量：4                                             │
│  特点：保留切换前的完整运行时状态                           │
├─────────────────────────────────────────────────────────┤
│  第二层：临时预加载缓存 (Temp Preload Cache)               │
│  存储：PackedScene 资源                                  │
│  淘汰策略：FIFO（先进先出）                                │
│  默认容量：8                                             │
│  特点：资源被使用后会从缓存中移除（消耗型）                  │
├─────────────────────────────────────────────────────────┤
│  第三层：固定预加载缓存 (Fixed Preload Cache)              │
│  存储：PackedScene 资源                                  │
│  淘汰策略：FIFO（先进先出）                                │
│  默认容量：4                                             │
│  特点：资源被使用后保留在缓存中（持久型）                    │
└─────────────────────────────────────────────────────────┘
```

**核心设计原则**：场景实例要么在场景树中（当前活跃），要么在缓存中（非活跃但保留在内存中）。两者严格分离，绝不共存。

### 缓存管理API

**GDScript：**
```gdscript
# 清空所有缓存
LongSceneManager.clear_all_cache()

# 清空特定缓存
LongSceneManager.clear_temp_preload_cache()  # 临时预加载缓存
LongSceneManager.clear_fixed_cache()          # 固定预加载缓存
LongSceneManager.clear_instance_cache()       # 实例缓存

# 移除单个资源
LongSceneManager.remove_temp_resource("res://scenes/level_02.tscn")
LongSceneManager.remove_fixed_resource("res://scenes/shop.tscn")
LongSceneManager.remove_cached_scene("res://scenes/level_02.tscn")

# 移动资源
LongSceneManager.move_to_fixed("res://scenes/boss_fight.tscn")  # 临时→固定
LongSceneManager.move_to_temp("res://scenes/boss_fight.tscn")   # 固定→临时

# 动态调整缓存大小
LongSceneManager.set_max_cache_size(10)
```

**C#：**
```csharp
// 清空所有缓存
LongSceneManagerCs.Instance.ClearAllCache();

// 清空特定缓存
LongSceneManagerCs.Instance.ClearTempPreloadCache();   // 临时预加载缓存
LongSceneManagerCs.Instance.ClearFixedCache();          // 固定预加载缓存
LongSceneManagerCs.Instance.ClearInstanceCache();     // 实例缓存

// 移除单个资源
LongSceneManagerCs.Instance.RemoveTempResource("res://scenes/Level02.tscn");
LongSceneManagerCs.Instance.RemoveFixedResource("res://scenes/Shop.tscn");
LongSceneManagerCs.Instance.RemoveCachedScene("res://scenes/Level02.tscn");

// 移动资源
LongSceneManagerCs.Instance.MoveToFixed("res://scenes/BossFight.tscn");  // 临时→固定
LongSceneManagerCs.Instance.MoveToTemp("res://scenes/BossFight.tscn");   // 固定→临时

// 动态调整缓存大小
LongSceneManagerCs.Instance.SetMaxCacheSize(10);
```

### 信号系统

**GDScript：**
```gdscript
# 监听场景切换事件
LongSceneManager.scene_switch_started.connect(_on_switch_started)
LongSceneManager.scene_switch_completed.connect(_on_switch_completed)
LongSceneManager.scene_preload_started.connect(_on_preload_started)
LongSceneManager.scene_preload_completed.connect(_on_preload_completed)
LongSceneManager.scene_cached.connect(_on_scene_cached)

# 或者使用自动连接
LongSceneManager.connect_all_signals(self)
# 然后实现对应方法：_on_scene_manager_scene_switch_started()
```

**C#：**
```csharp
// 监听场景切换事件
LongSceneManagerCs.Instance.SceneSwitchStarted += OnSceneSwitchStarted;
LongSceneManagerCs.Instance.SceneSwitchCompleted += OnSceneSwitchCompleted;
LongSceneManagerCs.Instance.ScenePreloadStarted += OnScenePreloadStarted;
LongSceneManagerCs.Instance.ScenePreloadCompleted += OnScenePreloadCompleted;
LongSceneManagerCs.Instance.SceneCached += OnSceneCached;

// 取消监听
LongSceneManagerCs.Instance.SceneSwitchStarted -= OnSceneSwitchStarted;
```

**可用信号列表：**

| 信号 | 参数 | 说明 |
|------|------|------|
| `scene_preload_started` | scene_path | 预加载开始 |
| `scene_preload_completed` | scene_path | 预加载完成 |
| `scene_preload_cancelled` | scene_path | 预加载取消 |
| `scene_switch_started` | from_scene, to_scene | 切换开始 |
| `scene_switch_completed` | scene_path | 切换完成 |
| `scene_cached` | scene_path | 场景已缓存 |
| `scene_removed_from_cache` | scene_path | 场景从缓存移除 |
| `load_screen_shown` | instance | 加载屏幕显示 |
| `load_screen_hidden` | instance | 加载屏幕隐藏 |
| `scene_preload_failed` | scene_path | 预加载失败 |
| `scene_switch_failed` | scene_path | 切换失败 |

---

## 插件开发指南

### 核心设计原则

1. **场景树与缓存严格分离**
   - 场景实例要么在场景树中（当前活跃），要么在缓存中
   - 两者绝不共存，这避免了节点管理混乱

2. **状态机驱动**
   - 每个场景资源经历五态流转：`NOT_LOADED → LOADING → LOADED → INSTANTIATED`
   - 支持 `CANCELLED` 状态用于取消预加载

3. **异步优先**
   - 资源加载使用 `ResourceLoader.load_threaded_request()`
   - 分帧实例化避免主线程卡顿

4. **可扩展加载屏幕**
   - 支持自定义加载屏幕场景
   - 通过方法名约定实现兼容性

### 代码结构解析

```
addons/long_scene_manager/
├── autoload/
│   ├── long_scene_manager.gd       # GDScript 核心管理器（单例）
│   └── LongSceneManagerCs.cs       # C# 核心管理器（单例）
├── ui/
│   └── loading_screen/
│       ├── GDScript/               # GDScript 版本加载屏幕
│       │   └── loading_black_screen.tscn
│       └── CSharp/                 # C# 版本加载屏幕
│           └── loading_black_screen_cs.tscn
└── image_icon/
    └── icon3.png
```

**long_scene_manager.gd 核心代码分区：**

| 区域 | 行号范围 | 说明 |
|------|---------|------|
| 常量和枚举 | 1-50 | LoadState、LoadMethod 枚举定义 |
| 信号定义 | 52-65 | 所有场景事件信号 |
| 导出变量 | 67-75 | 插件配置项 |
| 内部状态 | 77-95 | 缓存和场景状态变量 |
| 内部类 | 96-102 | CachedScene 数据类 |
| 生命周期 | 104-115 | _ready() 初始化 |
| 公开API - 场景切换 | 117-145 | switch_scene() |
| 公开API - 预加载 | 147-185 | preload_*() |
| 公开API - 缓存管理 | 187-320 | clear_*, remove_*, move_* |
| 公开API - 查询 | 322-420 | get_cache_info(), get_*() |
| 公开API - 调试 | 422-450 | print_debug_info() |
| 私有函数 - 初始化 | 550-600 | _init_default_load_screen() |
| 私有函数 - 预加载核心 | 650-750 | _async_preload_scene() |
| 私有函数 - 加载屏幕 | 760-830 | _show_load_screen() |
| 私有函数 - 场景加载 | 850-1000 | _load_scene_by_method() |
| 私有函数 - 实例化 | 1000-1100 | _instantiate_scene_deferred() |
| 私有函数 - 缓存管理 | 1100-1200 | _add_to_cache() |

### 运行逻辑流程

#### 场景切换流程

```
switch_scene() 被调用
    │
    ├─► 验证场景路径是否有效
    │
    ├─► 发送 scene_switch_started 信号
    │
    ├─► 获取加载屏幕实例
    │
    ├─► 显示加载屏幕 (_show_load_screen)
    │       │
    │       ├─► 设置可见性
    │       │
    │       └─► 调用 fade_in() 或 show_loading()
    │
    ├─► 根据 LoadMethod 选择加载策略
    │       │
    │       ├─► DIRECT: 直接加载或使用预加载资源
    │       │
    │       ├─► PRELOAD_CACHE: 只查预加载缓存
    │       │
    │       ├─► SCENE_CACHE: 只查实例缓存
    │       │
    │       ├─► BOTH_PRELOAD_FIRST: 预加载→实例→正在预加载→直接
    │       │
    │       └─► BOTH_INSTANCE_FIRST: 实例→预加载→正在预加载→直接
    │
    ├─► 执行场景切换 (_perform_scene_switch)
    │       │
    │       ├─► 移除旧场景
    │       │
    │       ├─► 如果需要缓存，添加到实例缓存
    │       │
    │       ├─► 添加新场景到场景树
    │       │
    │       └─► 等待 ready 信号
    │
    ├─► 隐藏加载屏幕 (_hide_load_screen)
    │       │
    │       ├─► 调用 fade_out() 或 hide_loading()
    │       │
    │       └─► 清理自定义加载屏幕
    │
    └─► 发送 scene_switch_completed 信号
```

#### 预加载流程

```
preload_scene() 被调用
    │
    ├─► 检查是否已在缓存或正在加载
    │
    ├─► 创建预加载状态 (LoadState.LOADING)
    │
    ├─► 发送 scene_preload_started 信号
    │
    └─► 启动后台预加载 (_preload_background)
            │
            ├─► 异步模式: ResourceLoader.load_threaded_request()
            │       │
            │       └─► 轮询状态直到 LOADED
            │
            └─► 同步模式: 直接 load()
                    │
                    └─► 存储到预加载缓存
                            │
                            ├─► 临时缓存 (FIFO淘汰)
                            └─► 固定缓存 (持久保留)
```

### 关键数据结构

#### LoadState 枚举

```gdscript
enum LoadState {
    NOT_LOADED,      # 初始状态
    LOADING,         # 异步加载中
    LOADED,          # 资源已加载（未实例化）
    INSTANTIATED,    # 已实例化（在实例缓存中）
    CANCELLED        # 预加载被取消
}
```

#### CachedScene 内部类

```gdscript
class CachedScene:
    var scene_instance: Node   # 场景实例
    var cached_time: float     # 缓存时间戳

    func _init(scene: Node):
        scene_instance = scene
        cached_time = Time.get_unix_time_from_system()
```

#### 缓存状态字典

```gdscript
# 实例缓存结构
instantiate_scene_cache = {
    "res://scenes/level_01.tscn": CachedScene,
    "res://scenes/level_02.tscn": CachedScene
}
instantiate_scene_cache_order = ["level_01", "level_02"]  # LRU顺序

# 预加载资源缓存结构
temp_preloaded_resource_cache = {
    "res://scenes/level_03.tscn": PackedScene
}
temp_preloaded_resource_cache_order = ["level_03"]  # FIFO顺序

fixed_preload_resource_cache = {
    "res://scenes/boss.tscn": PackedScene
}

# 预加载状态
_preload_resource_states = {
    "res://scenes/level_01.tscn": {
        "state": LoadState.LOADED,
        "resource": PackedScene,
        "fixed": false
    }
}
```

---

## 完整API参考

### 场景切换

| 函数 | 说明 |
|------|------|
| `switch_scene(new_scene_path, load_method, cache_current_scene, load_screen_path)` | 切换场景 |

### 预加载

| 函数 | 说明 |
|------|------|
| `preload_scene(scene_path, fixed)` | 预加载单个场景 |
| `preload_scenes(scene_paths, fixed)` | 批量预加载 |
| `cancel_preloading_scene(scene_path)` | 取消单个预加载 |
| `cancel_all_preloading()` | 取消所有预加载 |

### 缓存管理

| 函数 | 说明 |
|------|------|
| `clear_all_cache()` | 清空所有缓存 |
| `clear_temp_preload_cache()` | 清空临时预加载缓存 |
| `clear_fixed_cache()` | 清空固定预加载缓存 |
| `clear_instance_cache()` | 清空实例缓存 |
| `remove_temp_resource(scene_path)` | 移除临时预加载资源 |
| `remove_fixed_resource(scene_path)` | 移除固定预加载资源 |
| `remove_cached_scene(scene_path)` | 移除缓存场景 |
| `move_to_fixed(scene_path)` | 移至固定缓存 |
| `move_to_temp(scene_path)` | 移至临时缓存 |
| `set_max_cache_size(new_size)` | 设置实例缓存容量 |
| `set_max_temp_preload_resource_cache_size(new_size)` | 设置临时缓存容量 |
| `set_max_fixed_cache_size(new_size)` | 设置固定缓存容量 |

### 查询

| 函数 | 说明 |
|------|------|
| `get_cache_info()` | 获取完整缓存状态 |
| `is_scene_cached(scene_path)` | 检查场景是否在缓存中 |
| `is_scene_preloading(scene_path)` | 检查场景是否正在预加载 |
| `get_preloading_scenes()` | 获取正在预加载的场景列表 |
| `get_current_scene()` | 获取当前场景节点 |
| `get_previous_scene_path()` | 获取上一个场景路径 |
| `get_loading_progress(scene_path)` | 获取加载进度 (0.0~1.0) |
| `is_in_fixed_cache(scene_path)` | 检查是否在固定缓存中 |

### 调试

| 函数 | 说明 |
|------|------|
| `print_debug_info()` | 打印调试信息 |
| `connect_all_signals(target)` | 自动连接所有信号 |

---

## 加载屏幕自定义

### 使用默认加载屏幕

```gdscript
# 空字符串或 "default" 使用默认黑屏
await LongSceneManager.switch_scene("res://scenes/level_02.tscn", LoadMethod.BOTH_PRELOAD_FIRST, true, "")
```

### 创建自定义加载屏幕

自定义加载屏幕场景需要实现以下方法（可选，按需实现）：

```gdscript
extends CanvasLayer

@onready var progress_bar: ProgressBar = $ProgressBar

func fade_in():
    # 显示时的淡入动画
    var tween = create_tween()
    tween.tween_property(self, "modulate:a", 1.0, 0.3)

func fade_out():
    # 隐藏时的淡出动画
    var tween = create_tween()
    tween.tween_property(self, "modulate:a", 0.0, 0.3)

func set_progress(progress: float):
    # 更新进度 (0.0 ~ 1.0)
    progress_bar.value = progress * 100.0
```

### 使用自定义加载屏幕

```gdscript
await LongSceneManager.switch_scene(
    "res://scenes/level_02.tscn",
    LoadMethod.BOTH_PRELOAD_FIRST,
    true,
    "res://ui/my_loading_screen.tscn"  # 自定义路径
)
```

---

## 参考图片

插件自带演示项目 `demo_test_scene_manager/`，包含：

- `main_scene.tscn` - 主界面，展示基本切换功能
- `test_scene_1.tscn` - 测试场景1
- `test_scene_2.tscn` - 测试场景2，展示完整的缓存管理功能

运行演示项目可以直观了解插件的各项功能。

<img src="./addons/long_scene_manager/image_icon/main1.png" >
<img src="./addons/long_scene_manager/image_icon/scene2.png" >
<img src="./addons/long_scene_manager/image_icon/scene3.png" >

---

## 支持开发者

这个插件是我一个人用业余时间开发和维护的。如果你觉得它对你有帮助，请考虑以下方式支持我：

### 🌟 给项目一个 Star

访问 GitHub 仓库，点击右上角的 Star 按钮。这是最简单也最有力的支持方式！

📣 **向其他开发者推荐**

- 在 Godot 社区、论坛、Discord 群组中分享这个插件
- 写一篇使用心得或教程
- 在你的游戏开发视频/直播中提到这个插件

### 🏆 在游戏中致谢

在你的游戏感谢名单（Credits）中加入：

```
场景管理器插件 - LongSceneManager by LongZhan
https://github.com/AWAUOX/GodotPlugin_LongSceneManager
```

### 🐛 反馈与贡献

- 发现 Bug？在 GitHub 上提交 Issue
- 有好的想法？提交 Feature Request
- 想贡献代码？提交 Pull Request

### 💬 联系作者

- GitHub: [@AWAUOX](https://github.com/AWAUOX)

一个人的开发之路很孤独，你的每一个 Star 都是我继续前进的动力！谢谢！ 🙏
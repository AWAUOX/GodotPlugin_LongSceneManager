# Long Scene Manager Plugin

[English Documentation](README.md)

Long Scene Manager 是一个 Godot 插件，旨在简化和优化场景切换过程，特别是对于需要长时间加载的复杂场景。它通过提供异步场景加载、缓存机制和可定制的加载界面来改善用户体验。

---
<img src="./addons/long_scene_manager/image_icon/icon3.png" width = 30%>

## 项目介绍

### 为什么需要这个插件？

Godot 引擎内置的 `preload()` 函数在某些情况下可能存在以下问题：
- **内存泄漏风险**：预加载的资源可能不会被正确释放
- **阻塞主线程**：大型场景预加载时可能影响游戏流畅度
- **缺乏缓存管理**：没有内置的LRU缓存淘汰策略
- **无加载界面支持**：切换场景时无法显示自定义加载屏幕

**本插件可以作为 `preload()` 的完美替代品**，并提供更多高级功能：
- ✅ 完全异步加载，不阻塞主线程
- ✅ 双层LRU缓存（实例缓存 + 资源缓存）
- ✅ 可定制的加载屏幕（支持淡入淡出效果）
- ✅ 多场景预加载状态追踪
- ✅ 场景重置功能
- ✅ 完整的调试和监控支持

---

## 支持我

如果您觉得这个插件对您的项目有帮助，请：

1. **给 GitHub 仓库一个 Star**：[https://github.com/AWAUOX/GodotPlugin_LongSceneManager](https://github.com/AWAUOX/GodotPlugin_LongSceneManager)
2. **在游戏的感谢名单中提到本插件作者**

您的支持是我持续改进的动力！（求求啦，自己一个人干真的好累😭😭😭）

---

## 本次更新（2026.05）

本次更新基本完成了所有计划功能，包括：

### GDScript 版本
- ✅ 完整的场景切换、预加载、缓存管理
- ✅ 场景重置功能（`mark_scene_for_reset()` / `unmark_scene_for_reset()`）
- ✅ 多场景预加载状态追踪（`_preload_states` 字典）
- ✅ 跨多帧实例化场景（`instantiate_frames` 参数）
- ✅ 异步/同步加载可选（`use_async_loading`）
- ✅ 完整的信号系统

### C# 版本
- ✅ 与 GDScript 版本功能完全对齐
- ✅ 使用 C# 的 `async/await` 模式
- ✅ 正确的 Godot Signal 委托声明（`[Signal]` 特性）
- ✅ 修复了资源缓存问题（使用 `ResourceLoader.CacheMode.Ignore`）
- ✅ 完整的中文+英文双语注释

### 修复的问题
- 修复了重复方法定义导致的编译错误
- 修复了 `PerformSceneSwitch` 方法缺失的问题
- 修复了 `RemovePreloadedResource` 和 `RemoveCachedScene` 方法缺失的问题
- 修复了第一次场景切换无黑屏过渡的问题
- 修复了预加载后移除资源无法重新预加载的问题

**当前更新已经完成所有功能性需求，后续如果没有重大错误或者漏洞，将不再添加新功能，而只会跟进godot的官方版本更新**

---

## 项目设计思路

### 架构设计

```
LongSceneManager (AutoLoad 单例)
│
├─ 场景树管理
│  ├─ 当前活跃场景 (_current_scene)
│  └─ 场景切换逻辑 (SwitchScene)
│
├─ 双层缓存系统
│  ├─ 实例缓存 (_scene_cache) - 存储完整的场景节点
│  └─ 预加载资源缓存 (_preload_resource_cache) - 存储 PackedScene 资源
│
├─ 预加载管理
│  ├─ 多场景状态追踪 (_preload_states)
│  ├─ 异步加载 (ResourceLoader.ThreadedRequest)
│  └─ 同步加载 (ResourceLoader.Load)
│
├─ 加载屏幕管理
│  ├─ 默认加载屏幕
│  └─ 自定义加载屏幕
│
└─ 信号系统
   ├─ 场景切换信号
   ├─ 预加载信号
   └─ 缓存管理信号
```

### 核心设计原则

1. **场景树与缓存分离**
   - 场景实例要么在场景树中（当前活跃）
   - 要么在缓存中（非活跃但保留在内存中）
   - 这样设计可以避免场景节点同时存在于两个地方

2. **LRU 缓存策略**
   - 实例缓存：缓存完整的场景节点实例
   - 预加载资源缓存：只缓存 PackedScene 资源（更省内存）
   - 当缓存达到上限时，自动移除最久未使用的项目

3. **异步优先**
   - 默认使用 `ResourceLoader.ThreadedRequest` 进行异步加载
   - 支持进度回调，可在加载屏幕显示进度条
   - 可选使用同步加载（设置 `use_async_loading = false`）

4. **多场景预加载**
   - 使用字典追踪多个场景的预加载状态
   - 支持同时预加载多个场景
   - 每个场景有独立的加载状态（NOT_LOADED / LOADING / LOADED）

---

## 参考图片

<img src="./addons/long_scene_manager/image_icon/main1.png">

<img src="./addons/long_scene_manager/image_icon/scene22.png">

<img src="./addons/long_scene_manager/image_icon/scene3.png">


## 基础用法

### 安装方法

1. 将 `addons/long_scene_manager` 文件夹复制到项目的 `addons` 文件夹中
2. 在 Godot 中启用插件：
   - 转到 `项目 → 项目设置 → 插件`
   - 找到 "Long Scene Manager" 并将其状态设置为 "启用"

### 插件配置

该插件实现为全局自动加载单例。根据您想要使用 GDScript 还是 C# 实现，您需要在 `plugin.cfg` 文件中更改脚本路径：

1. 打开 `addons/long_scene_manager/plugin.cfg`
2. 修改 `script` 条目以指向 GDScript 或 C# 实现：
   - 对于 GDScript：`script="res://addons/long_scene_manager/autoload/long_scene_manager.gd"`
   - 对于 C#：`script="res://addons/long_scene_manager/autoload/LongSceneManagerCs.cs"`
3. 在项目设置 → 自动加载中，确认为 `LongSceneManager` 单例注册了正确的路径

---

### 场景切换 - 基础用法

#### 1. 预加载场景（后台提前加载）

**GDScript 版本**
```gdscript
# 预加载场景到缓存中（后台异步加载，不阻塞）
LongSceneManager.preload_scene("res://scenes/level2.tscn")

# 可以预加载多个场景
LongSceneManager.preload_scenes(["res://scenes/level2.tscn"])
LongSceneManager.preload_scenes(["res://scenes/level3.tscn"])
```

**C# 版本**
```csharp
var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");

// 预加载场景（后台异步加载）
manager.PreloadSceneGD("res://scenes/level2.tscn");

// 或者使用异步方法（可以 await 等待完成）
await manager.PreloadScene("res://scenes/level2.tscn");
```

---

#### 2. 切换预加载的场景（使用缓存）

**GDScript 版本**
```gdscript
# 先预加载场景
LongSceneManager.preload_scene("res://scenes/level2.tscn")

# 等待预加载完成（可选，如果已经预加载完成，会立即切换）
await LongSceneManager.switch_scene(
    "res://scenes/level2.tscn", 
    true,   # 使用缓存
    ""      # 使用默认加载屏幕
)
# 切换时会从预加载资源缓存中获取，速度很快
```

**C# 版本**
```csharp
var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");

// 先预加载场景
manager.PreloadSceneGD("res://scenes/level2.tscn");

// 切换场景（使用缓存，会从预加载资源缓存中获取）
await manager.SwitchScene("res://scenes/level2.tscn", true, "");
```

---

#### 3. 不预加载，直接切换场景（无缓存）

**GDScript 版本**
```gdscript
# 直接加载并切换场景（不使用缓存机制）
await LongSceneManager.switch_scene(
    "res://scenes/level2.tscn", 
    false,  # false = 不使用缓存
    ""     # 使用默认加载屏幕
)
# 场景会立即开始加载
```

**C# 版本**
```csharp
var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");

// 直接切换场景（不使用缓存）
await manager.SwitchScene("res://scenes/level2.tscn", false, "");
```

---

#### 4. 使用自定义过渡场景（加载屏幕）

**GDScript 版本**
```gdscript
# 使用自定义加载屏幕进行场景切换
await LongSceneManager.switch_scene(
    "res://scenes/level2.tscn", 
    true,                       # 使用缓存
    "res://ui/my_load_screen.tscn"  # 自定义加载屏幕路径
)
# 自定义加载屏幕应该实现 fade_in()、fade_out() 等方法来控制过渡效果
```

**C# 版本**
```csharp
var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");

// 使用自定义加载屏幕
await manager.SwitchScene(
    "res://scenes/level2.tscn", 
    true,                       
    "res://ui/my_load_screen.tscn"  // 自定义加载屏幕路径
);
```

**自定义加载屏幕需要实现的方法**：
```gdscript
# 在自定义加载屏幕场景中，可以实现以下方法：
extends CanvasLayer

func fade_in():
    # 淡入效果（显示加载屏幕）
    print("开始淡入")
    await get_tree().create_timer(0.5).timeout
    print("淡入完成")

func fade_out():
    # 淡出效果（隐藏加载屏幕）
    print("开始淡出")
    await get_tree().create_timer(0.5).timeout
    print("淡出完成")

func set_progress(progress: float):
    # 更新进度条（可选）
    $ProgressBar.value = progress * 100
```

---

#### 5. 使用默认过渡场景（加载屏幕）

**GDScript 版本**
```gdscript
# 使用默认加载屏幕（黑色背景 + "Loading..." 文字）
await LongSceneManager.switch_scene(
    "res://scenes/level2.tscn", 
    true,  # 使用缓存
    ""    # 空字符串 = 使用默认加载屏幕
)
# 默认加载屏幕会自动淡入和淡出
```

**C# 版本**
```csharp
var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");

// 使用默认加载屏幕
await manager.SwitchScene("res://scenes/level2.tscn", true, "");

// 或者使用 GDScript 兼容方法
manager.SwitchSceneGD("res://scenes/level2.tscn", true, "");
```

---

#### 6. 不使用过渡场景（无过渡，快速切换）

**GDScript 版本**
```gdscript
# 强制不使用任何加载屏幕，场景会立即切换（无淡入淡出）
await LongSceneManager.switch_scene(
    "res://scenes/level2.tscn", 
    true,
    "no_transition"  # 特殊值，表示无过渡
)
# 适合快速切换场景，或者自己控制过渡效果的情况
```

**C# 版本**
```csharp
var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");

// 不使用过渡场景
await manager.SwitchScene("res://scenes/level2.tscn", true, "no_transition");

// 或者使用 GDScript 兼容方法
manager.SwitchSceneGD("res://scenes/level2.tscn", true, "no_transition");
```

---

### 场景预加载 - 高级管理

#### GDScript 版本
```gdscript
# 批量预加载多个场景
LongSceneManager.preload_scenes([
    "res://scenes/level3.tscn",
    "res://scenes/level4.tscn"
])

# 取消预加载（如果正在加载中）
LongSceneManager.cancel_preload_scene("res://scenes/level3.tscn")

# 取消所有预加载
LongSceneManager.cancel_all_preloads()

# 获取预加载进度（0.0 - 1.0）
var progress = LongSceneManager.get_loading_progress("res://scenes/level3.tscn")
print("加载进度: ", progress * 100, "%")

# 检查场景是否正在预加载
if LongSceneManager.is_scene_preloading("res://scenes/level3.tscn"):
    print("场景正在预加载中...")
```

#### C# 版本
```csharp
var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");

// 批量预加载多个场景（循环调用）
string[] scenesToPreload = {
    "res://scenes/level3.tscn",
    "res://scenes/level4.tscn"
};
foreach (var scene in scenesToPreload)
{
    manager.PreloadSceneGD(scene);
}

// 取消预加载
manager.CancelPreloadScene("res://scenes/level3.tscn");

// 取消所有预加载
manager.CancelAllPreloads();

// 获取预加载进度（0.0 - 1.0）
float progress = manager.GetLoadingProgress("res://scenes/level3.tscn");
GD.Print($"加载进度: {progress * 100}%");
```

---

## 高级用法

### 1. 缓存管理 - 详细示例

缓存系统有**两层**：实例缓存（存储完整场景节点）+ 预加载资源缓存（存储 PackedScene 资源）。

#### GDScript 版本
```gdscript
# 获取缓存详细信息（用于调试和监控）
var cache_info = LongSceneManager.get_cache_info()
print("当前场景: ", cache_info.current_scene)
print("上一个场景: ", cache_info.previous_scene)
print("实例缓存数量: ", cache_info.instance_cache.size, "/", cache_info.instance_cache.max_size)
print("预加载缓存数量: ", cache_info.preload_cache.size, "/", cache_info.preload_cache.max_size)

# 遍历实例缓存中的场景
for item in cache_info.instance_cache.scenes:
    print("缓存场景: ", item.path, " 缓存时间: ", item.cached_time)

# 遍历预加载资源缓存中的场景
for scene_path in cache_info.preload_cache.scenes:
    print("预加载资源: ", scene_path)

# 动态调整缓存大小（⚠️ 注意：尽量一开始就确定好大小，少在运行时改动）
LongSceneManager.set_max_cache_size(10)       # 实例缓存最大10个场景
LongSceneManager.set_max_preload_resource_cache_size(30)  # 预加载资源缓存最大30个

# 清空所有缓存（实例缓存 + 预加载资源缓存）
LongSceneManager.clear_cache()
print("所有缓存已清空")

# 只移除预加载资源（不影响实例缓存）
LongSceneManager.remove_preloaded_resource("res://scenes/level3.tscn")
print("已移除预加载资源")

# 只移除缓存的场景实例（不影响预加载资源缓存）
LongSceneManager.remove_cached_scene("res://scenes/level3.tscn")
print("已移除缓存场景实例")

# 检查场景是否已在任意缓存中
if LongSceneManager.is_scene_cached("res://scenes/level3.tscn"):
    print("场景已在缓存中")

# 获取当前正在预加载的场景列表
var loading = LongSceneManager.get_preloading_scenes()
if loading.size() > 0:
    print("正在预加载的场景: ", loading)
```

#### C# 版本
```csharp
var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");

// 获取缓存详细信息（用于调试和监控）
var cacheInfo = manager.GetCacheInfo();
GD.Print($"当前场景: {cacheInfo["current_scene"]}");
GD.Print($"上一个场景: {cacheInfo["previous_scene"]}");

// 实例缓存信息
var instanceCache = (Godot.Collections.Dictionary)cacheInfo["instance_cache"];
GD.Print($"实例缓存数量: {instanceCache["size"]} / {instanceCache["max_size"]}");

// 预加载资源缓存信息
var preloadCache = (Godot.Collections.Dictionary)cacheInfo["preload_cache"];
GD.Print($"预加载缓存数量: {preloadCache["size"]} / {preloadCache["max_size"]}");

// 动态调整缓存大小（⚠️ 注意：尽量一开始就确定好大小，少在运行时改动）
manager.SetMaxCacheSize(10);       // 实例缓存最大10个场景
manager.SetMaxPreloadResourceCacheSize(30);  // 预加载资源缓存最大30个

// 清空所有缓存（实例缓存 + 预加载资源缓存）
manager.ClearCache();
GD.Print("所有缓存已清空");

// 只移除预加载资源（不影响实例缓存）
manager.RemovePreloadedResource("res://scenes/level3.tscn");
GD.Print("已移除预加载资源");

// 只移除缓存的场景实例（不影响预加载资源缓存）
manager.RemoveCachedScene("res://scenes/level3.tscn");
GD.Print("已移除缓存场景实例");

// 检查场景是否已在任意缓存中
if (manager.IsSceneCached("res://scenes/level3.tscn"))
{
    GD.Print("场景已在缓存中");
}

// 获取当前正在预加载的场景列表
var preloading = manager.GetPreloadingScenes();
if (preloading.Count > 0)
{
    GD.Print($"正在预加载的场景数量: {preloading.Count}");
}
```

**LRU 缓存淘汰策略**：
- 当缓存达到上限时，自动移除**最久未使用**的缓存项
- 实例缓存：移除时，场景实例会被 `queue_free()` 释放
- 预加载资源缓存：移除时，只从字典中删除，不释放资源（Godot 会自动管理）

---

### 2. 场景重置功能 - 详细示例

场景重置功能用于**强制刷新场景**。当您标记场景重置后，下次从场景树移除时，不会进入缓存，而是被释放并重新加载为预加载资源。

#### 使用场景
玩家死亡后需要重新加载关卡、场景状态需要完全重置等情况。

#### GDScript 版本
```gdscript
# 标记场景在下次切换时重置
# 例如：玩家死亡后，标记当前关卡需要重置
LongSceneManager.mark_scene_for_reset("res://scenes/level3.tscn")
print("场景已标记重置")

# 当切换场景时（比如回到主菜单），场景会：
# 1. 从场景树移除
# 2. 不进入实例缓存
# 3. 被释放（queue_free()）
# 4. 以资源形式重新加载到预加载缓存

# 取消标记（如果玩家复活了，不需要重置）
LongSceneManager.unmark_scene_for_reset("res://scenes/level3.tscn")
print("已取消场景重置标记")

# 检查场景是否被标记重置
if LongSceneManager._scenes_to_reset.has("res://scenes/level3.tscn"):
    print("场景已标记重置，下次切换会刷新")
```

#### C# 版本
```csharp
var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");

// 标记场景在下次切换时重置
manager.MarkSceneForReset("res://scenes/level3.tscn");
GD.Print("场景已标记重置");

// 当切换场景时，场景会：
// 1. 从场景树移除
// 2. 不进入实例缓存
// 3. 被释放（QueueFree()）
// 4. 以资源形式重新加载到预加载缓存

// 取消标记
manager.UnmarkSceneForReset("res://scenes/level3.tscn");
GD.Print("已取消场景重置标记");

// 检查场景是否被标记重置
// 注意：_scenesToReset 是内部字段，不建议直接访问
// 可以检查场景是否在缓存中来判断
if (!manager.IsSceneCached("res://scenes/level3.tscn"))
{
    GD.Print("场景不在缓存中，可能已标记重置");
}
```

**工作原理**：
1. 调用 `mark_scene_for_reset()` 后，场景路径被记录到 `_scenes_to_reset` 字典
2. 当场景从场景树移除时（切换场景），检查是否有重置标记
3. 如果有标记：释放场景实例 + 以资源形式重新加载到预加载缓存
4. 重置标记只生效**一次**，之后自动清除

**使用场景**：
- ✅ 玩家死亡后需要重新加载关卡
- ✅ 场景状态需要完全刷新
- ✅ 测试场景重新加载逻辑
- ❌ 不要用于需要保留状态的场景

---

### 3. 自定义加载屏幕 - 详细示例

自定义加载屏幕可以让您的游戏有独特的过渡效果。

#### 创建自定义加载屏幕（GDScript）

**步骤1：创建场景文件**
```
MyLoadScreen.tscn
└── CanvasLayer (根节点)
    ├── ColorRect (全屏背景)
    ├── Label (提示文字)
    └── ProgressBar (进度条，可选)
```

**步骤2：编写脚本 `my_load_screen.gd`**
```gdscript
extends CanvasLayer

func fade_in():
    print("自定义加载屏幕：开始淡入")
    
    # 设置初始状态（完全透明）
    modulate.a = 0.0
    
    # 使用 Tween 实现淡入动画
    var tween = get_tree().create_tween()
    tween.tween_property(self, "modulate:a", 1.0, 0.5)
    await tween.finished
    
    print("自定义加载屏幕：淡入完成")

func fade_out():
    print("自定义加载屏幕：开始淡出")
    
    # 使用 Tween 实现淡出动画
    var tween = get_tree().create_tween()
    tween.tween_property(self, "modulate:a", 0.0, 0.5)
    await tween.finished
    
    print("自定义加载屏幕：淡出完成")

func set_progress(progress: float):
    if has_node("ProgressBar"):
        $ProgressBar.value = progress * 100.0
        print("加载进度：", progress * 100, "%")

# 或者使用 show_loading / hide_loading 方法名
func show_loading():
    print("自定义加载屏幕：显示")
    visible = true
    modulate.a = 1.0

func hide_loading():
    print("自定义加载屏幕：隐藏")
    visible = false
```

#### 使用自定义加载屏幕（GDScript）

```gdscript
# 方式1：在场景切换时指定
await LongSceneManager.switch_scene(
    "res://scenes/level2.tscn", 
    true,                       # 使用缓存
    "res://ui/my_load_screen.tscn"  # 自定义加载屏幕路径
)
# 自定义加载屏幕会自动淡入和淡出

# 方式2：设置 always_use_default_load_screen，会忽略自定义设置
# 在编辑器中设置，或者通过代码：
LongSceneManager.always_use_default_load_screen = false
```

**注意事项**：
- ✅ 自定义加载屏幕会自动淡入和淡出
- ✅ 如果实现了 `set_progress` 方法，预加载时会自动更新进度
- ✅ 如果自定义加载屏幕加载失败，会自动回退到默认加载屏幕
- ❌ 自定义加载屏幕**不会**被缓存，每次都会重新加载

---

#### 创建自定义加载屏幕（C#）

**步骤1：创建场景文件**（同上）

**步骤2：编写脚本 `MyLoadScreenCs.cs`**
```csharp
using Godot;

public partial class MyLoadScreenCs : CanvasLayer
{
    private ProgressBar progressBar;

    public override void _Ready()
    {
        progressBar = GetNode<ProgressBar>("ProgressBar");
    }

    // 淡入效果（显示加载屏幕）
    public async void FadeIn()
    {
        GD.Print("自定义加载屏幕：开始淡入");
        
        // 设置初始状态（完全透明）
        Modulate = new Color(Modulate.R, Modulate.G, Modulate.B, 0.0f);
        
        // 使用 Tween 实现淡入动画
        var tween = GetTree().CreateTween();
        tween.TweenProperty(this, "modulate:a", 1.0f, 0.5f);
        await ToSignal(tween, "finished");
        
        GD.Print("自定义加载屏幕：淡入完成");
    }

    // 淡出效果（隐藏加载屏幕）
    public async void FadeOut()
    {
        GD.Print("自定义加载屏幕：开始淡出");
        
        // 使用 Tween 实现淡出动画
        var tween = GetTree().CreateTween();
        tween.TweenProperty(this, "modulate:a", 0.0f, 0.5f);
        await ToSignal(tween, "finished");
        
        GD.Print("自定义加载屏幕：淡出完成");
    }

    // 更新进度（可选）
    public void SetProgress(float progress)
    {
        if (progressBar != null)
        {
            progressBar.Value = progress * 100.0f;
            GD.Print($"加载进度：{progress * 100}%");
        }
    }
}
```

#### 使用自定义加载屏幕（C#）

```csharp
var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");

// 使用自定义加载屏幕
await manager.SwitchScene(
    "res://scenes/level2.tscn",
    true,                       
    "res://ui/my_load_screen.tscn"  // 自定义加载屏幕路径
);
```

---

### 4. 跨多帧实例化 - 详细示例

对于包含大量节点的复杂场景，可以使用多帧实例化来避免卡顿。原理是将场景实例化过程分散到多帧执行。

#### GDScript 版本
```gdscript
# 方式1：在编辑器中设置 instantiate_frames 参数（1-10）
# 选中 LongSceneManager 节点，在检查器中设置 Instantiate Frames

# 方式2：或者在代码中动态设置
LongSceneManager.instantiate_frames = 5  # 分散到5帧实例化

# 切换场景时，会自动使用多帧实例化
await LongSceneManager.switch_scene("res://scenes/heavy_scene.tscn")

# 原理：
# - 每帧只处理一部分节点（设置 process = false）
# - 全部处理完后，再统一恢复所有节点的 process 状态
# - 避免单帧处理过多节点导致卡顿
```

#### C# 版本
```csharp
var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");

// 设置分散到5帧实例化
manager.Set("instantiate_frames", 5);

// 或者使用属性访问（如果暴露了属性）
// manager.InstantiateFrames = 5;

// 切换场景时，会自动使用多帧实例化
await manager.SwitchScene("res://scenes/heavy_scene.tscn");
```

**使用场景**：
- ✅ 包含大量节点的复杂3D场景
- ✅ 需要实例化的场景节点超过100个
- ✅ 单帧实例化导致明显卡顿的情况
- ❌ 简单场景（不需要使用）

**参数说明**：
| instantiate_frames 值 | 效果 |
|-------------------|------|
| 1 | 单帧实例化（无优化） |
| 3-5 | 轻度优化，适合中等复杂度场景 |
| 6-10 | 强力优化，适合超复杂场景 |

---

### 5. 信号连接 - 详细示例

插件提供 **9个信号**，用于监控场景切换、预加载和缓存状态。

#### GDScript 版本
```gdscript
func _ready():
    # 连接所有场景管理器信号
    LongSceneManager.scene_switch_started.connect(_on_scene_switch_started)
    LongSceneManager.scene_switch_completed.connect(_on_scene_switch_completed)
    LongSceneManager.scene_preload_started.connect(_on_preload_started)
    LongSceneManager.scene_preload_completed.connect(_on_preload_completed)
    LongSceneManager.scene_preload_failed.connect(_on_preload_failed)
    LongSceneManager.scene_switch_failed.connect(_on_switch_failed)
    LongSceneManager.scene_cached.connect(_on_scene_cached)
    LongSceneManager.scene_removed_from_cache.connect(_on_scene_removed)
    LongSceneManager.load_screen_shown.connect(_on_load_screen_shown)
    LongSceneManager.load_screen_hidden.connect(_on_load_screen_hidden)

# 场景切换信号
func _on_scene_switch_started(from_scene: String, to_scene: String):
    print("场景切换开始: ", from_scene, " -> ", to_scene)

func _on_scene_switch_completed(scene_path: String):
    print("场景切换完成: ", scene_path)
    # 可以在这里更新UI、播放音效等

func _on_scene_switch_failed(scene_path: String):
    push_error("场景切换失败: ", scene_path)
    # 可以在这里显示错误提示

# 预加载信号
func _on_preload_started(scene_path: String):
    print("场景开始预加载: ", scene_path)

func _on_preload_completed(scene_path: String):
    print("场景预加载完成: ", scene_path)
    # 可以在这里更新UI，提示玩家可以切换了

func _on_preload_failed(scene_path: String):
    push_error("场景预加载失败: ", scene_path)

# 缓存信号
func _on_scene_cached(scene_path: String):
    print("场景已缓存: ", scene_path)

func _on_scene_removed(scene_path: String):
    print("场景从缓存移除: ", scene_path)

# 加载屏幕信号
func _on_load_screen_shown(load_screen_instance: Node):
    print("加载屏幕显示: ", load_screen_instance.name)

func _on_load_screen_hidden(load_screen_instance: Node):
    print("加载屏幕隐藏: ", load_screen_instance.name)
```

#### C# 版本
```csharp
public override void _Ready()
{
    var manager = (LongSceneManagerCs.LongSceneManagerCs)GetNode("/root/LongSceneManagerCs");
    
    // 连接所有场景管理器信号
    manager.SceneSwitchStarted += OnSceneSwitchStarted;
    manager.SceneSwitchCompleted += OnSceneSwitchCompleted;
    manager.ScenePreloadStarted += OnScenePreloadStarted;
    manager.ScenePreloadCompleted += OnScenePreloadCompleted;
    manager.ScenePreloadFailed += OnScenePreloadFailed;
    manager.SceneSwitchFailed += OnSceneSwitchFailed;
    manager.SceneCached += OnSceneCached;
    manager.SceneRemovedFromCache += OnSceneRemoved;
    manager.LoadScreenShown += OnLoadScreenShown;
    manager.LoadScreenHidden += OnLoadScreenHidden;
}

// 场景切换信号
private void OnSceneSwitchStarted(string fromScene, string toScene)
{
    GD.Print($"场景切换开始: {fromScene} -> {toScene}");
}

private void OnSceneSwitchCompleted(string scenePath)
{
    GD.Print($"场景切换完成: {scenePath}");
    // 可以在这里更新UI、播放音效等
}

private void OnSceneSwitchFailed(string scenePath)
{
    GD.PrintErr($"场景切换失败: {scenePath}");
    // 可以在这里显示错误提示
}

// 预加载信号
private void OnScenePreloadStarted(string scenePath)
{
    GD.Print($"场景开始预加载: {scenePath}");
}

private void OnScenePreloadCompleted(string scenePath)
{
    GD.Print($"场景预加载完成: {scenePath}");
    // 可以在这里更新UI，提示玩家可以切换了
}

private void OnScenePreloadFailed(string scenePath)
{
    GD.PrintErr($"场景预加载失败: {scenePath}");
}

// 缓存信号
private void OnSceneCached(string scenePath)
{
    GD.Print($"场景已缓存: {scenePath}");
}

private void OnSceneRemoved(string scenePath)
{
    GD.Print($"场景从缓存移除: {scenePath}");
}

// 加载屏幕信号
private void OnLoadScreenShown(Node loadScreenInstance)
{
    GD.Print($"加载屏幕显示: {loadScreenInstance.Name}");
}

private void OnLoadScreenHidden(Node loadScreenInstance)
{
    GD.Print($"加载屏幕隐藏: {loadScreenInstance.Name}");
}
```

**信号使用场景**：
- ✅ **场景切换开始**：显示"加载中..." UI
- ✅ **场景切换完成**：隐藏加载UI，播放进入音效
- ✅ **预加载完成**：启用场景切换按钮
- ✅ **场景缓存**：更新缓存状态显示
- ✅ **加载屏幕显示/隐藏**：同步其他系统状态

---

## 注意事项

### 1. 缓存行为

- **首次切换场景时不会使用缓存**（因为缓存是空的）
- **从缓存切换时，场景实例会**从缓存移除**，然后添加到场景树
- **场景进入缓存的时机**：当从场景树移除时，如果启用了缓存且未标记重置
- **LRU淘汰**：当缓存达到上限时，最久未使用的场景会被自动移除

### 2. 预加载注意事项

- **预加载是异步的**：调用 `preload_scene()` 后立即返回，不会阻塞
- **预加载完成后资源会进入预加载资源缓存**
- **如果预加载时场景已经在缓存中**：会输出警告并忽略请求
- **移除预加载资源时**：会同时清除预加载状态，允许重新预加载
- **C# 版本使用 `CacheMode.Ignore`**：确保每次都重新加载，避免使用 Godot 内部缓存

### 3. 加载屏幕注意事项

- **默认加载屏幕**：如果未指定加载屏幕路径，会使用默认的黑色加载屏幕
- **自定义加载屏幕**：需要是有效的 Godot 场景，应该实现 `fade_in`、`fade_out` 等方法
- **无过渡模式**：使用 `"no_transition"` 作为加载屏幕路径，会跳过加载屏幕显示
- **第一次切换无黑屏**：如果第一次切换时没有显示加载屏幕，请检查默认加载屏幕是否正确初始化

### 4. C# 版本特别说明

- **命名空间冲突**：命名空间和类名都是 `LongSceneManagerCs`，引用时需要写完整：`LongSceneManagerCs.LongSceneManagerCs`
- **GDScript 兼容方法**：使用 `SwitchSceneGD()` 和 `PreloadSceneGD()` 可以从 GDScript 调用 C# 版本
- **异步方法**：C# 版本支持真正的 `async/await`，可以使用 `SwitchScene()` 和 `PreloadScene()` 异步方法

### 5. 场景重置注意事项

- **标记重置后**：场景不会进入缓存，而是被释放并重新加载为预加载资源
- **只影响下一次切换**：场景重置标记只生效一次，之后会自动清除
- **可以用于强制刷新场景**：比如玩家死亡后需要重新加载关卡

---

## API 参考

### 场景切换方法

| 方法 | 描述 |
|------|------|
| `switch_scene(scene_path, use_cache=true, load_screen_path="")` | 切换到新场景，可选择使用缓存和自定义加载屏幕 |
| `switch_scene_gd(scene_path, use_cache=true, load_screen_path="")` | GDScript 兼容的场景切换包装器 |

### 预加载方法

| 方法 | 描述 |
|------|------|
| `preload_scene(scene_path)` | 将场景预加载到缓存中 |
| `preload_scene_gd(scene_path)` | GDScript 兼容的场景预加载包装器 |
| `preload_scenes(scene_paths)` | 批量预加载多个场景 |
| `cancel_preload_scene(scene_path)` | 取消场景预加载 |
| `cancel_all_preloads()` | 取消所有正在预加载的场景 |

### 缓存管理方法

| 方法 | 描述 |
|------|------|
| `clear_cache()` | 清除所有缓存的场景和预加载资源 |
| `get_cache_info()` | 获取当前缓存状态的详细信息 |
| `is_scene_cached(scene_path)` | 检查场景当前是否已缓存 |
| `set_max_cache_size(new_size)` | 设置可缓存场景的最大数量 |
| `set_max_preload_resource_cache_size(new_size)` | 设置可缓存的预加载资源的最大数量 |
| `remove_preloaded_resource(scene_path)` | 只移除预加载资源缓存 |
| `remove_cached_scene(scene_path)` | 只移除缓存的场景实例 |
| `get_loading_progress(scene_path)` | 获取场景的加载进度 (0.0 到 1.0) |

### 场景重置方法

| 方法 | 描述 |
|------|------|
| `mark_scene_for_reset(scene_path)` | 标记场景在下次切换时重置 |
| `unmark_scene_for_reset(scene_path)` | 取消标记场景在下次切换时重置 |

### 实用方法

| 方法 | 描述 |
|------|------|
| `get_current_scene()` | 获取当前场景实例 |
| `get_previous_scene_path()` | 获取上一个场景的路径 |
| `get_preloading_scenes()` | 获取当前正在预加载的场景列表 |
| `print_debug_info()` | 将调试信息打印到控制台 |

---

## 信号列表

| 信号 | 描述 |
|------|------|
| `scene_preload_started(scene_path)` | 场景预加载开始时发出 |
| `scene_preload_completed(scene_path)` | 场景预加载完成时发出 |
| `scene_preload_failed(scene_path)` | 场景预加载失败时发出 |
| `scene_switch_started(from_scene, to_scene)` | 场景切换开始时发出 |
| `scene_switch_completed(scene_path)` | 场景切换完成时发出 |
| `scene_switch_failed(scene_path)` | 场景切换失败时发出 |
| `scene_cached(scene_path)` | 场景添加到缓存时发出 |
| `scene_removed_from_cache(scene_path)` | 场景从缓存中移除时发出 |
| `load_screen_shown(load_screen_instance)` | 加载屏幕显示时发出 |
| `load_screen_hidden(load_screen_instance)` | 加载屏幕隐藏时发出 |

---

## 配置选项

插件暴露了几个可在编辑器中调整的配置选项：

| 选项 | 类型 | 默认值 | 范围 | 描述 |
|------|------|--------|------|------|
| `max_cache_size` | int | 8 | 1-20 | 要缓存的最大场景数 |
| `max_preload_resource_cache_size` | int | 20 | 1-50 | 要缓存的最大预加载资源数 |
| `use_async_loading` | bool | true | - | 是否使用异步加载 |
| `always_use_default_load_screen` | bool | false | - | 始终使用默认加载屏幕 |
| `instantiate_frames` | int | 3 | 1-10 | 跨多帧实例化场景的帧数 |

---

## 故障排除

### 场景预加载问题

如果您在清除缓存后遇到场景预加载问题，请确保您使用的是最新版本的插件，该版本在清除缓存时会正确重置加载状态。

**C# 版本特别说明**：C# 版本在预加载时使用 `ResourceLoader.CacheMode.Ignore`，确保每次都重新加载资源，避免使用 Godot 内部缓存。

### 缓存不工作

如果想利用缓存机制，请确保在切换场景时将 `use_cache` 参数设置为 `true`。

### 自定义加载屏幕

创建自定义加载屏幕时，请确保它们继承自有效的 Godot 节点类型，并在计划使用时实现预期的方法（如 `fade_in`、`fade_out` 等）。

### 第一次切换无黑屏

如果第一次切换场景时没有黑屏过渡，请检查：
1. 默认加载屏幕是否正确初始化（查看控制台输出）
2. `always_use_default_load_screen` 是否设置为 `false`
3. 加载屏幕实例是否为 `null`

### C# 版本编译错误

如果遇到编译错误，请检查：
1. 是否正确引用了 `LongSceneManagerCs` 命名空间
2. 是否使用了完整的类型名称 `LongSceneManagerCs.LongSceneManagerCs`
3. 是否所有必需的方法都存在（如 `RemovePreloadedResource`、`RemoveCachedScene` 等）

---

## 项目结构

```
addons/long_scene_manager/
├── plugin.cfg                    # 插件配置文件
├── autoload/
│   ├── long_scene_manager.gd    # GDScript 实现
│   └── LongSceneManagerCs.cs    # C# 实现
├── ui/
│   ├── loading_screen/
│   │   ├── GDScript/
│   │   │   └── loading_black_screen.tscn  # GDScript 默认加载屏幕
│   │   └── CSharp/
│   │       └── loading_black_screen_cs.tscn  # C# 默认加载屏幕
│   └── ...
├── image_icon/                  # 插件图标
└── ...

demo_test_scene_manager/         # 测试项目
├── main_scene.tscn
├── main_scene_script/
│   ├── main_scene_gd.gd
│   └── MainSceneCs.cs
├── test_scene_1.tscn
├── test_scene_1_script/
│   ├── test_scene_1_gd.gd
│   └── TestScene1Cs.cs
├── test_scene_2.tscn
├── test_scene_2_script/
│   ├── test_scene_2_gd.gd
│   └── TestScene2Cs.cs
└── ...
```

---

## 许可证

本插件使用 MIT 许可证。您可以自由使用、修改和分发本插件。

---

## 致谢

感谢所有为这个插件提供反馈和建议的开发者！

**特别感谢**：
- Godot 社区
- 所有给本插件 Star 的开发者
- 在游戏感谢名单中提到本插件作者的开发者

---

**最后更新**：2026年5月

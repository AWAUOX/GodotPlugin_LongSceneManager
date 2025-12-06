using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

namespace LongSceneManagerCs
{
	/// <summary>
	/// 全局场景管理器插件
	/// 
	/// 这是一个用于Godot游戏引擎的场景管理器，提供了以下核心功能:
	/// 1. 场景切换:可以在不同场景之间平滑切换
	/// 2. 自定义加载屏幕:支持在场景切换时显示加载界面
	/// 3. 预加载:提前加载场景资源以提高性能
	/// 4. LRU缓存:缓存最近使用的场景实例以减少重复加载
	/// 5. 场景树和缓存分离设计:场景实例要么在场景树中（当前活跃），要么在缓存中（非活跃但保留）
	/// 
	/// 使用方式:
	/// 在Godot项目中将其设为自动加载(AutoLoad)单例，然后通过调用SwitchScene等方法进行场景管理。
	/// 
	/// Global Scene Manager Plugin
	/// 
	/// This is a scene manager for the Godot game engine that provides the following core features:
	/// 1. Scene switching: Smooth switching between different scenes
	/// 2. Custom loading screens: Support for displaying loading interfaces during scene transitions
	/// 3. Preloading: Loading scene resources in advance to improve performance
	/// 4. LRU caching: Caching recently used scene instances to reduce repeated loading
	/// 5. Scene tree and cache separation design: Scene instances are either in the scene tree (currently active) or in the cache (inactive but retained)
	/// 
	/// Usage:
	/// Set it as an AutoLoad singleton in your Godot project, then manage scenes by calling methods like SwitchScene.
	/// </summary>
	public partial class LongSceneManagerCs : Node
	{
		#region Constants and Enums
		// 常量和枚举
		
		// 默认加载屏幕的资源路径
		// Default loading screen resource path
		public const string DefaultLoadScreenPath = "res://addons/long_scene_manager/ui/loading_screen/CSharp/loading_black_screen_cs.tscn";
		
		// 场景加载状态枚举
		// Scene loading state enumeration
		public enum LoadState
		{
			NotLoaded,      // 未加载 // Not loaded
			Loading,        // 正在加载中 // Loading in progress
			Loaded,         // 已加载（资源已加载但未实例化） // Loaded (resource loaded but not instantiated)
			Instantiated    // 已实例化（场景对象已创建） // Instantiated (scene object created)
		}
		
		#endregion
		
		#region Signal Definitions
		// 信号定义
		
		// 预加载开始信号
		// Preloading started signal
		[Signal] public delegate void ScenePreloadStartedEventHandler(string scenePath);
		
		// 预加载完成信号
		// Preloading completed signal
		[Signal] public delegate void ScenePreloadCompletedEventHandler(string scenePath);
		
		// 场景切换开始信号
		// Scene switching started signal
		[Signal] public delegate void SceneSwitchStartedEventHandler(string fromScene, string toScene);
		
		// 场景切换完成信号
		// Scene switching completed signal
		[Signal] public delegate void SceneSwitchCompletedEventHandler(string scenePath);
		
		// 场景被缓存信号
		// Scene cached signal
		[Signal] public delegate void SceneCachedEventHandler(string scenePath);
		
		// 场景从缓存中移除信号
		// Scene removed from cache signal
		[Signal] public delegate void SceneRemovedFromCacheEventHandler(string scenePath);
		
		// 加载屏幕显示信号
		// Loading screen shown signal
		[Signal] public delegate void LoadScreenShownEventHandler(Node loadScreenInstance);
		
		// 加载屏幕隐藏信号
		// Loading screen hidden signal
		[Signal] public delegate void LoadScreenHiddenEventHandler(Node loadScreenInstance);
		
		#endregion
		
		#region Export Variables
		// 导出变量
		
		// 在Godot编辑器中显示的分类标题
		// Category title displayed in Godot editor
		[ExportCategory("场景管理器全局配置")]
		// Scene Manager Global Configuration
		
		// 导出变量，允许在编辑器中设置，限制范围为1-20
		// Export variable, allows setting in editor, range limited to 1-20
		[Export(PropertyHint.Range, "1,20")] 
		private int _maxCacheSize = 8;     // 最大缓存场景数量，默认为8个
		                               // Maximum number of cached scenes, default is 8
		
		// 导出变量，允许在编辑器中设置预加载资源缓存的最大容量，限制范围为1-50
		// Export variable, allows setting maximum capacity of preload resource cache in editor, range limited to 1-50
		[Export(PropertyHint.Range, "1,50")]
		private int _maxPreloadResourceCacheSize = 20; // 预加载资源缓存最大容量，默认为20个
		                                           // Maximum preload resource cache capacity, default is 20
		
		// 导出布尔值变量，可在编辑器中设置
		// Export boolean variable, can be set in editor
		[Export] 
		private bool _useAsyncLoading = true;  // 是否使用异步加载，默认开启
		                                   // Whether to use asynchronous loading, enabled by default
		
		// 总是使用默认加载屏幕
		// Always use default loading screen
		[Export] 
		private bool _alwaysUseDefaultLoadScreen = false;

		//静态实例引用
		// Static instance reference
		private static LongSceneManagerCs _instance;
		
		#endregion
		
		#region Internal State Variables
		// 内部状态变量
		
		private Node _currentScene;                     // 当前场景实例 // Current scene instance
		private string _currentScenePath = "";          // 当前场景路径 // Current scene path
		private string _previousScenePath = "";         // 上一个场景路径 // Previous scene path
		
		private Node _defaultLoadScreen;                // 默认加载屏幕实例 // Default loading screen instance
		private Node _activeLoadScreen;                 // 当前激活的加载屏幕实例 // Currently active loading screen instance
		
		private string _loadingScenePath = "";          // 正在加载的场景路径 // Path of the scene currently being loaded
		private LoadState _loadingState = LoadState.NotLoaded;  // 当前加载状态 // Current loading state
		private PackedScene _loadingResource;           // 正在加载的场景资源 // Scene resource currently being loaded
		
		// 存储从场景树移除的节点实例（场景缓存）
		// Store node instances removed from the scene tree (scene cache)
		private readonly System.Collections.Generic.Dictionary<string, CachedScene> _sceneCache = new();
		
		// LRU缓存访问顺序记录（最近最少使用算法）
		// LRU cache access order record (Least Recently Used algorithm)
		private readonly List<string> _cacheAccessOrder = new();
		
		// 预加载资源缓存，存储预加载的PackedScene资源
		// Preload resource cache, stores preloaded PackedScene resources
		private readonly System.Collections.Generic.Dictionary<string, PackedScene> _preloadResourceCache = new();
		
		// 预加载资源缓存的LRU访问顺序记录
		// LRU access order record for preload resource cache
		private readonly List<string> _preloadResourceCacheAccessOrder = new();
		
		#endregion
		
		#region Lifecycle Functions
		// 生命周期函数
		
		// Godot节点生命周期函数，在节点添加到场景树后调用一次
		// Godot node lifecycle function, called once after node is added to the scene tree
		public override void _Ready()
		{
			GD.Print("[SceneManager] Scene manager singleton initialized");
			// Scene manager singleton initialization
			
			//初始化静态实例引用
			// Initialize static instance reference
			_instance = this;

			// 初始化默认加载屏幕
			// Initialize default loading screen
			InitDefaultLoadScreen();
			
			// 获取当前场景
			// Get current scene
			_currentScene = GetTree().CurrentScene;
			if (_currentScene != null)
			{
				_currentScenePath = _currentScene.SceneFilePath;
				GD.Print($"[SceneManager] Current scene: {_currentScenePath}");
				// Current scene:
			}
			
			GD.Print($"[SceneManager] Initialization complete, max cache: {_maxCacheSize}");
			// Initialization complete, max cache:
		}
		
		#endregion



		#region Scene Instance Reference
		// 场景实例引用
		/// <summary>
		/// 初始化场景实例引用
        /// 
		/// Initialize scene instance reference
        /// 
		/// </summary>
		public static LongSceneManagerCs Instance => _instance;
		// #endregion 场景实例引用
		#endregion Scene Instance Reference



		
		#region Initialization Functions
		// 初始化函数
		
		/// <summary>
		/// 初始化默认加载屏幕
		/// 尝试加载预设的加载屏幕，如果不存在则创建一个简单的黑色屏幕
		/// 
		/// Initialize default loading screen
		/// Try to load the preset loading screen, if it doesn't exist, create a simple black screen
		/// </summary>
		private void InitDefaultLoadScreen()
		{
			GD.Print("[SceneManager] Initializing default loading screen");
			// Initializing default loading screen
			
			// 检查默认加载屏幕资源是否存在
			// Check if default loading screen resource exists
			if (ResourceLoader.Exists(DefaultLoadScreenPath))
			{
				// 加载默认加载屏幕场景
				// Load default loading screen scene
				var loadScreenScene = ResourceLoader.Load<PackedScene>(DefaultLoadScreenPath);
				if (loadScreenScene != null)
				{
					// 实例化并添加到场景管理器中
					// Instantiate and add to scene manager
					_defaultLoadScreen = loadScreenScene.Instantiate();
					AddChild(_defaultLoadScreen);
					
					// 设置加载屏幕初始为不可见
					// Set loading screen initially invisible
					if (_defaultLoadScreen is CanvasItem canvasItem)
					{
						canvasItem.Visible = false;
					}
					else if (_defaultLoadScreen.HasMethod("set_visible"))
					{
						_defaultLoadScreen.Call("set_visible", false);
					}
					
					GD.Print("[SceneManager] Default loading screen loaded successfully");
					// Default loading screen loaded successfully
					return;
				}
			}
			
			// 如果默认加载屏幕不存在，则创建一个简单的纯色加载屏幕
			// If default loading screen doesn't exist, create a simple solid color loading screen
			GD.Print("[SceneManager] Warning: Default loading screen file does not exist, creating simple version");
			// Warning: Default loading screen file does not exist, creating simple version
			_defaultLoadScreen = CreateSimpleLoadScreen();
			AddChild(_defaultLoadScreen);
			
			// 设置简单加载屏幕初始为不可见
			// Set simple loading screen initially invisible
			if (_defaultLoadScreen is CanvasItem defaultCanvasItem)
			{
				defaultCanvasItem.Visible = false;
			}
			else if (_defaultLoadScreen.HasMethod("set_visible"))
			{
				_defaultLoadScreen.Call("set_visible", false);
			}
			
			GD.Print("[SceneManager] Simple loading screen creation completed");
			// Simple loading screen creation completed
		}
		
		/// <summary>
		/// 创建一个简单的加载屏幕（纯黑背景加"Loading..."文本）
		/// 当默认加载屏幕不存在时使用
		/// 
		/// Create a simple loading screen (solid black background with "Loading..." text)
		/// Used when default loading screen doesn't exist
		/// </summary>
		/// <returns>创建的简单加载屏幕节点 // Created simple loading screen node</returns>
		private Node CreateSimpleLoadScreen()
		{
			// 创建CanvasLayer作为容器，确保加载屏幕显示在最上层
			// Create CanvasLayer as container to ensure loading screen displays on top
			var canvasLayer = new CanvasLayer();
			canvasLayer.Name = "SimpleLoadScreen";
			canvasLayer.Layer = 1000;  // 设置层级为1000，确保显示在最前面
			                       // Set layer to 1000 to ensure it displays in front
			
			// 创建全屏黑色矩形
			// Create full-screen black rectangle
			var colorRect = new ColorRect();
			colorRect.Color = new Color(0, 0, 0, 1);  // 纯黑色不透明 // Pure black opaque
			colorRect.Size = GetViewport().GetVisibleRect().Size;  // 设置为视口大小 // Set to viewport size
			colorRect.AnchorLeft = 0;
			colorRect.AnchorTop = 0;
			colorRect.AnchorRight = 1;
			colorRect.AnchorBottom = 1;
			colorRect.MouseFilter = Control.MouseFilterEnum.Stop;  // 阻止鼠标事件穿透 // Prevent mouse events from passing through
			
			// 创建"Loading..."标签
			// Create "Loading..." label
			var label = new Label();
			label.Text = "Loading...";
			label.HorizontalAlignment = HorizontalAlignment.Center;  // 水平居中 // Horizontal center
			label.VerticalAlignment = VerticalAlignment.Center;      // 垂直居中 // Vertical center
			label.AddThemeFontSizeOverride("font_size", 32);         // 字体大小32 // Font size 32
			label.AddThemeColorOverride("font_color", Colors.White); // 白色字体 // White font
			
			// 组装UI层次结构
			// Assemble UI hierarchy
			canvasLayer.AddChild(colorRect);
			colorRect.AddChild(label);
			
			// 精确定位标签到屏幕中心
			// Precisely position label to screen center
			label.AnchorLeft = 0.5f;
			label.AnchorTop = 0.5f;
			label.AnchorRight = 0.5f;
			label.AnchorBottom = 0.5f;
			label.Position = new Vector2(-50, -16);  // 微调位置 // Fine-tune position
			label.Size = new Vector2(100, 32);
			
			return canvasLayer;
		}
		
		#endregion
		
		#region Public API - Scene Switching
		// 公开API - 场景切换
		
		/// <summary>
		/// 切换到指定场景（异步方法，供C#代码使用）
		/// 
		/// Switch to specified scene (async method, for C# code use)
		/// </summary>
		/// <param name="newScenePath">要切换到的新场景路径 // New scene path to switch to</param>
		/// <param name="useCache">是否使用缓存机制，默认为true // Whether to use caching mechanism, default is true</param>
		/// <param name="loadScreenPath">自定义加载屏幕路径，为空则使用默认加载屏幕 // Custom loading screen path, empty to use default loading screen</param>
		/// <returns>异步任务 // Async task</returns>
		public async Task SwitchScene(string newScenePath, bool useCache = true, string loadScreenPath = "")
		{
			GD.Print($"[SceneManager] Start switching scene to: {newScenePath}");
			// Starting to switch scene to:
			
			// 添加场景树验证，确保状态清晰
			// Add scene tree validation to ensure clear state
			DebugValidateSceneTree();
			
			// 如果设置了总是使用默认加载屏幕，则忽略自定义加载屏幕
			// If always use default loading screen is set, ignore custom loading screen
			if (_alwaysUseDefaultLoadScreen)
			{
				loadScreenPath = "";
				GD.Print("[SceneManager] Force using default loading screen");
				// Forced to use default loading screen
			}
			
			// 检查目标场景是否存在
			// Check if target scene exists
			if (!ResourceLoader.Exists(newScenePath))
			{
				GD.PrintErr($"[SceneManager] Error: Target scene path does not exist: {newScenePath}");
				// Error: Target scene path does not exist:
				return;
			}
			
			// 发送场景切换开始信号
			// Send scene switching started signal
			EmitSignal(SignalName.SceneSwitchStarted, _currentScenePath, newScenePath);
			
			// 如果目标场景就是当前场景，则无需切换
			// If target scene is the current scene, no need to switch
			if (_currentScenePath == newScenePath)
			{
				GD.Print($"[SceneManager] Scene already loaded: {newScenePath}");
				// Scene already loaded:
				EmitSignal(SignalName.SceneSwitchCompleted, newScenePath);
				return;
			}
			
			// 获取加载屏幕实例
			// Get loading screen instance
			var loadScreenToUse = GetLoadScreenInstance(loadScreenPath);
			if (loadScreenPath != "no_transition" && loadScreenToUse == null)
			{
				GD.PrintErr("[SceneManager] Error: Unable to get loading screen, switching aborted");
				// Error: Cannot get loading screen, switching aborted
				return;
			}
			
			// 检查预加载资源缓存
			// Check preload resource cache
			if (_preloadResourceCache.ContainsKey(newScenePath))
			{
				GD.Print($"[SceneManager] Using preload resource cache: {newScenePath}");
				// Using preload resource cache:
				await HandlePreloadedResource(newScenePath, loadScreenToUse, useCache);
				return;
			}
			
			// 如果场景正在预加载中，则等待预加载完成
			// If scene is preloading, wait for preload to complete
			if (_loadingScenePath == newScenePath && _loadingState == LoadState.Loading)
			{
				GD.Print("[SceneManager] Scene is preloading, waiting for completion...");
				// Scene is preloading, waiting for completion...
				await HandlePreloadingScene(newScenePath, loadScreenToUse, useCache);
				return;
			}
			
			// 如果启用了缓存并且场景在实例缓存中，则使用缓存的场景实例
			// If caching is enabled and scene is in instance cache, use cached scene instance
			if (useCache && _sceneCache.ContainsKey(newScenePath))
			{
				GD.Print($"[SceneManager] Loading scene from instance cache: {newScenePath}");
				// Loading scene from instance cache:
				await HandleCachedScene(newScenePath, loadScreenToUse);
				return;
			}
			
			// 直接加载场景（没有使用任何优化）
			// Directly load scene (without using any optimizations)
			GD.Print($"[SceneManager] Directly loading scene: {newScenePath}");
			// Directly loading scene:
			await HandleDirectLoad(newScenePath, loadScreenToUse, useCache);
		}
		
		/// <summary>
		/// 专供GDScript调用的场景切换方法（非异步包装）
		/// 
		/// Scene switching method specifically for GDScript calls (non-async wrapper)
		/// </summary>
		/// <param name="newScenePath">要切换到的新场景路径 // New scene path to switch to</param>
		/// <param name="useCache">是否使用缓存机制，默认为true // Whether to use caching mechanism, default is true</param>
		/// <param name="loadScreenPath">自定义加载屏幕路径，为空则使用默认加载屏幕 // Custom loading screen path, empty to use default loading screen</param>
		public void SwitchSceneGD(string newScenePath, bool useCache = true, string loadScreenPath = "")
		{
			// 调用异步方法，但不等待其结果
			// Call async method but don't wait for its result
			_ = SwitchScene(newScenePath, useCache, loadScreenPath);
		}
		
		#endregion
		
		#region Public API - Preloading
		// 公开API - 预加载
		
		/// <summary>
		/// 预加载指定场景（异步方法，供C#代码使用）
		/// 
		/// Preload specified scene (async method, for C# code use)
		/// </summary>
		/// <param name="scenePath">要预加载的场景路径 // Scene path to preload</param>
		/// <returns>异步任务 // Async task</returns>
		public async Task PreloadScene(string scenePath)
		{
			// 检查场景路径是否存在
			// Check if scene path exists
			if (!ResourceLoader.Exists(scenePath))
			{
				GD.PrintErr($"[SceneManager] Error: Preload scene path does not exist: {scenePath}");
				// Error: Preload scene path does not exist:
				return;
			}
			
			// 检查是否已预加载或已缓存，避免重复加载
			// Check if already preloaded or cached to avoid duplicate loading
			if (_preloadResourceCache.ContainsKey(scenePath))
			{
				GD.Print($"[SceneManager] Scene already preloaded: {scenePath}");
				// Scene already preloaded:
				// 更新LRU访问顺序
				// Update LRU access order
				UpdatePreloadResourceCacheAccess(scenePath);
				return;
			}
			
			// 检查场景是否正在加载或已经加载到实例缓存中
			// Check if scene is loading or already loaded into instance cache
			if ((_loadingScenePath == scenePath && _loadingState == LoadState.Loading) ||
				(_loadingScenePath == scenePath && _loadingState == LoadState.Loaded) ||
				_sceneCache.ContainsKey(scenePath))
			{
				GD.Print($"[SceneManager] Scene already loaded or loading: {scenePath}");
				// Scene already loaded or loading:
				return;
			}
			
			GD.Print($"[SceneManager] Start preloading scene: {scenePath}");
			// Starting to preload scene:
			// 发送预加载开始信号
			// Send preloading started signal
			EmitSignal(SignalName.ScenePreloadStarted, scenePath);
			
			// 设置当前正在加载的场景信息
			// Set current scene loading information
			_loadingScenePath = scenePath;
			_loadingState = LoadState.Loading;
			
			// 根据设置决定使用异步还是同步方式进行预加载
			// Decide whether to use async or sync method for preloading based on settings
			if (_useAsyncLoading)
			{
				await AsyncPreloadScene(scenePath);
			}
			else
			{
				SyncPreloadScene(scenePath);
			}
			
			// 如果预加载成功，则将资源放入预加载资源缓存中
			// If preload succeeds, put resource into preload resource cache
			if (_loadingResource != null)
			{
				// 预加载完成后，将资源存入预加载资源缓存
				// After preloading completes, store resource in preload resource cache
				_preloadResourceCache[scenePath] = _loadingResource;
				_preloadResourceCacheAccessOrder.Add(scenePath);
				_loadingState = LoadState.Loaded;
				// 发送预加载完成信号
				// Send preloading completed signal
				EmitSignal(SignalName.ScenePreloadCompleted, scenePath);
				GD.Print($"[SceneManager] Preloading complete, resource cached: {scenePath}");
				// Preloading complete, resource cached:
				
				// 如果预加载资源缓存数量超过最大限制，则移除最旧的缓存项
				// If preload resource cache count exceeds maximum limit, remove oldest cache item
				if (_preloadResourceCacheAccessOrder.Count > _maxPreloadResourceCacheSize)
				{
					RemoveOldestPreloadResource();
				}
			}
			else
			{
				// 预加载失败，重置加载状态
				// Preloading failed, reset loading state
				_loadingState = LoadState.NotLoaded;
				_loadingScenePath = "";
				GD.Print($"[SceneManager] Preloading failed: {scenePath}");
				// Preloading failed:
			}
		}
		
		/// <summary>
		/// 专供GDScript调用的预加载场景方法（非异步包装）
		/// 
		/// Preload scene method specifically for GDScript calls (non-async wrapper)
		/// </summary>
		/// <param name="scenePath">要预加载的场景路径 // Scene path to preload</param>
		public void PreloadSceneGD(string scenePath)
		{
			// 调用异步方法，但不等待其结果
			// Call async method but don't wait for its result
			_ = PreloadScene(scenePath);
		}
		
		#endregion
		
		#region Public API - Cache Management
		// 公开API - 缓存管理
		
		/// <summary>
		/// 清空所有缓存
		/// 包括预加载资源缓存和实例缓存
		/// 
		/// Clear all caches
		/// Including preload resource cache and instance cache
		/// </summary>
		public void ClearCache()
		{
			GD.Print("[SceneManager] Clearing cache...");
			// Clearing cache...
			
			// 清理预加载资源缓存（存储的是PackedScene资源）
			// Clean up preload resource cache (stores PackedScene resources)
			_preloadResourceCache.Clear();
			_preloadResourceCacheAccessOrder.Clear();
			GD.Print("[SceneManager] Preload resource cache cleared");
			// Preload resource cache cleared
			
			// 清理实例缓存（存储的是已实例化的场景节点）
			// Clean up instance cache (stores instantiated scene nodes)
			var toRemove = new List<string>();
			foreach (var kvp in _sceneCache)
			{
				var scenePath = kvp.Key;
				var cached = kvp.Value;
				// 检查缓存的场景实例是否仍然有效
				// Check if cached scene instance is still valid
				if (IsInstanceValid(cached.SceneInstance))
				{
					CleanupOrphanedNodes(cached.SceneInstance);  // 清理孤立节点 // Clean up orphaned nodes
					cached.SceneInstance.QueueFree();  // 释放场景实例 // Free scene instance
				}
				toRemove.Add(scenePath);
				// 发送场景从缓存移除信号
				// Send scene removed from cache signal
				EmitSignal(SignalName.SceneRemovedFromCache, scenePath);
			}
			
			// 从缓存字典中移除所有条目
			// Remove all entries from cache dictionary
			foreach (var scenePath in toRemove)
			{
				_sceneCache.Remove(scenePath);
				var index = _cacheAccessOrder.IndexOf(scenePath);
				if (index != -1)
				{
					_cacheAccessOrder.RemoveAt(index);
				}
			}
			
			// 重置加载状态，确保可以重新预加载场景
			// Reset loading state to ensure scenes can be preloaded again
			_loadingScenePath = "";
			_loadingState = LoadState.NotLoaded;
			_loadingResource = null;
			
			GD.Print("[SceneManager] Cache cleared");
			// Cache cleared
		}
		
		/// <summary>
		/// 获取缓存信息
		/// 返回关于当前缓存状态的详细信息
		/// 
		/// Get cache information
		/// Return detailed information about current cache state
		/// </summary>
		/// <returns>包含缓存信息的字典 // Dictionary containing cache information</returns>
		public Godot.Collections.Dictionary<string, Variant> GetCacheInfo()
		{
			// 构建实例缓存信息列表
			// Build instance cache information list
			var cachedScenes = new Array<Godot.Collections.Dictionary<string, Variant>>();
			foreach (var kvp in _sceneCache)
			{
				var path = kvp.Key;
				var cached = kvp.Value;
				var dict = new Godot.Collections.Dictionary<string, Variant>();
				dict.Add("path", path);                    // 场景路径 // Scene path
				dict.Add("access_count", cached.AccessCount);  // 访问次数 // Access count
				dict.Add("cached_time", cached.CachedTime);    // 缓存时间 // Cache time
				dict.Add("instance_valid", IsInstanceValid(cached.SceneInstance)); // 实例是否有效 // Instance validity
				cachedScenes.Add(dict);
			}
			
			// 构建预加载资源缓存路径列表
			// Build preload resource cache path list
			var preloadedScenes = new Array<string>();
			foreach (var path in _preloadResourceCache.Keys)
			{
				preloadedScenes.Add(path);
			}
			
			// 构建结果字典
			// Build result dictionary
			var result = new Godot.Collections.Dictionary<string, Variant>();
			result.Add("instance_cache_size", _sceneCache.Count);       // 实例缓存大小 // Instance cache size
			result.Add("max_size", _maxCacheSize);                      // 最大缓存大小 // Maximum cache size
			result.Add("access_order", _cacheAccessOrder.ToArray());    // 访问顺序 // Access order
			result.Add("cached_scenes", cachedScenes);                  // 缓存的场景详情 // Cached scene details
			result.Add("preload_resource_cache", preloadedScenes);      // 预加载资源缓存 // Preload resource cache
			result.Add("preload_cache_size", _preloadResourceCache.Count); // 预加载缓存大小 // Preload cache size
			result.Add("max_preload_resource_cache_size", _maxPreloadResourceCacheSize); // 预加载缓存最大大小 // Preload cache maximum size
			result.Add("preload_resource_access_order", _preloadResourceCacheAccessOrder.ToArray()); // 预加载资源访问顺序 // Preload resource access order
			
			return result;
		}
		
		/// <summary>
		/// 检查指定场景是否在缓存中
		/// 
		/// Check if specified scene is in cache
		/// </summary>
		/// <param name="scenePath">场景路径 // Scene path</param>
		/// <returns>如果场景在缓存中返回true，否则返回false // Returns true if scene is in cache, otherwise false</returns>
		public bool IsSceneCached(string scenePath)
		{
			// 检查场景是否在实例缓存或预加载资源缓存中
			// Check if scene is in instance cache or preload resource cache
			return _sceneCache.ContainsKey(scenePath) || _preloadResourceCache.ContainsKey(scenePath);
		}
		
		#endregion
		
		#region Public API - Utility Functions
		// 公开API - 实用函数
		
		/// <summary>
		/// 获取当前场景实例
		/// 
		/// Get current scene instance
		/// </summary>
		/// <returns>当前场景节点 // Current scene node</returns>
		public Node GetCurrentScene() => _currentScene;
		
		/// <summary>
		/// 获取上一个场景路径
		/// 
		/// Get previous scene path
		/// </summary>
		/// <returns>上一个场景的路径 // Path of previous scene</returns>
		public string GetPreviousScenePath() => _previousScenePath;
		
		/// <summary>
		/// 获取指定场景的加载进度
		/// 
		/// Get loading progress of specified scene
		/// </summary>
		/// <param name="scenePath">场景路径 // Scene path</param>
		/// <returns>加载进度(0.0-1.0)，如果场景已加载完成则返回1.0 // Loading progress (0.0-1.0), returns 1.0 if scene loading is complete</returns>
		public float GetLoadingProgress(string scenePath)
		{
			// 如果不是正在加载该场景，则检查是否已缓存
			// If not loading this scene, check if it's cached
			if (_loadingScenePath != scenePath || _loadingState != LoadState.Loading)
			{
				return (_sceneCache.ContainsKey(scenePath) || _preloadResourceCache.ContainsKey(scenePath)) ? 1.0f : 0.0f;
			}
			
			// 创建用于接收进度信息的数组
			// Create array to receive progress information
			Godot.Collections.Array progressArray = new();
			// 获取加载状态和进度
			// Get loading status and progress
			var status = ResourceLoader.LoadThreadedGetStatus(scenePath, progressArray);
			// 如果正在加载中且有进度信息，则返回进度值
			// If loading in progress and has progress info, return progress value
			if (status == ResourceLoader.ThreadLoadStatus.InProgress && progressArray.Count > 0)
			{
				return (float)progressArray[0];
			}
			
			return 0.0f;
		}
		
		/// <summary>
		/// 设置最大缓存大小
		/// 
		/// Set maximum cache size
		/// </summary>
		/// <param name="newSize">新的最大缓存大小 // New maximum cache size</param>
		public void SetMaxCacheSize(int newSize)
		{
			// 检查输入值有效性
			// Check input value validity
			if (newSize < 1)
			{
				GD.PrintErr("[SceneManager] Error: Cache size must be greater than 0");
				// Error: Cache size must be greater than 0
				return;
			}
			
			_maxCacheSize = newSize;
			GD.Print($"[SceneManager] Setting maximum cache size: {_maxCacheSize}");
			// Setting maximum cache size:
			
			// 如果当前缓存数量超过新设定的最大值，则移除最旧的缓存项
			// If current cache count exceeds newly set maximum, remove oldest cache items
			while (_cacheAccessOrder.Count > _maxCacheSize)
			{
				RemoveOldestCachedScene();
			}
		}
		
		/// <summary>
		/// 设置预加载资源缓存最大大小
		/// 
		/// Set preload resource cache maximum size
		/// </summary>
		/// <param name="newSize">新的预加载资源缓存最大大小 // New preload resource cache maximum size</param>
		public void SetMaxPreloadResourceCacheSize(int newSize)
		{
			// 检查输入值有效性
			// Check input value validity
			if (newSize < 1)
			{
				GD.PrintErr("[SceneManager] Error: Preload resource cache size must be greater than 0");
				// Error: Preload resource cache size must be greater than 0
				return;
			}
			
			_maxPreloadResourceCacheSize = newSize;
			GD.Print($"[SceneManager] Setting maximum preload resource cache size: {_maxPreloadResourceCacheSize}");
			// Setting maximum preload resource cache size:
			
			// 如果当前预加载资源缓存数量超过新设定的最大值，则移除最旧的缓存项
			// If current preload resource cache count exceeds newly set maximum, remove oldest cache items
			while (_preloadResourceCacheAccessOrder.Count > _maxPreloadResourceCacheSize)
			{
				RemoveOldestPreloadResource();
			}
		}
		
		#endregion
		
		#region Loading Screen Management
		// 加载屏幕管理
		
		/// <summary>
		/// 获取加载屏幕实例
		/// 根据传入的路径参数决定使用哪种加载屏幕
		/// 
		/// Get loading screen instance
		/// Decide which loading screen to use based on passed path parameter
		/// </summary>
		/// <param name="loadScreenPath">加载屏幕路径，空字符串表示使用默认加载屏幕，"no_transition"表示无过渡 // Loading screen path, empty string means use default loading screen, "no_transition" means no transition</param>
		/// <returns>加载屏幕节点实例，如果无法获取则返回null // Loading screen node instance, returns null if unable to get</returns>
		private Node GetLoadScreenInstance(string loadScreenPath)
		{
			// 如果路径为空，则使用默认加载屏幕
			// If path is empty, use default loading screen
			if (string.IsNullOrEmpty(loadScreenPath))
			{
				if (_defaultLoadScreen != null)
				{
					GD.Print("[SceneManager] Using default loading screen");
					// Using default loading screen
					return _defaultLoadScreen;
				}
				else
				{
					GD.PrintErr("[SceneManager] Error: Default loading screen not initialized");
					// Error: Default loading screen not initialized
					return null;
				}
			}
			// 如果指定为无过渡，则不使用加载屏幕
			// If specified as no transition, don't use loading screen
			else if (loadScreenPath == "no_transition")
			{
				GD.Print("[SceneManager] Using no transition mode");
				// Using no transition mode
				return null;
			}
			// 使用自定义加载屏幕
			// Use custom loading screen
			else
			{
				// 检查自定义加载屏幕资源是否存在
				// Check if custom loading screen resource exists
				if (ResourceLoader.Exists(loadScreenPath))
				{
					// 加载并实例化自定义加载屏幕
					// Load and instantiate custom loading screen
					var customScene = ResourceLoader.Load<PackedScene>(loadScreenPath);
					if (customScene != null)
					{
						var instance = customScene.Instantiate();
						AddChild(instance);
						GD.Print($"[SceneManager] Using custom loading screen: {loadScreenPath}");
						// Using custom loading screen:
						return instance;
					}
					else
					{
						GD.Print("[SceneManager] Warning: Custom loading screen failed to load, using default");
						// Warning: Custom loading screen failed to load, using default
						return _defaultLoadScreen;
					}
				}
				else
				{
					GD.Print("[SceneManager] Warning: Custom loading screen path does not exist, using default");
					// Warning: Custom loading screen path does not exist, using default
					return _defaultLoadScreen;
				}
			}
		}
		
		/// <summary>
		/// 显示加载屏幕
		/// 根据加载屏幕类型调用相应的显示方法
		/// 
		/// Show loading screen
		/// Call corresponding display method based on loading screen type
		/// </summary>
		/// <param name="loadScreenInstance">加载屏幕实例 // Loading screen instance</param>
		/// <returns>异步任务 // Async task</returns>
		private async Task ShowLoadScreen(Node loadScreenInstance)
		{
			// 如果没有加载屏幕，则直接返回
			// If no loading screen, return directly
			if (loadScreenInstance == null)
			{
				GD.Print("[SceneManager] No loading screen, switching directly");
				// No loading screen, switching directly
				return;
			}
			
			// 设置当前激活的加载屏幕
			// Set currently active loading screen
			_activeLoadScreen = loadScreenInstance;
			
			// 显示加载屏幕（根据不同的节点类型采用不同的显示方式）
			// Show loading screen (use different display methods based on node type)
			if (loadScreenInstance is CanvasItem canvasItem)
			{
				canvasItem.Visible = true;
			}
			else if (loadScreenInstance.HasMethod("set_visible"))
			{
				loadScreenInstance.Call("set_visible", true);
			}
			else if (loadScreenInstance.HasMethod("show"))
			{
				loadScreenInstance.Call("show");
			}
			
			// 如果加载屏幕有淡入效果方法，则调用它
			// If loading screen has fade-in effect method, call it
			if (loadScreenInstance.HasMethod("FadeIn"))
			{
				GD.Print("[SceneManager] Calling loading screen fade-in effect");
				// Calling loading screen fade-in effect
				try 
				{
					var result = loadScreenInstance.Call("FadeIn");
					// 首先检查是否有completed信号（兼容性处理）
					// First check if there's a completed signal (compatibility handling)
					if (result.AsGodotObject() != null && result.AsGodotObject().HasSignal("completed"))
					{
						await ToSignal(result.AsGodotObject(), "completed");
					}
					// 如果没有completed信号，尝试连接fade_in_completed信号
					// If no completed signal, try connecting to fade_in_completed signal
					else if (loadScreenInstance.HasSignal("fade_in_completed"))
					{
						await ToSignal(loadScreenInstance, "fade_in_completed");
					}
				}
				catch (Exception e)
				{
					GD.PrintErr($"[SceneManager] Error calling fade_in: {e.Message}");
					// Error occurred when calling fade_in:
				}
			}
			// 如果有show_loading方法，则调用它
			// If has show_loading method, call it
			else if (loadScreenInstance.HasMethod("show_loading"))
			{
				try
				{
					var result = loadScreenInstance.Call("show_loading");
					// 如果show_loading方法返回了带有completed信号的对象，则等待该信号
					// If show_loading method returns object with completed signal, wait for that signal
					if (result.AsGodotObject() != null && result.AsGodotObject().HasSignal("completed"))
					{
						await ToSignal(result.AsGodotObject(), "completed");
					}
				}
				catch (Exception e)
				{
					GD.PrintErr($"[SceneManager] Error calling show_loading: {e.Message}");
					// Error occurred when calling show_loading:
				}
			}
			
			// 发送加载屏幕显示信号
			// Send loading screen shown signal
			EmitSignal(SignalName.LoadScreenShown, loadScreenInstance);
			GD.Print("[SceneManager] Loading screen display completed");
			// Loading screen display completed
		}
		
		/// <summary>
		/// 隐藏加载屏幕
		/// 根据加载屏幕类型调用相应的隐藏方法
		/// 
		/// Hide loading screen
		/// Call corresponding hide method based on loading screen type
		/// </summary>
		/// <param name="loadScreenInstance">加载屏幕实例 // Loading screen instance</param>
		/// <returns>异步任务 // Async task</returns>
		private async Task HideLoadScreen(Node loadScreenInstance)
		{
			// 如果没有加载屏幕，则直接返回
			// If no loading screen, return directly
			if (loadScreenInstance == null)
			{
				return;
			}
			
			// 如果加载屏幕有淡出效果方法，则调用它
			// If loading screen has fade-out effect method, call it
			if (loadScreenInstance.HasMethod("FadeOut"))
			{
				GD.Print("[SceneManager] Calling loading screen fade-out effect");
				// Calling loading screen fade-out effect
				try
				{
					var result = loadScreenInstance.Call("FadeOut");
					// 首先检查是否有completed信号（兼容性处理）
					// First check if there's a completed signal (compatibility handling)
					if (result.AsGodotObject() != null && result.AsGodotObject().HasSignal("completed"))
					{
						await ToSignal(result.AsGodotObject(), "completed");
					}
					// 如果没有completed信号，尝试连接fade_out_completed信号
					// If no completed signal, try connecting to fade_out_completed signal
					else if (loadScreenInstance.HasSignal("fade_out_completed"))
					{
						await ToSignal(loadScreenInstance, "fade_out_completed");
					}
				}
				catch (Exception e)
				{
					GD.PrintErr($"[SceneManager] Error calling fade_out: {e.Message}");
					// Error occurred when calling fade_out:
				}
			}
			// 如果有hide_loading方法，则调用它
			// If has hide_loading method, call it
			else if (loadScreenInstance.HasMethod("hide_loading"))
			{
				try
				{
					var result = loadScreenInstance.Call("hide_loading");
					// 如果hide_loading方法返回了带有completed信号的对象，则等待该信号
					// If hide_loading method returns object with completed signal, wait for that signal
					if (result.AsGodotObject() != null && result.AsGodotObject().HasSignal("completed"))
					{
						await ToSignal(result.AsGodotObject(), "completed");
					}
				}
				catch (Exception e)
				{
					GD.PrintErr($"[SceneManager] Error calling hide_loading: {e.Message}");
					// Error occurred when calling hide_loading:
				}
			}
			// 如果有hide方法，则调用它
			// If has hide method, call it
			else if (loadScreenInstance.HasMethod("hide"))
			{
				try
				{
					loadScreenInstance.Call("hide");
				}
				catch (Exception e)
				{
					GD.PrintErr($"[SceneManager] Error calling hide: {e.Message}");
					// Error occurred when calling hide:
				}
			}
			
			// 清理加载屏幕实例
			// Clean up loading screen instance
			if (loadScreenInstance != _defaultLoadScreen)
			{
				// 如果不是默认加载屏幕，则释放自定义加载屏幕
				// If not default loading screen, free custom loading screen
				loadScreenInstance.QueueFree();
				GD.Print("[SceneManager] Cleaning up custom loading screen");
				// Cleaning up custom loading screen
			}
			else
			{
				// 如果是默认加载屏幕，则隐藏它而不是释放
				// If it's default loading screen, hide it instead of freeing
				if (loadScreenInstance is CanvasItem canvasItem)
				{
					canvasItem.Visible = false;
				}
				else if (loadScreenInstance.HasMethod("set_visible"))
				{
					try
					{
						loadScreenInstance.Call("set_visible", false);
					}
					catch (Exception e)
					{
						GD.PrintErr($"[SceneManager] Error calling set_visible: {e.Message}");
						// Error occurred when calling set_visible:
					}
				}
			}
			
			// 清空当前激活的加载屏幕引用
			// Clear currently active loading screen reference
			_activeLoadScreen = null;
			// 发送加载屏幕隐藏信号
			// Send loading screen hidden signal
			EmitSignal(SignalName.LoadScreenHidden, loadScreenInstance);
			GD.Print("[SceneManager] Loading screen hiding completed");
			// Loading screen hiding completed
		}
		
		#endregion
		
		#region Scene Switching Handler Functions
		// 场景切换处理函数
		
		/// <summary>
		/// 处理使用预加载资源的场景切换
		/// 从预加载资源缓存中获取场景资源并实例化
		/// 
		/// Handle scene switching using preload resources
		/// Get scene resource from preload resource cache and instantiate
		/// </summary>
		/// <param name="scenePath">场景路径 // Scene path</param>
		/// <param name="loadScreenInstance">加载屏幕实例 // Loading screen instance</param>
		/// <param name="useCache">是否使用缓存 // Whether to use cache</param>
		/// <returns>异步任务 // Async task</returns>
		private async Task HandlePreloadedResource(string scenePath, Node loadScreenInstance, bool useCache)
		{
			// 处理预加载资源缓存的场景
			// Handle scenes with preload resource cache
			await ShowLoadScreen(loadScreenInstance);
			
			// 从预加载资源缓存获取并移除
			// Get and remove from preload resource cache
			if (!_preloadResourceCache.TryGetValue(scenePath, out var packedScene))
			{
				GD.PrintErr($"[SceneManager] Preload resource cache error: {scenePath}");
				// Preload resource cache error:
				await HideLoadScreen(loadScreenInstance);
				return;
			}
			
			// 从预加载资源缓存中移除（因为即将使用）
			// Remove from preload resource cache (because it will be used)
			_preloadResourceCache.Remove(scenePath);
			var index = _preloadResourceCacheAccessOrder.IndexOf(scenePath);
			if (index != -1)
			{
				_preloadResourceCacheAccessOrder.RemoveAt(index);
			}
			
			GD.Print($"[SceneManager] Instantiate preloaded resources: {scenePath}");
			// Instantiating preloaded resource:
			// 实例化场景
			var newScene = packedScene.Instantiate();
			// 执行实际的场景切换
			await PerformSceneSwitch(newScene, scenePath, loadScreenInstance, useCache);
		}
		
		/// <summary>
		/// 处理正在预加载中的场景切换
		/// 等待预加载完成后再执行切换
		/// 
		/// Handle scene switching for scenes that are preloading
		/// Wait for preloading to complete before performing the switch
		/// </summary>
		/// <param name="scenePath">场景路径 // Scene path</param>
		/// <param name="loadScreenInstance">加载屏幕实例 // Loading screen instance</param>
		/// <param name="useCache">是否使用缓存 // Whether to use cache</param>
		/// <returns>异步任务 // Async task</returns>
		private async Task HandlePreloadingScene(string scenePath, Node loadScreenInstance, bool useCache)
		{
			// 显示加载屏幕
			// Show loading screen
			await ShowLoadScreen(loadScreenInstance);
			// 等待预加载完成
			// Wait for preload to complete
			await WaitForPreload(scenePath);
			
			// 预加载完成后，将资源存入预加载资源缓存
			// After preloading completes, store the resource in the preload resource cache
			if (_loadingResource != null)
			{
				_preloadResourceCache[scenePath] = _loadingResource;
				_preloadResourceCacheAccessOrder.Add(scenePath);
				GD.Print($"[SceneManager] Preload resource cached: {scenePath}");
				// Preload resource cached:
				
				// 如果预加载资源缓存数量超过最大限制，则移除最旧的缓存项
				// If preload resource cache count exceeds maximum limit, remove oldest cache item
				if (_preloadResourceCacheAccessOrder.Count > _maxPreloadResourceCacheSize)
				{
					RemoveOldestPreloadResource();
				}
			}
			
			// 实例化并切换场景
			// Instantiate and switch scene
			await InstantiateAndSwitch(scenePath, loadScreenInstance, useCache);
		}
		
		/// <summary>
		/// 处理使用缓存场景实例的场景切换
		/// 直接使用之前缓存的场景实例
		/// 
		/// Handle scene switching using cached scene instances
		/// Directly use previously cached scene instances
		/// </summary>
		/// <param name="scenePath">场景路径 // Scene path</param>
		/// <param name="loadScreenInstance">加载屏幕实例 // Loading screen instance</param>
		/// <returns>异步任务 // Async task</returns>
		private async Task HandleCachedScene(string scenePath, Node loadScreenInstance)
		{
			// 显示加载屏幕
			// Show loading screen
			await ShowLoadScreen(loadScreenInstance);
			// 切换到缓存的场景
			// Switch to cached scene
			await SwitchToCachedScene(scenePath, loadScreenInstance);
		}
		
		/// <summary>
		/// 处理直接加载场景的场景切换
		/// 不使用任何缓存，直接加载并实例化场景
		/// 
		/// Handle direct scene loading for scene switching
		/// Don't use any cache, directly load and instantiate the scene
		/// </summary>
		/// <param name="scenePath">场景路径 // Scene path</param>
		/// <param name="loadScreenInstance">加载屏幕实例 // Loading screen instance</param>
		/// <param name="useCache">是否使用缓存 // Whether to use cache</param>
		/// <returns>异步任务 // Async task</returns>
		private async Task HandleDirectLoad(string scenePath, Node loadScreenInstance, bool useCache)
		{
			// 显示加载屏幕
			// Show loading screen
			await ShowLoadScreen(loadScreenInstance);
			// 加载并切换场景
			// Load and switch scene
			await LoadAndSwitch(scenePath, loadScreenInstance, useCache);
		}
		
		#endregion
		
		#region Loading and Switching Core Functions
		// 加载和切换核心函数
		
		/// <summary>
		/// 等待预加载完成
		/// 定期检查加载进度并在控制台输出进度信息
		/// 
		/// Wait for preload to complete
		/// Periodically check loading progress and output progress information to console
		/// </summary>
		/// <param name="scenePath">正在预加载的场景路径 // Scene path being preloaded</param>
		/// <returns>异步任务 // Async task</returns>
		private async Task WaitForPreload(string scenePath)
		{
			GD.Print($"[SceneManager] Waiting for preload to complete: {scenePath}");
			// Waiting for preload to complete:
			
			var waitStartTime = Time.GetTicksMsec();
			// 循环等待直到预加载完成
			// Loop waiting until preload completes
			while (_loadingScenePath == scenePath && _loadingState == LoadState.Loading)
			{
				// 每500毫秒输出一次进度信息
				// Output progress information every 500 milliseconds
				if (Time.GetTicksMsec() - waitStartTime > 500)
				{
					var progress = GetLoadingProgress(scenePath);
					GD.Print($"[SceneManager] Preload progress: {progress * 100}%");
					// Preload progress:
					waitStartTime = Time.GetTicksMsec();
				}
				
				// 等待下一帧
				// Wait for next frame
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}
			
			GD.Print("[SceneManager] Preload waiting completed");
			// Preload waiting completed
		}
		
		/// <summary>
		/// 实例化预加载的场景并执行切换
		/// 
		/// Instantiate preloaded scene and perform switch
		/// </summary>
		/// <param name="scenePath">场景路径 // Scene path</param>
		/// <param name="loadScreenInstance">加载屏幕实例 // Loading screen instance</param>
		/// <param name="useCache">是否使用缓存 // Whether to use cache</param>
		/// <returns>异步任务 // Async task</returns>
		private async Task InstantiateAndSwitch(string scenePath, Node loadScreenInstance, bool useCache)
		{
			// 检查预加载资源是否有效
			// Check if preload resource is valid
			if (_loadingResource == null || _loadingScenePath != scenePath)
			{
				GD.PrintErr("[SceneManager] Preloaded resource does not exist or path mismatch");
				// Preloaded resource does not exist or path mismatch
				await HideLoadScreen(loadScreenInstance);
				return;
			}
			
			GD.Print($"[SceneManager] Instantiating preloaded scene: {scenePath}");
			// Instantiating preloaded scene:
			
			// 实例化场景
			// Instantiate scene
			var newScene = _loadingResource.Instantiate();
			if (newScene == null)
			{
				GD.PrintErr("[SceneManager] Scene instantiation failed");
				// Scene instantiation failed
				await HideLoadScreen(loadScreenInstance);
				return;
			}
			
			// 执行场景切换
			// Perform scene switch
			await PerformSceneSwitch(newScene, scenePath, loadScreenInstance, useCache);
			
			// 重置加载状态
			// Reset loading state
			_loadingScenePath = "";
			_loadingState = LoadState.NotLoaded;
			_loadingResource = null;
		}
		
		/// <summary>
		/// 切换到缓存中的场景
		/// 
		/// Switch to scene in cache
		/// </summary>
		/// <param name="scenePath">场景路径 // Scene path</param>
		/// <param name="loadScreenInstance">加载屏幕实例 // Loading screen instance</param>
		/// <returns>异步任务 // Async task</returns>
		private async Task SwitchToCachedScene(string scenePath, Node loadScreenInstance)
		{
			// 从缓存中获取场景
			// Get scene from cache
			if (!_sceneCache.TryGetValue(scenePath, out var cached))
			{
				GD.PrintErr($"[SceneManager] Scene not found in cache: {scenePath}");
				// Scene not found in cache:
				await HideLoadScreen(loadScreenInstance);
				return;
			}
			
			// 检查缓存的场景实例是否仍然有效
			// Check if cached scene instance is still valid
			if (!IsInstanceValid(cached.SceneInstance))
			{
				GD.PrintErr("[SceneManager] Cached scene instance is invalid");
				// Cached scene instance is invalid
				// 从缓存中移除无效的场景
				// Remove invalid scene from cache
				_sceneCache.Remove(scenePath);
				var index = _cacheAccessOrder.IndexOf(scenePath);
				if (index != -1)
				{
					_cacheAccessOrder.RemoveAt(index);
				}
				await HideLoadScreen(loadScreenInstance);
				return;
			}
			
			GD.Print($"[SceneManager] Using cached scene: {scenePath}");
			// Using cached scene:
			
			var sceneInstance = cached.SceneInstance;
			
			// 从缓存中移除（因为即将使用）
			// Remove from cache (because it will be used)
			_sceneCache.Remove(scenePath);
			var index2 = _cacheAccessOrder.IndexOf(scenePath);
			if (index2 != -1)
			{
				_cacheAccessOrder.RemoveAt(index2);
			}
			
			// 更新缓存访问统计
			// Update cache access statistics
			cached.Access();
			
			// 确保缓存节点不在任何父节点下（防止重复父节点）
			// Ensure cached node is not under any parent node (prevent duplicate parent nodes)
			if (sceneInstance.IsInsideTree())
			{
				sceneInstance.GetParent().RemoveChild(sceneInstance);
			}
			
			// 执行场景切换
			// Perform scene switch
			await PerformSceneSwitch(sceneInstance, scenePath, loadScreenInstance, true);
		}
		
		/// <summary>
		/// 直接加载并切换场景
		/// 
		/// Directly load and switch scene
		/// </summary>
		/// <param name="scenePath">场景路径 // Scene path</param>
		/// <param name="loadScreenInstance">加载屏幕实例 // Loading screen instance</param>
		/// <param name="useCache">是否使用缓存 // Whether to use cache</param>
		/// <returns>异步任务 // Async task</returns>
		private async Task LoadAndSwitch(string scenePath, Node loadScreenInstance, bool useCache)
		{
			GD.Print($"[SceneManager] Loading scene: {scenePath}");
			// Loading scene:
			
			// 加载场景资源
			// Load scene resource
			var newSceneResource = ResourceLoader.Load<PackedScene>(scenePath);
			if (newSceneResource == null)
			{
				GD.PrintErr($"[SceneManager] Scene loading failed: {scenePath}");
				// Scene loading failed:
				await HideLoadScreen(loadScreenInstance);
				return;
			}
			
			// 实例化场景
			// Instantiate scene
			var newScene = newSceneResource.Instantiate();
			if (newScene == null)
			{
				GD.PrintErr($"[SceneManager] Scene instantiation failed: {scenePath}");
				// Scene instantiation failed:
				await HideLoadScreen(loadScreenInstance);
				return;
			}
			
			// 执行场景切换
			// Perform scene switch
			await PerformSceneSwitch(newScene, scenePath, loadScreenInstance, useCache);
		}
		
		/// <summary>
		/// 执行实际的场景切换操作
		/// 处理旧场景的移除和新场景的添加
		/// 
		/// Perform actual scene switching operation
		/// Handle removal of old scene and addition of new scene
		/// </summary>
		/// <param name="newScene">新场景实例 // New scene instance</param>
		/// <param name="newScenePath">新场景路径 // New scene path</param>
		/// <param name="loadScreenInstance">加载屏幕实例 // Loading screen instance</param>
		/// <param name="useCache">是否使用缓存 // Whether to use cache</param>
		/// <returns>异步任务 // Async task</returns>
		private async Task PerformSceneSwitch(Node newScene, string newScenePath, Node loadScreenInstance, bool useCache)
		{
			GD.Print($"[SceneManager] Performing scene switch to: {newScenePath}");
			// Performing scene switch to:
			
			// 保存当前场景信息
			// Save current scene information
			var oldScene = _currentScene;
			var oldScenePath = _currentScenePath;
			
			// 更新场景管理器状态
			// Update scene manager state
			_previousScenePath = _currentScenePath;
			_currentScene = newScene;
			_currentScenePath = newScenePath;
			
			// 处理旧场景
			// Handle old scene
			if (oldScene != null && oldScene != newScene)
			{
				GD.Print($"[SceneManager] Removing current scene: {oldScene.Name}");
				// Removing current scene:
				
				// 从场景树中移除旧场景
				// Remove old scene from scene tree
				if (oldScene.IsInsideTree())
				{
					oldScene.GetParent().RemoveChild(oldScene);
				}
				
				// 如果启用缓存且旧场景路径有效，则将旧场景添加到缓存中
				// If caching is enabled and old scene path is valid, add old scene to cache
				if (useCache && !string.IsNullOrEmpty(oldScenePath) && oldScenePath != newScenePath)
				{
					AddToCache(oldScenePath, oldScene);
				}
				else
				{
					// 不使用缓存则直接清理旧场景
					// If not using cache, directly clean up old scene
					CleanupOrphanedNodes(oldScene);
					oldScene.QueueFree();
				}
			}
			
			GD.Print($"[SceneManager] Adding new scene: {newScene.Name}");
			// Adding new scene:
			
			// 确保新场景不在任何父节点下（防止重复父节点）
			// Ensure new scene is not under any parent node (prevent duplicate parent nodes)
			if (newScene.IsInsideTree())
			{
				newScene.GetParent().RemoveChild(newScene);
			}
			
			// 将新场景添加到场景树
			// Add new scene to scene tree
			GetTree().Root.AddChild(newScene);
			GetTree().CurrentScene = newScene;
			
			// 等待场景就绪
			// Wait for scene to be ready
			if (!newScene.IsNodeReady())
			{
				GD.Print("[SceneManager] Waiting for new scene to be ready...");
				// Waiting for new scene to be ready...
				await ToSignal(newScene, Node.SignalName.Ready);
			}
			
			// 隐藏加载屏幕
			// Hide loading screen
			await HideLoadScreen(loadScreenInstance);
			
			// 验证场景树状态
			// Validate scene tree state
			DebugValidateSceneTree();
			
			// 发送场景切换完成信号
			// Send scene switch completed signal
			EmitSignal(SignalName.SceneSwitchCompleted, newScenePath);
			GD.Print($"[SceneManager] Scene switching completed: {newScenePath}");
			// Scene switching completed:
		}
		
		#endregion
		
		#region Cache Management Internal Functions
		// 缓存管理内部函数
		
		/// <summary>
		/// 将场景添加到缓存中
		/// 使用LRU(最近最少使用)策略管理缓存
		/// 
		/// Add scene to cache
		/// Manage cache using LRU (Least Recently Used) strategy
		/// </summary>
		/// <param name="scenePath">场景路径 // Scene path</param>
		/// <param name="sceneInstance">场景实例 // Scene instance</param>
		private void AddToCache(string scenePath, Node sceneInstance)
		{
			// 检查参数有效性
			// Check parameter validity
			if (string.IsNullOrEmpty(scenePath) || sceneInstance == null)
			{
				GD.Print("[SceneManager] Warning: Cannot cache empty scene or path");
				// Warning: Cannot cache empty scene or path
				return;
			}
			
			// 如果场景已在缓存中，则先移除旧的缓存项
			// If scene is already in cache, first remove the old cache entry
			if (_sceneCache.ContainsKey(scenePath))
			{
				GD.Print($"[SceneManager] Scene already in instance cache: {scenePath}");
				// Scene already in instance cache:
				if (_sceneCache.TryGetValue(scenePath, out var oldCached) && IsInstanceValid(oldCached.SceneInstance))
				{
					CleanupOrphanedNodes(oldCached.SceneInstance);
					oldCached.SceneInstance.QueueFree();
				}
				_sceneCache.Remove(scenePath);
				var index = _cacheAccessOrder.IndexOf(scenePath);
				if (index != -1)
				{
					_cacheAccessOrder.RemoveAt(index);
				}
			}
			
			// 清理孤立节点确保节点不在场景树中
			// Clean up orphaned nodes to ensure node is not in scene tree
			CleanupOrphanedNodes(sceneInstance);
			
			// 如果节点仍在场景树中，这是错误状态，强制移除
			// If node is still in scene tree, this is an error state, forcibly remove
			if (sceneInstance.IsInsideTree())
			{
				GD.PrintErr("[SceneManager] Error: Attempting to cache node still in scene tree");
				// Error: Attempting to cache node still in scene tree
				sceneInstance.GetParent().RemoveChild(sceneInstance);
			}
			
			GD.Print($"[SceneManager] Adding to instance cache: {scenePath}");
			// Adding to instance cache:
			
			// 创建缓存项并添加到缓存中
			// Create cache entry and add to cache
			var cached = new CachedScene(sceneInstance);
			_sceneCache[scenePath] = cached;
			_cacheAccessOrder.Add(scenePath);
			// 发送场景缓存信号
			// Send scene cached signal
			EmitSignal(SignalName.SceneCached, scenePath);
			
			// 如果缓存数量超过最大限制，则移除最旧的缓存项
			// If cache count exceeds maximum limit, remove the oldest cache entry
			if (_cacheAccessOrder.Count > _maxCacheSize)
			{
				RemoveOldestCachedScene();
			}
		}
		
		/// <summary>
		/// 更新缓存访问记录
		/// 将指定场景标记为最近访问，更新其在LRU队列中的位置
		/// 
		/// Update cache access record
		/// Mark specified scene as recently accessed, update its position in LRU queue
		/// </summary>
		/// <param name="scenePath">场景路径 // Scene path</param>
		private void UpdateCacheAccess(string scenePath)
		{
			// 从访问顺序列表中移除该场景
			// Remove this scene from access order list
			var index = _cacheAccessOrder.IndexOf(scenePath);
			if (index != -1)
			{
				_cacheAccessOrder.RemoveAt(index);
			}
			// 将该场景添加到访问顺序列表末尾（表示最近访问）
			// Add this scene to the end of access order list (indicating recent access)
			_cacheAccessOrder.Add(scenePath);
			
			// 更新缓存项的时间戳
			// Update cache entry timestamp
			if (_sceneCache.TryGetValue(scenePath, out var cached))
			{
				cached.CachedTime = Time.GetUnixTimeFromSystem();
			}
		}
		
		/// <summary>
		/// 移除最旧的缓存场景
		/// 根据LRU策略移除最早未使用的场景
		/// 
		/// Remove oldest cached scene
		/// Remove earliest unused scene according to LRU strategy
		/// </summary>
		private void RemoveOldestCachedScene()
		{
			// 检查缓存是否为空
			// Check if cache is empty
			if (_cacheAccessOrder.Count == 0)
			{
				return;
			}
			
			// 获取最早访问的场景路径
			// Get earliest accessed scene path
			var oldestPath = _cacheAccessOrder[0];
			_cacheAccessOrder.RemoveAt(0);
			
			// 从缓存中移除该场景
			// Remove this scene from cache
			if (_sceneCache.TryGetValue(oldestPath, out var cached))
			{
				// 如果场景实例仍然有效，则释放它
				// If scene instance is still valid, free it
				if (IsInstanceValid(cached.SceneInstance))
				{
					CleanupOrphanedNodes(cached.SceneInstance);
					cached.SceneInstance.QueueFree();
				}
				_sceneCache.Remove(oldestPath);
				// 发送场景从缓存移除信号
				// Send scene removed from cache signal
				EmitSignal(SignalName.SceneRemovedFromCache, oldestPath);
				GD.Print($"[SceneManager] Removing old cache: {oldestPath}");
				// Removing old cache:
			}
		}
		
		/// <summary>
		/// 更新预加载资源缓存访问记录
		/// 将指定预加载资源标记为最近访问，更新其在LRU队列中的位置
		/// 
		/// Update preload resource cache access record
		/// Mark specified preload resource as recently accessed, update its position in LRU queue
		/// </summary>
		/// <param name="scenePath">场景路径 // Scene path</param>
		private void UpdatePreloadResourceCacheAccess(string scenePath)
		{
			// 从访问顺序列表中移除该场景
			// Remove this scene from access order list
			var index = _preloadResourceCacheAccessOrder.IndexOf(scenePath);
			if (index != -1)
			{
				_preloadResourceCacheAccessOrder.RemoveAt(index);
			}
			// 将该场景添加到访问顺序列表末尾（表示最近访问）
			// Add this scene to the end of access order list (indicating recent access)
			_preloadResourceCacheAccessOrder.Add(scenePath);
		}
		
		/// <summary>
		/// 移除最旧的预加载资源
		/// 根据LRU策略移除最早未使用的预加载资源
		/// 
		/// Remove oldest preload resource
		/// Remove earliest unused preload resource according to LRU strategy
		/// </summary>
		private void RemoveOldestPreloadResource()
		{
			// 检查预加载资源缓存是否为空
			// Check if preload resource cache is empty
			if (_preloadResourceCacheAccessOrder.Count == 0)
			{
				return;
			}
			
			// 获取最早访问的场景路径
			// Get earliest accessed scene path
			var oldestPath = _preloadResourceCacheAccessOrder[0];
			_preloadResourceCacheAccessOrder.RemoveAt(0);
			
			// 从预加载资源缓存中移除该资源
			// Remove this resource from preload resource cache
			if (_preloadResourceCache.Remove(oldestPath))
			{
				GD.Print($"[SceneManager] Removing old preload resource: {oldestPath}");
				// Removing old preload resource:
			}
		}
		
		#endregion
		
		#region Preload Internal Functions
		// 预加载内部函数
		
		/// <summary>
		/// 异步预加载场景
		/// 使用Godot的线程化资源加载功能在后台加载场景资源
		/// 
		/// Asynchronously preload scene
		/// Use Godot's threaded resource loading feature to load scene resources in background
		/// </summary>
		/// <param name="scenePath">场景路径 // Scene path</param>
		/// <returns>异步任务 // Async task</returns>
		private async Task AsyncPreloadScene(string scenePath)
		{
			GD.Print($"[SceneManager] Asynchronous preload: {scenePath}");
			// Asynchronous preload:
			
			var loadStartTime = Time.GetTicksMsec();
			// 请求线程化加载
			// Request threaded loading
			ResourceLoader.LoadThreadedRequest(scenePath);
			
			// 循环检查加载状态
			// Loop to check loading status
			while (true)
			{
				// 创建用于接收进度信息的数组
				// Create array to receive progress information
				var progressArray = new Godot.Collections.Array();
				// 获取加载状态和进度
				// Get loading status and progress
				var status = ResourceLoader.LoadThreadedGetStatus(scenePath, progressArray);
				
				// 根据不同状态进行处理
				// Handle based on different states
				switch (status)
				{
					case ResourceLoader.ThreadLoadStatus.InProgress:
						// 如果正在加载中，定期输出进度信息
						// If loading is in progress, periodically output progress information
						if (Time.GetTicksMsec() - loadStartTime > 500)
						{
							if (progressArray.Count > 0)
							{
								GD.Print($"[SceneManager] Asynchronous loading progress: {(float)progressArray[0] * 100}%");
								// Asynchronous loading progress:
							}
							loadStartTime = Time.GetTicksMsec();
						}
						
						// 等待下一帧继续检查
						// Wait for next frame to continue checking
						await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
						break;
					
					case ResourceLoader.ThreadLoadStatus.Loaded:
						// 加载完成，获取加载的资源
						// Loading complete, get loaded resource
						var loadedResource = ResourceLoader.LoadThreadedGet(scenePath);
						// 检查资源类型并赋值给_loadingResource
						// Check resource type and assign to _loadingResource
						if (loadedResource is PackedScene packedScene)
						{
							_loadingResource = packedScene;
						}
						GD.Print($"[SceneManager] Asynchronous preload completed: {scenePath}");
						// Asynchronous preload completed:
						return;
					
					case ResourceLoader.ThreadLoadStatus.Failed:
						// 加载失败，记录错误并重置_loadingResource
						// Loading failed, log error and reset _loadingResource
						GD.PrintErr($"[SceneManager] Asynchronous loading failed: {scenePath}");
						// Asynchronous loading failed:
						_loadingResource = null;
						return;
					
					default:
						// 未知状态，记录错误并重置_loadingResource
						// Unknown state, log error and reset _loadingResource
						GD.PrintErr($"[SceneManager] Unknown loading status: {status}");
						// Unknown loading status:
						_loadingResource = null;
						return;
				}
			}
		}
		
		/// <summary>
		/// 同步预加载场景
		/// 直接在主线程中加载场景资源，会阻塞游戏进程直到加载完成
		/// 
		/// Synchronously preload scene
		/// Directly load scene resources in main thread, will block game process until loading completes
		/// </summary>
		/// <param name="scenePath">场景路径 // Scene path</param>
		private void SyncPreloadScene(string scenePath)
		{
			GD.Print($"[SceneManager] Synchronous preload: {scenePath}");
			// Synchronous preload:
			// 直接加载场景资源
			// Directly load scene resource
			_loadingResource = ResourceLoader.Load<PackedScene>(scenePath);
		}
		
		#endregion
		
		#region Orphaned Node Cleanup Functions
		// 孤立节点清理函数
		
		/// <summary>
		/// 清理孤立节点
		/// 递归清理可能成为孤立节点的子节点
		/// 
		/// Clean up orphaned nodes
		/// Recursively clean up child nodes that may become orphaned
		/// </summary>
		/// <param name="rootNode">根节点 // Root node</param>
		private void CleanupOrphanedNodes(Node rootNode)
		{
			// 递归清理可能成为孤立节点的子节点
			// Recursively clean up child nodes that may become orphaned
			if (rootNode == null || !IsInstanceValid(rootNode))
			{
				return;
			}
			
			// 如果节点仍在场景树中，强制移除
			// If node is still in scene tree, forcibly remove
			if (rootNode.IsInsideTree())
			{
				var parent = rootNode.GetParent();
				if (parent != null)
				{
					parent.RemoveChild(rootNode);
				}
			}
			
			// 递归清理所有子节点
			// Recursively clean up all child nodes
			foreach (var child in rootNode.GetChildren())
			{
				CleanupOrphanedNodes(child);
			}
		}
		
		/// <summary>
		/// 调试验证场景树
		/// 调试用:验证场景树状态
		/// 
		/// Debug validate scene tree
		/// Debug: Validate scene tree state
		/// </summary>
		private void DebugValidateSceneTree()
		{
			// 调试用:验证场景树状态
			// Debug: Validate scene tree state
			var root = GetTree().Root;
			var current = GetTree().CurrentScene;
			
			GD.Print($"[SceneManager] Scene tree validation - Root node child count: {root.GetChildCount()}");
			// Scene tree validation - Root node child count:
			GD.Print($"[SceneManager] Current scene: {(current != null ? current.Name : "None")}");
			// Current scene:
			
			// 检查缓存节点是否意外在场景树中
			// Check if cached nodes are unexpectedly in scene tree
			foreach (var kvp in _sceneCache)
			{
				var scenePath = kvp.Key;
				var cached = kvp.Value;
				if (IsInstanceValid(cached.SceneInstance) && cached.SceneInstance.IsInsideTree())
				{
					GD.PrintErr($"[SceneManager] Error: Cached node still in scene tree: {scenePath}");
					// Error: Cached node still in scene tree:
				}
			}
		}
		
		#endregion
		
		#region Signal Connection Helper
		// 信号连接辅助
		
		/// <summary>
		/// 连接所有信号
		/// 
		/// Connect all signals
		/// </summary>
		/// <param name="target">目标节点 // Target node</param>
		public void ConnectAllSignals(Node target)
		{
			if (target == null)
			{
				return;
			}
			
			var signalNames = new[]
			{
				SignalName.ScenePreloadStarted,
				SignalName.ScenePreloadCompleted,
				SignalName.SceneSwitchStarted,
				SignalName.SceneSwitchCompleted,
				SignalName.SceneCached,
				SignalName.SceneRemovedFromCache,
				SignalName.LoadScreenShown,
				SignalName.LoadScreenHidden
			};
			
			foreach (var signalName in signalNames)
			{
				var methodName = "_on_scene_manager_" + signalName;
				if (target.HasMethod(methodName))
				{
					Connect(signalName, new Callable(target, methodName));
					GD.Print($"[SceneManager] Connecting signal: {signalName} -> {methodName}");
					// Connecting signal:
				}
			}
		}
		
		#endregion
		
		#region Debug and Utility Functions
		// 调试和工具函数
		
		/// <summary>
		/// 打印调试信息
		/// 
		/// Print debug information
		/// </summary>
		public void PrintDebugInfo()
		{
			GD.Print("\n=== SceneManager Debug Info ===");
			// SceneManager Debug Info
			// 场景管理器调试信息
			GD.Print($"Current scene: {(_currentScene != null ? _currentScenePath : "None")}");
			// Current scene:
			// 当前场景:
			GD.Print($"Previous scene: {_previousScenePath}");
			// Previous scene:
			// 上一个场景:
			GD.Print($"Instance cache count: {_sceneCache.Count}/{_maxCacheSize}");
			// Instance cache count:
			// 实例缓存数量:
			GD.Print($"Preload resource cache count: {_preloadResourceCache.Count}/{_maxPreloadResourceCacheSize}");
			// Preload resource cache count:
			// 预加载资源缓存数量:
			GD.Print($"Cache access order: {string.Join(", ", _cacheAccessOrder)}");
			// Cache access order:
			// 缓存访问顺序:
			GD.Print($"Preload resource cache access order: {string.Join(", ", _preloadResourceCacheAccessOrder)}");
			// Preload resource cache access order:
			// 预加载资源缓存访问顺序:
			GD.Print($"Scene currently loading: {(!string.IsNullOrEmpty(_loadingScenePath) ? _loadingScenePath : "None")}");
			// Scene currently loading:
			// 正在加载的场景:
			GD.Print($"Loading state: {_loadingState}");
			// Loading state:
			// 加载状态:
			GD.Print($"Default loading screen: {(_defaultLoadScreen != null ? "Loaded" : "Not loaded")}");
			// Default loading screen: "Loaded" or "Not loaded"
			// 默认加载屏幕:
			GD.Print($"Active loading screen: {(_activeLoadScreen != null ? "Yes" : "No")}");
			// Active loading screen: "Yes" or "No"
			// 活动加载屏幕:
			GD.Print($"Using asynchronous loading: {_useAsyncLoading}");
			// Using asynchronous loading:
			// 使用异步加载:
			GD.Print($"Always use default loading screen: {_alwaysUseDefaultLoadScreen}");
			// Always use default loading screen:
			// 总是使用默认加载屏幕:
			GD.Print("===============================\n");
		}
		
		#endregion
		
		#region Inner Classes
		// 内部类
		
		/// <summary>
		/// 缓存的场景类
		/// 
		/// Cached scene class
		/// </summary>
		private class CachedScene
		{
			public Node SceneInstance { get; }
			public double CachedTime { get; set; }
			public int AccessCount { get; private set; }
			
			public CachedScene(Node scene)
			{
				SceneInstance = scene;
				CachedTime = Time.GetUnixTimeFromSystem();
				AccessCount = 0;
			}
			
			public void Access()
			{
				AccessCount++;
			}
		}
		
		#endregion
		
	}
}
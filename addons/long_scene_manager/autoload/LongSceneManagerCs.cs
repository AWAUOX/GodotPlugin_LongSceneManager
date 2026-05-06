using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class LongSceneManagerCs : Node
{
	private const string DefaultLoadScreenPath = "res://addons/long_scene_manager/ui/loading_screen/CSharp/loading_black_screen_cs.tscn";

	private static LongSceneManagerCs _instance;
	public static LongSceneManagerCs Instance => _instance;

	public enum LoadState
	{
		NotLoaded,
		Loading,
		Loaded,
		Instantiated,
		Cancelled
	}

	public enum LoadMethod
	{
		Direct,
		PreloadCache,
		SceneCache,
		BothPreloadFirst,
		BothInstanceFirst
	}

	[Signal]
	public delegate void ScenePreloadStartedEventHandler(string scenePath);

	[Signal]
	public delegate void ScenePreloadCompletedEventHandler(string scenePath);

	[Signal]
	public delegate void ScenePreloadCancelledEventHandler(string scenePath);

	[Signal]
	public delegate void SceneSwitchStartedEventHandler(string fromScene, string toScene);

	[Signal]
	public delegate void SceneSwitchCompletedEventHandler(string scenePath);

	[Signal]
	public delegate void SceneCachedEventHandler(string scenePath);

	[Signal]
	public delegate void SceneRemovedFromCacheEventHandler(string scenePath);

	[Signal]
	public delegate void LoadScreenShownEventHandler(Node loadScreenInstance);

	[Signal]
	public delegate void LoadScreenHiddenEventHandler(Node loadScreenInstance);

	[Signal]
	public delegate void ScenePreloadFailedEventHandler(string scenePath);

	[Signal]
	public delegate void SceneSwitchFailedEventHandler(string scenePath);

	[ExportCategory("Scene Manager Global Configuration")]
	[Export(PropertyHint.Range, "1,20")]
	private int _maxCacheSize = 4;

	[Export(PropertyHint.Range, "1,50")]
	private int _maxTempPreloadResourceCacheSize = 8;

	[Export(PropertyHint.Range, "0,50")]
	private int _maxFixedPreloadResourceCacheSize = 4;

	[Export]
	private bool _useAsyncLoading = true;

	[Export]
	private bool _alwaysUseDefaultLoadScreen = false;

	[Export(PropertyHint.Range, "1,10")]
	private int _instantiateFrames = 3;

	public int MaxCacheSize
	{
		get => _maxCacheSize;
		set
		{
			if (value < 1)
			{
				GD.PushError("[SceneManager] Error: Cache size must be greater than 0");
				return;
			}
			_maxCacheSize = value;
			GD.Print($"[SceneManager] Setting maximum cache size: {_maxCacheSize}");

			while (_instantiateSceneCacheOrder.Count > _maxCacheSize)
			{
				RemoveOldestCachedScene();
			}
		}
	}

	public int MaxTempPreloadResourceCacheSize
	{
		get => _maxTempPreloadResourceCacheSize;
		set
		{
			if (value < 1)
			{
				GD.PushError("[SceneManager] Error: Temp preload cache size must be greater than 0");
				return;
			}
			_maxTempPreloadResourceCacheSize = value;
			GD.Print($"[SceneManager] Setting maximum temp preload cache size: {_maxTempPreloadResourceCacheSize}");

			while (_tempPreloadedResourceCacheOrder.Count > _maxTempPreloadResourceCacheSize)
			{
				RemoveOldestTempPreloadResource();
			}
		}
	}

	public int MaxFixedPreloadResourceCacheSize
	{
		get => _maxFixedPreloadResourceCacheSize;
		set
		{
			if (value < 0)
			{
				GD.PushError("[SceneManager] Error: Fixed cache size must be >= 0");
				return;
			}
			_maxFixedPreloadResourceCacheSize = value;
			GD.Print($"[SceneManager] Setting maximum fixed cache size: {_maxFixedPreloadResourceCacheSize}");

			while (_fixedPreloadResourceCacheOrder.Count > _maxFixedPreloadResourceCacheSize && _maxFixedPreloadResourceCacheSize > 0)
			{
				RemoveOldestFixedPreloadResource();
			}
		}
	}

	public bool UseAsyncLoading
	{
		get => _useAsyncLoading;
		set => _useAsyncLoading = value;
	}

	public bool AlwaysUseDefaultLoadScreen
	{
		get => _alwaysUseDefaultLoadScreen;
		set => _alwaysUseDefaultLoadScreen = value;
	}

	public int InstantiateFrames
	{
		get => _instantiateFrames;
		set => _instantiateFrames = value;
	}

	private Node _currentScene;
	private string _currentScenePath = "";
	private string _previousScenePath = "";
	private Node _defaultLoadScreen;
	private Node _activeLoadScreen;

	private readonly Dictionary<string, CachedScene> _instantiateSceneCache = new();
	private readonly List<string> _instantiateSceneCacheOrder = new();

	private readonly Dictionary<string, Resource> _tempPreloadedResourceCache = new();
	private readonly List<string> _tempPreloadedResourceCacheOrder = new();

	private readonly Dictionary<string, Resource> _fixedPreloadResourceCache = new();
	private readonly List<string> _fixedPreloadResourceCacheOrder = new();

	private readonly Dictionary<string, PreloadResourceState> _preloadResourceStates = new();
	private bool _isSwitching = false;

	private class CachedScene
	{
		public Node SceneInstance { get; }
		public double CachedTime { get; }

		public CachedScene(Node scene)
		{
			SceneInstance = scene;
			CachedTime = Time.GetUnixTimeFromSystem();
		}
	}

	private class PreloadResourceState
	{
		public LoadState State { get; set; }
		public Resource Resource { get; set; }
		public bool Fixed { get; set; }

		public PreloadResourceState()
		{
			State = LoadState.NotLoaded;
			Resource = null;
			Fixed = false;
		}
	}

	public override void _Ready()
	{
		_instance = this;
		GD.Print("[SceneManager] Scene manager singleton initialized");
		InitDefaultLoadScreen();
		_currentScene = GetTree().CurrentScene;
		if (_currentScene != null)
		{
			_currentScenePath = _currentScene.SceneFilePath;
			GD.Print($"[SceneManager] Current scene: {_currentScenePath}");
		}
		GD.Print($"[SceneManager] Initialization complete, max cache: {_maxCacheSize}");
	}

	public async Task SwitchScene(string newScenePath, object loadMethod = null, bool cacheCurrentScene = true, string loadScreenPath = "")
	{
		if (_isSwitching)
		{
			GD.PushWarning($"[SceneManager] Warning: Scene switch already in progress, ignoring request to: {newScenePath}");
			return;
		}

		_isSwitching = true;
		GD.Print($"[SceneManager] Start switching scene to: {newScenePath}");

		DebugValidateSceneTree();

		if (_alwaysUseDefaultLoadScreen)
		{
			loadScreenPath = "";
			GD.Print("[SceneManager] Force using default loading screen");
		}

		if (!ResourceLoader.Exists(newScenePath))
		{
			GD.PushError($"[SceneManager] Error: Target scene path does not exist: {newScenePath}");
			_isSwitching = false;
			EmitSignal(SignalName.SceneSwitchFailed, newScenePath);
			return;
		}

		EmitSignal(SignalName.SceneSwitchStarted, _currentScenePath, newScenePath);

		if (_currentScenePath == newScenePath)
		{
			GD.Print($"[SceneManager] Scene already loaded: {newScenePath}");
			_isSwitching = false;
			EmitSignal(SignalName.SceneSwitchCompleted, newScenePath);
			return;
		}

		Node loadScreenToUse = GetLoadScreenInstance(loadScreenPath);
		if (loadScreenPath != "no_transition" && loadScreenToUse == null)
		{
			GD.PushError("[SceneManager] Error: Unable to get loading screen, switching aborted");
			_isSwitching = false;
			EmitSignal(SignalName.SceneSwitchFailed, newScenePath);
			return;
		}

		await LoadSceneByMethod(newScenePath, loadMethod, cacheCurrentScene, loadScreenToUse);

		_isSwitching = false;
	}

	public void PreloadScene(string scenePath, bool fixedPreload = false)
	{
		if (!ResourceLoader.Exists(scenePath))
		{
			GD.PushError($"[SceneManager] Error: Preload scene path does not exist: {scenePath}");
			return;
		}

		var resourceState = GetPreloadResourceState(scenePath);
		if (resourceState.State == LoadState.Loading)
		{
			GD.Print($"[SceneManager] Scene is loading: {scenePath}");
			return;
		}
		if (resourceState.State == LoadState.Loaded)
		{
			GD.Print($"[SceneManager] Scene already preloaded: {scenePath}");
			return;
		}
		if (resourceState.State == LoadState.Instantiated)
		{
			GD.Print($"[SceneManager] Scene was instantiated, allowing re-preload: {scenePath}");
		}
		if (resourceState.State == LoadState.Cancelled)
		{
			GD.Print($"[SceneManager] Scene preload was cancelled, will restart: {scenePath}");
		}

		if (_tempPreloadedResourceCache.ContainsKey(scenePath))
		{
			GD.Print($"[SceneManager] Scene already in temp cache: {scenePath}");
			return;
		}
		if (_fixedPreloadResourceCache.ContainsKey(scenePath))
		{
			GD.Print($"[SceneManager] Scene already in fixed cache: {scenePath}");
			return;
		}

		GD.Print($"[SceneManager] Start preloading scene: {scenePath} (fixed: {fixedPreload})");
		EmitSignal(SignalName.ScenePreloadStarted, scenePath);

		resourceState.State = LoadState.Loading;
		resourceState.Fixed = fixedPreload;
		resourceState.Resource = null;

		PreloadBackground(scenePath);
	}

	public void PreloadScenes(string[] scenePaths, bool fixedPreload = false)
	{
		foreach (var path in scenePaths)
		{
			PreloadScene(path, fixedPreload);
		}
	}

	public void CancelPreloadingScene(string scenePath)
	{
		if (_preloadResourceStates.TryGetValue(scenePath, out var state))
		{
			if (state.State == LoadState.Loading)
			{
				state.State = LoadState.Cancelled;
				GD.Print($"[SceneManager] Preload cancelled: {scenePath}");
				EmitSignal(SignalName.ScenePreloadCancelled, scenePath);
			}
			else
			{
				GD.Print($"[SceneManager] Preload not in loading state: {scenePath}");
			}
		}
		else
		{
			GD.Print($"[SceneManager] No preload state found: {scenePath}");
		}
	}

	public void CancelAllPreloading()
	{
		var toCancel = new List<string>();
		foreach (var path in _preloadResourceStates.Keys)
		{
			if (_preloadResourceStates[path].State == LoadState.Loading)
			{
				toCancel.Add(path);
			}
		}

		foreach (var path in toCancel)
		{
			CancelPreloadingScene(path);
		}
	}

	public void ClearAllCache()
	{
		GD.Print("[SceneManager] Clearing cache...");

		_tempPreloadedResourceCache.Clear();
		_tempPreloadedResourceCacheOrder.Clear();
		_fixedPreloadResourceCache.Clear();
		_fixedPreloadResourceCacheOrder.Clear();
		_preloadResourceStates.Clear();
		GD.Print("[SceneManager] Temp and fixed preload resource cache cleared");

		var toRemove = new List<string>();
		foreach (var scenePath in _instantiateSceneCache.Keys)
		{
			var cached = _instantiateSceneCache[scenePath];
			if (IsInstanceValid(cached.SceneInstance))
			{
				CleanupOrphanedNodes(cached.SceneInstance);
				cached.SceneInstance.QueueFree();
			}
			toRemove.Add(scenePath);
			EmitSignal(SignalName.SceneRemovedFromCache, scenePath);
		}

		foreach (var scenePath in toRemove)
		{
			_instantiateSceneCache.Remove(scenePath);
			var index = _instantiateSceneCacheOrder.IndexOf(scenePath);
			if (index != -1)
			{
				_instantiateSceneCacheOrder.RemoveAt(index);
			}
		}

		GD.Print("[SceneManager] Cache cleared");
	}

	public void ClearTempPreloadCache()
	{
		GD.Print("[SceneManager] Clearing temp preload cache...");

		var toRemove = new List<string>();
		foreach (var path in _preloadResourceStates.Keys)
		{
			if (_preloadResourceStates[path].Fixed == false)
			{
				toRemove.Add(path);
			}
		}

		foreach (var path in toRemove)
		{
			_preloadResourceStates.Remove(path);
		}

		_tempPreloadedResourceCache.Clear();
		_tempPreloadedResourceCacheOrder.Clear();
		GD.Print("[SceneManager] Temp preload cache cleared");
	}

	public void ClearFixedCache()
	{
		GD.Print("[SceneManager] Clearing fixed cache...");

		var toRemove = new List<string>();
		foreach (var path in _fixedPreloadResourceCache.Keys)
		{
			toRemove.Add(path);
		}

		foreach (var path in toRemove)
		{
			_fixedPreloadResourceCache.Remove(path);
			var index = _fixedPreloadResourceCacheOrder.IndexOf(path);
			if (index != -1)
			{
				_fixedPreloadResourceCacheOrder.RemoveAt(index);
			}
			EmitSignal(SignalName.SceneRemovedFromCache, path);
		}

		foreach (var path in toRemove)
		{
			_preloadResourceStates.Remove(path);
		}

		GD.Print("[SceneManager] Fixed cache cleared");
	}

	public void ClearInstanceCache()
	{
		GD.Print("[SceneManager] Clearing instance cache...");

		var toRemove = new List<string>();
		foreach (var scenePath in _instantiateSceneCache.Keys)
		{
			var cached = _instantiateSceneCache[scenePath];
			if (IsInstanceValid(cached.SceneInstance))
			{
				CleanupOrphanedNodes(cached.SceneInstance);
				cached.SceneInstance.QueueFree();
			}
			toRemove.Add(scenePath);
			EmitSignal(SignalName.SceneRemovedFromCache, scenePath);
		}

		foreach (var scenePath in toRemove)
		{
			_instantiateSceneCache.Remove(scenePath);
			var index = _instantiateSceneCacheOrder.IndexOf(scenePath);
			if (index != -1)
			{
				_instantiateSceneCacheOrder.RemoveAt(index);
			}
		}

		GD.Print("[SceneManager] Instance cache cleared");
	}

	public void RemoveTempResource(string scenePath)
	{
		if (_tempPreloadedResourceCache.ContainsKey(scenePath) || _preloadResourceStates.ContainsKey(scenePath))
		{
			_tempPreloadedResourceCache.Remove(scenePath);

			var index = _tempPreloadedResourceCacheOrder.IndexOf(scenePath);
			if (index != -1)
			{
				_tempPreloadedResourceCacheOrder.RemoveAt(index);
			}

			ClearPreloadResourceState(scenePath);

			GD.Print($"[SceneManager] Removed temp preloaded resource: {scenePath}");
			EmitSignal(SignalName.SceneRemovedFromCache, scenePath);
		}
		else
		{
			GD.Print($"[SceneManager] Warning: Temp preloaded resource not found: {scenePath}");
			if (_instantiateSceneCache.ContainsKey(scenePath))
			{
				GD.Print("[SceneManager] Hint: Scene is in instance cache. Use 'RemoveCachedScene()' instead.");
			}
		}
	}

	public void RemoveFixedResource(string scenePath)
	{
		if (_fixedPreloadResourceCache.ContainsKey(scenePath))
		{
			_fixedPreloadResourceCache.Remove(scenePath);

			var index = _fixedPreloadResourceCacheOrder.IndexOf(scenePath);
			if (index != -1)
			{
				_fixedPreloadResourceCacheOrder.RemoveAt(index);
			}

			ClearPreloadResourceState(scenePath);

			GD.Print($"[SceneManager] Removed fixed preloaded resource: {scenePath}");
			EmitSignal(SignalName.SceneRemovedFromCache, scenePath);
		}
		else
		{
			GD.Print($"[SceneManager] Warning: Fixed preloaded resource not found: {scenePath}");
		}
	}

	public void RemoveCachedScene(string scenePath)
	{
		if (_instantiateSceneCache.TryGetValue(scenePath, out var cached))
		{
			if (IsInstanceValid(cached.SceneInstance))
			{
				CleanupOrphanedNodes(cached.SceneInstance);
				cached.SceneInstance.QueueFree();
			}

			_instantiateSceneCache.Remove(scenePath);

			var index = _instantiateSceneCacheOrder.IndexOf(scenePath);
			if (index != -1)
			{
				_instantiateSceneCacheOrder.RemoveAt(index);
			}

			ClearPreloadResourceState(scenePath);

			GD.Print($"[SceneManager] Removed cached scene: {scenePath}");
			EmitSignal(SignalName.SceneRemovedFromCache, scenePath);
		}
		else
		{
			GD.Print($"[SceneManager] Warning: Cached scene not found: {scenePath}");
			if (_tempPreloadedResourceCache.ContainsKey(scenePath))
			{
				GD.Print("[SceneManager] Hint: Scene is in temp preload cache. Use 'RemoveTempResource()' instead.");
			}
		}
	}

	public void MoveToFixed(string scenePath)
	{
		if (_tempPreloadedResourceCache.TryGetValue(scenePath, out var resource))
		{
			if (_fixedPreloadResourceCacheOrder.Count >= _maxFixedPreloadResourceCacheSize && _maxFixedPreloadResourceCacheSize > 0)
			{
				RemoveOldestFixedPreloadResource();
			}

			_tempPreloadedResourceCache.Remove(scenePath);
			var index = _tempPreloadedResourceCacheOrder.IndexOf(scenePath);
			if (index != -1)
			{
				_tempPreloadedResourceCacheOrder.RemoveAt(index);
			}

			_fixedPreloadResourceCache[scenePath] = resource;
			_fixedPreloadResourceCacheOrder.Add(scenePath);

			GD.Print($"[SceneManager] Moved resource to fixed cache: {scenePath}");
		}
		else
		{
			GD.Print($"[SceneManager] Warning: Resource not found in temp preload cache: {scenePath}");
		}
	}

	public void MoveToTemp(string scenePath)
	{
		if (_fixedPreloadResourceCache.TryGetValue(scenePath, out var resource))
		{
			if (_tempPreloadedResourceCacheOrder.Count >= _maxTempPreloadResourceCacheSize)
			{
				RemoveOldestTempPreloadResource();
			}

			_fixedPreloadResourceCache.Remove(scenePath);
			var index = _fixedPreloadResourceCacheOrder.IndexOf(scenePath);
			if (index != -1)
			{
				_fixedPreloadResourceCacheOrder.RemoveAt(index);
			}

			_tempPreloadedResourceCache[scenePath] = resource;
			_tempPreloadedResourceCacheOrder.Add(scenePath);

			GD.Print($"[SceneManager] Moved resource to temp cache: {scenePath}");
		}
		else
		{
			GD.Print($"[SceneManager] Warning: Resource not found in fixed preload cache: {scenePath}");
		}
	}

	public void SetMaxFixedCacheSize(int newSize)
	{
		if (newSize < 0)
		{
			GD.PushError("[SceneManager] Error: Fixed cache size must be >= 0");
			return;
		}

		MaxFixedPreloadResourceCacheSize = newSize;
	}

	public void SetMaxCacheSize(int newSize)
	{
		MaxCacheSize = newSize;
	}

	public void SetMaxTempPreloadResourceCacheSize(int newSize)
	{
		MaxTempPreloadResourceCacheSize = newSize;
	}

	public Godot.Collections.Dictionary GetCacheInfo()
	{
		var cachedScenes = new Godot.Collections.Array();
		foreach (var path in _instantiateSceneCache.Keys)
		{
			var cached = _instantiateSceneCache[path];
			cachedScenes.Add(new Godot.Collections.Dictionary
			{
				{ "path", path },
				{ "cached_time", cached.CachedTime },
				{ "instance_valid", IsInstanceValid(cached.SceneInstance) }
			});
		}

		var tempPreloadedScenes = new Godot.Collections.Array();
		foreach (var path in _tempPreloadedResourceCache.Keys)
		{
			tempPreloadedScenes.Add(path);
		}

		var fixedPreloadedScenes = new Godot.Collections.Array();
		foreach (var path in _fixedPreloadResourceCache.Keys)
		{
			fixedPreloadedScenes.Add(path);
		}

		var preloadStatesInfo = new Godot.Collections.Array();
		foreach (var path in _preloadResourceStates.Keys)
		{
			var stateInfo = _preloadResourceStates[path];
			preloadStatesInfo.Add(new Godot.Collections.Dictionary
			{
				{ "path", path },
				{ "state", (int)stateInfo.State },
				{ "fixed", stateInfo.Fixed },
				{ "has_resource", stateInfo.Resource != null }
			});
		}

		return new Godot.Collections.Dictionary
		{
			{ "current_scene", _currentScenePath },
			{ "previous_scene", _previousScenePath },
			{ "instance_cache", new Godot.Collections.Dictionary
				{
					{ "size", _instantiateSceneCache.Count },
					{ "max_size", _maxCacheSize },
					{ "access_order", CreateArrayFromList(_instantiateSceneCacheOrder) },
					{ "scenes", cachedScenes }
				}
			},
			{ "temp_preload_cache", new Godot.Collections.Dictionary
				{
					{ "size", _tempPreloadedResourceCache.Count },
					{ "max_size", _maxTempPreloadResourceCacheSize },
					{ "access_order", CreateArrayFromList(_tempPreloadedResourceCacheOrder) },
					{ "scenes", tempPreloadedScenes }
				}
			},
			{ "fixed_preload_cache", new Godot.Collections.Dictionary
				{
					{ "size", _fixedPreloadResourceCache.Count },
					{ "max_size", _maxFixedPreloadResourceCacheSize },
					{ "access_order", CreateArrayFromList(_fixedPreloadResourceCacheOrder) },
					{ "scenes", fixedPreloadedScenes }
				}
			},
			{ "preload_states", new Godot.Collections.Dictionary
				{
					{ "size", _preloadResourceStates.Count },
					{ "states", preloadStatesInfo }
				}
			}
		};
	}

	public bool IsSceneCached(string scenePath)
	{
		return _instantiateSceneCache.ContainsKey(scenePath) ||
			   _tempPreloadedResourceCache.ContainsKey(scenePath) ||
			   _fixedPreloadResourceCache.ContainsKey(scenePath);
	}

	public bool IsScenePreloading(string scenePath)
	{
		return _preloadResourceStates.TryGetValue(scenePath, out var state) && state.State == LoadState.Loading;
	}

	public string[] GetPreloadingScenes()
	{
		var loading = new List<string>();
		foreach (var path in _preloadResourceStates.Keys)
		{
			if (_preloadResourceStates[path].State == LoadState.Loading)
			{
				loading.Add(path);
			}
		}
		return loading.ToArray();
	}

	public Node GetCurrentScene()
	{
		return _currentScene;
	}

	public string GetPreviousScenePath()
	{
		return _previousScenePath;
	}

	public float GetLoadingProgress(string scenePath)
	{
		if (_preloadResourceStates.TryGetValue(scenePath, out var state))
		{
			if (state.State == LoadState.Loading)
			{
				var progress = new Godot.Collections.Array();
				progress.Add(0.0f);
				var status = ResourceLoader.LoadThreadedGetStatus(scenePath, progress);
				if (status == ResourceLoader.ThreadLoadStatus.InProgress && progress.Count > 0)
				{
					return (float)progress[0];
				}
				return 0.0f;
			}
			else if (state.State == LoadState.Loaded)
			{
				return 1.0f;
			}
		}

		return (_instantiateSceneCache.ContainsKey(scenePath) ||
				_tempPreloadedResourceCache.ContainsKey(scenePath) ||
				_fixedPreloadResourceCache.ContainsKey(scenePath)) ? 1.0f : 0.0f;
	}

	public int GetResourceFileSize(string scenePath)
	{
		if (!ResourceLoader.Exists(scenePath))
		{
			return -1;
		}

		using var file = FileAccess.Open(scenePath, FileAccess.ModeFlags.Read);
		if (file == null)
		{
			return -1;
		}

		var size = file.GetLength();
		file.Close();
		return (int)size;
	}

	public string GetResourceFileSizeFormatted(string scenePath)
	{
		var size = GetResourceFileSize(scenePath);
		if (size < 0)
		{
			return "N/A";
		}

		if (size < 1024)
		{
			return $"{size} B";
		}
		if (size < 1024 * 1024)
		{
			return $"{size / 1024.0:F} KB";
		}
		if (size < 1024 * 1024 * 1024)
		{
			return $"{size / (1024.0 * 1024.0):F} MB";
		}
		return $"{size / (1024.0 * 1024.0 * 1024.0):F} GB";
	}

	public Godot.Collections.Dictionary GetResourceInfo(string scenePath)
	{
		var info = new Godot.Collections.Dictionary
		{
			{ "path", scenePath },
			{ "exists", ResourceLoader.Exists(scenePath) },
			{ "file_size_bytes", GetResourceFileSize(scenePath) },
			{ "file_size_formatted", GetResourceFileSizeFormatted(scenePath) },
			{ "in_temp_cache", _tempPreloadedResourceCache.ContainsKey(scenePath) },
			{ "in_fixed_cache", _fixedPreloadResourceCache.ContainsKey(scenePath) },
			{ "in_instance_cache", _instantiateSceneCache.ContainsKey(scenePath) },
			{ "is_preloading", IsScenePreloading(scenePath) },
			{ "loading_progress", GetLoadingProgress(scenePath) }
		};

		if (_preloadResourceStates.TryGetValue(scenePath, out var state))
		{
			info["preload_state"] = (int)state.State;
			info["is_fixed_preload"] = state.Fixed;
		}

		return info;
	}

	public bool IsInFixedCache(string scenePath)
	{
		return _fixedPreloadResourceCache.ContainsKey(scenePath);
	}

	public void PrintDebugInfo()
	{
		GD.Print("\n=== SceneManager Debug Info ===");
		GD.Print($"Current scene: {(_currentScene != null ? _currentScenePath : "None")}");
		GD.Print($"Previous scene: {_previousScenePath}");

		GD.Print($"\n[Instance Cache] Count: {_instantiateSceneCache.Count}/{_maxCacheSize}");
		GD.Print($"  Access order: {string.Join(", ", _instantiateSceneCacheOrder)}");
		GD.Print($"  Scenes: {string.Join(", ", _instantiateSceneCache.Keys)}");

		GD.Print($"\n[Temp Preload Cache] Count: {_tempPreloadedResourceCache.Count}/{_maxTempPreloadResourceCacheSize}");
		GD.Print($"  Access order: {string.Join(", ", _tempPreloadedResourceCacheOrder)}");
		GD.Print($"  Scenes: {string.Join(", ", _tempPreloadedResourceCache.Keys)}");

		GD.Print($"\n[Fixed Preload Cache] Count: {_fixedPreloadResourceCache.Count}/{_maxFixedPreloadResourceCacheSize}");
		GD.Print($"  Access order: {string.Join(", ", _fixedPreloadResourceCacheOrder)}");
		GD.Print($"  Scenes: {string.Join(", ", _fixedPreloadResourceCache.Keys)}");

		GD.Print($"\n[Preload States] Count: {_preloadResourceStates.Count}");
		foreach (var path in _preloadResourceStates.Keys)
		{
			var stateInfo = _preloadResourceStates[path];
			GD.Print($"  {path} -> {(int)stateInfo.State} | fixed: {stateInfo.Fixed} | has_resource: {stateInfo.Resource != null}");
		}

		GD.Print($"\nDefault loading screen: {(_defaultLoadScreen != null ? "Loaded" : "Not loaded")}");
		GD.Print($"Active loading screen: {(_activeLoadScreen != null ? "Yes" : "No")}");
		GD.Print($"Using asynchronous loading: {_useAsyncLoading}");
		GD.Print($"Always use default loading screen: {_alwaysUseDefaultLoadScreen}");
		GD.Print("===============================\n");
	}

	public void ConnectAllSignals(Object target)
	{
		if (target == null)
		{
			return;
		}

		var signalsList = GetSignalList();
		foreach (var signalInfo in signalsList)
		{
			var signalName = (string)signalInfo["name"];
			var methodName = "_on_scene_manager_" + signalName;
			if (target is Node nodeTarget && nodeTarget.HasMethod(methodName))
			{
				Connect(signalName, new Callable(nodeTarget, methodName));
				GD.Print($"[SceneManager] Connecting signal: {signalName} -> {methodName}");
			}
		}
	}

	private void InitDefaultLoadScreen()
	{
		GD.Print("[SceneManager] Initializing default loading screen");

		if (ResourceLoader.Exists(DefaultLoadScreenPath))
		{
			var loadScreenScene = GD.Load<PackedScene>(DefaultLoadScreenPath);
			if (loadScreenScene != null)
			{
				_defaultLoadScreen = loadScreenScene.Instantiate();
				AddChild(_defaultLoadScreen);

				if (_defaultLoadScreen is CanvasItem canvasItem)
				{
					canvasItem.Visible = false;
				}
				else if (_defaultLoadScreen.HasMethod("set_visible"))
				{
					_defaultLoadScreen.Set("visible", false);
				}

				GD.Print("[SceneManager] Default loading screen loaded successfully");
				return;
			}
		}

		GD.Print("[SceneManager] Warning: Default loading screen file does not exist, creating simple version");
		_defaultLoadScreen = CreateSimpleLoadScreen();
		AddChild(_defaultLoadScreen);

		if (_defaultLoadScreen is CanvasItem canvasItem2)
		{
			canvasItem2.Visible = false;
		}

		GD.Print("[SceneManager] Simple loading screen creation completed");
	}

	private Node CreateSimpleLoadScreen()
	{
		var canvasLayer = new CanvasLayer();
		canvasLayer.Name = "SimpleLoadScreen";
		canvasLayer.Layer = 1000;

		var colorRect = new ColorRect();
		colorRect.Color = new Color(0, 0, 0, 1);
		colorRect.Size = GetViewport().GetVisibleRect().Size;
		colorRect.AnchorLeft = 0;
		colorRect.AnchorTop = 0;
		colorRect.AnchorRight = 1;
		colorRect.AnchorBottom = 1;
		colorRect.MouseFilter = Control.MouseFilterEnum.Stop;

		var label = new Label();
		label.Text = "Loading...";
		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.VerticalAlignment = VerticalAlignment.Center;
		label.AddThemeFontSizeOverride("font_size", 32);
		label.AddThemeColorOverride("font_color", Colors.White);

		canvasLayer.AddChild(colorRect);
		colorRect.AddChild(label);

		label.AnchorLeft = 0.5f;
		label.AnchorTop = 0.5f;
		label.AnchorRight = 0.5f;
		label.AnchorBottom = 0.5f;
		label.Position = new Vector2(-50, -16);
		label.Size = new Vector2(100, 32);

		return canvasLayer;
	}

	private PreloadResourceState GetPreloadResourceState(string scenePath)
	{
		if (!_preloadResourceStates.TryGetValue(scenePath, out var state))
		{
			state = new PreloadResourceState();
			_preloadResourceStates[scenePath] = state;
		}
		return state;
	}

	private void ClearPreloadResourceState(string scenePath)
	{
		_preloadResourceStates.Remove(scenePath);
	}

	private async void PreloadBackground(string scenePath)
	{
		if (_useAsyncLoading)
		{
			await AsyncPreloadScene(scenePath);
		}
		else
		{
			SyncPreloadScene(scenePath);
		}

		if (!_preloadResourceStates.TryGetValue(scenePath, out var preloadState))
		{
			GD.Print($"[SceneManager] Preload state cleared: {scenePath}");
			return;
		}

		if (preloadState.State == LoadState.Cancelled)
		{
			GD.Print($"[SceneManager] Preload was cancelled: {scenePath}");
			ClearPreloadResourceState(scenePath);
			return;
		}

		if (preloadState.State != LoadState.Loading)
		{
			GD.Print($"[SceneManager] Preload state changed unexpectedly: {scenePath}");
			return;
		}

		if (preloadState.Resource == null)
		{
			preloadState.State = LoadState.NotLoaded;
			preloadState.Resource = null;
			ClearPreloadResourceState(scenePath);
			EmitSignal(SignalName.ScenePreloadFailed, scenePath);
			GD.Print($"[SceneManager] Preloading failed: {scenePath}");
			return;
		}

		var isFixed = preloadState.Fixed;

		if (isFixed)
		{
			if (_fixedPreloadResourceCacheOrder.Count >= _maxFixedPreloadResourceCacheSize && _maxFixedPreloadResourceCacheSize > 0)
			{
				RemoveOldestFixedPreloadResource();
			}
			_fixedPreloadResourceCache[scenePath] = preloadState.Resource;
			_fixedPreloadResourceCacheOrder.Add(scenePath);
			GD.Print($"[SceneManager] Preloading complete, fixed resource cached: {scenePath}");
		}
		else
		{
			if (_tempPreloadedResourceCacheOrder.Count >= _maxTempPreloadResourceCacheSize)
			{
				RemoveOldestTempPreloadResource();
			}
			_tempPreloadedResourceCache[scenePath] = preloadState.Resource;
			_tempPreloadedResourceCacheOrder.Add(scenePath);
			GD.Print($"[SceneManager] Preloading complete, temp resource cached: {scenePath}");
		}

		preloadState.State = LoadState.Loaded;
		EmitSignal(SignalName.ScenePreloadCompleted, scenePath);
	}

	private async Task AsyncPreloadScene(string scenePath)
	{
		GD.Print($"[SceneManager] Asynchronous preload: {scenePath}");

		var loadStartTime = Time.GetTicksMsec();
		ResourceLoader.LoadThreadedRequest(scenePath, "", false, ResourceLoader.CacheMode.Ignore);

		while (true)
		{
			var status = ResourceLoader.LoadThreadedGetStatus(scenePath);

			switch (status)
			{
				case ResourceLoader.ThreadLoadStatus.InProgress:
					if (Time.GetTicksMsec() - loadStartTime > 500)
					{
						var progress = new Godot.Collections.Array();
						progress.Add(0.0f);
						ResourceLoader.LoadThreadedGetStatus(scenePath, progress);
						if (progress.Count > 0)
						{
							GD.Print($"[SceneManager] Asynchronous loading progress: {(float)progress[0] * 100}%");
						}
						loadStartTime = Time.GetTicksMsec();
					}

					await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
					break;

				case ResourceLoader.ThreadLoadStatus.Loaded:
					var preloadState = GetPreloadResourceState(scenePath);
					preloadState.Resource = ResourceLoader.LoadThreadedGet(scenePath);
					GD.Print($"[SceneManager] Asynchronous preload completed: {scenePath}");
					return;

				case ResourceLoader.ThreadLoadStatus.Failed:
					GD.PushError($"[SceneManager] Asynchronous loading failed: {scenePath}");
					var preloadStateFailed = GetPreloadResourceState(scenePath);
					preloadStateFailed.Resource = null;
					return;

				default:
					GD.PushError($"[SceneManager] Unknown loading status: {status}");
					var preloadStateUnknown = GetPreloadResourceState(scenePath);
					preloadStateUnknown.Resource = null;
					return;
			}
		}
	}

	private void SyncPreloadScene(string scenePath)
	{
		GD.Print($"[SceneManager] Synchronous preload: {scenePath}");
		var preloadState = GetPreloadResourceState(scenePath);
		preloadState.Resource = GD.Load(scenePath);
	}

	private Node GetLoadScreenInstance(string loadScreenPath)
	{
		if (loadScreenPath == "" || loadScreenPath == "default")
		{
			return _defaultLoadScreen;
		}

		if (loadScreenPath == "no_transition")
		{
			return null;
		}

		if (ResourceLoader.Exists(loadScreenPath))
		{
			var scene = GD.Load<PackedScene>(loadScreenPath);
			if (scene != null)
			{
				var customScreen = scene.Instantiate();
				AddChild(customScreen);
				if (customScreen is CanvasItem canvasItem)
				{
					canvasItem.Visible = false;
				}
				else if (customScreen.HasMethod("set_visible"))
				{
					customScreen.Set("visible", false);
				}
				return customScreen;
			}
		}

		return _defaultLoadScreen;
	}

	private async Task ShowLoadScreen(Node loadScreenInstance)
	{
		if (loadScreenInstance == null)
		{
			GD.Print("[SceneManager] No loading screen, switching directly");
			return;
		}

		_activeLoadScreen = loadScreenInstance;

		if (loadScreenInstance is CanvasItem canvasItem)
		{
			canvasItem.Visible = true;
		}
		else if (loadScreenInstance.HasMethod("show"))
		{
			loadScreenInstance.Call("show");
		}
		else if (loadScreenInstance.HasMethod("set_visible"))
		{
			loadScreenInstance.Set("visible", true);
		}

		if (loadScreenInstance.HasMethod("fade_in"))
		{
			GD.Print("[SceneManager] Calling loading screen fade-in effect");
			loadScreenInstance.CallDeferred("fade_in");
			await ToSignal(loadScreenInstance, "fade_in_completed");
		}
		else if (loadScreenInstance.HasMethod("show_loading"))
		{
			await ToSignal(loadScreenInstance, "show_loading_completed");
		}

		EmitSignal(SignalName.LoadScreenShown, loadScreenInstance);
		GD.Print("[SceneManager] Loading screen display completed");
	}

	private async Task HideLoadScreen(Node loadScreenInstance)
	{
		if (loadScreenInstance == null)
		{
			return;
		}

		if (loadScreenInstance.HasMethod("fade_out"))
		{
			GD.Print("[SceneManager] Calling loading screen fade-out effect");
			loadScreenInstance.CallDeferred("fade_out");
			await ToSignal(loadScreenInstance, "fade_out_completed");
		}
		else if (loadScreenInstance.HasMethod("hide_loading"))
		{
			await ToSignal(loadScreenInstance, "hide_loading_completed");
		}
		else if (loadScreenInstance is CanvasItem canvasItem)
		{
			canvasItem.Visible = false;
		}
		else if (loadScreenInstance.HasMethod("hide"))
		{
			loadScreenInstance.Call("hide");
		}
		else if (loadScreenInstance.HasMethod("set_visible"))
		{
			loadScreenInstance.Set("visible", false);
		}

		_activeLoadScreen = null;
		EmitSignal(SignalName.LoadScreenHidden, loadScreenInstance);

		if (loadScreenInstance != _defaultLoadScreen && loadScreenInstance.GetParent() == this)
		{
			RemoveChild(loadScreenInstance);
			loadScreenInstance.QueueFree();
			GD.Print("[SceneManager] Cleaning up custom loading screen");
		}
	}

	private async Task LoadSceneByMethod(string scenePath, object loadMethod, bool cacheCurrentScene, Node loadScreenInstance)
	{
		await ShowLoadScreen(loadScreenInstance);

		int methodIndex = loadMethod switch
		{
			null => (int)LoadMethod.BothPreloadFirst,
			LoadMethod lm => (int)lm,
			int i => i,
			string s when s.ToUpper() == "DIRECT" => 0,
			string s when s.ToUpper() == "PRELOAD_CACHE" => 1,
			string s when s.ToUpper() == "SCENE_CACHE" => 2,
			string s when s.ToUpper() == "BOTH_PRELOAD_FIRST" => 3,
			string s when s.ToUpper() == "BOTH_INSTANCE_FIRST" => 4,
			_ => -1
		};

		switch (methodIndex)
		{
			case 0:
				if (_tempPreloadedResourceCache.ContainsKey(scenePath) || _fixedPreloadResourceCache.ContainsKey(scenePath))
				{
					GD.Print("[SceneManager] DIRECT: resource found in preload cache, using preloaded resource");
					await HandlePreloadedResource(scenePath, loadScreenInstance, cacheCurrentScene);
				}
				else
				{
					GD.Print("[SceneManager] DIRECT: resource not in any cache, using async loading");
					await LoadAndSwitch(scenePath, loadScreenInstance, cacheCurrentScene);
				}
				break;

			case 1:
				if (_tempPreloadedResourceCache.ContainsKey(scenePath) || _fixedPreloadResourceCache.ContainsKey(scenePath))
				{
					await HandlePreloadedResource(scenePath, loadScreenInstance, cacheCurrentScene);
				}
				else
				{
					GD.Print("[SceneManager] PRELOAD_CACHE: resource not in preload cache, falling back to direct load");
					await LoadAndSwitch(scenePath, loadScreenInstance, cacheCurrentScene);
				}
				break;

			case 2:
				if (_instantiateSceneCache.ContainsKey(scenePath))
				{
					await HandleCachedScene(scenePath, loadScreenInstance, cacheCurrentScene);
				}
				else
				{
					GD.Print("[SceneManager] SCENE_CACHE: scene not in instance cache, falling back to direct load");
					await LoadAndSwitch(scenePath, loadScreenInstance, cacheCurrentScene);
				}
				break;

			case 3:
				if (_tempPreloadedResourceCache.ContainsKey(scenePath) || _fixedPreloadResourceCache.ContainsKey(scenePath))
				{
					await HandlePreloadedResource(scenePath, loadScreenInstance, cacheCurrentScene);
				}
				else if (_instantiateSceneCache.ContainsKey(scenePath))
				{
					await HandleCachedScene(scenePath, loadScreenInstance, cacheCurrentScene);
				}
				else
				{
					await HandlePreloadingScene(scenePath, loadScreenInstance, cacheCurrentScene);
				}
				break;

			case 4:
				if (_instantiateSceneCache.ContainsKey(scenePath))
				{
					await HandleCachedScene(scenePath, loadScreenInstance, cacheCurrentScene);
				}
				else if (_tempPreloadedResourceCache.ContainsKey(scenePath) || _fixedPreloadResourceCache.ContainsKey(scenePath))
				{
					await HandlePreloadedResource(scenePath, loadScreenInstance, cacheCurrentScene);
				}
				else
				{
					await HandlePreloadingScene(scenePath, loadScreenInstance, cacheCurrentScene);
				}
				break;

			default:
				GD.PushError($"[SceneManager] Error: Unknown load method: {loadMethod}");
				await HideLoadScreen(loadScreenInstance);
				EmitSignal(SignalName.SceneSwitchFailed, scenePath);
				break;
		}
	}

	private async Task HandlePreloadedResource(string scenePath, Node loadScreenInstance, bool useCache)
	{
		GD.Print($"[SceneManager] Handling preloaded resource: {scenePath}");
		await InstantiateAndSwitch(scenePath, loadScreenInstance, useCache);
	}

	private async Task HandlePreloadingScene(string scenePath, Node loadScreenInstance, bool useCache)
	{
		GD.Print($"[SceneManager] Handling preloading scene: {scenePath}");

		var progressArray = new Godot.Collections.Array();
		progressArray.Add(0.0f);
		var loadStartTime = Time.GetTicksMsec();

		ResourceLoader.LoadThreadedRequest(scenePath, "", false, ResourceLoader.CacheMode.Ignore);

		while (true)
		{
			var status = ResourceLoader.LoadThreadedGetStatus(scenePath, progressArray);

			switch (status)
			{
				case ResourceLoader.ThreadLoadStatus.InProgress:
					var progress = (float)progressArray[0];
					if (loadScreenInstance != null && loadScreenInstance.HasMethod("set_progress"))
					{
						loadScreenInstance.Call("set_progress", progress);
					}
					else if (loadScreenInstance != null && loadScreenInstance.HasMethod("update_progress"))
					{
						loadScreenInstance.Call("update_progress", progress);
					}

					if (Time.GetTicksMsec() - loadStartTime > 500)
					{
						GD.Print($"[SceneManager] Preload scene loading progress: {progress * 100}%");
						loadStartTime = Time.GetTicksMsec();
					}

					await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
					break;

				case ResourceLoader.ThreadLoadStatus.Loaded:
					GD.Print($"[SceneManager] Preload scene loading completed: {scenePath}");
					break;

				case ResourceLoader.ThreadLoadStatus.Failed:
					GD.PushError($"[SceneManager] Scene loading failed: {scenePath}");
					await HideLoadScreen(loadScreenInstance);
					EmitSignal(SignalName.SceneSwitchFailed, scenePath);
					return;

				default:
					GD.PushError($"[SceneManager] Unknown loading status: {status}");
					await HideLoadScreen(loadScreenInstance);
					EmitSignal(SignalName.SceneSwitchFailed, scenePath);
					return;
			}

			if (status == ResourceLoader.ThreadLoadStatus.Loaded)
			{
				break;
			}
		}

		var packedScene = ResourceLoader.LoadThreadedGet(scenePath);
		if (packedScene == null)
		{
			GD.PushError($"[SceneManager] Scene resource retrieval failed: {scenePath}");
			await HideLoadScreen(loadScreenInstance);
			EmitSignal(SignalName.SceneSwitchFailed, scenePath);
			return;
		}

		GD.Print($"[SceneManager] Instantiating scene: {scenePath}");
		var newScene = await InstantiateSceneDeferred((PackedScene)packedScene, loadScreenInstance);
		if (newScene == null)
		{
			GD.PushError($"[SceneManager] Scene instantiation failed: {scenePath}");
			await HideLoadScreen(loadScreenInstance);
			EmitSignal(SignalName.SceneSwitchFailed, scenePath);
			return;
		}

		await PerformSceneSwitch(newScene, scenePath, loadScreenInstance, useCache);
	}

	private async Task HandleCachedScene(string scenePath, Node loadScreenInstance, bool cacheCurrentScene)
	{
		GD.Print($"[SceneManager] Handling cached scene: {scenePath}");
		await SwitchToCachedScene(scenePath, loadScreenInstance, cacheCurrentScene);
	}

	private async Task LoadAndSwitch(string scenePath, Node loadScreenInstance, bool currentSceneUseCache)
	{
		GD.Print($"[SceneManager] Loading scene: {scenePath}");

		ResourceLoader.LoadThreadedRequest(scenePath, "", false, ResourceLoader.CacheMode.Ignore);

		var progressArray = new Godot.Collections.Array();
		progressArray.Add(0.0f);
		var loadStartTime = Time.GetTicksMsec();

		while (true)
		{
			var status = ResourceLoader.LoadThreadedGetStatus(scenePath, progressArray);

			switch (status)
			{
				case ResourceLoader.ThreadLoadStatus.InProgress:
					var progress = (float)progressArray[0];
					if (loadScreenInstance != null && loadScreenInstance.HasMethod("set_progress"))
					{
						loadScreenInstance.Call("set_progress", progress);
					}
					else if (loadScreenInstance != null && loadScreenInstance.HasMethod("update_progress"))
					{
						loadScreenInstance.Call("update_progress", progress);
					}

					if (Time.GetTicksMsec() - loadStartTime > 500)
					{
						GD.Print($"[SceneManager] Direct load progress: {progress * 100}%");
						loadStartTime = Time.GetTicksMsec();
					}

					await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
					break;

				case ResourceLoader.ThreadLoadStatus.Loaded:
					GD.Print($"[SceneManager] Direct load completed: {scenePath}");
					break;

				case ResourceLoader.ThreadLoadStatus.Failed:
					GD.PushError($"[SceneManager] Scene loading failed: {scenePath}");
					await HideLoadScreen(loadScreenInstance);
					EmitSignal(SignalName.SceneSwitchFailed, scenePath);
					return;

				default:
					GD.PushError($"[SceneManager] Unknown loading status: {status}");
					await HideLoadScreen(loadScreenInstance);
					EmitSignal(SignalName.SceneSwitchFailed, scenePath);
					return;
			}

			if (status == ResourceLoader.ThreadLoadStatus.Loaded)
			{
				break;
			}
		}

		var newSceneResource = ResourceLoader.LoadThreadedGet(scenePath);
		if (newSceneResource == null)
		{
			GD.PushError($"[SceneManager] Scene resource retrieval failed: {scenePath}");
			await HideLoadScreen(loadScreenInstance);
			EmitSignal(SignalName.SceneSwitchFailed, scenePath);
			return;
		}

		var newScene = await InstantiateSceneDeferred((PackedScene)newSceneResource, loadScreenInstance);
		if (newScene == null)
		{
			GD.PushError($"[SceneManager] Scene instantiation failed: {scenePath}");
			await HideLoadScreen(loadScreenInstance);
			EmitSignal(SignalName.SceneSwitchFailed, scenePath);
			return;
		}

		await PerformSceneSwitch(newScene, scenePath, loadScreenInstance, currentSceneUseCache);
	}

	private async Task<Node> InstantiateSceneDeferred(PackedScene packedScene, Node loadScreenInstance)
	{
		for (int i = 0; i < _instantiateFrames; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}

		var instance = packedScene.Instantiate();
		if (instance == null)
		{
			GD.PushError("[SceneManager] Scene instantiation failed");
			return null;
		}

		return instance;
	}

	private async Task InstantiateAndSwitch(string scenePath, Node loadScreenInstance, bool useCache)
	{
		PackedScene packedScene;

		if (_tempPreloadedResourceCache.TryGetValue(scenePath, out var resource))
		{
			packedScene = (PackedScene)resource;
			_tempPreloadedResourceCache.Remove(scenePath);
			var index = _tempPreloadedResourceCacheOrder.IndexOf(scenePath);
			if (index != -1)
			{
				_tempPreloadedResourceCacheOrder.RemoveAt(index);
			}
		}
		else if (_fixedPreloadResourceCache.TryGetValue(scenePath, out resource))
		{
			packedScene = (PackedScene)resource;
			GD.Print($"[SceneManager] Using from fixed cache (copy mode): {scenePath}");
		}
		else
		{
			GD.PushError($"[SceneManager] Preloaded resource does not exist: {scenePath}");
			await HideLoadScreen(loadScreenInstance);
			EmitSignal(SignalName.SceneSwitchFailed, scenePath);
			return;
		}

		GD.Print($"[SceneManager] Instantiating preloaded scene: {scenePath}");

		var newScene = await InstantiateSceneDeferred(packedScene, loadScreenInstance);
		if (newScene == null)
		{
			GD.PushError("[SceneManager] Scene instantiation failed");
			await HideLoadScreen(loadScreenInstance);
			EmitSignal(SignalName.SceneSwitchFailed, scenePath);
			return;
		}

		await PerformSceneSwitch(newScene, scenePath, loadScreenInstance, useCache);
	}

	private async Task SwitchToCachedScene(string scenePath, Node loadScreenInstance, bool cacheCurrentScene)
	{
		if (!_instantiateSceneCache.TryGetValue(scenePath, out var cached))
		{
			GD.PushError($"[SceneManager] Scene not found in cache: {scenePath}");
			await HideLoadScreen(loadScreenInstance);
			return;
		}

		if (!IsInstanceValid(cached.SceneInstance))
		{
			GD.PushError("[SceneManager] Cached scene instance is invalid");
			_instantiateSceneCache.Remove(scenePath);
			var index = _instantiateSceneCacheOrder.IndexOf(scenePath);
			if (index != -1)
			{
				_instantiateSceneCacheOrder.RemoveAt(index);
			}
			await HideLoadScreen(loadScreenInstance);
			return;
		}

		GD.Print($"[SceneManager] Using cached scene: {scenePath}");

		var sceneInstance = cached.SceneInstance;

		_instantiateSceneCache.Remove(scenePath);
		var orderIndex = _instantiateSceneCacheOrder.IndexOf(scenePath);
		if (orderIndex != -1)
		{
			_instantiateSceneCacheOrder.RemoveAt(orderIndex);
		}

		if (sceneInstance.IsInsideTree())
		{
			sceneInstance.GetParent()?.RemoveChild(sceneInstance);
		}

		await PerformSceneSwitch(sceneInstance, scenePath, loadScreenInstance, cacheCurrentScene);
	}

	private async Task PerformSceneSwitch(Node newScene, string newScenePath, Node loadScreenInstance, bool currentSceneUseCache)
	{
		GD.Print($"[SceneManager] Performing scene switch to: {newScenePath}");

		var oldScene = _currentScene;
		var oldScenePath = _currentScenePath;

		_previousScenePath = _currentScenePath;
		_currentScene = newScene;
		_currentScenePath = newScenePath;

		if (oldScene != null && oldScene != newScene)
		{
			GD.Print($"[SceneManager] Removing current scene: {oldScene.Name}");

			if (oldScene.IsInsideTree())
			{
				oldScene.GetParent()?.RemoveChild(oldScene);
			}

			if (currentSceneUseCache && oldScenePath != "" && oldScenePath != newScenePath)
			{
				AddToCache(oldScenePath, oldScene);
			}
			else
			{
				CleanupOrphanedNodes(oldScene);
				oldScene.QueueFree();
			}
		}

		GD.Print($"[SceneManager] Adding new scene: {newScene.Name}");

		if (newScene.IsInsideTree())
		{
			newScene.GetParent()?.RemoveChild(newScene);
		}

		GetTree().Root.AddChild(newScene);
		GetTree().CurrentScene = newScene;

		if (!newScene.IsNodeReady())
		{
			GD.Print("[SceneManager] Waiting for new scene to be ready...");
			await ToSignal(newScene, Node.SignalName.Ready);
		}

		await HideLoadScreen(loadScreenInstance);

		DebugValidateSceneTree();

		EmitSignal(SignalName.SceneSwitchCompleted, newScenePath);
		GD.Print($"[SceneManager] Scene switching completed: {newScenePath}");
	}

	private void AddToCache(string scenePath, Node sceneInstance)
	{
		if (scenePath == "" || sceneInstance == null)
		{
			GD.Print("[SceneManager] Warning: Cannot cache empty scene or path");
			return;
		}

		if (_instantiateSceneCache.TryGetValue(scenePath, out var oldCached))
		{
			GD.Print($"[SceneManager] Scene already in instance cache: {scenePath}");
			if (IsInstanceValid(oldCached.SceneInstance))
			{
				CleanupOrphanedNodes(oldCached.SceneInstance);
				oldCached.SceneInstance.QueueFree();
			}
			_instantiateSceneCache.Remove(scenePath);
			var index = _instantiateSceneCacheOrder.IndexOf(scenePath);
			if (index != -1)
			{
				_instantiateSceneCacheOrder.RemoveAt(index);
			}
		}

		CleanupOrphanedNodes(sceneInstance);

		if (sceneInstance.IsInsideTree())
		{
			GD.PushError("[SceneManager] Error: Attempting to cache node still in scene tree");
			sceneInstance.GetParent()?.RemoveChild(sceneInstance);
		}

		GD.Print($"[SceneManager] Adding to instance cache: {scenePath}");

		var cached = new CachedScene(sceneInstance);
		_instantiateSceneCache[scenePath] = cached;
		_instantiateSceneCacheOrder.Add(scenePath);
		EmitSignal(SignalName.SceneCached, scenePath);

		if (_preloadResourceStates.TryGetValue(scenePath, out var state))
		{
			state.State = LoadState.Instantiated;
		}

		if (_instantiateSceneCacheOrder.Count > _maxCacheSize)
		{
			RemoveOldestCachedScene();
		}
	}

	private void RemoveOldestCachedScene()
	{
		if (_instantiateSceneCacheOrder.Count == 0)
		{
			return;
		}

		var oldestPath = _instantiateSceneCacheOrder[0];
		_instantiateSceneCacheOrder.RemoveAt(0);

		if (_instantiateSceneCache.TryGetValue(oldestPath, out var cached))
		{
			if (IsInstanceValid(cached.SceneInstance))
			{
				CleanupOrphanedNodes(cached.SceneInstance);
				cached.SceneInstance.QueueFree();
			}
			_instantiateSceneCache.Remove(oldestPath);
			EmitSignal(SignalName.SceneRemovedFromCache, oldestPath);
			GD.Print($"[SceneManager] Removing old cache: {oldestPath}");
		}

		ClearPreloadResourceState(oldestPath);
	}

	private void RemoveOldestTempPreloadResource()
	{
		if (_tempPreloadedResourceCacheOrder.Count == 0)
		{
			return;
		}

		var oldestPath = _tempPreloadedResourceCacheOrder[0];
		_tempPreloadedResourceCacheOrder.RemoveAt(0);

		if (_tempPreloadedResourceCache.Remove(oldestPath))
		{
			EmitSignal(SignalName.SceneRemovedFromCache, oldestPath);
			GD.Print($"[SceneManager] Removing old temp preload resource: {oldestPath}");
		}
	}

	private void RemoveOldestFixedPreloadResource()
	{
		if (_fixedPreloadResourceCacheOrder.Count == 0)
		{
			return;
		}

		var oldestPath = _fixedPreloadResourceCacheOrder[0];
		_fixedPreloadResourceCacheOrder.RemoveAt(0);

		if (_fixedPreloadResourceCache.Remove(oldestPath))
		{
			EmitSignal(SignalName.SceneRemovedFromCache, oldestPath);
			GD.Print($"[SceneManager] Removing oldest fixed preload resource (FIFO): {oldestPath}");
		}
	}

	private void CleanupOrphanedNodes(Node rootNode)
	{
		if (rootNode == null || !IsInstanceValid(rootNode))
		{
			return;
		}

		if (rootNode.IsInsideTree())
		{
			var parent = rootNode.GetParent();
			parent?.RemoveChild(rootNode);
		}

		foreach (var child in rootNode.GetChildren())
		{
			CleanupOrphanedNodes(child);
		}
	}

	private void DebugValidateSceneTree()
	{
		var root = GetTree().Root;
		var current = GetTree().CurrentScene;

		GD.Print($"[SceneManager] Scene tree validation - Root node child count: {root.GetChildCount()}");
		GD.Print($"[SceneManager] Current scene: {(current != null ? current.Name : "None")}");

		foreach (var scenePath in _instantiateSceneCache.Keys)
		{
			var cached = _instantiateSceneCache[scenePath];
			if (IsInstanceValid(cached.SceneInstance) && cached.SceneInstance.IsInsideTree())
			{
				GD.PushError($"[SceneManager] Error: Cached node still in scene tree: {scenePath}");
			}
		}
	}

	private Godot.Collections.Array CreateArrayFromList(List<string> list)
	{
		var array = new Godot.Collections.Array();
		foreach (var item in list)
		{
			array.Add(item);
		}
		return array;
	}
}

using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using MudBlazor;

namespace Mythetech.Framework.Infrastructure.Plugins;

/// <summary>
/// Responsible for loading plugin assemblies and discovering plugin components
/// </summary>
public class PluginLoader
{
    private readonly ILogger<PluginLoader> _logger;
    private readonly PluginState _pluginState;

    /// <summary>
    /// Constructor
    /// </summary>
    public PluginLoader(ILogger<PluginLoader> logger, PluginState pluginState)
    {
        _logger = logger;
        _pluginState = pluginState;
    }
    
    /// <summary>
    /// Load a plugin from a DLL file path.
    /// Uses a custom AssemblyLoadContext to resolve dependencies from the plugin's directory.
    /// The load context is stored in the PluginInfo to enable unloading when the plugin is removed.
    /// </summary>
    /// <param name="dllPath">Full path to the plugin DLL</param>
    /// <returns>PluginInfo if loaded successfully, null otherwise</returns>
    public PluginInfo? LoadPlugin(string dllPath)
    {
        if (!File.Exists(dllPath))
        {
            _logger.LogWarning("Plugin DLL not found: {Path}", dllPath);
            return null;
        }

        try
        {
            var fullPath = Path.GetFullPath(dllPath);
            var pluginDirectory = Path.GetDirectoryName(fullPath)!;

            PreloadDependencies(pluginDirectory, Path.GetFileName(fullPath));

            var loadContext = new PluginLoadContext(fullPath);
            var assembly = loadContext.LoadFromAssemblyPath(fullPath);

            return LoadPlugin(assembly, dllPath, loadContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin assembly from {Path}", dllPath);
            return null;
        }
    }
    
    private void PreloadDependencies(string directory, string mainDllName)
    {
        foreach (var dllPath in Directory.GetFiles(directory, "*.dll"))
        {
            var fileName = Path.GetFileName(dllPath);
            
            // Skip the main plugin DLL and any already-loaded assemblies
            if (fileName.Equals(mainDllName, StringComparison.OrdinalIgnoreCase))
                continue;
            
            try
            {
                // Check if already loaded in default context
                var assemblyName = AssemblyName.GetAssemblyName(dllPath);
                var existingAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == assemblyName.Name);
                
                if (existingAssembly != null)
                {
                    _logger.LogDebug("Dependency {Name} already loaded, skipping", assemblyName.Name);
                    continue;
                }
                
                // Load into default context so it's available to all plugins
                AssemblyLoadContext.Default.LoadFromAssemblyPath(dllPath);
                _logger.LogDebug("Pre-loaded dependency: {Name}", assemblyName.Name);
            }
            catch (BadImageFormatException)
            {
                // Not a .NET assembly, skip
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not pre-load dependency: {Path}", dllPath);
            }
        }
    }
    
    /// <summary>
    /// Load a plugin from an already-loaded assembly (useful for testing or embedded plugins)
    /// </summary>
    /// <param name="assembly">The plugin assembly</param>
    /// <param name="sourcePath">Optional source path for reference</param>
    /// <param name="loadContext">Optional load context for unloading support</param>
    /// <returns>PluginInfo if valid plugin, null otherwise</returns>
    public PluginInfo? LoadPlugin(Assembly assembly, string? sourcePath = null, AssemblyLoadContext? loadContext = null)
    {
        try
        {
            var manifest = DiscoverManifest(assembly);
            if (manifest is null)
            {
                _logger.LogWarning("No IPluginManifest implementation found in assembly {Assembly}", assembly.FullName);
                return null;
            }

            var menuComponents = DiscoverComponentsOfType<Components.PluginMenu>(assembly);
            var contextPanelComponents = DiscoverComponentsOfType<Components.PluginContextPanel>(assembly);

            var menuMetadata = ExtractMenuMetadata(menuComponents);
            var panelMetadata = ExtractContextPanelMetadata(contextPanelComponents);

            var pluginInfo = new PluginInfo
            {
                Manifest = manifest,
                Assembly = assembly,
                SourcePath = sourcePath,
                LoadContext = loadContext,
                MenuComponents = menuComponents,
                ContextPanelComponents = contextPanelComponents,
                MenuComponentsMetadata = menuMetadata,
                ContextPanelComponentsMetadata = panelMetadata
            };

            _logger.LogInformation(
                "Loaded plugin '{Name}' v{Version} by {Developer} with {MenuCount} menu and {PanelCount} panel components",
                manifest.Name, manifest.Version, manifest.Developer,
                menuComponents.Count, contextPanelComponents.Count);

            return pluginInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover plugin from assembly {Assembly}", assembly.FullName);
            return null;
        }
    }
    
    /// <summary>
    /// Load all plugins from a directory.
    /// Scans both the root directory and subdirectories (each plugin can have its own folder).
    /// First pre-loads all DLLs as dependencies, then discovers which are actual plugins.
    /// </summary>
    /// <param name="pluginDirectory">Directory containing plugin DLLs or plugin subdirectories</param>
    /// <returns>List of successfully loaded plugins</returns>
    public IReadOnlyList<PluginInfo> LoadPluginsFromDirectory(string pluginDirectory)
    {
        var plugins = new List<PluginInfo>();
        
        if (!Directory.Exists(pluginDirectory))
        {
            _logger.LogDebug("Plugin directory does not exist: {Path}", pluginDirectory);
            return plugins;
        }

        // Collect all plugin directories to scan:
        // 1. The root plugins directory (for loose DLLs)
        // 2. Each subdirectory (for plugins with their own folder)
        var directoriesToScan = new List<string> { pluginDirectory };
        directoriesToScan.AddRange(Directory.GetDirectories(pluginDirectory));
        
        _logger.LogDebug("Scanning {Count} directories for plugins", directoriesToScan.Count);

        foreach (var directory in directoriesToScan)
        {
            var loadedPlugins = LoadPluginsFromSingleDirectory(directory);
            plugins.AddRange(loadedPlugins);
        }
        
        _logger.LogInformation("Loaded {Count} plugins from {Path}", plugins.Count, pluginDirectory);
        return plugins;
    }
    
    private List<PluginInfo> LoadPluginsFromSingleDirectory(string directory)
    {
        var plugins = new List<PluginInfo>();
        var allDlls = Directory.GetFiles(directory, "*.dll");
        
        if (allDlls.Length == 0)
        {
            return plugins;
        }
        
        _logger.LogDebug("Found {Count} DLLs in {Directory}", allDlls.Length, directory);
        
        // Step 1: Pre-load ALL DLLs as dependencies into the default context
        var loadedAssemblies = new List<(string Path, Assembly Assembly)>();
        foreach (var dllPath in allDlls)
        {
            try
            {
                var fullPath = Path.GetFullPath(dllPath);
                var assemblyName = AssemblyName.GetAssemblyName(fullPath);
                
                // Check if already loaded
                var existing = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == assemblyName.Name);
                
                if (existing != null)
                {
                    loadedAssemblies.Add((fullPath, existing));
                    _logger.LogDebug("Assembly {Name} already loaded", assemblyName.Name);
                }
                else
                {
                    var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(fullPath);
                    loadedAssemblies.Add((fullPath, assembly));
                    _logger.LogDebug("Loaded assembly: {Name}", assemblyName.Name);
                }
            }
            catch (BadImageFormatException)
            {
                // Not a .NET assembly, skip
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not load assembly: {Path}", dllPath);
            }
        }
        
        // Step 2: Find which assemblies are actual plugins (have IPluginManifest)
        foreach (var (path, assembly) in loadedAssemblies)
        {
            var plugin = LoadPlugin(assembly, path);
            if (plugin is not null)
            {
                plugins.Add(plugin);
            }
        }
        
        return plugins;
    }
    
    private IPluginManifest? DiscoverManifest(Assembly assembly)
    {
        var manifestType = assembly.GetTypes()
            .FirstOrDefault(t => 
                !t.IsAbstract && 
                !t.IsInterface && 
                typeof(IPluginManifest).IsAssignableFrom(t));
        
        if (manifestType is null) return null;
        
        var constructor = manifestType.GetConstructor(Type.EmptyTypes);
        if (constructor is null)
        {
            _logger.LogWarning(
                "Plugin manifest type {Type} must have a parameterless constructor", 
                manifestType.FullName);
            return null;
        }
        
        return Activator.CreateInstance(manifestType) as IPluginManifest;
    }
    
    private List<Type> DiscoverComponentsOfType<TBase>(Assembly assembly) where TBase : class
    {
        return assembly.GetTypes()
            .Where(t => 
                !t.IsAbstract && 
                !t.IsInterface && 
                typeof(TBase).IsAssignableFrom(t))
            .ToList();
    }
    
    private List<PluginComponentMetadata> ExtractMenuMetadata(List<Type> menuTypes)
    {
        var metadata = new List<PluginComponentMetadata>();
        
        foreach (var type in menuTypes)
        {
            try
            {
                var meta = ExtractMetadataViaReflection(type);
                if (meta != null)
                {
                    metadata.Add(meta);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract metadata from menu component {Type}", type.FullName);
            }
        }
        
        return metadata;
    }
    
    private List<PluginComponentMetadata> ExtractContextPanelMetadata(List<Type> panelTypes)
    {
        var metadata = new List<PluginComponentMetadata>();
        
        foreach (var type in panelTypes)
        {
            try
            {
                var meta = ExtractMetadataViaReflection(type);
                if (meta != null)
                {
                    metadata.Add(meta);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract metadata from panel component {Type}", type.FullName);
            }
        }
        
        return metadata;
    }
    
    /// <summary>
    /// Extract metadata from a plugin component type.
    /// First checks for PluginComponentMetadataAttribute, then falls back to property reflection.
    /// </summary>
    private PluginComponentMetadata? ExtractMetadataViaReflection(Type componentType)
    {
        string icon = Icons.Material.Filled.Extension;
        string title = componentType.Name;
        int order = 100;
        string? tooltip = null;
        
        var metadataAttr = componentType.GetCustomAttribute<Components.PluginComponentMetadataAttribute>();
        if (metadataAttr is not null)
        {
            _logger.LogDebug("Found PluginComponentMetadataAttribute on {Type}", componentType.Name);
            
            if (!string.IsNullOrEmpty(metadataAttr.Icon))
                icon = metadataAttr.Icon;
            if (!string.IsNullOrEmpty(metadataAttr.Title))
                title = metadataAttr.Title;
            order = metadataAttr.Order;
            tooltip = metadataAttr.Tooltip;
        }
        else
        {
            _logger.LogDebug("No attribute found on {Type}, attempting property reflection", componentType.Name);
            
            object? instance = null;
            try
            {
                instance = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(componentType);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not create uninitialized instance of {Type}, using defaults", componentType.Name);
            }
            
            if (instance != null)
            {
                icon = TryGetPropertyValue<string>(componentType, instance, "Icon") ?? icon;
                title = TryGetPropertyValue<string>(componentType, instance, "Title") ?? title;
                order = TryGetPropertyValue<int?>(componentType, instance, "Order") ?? order;
                tooltip = TryGetPropertyValue<string>(componentType, instance, "Tooltip");
            }
        }
        
        return new PluginComponentMetadata
        {
            ComponentType = componentType,
            Title = title,
            Icon = icon,
            Order = order,
            Tooltip = tooltip
        };
    }
    
    private T? TryGetPropertyValue<T>(Type componentType, object instance, string propertyName)
    {
        try
        {
            var prop = componentType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (prop is null)
            {
                _logger.LogDebug("Property {Property} not found on {Type}", propertyName, componentType.Name);
                return default;
            }
            
            var value = prop.GetValue(instance);
            
            if (value is T typedValue)
            {
                return typedValue;
            }
            
            if (value is null)
            {
                _logger.LogDebug("Property {Property} on {Type} returned null", propertyName, componentType.Name);
                return default;
            }
            
            _logger.LogDebug("Property {Property} on {Type} returned {ValueType} instead of {ExpectedType}", 
                propertyName, componentType.Name, value.GetType().Name, typeof(T).Name);
            return default;
        }
        catch (TargetInvocationException ex)
        {
            _logger.LogDebug(ex.InnerException ?? ex, 
                "Property getter {Property} on {Type} threw an exception", propertyName, componentType.Name);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to read {Property} from {Type}", propertyName, componentType.Name);
            return default;
        }
    }
}


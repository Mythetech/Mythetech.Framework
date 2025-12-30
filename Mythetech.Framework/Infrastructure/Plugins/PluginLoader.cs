using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;

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
            
            // Pre-load all DLLs from the plugin directory to ensure dependencies are available
            PreloadDependencies(pluginDirectory, Path.GetFileName(fullPath));
            
            // Use custom load context for dependency resolution
            var loadContext = new PluginLoadContext(fullPath);
            var assembly = loadContext.LoadFromAssemblyPath(fullPath);
            
            return LoadPlugin(assembly, dllPath);
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
    /// <returns>PluginInfo if valid plugin, null otherwise</returns>
    public PluginInfo? LoadPlugin(Assembly assembly, string? sourcePath = null)
    {
        try
        {
            var manifest = DiscoverManifest(assembly);
            if (manifest is null)
            {
                _logger.LogWarning("No IPluginManifest implementation found in assembly {Assembly}", assembly.FullName);
                return null;
            }
            
            if (_pluginState.GetPlugin(manifest.Id) is not null)
            {
                _logger.LogWarning("Plugin with ID '{Id}' is already loaded", manifest.Id);
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
    /// Extract metadata using reflection to avoid triggering OnInitialized.
    /// Reads Icon, Title, Order, Tooltip properties directly from the type.
    /// </summary>
    private PluginComponentMetadata? ExtractMetadataViaReflection(Type componentType)
    {
        // Get property values via reflection without instantiating
        // These are virtual properties with default implementations, so we need to create
        // an instance but use FormatterServices to skip the constructor
        object? instance = null;
        
        try
        {
            // Use RuntimeHelpers.GetUninitializedObject to create instance without calling constructor
            // This avoids triggering OnInitialized and accessing injected services
            instance = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(componentType);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not create uninitialized instance of {Type}, using defaults", componentType.Name);
        }
        
        string icon = "extension"; // Default icon
        string title = componentType.Name;
        int order = 100;
        string? tooltip = null;
        
        if (instance != null)
        {
            try
            {
                var iconProp = componentType.GetProperty("Icon");
                if (iconProp?.GetValue(instance) is string iconValue)
                    icon = iconValue;
            }
            catch { /* Use default */ }
            
            try
            {
                var titleProp = componentType.GetProperty("Title");
                if (titleProp?.GetValue(instance) is string titleValue)
                    title = titleValue;
            }
            catch { /* Use default */ }
            
            try
            {
                var orderProp = componentType.GetProperty("Order");
                if (orderProp?.GetValue(instance) is int orderValue)
                    order = orderValue;
            }
            catch { /* Use default */ }
            
            try
            {
                var tooltipProp = componentType.GetProperty("Tooltip");
                if (tooltipProp?.GetValue(instance) is string tooltipValue)
                    tooltip = tooltipValue;
            }
            catch { /* Use default */ }
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
}


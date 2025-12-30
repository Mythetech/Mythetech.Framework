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
    /// Load a plugin from a DLL file path
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
            var loadContext = AssemblyLoadContext.Default;
            var assembly = loadContext.LoadFromAssemblyPath(Path.GetFullPath(dllPath));
            
            return LoadPlugin(assembly, dllPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin assembly from {Path}", dllPath);
            return null;
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
    /// Load all plugins from a directory
    /// </summary>
    /// <param name="pluginDirectory">Directory containing plugin DLLs</param>
    /// <returns>List of successfully loaded plugins</returns>
    public IReadOnlyList<PluginInfo> LoadPluginsFromDirectory(string pluginDirectory)
    {
        var plugins = new List<PluginInfo>();
        
        if (!Directory.Exists(pluginDirectory))
        {
            _logger.LogDebug("Plugin directory does not exist: {Path}", pluginDirectory);
            return plugins;
        }

        foreach (var dllPath in Directory.GetFiles(pluginDirectory, "*.dll"))
        {
            var plugin = LoadPlugin(dllPath);
            if (plugin is not null)
            {
                plugins.Add(plugin);
            }
        }
        
        _logger.LogInformation("Loaded {Count} plugins from {Path}", plugins.Count, pluginDirectory);
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
                if (Activator.CreateInstance(type) is Components.PluginMenu instance)
                {
                    metadata.Add(new PluginComponentMetadata
                    {
                        ComponentType = type,
                        Title = instance.Title,
                        Icon = instance.Icon,
                        Order = instance.Order,
                        Tooltip = instance.Tooltip
                    });
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
                if (Activator.CreateInstance(type) is Components.PluginContextPanel instance)
                {
                    metadata.Add(new PluginComponentMetadata
                    {
                        ComponentType = type,
                        Title = instance.Title,
                        Icon = instance.Icon,
                        Order = instance.Order
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract metadata from panel component {Type}", type.FullName);
            }
        }
        
        return metadata;
    }
}


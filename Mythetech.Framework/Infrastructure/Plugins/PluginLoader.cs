using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using MudBlazor;
using Mythetech.Framework.Infrastructure.Plugins.Installation;

namespace Mythetech.Framework.Infrastructure.Plugins;

/// <summary>
/// Responsible for loading plugin assemblies and discovering plugin components
/// </summary>
public class PluginLoader
{
    private const string DepsJsonSuffix = ".deps.json";
    private static readonly string FrameworkAssemblyName = typeof(IPluginManifest).Assembly.GetName().Name!;

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
    /// Load a plugin from a DLL file path into a new <see cref="PluginLoadContext"/>.
    /// The plugin's dependencies resolve through its .deps.json or its directory, and host assemblies come from the host.
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
            var loadContext = new PluginLoadContext(Path.GetDirectoryName(fullPath)!, fullPath);
            var assembly = loadContext.LoadFromAssemblyPath(fullPath);

            return LoadPlugin(assembly, dllPath, loadContext);
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
    /// <param name="loadContext">Optional load context for unloading support</param>
    /// <returns>PluginInfo if valid plugin, null otherwise</returns>
    public PluginInfo? LoadPlugin(Assembly assembly, string? sourcePath = null, AssemblyLoadContext? loadContext = null)
        => LoadPlugin(assembly, sourcePath, loadContext, isLocal: false);

    private PluginInfo? LoadPlugin(Assembly assembly, string? sourcePath, AssemblyLoadContext? loadContext, bool isLocal)
    {
        try
        {
            var types = assembly.GetTypes();
            var manifest = DiscoverManifest(types);
            if (manifest is null)
            {
                _logger.LogWarning("No IPluginManifest implementation found in assembly {Assembly}", assembly.FullName);
                return null;
            }

            var menuComponents = DiscoverComponentsOfType<Components.PluginMenu>(types);
            var contextPanelComponents = DiscoverComponentsOfType<Components.PluginContextPanel>(types);

            var menuMetadata = ExtractMenuMetadata(menuComponents);
            var panelMetadata = ExtractContextPanelMetadata(contextPanelComponents);

            var frameworkVersion = GetReferencedFrameworkVersion(assembly);
            var hostFrameworkVersion = typeof(IPluginManifest).Assembly.GetName().Version;
            if (hostFrameworkVersion is not null && IsBuiltAgainstNewerFramework(frameworkVersion, hostFrameworkVersion))
            {
                _logger.LogWarning(
                    "Plugin {PluginId} was built against Mythetech.Framework {PluginFrameworkVersion}, newer than this app's {HostFrameworkVersion}; loading it anyway",
                    manifest.Id, frameworkVersion, hostFrameworkVersion);
            }

            var pluginInfo = new PluginInfo
            {
                Manifest = manifest,
                FrameworkVersion = frameworkVersion,
                Assembly = assembly,
                SourcePath = sourcePath,
                IsLocal = isLocal,
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
    
    internal static bool IsBuiltAgainstNewerFramework(Version? pluginFrameworkVersion, Version hostFrameworkVersion) =>
        pluginFrameworkVersion is not null && pluginFrameworkVersion > hostFrameworkVersion;

    private static Version? GetReferencedFrameworkVersion(Assembly assembly)
    {
        var frameworkName = typeof(IPluginManifest).Assembly.GetName().Name;
        return assembly.GetReferencedAssemblies().FirstOrDefault(reference => reference.Name == frameworkName)?.Version;
    }

    /// <summary>
    /// Load all plugins from a directory.
    /// First deletes retired install folders (see <see cref="PluginInstallLayout.PrepareForLoad"/>), then loads
    /// the newest install of each plugin, each legacy plugin folder and any loose DLLs, each folder into its own context.
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

        var directoriesToScan = PluginInstallLayout.PrepareForLoad(pluginDirectory, _logger);

        _logger.LogDebug("Scanning {Count} directories for plugins", directoriesToScan.Count);

        foreach (var directory in directoriesToScan)
        {
            plugins.AddRange(LoadPluginsFromFolder(directory));
        }
        
        _logger.LogInformation("Loaded {Count} plugins from {Path}", plugins.Count, pluginDirectory);
        return plugins;
    }

    /// <summary>
    /// Load the plugins in one folder into a new <see cref="PluginLoadContext"/>.
    /// DLLs the host provides, such as Mythetech.Framework or MudBlazor, resolve to the host's copies.
    /// </summary>
    /// <param name="folder">Folder containing a plugin DLL and its dependencies</param>
    /// <param name="isLocal">True when the folder is a local plugin's build output</param>
    /// <returns>The plugins found in the folder</returns>
    public IReadOnlyList<PluginInfo> LoadPluginsFromFolder(string folder, bool isLocal = false)
    {
        var plugins = new List<PluginInfo>();

        if (!Directory.Exists(folder))
        {
            _logger.LogDebug("Plugin folder does not exist: {Path}", folder);
            return plugins;
        }

        var dllPaths = Directory.GetFiles(folder, "*.dll");
        if (dllPaths.Length == 0)
        {
            return plugins;
        }

        _logger.LogDebug("Found {Count} DLLs in {Directory}", dllPaths.Length, folder);

        // Load every DLL before scanning any, so a plugin's dependencies are already in its context.
        var fullFolder = Path.GetFullPath(folder);
        var loadContext = new PluginLoadContext(fullFolder, FindMainAssemblyPath(fullFolder));
        var folderAssemblies = new List<(string Path, Assembly Assembly, bool InContext)>();
        foreach (var dllPath in dllPaths)
        {
            var fullPath = Path.GetFullPath(dllPath);
            try
            {
                var assemblyName = AssemblyName.GetAssemblyName(fullPath);
                var hostAssembly = LoadFromHost(assemblyName);
                folderAssemblies.Add(hostAssembly is null
                    ? (fullPath, loadContext.LoadFromAssemblyPath(fullPath), true)
                    : (fullPath, hostAssembly, false));
            }
            catch (BadImageFormatException)
            {
                // Not a .NET assembly, skip
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load assembly {Path}; skipping it", fullPath);
            }
        }

        foreach (var (path, assembly, inContext) in folderAssemblies)
        {
            if (!ReferencesFramework(assembly))
            {
                continue;
            }

            var plugin = LoadPlugin(assembly, path, inContext ? loadContext : null, isLocal);
            if (plugin is not null)
            {
                plugins.Add(plugin);
            }
        }

        return plugins;
    }

    private static Assembly? LoadFromHost(AssemblyName assemblyName)
    {
        if (!SharedAssemblies.IsShared(assemblyName))
        {
            return null;
        }

        // By simple name, so a copy at a different version than the host's still binds to the host's.
        return SharedAssemblies.FindInDefault(assemblyName)
            ?? AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(assemblyName.Name!));
    }

    private static bool ReferencesFramework(Assembly assembly)
    {
        return assembly.GetReferencedAssemblies()
            .Any(reference => string.Equals(reference.Name, FrameworkAssemblyName, StringComparison.OrdinalIgnoreCase));
    }

    // The plugin's deps.json locates assets outside the folder root, such as runtimes/{rid}/ and resource assemblies.
    private static string? FindMainAssemblyPath(string folder)
    {
        var candidates = Directory.GetFiles(folder, $"*{DepsJsonSuffix}")
            .Select(depsPath => Path.Combine(folder, $"{Path.GetFileName(depsPath)[..^DepsJsonSuffix.Length]}.dll"))
            .Where(File.Exists)
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .ToList();

        return candidates.FirstOrDefault(FileReferencesFramework) ?? candidates.FirstOrDefault();
    }

    private static bool FileReferencesFramework(string dllPath)
    {
        try
        {
            using var peReader = new PEReader(File.OpenRead(dllPath));
            if (!peReader.HasMetadata)
            {
                return false;
            }

            var metadata = peReader.GetMetadataReader();
            return metadata.AssemblyReferences.Any(handle =>
                metadata.StringComparer.Equals(metadata.GetAssemblyReference(handle).Name, FrameworkAssemblyName, ignoreCase: true));
        }
        catch (Exception ex) when (ex is BadImageFormatException or IOException)
        {
            return false;
        }
    }

    private IPluginManifest? DiscoverManifest(Type[] types)
    {
        var manifestType = types
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
    
    private static List<Type> DiscoverComponentsOfType<TBase>(Type[] types) where TBase : class
    {
        return types
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


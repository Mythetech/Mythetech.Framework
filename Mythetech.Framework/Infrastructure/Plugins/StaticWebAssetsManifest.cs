using System.Text.Json;

namespace Mythetech.Framework.Infrastructure.Plugins;

/// <summary>
/// Reads an ASP.NET <c>*.staticwebassets.runtime.json</c> file and maps request paths to files on disk.
/// </summary>
public sealed class StaticWebAssetsManifest
{
    private readonly JsonElement _root;

    private StaticWebAssetsManifest(IReadOnlyList<string> contentRoots, JsonElement root)
    {
        ContentRoots = contentRoots;
        _root = root;
    }

    /// <summary>The folders the manifest serves assets from.</summary>
    public IReadOnlyList<string> ContentRoots { get; }

    /// <summary>Loads a manifest, returning null when the file is missing or isn't a valid manifest.</summary>
    public static StaticWebAssetsManifest? TryLoad(string manifestPath)
    {
        try
        {
            if (!File.Exists(manifestPath))
                return null;

            using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("ContentRoots", out var roots) || roots.ValueKind != JsonValueKind.Array
                || !root.TryGetProperty("Root", out var tree) || tree.ValueKind != JsonValueKind.Object)
                return null;

            var contentRoots = new List<string>();
            foreach (var item in roots.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String)
                    return null;
                contentRoots.Add(item.GetString()!);
            }

            return new StaticWebAssetsManifest(contentRoots, tree.Clone());
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>Returns the file that serves <paramref name="requestPath"/>, or null when there is none on disk.</summary>
    public string? Resolve(string requestPath)
    {
        var segments = requestPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var node = _root;
        string? patternFile = null;

        for (var i = 0; i <= segments.Length; i++)
        {
            var remaining = segments.Skip(i);
            if (TryGetPatternRoot(node, out var patternRoot))
                patternFile = Combine(patternRoot, remaining);

            if (i == segments.Length)
                break;

            if (!node.TryGetProperty("Children", out var children) || children.ValueKind != JsonValueKind.Object
                || !children.TryGetProperty(segments[i], out node))
            {
                return ExistingOrNull(patternFile);
            }
        }

        if (node.TryGetProperty("Asset", out var asset) && asset.ValueKind == JsonValueKind.Object
            && asset.TryGetProperty("ContentRootIndex", out var index) && index.TryGetInt32(out var rootIndex)
            && asset.TryGetProperty("SubPath", out var subPath) && subPath.ValueKind == JsonValueKind.String
            && rootIndex >= 0 && rootIndex < ContentRoots.Count)
        {
            return ExistingOrNull(Path.Combine(ContentRoots[rootIndex], subPath.GetString()!));
        }

        return ExistingOrNull(patternFile);
    }

    private bool TryGetPatternRoot(JsonElement node, out string root)
    {
        root = string.Empty;
        if (!node.TryGetProperty("Patterns", out var patterns) || patterns.ValueKind != JsonValueKind.Array)
            return false;

        foreach (var pattern in patterns.EnumerateArray())
        {
            if (pattern.TryGetProperty("Pattern", out var text) && text.GetString() == "**"
                && pattern.TryGetProperty("ContentRootIndex", out var index) && index.TryGetInt32(out var rootIndex)
                && rootIndex >= 0 && rootIndex < ContentRoots.Count)
            {
                root = ContentRoots[rootIndex];
                return true;
            }
        }

        return false;
    }

    private static string Combine(string root, IEnumerable<string> segments) =>
        Path.Combine([root, .. segments]);

    private static string? ExistingOrNull(string? path) => path is not null && File.Exists(path) ? path : null;
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Mythetech.Framework.Test.Infrastructure.Plugins.TestPlugins;

/// <summary>
/// Compiles plugin and library assemblies on disk so loader tests can load real, uniquely named DLLs.
/// </summary>
public static class TestPluginBuilder
{
    /// <summary>
    /// Namespace of the generated <c>Manifest</c> class. Extra source can declare
    /// <c>static partial void OverrideDescription(ref string description)</c> on it to set the description.
    /// </summary>
    public const string ManifestNamespace = "TestPlugin";

    private static readonly IReadOnlyList<MetadataReference> PlatformReferences =
        ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToList();

    /// <summary>
    /// Compiles a plugin whose manifest returns <paramref name="pluginId"/> and <paramref name="version"/>,
    /// writes <c>{outputFolder}/{assemblyName}.dll</c> and returns its path.
    /// </summary>
    public static string Build(
        string outputFolder,
        string assemblyName,
        string pluginId,
        Version version,
        string? extraSource = null,
        IEnumerable<string>? extraReferences = null)
    {
        var manifest = $$"""
            [assembly: System.Reflection.AssemblyVersion({{Literal(version.ToString())}})]

            namespace {{ManifestNamespace}};

            public partial class Manifest : Mythetech.Framework.Infrastructure.Plugins.IPluginManifest
            {
                public string Id => {{Literal(pluginId)}};
                public string Name => {{Literal(assemblyName)}};
                public System.Version Version => System.Version.Parse({{Literal(version.ToString())}});
                public string Developer => "Mythetech";

                public string Description
                {
                    get
                    {
                        var description = "Plugin compiled by the loader tests";
                        OverrideDescription(ref description);
                        return description;
                    }
                }

                static partial void OverrideDescription(ref string description);
            }
            """;

        string[] sources = extraSource is null ? [manifest] : [manifest, extraSource];
        return Compile(outputFolder, assemblyName, sources, extraReferences);
    }

    /// <summary>
    /// Compiles a library from <paramref name="source"/>, writes <c>{outputFolder}/{assemblyName}.dll</c>
    /// and returns its path.
    /// </summary>
    public static string BuildLibrary(
        string outputFolder,
        string assemblyName,
        string source,
        IEnumerable<string>? extraReferences = null)
    {
        return Compile(outputFolder, assemblyName, [source], extraReferences);
    }

    private static string Compile(
        string outputFolder,
        string assemblyName,
        IEnumerable<string> sources,
        IEnumerable<string>? extraReferences)
    {
        var references = PlatformReferences
            .Concat((extraReferences ?? []).Select(path => MetadataReference.CreateFromFile(path)));

        var compilation = CSharpCompilation.Create(
            assemblyName,
            sources.Select(source => CSharpSyntaxTree.ParseText(source)),
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        using var image = new MemoryStream();
        var result = compilation.Emit(image);
        if (!result.Success)
        {
            var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
            throw new InvalidOperationException(
                $"Compiling {assemblyName} failed:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }

        Directory.CreateDirectory(outputFolder);
        var outputPath = Path.Combine(outputFolder, $"{assemblyName}.dll");

        // Replacing the file rather than rewriting it keeps a copy that is already loaded intact.
        var stagingPath = Path.Combine(outputFolder, $"{assemblyName}.{Guid.NewGuid():N}.tmp");
        File.WriteAllBytes(stagingPath, image.ToArray());
        File.Move(stagingPath, outputPath, overwrite: true);

        return outputPath;
    }

    private static string Literal(string value) => SymbolDisplay.FormatLiteral(value, quote: true);
}

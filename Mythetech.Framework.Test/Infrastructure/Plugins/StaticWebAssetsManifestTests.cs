using Shouldly;
using Mythetech.Framework.Infrastructure.Plugins;

namespace Mythetech.Framework.Test.Infrastructure.Plugins;

public class StaticWebAssetsManifestTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"swa_manifest_test_{Guid.NewGuid()}");

    private string WriteManifest(string contentRoot, string json)
    {
        var path = Path.Combine(_directory, "Plugin.staticwebassets.runtime.json");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(path, json.Replace("{root}", contentRoot.Replace("\\", "\\\\")));
        return path;
    }

    private string CreateFile(string relativePath)
    {
        var path = Path.Combine(_directory, "content", relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "x");
        return path;
    }

    private string ContentRoot => Path.Combine(_directory, "content") + Path.DirectorySeparatorChar;

    [Fact(DisplayName = "Resolve maps an asset node to its content root path")]
    public void Resolve_AssetNode_ReturnsContentRootPath()
    {
        var file = CreateFile("css/site.css");
        var manifest = StaticWebAssetsManifest.TryLoad(WriteManifest(ContentRoot, """
            {"ContentRoots":["{root}"],"Root":{"Children":{"css":{"Children":{"site.css":{"Children":null,"Asset":{"ContentRootIndex":0,"SubPath":"css/site.css"},"Patterns":null}},"Asset":null,"Patterns":null}},"Asset":null,"Patterns":null}}
            """));

        manifest.ShouldNotBeNull();
        manifest.ContentRoots.ShouldBe([ContentRoot]);
        manifest.Resolve("css/site.css").ShouldBe(Path.Combine(ContentRoot, "css/site.css"));
        File.Exists(file).ShouldBeTrue();
    }

    [Fact(DisplayName = "Resolve maps a ** pattern to the content root plus the remaining segments")]
    public void Resolve_DoubleStarPattern_ReturnsContentRootPath()
    {
        CreateFile("deep/file.js");
        var manifest = StaticWebAssetsManifest.TryLoad(WriteManifest(ContentRoot, """
            {"ContentRoots":["{root}"],"Root":{"Children":{"lib":{"Children":null,"Asset":null,"Patterns":[{"ContentRootIndex":0,"Pattern":"**","Depth":0}]}},"Asset":null,"Patterns":null}}
            """));

        manifest.ShouldNotBeNull();
        manifest.Resolve("lib/deep/file.js").ShouldBe(Path.Combine(ContentRoot, "deep", "file.js"));
    }

    [Fact(DisplayName = "Resolve returns null for a path the manifest doesn't know or whose file is missing")]
    public void Resolve_UnknownPath_ReturnsNull()
    {
        var manifest = StaticWebAssetsManifest.TryLoad(WriteManifest(ContentRoot, """
            {"ContentRoots":["{root}"],"Root":{"Children":{"gone.css":{"Children":null,"Asset":{"ContentRootIndex":0,"SubPath":"gone.css"},"Patterns":null}},"Asset":null,"Patterns":null}}
            """));

        manifest.ShouldNotBeNull();
        manifest.Resolve("nothing/here.css").ShouldBeNull();
        manifest.Resolve("gone.css").ShouldBeNull();
    }

    [Theory(DisplayName = "TryLoad returns null for malformed or unexpected content")]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("{\"ContentRoots\":5,\"Root\":{}}")]
    [InlineData("{\"ContentRoots\":[1],\"Root\":{}}")]
    [InlineData("{}")]
    public void TryLoad_MalformedFile_ReturnsNull(string content)
    {
        var path = WriteManifest(ContentRoot, content);

        StaticWebAssetsManifest.TryLoad(path).ShouldBeNull();
    }

    [Fact(DisplayName = "TryLoad returns null when the file doesn't exist")]
    public void TryLoad_MissingFile_ReturnsNull() =>
        StaticWebAssetsManifest.TryLoad(Path.Combine(_directory, "missing.json")).ShouldBeNull();

    public void Dispose()
    {
        try { Directory.Delete(_directory, recursive: true); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }
}

using System.IO.Compression;
using Mythetech.Framework.Infrastructure.Plugins.Installation;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Plugins;

public class PluginPackageExtractorTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "extractor-" + Guid.NewGuid().ToString("N"));

    [Fact(DisplayName = "An entry escaping the target throws and writes nothing outside it")]
    public void Extract_EntryEscapingTarget_ThrowsAndWritesNothingOutside()
    {
        var target = Path.Combine(_root, "target");
        Directory.CreateDirectory(target);

        using var zip = new MemoryStream();
        using (var archive = new ZipArchive(zip, ZipArchiveMode.Create, leaveOpen: true))
        {
            // A root-level file keeps the extractor from treating ".." as a strippable single root folder.
            WriteEntry(archive, "Good.dll");
            WriteEntry(archive, "../evil.dll");
        }
        zip.Position = 0;

        Should.Throw<InvalidOperationException>(() => PluginPackageExtractor.Extract(zip, target));

        File.Exists(Path.Combine(_root, "evil.dll")).ShouldBeFalse();
        Directory.GetFiles(_root, "evil.dll", SearchOption.AllDirectories).ShouldBeEmpty();
    }

    private static void WriteEntry(ZipArchive archive, string name)
    {
        using var writer = new StreamWriter(archive.CreateEntry(name).Open());
        writer.Write("x");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}

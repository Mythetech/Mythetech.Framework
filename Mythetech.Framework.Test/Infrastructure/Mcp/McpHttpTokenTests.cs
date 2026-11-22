using Mythetech.Framework.Infrastructure.Mcp.Transport;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Mcp;

public sealed class McpHttpTokenTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("mcp-token-").FullName;

    private string TokenPath => Path.Combine(_directory, "mcp", "http-token");

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact(DisplayName = "Token_is_reused_across_restarts")]
    public void Token_is_reused_across_restarts()
    {
        var first = McpHttpToken.LoadOrCreate(TokenPath);
        var second = McpHttpToken.LoadOrCreate(TokenPath);

        first.ShouldNotBeNullOrWhiteSpace();
        second.ShouldBe(first);
    }

    [Fact(DisplayName = "Token_is_32_random_bytes_in_Base64Url")]
    public void Token_is_32_random_bytes_in_Base64Url()
    {
        var token = McpHttpToken.LoadOrCreate(TokenPath);

        System.Buffers.Text.Base64Url.DecodeFromChars(token).Length.ShouldBe(32);
    }

    [Fact(DisplayName = "Two_instances_starting_together_share_one_token")]
    public void Two_instances_starting_together_share_one_token()
    {
        var tokens = new string[8];

        Parallel.For(0, tokens.Length, i => tokens[i] = McpHttpToken.LoadOrCreate(TokenPath));

        tokens.Distinct().Count().ShouldBe(1);
        tokens[0].ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Token_file_is_owner_only")]
    public void Token_file_is_owner_only()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Unix file modes only");

        McpHttpToken.LoadOrCreate(TokenPath);

        File.GetUnixFileMode(TokenPath).ShouldBe(UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    [Fact(DisplayName = "A_token_file_readable_by_others_is_made_owner_only")]
    public void A_token_file_readable_by_others_is_made_owner_only()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Unix file modes only");
        var existing = McpHttpToken.LoadOrCreate(TokenPath);
        File.SetUnixFileMode(TokenPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead);

        var token = McpHttpToken.LoadOrCreate(TokenPath);

        token.ShouldBe(existing);
        File.GetUnixFileMode(TokenPath).ShouldBe(UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }
}

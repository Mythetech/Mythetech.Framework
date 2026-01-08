using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mythetech.Framework.Infrastructure.Mcp.Protocol.JsonRpc;

namespace Mythetech.Framework.Infrastructure.Mcp.Transport;

/// <summary>
/// MCP transport over stdio (stdin/stdout).
/// Messages are newline-delimited JSON.
/// Note: Console.OpenStandardInput/Output is not supported on browser platform.
/// </summary>
[UnsupportedOSPlatform("browser")]
public class StdioMcpTransport : IMcpTransport
{
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Create a transport using standard Console input/output
    /// </summary>
    public StdioMcpTransport() : this(Console.OpenStandardInput(), Console.OpenStandardOutput())
    {
    }

    /// <summary>
    /// Create a transport using custom streams (useful for testing)
    /// </summary>
    public StdioMcpTransport(Stream input, Stream output)
    {
        _reader = new StreamReader(input, Encoding.UTF8);
        _writer = new StreamWriter(output, new UTF8Encoding(false)) { AutoFlush = true };
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <inheritdoc />
    public async Task<JsonRpcRequest?> ReadMessageAsync(CancellationToken cancellationToken = default)
    {
        var line = await _reader.ReadLineAsync(cancellationToken);
        if (line is null) return null;
        if (string.IsNullOrWhiteSpace(line)) return await ReadMessageAsync(cancellationToken);

        return JsonSerializer.Deserialize<JsonRpcRequest>(line, _jsonOptions);
    }

    /// <inheritdoc />
    public async Task WriteMessageAsync(JsonRpcResponse response, CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            var json = JsonSerializer.Serialize(response, _jsonOptions);
            await _writer.WriteLineAsync(json);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task WriteNotificationAsync(string method, object? @params, CancellationToken cancellationToken = default)
    {
        var notification = new { jsonrpc = "2.0", method, @params };
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            var json = JsonSerializer.Serialize(notification, _jsonOptions);
            await _writer.WriteLineAsync(json);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _writer.DisposeAsync();
        _reader.Dispose();
        _writeLock.Dispose();
    }
}

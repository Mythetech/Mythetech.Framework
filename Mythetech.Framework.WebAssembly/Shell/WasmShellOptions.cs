namespace Mythetech.Framework.WebAssembly.Shell;

/// <summary>
/// Configuration options for WebAssembly shell execution.
/// </summary>
public class WasmShellOptions
{
    /// <summary>
    /// Allow the 'eval' command to execute arbitrary JavaScript.
    /// Default: false (security consideration).
    /// </summary>
    /// <remarks>
    /// When enabled, users can execute arbitrary JavaScript code via the eval command.
    /// Only enable this in trusted environments where script execution is expected.
    /// </remarks>
    public bool AllowEval { get; set; }

    /// <summary>
    /// Whether to register built-in commands (echo, help, env) automatically.
    /// Default: true.
    /// </summary>
    public bool RegisterBuiltInCommands { get; set; } = true;
}

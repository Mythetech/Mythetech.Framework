using Microsoft.JSInterop;

namespace Mythetech.Framework.Infrastructure.Guards;

/// <summary>
/// The outcome of a guarded JavaScript interop call.
/// </summary>
public enum JsInvokeStatus
{
    /// <summary>The call completed.</summary>
    Success,

    /// <summary>
    /// The JavaScript runtime was gone before the call could complete, because the
    /// WebView was torn down or the circuit disconnected. Expected during disposal
    /// and normally safe to ignore.
    /// </summary>
    Disconnected,

    /// <summary>
    /// The call reached JavaScript and JavaScript raised an error. This is a genuine
    /// fault and the exception is carried on the result.
    /// </summary>
    Failed
}

/// <summary>
/// The result of a guarded JavaScript interop call that returns no value.
/// </summary>
/// <param name="Status">How the call ended.</param>
/// <param name="Exception">
/// The JavaScript error when <paramref name="Status"/> is <see cref="JsInvokeStatus.Failed"/>,
/// otherwise null. Disconnection carries no exception; the status conveys it.
/// </param>
public readonly record struct JsInvokeResult(JsInvokeStatus Status, JSException? Exception)
{
    /// <summary>True only when the call completed.</summary>
    public bool Success => Status is JsInvokeStatus.Success;
}

/// <summary>
/// The result of a guarded JavaScript interop call that returns a value.
/// </summary>
/// <param name="Status">How the call ended.</param>
/// <param name="Value">
/// The returned value when <paramref name="Status"/> is <see cref="JsInvokeStatus.Success"/>,
/// otherwise the default for <typeparamref name="T"/>.
/// </param>
/// <param name="Exception">
/// The JavaScript error when <paramref name="Status"/> is <see cref="JsInvokeStatus.Failed"/>,
/// otherwise null.
/// </param>
public readonly record struct JsInvokeResult<T>(JsInvokeStatus Status, T? Value, JSException? Exception)
{
    /// <summary>True only when the call completed.</summary>
    public bool Success => Status is JsInvokeStatus.Success;
}

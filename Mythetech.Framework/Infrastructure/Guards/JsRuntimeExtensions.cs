using Microsoft.JSInterop;

namespace Mythetech.Framework.Infrastructure.Guards;

/// <summary>
/// Guarded wrappers around <see cref="IJSRuntime"/> interop calls.
/// </summary>
/// <remarks>
/// A JavaScript call racing a torn-down WebView or a dropped circuit throws even though
/// nothing is wrong, which is what the BL0016 analyzer warns about. These wrappers absorb
/// that case and leave genuine JavaScript errors alone, so a call site never has to choose
/// between an unguarded call and a catch-all that hides real faults.
///
/// Prefer the <c>Safe</c> methods: they ignore disconnection and rethrow real errors, which
/// is what nearly every call site wants. Reach for the <c>Try</c> methods only when the call
/// site genuinely acts on the difference, for example to skip bookkeeping when a call did
/// not land.
/// </remarks>
public static class JsRuntimeExtensions
{
    /// <summary>
    /// Invokes a JavaScript function that returns no value, ignoring disconnection and
    /// rethrowing genuine JavaScript errors.
    /// </summary>
    public static async Task InvokeVoidSafeAsync(this IJSRuntime js, string identifier, params object?[]? args)
    {
        var result = await js.TryInvokeVoidAsync(identifier, args);

        if (result.Exception is not null)
            throw result.Exception;
    }

    /// <summary>
    /// Invokes a JavaScript function and returns its value, yielding the default for
    /// <typeparamref name="T"/> on disconnection and rethrowing genuine JavaScript errors.
    /// </summary>
    public static async Task<T?> InvokeSafeAsync<T>(this IJSRuntime js, string identifier, params object?[]? args)
    {
        var result = await js.TryInvokeAsync<T>(identifier, args);

        if (result.Exception is not null)
            throw result.Exception;

        return result.Value;
    }

    /// <summary>
    /// Invokes a JavaScript function that returns no value, reporting the outcome rather
    /// than throwing. Exceptions unrelated to JavaScript interop still propagate.
    /// </summary>
    public static async Task<JsInvokeResult> TryInvokeVoidAsync(this IJSRuntime js, string identifier, params object?[]? args)
    {
        try
        {
            await js.InvokeVoidAsync(identifier, args);
            return new JsInvokeResult(JsInvokeStatus.Success, null);
        }
        catch (Exception ex) when (IsDisconnection(ex))
        {
            return new JsInvokeResult(JsInvokeStatus.Disconnected, null);
        }
        catch (JSException ex)
        {
            return new JsInvokeResult(JsInvokeStatus.Failed, ex);
        }
    }

    /// <summary>
    /// Invokes a JavaScript function and reports its outcome rather than throwing.
    /// Exceptions unrelated to JavaScript interop still propagate.
    /// </summary>
    public static async Task<JsInvokeResult<T>> TryInvokeAsync<T>(this IJSRuntime js, string identifier, params object?[]? args)
    {
        try
        {
            var value = await js.InvokeAsync<T>(identifier, args);
            return new JsInvokeResult<T>(JsInvokeStatus.Success, value, null);
        }
        catch (Exception ex) when (IsDisconnection(ex))
        {
            return new JsInvokeResult<T>(JsInvokeStatus.Disconnected, default, null);
        }
        catch (JSException ex)
        {
            return new JsInvokeResult<T>(JsInvokeStatus.Failed, default, ex);
        }
    }

    private static bool IsDisconnection(Exception ex) =>
        ex is JSDisconnectedException or ObjectDisposedException;
}

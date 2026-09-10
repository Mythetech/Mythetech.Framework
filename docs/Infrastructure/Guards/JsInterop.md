# Guarded JS Interop

.NET 11 ships the `BL0016` analyzer, which warns that a JS interop call is not guarded
with a try/catch. The problem it points at is real: a call racing a torn-down WebView or
a dropped circuit throws even though nothing is actually wrong.

The naive fix, wrapping every call site in a catch-all, trades a loud failure for a silent
one and hides genuine JavaScript errors. `JsRuntimeExtensions` handles the disconnection
case centrally and leaves real errors alone.

## Choosing a method

| Method | Disconnection | JavaScript error |
| --- | --- | --- |
| `InvokeVoidSafeAsync` | ignored | **throws** |
| `InvokeSafeAsync<T>` | returns `default(T)` | **throws** |
| `TryInvokeVoidAsync` | `Status = Disconnected` | `Status = Failed`, `Exception` set |
| `TryInvokeAsync<T>` | `Status = Disconnected`, `Value = default` | `Status = Failed`, `Exception` set |

**Reach for the `Safe` methods by default.** They satisfy the analyzer, ignore the teardown
race, and let genuine JavaScript errors surface as they do today.

```csharp
using Mythetech.Framework.Infrastructure.Guards;

await JS.InvokeVoidSafeAsync("myApp.highlight", elementId);
var width = await JS.InvokeSafeAsync<int>("myApp.measure", elementId);
```

**Reach for the `Try` methods only when the call site acts on the difference**, typically to
skip bookkeeping when a call did not land:

```csharp
var result = await JS.TryInvokeVoidAsync("eval", js);
if (result.Exception is not null)
    throw result.Exception;

if (result.Success)
    _loadedAssets[id] = true;
```

Recording work as done when the call never reached the browser is the bug this shape
prevents. `DesktopPluginAssetLoader` had exactly that: it marked plugin assets loaded
unconditionally, so an asset lost to a teardown race was never retried.

## What counts as disconnection

`JSDisconnectedException` and `ObjectDisposedException`. Both mean the runtime is gone and
the call can never succeed.

`JSException` means the call reached JavaScript and JavaScript raised an error. That is a
genuine fault, never treated as disconnection. Anything else propagates untouched.

## Migrating a project

1. Add `using Mythetech.Framework.Infrastructure.Guards;`.
2. Replace `InvokeVoidAsync` with `InvokeVoidSafeAsync` and `InvokeAsync<T>` with
   `InvokeSafeAsync<T>`.
3. Where a call site records state after the call, switch to the `Try` form and make the
   bookkeeping conditional on `Success`.
4. Leave calls that already sit inside a meaningful try/catch alone; the analyzer does not
   flag them.

Test projects are a special case. Substituted `IJSRuntime` calls are mock configuration
rather than interop and cannot disconnect, so suppress the rule for the test project
instead of wrapping the setup:

```xml
<NoWarn>$(NoWarn);BL0016</NoWarn>
```

# Changelog

## [0.21.3] - 2026-10-06

### Added

- `SettingAttribute.Stacked`: renders a setting's editor on its own full-width row beneath the label and description instead of beside them. The default row sizes the editor to its content and lets the label column shrink to nothing, so a large custom editor, such as a list or a status panel with a long message, pushed the label and description into a column one word wide. Set `Stacked = true` on those settings

### Notes

- All four packages move to `0.21.3`. `Mythetech.Framework.AI.Generator` is unchanged apart from the version, which follows `Mythetech.Framework`

## [0.21.2] - 2026-10-04

### Changed

- The embedded Material Symbols Rounded font moves from Google Fonts v373 to v376. It adds 15 icons (`apps_plus`, `chat_display`, `closed_caption_display`, `display_group`, `document_share`, `edit_line`, `filter_cancel`, `filter_plus`, `function_search`, `group_eye`, `import_spark`, `markdown_convert`, `markdown_document`, `markdown_spark`, `sheets_column_swap`) and removes none. `subway` is the only existing icon whose outline changed
- `scripts/update-material-symbols.sh` is now `scripts/update-material-symbols.cs`, a file-based C# app, so the font refresh also runs on Windows: `dotnet scripts/update-material-symbols.cs`

## [0.21.1] - 2026-09-28

### Fixed

- `McpServer` handled one request at a time across every client, so a slow tool call in one agent session held up every other session on the app's MCP server, down to a `ping`. It now reads requests in one loop and handles them on a pool of workers, so calls from different clients, and pipelined calls from one client, run concurrently. Responses can arrive out of order, which JSON-RPC allows since each carries its request's id

### Added

- `McpServerOptions.MaxConcurrentRequests` (default 16): how many requests are handled at once across all clients. Set it to 1 to restore one-at-a-time handling
- `McpToolInvoker.InvokeAsync(toolName, arguments, cancellationToken)`: calls a registered tool by name with JSON arguments, without an MCP transport or the message bus, and passes the cancellation token to the tool. It checks for unknown and disabled tools, reads enum names, fills in defaults when there are no arguments, and returns exceptions as error results, exactly as MCP clients see them. `AddMcp()` registers it. It is for code that offers the app's tools some other way, such as Mythetech.Agents handing them to a chat client

### Changed

- `McpToolCallHandler` takes a `McpToolInvoker` and delegates to it, so the MCP server and direct callers share one code path. Its constructor changed, which only matters to code that constructs it by hand instead of through DI

### Notes

- Tools that were only ever called one at a time can now run concurrently with each other, as they already could with the app's own UI. A tool that changes shared state should do it through a state class that is safe to call from several threads, or the app can set `MaxConcurrentRequests` to 1
- All four packages move to `0.21.1`. `Mythetech.Framework.AI.Generator` is unchanged apart from the version, which follows `Mythetech.Framework`

## [0.21.0] - 2026-09-28

### Added

- `text-truncate` utility class: single-line truncation with an ellipsis. It includes `min-width: 0`, so it also truncates inside a flex row, where the text would otherwise overflow or wrap. Apps that define their own `text-truncate` can drop it
- `sr-only` utility class: hides content visually while screen readers still announce it. Unlike MudBlazor's `mud-typography-srOnly`, it clips the content
- `mt-hover-row`, `mt-hover-row-label` and `mt-hover-actions`: a single-line row whose actions appear on hover or keyboard focus without shifting layout. The label truncates to make room, so the row never grows taller, and the actions are always shown on touch devices. Don't put a menu activator in `mt-hover-actions`; opening the menu ends the hover and hides its anchor

- `HoverStack.Actions`: controls shown at the end of the row on hover or keyboard focus, built on the `mt-hover-*` classes. The content gives up room to them, so the row never grows
- `MythetechFrameworkIcons.ChevronRight`, `Edit`, `Folder` and `RadioButtonUnchecked`

- `[ToolRequest]`: marks a message as a request/response MCP tool, sent with `SendAsync`. It replaces `[ToolQuery]` with the same `Name`, `Description` and `ResponseType` properties, and the new name fits tools that change state as well as reads
- `ToolResult<T>` and `ToolError`: a handler's success value, or an expected failure with a code and a message. C# callers check `IsSuccess` instead of catching. When a `[ToolRequest]`'s `ResponseType` is a `ToolResult<T>`, the generated tool returns the value on success and an MCP error result (`isError: true`) with the text `code: message` on failure. Handlers can return a value or a `ToolError` directly, as both convert implicitly. Unexpected exceptions still go through `McpToolCallHandler`'s catch
- `McpToolResult.Json(value)`, a text result holding the value as camelCase JSON with enums as names, or the string itself for a string; and `McpToolResult.FromToolResult(result)`
- Generator diagnostic `MTAI001`: a `[ToolRequest]` (or `[ToolQuery]`) with no `ResponseType` is now a build error naming the type, instead of generated code that doesn't compile

### Changed

- **Breaking:** `HoverStack` is now pure CSS. It no longer tracks the pointer, so hovering never re-renders it, and its actions also appear on keyboard focus and always on touch devices. `ChildContent` is a plain `RenderFragment` and `HoverContext` is gone. No app read `IsHovering`, so migrating is:
  - Remove `Context="..."` from each `<HoverStack>`
  - Move hand-rolled hover actions (such as a `*-hover-actions` div revealed by the app's own CSS) into `<Actions>`, and delete that CSS
- **Breaking:** `HoverStack`'s `Class` and `Style` now apply to the whole row, actions included, instead of the inner `MudStack`. Row padding, backgrounds and hover styles keep working; a scoped `::deep` rule that targeted the class still matches
- `HoverStack.Wrap` defaults to `NoWrap`, so a row stays one line tall and its content truncates. Pass `Wrap="Wrap.Wrap"` for the old behavior
- Generated request tools return their response as JSON (see `McpToolResult.Json`) instead of `ToString()`, so response types no longer need to override `ToString`
- MCP input schemas describe more types. `Guid` is a `uuid` string, `DateTime` and `DateTimeOffset` are `date-time` strings, `DateOnly`, `TimeOnly` and `Uri` get the `date`, `time` and `uri` formats, enums are strings with an `enum` list of their names, and arrays and lists give their element type in `items`. Dictionaries are `object` rather than `array`, and unsigned integers are `integer`
- Tool arguments accept enum names, so the enum schema round-trips. `Guid` and date strings were already accepted
- A tool call without arguments gets the input type's defaults instead of a null input, so a tool whose parameters are all optional can be called bare
- The HTTP transport answers a request for an unknown or ended session with 404, as the MCP spec asks, which tells the client to initialize again. A request without a session ID, once any client has initialized, is still 400
- **Breaking (generator):** `AddGeneratedMcpTools()` moves from `Mythetech.Framework.AI.Generator.Generated` to `<AssemblyName>.Generated`, so two assemblies that use the generator no longer emit the same public class. Update the `using`; an app that imports two such namespaces calls one of them by its full name. `AddMcpTools(assembly)` is unaffected
- The generator reads a message's `///` summary and parameter docs even when the project doesn't set `GenerateDocumentationFile`. Before, such projects got the fallback descriptions ("Executes the X operation", "The X parameter")

### Deprecated

- `[ToolQuery]`, replaced by `[ToolRequest]`. It is `[Obsolete]` and the generator still recognizes it for this release

### Removed

- `align-items-center` utility class, which was unused and duplicated MudBlazor's `align-center`
- `HoverContext`, replaced by `HoverStack.Actions` (see Changed)

### Fixed

- The HTTP MCP transport kept a single session, so an `initialize` from a second client reset it and dropped the first client. Two Claude Code sessions against one app kept disconnecting each other. The transport now keeps a session per client: each `initialize` starts a new one, requests are checked against their own session, and `DELETE` ends only the caller's
- The HTTP transport matched responses to requests by JSON-RPC id alone, so two clients that both sent id 1 could get each other's response. Requests now get a transport-wide id, and each client sees its own id on the response
- Constructor defaults were ignored by the generator, so `int TopK = 10` arrived as 0 when the caller left it out. The generated input property now starts at the declared default, including enums, strings, `float`, `decimal` and `null`. Parameters with a default were already optional in the schema
- A `ResponseType` that isn't a named type, such as `typeof(string[])`, was dropped by the generator and produced code that didn't compile

### Notes

- All four packages move to `0.21.0`. `Mythetech.Framework.AI.Generator` jumps from `0.14.0` to match the other packages, and from now on its version shows which `Mythetech.Framework` it pairs with. Its generated code calls `McpToolResult.Json` and `McpToolResult.FromToolResult`, so it needs `Mythetech.Framework` `0.21.0` or later

## [0.20.1] - 2026-09-28

### Added

- `OnePasswordCliSecretManager` implements `ISecretWriter`. `SetSecretAsync` stores the value in the password field of the item titled with the key, creating a Password item in the default vault when none has that title and editing the existing item otherwise, so the value reads back through `GetSecretAsync`. `DeleteSecretAsync` permanently deletes the item. A missing item is `NotFound`, and signed-out, expired-session, locked and dismissed or timed-out unlock errors are `AccessDenied`
- The active secret manager survives restarts. `SecretManagerState.SetActiveManagerAsync` switches manager and saves its name in `SecretManagerSettings`, a hidden settings section that `AddSecretManagerFramework` registers. The saved manager becomes active again when it registers or when persisted settings load, whichever comes last, and a saved manager that is no longer registered leaves the first registered one active. The Secret Manager dialog's manager switch saves the choice
- `SecretManagerState.GetManager(string name)`, a case-insensitive lookup of a registered manager, for apps that record which manager holds a secret

### Fixed

- `OnePasswordCliSecretManager` started `op` by name, so it failed in a macOS app launched from Finder (or a Linux app started from a desktop launcher), whose minimal PATH has no Homebrew or `/usr/local/bin`, even with the CLI installed. It now looks for `op` on PATH first and then in the usual install locations: `/opt/homebrew/bin` and `/usr/local/bin` on macOS, `/usr/bin` and `/usr/local/bin` on Linux, and 1Password's documented `C:\Program Files\1Password CLI` on Windows. Empty and relative PATH entries are skipped, and `op` is looked up on every call, so installing it while the app runs needs no restart
- 1Password reads recognised only op's "not signed in" wording, so "You are not currently signed in", a locked 1Password app, an expired session and a dismissed or timed-out unlock prompt came back from `GetSecretAsync`, `ListSecretsAsync` and `TestConnectionAsync` as `Unknown`. Reads now map them to `AccessDenied` exactly as writes do, and an `op` that cannot be started to `ConnectionFailed`

### Security

- 1Password writes never put the secret on `op`'s command line, which other processes can read. A new item's JSON template is piped to `op item create -`, and an existing item's own JSON from `op item get`, with only the password value changed, is piped to `op item edit <id>`

### Changed

- `SecretManagerState`'s constructor takes optional `SecretManagerSettings` and `ISettingsProvider` parameters, which DI supplies. `new SecretManagerState()` still compiles and keeps the choice for the session only; code compiled against an earlier version must be recompiled
- The synchronous `SetActiveManager` overloads still switch manager for the session only. Use `SetActiveManagerAsync` to remember the choice
- `OnePasswordCliSecretManager` passes `op` its arguments as a list instead of a quoted string and reads its output as UTF-8 on every platform, which was already the default on macOS and Linux. Reads and writes share the same `AccessDenied` and `ConnectionFailed` messages

### Notes

- Remembering the choice needs the settings framework (`AddSettingsFramework`, `UseSettingsFramework` and an `ISettingsStorage`) as well as `UseSecretManager`. Hosts that load settings in `SettingsInitializationHook` get the saved manager back when that hook runs, so startup work that depends on the restored manager should run after it
- `Mythetech.Framework`, `Mythetech.Framework.Desktop` and `Mythetech.Framework.WebAssembly` all move to `0.20.1`

## [0.20.0] - 2026-09-27

### Added

- **Smoke checks** for Hermes smoke mode (Hermes 1.4.1). `services.AddSmokeChecks()` opts an app into the general Framework checks and returns a builder for its own: `.WithSmokeCheck<T>()` registers an `ISmokeCheck`. In a smoke run (`HERMES_SMOKE_TEST=1`) the checks run after the first render and after startup initialization, and each appears in the Hermes verdict; outside one nothing is created. `AddSmokeChecks()` can be called before or after the parts it checks
  - `framework/initialization`, for apps with an `IAsyncInitializationHost`: every initialization hook succeeded. A failure names each failed hook and its exception, which the host otherwise only logs
  - `framework/storage`, for apps with an `ISettingsStorage`: the settings store opens and can be read. The LiteDB and SQLite storages log their own failures instead of throwing, so the check probes them directly. It never writes, so a local smoke run leaves real settings untouched
  - `framework/plugins`, for apps using the plugin framework: plugin loading started at startup completed within 30 seconds. Apps that register the plugin framework without loading plugins at startup pass
- `ISmokeTestContext`, the switch for services and components: inject it to turn off side effects such as telemetry in a smoke run. `AddDesktopServices` registers one backed by Hermes smoke mode, and `AddWebAssemblyServices` and `AddSmokeChecks()` register a disabled one, so shared components can inject it on every host
- `ApplicationReady`, published on the message bus on every launch once async initialization finishes, carrying each hook's outcome. Consume it to dismiss a splash screen or start post-startup work. Apps that do not use `IAsyncInitializationHost` can publish it themselves
- `InitializationHookResult` and `IAsyncInitializationHost.Results`: each hook's name, order, duration and exception
- Desktop: in a smoke run `AddDesktopServices` connects all of this to Hermes. The checks wait for an `app-ready` gate that `ApplicationReady` completes, so an app that never runs its initialization host fails with `timed out waiting for app-ready`. Apps without an `IAsyncInitializationHost`, or without a message bus to publish `ApplicationReady` on, do not wait for it
- SampleHost.Desktop registers smoke checks and a startup hook and runs its initialization host, and PR CI smoke tests it on Windows, macOS and Linux

### Changed

- `IAsyncInitializationHost.IsInitialized` becomes true once the run has finished, not when initialization starts
- Calling `IAsyncInitializationHost.InitializeAsync` again while a run is in progress now waits for that run instead of returning at once, so `IsInitialized` is true for every caller once its await completes
- A cancelled initialization run records the hooks it skipped as cancelled, so it no longer reads as a successful run
- `AsyncInitializationHost` takes an optional `IMessageBus` to publish `ApplicationReady`
- `HasSeenPrivacyDialog()` returns true in a smoke run, so apps that check it before showing `PrivacyConsentDialog` never show the dialog there. Nothing is written to the privacy settings
- Mythetech.Framework.Desktop depends on Hermes 1.4.1

### Notes

- `IAsyncInitializationHost.Results` has a default implementation, so custom hosts keep compiling and report no results. A custom host in a smoke run must publish `ApplicationReady` itself, or the run times out on `app-ready`

## [0.19.10] - 2026-09-25

### Fixed

- WebAssembly `SaveFileAsync` closed the picked file's writable stream twice: once explicitly and again when the stream was disposed, because `WritableStream.DisposeAsync` closes the stream itself. Chromium browsers reject the second close with "Cannot close a CLOSED writable stream", so every save through the picker in Chrome and Edge wrote the file and then threw, and apps reported a failed export. The stream is now closed once, by disposal. Firefox, Safari and Desktop were not affected

## [0.19.9] - 2026-09-25

### Fixed

- `FileSystemAccessFileSaveService.SaveFileAsync` (WebAssembly) showed the save picker and returned `true` without writing anything, so exports reported success and left the user an empty file. It now writes the data through the file handle's writable stream and returns `true` only after the stream closes, or `false` when the user cancels
- On browsers without `showSaveFilePicker`, such as Firefox and Safari, WebAssembly `SaveFileAsync` threw `UnsupportedBrowserApiException`. It now falls back to a regular browser download through `file-download.js`, a module the service imports itself, so apps add no script tag. The same fallback covers Chromium refusing the picker once the click's user activation has expired
- The WebAssembly save picker always offered `.txt` files. `SaveFileAsync` now takes the accept type and MIME type from the file name's extension, with no type filter when there is none, and knows the MIME types for `xlsx`, `xls`, `sql`, `md` and `tsv`
- `HermesInteropFileSaveService.SaveFileAsync` (Desktop) filtered the save dialog to `.txt` whatever the file was. The filter now comes from the file name's extension and is named after the extension rather than the file

### Added

- `IFileSaveService.SaveFileAsync(string fileName, byte[] data)` for binary content such as spreadsheets. WebAssembly writes the bytes to the picked file or downloads them, and Desktop writes them to the chosen path. Use it instead of calling `PromptFileSaveAsync` and writing to the returned location, which on WebAssembly is only a file name

### Changed

- `FileSystemAccessFileSaveService`'s public constructor takes an `IJSRuntime` alongside `IFileSystemAccessService`. Apps that register it through `AddFileSaveService` or `AddWebAssemblyServices` need no change
- `IFileSaveService` has a new member, so custom implementations must add the byte array overload

### Notes

- `PromptFileSaveAsync` behaves as before: on WebAssembly it returns only the chosen file's name and still throws `UnsupportedBrowserApiException` where the API is missing

## [0.19.8] - 2026-09-22

### Fixed

- `ShellQuoting.QuoteWindows` (and so `QuotePlatform` and `QuoteIfNeeded` on Windows) escaped for cmd.exe, but `ShellExecutor` never runs arguments through cmd.exe on Windows; it hands them to CreateProcess, whose parser knows nothing about `%`, `^` or `!` and treats backslashes as literal unless they come before a quote. Every quoted value reached the child corrupted: `100%` arrived as `100%%`, `a^b` as `a^^b`, `hi!` as `hi^!` and `C:\Users\tom` as `C:\\Users\\tom`. It now follows the CreateProcess / `CommandLineToArgvW` rules: wrap in quotes, escape `"` as `\"`, double only the backslashes that come before a quote or the closing quote, and leave everything else alone. macOS and Linux were never affected

### Added

- `ShellCommand.ArgumentList` and `WithArgumentList(...)`, the argument-list form of `Arguments`. Each value reaches the child process exactly as given and callers never quote. On Windows and for direct execution the values go straight into `ProcessStartInfo.ArgumentList`; through the macOS and Linux shell the executor POSIX-quotes `Command` and every argument itself when building the `-c` string. Setting both `Arguments` and `ArgumentList` throws `ArgumentException`
- `WasmShellExecutor` passes `ArgumentList` to registered C# and JavaScript handlers as-is, and `WasmShellProcess` to C# handlers, instead of parsing a string
- `ContextPanelItem.ExactRouteMatch`. When true, route highlighting matches only the `RoutePrefix` route itself (ignoring the query string and fragment), not the routes nested beneath it, while double clicking still navigates to `RoutePrefix`. It is for an index page whose child pages have panel items of their own, such as a sessions list beside one item per session: with a plain prefix both icons light up on a child page, and the workaround of passing a null `RoutePrefix` off the index route also took away the double click's destination

### Security

- On Windows, a `.cmd` or `.bat` target runs through cmd.exe even when launched directly, and .NET's `ArgumentList` quoting does not protect against cmd.exe (the BatBadBut class, CVE-2024-24576). `ShellExecutor` throws `NotSupportedException` when an `ArgumentList` value for a batch file contains anything other than letters, digits, spaces and `_ - . , / : = @ + \ ~`. Launch the real `.exe` where one exists (the native Claude Code installer ships `claude.exe`)

### Notes

- `Mythetech.Framework`, `Mythetech.Framework.Desktop` and `Mythetech.Framework.WebAssembly` all move to `0.19.8`. Desktop skips `0.19.7` and WebAssembly skips `0.19.1` through `0.19.7` because those numbers already belong to other packages
- `QuoteWindows` output changes, but only from wrong to right: nothing correct depended on the old output
- `Command` is only quoted by the executor in `ArgumentList` mode. With the legacy `Arguments` string it is still inserted into the shell command as given, so existing callers that pre-quote it or pass a shell snippet keep working. Callers that pre-quote `Command` (Horizon's language server preparer) should move to `ArgumentList` and drop the quoting, since a pre-quoted `Command` is used verbatim as `FileName` on Windows and will not launch
- `Process.Start` with a bare name such as `claude` searches for `.exe` only, never `.cmd`, so npm-installed CLIs need a resolved path on Windows
- `QuoteWindows` is not safe for batch files either. Prefer `ArgumentList` everywhere; `ShellQuoting` remains for callers building strings

## [0.19.6] - 2026-09-18

### Changed

- `Mythetech.Framework.Desktop` moves to `Mythetech.Hermes` and `Mythetech.Hermes.Blazor` 1.4.0, which add native notifications via `HermesApplication.Notifications` and the `INativeNotifications` DI contract. Hermes 1.4.0 introduces no new packages and no native dependencies

### Notes

- Only `Mythetech.Framework.Desktop` is bumped. `0.19.3` through `0.19.5` are skipped because those numbers already belong to `Mythetech.Framework` only releases

## [0.19.5] - 2026-09-16

### Changed

- `InMemoryMessageBus.PublishAsync` no longer allocates per publish when consumers complete synchronously. Consumer, subscriber and pipe lists are cached as arrays, a cancellation source is only created when a timeout is set, activity names are cached per message type and only used when a listener is attached, and only consumers that actually suspend are awaited. A publish to 16 synchronous consumers went from 1,247 ns and 4,016 B to 222 ns and 0 B; with 16 consumers that yield, from 18.2 µs and 10,458 B to 16.7 µs and 4,085 B
- `SendAsync` also skips the cancellation source when no timeout is set and reuses cached activity names
- Consumer filters are now evaluated for each consumer just before it is invoked, rather than for all consumers up front

### Fixed

- A `Subscribe` racing the `Unsubscribe` of the last subscriber for a message type could add the consumer to a list that had just been removed, so it silently never received messages. Subscriber lists are now replaced under the lock instead of mutated

### Added

- `Mythetech.Framework.Benchmarks`, a BenchmarkDotNet project measuring `InMemoryMessageBus.PublishAsync` for consumers that complete synchronously and consumers that yield, at fan-outs of 1, 4 and 16, against a direct fan-out without the bus. It runs in-process because BenchmarkDotNet 0.15.8 has no .NET 11 moniker

### Notes

- Only `Mythetech.Framework` is bumped
- .NET 11 runtime async was evaluated alongside this and not adopted. After these changes it made publishing slightly slower and allocated more (112 B on every publish, about twice as much with yielding consumers), and Mono, which runs Blazor WebAssembly in .NET 11, cannot run it. Worth revisiting on .NET 11 GA or .NET 12

## [0.19.4] - 2026-09-14

### Changed

- Material Symbols Rounded is now embedded in `Mythetech.Framework` at `_content/Mythetech.Framework/fonts/material-symbols-rounded.woff2` and declared by `mythetech.css`. The framework owns the icon font version, every consuming app picks it up through the stylesheet it already links, and icons render without a network connection
- The `@import` of the Azure CDN `material-symbols.css` is gone. That file only pinned Google's CSS, whose `@font-face` still pointed at `fonts.gstatic.com`, so the CDN never removed the runtime dependency on Google

### Added

- `scripts/update-material-symbols.sh` fetches the latest Google Fonts release, downloads the woff2 over the embedded copy and updates the `material-symbols-version` tag in `mythetech.css`. Run it, review the diff, bump the package

### Notes

- Apps should remove their own `<link>` to the Google or CDN Material Symbols stylesheet once they upgrade. Leaving it in place is harmless but reintroduces the network fetch
- The embedded build is the default-axes font (weight 400, no FILL, GRAD or opsz ranges) matching what the CDN served. The `.material-symbols-filled` class defined in some apps sets `FILL` and was never honoured by that build either. Nothing applies the class, so it can be deleted rather than supported

## [0.19.3] - 2026-09-12

### Added

- Double clicking a `ContextPanelItem` in the `AppContextDrawer` navigates to its `RoutePrefix` and collapses the drawer. The icon rail was panel state only, so reaching a page meant clicking the icon, moving into the panel, and clicking again. Panels without a `RoutePrefix` (plugin panels) ignore the gesture
- `ContextPanelItem` renders a `data-panel-id` attribute, so hosts and tests can target a specific panel icon

### Notes

- The browser fires `click`, `click`, `dblclick`, so both single clicks toggle the panel before the double click is seen. `AppContextDrawer` forces the drawer closed on navigation rather than inheriting whatever those clicks left behind, which keeps the end state deterministic
- Double clicking the panel for the page you are already on is a no-op, leaving the two clicks to behave as a plain toggle. A nested page such as `/Endpoints/my-endpoint` still counts as somewhere to navigate away from, so double clicking there takes you up to `/Endpoints`
- Only `Mythetech.Framework` is bumped. `0.19.2` is skipped because that number already belongs to the `Mythetech.Framework.Desktop` only release below

## [0.19.2] - 2026-09-11

### Fixed

- `Mythetech.Framework.Desktop` upgraded to `Mythetech.Hermes.Blazor` 1.3.1, which aligns `Microsoft.AspNetCore.Components.WebView` with the .NET 11 shared framework. The 1.3.0 package embedded the 10.x `blazor.webview.js` against the RC1 `Components.Web` runtime, so any app rendering a `Virtualize` failed at first render with `Blazor._internal.Virtualize.setAnchorMode is not a function`
- Only `Mythetech.Framework.Desktop` is bumped; the other packages stay on their current versions

## [0.19.1] - 2026-09-11

### Removed

- Photino host support from `Mythetech.Framework.Desktop`. Hermes is the only desktop host; nothing in the suite has referenced Photino since Hermes 1.0. Gone: `DesktopHost.Photino`, `AddPhotinoServices()`, `IPhotinoAppProvider`, `PhotinoInteropFileOpenService`, `PhotinoInteropFileSaveService` and the `PhotinoBlazorApp.RegisterProvider()` extensions. `AddDesktopServices()` now defaults to `DesktopHost.Hermes`
- The `Photino.Blazor` package dependency, and with it `Photino.NET` and `Photino.Native`

### Changed

- `Mythetech.Framework.Desktop` now references `Microsoft.Extensions.Logging.Console` directly. Photino brought it transitively and every desktop host calls `AddConsole()`, so keeping it explicit means this stays a patch release. Hosts can take the reference themselves later
- The desktop sample host runs on Hermes
- `Mythetech.Framework` drops the `photino` package tag; bumped to 0.19.1 alongside Desktop

## [0.19.0] - 2026-09-09

### Added

- Guarded JS interop in `Mythetech.Framework.Infrastructure.Guards`, for the new .NET 11 `BL0016` analyzer
  - `InvokeVoidSafeAsync` and `InvokeSafeAsync<T>` ignore a torn-down runtime and rethrow genuine JavaScript errors. Prefer these
  - `TryInvokeVoidAsync` and `TryInvokeAsync<T>` return a `JsInvokeResult` / `JsInvokeResult<T>` carrying `JsInvokeStatus` (`Success`, `Disconnected`, `Failed`) for call sites that act on the difference
  - Disconnection means `JSDisconnectedException` or `ObjectDisposedException`; a `JSException` is a genuine fault and is never treated as disconnection. Anything else propagates
  - See `docs/Infrastructure/Guards/JsInterop.md`

### Fixed

- `DesktopPluginAssetLoader` marked plugin assets as loaded even when the injecting JS call never reached the browser, so an asset lost to a teardown race was never retried. The bookkeeping is now conditional on the call succeeding
- `JsGuardService` treats an unreachable runtime as "not ready" rather than throwing

### Changed

- Retargeted to `net11.0` on the .NET 11 RC1 SDK, pinned via the `sdk` section of `global.json`; CI resolves the SDK from that file instead of a hardcoded `dotnet-version`
- `Mythetech.Framework.AI.Generator` stays on `netstandard2.0`, as source generators must
- Upgraded to Hermes 1.3.0
- Microsoft package pins moved to their latest stable versions (`10.0.12`, Roslyn `5.9.0`, hot reload `10.0.204`). Only packages carrying SDK-locked runtime assets need the RC build, and none of the packable projects do, so all four packages ship with stable dependencies
- Removed the explicit `Microsoft.Extensions.DependencyInjection.Abstractions` reference. .NET 11 reports it as automatically available via `NU1510`, and referencing it explicitly now fails restore

## [0.18.2] - 2026-09-05

### Added

- Key binding subsystem for rebindable keyboard shortcuts
  - `AddKeyBindings()` declares actions, each with an optional default binding, so an action can ship unbound and be assigned by the user later
  - `AddKeyBindingSettings()` adds a shortcuts section to the settings panel and persists overrides
  - `<MtKeyBindings />` registers a `MudHotkey` per bound action; unbound actions register nothing
  - `MtKeyBindingsEditor`, `MtKeyBindingRecorder` and `MtKeyBindingDisplay` provide the settings UI
  - Handlers receive `IMessageBus` and are declared alongside the action
  - Located in the `Mythetech.Framework.Infrastructure.Keyboard` namespace
- Text variant of `CopyButton`

### Changed

- Upgraded Hermes, Desktop, and test packages; resolved vulnerable SQLite dependencies

## [0.18.1] - 2026-05-17

### Added

- `CopyButton` component: cross-platform copy to clipboard with an inline checkmark animation instead of a toast, plus success and error states
- Blazor Server sample host

### Changed

- Clipboard copy reworked to a JS approach that also works under Blazor Server
- WebAssembly services reorganized into a `Services/` folder

## [0.18.0] - 2026-05-10

### Changed

- Plugin storage takes an app name for its base directory so hosts no longer collide on a shared path
- Framework icons standardized on Material Symbols

## [0.17.5] - 2026-05-07

### Fixed

- Settings panel contrast issues

## [0.17.0 - 0.17.4] - 2026-05-02

### Breaking Changes

Command palette providers now receive the query string, so a provider can decide whether to respond to it (for example only answering when the input starts with `>`). Migration is to implement the new signature; the parameter can be ignored.

### Added

- Command palette hosts can opt out of the default hotkey registration

### Fixed

- Command grouping, result ordering, command parsing, and keyboard scroll behavior in the palette
- Command scoring tuned for better matches

## [0.16.2] - 2026-05-01

### Fixed

- Plugin manifests fetched without cache control headers so updates are picked up
- Deleted plugins clean up their files properly

## [0.16.1] - 2026-04-28

### Added

- Shared `NavLink` component
- Fonts and icon assets now ship with the framework instead of being duplicated per app

## [0.16.0] - 2026-04-28

### Changed

- Upgraded Hermes packages for the Hermes GA release

## [0.15.1 - 0.15.2] - 2026-04-24

### Added

- `MtNumericField` component
- Settings sections can be hidden more easily, and sections whose settings are all hidden no longer render

## [0.15.0] - 2026-04-23

### Breaking Changes

Desktop storage classes have been reorganized into provider-specific namespaces under `Storage/`.

| Old Namespace                                             | New Namespace                                |
| --------------------------------------------------------- | -------------------------------------------- |
| `Mythetech.Framework.Desktop` (LiteDbPluginStorage, etc.) | `Mythetech.Framework.Desktop.Storage.LiteDb` |
| `Mythetech.Framework.Desktop.Settings`                    | `Mythetech.Framework.Desktop.Storage.LiteDb` |
| `Mythetech.Framework.Desktop.Queue`                       | `Mythetech.Framework.Desktop.Storage.LiteDb` |

**Migration:** Update `using` directives in consuming projects. Registration extension method names are unchanged; add `using Mythetech.Framework.Desktop.Storage.LiteDb;` where `AddPluginStorage`, `AddDesktopSettingsStorage`, `AddPluginStateProvider`, or `AddLiteDbQueue` are called.

### Added

- SQLite storage provider for Desktop as an alternative to LiteDB, using `Microsoft.Data.Sqlite`
  - `SqlitePluginStorage` / `SqlitePluginStorageFactory` - implements `IPluginStorage` / `IPluginStorageFactory`
  - `SqliteSettingsStorage` - implements `ISettingsStorage`
  - `SqlitePluginStateProvider` - implements `IPluginStateProvider`
  - `SqliteQueue<T>` / `SqliteQueueFactory` - implements `IQueue<T>` / `IQueueFactory`
  - Registration via `AddSqlitePluginStorage()`, `AddSqliteSettingsStorage()`, `AddSqlitePluginStateProvider()`, `AddSqliteQueue()`
  - Located in `Mythetech.Framework.Desktop.Storage.Sqlite` namespace

### Changed

- `Mythetech.Framework` version bumped from 0.14.1 to 0.15.0
- `Mythetech.Framework.Desktop` version bumped from 0.14.0 to 0.15.0
- `Mythetech.Framework.WebAssembly` version bumped from 0.14.0 to 0.15.0

## [0.14.0] - 2026-04-22

### Breaking Changes

Primitive framework components now use the `Mt` prefix to avoid confusion with HTML elements and MudBlazor components. Composed/feature components with descriptive names are unchanged.

| Old Name     | New Name       |
| ------------ | -------------- |
| `Button`     | `MtButton`     |
| `IconButton` | `MtIconButton` |
| `Switch<T>`  | `MtSwitch<T>`  |
| `Badge`      | `MtBadge`      |
| `Kbd`        | `MtKbd`        |

**Migration:** Find and replace component tag names in your `.razor` files. Namespaces (`Mythetech.Framework.Components.Buttons`, `.Switch`, `.Badge`, `.Kbd`) are unchanged, so `@using` directives do not need updating.

### Added

- `MtTextField<T>` - Dense text input component with floating label, adornments, clearable, error state, and multiline support
- `MtSelect<T>` / `MtSelectItem<T>` - Dense select dropdown with keyboard navigation, floating label, and adornment support
- `ProtectedTextField` now renders `MtTextField` when `Dense="true"`, providing the compact styling

### Changed

- `Mythetech.Framework` version bumped from 0.13.6 to 0.14.0
- `Mythetech.Framework.Desktop` version bumped from 0.13.4 to 0.14.0
- `Mythetech.Framework.WebAssembly` version bumped from 0.13.4 to 0.14.0

## [0.13.6] - 2026-04-20

### Added

- Separate active state classes for page and panel navigation
- Route prefix matching so multi-route pages keep their context menu item highlighted
- CSS utility for merging and deduplicating class lists

## [0.13.0 - 0.13.5] - 2026-04-09 to 2026-04-18

### Changed

- Upgraded MudBlazor to v9

### Added

- Command Palette and `Kbd` components
- `IconButton` component with built in tooltip support

### Fixed

- `JsGuard` polling race condition when waiting on script load, and error content shown for JS timeouts
- CSS that bypassed the MudBlazor theme system
- Removed a registration extension that could block startup

## [0.12.7 - 0.12.8] - 2026-04-07 to 2026-04-08

### Added

- Privacy settings and dialog
- Option to hide individual settings from the settings UI

## [0.12.1 - 0.12.2] - 2026-04-04

### Added

- `JsGuard` components for gating UI on JS availability

### Security

- Fixed a zip slip vulnerability in plugin directory extraction

## [0.12.0] - 2026-04-03

### Breaking Changes

The observability abstractions were dropped. They wrapped standard APIs without adding value; use the underlying logging and telemetry APIs directly.

### Added

- `ToolCommand` / `ToolQuery` attributes and the `Mythetech.Framework.AI.Generator` source generator, which builds AI tool definitions from XML documentation comments and those attributes
- The previous attribute remains supported for manually authored tools

## [0.11.0 - 0.11.1] - 2026-02-02

### Added

- Observability packages
- Queue abstraction backed by LiteDB

## [0.10.0] - 2026-01-31

### Added

- Cross-platform shell command execution for Desktop and WebAssembly
- `IAsyncInitializationHook` for use with `IAsyncInitializationHost`, deferring async settings initialization until after the UI renders
- Improved Hermes support and result handling

## [0.9.0 - 0.9.1] - 2026-01-28

### Added

- Hermes desktop provider (experimental)
- NuGet auth for GitHub Packages

## [0.8.0 - 0.8.1] - 2026-01-20 to 2026-01-23

### Added

- Feature flag system
- Plugin auto install, and settings registration methods that work with DI
- Improved update settings UI

## [0.7.0 - 0.7.1] - 2026-01-17

### Added

- App context panel / drawer
- Additional `ComponentConsumer` overloads

### Fixed

- Updates no longer block startup, and update errors are handled properly
- Asset loading conflicts with Monaco

## [0.6.0 - 0.6.1] - 2026-01-14 to 2026-01-15

### Added

- Plugin lifecycle events, plugin settings, and a retry button on the plugin error boundary
- Button component reworked with a code behind and a fuller parameter set

### Changed

- HTTP MCP transport improvements
- Secret manager dialog and settings nav polish

## [0.5.0 - 0.5.7] - 2026-01-12 to 2026-01-13

### Added

- Settings framework ported to framework level: async API over the message bus, typed domain configuration models, and dynamic UI generation for dialogs
- Dynamic settings content sections and support for additional settings ids
- `VirtualizeContainer` and `VirtualizeGrid` for large data sets with volatile height or width (splitter resizing and similar)

### Changed

- Framework plugin and MCP configuration moved onto the new settings framework

### Fixed

- Settings control styling and the boolean settings editor
- Plugin asset encoding

## [0.4.0] - 2026-01-11

### Added

- Toggleable MCP tools
- Documentation for the infrastructure APIs

## [0.3.0 - 0.3.3] - 2026-01-08

### Added

- MCP framework with HTTP transport

## [0.2.0 - 0.2.9] - 2025-12-31 to 2026-01-07

### Added

- Secrets framework with a native secret manager implementation
- Plugin management dialog and registry service, plugin folder open, and dev / preview plugin support
- Message bus query support
- `IRuntimeEnvironment` abstraction and basic environment support
- Clipboard, dialog, and snackbar command providers for Desktop, plus a notification bar component
- Basic telemetry support
- `HoverStack` component
- Service to reveal files and folders in the OS file explorer
- Platform guards for framework code

## [0.1.0 - 0.1.14] - 2025-12-28 to 2025-12-30

### Breaking Changes

Packages were renamed to the `Mythetech.Framework` naming scheme.

### Added

- Generic plugin framework: persistence model, asset loading and preloading, component metadata, URL uploads, plugin level icons, and a plugin guard component
- Test workflow for pull requests

## [0.0.1 - 0.0.9] - 2025-04-27 to 2025-12-24

### Added

- Initial shared component library and storybook, extracting common non-trivial components from the apps
- Tabs with icon, color, and selection-by-name support
- `ProgressCountdown`, `ExternalLink`, `Badge`, and light / dark toggle components
- `ILinkOpenService` with WebAssembly and Desktop (Photino) implementations
- File services
- Message bus documentation and storybook examples

### Changed

- Upgraded to .NET 10
- NuGet package publishing workflow

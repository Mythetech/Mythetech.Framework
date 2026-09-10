# Changelog

## [0.19.0] - 2026-09-09

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

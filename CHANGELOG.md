# Changelog

## [0.15.0] - 2026-04-23

### Breaking Changes

Desktop storage classes have been reorganized into provider-specific namespaces under `Storage/`.

| Old Namespace | New Namespace |
|---|---|
| `Mythetech.Framework.Desktop` (LiteDbPluginStorage, etc.) | `Mythetech.Framework.Desktop.Storage.LiteDb` |
| `Mythetech.Framework.Desktop.Settings` | `Mythetech.Framework.Desktop.Storage.LiteDb` |
| `Mythetech.Framework.Desktop.Queue` | `Mythetech.Framework.Desktop.Storage.LiteDb` |

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

| Old Name | New Name |
|----------|----------|
| `Button` | `MtButton` |
| `IconButton` | `MtIconButton` |
| `Switch<T>` | `MtSwitch<T>` |
| `Badge` | `MtBadge` |
| `Kbd` | `MtKbd` |

**Migration:** Find and replace component tag names in your `.razor` files. Namespaces (`Mythetech.Framework.Components.Buttons`, `.Switch`, `.Badge`, `.Kbd`) are unchanged, so `@using` directives do not need updating.

### Added

- `MtTextField<T>` - Dense text input component with floating label, adornments, clearable, error state, and multiline support
- `MtSelect<T>` / `MtSelectItem<T>` - Dense select dropdown with keyboard navigation, floating label, and adornment support
- `ProtectedTextField` now renders `MtTextField` when `Dense="true"`, providing the compact styling

### Changed

- `Mythetech.Framework` version bumped from 0.13.6 to 0.14.0
- `Mythetech.Framework.Desktop` version bumped from 0.13.4 to 0.14.0
- `Mythetech.Framework.WebAssembly` version bumped from 0.13.4 to 0.14.0

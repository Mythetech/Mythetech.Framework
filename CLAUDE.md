# Mythetech.Framework

> Shared C# patterns, Blazor UI conventions, state management, and testing patterns are in the **global CLAUDE.md** (`~/.claude/CLAUDE.md`). This file covers framework-specific development workflow.

## Project Structure

- **Mythetech.Framework** — Core framework library with components, settings, plugins, and message bus
- **Mythetech.Framework.WebAssembly** — WebAssembly-specific implementations
- **Mythetech.Framework.Desktop** — Desktop specific implementations
- **Mythetech.Framework.Storybook** — Component showcase and testing
- **Mythetech.Framework.Test** — Unit tests
- **samples/** — Sample applications demonstrating framework usage

## Adding New Components

1. **Create the component** in `Mythetech.Framework/Components/[ComponentName]/`
2. **Add a storybook** in `Mythetech.Framework.Storybook/Stories/[ComponentName].stories.razor`
   - Include multiple stories showing different states and configurations
   - See existing stories (e.g., `Button.stories.razor`, `Badge.stories.razor`) for patterns
3. **Add unit tests** if the component has logic that should be tested

## Target Frameworks

- `Mythetech.Framework` and `Mythetech.Framework.WebAssembly` target `net10.0;net11.0`. `net10.0` is the floor: don't use an API that only exists in a newer .NET without a per-framework fallback.
- Microsoft package references are conditioned per target framework, and each must come from that framework's .NET major (`10.0.x` for `net10.0`, `11.0.x` for `net11.0`). Never share one version across target frameworks.
- `Mythetech.Framework.Desktop` targets only the frameworks its Hermes version ships. Samples, the Storybook, benchmarks and tests stay on `net11.0`.
- The `net11.0` build compiles against .NET 11's Components, which ship the `BL0012` and `BL0016` analyzers. Fix their findings with `Infrastructure.Guards` (see `docs/Infrastructure/Guards/JsInterop.md`) rather than suppressing them.

## Build Commands

```bash
# Build entire solution
dotnet build

# Run tests
dotnet test

# Run storybook
dotnet run --project Mythetech.Framework.Storybook
```

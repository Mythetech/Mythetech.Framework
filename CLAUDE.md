# Mythetech.Framework

> Shared C# patterns, Blazor UI conventions, state management, and testing patterns are in the **global CLAUDE.md** (`~/.claude/CLAUDE.md`). This file covers framework-specific development workflow.

## Project Structure

- **Mythetech.Framework** — Core framework library with components, settings, plugins, and message bus
- **Mythetech.Framework.WebAssembly** — WebAssembly-specific implementations
- **Mythetech.Framework.Desktop** — Desktop (Photino) specific implementations
- **Mythetech.Framework.Storybook** — Component showcase and testing
- **Mythetech.Framework.Test** — Unit tests
- **samples/** — Sample applications demonstrating framework usage

## Adding New Components

1. **Create the component** in `Mythetech.Framework/Components/[ComponentName]/`
2. **Add a storybook** in `Mythetech.Framework.Storybook/Stories/[ComponentName].stories.razor`
   - Include multiple stories showing different states and configurations
   - See existing stories (e.g., `Button.stories.razor`, `Badge.stories.razor`) for patterns
3. **Add unit tests** if the component has logic that should be tested

## Build Commands

```bash
# Build entire solution
dotnet build

# Run tests
dotnet test

# Run storybook
dotnet run --project Mythetech.Framework.Storybook
```

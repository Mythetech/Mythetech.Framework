[![PR Tests](https://github.com/Mythetech/Mythetech.Framework/actions/workflows/pr.yml/badge.svg)](https://github.com/Mythetech/Mythetech.Framework/actions/workflows/pr.yml)
[![Deploy Component Storybook](https://github.com/Mythetech/Mythetech.Framework/actions/workflows/storybook.yml/badge.svg)](https://github.com/Mythetech/Mythetech.Framework/actions/workflows/storybook.yml)

[![NuGet Version](https://img.shields.io/nuget/v/Mythetech.Framework?label=Core%20Framework&color=%232d2d2d)](https://www.nuget.org/packages/Mythetech.Framework/)
[![NuGet Version](https://img.shields.io/nuget/v/Mythetech.Framework.Desktop?label=Desktop&color=%232d2d2d)](https://www.nuget.org/packages/Mythetech.Framework.Desktop/)
[![NuGet Version](https://img.shields.io/nuget/v/Mythetech.Framework.WebAssembly?label=WebAssembly&color=%232d2d2d)](https://www.nuget.org/packages/Mythetech.Framework.WebAssembly/)
[![NuGet Version](https://img.shields.io/nuget/v/Mythetech.Framework.AI.Generator?label=AI%20Generator&color=%232d2d2d)](https://www.nuget.org/packages/Mythetech.Framework.AI.Generator/)

```markdown
    ______                                             __  
   / ____/________ _____ ___  ___ _      ______  _____/ /__
  / /_  / ___/ __ `/ __ `__ \/ _ \ | /| / / __ \/ ___/ //_/
 / __/ / /  / /_/ / / / / / /  __/ |/ |/ / /_/ / /  / ,<   
/_/   /_/   \__,_/_/ /_/ /_/\___/|__/|__/\____/_/  /_/|_|  
                                                           
```

# Mythetech.Framework

A collection of reusable Blazor components and abstractions to help build cross platform rich application experiences for desktop and web in blazor.

## Overview

This repository contains a set of custom Blazor components that extend the functionality of MudBlazor, providing a consistent and reusable component library for Mythetech applications. The components are designed to be modular, maintainable, and follow best practices for Blazor development.

The repository also contains abstractions for functionality to work generically with blazor applications acknowledging the behavior for some interactions is broadly different across runtime environments, like desktop in a webview versus natively in WebAssembly in the browser.

## Storybook Demo

The component library leverages [BlazingStory](https://github.com/jsakamoto/BlazingStory) to create a visual storybook of components shipped in the library, with wrappings to provide product-themed previews and light/dark toggles.

The WebAssembly story book is hosted on github pages here: https://mythetech.github.io/Mythetech.Framework/

## Features

- Custom components to either deliver a unique experience or style
- Built on top of MudBlazor for consistent styling, behavior, and theming system
- CSS utility classes that work with existing MudBlazor variables
- In-memory message bus for decoupled communication
- Settings framework with auto-discovery and persistence
- Extensible plugin system with storage and state management
- AI generator integration for LLM-powered features
- Desktop support via [Hermes](https://github.com/Mythetech/Hermes) (recommended) or Photino
- .NET 10.0 support
- Comprehensive test coverage

## Project Structure

- `Mythetech.Framework/` - Main component library
  - `Components/` - Custom Blazor components
  - `Infrastructure/` - Supporting infrastructure code and abstractions
- `Mythetech.Framework.Desktop/` - Desktop application specific implementations (Hermes and Photino hosts)
- `Mythetech.Framework.WebAssembly/` - WebAssembly specific implementations
- `Mythetech.Framework.AI.Generator/` - AI generator integration for LLM-powered features
- `Mythetech.Framework.Storybook/` - Component documentation and showcase
- `Mythetech.Framework.Test/` - Unit tests for components
- `samples/` - Sample applications demonstrating framework usage

## Requirements

- .NET 10.0 SDK
- Editor of choice (Rider, Visual Studio 2022+, or VS Code with C# Dev Kit)

## Getting Started

1. Clone the repository
2. Restore NuGet packages and build:

```bash
dotnet build
```

## Usage

To use these components in your Blazor application:

1. Add a reference to the `Mythetech.Framework` project
2. Add the following to your `_Imports.razor`:

```razor
@using Mythetech.Framework
```

3. Register the services in your `Program.cs`:

The component library provides a lightweight message bus for commands/events

```csharp
builder.Services.AddMessageBus();

...

app.Services.UseMessageBus(typeof(Program).Assembly, typeof(IConsumer<>).Assembly);
```

The library has additional packages to provide concrete implementations for the underlying hosting architecture so generic components and concepts can work across discrete runtimes.

With desktop (Hermes, recommended):

```csharp
builder.Services.AddDesktopServices(DesktopHost.Hermes);

...

HermesBlazorApp app = appBuilder.Build();
app.RegisterHermesProvider();
```

With desktop (Photino):

```csharp
builder.Services.AddDesktopServices(DesktopHost.Photino);

...

PhotinoBlazorApp app = appBuilder.Build();
app.RegisterProvider(app.Services);
```

## Development

### Running Tests

```bash
dotnet test
```

### Storybook

The project includes a Storybook implementation for component documentation and testing. 

```bash
dotnet run --project Mythetech.Framework.Storybook
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## Credits

1. MudBlazor
2. CodeBeams MudExtensions
3. [Hermes](https://github.com/Mythetech/Hermes)

## License

This project is licensed under the terms included in the LICENSE file.

## Support

For support, please open an issue in the GitHub repository.

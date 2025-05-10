```markdown
   ______                                             __      
  / ____/___  ____ ___  ____  ____  ____  ___  ____  / /______
 / /   / __ \/ __ `__ \/ __ \/ __ \/ __ \/ _ \/ __ \/ __/ ___/
/ /___/ /_/ / / / / / / /_/ / /_/ / / / /  __/ / / / /_(__  ) 
\____/\____/_/ /_/ /_/ .___/\____/_/ /_/\___/_/ /_/\__/____/  
                    /_/
```

# Mythetech.Components

A collection of reusable Blazor components and abstractions to help build cross platform rich application experiences for desktop and web in blazor.

## Overview

This repository contains a set of custom Blazor components that extend the functionality of MudBlazor, providing a consistent and reusable component library for Mythetech applications. The components are designed to be modular, maintainable, and follow best practices for Blazor development.

The repository also contains abstractions for functionality to work generically with blazor applications acknowledging the behavior for some interactions is broadly different across runtime environments, like desktop in a webview versus natively in WebAssembly in the browser.

## Storybook Demo

The component library leverages blazingstory to create a visual storybook of components shipped in the library, with wrappings to provide product-themed previews and light/dark toggles.

The WebAssembly story book is hosted on github pages here: https://mythetech.github.io/Mythetech.Components/

## Features

- Custom components to either deliver a unique experience or style
- Built on top of MudBlazor for consistent styling, behavior, and themeing system
- CSS utility classes that work with existing MudBlazor variables
- .NET 9.0 support
- Comprehensive test coverage

## Project Structure

- `Mythetech.Components/` - Main component library
  - `Components/` - Custom Blazor components
  - `Infrastructure/` - Supporting infrastructure code and abstractions
- `Mythetech.Components.Desktop/` - Desktop application specific implementations
- `Mythetech.Components.WebAssembly/` - WebAssembly specific implementations
- `Mythetech.Components.Storybook/` - Component documentation and showcase
- `Mythetech.Components.Test/` - Unit tests for components

## Requirements

- .NET 9.0 SDK
- Visual Studio 2022 or later (recommended)

## Getting Started

1. Clone the repository
2. Open the solution in Visual Studio
3. Restore NuGet packages
4. Build the solution

## Usage

To use these components in your Blazor application:

1. Add a reference to the `Mythetech.Components` project
2. Add the following to your `_Imports.razor`:
```razor
@using Mythetech.Components
```

3. Register the services in your `Program.cs`:

The component library provides a lightweight message bus for commands/events 


```csharp
builder.Services.AddMessageBus();

...

app.Services.UseMessageBus(typeof(Program).Assembly, typeof(IConsumer<>).Assembly);
```

The library has additional packages to provide concrete implementations for the underlying hosting architecture so generic components and concepts can work across discrete runtimes.

With desktop:

```csharp
builder.Services.AddPhotinoServices();

...

 BlazorPhotinoApp app = appBuilder.Build();
 app.RegisterProvider(app.Services);
```

## Development

### Running Tests

The project includes a comprehensive test suite. To run the tests:

1. Open the solution in Visual Studio
2. Use the Test Explorer to run individual tests
3. Or run all tests using the command line:
```bash
dotnet test
```

### Storybook

The project includes a Storybook implementation for component documentation and testing. To run Storybook:

1. Navigate to the `Mythetech.Components.Storybook` directory
2. Run the project:
```bash
dotnet run
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

This project is licensed under the terms included in the LICENSE file.

## Support

For support, please open an issue in the GitHub repository.

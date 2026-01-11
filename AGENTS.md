# Mythetech.Framework Coding Patterns

## Table of Contents

- [Async Patterns](#async-patterns)
- [Blazor Component Patterns](#blazor-component-patterns)
- [State Management](#state-management)
- [Testing Patterns](#testing-patterns)
- [External References](#external-references)

---

## Async Patterns

### Never Use `async void`

**Never mark a method `async void`** - this swallows exceptions and makes debugging impossible.

Bad:
```csharp
private async void OnStateChanged(object? sender, EventArgs e)
{
    await InvokeAsync(StateHasChanged);
}
```

Good (for async work with side effects - use MessageBus):
```csharp
// Dispatch an event that can be handled by an async Task consumer
await MessageBus.PublishAsync(new SomeStateChangedMessage());
```

### Never Fire-and-Forget Async Calls

**Never call an async method without awaiting it.** This includes patterns like `_ = SomeAsync()`.

Bad:
```csharp
protected override void OnInitialized()
{
    _ = SomeService.InitializeAsync(); // Fire and forget - causes bugs
}
```

Good:
```csharp
protected override async Task OnInitializedAsync()
{
    await SomeService.InitializeAsync();
}
```

### Never Call Async from Property Setters

Property setters cannot be async, so calling async methods from them requires fire-and-forget which violates the rules above.

Bad:
```csharp
public string Query
{
    get => _query;
    set
    {
        _query = value;
        _ = SearchAsync(); // Fire-and-forget in setter - banned
    }
}
```

Good:
```csharp
// Use event callbacks from components
<MudTextField @bind-Value="_query" ValueChanged="@OnQueryChangedAsync" />

@code {
    private async Task OnQueryChangedAsync(string value)
    {
        _query = value;
        await SearchAsync();
    }
}
```

---

## Blazor Component Patterns

### Event Handler Subscriptions

When subscribing to C# events (like `StateChanged`) from components:

1. Subscribe in `OnInitialized()`
2. Unsubscribe in `Dispose()`
2. For complex async work, dispatch via `IMessageBus` instead

```csharp
@implements IDisposable

@code {
    [Inject] private SomeState State { get; set; } = default!;

    protected override void OnInitialized()
    {
        State.StateChanged += OnStateChanged;
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        InvokeAsync(StateHasChanged).ContinueWith(t =>
        {
            if (t.IsFaulted && t.Exception != null)
            {
                Console.Error.WriteLine($"Error in OnStateChanged: {t.Exception}");
            }
        }, TaskScheduler.Default);
    }

    public void Dispose()
    {
        State.StateChanged -= OnStateChanged;
    }
}
```

### Cross-Platform Link Opening

**Use `ILinkOpenService` for opening external URLs, never JS interop.**

Since the framework supports desktop apps (Photino), `window.open()` via JS interop doesn't work.

```csharp
@using Mythetech.Framework.Infrastructure

[Inject] protected ILinkOpenService LinkService { get; set; } = default!;

private async Task OpenLink()
{
    await LinkService.OpenLinkAsync("https://example.com");
}
```

---

## State Management

### State Classes Own Their Data

**Never manipulate state from a component and then call a public Notify method.** State classes own their data and mutations.

Bad:
```csharp
// In Component.razor
private void UpdateItems()
{
    var items = State.GetItems().ToList();
    items.Add(newItem);
    State.NotifyChanged(); // Public notify = code smell
}
```

Good:
```csharp
// In Component.razor
private void UpdateItems()
{
    State.AddItem(newItem); // Request change from state
}

// In SomeState.cs
public void AddItem(Item item)
{
    _items.Add(item);
    NotifyStateChanged(); // Private notify
}

private void NotifyStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);
```

### Event Naming Convention

**Use `{Subject}Changed` pattern for events.** Avoid `On` prefix.

Bad:
```csharp
public event EventHandler? OnChange;
public event EventHandler? OnStateChanged;
```

Good:
```csharp
public event EventHandler? StateChanged;
public event EventHandler<bool>? EnabledChanged;
```

---

## Testing Patterns

### Component Testing with bUnit

Use bUnit for component testing with MudBlazor services:

```csharp
public class MyComponentTests : TestContext
{
    public MyComponentTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Component_RendersCorrectly()
    {
        var cut = RenderComponent<MyComponent>(parameters => parameters
            .Add(p => p.Text, "Hello"));

        cut.Markup.ShouldContain("Hello");
    }
}
```

### Mocking Services

Use NSubstitute for interface dependencies:

```csharp
var linkService = Substitute.For<ILinkOpenService>();
Services.AddSingleton(linkService);

// ... render component and trigger action ...

await linkService.Received(1).OpenLinkAsync("https://example.com");
```

### Async Event Testing

For mouse events, use `TriggerEventAsync` with the correct event name:

```csharp
[Fact]
public async Task Component_HandlesMouseEnter()
{
    var cut = RenderComponent<HoverStack>(/* ... */);

    var container = cut.Find("div");
    await container.TriggerEventAsync("onmouseenter", new MouseEventArgs());

    cut.Markup.ShouldContain("Hovering: True");
}
```

---

## External References

### MudBlazor AGENTS.md

This framework is built on MudBlazor. For complex Blazor component patterns, consult **[MudBlazor's AGENTS.md](https://github.com/MudBlazor/MudBlazor/blob/dev/AGENTS.md)** which contains valuable patterns for:

- **ParameterState pattern** for handling async work triggered by parameter changes
- **EventCallback invocation** best practices
- **bUnit testing patterns** for MudBlazor components
- **Why `_ = SomeAsync()` is treated as an error** (CS4014)

MudBlazor utilities and base classes are available for use in this framework.

### Project Structure

- **Mythetech.Framework** - Core component library with UI components and infrastructure
- **Mythetech.Framework.Desktop** - Desktop-specific implementations (Photino, LiteDB)
- **Mythetech.Framework.WebAssembly** - Browser-specific implementations
- **Mythetech.Framework.Storybook** - Component documentation with BlazingStory
- **Mythetech.Framework.Test** - Unit tests with xUnit, bUnit, NSubstitute, Shouldly

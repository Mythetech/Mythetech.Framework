using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mythetech.Framework.Test.Components.Input;

/// <summary>
/// Renders a component the way Blazor Server does and records the value attributes each render sends to the browser.
/// Unlike bUnit, it dispatches events with the field value a browser sends, which Blazor reconciles with its render tree.
/// </summary>
internal sealed class BatchRecorder() : Renderer(new ServiceCollection().BuildServiceProvider(), NullLoggerFactory.Instance)
{
    private readonly Dictionary<string, ulong> _handlers = new(StringComparer.Ordinal);

    public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

    /// <summary>The value attributes the renders sent to the browser, in order.</summary>
    public List<string?> SentValues { get; } = [];

    public Task<int> RenderAsync(IComponent component, IDictionary<string, object?> parameters) => Dispatcher.InvokeAsync(async () =>
    {
        var componentId = AssignRootComponentId(component);
        await RenderRootComponentAsync(componentId, ParameterView.FromDictionary(parameters));
        return componentId;
    });

    /// <summary>Dispatches an event as the browser does, with the field's value as it is in the browser.</summary>
    public Task DispatchAsync(int componentId, string eventName, string fieldValue) =>
        Dispatcher.InvokeAsync(() => DispatchEventAsync(_handlers[eventName],
            new EventFieldInfo { ComponentId = componentId, FieldValue = fieldValue }, new ChangeEventArgs { Value = fieldValue }));

    protected override void HandleException(Exception exception) => ExceptionDispatchInfo.Capture(exception).Throw();

    protected override Task UpdateDisplayAsync(in RenderBatch batch)
    {
        for (var component = 0; component < batch.UpdatedComponents.Count; component++)
        {
            var diff = batch.UpdatedComponents.Array[component];
            for (var index = 0; index < diff.Edits.Count; index++)
            {
                var edit = diff.Edits.Array[index];
                var frame = batch.ReferenceFrames.Array[edit.ReferenceFrameIndex];
                if (edit.Type == RenderTreeEditType.SetAttribute && frame.AttributeName == "value")
                    SentValues.Add(frame.AttributeValue?.ToString());
            }
        }

        // Each render can give an event handler a new id, and the browser dispatches with the newest.
        for (var index = 0; index < batch.ReferenceFrames.Count; index++)
        {
            var frame = batch.ReferenceFrames.Array[index];
            if (frame.FrameType == RenderTreeFrameType.Attribute && frame.AttributeEventHandlerId != 0)
                _handlers[frame.AttributeName] = frame.AttributeEventHandlerId;
        }

        return Task.CompletedTask;
    }
}

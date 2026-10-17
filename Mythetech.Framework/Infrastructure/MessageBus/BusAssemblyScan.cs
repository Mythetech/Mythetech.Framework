using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;

namespace Mythetech.Framework.Infrastructure.MessageBus;

// An assembly's consumers and query handlers, found once and reused by every Add* and Use* call that registers it.
internal static class BusAssemblyScan
{
    private static readonly ConditionalWeakTable<Assembly, Result> Cache = new();

    public static Result Get(Assembly assembly) => Cache.GetValue(assembly, Scan);

    private static Result Scan(Assembly assembly)
    {
        var candidates = AssemblyTypes.Get(assembly)
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => !IsMcpHandler(t) && !IsFeatureFlagHandler(t))
            .Where(t => !typeof(ComponentBase).IsAssignableFrom(t))
            .ToArray();

        var consumers = candidates
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>))
                .Select(i => new ConsumerRegistration(t, i.GetGenericArguments()[0])))
            .Where(c => !c.MessageType.ContainsGenericParameters)
            .ToArray();

        var queryHandlers = candidates
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>))
                .Select(i => new QueryHandlerRegistration(t, i.GetGenericArguments()[0], i.GetGenericArguments()[1])))
            .ToArray();

        return new Result(consumers, queryHandlers);
    }

    private static bool IsMcpHandler(Type type)
        => type.Namespace?.Contains(".Mcp") == true;

    private static bool IsFeatureFlagHandler(Type type)
        => type.Namespace?.Contains(".FeatureFlags") == true;

    internal sealed record Result(ConsumerRegistration[] Consumers, QueryHandlerRegistration[] QueryHandlers);

    internal readonly record struct ConsumerRegistration(Type ConsumerType, Type MessageType);

    internal readonly record struct QueryHandlerRegistration(Type HandlerType, Type MessageType, Type ResponseType);
}

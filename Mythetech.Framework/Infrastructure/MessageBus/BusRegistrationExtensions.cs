using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Mythetech.Framework.Infrastructure.Plugins;

namespace Mythetech.Framework.Infrastructure.MessageBus;

/// <summary>
/// Extensions for registering consumers to work with the message bus
/// </summary>
public static class BusRegistrationExtensions
{
    #region Pipe and Filter Registration
    
    /// <summary>
    /// Register a global message pipe that processes all messages
    /// </summary>
    /// <typeparam name="TPipe">The pipe type</typeparam>
    public static IServiceCollection AddMessagePipe<TPipe>(this IServiceCollection services)
        where TPipe : class, IMessagePipe
    {
        services.AddSingleton<IMessagePipe, TPipe>();
        return services;
    }
    
    /// <summary>
    /// Register a typed message pipe that processes specific message types
    /// </summary>
    /// <typeparam name="TPipe">The pipe type</typeparam>
    /// <typeparam name="TMessage">The message type this pipe handles</typeparam>
    public static IServiceCollection AddMessagePipe<TPipe, TMessage>(this IServiceCollection services)
        where TPipe : class, IMessagePipe<TMessage>
        where TMessage : class
    {
        services.AddSingleton<IMessagePipe<TMessage>, TPipe>();
        return services;
    }
    
    /// <summary>
    /// Register a consumer filter
    /// </summary>
    /// <typeparam name="TFilter">The filter type</typeparam>
    public static IServiceCollection AddConsumerFilter<TFilter>(this IServiceCollection services)
        where TFilter : class, IConsumerFilter
    {
        services.AddSingleton<IConsumerFilter, TFilter>();
        return services;
    }
    
    /// <summary>
    /// Add optional message logging for debugging/visibility.
    /// Logs all messages passing through the bus at Information level.
    /// </summary>
    public static IServiceCollection AddMessageLogging(this IServiceCollection services)
    {
        return services.AddMessagePipe<MessageLoggingPipe>();
    }
    
    /// <summary>
    /// Add a filter that prevents consumers from disabled plugins from receiving messages.
    /// Requires AddPluginFramework() to be called first.
    /// </summary>
    public static IServiceCollection AddPluginConsumerFilter(this IServiceCollection services)
    {
        return services.AddConsumerFilter<DisabledPluginConsumerFilter>();
    }
    
    #endregion

    /// <summary>
    /// Registers consumers to the bus instance
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="assembly">Assembly to search</param>
    public static void RegisterConsumers(this IServiceCollection services, Assembly assembly)
    {
        var consumerTypes = assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => !IsMcpHandler(t))
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>))
                .Select(i => new { ConsumerType = t, MessageType = i.GetGenericArguments()[0] }))
            .Where(c => !typeof(ComponentBase).IsAssignableFrom(c.ConsumerType))
            .Where(c => !c.MessageType.ContainsGenericParameters);

        foreach (var consumer in consumerTypes)
        {
            services.AddTransient(consumer.ConsumerType);
        }
    }

    /// <summary>
    /// Registers query handlers to the DI container from the specified assembly.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="assembly">Assembly to search for query handlers</param>
    public static void RegisterQueryHandlers(this IServiceCollection services, Assembly assembly)
    {
        var handlerTypes = assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => !IsMcpHandler(t))
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>))
                .Select(i => new
                {
                    HandlerType = t,
                    MessageType = i.GetGenericArguments()[0],
                    ResponseType = i.GetGenericArguments()[1]
                }))
            .Where(h => !typeof(ComponentBase).IsAssignableFrom(h.HandlerType));

        foreach (var handler in handlerTypes)
        {
            services.AddTransient(handler.HandlerType);
        }
    }

    private static bool IsMcpHandler(Type type)
        => type.Namespace?.Contains(".Mcp") == true;
    
    /// <summary>
    /// Registers consumers to the bus
    /// </summary>
    /// <param name="bus">Message bus abstraction</param>
    /// <param name="assembly">Assembly to search</param>
    public static void RegisterConsumersToBus(this IMessageBus bus, Assembly assembly)
    {
        var consumerTypes = assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => !IsMcpHandler(t))
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>))
                .Select(i => new { ConsumerType = t, MessageType = i.GetGenericArguments()[0] }))
            .Where(c => !typeof(ComponentBase).IsAssignableFrom(c.ConsumerType))
            .Where(c => !c.MessageType.ContainsGenericParameters);

        foreach (var consumer in consumerTypes)
        {
            bus.RegisterConsumerType(consumer.MessageType, consumer.ConsumerType);
        }
    }

    /// <summary>
    /// Register specific consumer type and message to a bus
    /// </summary>
    /// <param name="bus">Bus to register to</param>
    /// <param name="messageType">Message type</param>
    /// <param name="consumerType">Consumer Type</param>
    public static void RegisterConsumerType(this IMessageBus bus, Type messageType, Type consumerType)
    {
        var method = typeof(IMessageBus).GetMethod(nameof(IMessageBus.RegisterConsumerType))?
            .MakeGenericMethod(messageType, consumerType);
        method?.Invoke(bus, null);
    }

    /// <summary>
    /// Registers query handlers from the specified assembly to the bus.
    /// </summary>
    /// <param name="bus">Message bus abstraction</param>
    /// <param name="assembly">Assembly to search for query handlers</param>
    public static void RegisterQueryHandlersToBus(this IMessageBus bus, Assembly assembly)
    {
        var handlerTypes = assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => !IsMcpHandler(t))
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>))
                .Select(i => new
                {
                    HandlerType = t,
                    MessageType = i.GetGenericArguments()[0],
                    ResponseType = i.GetGenericArguments()[1]
                }))
            .Where(h => !typeof(ComponentBase).IsAssignableFrom(h.HandlerType));

        foreach (var handler in handlerTypes)
        {
            bus.RegisterQueryHandler(handler.MessageType, handler.ResponseType, handler.HandlerType);
        }
    }

    /// <summary>
    /// Register a specific query handler type to a bus
    /// </summary>
    /// <param name="bus">Bus to register to</param>
    /// <param name="messageType">Message type</param>
    /// <param name="responseType">Response type</param>
    /// <param name="handlerType">Handler type</param>
    public static void RegisterQueryHandler(this IMessageBus bus, Type messageType, Type responseType, Type handlerType)
    {
        var method = typeof(IMessageBus).GetMethod(nameof(IMessageBus.RegisterQueryHandler))?
            .MakeGenericMethod(messageType, responseType, handlerType);
        method?.Invoke(bus, null);
    }

    /// <summary>
    /// Add message bus to the DI container
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns></returns>
    public static IServiceCollection AddMessageBus(this IServiceCollection services)
    {
        services.AddSingleton<IMessageBus, InMemoryMessageBus>();

        var executingAssembly = Assembly.GetExecutingAssembly();
        services.RegisterConsumers(executingAssembly);
        services.RegisterQueryHandlers(executingAssembly);

        return services;
    }

    /// <summary>
    /// Add message bus to the DI container for arbitrary assemblies
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="assemblies">Assemblies to register from</param>
    public static IServiceCollection AddMessageBus(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddSingleton<IMessageBus, InMemoryMessageBus>();

        foreach (var assembly in assemblies)
        {
            services.RegisterConsumers(assembly);
            services.RegisterQueryHandlers(assembly);
        }

        return services;
    }

    /// <summary>
    /// Registers services for the bus
    /// </summary>
    /// <param name="serviceProvider">Built service provider</param>
    public static IServiceProvider UseMessageBus(this IServiceProvider serviceProvider)
    {
        var bus = serviceProvider.GetRequiredService<IMessageBus>();
        var executingAssembly = Assembly.GetExecutingAssembly();
        bus.RegisterConsumersToBus(executingAssembly);
        bus.RegisterQueryHandlersToBus(executingAssembly);

        return serviceProvider;
    }

    /// <summary>
    /// Registers services for the bus
    /// </summary>
    /// <param name="serviceProvider">Built service provider</param>
    /// <param name="assemblies">Registers consumers and query handlers in each assembly</param>
    public static IServiceProvider UseMessageBus(this IServiceProvider serviceProvider, params Assembly[] assemblies)
    {
        var bus = serviceProvider.GetRequiredService<IMessageBus>();
        foreach (var assembly in assemblies)
        {
            bus.RegisterConsumersToBus(assembly);
            bus.RegisterQueryHandlersToBus(assembly);
        }
        return serviceProvider;
    }
}
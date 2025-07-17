using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Mythetech.Components.Infrastructure.MessageBus;

/// <summary>
/// Extensions for registering consumers to work with the message bus
/// </summary>
public static class BusRegistrationExtensions
{
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
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>))
                .Select(i => new { ConsumerType = t, MessageType = i.GetGenericArguments()[0] }))
            .Where(c => !typeof(ComponentBase).IsAssignableFrom(c.ConsumerType)); 

        foreach (var consumer in consumerTypes)
        {
            services.AddTransient(consumer.ConsumerType);
        }
    }
    
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
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>))
                .Select(i => new { ConsumerType = t, MessageType = i.GetGenericArguments()[0] }))
            .Where(c => !typeof(ComponentBase).IsAssignableFrom(c.ConsumerType));

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
    /// Add message bus to the DI container
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns></returns>
    public static IServiceCollection AddMessageBus(this IServiceCollection services)
    {
        services.AddSingleton<IMessageBus, InMemoryMessageBus>();
        
        services.RegisterConsumers(Assembly.GetExecutingAssembly());
        
        return services;
    }
    
    /// <summary>
    /// Add message bus to the DI container for arbitrary assemblies
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="assemblies">Assemblies to register from</param>
    public static IServiceCollection AddMessageBus(this IServiceCollection services,  params Assembly[] assemblies)
    {
        services.AddSingleton<IMessageBus, InMemoryMessageBus>();
        
        foreach (var assembly in assemblies)
        {
            services.RegisterConsumers(assembly);
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
        bus.RegisterConsumersToBus(Assembly.GetExecutingAssembly());
        
        return serviceProvider;
    }
    
    /// <summary>
    /// Registers services for the bus
    /// </summary>
    /// <param name="serviceProvider">Built service provider</param>
    /// <param name="assemblies">Registers consumers in each assembly</param>
    public static IServiceProvider UseMessageBus(this IServiceProvider serviceProvider, params Assembly[] assemblies)
    {
        var bus = serviceProvider.GetRequiredService<IMessageBus>();
        foreach (var assembly in assemblies)
        {
            bus.RegisterConsumersToBus(assembly);
        }
        return serviceProvider;
    }
}
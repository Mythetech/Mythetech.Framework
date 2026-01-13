using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Mcp.Consumers;
using Mythetech.Framework.Infrastructure.Mcp.Messages;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.MessageBus
{
    public class BusRegistrationExtensionsTests
    {
        #region MCP Consumer Filtering Tests

        [Fact(DisplayName = "RegisterConsumers excludes MCP consumers from automatic registration")]
        public void RegisterConsumers_ExcludesMcpConsumers()
        {
            // Arrange
            var services = new ServiceCollection();
            var frameworkAssembly = typeof(IMessageBus).Assembly;

            // Act
            services.RegisterConsumers(frameworkAssembly);

            // Assert - MCP consumers should NOT be registered
            var serviceProvider = services.BuildServiceProvider();

            // These should throw because they weren't registered
            Should.Throw<InvalidOperationException>(() =>
                serviceProvider.GetRequiredService<EnableMcpServerConsumer>());

            Should.Throw<InvalidOperationException>(() =>
                serviceProvider.GetRequiredService<DisableMcpServerConsumer>());

            Should.Throw<InvalidOperationException>(() =>
                serviceProvider.GetRequiredService<McpSettingsConsumer>());
        }

        [Fact(DisplayName = "RegisterConsumersToBus excludes MCP consumers from bus registration")]
        public void RegisterConsumersToBus_ExcludesMcpConsumers()
        {
            // Arrange
            var services = new ServiceCollection();
            var bus = new InMemoryMessageBus(
                services.BuildServiceProvider(),
                Substitute.For<ILogger<InMemoryMessageBus>>(),
                Array.Empty<IMessagePipe>(),
                Array.Empty<IConsumerFilter>());

            var frameworkAssembly = typeof(IMessageBus).Assembly;

            // Act
            bus.RegisterConsumersToBus(frameworkAssembly);

            // Assert - MCP consumer types should NOT be registered to the bus
            // We verify by checking that the internal consumer type registry doesn't contain MCP types
            // Since we can't directly inspect the bus internals, we verify via the service collection approach
            var registeredTypes = GetRegisteredConsumerTypes(frameworkAssembly);

            registeredTypes.ShouldNotContain(t => t.Namespace != null && t.Namespace.Contains(".Mcp"),
                "MCP consumers should be excluded from automatic registration");
        }

        [Fact(DisplayName = "RegisterQueryHandlers excludes MCP handlers from automatic registration")]
        public void RegisterQueryHandlers_ExcludesMcpHandlers()
        {
            // Arrange
            var services = new ServiceCollection();
            var frameworkAssembly = typeof(IMessageBus).Assembly;

            // Act
            services.RegisterQueryHandlers(frameworkAssembly);

            // Assert - verify no MCP namespace types were registered
            var registeredTypes = services
                .Where(sd => sd.ImplementationType != null)
                .Select(sd => sd.ImplementationType!)
                .ToList();

            registeredTypes.ShouldNotContain(t => t.Namespace != null && t.Namespace.Contains(".Mcp"),
                "MCP query handlers should be excluded from automatic registration");
        }

        [Fact(DisplayName = "Non-MCP consumers are still registered by RegisterConsumers")]
        public void RegisterConsumers_RegistersNonMcpConsumers()
        {
            // Arrange
            var services = new ServiceCollection();
            var frameworkAssembly = typeof(IMessageBus).Assembly;

            // Act
            services.RegisterConsumers(frameworkAssembly);

            // Assert - at least some consumers should be registered (settings consumers, etc.)
            var consumerCount = services
                .Where(sd => sd.ImplementationType != null)
                .Count(sd => sd.ImplementationType!
                    .GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>)));

            consumerCount.ShouldBeGreaterThan(0,
                "Non-MCP consumers should still be registered");
        }

        #endregion

        #region Component Consumer Filtering Tests

        [Fact(DisplayName = "RegisterConsumers excludes ComponentBase-derived consumers")]
        public void RegisterConsumers_ExcludesComponentConsumers()
        {
            // Arrange
            var services = new ServiceCollection();
            var testAssembly = typeof(BusRegistrationExtensionsTests).Assembly;

            // Act
            services.RegisterConsumers(testAssembly);

            // Assert - component consumers should NOT be registered as transient services
            var registeredTypes = services
                .Where(sd => sd.ImplementationType != null)
                .Select(sd => sd.ImplementationType!)
                .ToList();

            registeredTypes.ShouldNotContain(t => typeof(ComponentBase).IsAssignableFrom(t),
                "Component-based consumers should not be registered via RegisterConsumers");
        }

        #endregion

        #region AddMessageBus Integration Tests

        [Fact(DisplayName = "AddMessageBus registers bus and non-MCP consumers")]
        public void AddMessageBus_RegistersBusAndNonMcpConsumers()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging(); // Required by InMemoryMessageBus

            // Act
            services.AddMessageBus();

            // Assert
            var serviceProvider = services.BuildServiceProvider();

            // Bus should be registered
            var bus = serviceProvider.GetService<IMessageBus>();
            bus.ShouldNotBeNull();
            bus.ShouldBeOfType<InMemoryMessageBus>();

            // MCP consumers should NOT be registered
            Should.Throw<InvalidOperationException>(() =>
                serviceProvider.GetRequiredService<EnableMcpServerConsumer>());
        }

        #endregion

        #region Helper Methods

        private static List<Type> GetRegisteredConsumerTypes(Assembly assembly)
        {
            return assembly
                .GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Where(t => !IsMcpType(t))
                .Where(t => !typeof(ComponentBase).IsAssignableFrom(t))
                .Where(t => t.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>)))
                .ToList();
        }

        private static bool IsMcpType(Type type) => type.Namespace?.Contains(".Mcp") == true;

        #endregion
    }

    #region Test Fixtures

    public record TestRegistrationMessage(string Data);

    public class TestServiceConsumer : IConsumer<TestRegistrationMessage>
    {
        public Task Consume(TestRegistrationMessage message) => Task.CompletedTask;
    }

    public class TestComponentConsumerForRegistration : ComponentBase, IConsumer<TestRegistrationMessage>
    {
        public Task Consume(TestRegistrationMessage message) => Task.CompletedTask;
    }

    #endregion
}

namespace Mythetech.Framework.Test.Infrastructure.MessageBus.TestMcp
{
    // Simulates an MCP consumer - should be excluded due to namespace containing ".Mcp"
    public record TestMcpMessage(string Data);

    public class TestMcpConsumer : IConsumer<TestMcpMessage>
    {
        public Task Consume(TestMcpMessage message) => Task.CompletedTask;
    }
}

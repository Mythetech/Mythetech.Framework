using System.Diagnostics;
using System.Reflection;

namespace Mythetech.Framework.Infrastructure.Telemetry;

/// <summary>
/// Central ActivitySource definitions for distributed tracing across the Mythetech Framework.
/// </summary>
public static class FrameworkTelemetry
{
    /// <summary>
    /// Gets the version from the entry assembly, falling back to "1.0.0" if unavailable.
    /// This makes telemetry version dynamic based on the consuming application.
    /// </summary>
    public static string GetVersion()
        => Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0";

    /// <summary>
    /// ActivitySource for message bus operations.
    /// </summary>
    public static readonly ActivitySource MessageBusSource = new("Mythetech.MessageBus", GetVersion());

    /// <summary>
    /// Common tag names for consistency across traces.
    /// </summary>
    public static class Tags
    {
        /// <summary>
        /// The type of message being published or queried.
        /// </summary>
        public const string MessageType = "mythetech.message.type";

        /// <summary>
        /// The type of consumer handling the message.
        /// </summary>
        public const string ConsumerType = "mythetech.consumer.type";

        /// <summary>
        /// The type of query handler processing the query.
        /// </summary>
        public const string HandlerType = "mythetech.handler.type";

        /// <summary>
        /// Whether the operation completed successfully.
        /// </summary>
        public const string Success = "mythetech.success";

        /// <summary>
        /// Error message if the operation failed.
        /// </summary>
        public const string ErrorMessage = "mythetech.error.message";

        /// <summary>
        /// The number of consumers that received the message.
        /// </summary>
        public const string ConsumerCount = "mythetech.consumer.count";
    }
}

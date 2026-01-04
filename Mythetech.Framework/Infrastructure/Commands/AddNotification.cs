using MudBlazor;

namespace Mythetech.Framework.Infrastructure.Commands;

/// <summary>
/// Command message to display a notification snackbar.
/// </summary>
/// <param name="Message">The notification message to display</param>
/// <param name="Severity">The severity level of the notification</param>
public record AddNotification(string Message, Severity Severity);

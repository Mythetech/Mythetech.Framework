using MudBlazor;

namespace Mythetech.Framework.Components.Snackbar;

/// <summary>
/// Extension methods for <see cref="ISnackbar"/> to display Mythetech-styled notifications.
/// </summary>
public static class SnackbarExtensions
{
    /// <summary>
    /// Adds a Mythetech-styled notification to the snackbar.
    /// </summary>
    /// <param name="snackbar">The snackbar service</param>
    /// <param name="message">The message to display</param>
    /// <param name="severity">The severity level (affects color and duration)</param>
    public static void AddMythetechNotification(this ISnackbar snackbar, string message, Severity severity = Severity.Info)
    {
        var parameters = new Dictionary<string, object>
        {
            { "Message", message },
            { "Severity", severity }
        };

        snackbar.Add<MythetechNotificationBar>(parameters, severity, configure =>
        {
            switch (severity)
            {
                case Severity.Error:
                    configure.VisibleStateDuration = 5500;
                    configure.ActionColor = Color.Error;
                    configure.IconColor = Color.Error;
                    break;
                case Severity.Success:
                    configure.ActionColor = Color.Success;
                    configure.IconColor = Color.Success;
                    break;
                case Severity.Warning:
                    configure.ActionColor = Color.Warning;
                    configure.IconColor = Color.Warning;
                    break;
                case Severity.Info:
                    configure.ActionColor = Color.Info;
                    configure.IconColor = Color.Info;
                    break;
                case Severity.Normal:
                    configure.ActionColor = Color.Inherit;
                    configure.IconColor = Color.Inherit;
                    break;
            }

            configure.BackgroundBlurred = true;
        });
    }

    /// <summary>
    /// Adds an info notification to the snackbar.
    /// </summary>
    public static void AddInfo(this ISnackbar snackbar, string message)
        => snackbar.AddMythetechNotification(message, Severity.Info);

    /// <summary>
    /// Adds a success notification to the snackbar.
    /// </summary>
    public static void AddSuccess(this ISnackbar snackbar, string message)
        => snackbar.AddMythetechNotification(message, Severity.Success);

    /// <summary>
    /// Adds a warning notification to the snackbar.
    /// </summary>
    public static void AddWarning(this ISnackbar snackbar, string message)
        => snackbar.AddMythetechNotification(message, Severity.Warning);

    /// <summary>
    /// Adds an error notification to the snackbar.
    /// </summary>
    public static void AddError(this ISnackbar snackbar, string message)
        => snackbar.AddMythetechNotification(message, Severity.Error);
}

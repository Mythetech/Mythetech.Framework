namespace Mythetech.Components.WebAssembly.Exceptions;

/// <summary>
/// Exception thrown when a browser API is not supported by the current browser
/// </summary>
public class UnsupportedBrowserApiException : Exception
{
    /// <summary>
    /// The name of the unsupported API
    /// </summary>
    public string ApiName { get; }

    /// <summary>
    /// Creates a new instance of the exception
    /// </summary>
    /// <param name="apiName">The name of the unsupported API</param>
    public UnsupportedBrowserApiException(string apiName)
        : base($"The browser API '{apiName}' is not supported by the current browser")
    {
        ApiName = apiName;
    }

    /// <summary>
    /// Creates a new instance of the exception with an inner exception
    /// </summary>
    /// <param name="apiName">The name of the unsupported API</param>
    /// <param name="innerException">The inner exception that caused this exception</param>
    public UnsupportedBrowserApiException(string apiName, Exception innerException)
        : base($"The browser API '{apiName}' is not supported by the current browser", innerException)
    {
        ApiName = apiName;
    }
}


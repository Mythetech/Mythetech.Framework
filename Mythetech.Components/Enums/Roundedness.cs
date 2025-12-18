namespace Mythetech.Components.Enums;

/// <summary>
/// Controls roundedness of a component
/// </summary>
public enum Roundedness
{
    /// <summary>
    /// No roundedness
    /// </summary>
    None,
    /// <summary>
    /// Small amount of rounding
    /// </summary>
    Small,
    /// <summary>
    /// Medium amount of rounding
    /// </summary>
    Medium,
    /// <summary>
    /// Large amount of rounding
    /// </summary>
    Large,
    /// <summary>
    /// XL amount of rounding
    /// </summary>
    XL,
    /// <summary>
    /// Pill shape
    /// </summary>
    Pill,
}

/// <summary>
/// Turns rounded enum to string classes
/// </summary>
public static class RoundedExtensions
{
    /// <summary>
    /// Class name for the rounded value
    /// </summary>
    public static string Class(this Roundedness roundedness)
    {
        return roundedness switch
        {
            Roundedness.None => "square",
            Roundedness.Small => "rounded",
            Roundedness.Medium => "rounded-md",
            Roundedness.Large => "rounded-lg",
            Roundedness.XL => "rounded-xl",
            Roundedness.Pill => "rounded-pill",
            _ => throw new ArgumentOutOfRangeException(nameof(roundedness), roundedness, null)
        };
    }
}
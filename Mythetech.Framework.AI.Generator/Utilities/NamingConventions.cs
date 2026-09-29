using System.Text;

namespace Mythetech.Framework.AI.Generator.Utilities;

/// <summary>
/// Utilities for converting between naming conventions.
/// </summary>
public static class NamingConventions
{
    /// <summary>
    /// Converts PascalCase to snake_case.
    /// </summary>
    public static string ToSnakeCase(string pascalCase)
    {
        if (string.IsNullOrEmpty(pascalCase))
            return pascalCase;

        var result = new StringBuilder();

        for (int i = 0; i < pascalCase.Length; i++)
        {
            var c = pascalCase[i];

            if (i > 0 && char.IsUpper(c))
            {
                result.Append('_');
            }

            result.Append(char.ToLowerInvariant(c));
        }

        return result.ToString();
    }

    /// <summary>
    /// Converts a parameter name to camelCase.
    /// </summary>
    public static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        if (char.IsLower(name[0]))
            return name;

        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }

    /// <summary>
    /// Converts a parameter name to PascalCase.
    /// </summary>
    public static string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        if (char.IsUpper(name[0]))
            return name;

        return char.ToUpperInvariant(name[0]) + name.Substring(1);
    }

    /// <summary>
    /// Converts an assembly name to a valid namespace, replacing characters that
    /// are not allowed in identifiers with underscores.
    /// </summary>
    public static string ToNamespace(string? assemblyName)
    {
        if (string.IsNullOrWhiteSpace(assemblyName))
            return "Mythetech.Framework.AI.Generator";

        var segments = assemblyName!.Split('.')
            .Where(segment => segment.Length > 0)
            .Select(ToIdentifier);

        return string.Join(".", segments);
    }

    private static string ToIdentifier(string segment)
    {
        var result = new StringBuilder(segment.Length + 1);

        if (!char.IsLetter(segment[0]) && segment[0] != '_')
        {
            result.Append('_');
        }

        foreach (var c in segment)
        {
            result.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
        }

        return result.ToString();
    }
}

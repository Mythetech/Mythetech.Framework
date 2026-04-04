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
}

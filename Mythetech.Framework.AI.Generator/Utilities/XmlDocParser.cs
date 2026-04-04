using System.Xml;

namespace Mythetech.Framework.AI.Generator.Utilities;

/// <summary>
/// Parses XML documentation comments to extract summaries and parameter descriptions.
/// </summary>
public static class XmlDocParser
{
    /// <summary>
    /// Parses the XML documentation and returns a structured result.
    /// </summary>
    public static ParsedXmlDoc Parse(string? xml)
    {
        var result = new ParsedXmlDoc();

        if (string.IsNullOrWhiteSpace(xml))
            return result;

        try
        {
            var doc = new XmlDocument { XmlResolver = null };
            doc.LoadXml($"<root>{xml}</root>");

            var summaryNode = doc.SelectSingleNode("//summary");
            if (summaryNode != null)
            {
                result.Summary = CleanXmlText(summaryNode.InnerText);
            }

            var paramNodes = doc.SelectNodes("//param");
            if (paramNodes != null)
            {
                foreach (XmlNode paramNode in paramNodes)
                {
                    var name = paramNode.Attributes?["name"]?.Value;
                    if (!string.IsNullOrEmpty(name))
                    {
                        result.ParamDescriptions[name] = CleanXmlText(paramNode.InnerText);
                    }
                }
            }
        }
        catch
        {
            // Silently ignore XML parsing errors
        }

        return result;
    }

    private static string CleanXmlText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return string.Join(" ", text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries));
    }
}

/// <summary>
/// Result of parsing XML documentation.
/// </summary>
public sealed class ParsedXmlDoc
{
    public string? Summary { get; set; }
    public Dictionary<string, string> ParamDescriptions { get; } = new(StringComparer.OrdinalIgnoreCase);

    public string GetParamDescription(string paramName)
    {
        return ParamDescriptions.TryGetValue(paramName, out var desc) ? desc : $"The {paramName} parameter";
    }
}

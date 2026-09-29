using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Mythetech.Framework.AI.Generator.Emitters;
using Mythetech.Framework.AI.Generator.Models;
using Mythetech.Framework.AI.Generator.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Mythetech.Framework.AI.Generator;

/// <summary>
/// Incremental source generator that creates MCP tools from [ToolCommand] and [ToolRequest] decorated types.
/// </summary>
[Generator]
public class McpToolGenerator : IIncrementalGenerator
{
    private const string ToolCommandAttributeName = "Mythetech.Framework.Infrastructure.Mcp.ToolCommandAttribute";
    private const string ToolRequestAttributeName = "Mythetech.Framework.Infrastructure.Mcp.ToolRequestAttribute";
    private const string ToolQueryAttributeName = "Mythetech.Framework.Infrastructure.Mcp.ToolQueryAttribute";
    private const string ToolResultTypeName = "Mythetech.Framework.Infrastructure.Mcp.ToolResult<T>";

    private static readonly DiagnosticDescriptor MissingResponseType = new(
        id: "MTAI001",
        title: "Tool request has no response type",
        messageFormat: "[{0}] on '{1}' needs a ResponseType, for example [{0}(ResponseType = typeof(MyResponse))]",
        category: "Mythetech.Framework.AI.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var commandDeclarations = FindTools(context, ToolCommandAttributeName, "ToolCommand", isRequest: false);
        var requestDeclarations = FindTools(context, ToolRequestAttributeName, "ToolRequest", isRequest: true);
        var queryDeclarations = FindTools(context, ToolQueryAttributeName, "ToolQuery", isRequest: true);

        var allDeclarations = commandDeclarations.Collect()
            .Combine(requestDeclarations.Collect())
            .Combine(queryDeclarations.Collect())
            .Select(static (pair, _) => pair.Left.Left.AddRange(pair.Left.Right).AddRange(pair.Right));

        var assemblyName = context.CompilationProvider.Select(static (compilation, _) => compilation.AssemblyName);

        context.RegisterSourceOutput(allDeclarations.Combine(assemblyName),
            static (ctx, pair) => GenerateTools(ctx, pair.Left, pair.Right));
    }

    private static IncrementalValuesProvider<ToolMetadata> FindTools(
        IncrementalGeneratorInitializationContext context,
        string attributeMetadataName,
        string attributeName,
        bool isRequest)
    {
        return context.SyntaxProvider
            .ForAttributeWithMetadataName(
                attributeMetadataName,
                predicate: static (node, _) => node is RecordDeclarationSyntax or ClassDeclarationSyntax,
                transform: (ctx, ct) => ExtractToolMetadata(ctx, ct, attributeName, isRequest))
            .Where(static m => m is not null)
            .Select(static (m, _) => m!);
    }

    private static ToolMetadata? ExtractToolMetadata(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken,
        string attributeName,
        bool isRequest)
    {
        if (context.TargetSymbol is not INamedTypeSymbol typeSymbol)
            return null;

        var attribute = context.Attributes.FirstOrDefault();
        if (attribute is null)
            return null;

        string? name = null;
        string? description = null;
        ITypeSymbol? responseType = null;

        foreach (var namedArg in attribute.NamedArguments)
        {
            switch (namedArg.Key)
            {
                case "Name":
                    name = namedArg.Value.Value as string;
                    break;
                case "Description":
                    description = namedArg.Value.Value as string;
                    break;
                case "ResponseType":
                    responseType = namedArg.Value.Value as ITypeSymbol;
                    break;
            }
        }

        var xmlDoc = typeSymbol.GetDocumentationCommentXml(cancellationToken: cancellationToken);
        if (string.IsNullOrWhiteSpace(xmlDoc))
        {
            xmlDoc = ReadDocumentationComment(context.TargetNode);
        }

        var parsedDoc = XmlDocParser.Parse(xmlDoc);

        var parameters = ExtractParameters(typeSymbol, parsedDoc);

        return new ToolMetadata(
            typeName: typeSymbol.Name,
            fullTypeName: typeSymbol.ToDisplayString(),
            ns: typeSymbol.ContainingNamespace.ToDisplayString(),
            toolName: name ?? NamingConventions.ToSnakeCase(typeSymbol.Name),
            description: description ?? parsedDoc.Summary ?? $"Executes the {typeSymbol.Name} operation",
            parameters: parameters,
            attributeName: attributeName,
            location: attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation(),
            isRequest: isRequest,
            responseTypeName: responseType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            isToolResult: IsToolResult(responseType)
        );
    }

    /// <summary>
    /// Reads a type's /// comment from its syntax. The compiler only provides documentation XML when
    /// the project generates a documentation file, and tool descriptions shouldn't depend on that.
    /// </summary>
    private static string? ReadDocumentationComment(SyntaxNode declaration)
    {
        var lines = declaration.GetLeadingTrivia()
            .Where(trivia => trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                             trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
            .SelectMany(trivia => trivia.ToFullString().Split('\n'))
            .Select(line => line.Trim())
            .Where(line => line.StartsWith("///", StringComparison.Ordinal))
            .Select(line => line.Substring(3))
            .ToList();

        return lines.Count > 0 ? string.Join("\n", lines) : null;
    }

    private static bool IsToolResult(ITypeSymbol? type) =>
        type is INamedTypeSymbol { IsGenericType: true } named &&
        named.ConstructedFrom.ToDisplayString() == ToolResultTypeName;

    private static List<ParameterMetadata> ExtractParameters(
        INamedTypeSymbol typeSymbol,
        ParsedXmlDoc xmlDoc)
    {
        var parameters = new List<ParameterMetadata>();

        var primaryCtor = typeSymbol.InstanceConstructors
            .Where(c => c.DeclaredAccessibility == Accessibility.Public)
            .Where(c => !IsCopyConstructor(c, typeSymbol))
            .OrderByDescending(c => c.Parameters.Length)
            .FirstOrDefault();

        if (primaryCtor is null || primaryCtor.Parameters.Length == 0)
            return parameters;

        foreach (var param in primaryCtor.Parameters)
        {
            var isNullable = param.Type.NullableAnnotation == NullableAnnotation.Annotated ||
                             param.Type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

            parameters.Add(new ParameterMetadata(
                name: param.Name,
                typeName: param.Type.ToDisplayString(),
                description: xmlDoc.GetParamDescription(param.Name),
                isRequired: !param.IsOptional && !isNullable,
                defaultValueLiteral: FormatDefaultValue(param)
            ));
        }

        return parameters;
    }

    /// <summary>
    /// Formats a parameter's declared default as a C# expression for the generated input property.
    /// </summary>
    private static string? FormatDefaultValue(IParameterSymbol param)
    {
        if (!param.HasExplicitDefaultValue)
            return null;

        var value = param.ExplicitDefaultValue;
        if (value is null)
            return "default!";

        var type = param.Type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable
            ? nullable.TypeArguments[0]
            : param.Type;

        // An enum default is stored as its underlying number
        if (type.TypeKind == TypeKind.Enum)
        {
            var enumType = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return $"({enumType})({SymbolDisplay.FormatPrimitive(value, quoteStrings: false, useHexadecimalNumbers: false)})";
        }

        return value switch
        {
            string s => SymbolDisplay.FormatLiteral(s, quote: true),
            char c => SymbolDisplay.FormatLiteral(c, quote: true),
            bool b => b ? "true" : "false",
            double d => FormatReal(d, "double", "D"),
            float f => FormatReal(f, "float", "F"),
            decimal m => m.ToString(CultureInfo.InvariantCulture) + "M",
            _ => SymbolDisplay.FormatPrimitive(value, quoteStrings: true, useHexadecimalNumbers: false)
        };
    }

    private static string FormatReal(double value, string keyword, string suffix)
    {
        if (double.IsNaN(value))
            return $"{keyword}.NaN";
        if (double.IsPositiveInfinity(value))
            return $"{keyword}.PositiveInfinity";
        if (double.IsNegativeInfinity(value))
            return $"{keyword}.NegativeInfinity";

        return value.ToString("R", CultureInfo.InvariantCulture) + suffix;
    }

    private static string FormatReal(float value, string keyword, string suffix)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
            return FormatReal((double)value, keyword, suffix);

        return value.ToString("R", CultureInfo.InvariantCulture) + suffix;
    }

    private static bool IsCopyConstructor(IMethodSymbol constructor, INamedTypeSymbol containingType)
    {
        if (constructor.Parameters.Length != 1)
            return false;

        return SymbolEqualityComparer.Default.Equals(
            constructor.Parameters[0].Type,
            containingType);
    }

    private static void GenerateTools(
        SourceProductionContext context,
        ImmutableArray<ToolMetadata> tools,
        string? assemblyName)
    {
        if (tools.IsDefaultOrEmpty)
            return;

        var toolClassNames = new List<string>();
        var toolNamespaces = new List<string>();

        foreach (var tool in tools)
        {
            if (tool.IsRequest && tool.ResponseTypeName is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    MissingResponseType, tool.Location, tool.AttributeName, tool.FullTypeName));
                continue;
            }

            var toolClassName = $"{tool.TypeName}McpTool";
            toolClassNames.Add(toolClassName);
            toolNamespaces.Add(tool.Namespace);

            var source = McpToolEmitter.GenerateMcpTool(tool);
            context.AddSource($"{toolClassName}.g.cs", SourceText.From(source, Encoding.UTF8));
        }

        if (toolClassNames.Count == 0)
            return;

        var registrationNamespace = $"{NamingConventions.ToNamespace(assemblyName)}.Generated";
        var registrationSource = McpToolEmitter.GenerateRegistration(toolClassNames, toolNamespaces, registrationNamespace);
        context.AddSource("McpToolRegistration.g.cs", SourceText.From(registrationSource, Encoding.UTF8));
    }
}

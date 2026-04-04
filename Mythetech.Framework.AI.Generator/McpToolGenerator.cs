using System.Collections.Immutable;
using System.Text;
using Mythetech.Framework.AI.Generator.Emitters;
using Mythetech.Framework.AI.Generator.Models;
using Mythetech.Framework.AI.Generator.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Mythetech.Framework.AI.Generator;

/// <summary>
/// Incremental source generator that creates MCP tools from [ToolCommand] and [ToolQuery] decorated types.
/// </summary>
[Generator]
public class McpToolGenerator : IIncrementalGenerator
{
    private const string ToolCommandAttributeName = "Mythetech.Framework.Infrastructure.Mcp.ToolCommandAttribute";
    private const string ToolQueryAttributeName = "Mythetech.Framework.Infrastructure.Mcp.ToolQueryAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var commandDeclarations = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                ToolCommandAttributeName,
                predicate: static (node, _) => node is RecordDeclarationSyntax or ClassDeclarationSyntax,
                transform: static (ctx, ct) => ExtractToolMetadata(ctx, ct, isQuery: false))
            .Where(static m => m is not null)
            .Select(static (m, _) => m!);

        var queryDeclarations = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                ToolQueryAttributeName,
                predicate: static (node, _) => node is RecordDeclarationSyntax or ClassDeclarationSyntax,
                transform: static (ctx, ct) => ExtractToolMetadata(ctx, ct, isQuery: true))
            .Where(static m => m is not null)
            .Select(static (m, _) => m!);

        var allDeclarations = commandDeclarations.Collect()
            .Combine(queryDeclarations.Collect())
            .Select(static (pair, _) => pair.Left.AddRange(pair.Right));

        context.RegisterSourceOutput(allDeclarations, GenerateTools);
    }

    private static ToolMetadata? ExtractToolMetadata(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken,
        bool isQuery)
    {
        if (context.TargetSymbol is not INamedTypeSymbol typeSymbol)
            return null;

        var attribute = context.Attributes.FirstOrDefault();
        if (attribute is null)
            return null;

        string? name = null;
        string? description = null;
        string? responseTypeName = null;

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
                    if (namedArg.Value.Value is INamedTypeSymbol responseType)
                    {
                        responseTypeName = responseType.ToDisplayString();
                    }
                    break;
            }
        }

        var xmlDoc = typeSymbol.GetDocumentationCommentXml(cancellationToken: cancellationToken);
        var parsedDoc = XmlDocParser.Parse(xmlDoc);

        var parameters = ExtractParameters(typeSymbol, parsedDoc);

        return new ToolMetadata(
            typeName: typeSymbol.Name,
            fullTypeName: typeSymbol.ToDisplayString(),
            ns: typeSymbol.ContainingNamespace.ToDisplayString(),
            toolName: name ?? NamingConventions.ToSnakeCase(typeSymbol.Name),
            description: description ?? parsedDoc.Summary ?? $"Executes the {typeSymbol.Name} operation",
            parameters: parameters,
            isQuery: isQuery,
            responseTypeName: responseTypeName
        );
    }

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
                hasDefaultValue: param.HasExplicitDefaultValue,
                defaultValue: param.HasExplicitDefaultValue ? param.ExplicitDefaultValue : null
            ));
        }

        return parameters;
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
        ImmutableArray<ToolMetadata> tools)
    {
        if (tools.IsDefaultOrEmpty)
            return;

        var toolClassNames = new List<string>();
        var toolNamespaces = new List<string>();

        foreach (var tool in tools)
        {
            var toolClassName = $"{tool.TypeName}McpTool";
            toolClassNames.Add(toolClassName);
            toolNamespaces.Add(tool.Namespace);

            var source = McpToolEmitter.GenerateMcpTool(tool);
            context.AddSource($"{toolClassName}.g.cs", SourceText.From(source, Encoding.UTF8));
        }

        var registrationSource = McpToolEmitter.GenerateRegistration(toolClassNames, toolNamespaces);
        context.AddSource("McpToolRegistration.g.cs", SourceText.From(registrationSource, Encoding.UTF8));
    }
}

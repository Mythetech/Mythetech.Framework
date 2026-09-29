using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Mythetech.Framework.AI.Generator;
using Mythetech.Framework.Infrastructure.Mcp;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Mcp.Generator;

public class McpToolGeneratorTests
{
    [Fact(DisplayName = "A [ToolRequest] without a ResponseType reports MTAI001 naming the type")]
    public void MissingResponseTypeReportsDiagnostic()
    {
        var result = RunGenerator("""
            using Mythetech.Framework.Infrastructure.Mcp;
            namespace Cerberus.Core;
            [ToolRequest]
            public record CreateWorkspace(string Name);
            """);

        var diagnostic = result.Diagnostics.ShouldHaveSingleItem();
        diagnostic.Id.ShouldBe("MTAI001");
        diagnostic.Severity.ShouldBe(DiagnosticSeverity.Error);
        diagnostic.GetMessage().ShouldContain("Cerberus.Core.CreateWorkspace");
        result.GeneratedTrees.ShouldBeEmpty();
    }

    [Fact(DisplayName = "The registration class is placed in a namespace named after the assembly")]
    public void RegistrationNamespaceFollowsAssembly()
    {
        var result = RunGenerator("""
            using Mythetech.Framework.Infrastructure.Mcp;
            namespace Cerberus.Core;
            [ToolRequest(ResponseType = typeof(string))]
            public record GetWorkspaceName(System.Guid WorkspaceId);
            """, assemblyName: "Cerberus.Core");

        result.Diagnostics.ShouldBeEmpty();
        var registration = result.GeneratedTrees.Single(t => t.FilePath.EndsWith("McpToolRegistration.g.cs"));
        registration.ToString().ShouldContain("namespace Cerberus.Core.Generated;");
    }

    [Fact(DisplayName = "Two assemblies using the generator can both be referenced without a conflict")]
    public void TwoAssembliesDoNotConflict()
    {
        var core = CompileWithGenerator("""
            using Mythetech.Framework.Infrastructure.Mcp;
            namespace Cerberus.Core;
            [ToolRequest(ResponseType = typeof(string))]
            public record GetWorkspaceName(System.Guid WorkspaceId);
            """, "Cerberus.Core");

        var app = CompileWithGenerator("""
            using Mythetech.Framework.Infrastructure.Mcp;
            namespace Cerberus;
            [ToolCommand]
            public record OpenPlan(string PlanId);
            public static class Startup
            {
                public static void Configure(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
                {
                    Cerberus.Core.Generated.McpToolRegistration.AddGeneratedMcpTools(services);
                    Cerberus.Generated.McpToolRegistration.AddGeneratedMcpTools(services);
                }
            }
            """, "Cerberus", core.ToMetadataReference());

        app.GetDiagnostics().Where(d => d.Severity >= DiagnosticSeverity.Warning).ShouldBeEmpty();
    }

    [Theory(DisplayName = "Assembly names become valid namespaces")]
    [InlineData("Cerberus.Core", "Cerberus.Core")]
    [InlineData("my-app.2d", "my_app._2d")]
    [InlineData(null, "Mythetech.Framework.AI.Generator")]
    public void AssemblyNamesBecomeNamespaces(string? assemblyName, string expected)
    {
        Mythetech.Framework.AI.Generator.Utilities.NamingConventions.ToNamespace(assemblyName).ShouldBe(expected);
    }

    private static GeneratorDriverRunResult RunGenerator(string source, string assemblyName = "TestAssembly")
    {
        var compilation = CreateCompilation(source, assemblyName);
        var driver = CSharpGeneratorDriver.Create(new McpToolGenerator()).RunGenerators(compilation, TestContext.Current.CancellationToken);
        return driver.GetRunResult();
    }

    private static Compilation CompileWithGenerator(string source, string assemblyName, params MetadataReference[] references)
    {
        var compilation = CreateCompilation(source, assemblyName, references);
        CSharpGeneratorDriver.Create(new McpToolGenerator())
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics, TestContext.Current.CancellationToken);

        diagnostics.ShouldBeEmpty();
        output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        return output;
    }

    private static CSharpCompilation CreateCompilation(string source, string assemblyName, params MetadataReference[] references)
    {
        var platformReferences = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));

        return CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken)],
            platformReferences
                .Append(MetadataReference.CreateFromFile(typeof(ToolRequestAttribute).Assembly.Location))
                .Append(MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly.Location))
                .Append(MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions).Assembly.Location))
                .Concat(references),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
    }
}

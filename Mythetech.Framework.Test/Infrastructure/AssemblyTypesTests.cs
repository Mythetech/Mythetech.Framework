using Mythetech.Framework.Infrastructure;
using Mythetech.Framework.Infrastructure.MessageBus;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure;

public class AssemblyTypesTests
{
    [Fact(DisplayName = "An assembly's types are read once and shared by every scan")]
    public void Get_SameAssembly_ReturnsTheSameTypes()
    {
        var assembly = typeof(AssemblyTypesTests).Assembly;

        AssemblyTypes.Get(assembly).ShouldBeSameAs(AssemblyTypes.Get(assembly));
    }

    [Fact(DisplayName = "An assembly's types include its own types")]
    public void Get_ContainsTheAssemblysTypes()
    {
        AssemblyTypes.Get(typeof(AssemblyTypesTests).Assembly).ShouldContain(typeof(AssemblyTypesTests));
    }

    [Fact(DisplayName = "An assembly's consumers and query handlers are found once for every bus registration")]
    public void BusScan_SameAssembly_ReturnsTheSameResult()
    {
        var assembly = typeof(AssemblyTypesTests).Assembly;

        BusAssemblyScan.Get(assembly).ShouldBeSameAs(BusAssemblyScan.Get(assembly));
    }
}

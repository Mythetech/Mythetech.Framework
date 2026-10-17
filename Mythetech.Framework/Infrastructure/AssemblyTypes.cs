using System.Reflection;
using System.Runtime.CompilerServices;

namespace Mythetech.Framework.Infrastructure;

// Startup registration scans the same assemblies many times, and GetTypes builds a new array on every call.
internal static class AssemblyTypes
{
    private static readonly ConditionalWeakTable<Assembly, Type[]> Cache = new();

    public static Type[] Get(Assembly assembly) => Cache.GetValue(assembly, static a => a.GetTypes());
}

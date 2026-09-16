using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace Mythetech.Framework.Benchmarks;

/// <summary>
/// Default config for every benchmark in this assembly.
/// </summary>
/// <remarks>
/// This solution targets .NET 11, and BenchmarkDotNet 0.15.8 has no RuntimeMoniker for it, so its
/// default out-of-process toolchain fails SDK validation before a single iteration runs. Running
/// in-process trades away isolation between the benchmark process and this one, but that cost is
/// acceptable here: this project only ever compares before against after on one machine, it never
/// publishes numbers against a baseline captured elsewhere. Setting the toolchain here, once, means
/// no class needs its own job attribute and no command line flag can be forgotten. Never add
/// <c>[SimpleJob]</c> or another job attribute to a benchmark class; it conflicts with this job.
/// <c>AsDefault()</c> also registers this job as the one BenchmarkDotNet's console argument parser
/// treats as the base job: a <c>--job</c> flag on the command line is resolved against it instead of
/// replacing it, so passing one cannot reintroduce the out-of-process toolchain by accident.
/// </remarks>
public static class BenchmarkConfig
{
    public static IConfig Create() =>
        ManualConfig.Create(DefaultConfig.Instance)
            .AddJob(Job.Default.WithToolchain(InProcessEmitToolchain.Instance).AsDefault());
}

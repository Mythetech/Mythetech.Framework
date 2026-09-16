using BenchmarkDotNet.Running;
using Mythetech.Framework.Benchmarks;

// Measure a hot-path change before and after with the same filter, always in Release:
//   dotnet run -c Release --project Mythetech.Framework.Benchmarks -- --filter *MessageBus*
// --list flat prints every benchmark name.
// Every benchmark runs in-process (see BenchmarkConfig for why), so no --job or --inProcess flag
// is required.
// Results are written under BenchmarkDotNet.Artifacts/results/ in the working directory.
// CI does not build this project.
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, BenchmarkConfig.Create());

using NArchitecture.Core.Benchmarks;

var commandLineArgs = Environment.GetCommandLineArgs();
var silent = commandLineArgs.Contains("--last-run") || commandLineArgs.Contains("--last-benchmark");
await BenchmarkRunner.RunBenchmarksInteractivelyAsync(typeof(Program).Assembly, silent);

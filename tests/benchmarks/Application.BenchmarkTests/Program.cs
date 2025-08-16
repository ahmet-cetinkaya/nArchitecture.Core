using NArchitecture.Core.Benchmarks;

string[] commandLineArgs = Environment.GetCommandLineArgs();
bool silent = commandLineArgs.Contains("--last-run") || commandLineArgs.Contains("--last-benchmark");
await BenchmarkRunner.RunBenchmarksInteractivelyAsync(typeof(Program).Assembly, silent);

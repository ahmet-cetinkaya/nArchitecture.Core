using System.Reflection;
using Spectre.Console;

namespace NArchitecture.Core.Benchmarks;

public static class BenchmarkRunner
{
    private const string LAST_BENCHMARK_FLAG = "--last-benchmark";
    private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
    private static readonly string LastBenchmarkFilePath = Path.Combine(LogDirectory, "last-benchmark.txt");

    public static void RunBenchmarksInteractively(Assembly assembly) =>
        RunBenchmarksInteractivelyAsync(assembly).GetAwaiter().GetResult();

    public static async Task RunBenchmarksInteractivelyAsync(Assembly assembly, bool silent = false)
    {
        Directory.CreateDirectory(LogDirectory);

        try
        {
            var benchmarkTypes = assembly
                .GetTypes()
                .Where(t => t.Name.EndsWith("Benchmark"))
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .ToList();

            if (benchmarkTypes is [])
            {
                AnsiConsole.MarkupLine("[red]:cross_mark: No benchmark classes found in the assembly![/]");
                return;
            }

            Type selectedType;
            if (silent && File.Exists(LastBenchmarkFilePath))
            {
                var lastBenchmarkName = await File.ReadAllTextAsync(LastBenchmarkFilePath);
                if (string.IsNullOrEmpty(lastBenchmarkName))
                {
                    AnsiConsole.MarkupLine("[red]:cross_mark: Invalid last benchmark data![/]");
                    return;
                }

                selectedType =
                    benchmarkTypes.FirstOrDefault(t => t.FullName == lastBenchmarkName)
                    ?? throw new InvalidOperationException("Benchmark type not found.");
                if (selectedType == null)
                {
                    AnsiConsole.MarkupLine($"[red]:cross_mark: Last run benchmark '{lastBenchmarkName}' not found![/]");
                    return;
                }

                AnsiConsole.MarkupLine(
                    $"[blue]:magnifying_glass_tilted_left: Running last benchmark: [green]{GetRelativePath(selectedType)}[/][/]"
                );
                AnsiConsole.WriteLine();
            }
            else
            {
                AnsiConsole.MarkupLine("[blue]:magnifying_glass_tilted_left: Available benchmarks:[/]");
                selectedType = AnsiConsole.Prompt(
                    new SelectionPrompt<Type>()
                        .Title("Select a [green]:racing_car: benchmark[/] to run:")
                        .PageSize(10)
                        .UseConverter(t => $"[blue]:file_folder: {GetRelativePath(t)}[/]")
                        .AddChoices(benchmarkTypes)
                );

                AnsiConsole.MarkupLine(
                    $"[blue]:magnifying_glass_tilted_left: Selected benchmark: [green]{GetRelativePath(selectedType)}[/][/]"
                );
                AnsiConsole.WriteLine();

                await File.WriteAllTextAsync(LastBenchmarkFilePath, selectedType.FullName ?? selectedType.Name);
            }

            AnsiConsole.MarkupLine("[yellow]:rocket: Starting benchmark...[/]");
            AnsiConsole.WriteLine();

            await RunBenchmarkTypeAsync(selectedType);

            // Save last run benchmark
            await File.WriteAllTextAsync(LastBenchmarkFilePath, selectedType.FullName);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]:cross_mark: Error running benchmark: {ex.Message}[/]");
            AnsiConsole.WriteException(ex);
        }
    }

    private static async Task RunBenchmarkTypeAsync(Type benchmarkType)
    {
        var config = BenchmarkDotNet
            .Configs.ManualConfig.Create(BenchmarkDotNet.Configs.DefaultConfig.Instance)
            .WithOptions(BenchmarkDotNet.Configs.ConfigOptions.DisableOptimizationsValidator);

        await Task.Run(() => BenchmarkDotNet.Running.BenchmarkRunner.Run(benchmarkType, config));
    }

    private static string GetRelativePath(Type type)
    {
        var codeBase = type.Assembly.Location;
        var assemblyPath = Path.GetDirectoryName(codeBase);
        var projectRoot = Directory.GetParent(assemblyPath!)?.Parent?.Parent?.Parent?.Parent?.FullName;

        if (projectRoot == null)
            return type.Name;

        var namespaceSegments = type.Namespace?.Split('.');
        var benchmarkIndex = Array.IndexOf(namespaceSegments ?? [], "BenchmarkTests");

        if (benchmarkIndex == -1)
            return type.Name;

        var relevantPath = string.Join(
            Path.DirectorySeparatorChar,
            namespaceSegments?.Skip(benchmarkIndex + 1) ?? Array.Empty<string>()
        );

        return Path.Combine(relevantPath, type.Name + ".cs");
    }
}

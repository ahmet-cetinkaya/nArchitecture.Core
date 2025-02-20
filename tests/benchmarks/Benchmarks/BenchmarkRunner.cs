using System.Reflection;
using Spectre.Console;

namespace NArchitecture.Core.Benchmarks;

public static class BenchmarkRunner
{
    private const string LAST_BENCHMARK_FLAG = "--last-benchmark";
    private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
    private static readonly string LastBenchmarkFilePath = Path.Combine(LogDirectory, "last-benchmark.txt");

    public static void RunBenchmarksInteractively(Assembly assembly)
    {
        RunBenchmarksInteractivelyAsync(assembly).GetAwaiter().GetResult();
    }

    public static async Task RunBenchmarksInteractivelyAsync(Assembly assembly, bool silent = false)
    {
        _ = Directory.CreateDirectory(LogDirectory);

        try
        {
            var benchmarkTypes = assembly
                .GetTypes()
                .Where(t => t.Name.EndsWith("Benchmarks"))
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .OrderBy(GetRelativePath) // Sort by relative path
                .ToList();

            if (benchmarkTypes is [])
            {
                AnsiConsole.MarkupLine("[red]:cross_mark: No benchmark classes found in the assembly![/]");
                return;
            }

            Type selectedType;
            if (silent && File.Exists(LastBenchmarkFilePath))
            {
                string lastBenchmarkName = await File.ReadAllTextAsync(LastBenchmarkFilePath);
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
        BenchmarkDotNet.Configs.ManualConfig config = BenchmarkDotNet
            .Configs.ManualConfig.Create(BenchmarkDotNet.Configs.DefaultConfig.Instance)
            .WithOptions(BenchmarkDotNet.Configs.ConfigOptions.DisableOptimizationsValidator);

        _ = await Task.Run(() => BenchmarkDotNet.Running.BenchmarkRunner.Run(benchmarkType, config));
    }

    private static string GetRelativePath(Type type)
    {
        string codeBase = type.Assembly.Location;
        string? assemblyPath = Path.GetDirectoryName(codeBase);
        if (assemblyPath == null)
            return type.Name + ".cs";

        // Find the nearest .csproj file by walking up the directory tree
        var currentDir = new DirectoryInfo(assemblyPath);
        while (currentDir != null && currentDir.GetFiles("*.csproj").Length == 0)
        {
            currentDir = currentDir.Parent;
        }

        string? projectDir = currentDir?.FullName;
        if (projectDir == null)
            return type.Name + ".cs";

        // Search for the file in and below the project directory
        string fileName = type.Name + ".cs";
        string[] files = Directory.GetFiles(projectDir, fileName, SearchOption.AllDirectories);

        if (files.Length == 0)
            return fileName;

        string fullPath = files.First();
        return Path.GetRelativePath(projectDir, fullPath);
    }
}

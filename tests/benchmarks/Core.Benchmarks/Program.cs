﻿using System.Diagnostics;
using Spectre.Console;

namespace NArchitecture.Core.Benchmarks;

class BenchmarkProject(string Name, string Path)
{
    public string Name { get; } = Name;
    public string Path { get; } = Path;
    public string LastRunLog { get; set; } = string.Empty;
}

class Program
{
    private const string LAST_RUN_FLAG = "--last-run";
    private const string LAST_BENCHMARK_FLAG = "--last-benchmark";
    private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
    private static readonly string LastRunFilePath = Path.Combine(LogDirectory, "last-run.txt");
    private static readonly string LastBenchmarkFilePath = Path.Combine(LogDirectory, "last-benchmark.txt");

    static async Task Main(string[] args)
    {
        Directory.CreateDirectory(LogDirectory);

        if (args is [LAST_RUN_FLAG, ..])
        {
            await RunLastBenchmarkAsync();
            return;
        }

        var benchmarkProjects = FindBenchmarkProjects();
        if (benchmarkProjects is [])
        {
            AnsiConsole.MarkupLine("[red]:cross_mark: No benchmark projects found![/]");
            return;
        }

        AnsiConsole.MarkupLine("[blue]:magnifying_glass_tilted_left: Available benchmark projects:[/]");
        var selectedProject = AnsiConsole.Prompt(
            new SelectionPrompt<BenchmarkProject>()
                .Title("Select a [green]:racing_car: benchmark project[/] to run:")
                .PageSize(10)
                .UseConverter(p => $"[blue]:file_folder: {Path.GetRelativePath(Directory.GetCurrentDirectory(), p.Path)}[/]")
                .AddChoices(benchmarkProjects)
        );

        await RunBenchmarkAsync(selectedProject);
    }

    private static List<BenchmarkProject> FindBenchmarkProjects()
    {
        string rootDir = FindSolutionDirectory() ?? AppDomain.CurrentDomain.BaseDirectory;
        if (!Directory.Exists(rootDir))
            return [];

        string[] excludedProjects = ["Core.Benchmarks", "BenchmarkDotNet.Autogenerated"];

        return Directory
            .GetFiles(rootDir, "*.csproj", SearchOption.AllDirectories)
            .Where(f =>
                f.Contains("Benchmark", StringComparison.OrdinalIgnoreCase)
                && !excludedProjects.Any(excluded =>
                    Path.GetFileNameWithoutExtension(f).Equals(excluded, StringComparison.OrdinalIgnoreCase)
                )
            )
            .Select(f => new BenchmarkProject(Path.GetFileNameWithoutExtension(f), f))
            .ToList();
    }

    private static string? FindSolutionDirectory()
    {
        var currentDir = AppDomain.CurrentDomain.BaseDirectory;
        while (currentDir != null)
        {
            if (Directory.GetFiles(currentDir, "*.sln").Any())
                return currentDir;

            currentDir = Path.GetDirectoryName(currentDir);
        }
        return null;
    }

    static async Task RunLastBenchmarkAsync()
    {
        if (!File.Exists(LastRunFilePath))
        {
            AnsiConsole.MarkupLine("[red]:cross_mark: No previous benchmark run found![/]");
            return;
        }

        var lastProjectPath = await File.ReadAllTextAsync(LastRunFilePath);
        var project = new BenchmarkProject(Path.GetFileNameWithoutExtension(lastProjectPath), lastProjectPath);
        await RunBenchmarkAsync(project);
    }

    static async Task RunBenchmarkAsync(BenchmarkProject project)
    {
        await BuildProjectAsync(project);

        AnsiConsole.MarkupLine($"[blue]:magnifying_glass_tilted_left: Selected project: [green]{project.Name}[/][/]");
        AnsiConsole.WriteLine();

        var projectDir = Path.GetDirectoryName(project.Path)!;
        using var process = new Process
        {
            StartInfo = new()
            {
                FileName = "dotnet",
                Arguments =
                    $"run -c Release -- {(Environment.GetCommandLineArgs().Contains(LAST_RUN_FLAG) ? LAST_BENCHMARK_FLAG : "")}",
                UseShellExecute = false,
                WorkingDirectory = projectDir,
            },
        };

        process.Start();
        await process.WaitForExitAsync();

        await File.WriteAllTextAsync(LastRunFilePath, project.Path);
    }

    static Task BuildProjectAsync(BenchmarkProject project) =>
        Task.Run(
            () =>
                AnsiConsole
                    .Status()
                    .Start(
                        $"[blue]:hammer_and_wrench: Building {project.Name}...[/]",
                        ctx =>
                        {
                            ctx.Spinner(Spinner.Known.Star);
                            using var process = new Process
                            {
                                StartInfo = new()
                                {
                                    FileName = "dotnet",
                                    Arguments = $"build {project.Path} -c Release",
                                    RedirectStandardOutput = true,
                                    UseShellExecute = false,
                                },
                            };
                            process.Start();
                            process.WaitForExit();
                        }
                    )
        );
}

// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;

[assembly: CLSCompliant(false)]

namespace EliteSharpLib.Benchmarks;

internal static class Program
{
    public static void Main(string[] args) => BenchmarkSwitcher
        .FromAssembly(typeof(Program).Assembly)
        .Run(
            args.Length == 0 ? ["--filter", "*"] : args,
            ManualConfig
                .Create(DefaultConfig.Instance)

                // Relative to the project directory, matching .gitignore's src/*/perf/*/reports/ and the workflow's output-file-path.
                .WithArtifactsPath("reports")

                // In-process, not a throwaway generated project: that generation throws if it finds
                // more than one project of the same name across the repo tree, which a nested git worktree checkout causes.
                .AddJob(Job.Default.WithToolchain(InProcessNoEmitToolchain.Instance)));
}

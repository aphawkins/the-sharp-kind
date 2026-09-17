// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;

[assembly: CLSCompliant(false)]

namespace SharpKind.Graphics.Benchmarks;

internal static class Program
{
    // No arguments runs every benchmark; pass CLI arguments to speed up local iteration, e.g. --filter *DrawPixel* --job short.
    // In-process, not out-of-process: avoids BenchmarkDotNet's project-file lookup, which throws if a nested worktree checkout gives it more than one match.
    public static void Main(string[] args) => BenchmarkSwitcher
        .FromAssembly(typeof(Program).Assembly)
        .Run(
            args.Length == 0 ? ["--filter", "*"] : args,
            ManualConfig
                .Create(DefaultConfig.Instance)

                // Kept in step with .gitignore's src/*/perf/*/reports/ and the workflow's output-file-path.
                .WithArtifactsPath("reports")
                .AddJob(Job.Default.WithToolchain(InProcessNoEmitToolchain.Instance)));
}

// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;

[assembly: CLSCompliant(false)]

namespace SharpKind.Input.Benchmarks;

internal static class Program
{
    public static void Main(string[] args) => BenchmarkSwitcher
        .FromAssembly(typeof(Program).Assembly)
        .Run(
            args.Length == 0 ? ["--filter", "*"] : args,
            ManualConfig
                .Create(DefaultConfig.Instance)

                // Kept in step with .gitignore's src/*/perf/*/reports/ and the workflow's output-file-path.
                .WithArtifactsPath("reports")

                // In-process, not out-of-process: avoids BenchmarkDotNet's project-file lookup, which throws if a nested worktree checkout gives it more than one match.
                .AddJob(Job.Default.WithToolchain(InProcessNoEmitToolchain.Instance)));
}

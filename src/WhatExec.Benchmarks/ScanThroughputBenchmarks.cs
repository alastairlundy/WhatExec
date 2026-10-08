/*
    WhatExec.Benchmarks
    Copyright (c) 2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.IO.Abstractions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using WhatExec.Lib.Detectors;
using WhatExec.Lib.Locators;

namespace WhatExec.Benchmarks;

/// <summary>
/// Throughput evidence for the scan pipeline (ledger T006): the directory walk
/// through <see cref="IExecutablesLocator"/> (top-level and all-subdirectories)
/// and the unified single-pass drive fan-out across ready drives.
/// Run by hand locally - never from a workflow (ledger T008).
/// </summary>
[MemoryDiagnoser]
public class ScanThroughputBenchmarks
{
    // Fixed path + fixed layout so repeated runs measure the same tree.
    private static readonly string FixtureRoot =
        Path.Combine(Path.GetTempPath(), "WhatExec.Benchmarks", "scan-tree");
    private const int FilesPerDirectory = 8;
    private const int SubdirectoriesPerDirectory = 3;
    private const int MaxDepth = 2;

    private IExecutablesLocator _locator = null!;
    private DirectoryInfo _root = null!;

    public static void Main(string[] args)
        // No arguments means "run everything" - the switcher otherwise prompts
        // interactively, which would break the plain `dotnet run` invocation.
        => BenchmarkSwitcher.FromAssembly(typeof(ScanThroughputBenchmarks).Assembly)
            .Run(args.Length == 0 ? ["--filter", "*"] : args);

    /// <summary>
    /// Builds a deterministic real directory tree, then wires the locator over the
    /// real filesystem seam (not the test fake) with the production detector.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        BuildFixtureTree(FixtureRoot, depth: 0);

        IFileSystem fileSystem = new FileSystem();
        _locator = new ExecutablesLocator(new ExecutableFileDetector(fileSystem), fileSystem);
        _root = new DirectoryInfo(FixtureRoot);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        try
        {
            Directory.Delete(FixtureRoot, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort cleanup; a leftover tree is rebuilt identically next run.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    // ── Scenario 1: directory walk ───────────────────────────────────────

    [Benchmark]
    public async Task DirectoryWalk_TopDirectoryOnly()
        => _ = await CountAsync(
            _locator.EnumerateExecutablesInDirectoryAsync(_root, SearchOption.TopDirectoryOnly, CancellationToken.None));

    [Benchmark]
    public async Task DirectoryWalk_AllDirectories()
        => _ = await CountAsync(
            _locator.EnumerateExecutablesInDirectoryAsync(_root, SearchOption.AllDirectories, CancellationToken.None));

    // ── Scenario 2: drive fan-out single pass ────────────────────────────

    /// <summary>
    /// The across-drives entry point ticket 003 unified: one lazy pass over every
    /// ready drive, no eager intermediate collections. Top-level only so a hand run
    /// stays short even though whole-drive recursion would take minutes.
    /// </summary>
    [Benchmark]
    public async Task DriveFanOut_SinglePass()
        => _ = await CountAsync(
            _locator.EnumerateExecutablesAcrossDrivesAsync(SearchOption.TopDirectoryOnly, CancellationToken.None));

    /// <summary>
    /// Drains the stream the way callers consume it, returning the count so the walk
    /// cannot be optimised away. Callers not under a host token supply
    /// <see cref="CancellationToken.None"/>; that token is threaded through here.
    /// </summary>
    private static async Task<int> CountAsync(IAsyncEnumerable<FileInfo> executables)
    {
        int count = 0;
        await foreach (FileInfo _ in executables.ConfigureAwait(false))
        {
            count++;
        }

        return count;
    }

    // ── Fixture tree ─────────────────────────────────────────────────────

    private static void BuildFixtureTree(string root, int depth)
    {
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);

        Directory.CreateDirectory(root);
        WriteLevel(root, depth);
    }

    private static void WriteLevel(string path, int depth)
    {
        for (int i = 0; i < FilesPerDirectory; i++)
        {
            if (i % 2 == 0)
            {
                // MZ header so Windows detection recognises these as executables.
                string exePath = Path.Combine(path, $"file{i}.exe");
                File.WriteAllBytes(exePath, [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00]);
            }
            else
            {
                File.WriteAllText(Path.Combine(path, $"file{i}.txt"), "not executable");
            }
        }

        if (depth >= MaxDepth)
            return;

        for (int i = 0; i < SubdirectoriesPerDirectory; i++)
        {
            string subdirectory = Path.Combine(path, $"dir{depth}-{i}");
            Directory.CreateDirectory(subdirectory);
            WriteLevel(subdirectory, depth + 1);
        }
    }
}

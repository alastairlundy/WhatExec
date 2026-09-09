using WhatExec.Lib;
using WhatExec.Lib.Resolvers;

namespace WhatExecLib.Tests.Resolvers;

// Serialized: every test mutates process-wide PATH/PATHEXT, so parallel
// execution races (one test restoring PATH while another enumerates).
[NotInParallel]
public class PathEnvironmentVariableResolverUnitTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _originalPath;
    private readonly string? _originalPathExt;

    public PathEnvironmentVariableResolverUnitTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"whatexec_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRoot);
        _originalPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        _originalPathExt = Environment.GetEnvironmentVariable("PATHEXT");
    }

    public void Dispose()
    {
        // Restore env vars
        Environment.SetEnvironmentVariable("PATH", _originalPath);
        if (_originalPathExt is not null)
            Environment.SetEnvironmentVariable("PATHEXT", _originalPathExt);
        else
            Environment.SetEnvironmentVariable("PATHEXT", null);

        // Clean up temp directory
        try
        {
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, recursive: true);
        }
        catch
        {
            // Best effort cleanup
        }
    }

    /// <summary>
    /// Creates a fake executable file that always passes the detector.
    /// Uses a real file on disk so File.Exists succeeds.
    /// </summary>
    private static string CreateFakeExecutable(string directory, string fileName)
    {
        Directory.CreateDirectory(directory);
        string filePath = Path.Combine(directory, fileName);
        File.WriteAllText(filePath, "fake");
        return filePath;
    }

    /// <summary>
    /// A stub detector that always considers files executable.
    /// </summary>
    private class AlwaysExecutableDetector : IExecutableFileDetector
    {
        public bool IsFileExecutable(FileInfo file) => true;

        public Task<bool> IsFileExecutableAsync(FileInfo file, CancellationToken cancellationToken)
            => Task.FromResult(true);
    }

    private IPathEnvironmentVariableResolver CreateResolver()
        => new PathEnvironmentVariableResolver(new AlwaysExecutableDetector());

    #region Streaming Enumeration Tests

    [Test]
    public async Task Enumerate_FoundInFirstPathDir_YieldsResult()
    {
        string dirA = Path.Combine(_tempRoot, "dirA");
        CreateFakeExecutable(dirA, OperatingSystem.IsWindows() ? "tool.exe" : "tool");

        Environment.SetEnvironmentVariable("PATH", dirA);
        if (OperatingSystem.IsWindows())
            Environment.SetEnvironmentVariable("PATHEXT", ".EXE");

        IPathEnvironmentVariableResolver resolver = CreateResolver();
        string name = OperatingSystem.IsWindows() ? "tool.exe" : "tool";

        List<KeyValuePair<string, FileInfo>> results = new();
        await foreach (KeyValuePair<string, FileInfo> kvp in
            resolver.EnumerateExecutableFilePathsAsync([name], CancellationToken.None))
        {
            results.Add(kvp);
        }

        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].Key).IsEqualTo(name);
        await Assert.That(results[0].Value.Name).IsEqualTo(name);
    }

    [Test]
    public async Task Enumerate_NotFound_YieldsNothing()
    {
        string dirA = Path.Combine(_tempRoot, "dirA");
        Directory.CreateDirectory(dirA);

        Environment.SetEnvironmentVariable("PATH", dirA);
        if (OperatingSystem.IsWindows())
            Environment.SetEnvironmentVariable("PATHEXT", ".EXE");

        IPathEnvironmentVariableResolver resolver = CreateResolver();

        List<KeyValuePair<string, FileInfo>> results = new();
        await foreach (KeyValuePair<string, FileInfo> kvp in
            resolver.EnumerateExecutableFilePathsAsync(["nonexistent"], CancellationToken.None))
        {
            results.Add(kvp);
        }

        await Assert.That(results).IsEmpty();
    }

    [Test]
    public async Task Enumerate_MultipleNames_OnlyFoundOnes()
    {
        string dirA = Path.Combine(_tempRoot, "dirA");
        CreateFakeExecutable(dirA, OperatingSystem.IsWindows() ? "found.exe" : "found");

        Environment.SetEnvironmentVariable("PATH", dirA);
        if (OperatingSystem.IsWindows())
            Environment.SetEnvironmentVariable("PATHEXT", ".EXE");

        IPathEnvironmentVariableResolver resolver = CreateResolver();
        string foundName = OperatingSystem.IsWindows() ? "found.exe" : "found";

        List<KeyValuePair<string, FileInfo>> results = new();
        await foreach (KeyValuePair<string, FileInfo> kvp in
            resolver.EnumerateExecutableFilePathsAsync(
                [foundName, "missing"], CancellationToken.None))
        {
            results.Add(kvp);
        }

        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].Key).IsEqualTo(foundName);
    }

    [Test]
    public async Task Enumerate_SingleNameOverload_DelegatesToBatch()
    {
        string dirA = Path.Combine(_tempRoot, "dirA");
        CreateFakeExecutable(dirA, OperatingSystem.IsWindows() ? "myapp.exe" : "myapp");

        Environment.SetEnvironmentVariable("PATH", dirA);
        if (OperatingSystem.IsWindows())
            Environment.SetEnvironmentVariable("PATHEXT", ".EXE");

        IPathEnvironmentVariableResolver resolver = CreateResolver();
        string name = OperatingSystem.IsWindows() ? "myapp.exe" : "myapp";

        List<KeyValuePair<string, FileInfo>> results = new();
        await foreach (KeyValuePair<string, FileInfo> kvp in
            resolver.EnumerateExecutableFilePathsAsync(name, CancellationToken.None))
        {
            results.Add(kvp);
        }

        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].Key).IsEqualTo(name);
    }

    #endregion

    #region First-Match PATH Semantics

    [Test]
    public async Task Enumerate_FirstMatchSemantics_OnlyFirstDirWins()
    {
        string dirA = Path.Combine(_tempRoot, "dirA");
        string dirB = Path.Combine(_tempRoot, "dirB");

        string name = OperatingSystem.IsWindows() ? "multi.exe" : "multi";
        CreateFakeExecutable(dirA, name);
        CreateFakeExecutable(dirB, name);

        // dirA is listed first
        string separator = OperatingSystem.IsWindows() ? ";" : ":";
        Environment.SetEnvironmentVariable("PATH", $"{dirA}{separator}{dirB}");
        if (OperatingSystem.IsWindows())
            Environment.SetEnvironmentVariable("PATHEXT", ".EXE");

        IPathEnvironmentVariableResolver resolver = CreateResolver();

        List<KeyValuePair<string, FileInfo>> results = new();
        await foreach (KeyValuePair<string, FileInfo> kvp in
            resolver.EnumerateExecutableFilePathsAsync([name], CancellationToken.None))
        {
            results.Add(kvp);
        }

        // First-match: only the first PATH directory match is returned
        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].Value.DirectoryName).IsEqualTo(dirA);
    }

    [Test]
    public async Task TryGet_FirstMatchSemantics_ReturnsFirstDirMatch()
    {
        string dirA = Path.Combine(_tempRoot, "dirA");
        string dirB = Path.Combine(_tempRoot, "dirB");

        string name = OperatingSystem.IsWindows() ? "multi.exe" : "multi";
        CreateFakeExecutable(dirA, name);
        CreateFakeExecutable(dirB, name);

        string separator = OperatingSystem.IsWindows() ? ";" : ":";
        Environment.SetEnvironmentVariable("PATH", $"{dirA}{separator}{dirB}");
        if (OperatingSystem.IsWindows())
            Environment.SetEnvironmentVariable("PATHEXT", ".EXE");

        IPathEnvironmentVariableResolver resolver = CreateResolver();

        IReadOnlyDictionary<string, FileInfo> result =
            await resolver.TryGetExecutableFilePathsAsync([name], CancellationToken.None);

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[name].DirectoryName).IsEqualTo(dirA);
    }

    #endregion

    #region Batch TryGet Tests

    [Test]
    public async Task TryGet_AllFound_ReturnsAll()
    {
        string dirA = Path.Combine(_tempRoot, "dirA");
        string exe1Name = OperatingSystem.IsWindows() ? "app1.exe" : "app1";
        string exe2Name = OperatingSystem.IsWindows() ? "app2.exe" : "app2";
        CreateFakeExecutable(dirA, exe1Name);
        CreateFakeExecutable(dirA, exe2Name);

        Environment.SetEnvironmentVariable("PATH", dirA);
        if (OperatingSystem.IsWindows())
            Environment.SetEnvironmentVariable("PATHEXT", ".EXE");

        IPathEnvironmentVariableResolver resolver = CreateResolver();

        IReadOnlyDictionary<string, FileInfo> result =
            await resolver.TryGetExecutableFilePathsAsync(
                [exe1Name, exe2Name], CancellationToken.None);

        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result.ContainsKey(exe1Name)).IsTrue();
        await Assert.That(result.ContainsKey(exe2Name)).IsTrue();
    }

    [Test]
    public async Task TryGet_NoneFound_ReturnsEmptyDict()
    {
        string dirA = Path.Combine(_tempRoot, "dirA");
        Directory.CreateDirectory(dirA);

        Environment.SetEnvironmentVariable("PATH", dirA);
        if (OperatingSystem.IsWindows())
            Environment.SetEnvironmentVariable("PATHEXT", ".EXE");

        IPathEnvironmentVariableResolver resolver = CreateResolver();

        IReadOnlyDictionary<string, FileInfo> result =
            await resolver.TryGetExecutableFilePathsAsync(
                ["ghost1", "ghost2"], CancellationToken.None);

        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task TryGet_PartialFound_ReturnsOnlyFound()
    {
        string dirA = Path.Combine(_tempRoot, "dirA");
        string foundName = OperatingSystem.IsWindows() ? "exists.exe" : "exists";
        CreateFakeExecutable(dirA, foundName);

        Environment.SetEnvironmentVariable("PATH", dirA);
        if (OperatingSystem.IsWindows())
            Environment.SetEnvironmentVariable("PATHEXT", ".EXE");

        IPathEnvironmentVariableResolver resolver = CreateResolver();

        IReadOnlyDictionary<string, FileInfo> result =
            await resolver.TryGetExecutableFilePathsAsync(
                [foundName, "missing"], CancellationToken.None);

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result.ContainsKey(foundName)).IsTrue();
    }

    [Test]
    public async Task TryGet_SingleNameOverload_DelegatesToBatch()
    {
        string dirA = Path.Combine(_tempRoot, "dirA");
        string name = OperatingSystem.IsWindows() ? "single.exe" : "single";
        CreateFakeExecutable(dirA, name);

        Environment.SetEnvironmentVariable("PATH", dirA);
        if (OperatingSystem.IsWindows())
            Environment.SetEnvironmentVariable("PATHEXT", ".EXE");

        IPathEnvironmentVariableResolver resolver = CreateResolver();

        IReadOnlyDictionary<string, FileInfo> result =
            await resolver.TryGetExecutableFilePathsAsync(name, CancellationToken.None);

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result.ContainsKey(name)).IsTrue();
    }

    #endregion

    #region Empty PATH Tests

    [Test]
    public async Task TryGet_EmptyPath_ReturnsEmptyDict()
    {
        Environment.SetEnvironmentVariable("PATH", "");
        if (OperatingSystem.IsWindows())
            Environment.SetEnvironmentVariable("PATHEXT", ".EXE");

        IPathEnvironmentVariableResolver resolver = CreateResolver();

        IReadOnlyDictionary<string, FileInfo> result =
            await resolver.TryGetExecutableFilePathsAsync(
                ["anything"], CancellationToken.None);

        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task Enumerate_EmptyPath_YieldsNothing()
    {
        Environment.SetEnvironmentVariable("PATH", "");
        if (OperatingSystem.IsWindows())
            Environment.SetEnvironmentVariable("PATHEXT", ".EXE");

        IPathEnvironmentVariableResolver resolver = CreateResolver();

        List<KeyValuePair<string, FileInfo>> results = new();
        await foreach (KeyValuePair<string, FileInfo> kvp in
            resolver.EnumerateExecutableFilePathsAsync(["anything"], CancellationToken.None))
        {
            results.Add(kvp);
        }

        await Assert.That(results).IsEmpty();
    }

    #endregion

    #region Consistent Casing Tests

    [Test]
    public async Task TryGet_CaseInsensitiveMatchOnWindows_FindsFile()
    {
        if (!OperatingSystem.IsWindows())
        {
            // TUnit: skip this test on non-Windows
            await Assert.That(true).IsTrue();
            return;
        }

        string dirA = Path.Combine(_tempRoot, "dirA");
        CreateFakeExecutable(dirA, "MyTool.exe");

        Environment.SetEnvironmentVariable("PATH", dirA);
        Environment.SetEnvironmentVariable("PATHEXT", ".EXE");

        IPathEnvironmentVariableResolver resolver = CreateResolver();

        IReadOnlyDictionary<string, FileInfo> result =
            await resolver.TryGetExecutableFilePathsAsync(
                ["mytool.exe"], CancellationToken.None);

        await Assert.That(result.Count).IsEqualTo(1);
    }

    #endregion

    #region Materialized Snapshot Tests

    [Test]
    public async Task TryGet_EnumerableParameter_MaterializesSnapshot()
    {
        string dirA = Path.Combine(_tempRoot, "dirA");
        string name1 = OperatingSystem.IsWindows() ? "a.exe" : "a";
        string name2 = OperatingSystem.IsWindows() ? "b.exe" : "b";
        CreateFakeExecutable(dirA, name1);
        CreateFakeExecutable(dirA, name2);

        Environment.SetEnvironmentVariable("PATH", dirA);
        if (OperatingSystem.IsWindows())
            Environment.SetEnvironmentVariable("PATHEXT", ".EXE");

        IPathEnvironmentVariableResolver resolver = CreateResolver();

        // Pass a lazy enumerable - should still work via snapshot
        IEnumerable<string> lazyNames = GetLazyNames(name1, name2);

        IReadOnlyDictionary<string, FileInfo> result =
            await resolver.TryGetExecutableFilePathsAsync(lazyNames, CancellationToken.None);

        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result.ContainsKey(name1)).IsTrue();
        await Assert.That(result.ContainsKey(name2)).IsTrue();
    }

    private static IEnumerable<string> GetLazyNames(params string[] names)
    {
        foreach (string name in names)
        {
            yield return name;
        }
    }

    #endregion
}

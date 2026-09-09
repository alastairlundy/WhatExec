using System.IO.Abstractions.TestingHelpers;
using WhatExec.Lib.Locators;

namespace WhatExecLib.Tests.Locators;

/// <summary>
/// Unit tests for <see cref="ExecutableFileInstancesLocator"/> and <see cref="IExecutableInstancesLocator"/>.
/// Uses System.IO.Abstractions test helper fake (D009).
/// One shared mock detector covering every executability test (D014).
/// </summary>
public class ExecutableInstancesLocatorTests
{
    private const string TestRoot = @"C:\test";

    // ── Directory tests ───────────────────────────────────────────────────

    [Test]
    public async Task EnumerateInstancesInDirectory_ByName_FindsMatch(
        CancellationToken cancellationToken)
    {
        MockFileSystem fs = LocatorTestHelpers.CreateFileSystem(
            TestRoot, "dotnet.exe", "other.exe", "readme.txt");

        var detector = new LocatorTestHelpers.MockExecutableDetector(".exe");
        var locator = new ExecutableFileInstancesLocator(detector, fs);

        DirectoryInfo dir = new DirectoryInfo(TestRoot);
        List<FileInfo> results = await locator
            .EnumerateExecutableInstancesInDirectoryAsync(dir, "dotnet.exe", SearchOption.TopDirectoryOnly, cancellationToken)
            .ToListAsync(cancellationToken);

        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].Name).IsEqualTo("dotnet.exe");
    }

    [Test]
    public async Task EnumerateInstancesInDirectory_ByNameNotFound_ReturnsEmpty(
        CancellationToken cancellationToken)
    {
        MockFileSystem fs = LocatorTestHelpers.CreateFileSystem(
            TestRoot, "app.exe");

        var detector = new LocatorTestHelpers.MockExecutableDetector(".exe");
        var locator = new ExecutableFileInstancesLocator(detector, fs);

        DirectoryInfo dir = new DirectoryInfo(TestRoot);
        List<FileInfo> results = await locator
            .EnumerateExecutableInstancesInDirectoryAsync(dir, "nonexistent.exe", SearchOption.TopDirectoryOnly, cancellationToken)
            .ToListAsync(cancellationToken);

        await Assert.That(results).IsEmpty();
    }

    [Test]
    public async Task EnumerateInstancesInDirectory_NameFilterIsCaseInsensitive(
        CancellationToken cancellationToken)
    {
        MockFileSystem fs = LocatorTestHelpers.CreateFileSystem(
            TestRoot, "DotNet.exe");

        var detector = new LocatorTestHelpers.MockExecutableDetector(".exe");
        var locator = new ExecutableFileInstancesLocator(detector, fs);

        DirectoryInfo dir = new DirectoryInfo(TestRoot);
        List<FileInfo> results = await locator
            .EnumerateExecutableInstancesInDirectoryAsync(dir, "dotnet.exe", SearchOption.TopDirectoryOnly, cancellationToken)
            .ToListAsync(cancellationToken);

        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].Name).IsEqualTo("DotNet.exe");
    }

    [Test]
    public async Task EnumerateInstancesInDirectory_DetectorFiltersNonExecutable(
        CancellationToken cancellationToken)
    {
        MockFileSystem fs = LocatorTestHelpers.CreateFileSystem(
            TestRoot, "target.exe");

        var detector = new LocatorTestHelpers.NeverExecutableDetector();
        var locator = new ExecutableFileInstancesLocator(detector, fs);

        DirectoryInfo dir = new DirectoryInfo(TestRoot);
        List<FileInfo> results = await locator
            .EnumerateExecutableInstancesInDirectoryAsync(dir, "target.exe", SearchOption.TopDirectoryOnly, cancellationToken)
            .ToListAsync(cancellationToken);

        await Assert.That(results).IsEmpty();
    }

    [Test]
    public async Task EnumerateInstancesInDirectory_AllDirectories_RecurseSubdirs(
        CancellationToken cancellationToken)
    {
        MockFileSystem fs = LocatorTestHelpers.CreateFileSystem(
            TestRoot, "root.exe", @"sub\nested.exe", @"sub\readme.txt");

        var detector = new LocatorTestHelpers.MockExecutableDetector(".exe");
        var locator = new ExecutableFileInstancesLocator(detector, fs);

        DirectoryInfo dir = new DirectoryInfo(TestRoot);
        List<FileInfo> results = await locator
            .EnumerateExecutableInstancesInDirectoryAsync(dir, "nested.exe", SearchOption.AllDirectories, cancellationToken)
            .ToListAsync(cancellationToken);

        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].Name).IsEqualTo("nested.exe");
    }

    [Test]
    public async Task EnumerateInstancesInDirectory_TopDirectoryOnly_DoesNotRecurse(
        CancellationToken cancellationToken)
    {
        MockFileSystem fs = LocatorTestHelpers.CreateFileSystem(
            TestRoot, "target.exe", @"sub\target.exe");

        var detector = new LocatorTestHelpers.MockExecutableDetector(".exe");
        var locator = new ExecutableFileInstancesLocator(detector, fs);

        DirectoryInfo dir = new DirectoryInfo(TestRoot);
        List<FileInfo> results = await locator
            .EnumerateExecutableInstancesInDirectoryAsync(dir, "target.exe", SearchOption.TopDirectoryOnly, cancellationToken)
            .ToListAsync(cancellationToken);

        // Only the root-level file should be found.
        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].DirectoryName).IsEqualTo(TestRoot);
    }

    // ── Unauthorized entry skipping (D010) ───────────────────────────────

    [Test]
    public async Task EnumerateInstancesInDirectory_UnauthorizedEntry_IsSkipped(
        CancellationToken cancellationToken)
    {
        MockFileSystem fs = LocatorTestHelpers.CreateFileSystem(
            TestRoot, "target.exe", "unauthorized_target.exe");

        var detector = LocatorTestHelpers.UnauthorizedThrowingDetector.Instance;
        var locator = new ExecutableFileInstancesLocator(detector, fs);

        DirectoryInfo dir = new DirectoryInfo(TestRoot);
        List<FileInfo> results = await locator
            .EnumerateExecutableInstancesInDirectoryAsync(dir, "target.exe", SearchOption.TopDirectoryOnly, cancellationToken)
            .ToListAsync(cancellationToken);

        // "unauthorized_target.exe" doesn't match name filter "target.exe" so it's skipped by name filter too.
        // "target.exe" matches and passes detector.
        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].Name).IsEqualTo("target.exe");
    }

    [Test]
    public async Task EnumerateInstancesInDirectory_UnauthorizedMatch_IsSkipped(
        CancellationToken cancellationToken)
    {
        MockFileSystem fs = LocatorTestHelpers.CreateFileSystem(
            TestRoot, "unauthorized_target.exe", "good_target.exe");

        var detector = LocatorTestHelpers.UnauthorizedThrowingDetector.Instance;
        var locator = new ExecutableFileInstancesLocator(detector, fs);

        DirectoryInfo dir = new DirectoryInfo(TestRoot);
        List<FileInfo> results = await locator
            .EnumerateExecutableInstancesInDirectoryAsync(dir, "unauthorized_target.exe", SearchOption.TopDirectoryOnly, cancellationToken)
            .ToListAsync(cancellationToken);

        // The unauthorized file matches the name but the detector throws → skipped (D010).
        await Assert.That(results).IsEmpty();
    }

    // ── Across-drives sugar (D017) ───────────────────────────────────────

    [Test]
    public async Task EnumerateInstancesAcrossDrives_DoesNotThrow(
        CancellationToken cancellationToken)
    {
        var detector = LocatorTestHelpers.NeverExecutableDetector.Instance;
        var fs = new MockFileSystem();
        var locator = new ExecutableFileInstancesLocator(detector, fs);

        List<FileInfo> results = await locator
            .EnumerateExecutableInstancesAcrossDrivesAsync("any.exe", SearchOption.TopDirectoryOnly, cancellationToken)
            .ToListAsync(cancellationToken);

        await Assert.That(results).IsNotNull();
    }

    // ── Consistent casing (D010) ─────────────────────────────────────────

    [Test]
    public async Task EnumerateInstancesInDirectory_CaseInsensitiveNameFilter(
        CancellationToken cancellationToken)
    {
        MockFileSystem fs = LocatorTestHelpers.CreateFileSystem(
            TestRoot, "MyApp.EXE");

        var detector = new LocatorTestHelpers.MockExecutableDetector(".exe");
        var locator = new ExecutableFileInstancesLocator(detector, fs);

        DirectoryInfo dir = new DirectoryInfo(TestRoot);
        List<FileInfo> results = await locator
            .EnumerateExecutableInstancesInDirectoryAsync(dir, "myapp.exe", SearchOption.TopDirectoryOnly, cancellationToken)
            .ToListAsync(cancellationToken);

        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].Name).IsEqualTo("MyApp.EXE");
    }

    // ── Empty / null name guard ───────────────────────────────────────────

    [Test]
    public async Task EnumerateInstancesInDirectory_EmptyName_ThrowsArgumentException(
        CancellationToken cancellationToken)
    {
        MockFileSystem fs = LocatorTestHelpers.CreateFileSystem(TestRoot, "app.exe");
        var detector = new LocatorTestHelpers.AlwaysExecutableDetector();
        var locator = new ExecutableFileInstancesLocator(detector, fs);
        DirectoryInfo dir = new DirectoryInfo(TestRoot);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await foreach (FileInfo _ in locator
                .EnumerateExecutableInstancesInDirectoryAsync(dir, "", SearchOption.TopDirectoryOnly, cancellationToken))
            {
            }
        });
    }
}

using System.IO.Abstractions.TestingHelpers;
using WhatExec.Lib.Locators;

namespace WhatExecLib.Tests.Locators;

/// <summary>
/// Unit tests for <see cref="ExecutablesLocator"/> and <see cref="IExecutablesLocator"/>.
/// Uses System.IO.Abstractions test helper fake (D009).
/// One shared mock detector covering every executability test (D014).
/// </summary>
public class ExecutablesLocatorTests
{
    private const string TestRoot = @"C:\test";

    // ── Directory tests ───────────────────────────────────────────────────

    [Test]
    public async Task EnumerateInDirectory_ExecutableFiles_ReturnsOnlyExecutable(
        CancellationToken cancellationToken)
    {
        MockFileSystem fs = LocatorTestHelpers.CreateFileSystem(
            TestRoot, "app.exe", "readme.txt", "lib.dll", "image.png");

        var detector = new LocatorTestHelpers.MockExecutableDetector(".exe", ".dll");
        var locator = new ExecutablesLocator(detector, fs);

        DirectoryInfo dir = new DirectoryInfo(TestRoot);
        List<FileInfo> results = await locator
            .EnumerateExecutablesInDirectoryAsync(dir, SearchOption.TopDirectoryOnly, cancellationToken)
            .ToListAsync(cancellationToken);

        await Assert.That(results.Count).IsEqualTo(2);
        await Assert.That(results.Select(f => f.Name).Order())
            .IsEquivalentTo(new[] { "app.exe", "lib.dll" });
    }

    [Test]
    public async Task EnumerateInDirectory_NoExecutables_ReturnsEmpty(
        CancellationToken cancellationToken)
    {
        MockFileSystem fs = LocatorTestHelpers.CreateFileSystem(
            TestRoot, "readme.txt", "image.png");

        var detector = new LocatorTestHelpers.MockExecutableDetector(".exe");
        var locator = new ExecutablesLocator(detector, fs);

        DirectoryInfo dir = new DirectoryInfo(TestRoot);
        List<FileInfo> results = await locator
            .EnumerateExecutablesInDirectoryAsync(dir, SearchOption.TopDirectoryOnly, cancellationToken)
            .ToListAsync(cancellationToken);

        await Assert.That(results).IsEmpty();
    }

    [Test]
    public async Task EnumerateInDirectory_AllDirectories_RecurseSubdirs(
        CancellationToken cancellationToken)
    {
        MockFileSystem fs = LocatorTestHelpers.CreateFileSystem(
            TestRoot, "root.exe", @"sub\nested.exe", @"sub\readme.txt");

        var detector = new LocatorTestHelpers.MockExecutableDetector(".exe");
        var locator = new ExecutablesLocator(detector, fs);

        DirectoryInfo dir = new DirectoryInfo(TestRoot);
        List<FileInfo> results = await locator
            .EnumerateExecutablesInDirectoryAsync(dir, SearchOption.AllDirectories, cancellationToken)
            .ToListAsync(cancellationToken);

        await Assert.That(results.Count).IsEqualTo(2);
        await Assert.That(results.Select(f => f.Name).Order())
            .IsEquivalentTo(new[] { "nested.exe", "root.exe" });
    }

    [Test]
    public async Task EnumerateInDirectory_TopDirectoryOnly_DoesNotRecurse(
        CancellationToken cancellationToken)
    {
        MockFileSystem fs = LocatorTestHelpers.CreateFileSystem(
            TestRoot, "root.exe", @"sub\nested.exe");

        var detector = new LocatorTestHelpers.MockExecutableDetector(".exe");
        var locator = new ExecutablesLocator(detector, fs);

        DirectoryInfo dir = new DirectoryInfo(TestRoot);
        List<FileInfo> results = await locator
            .EnumerateExecutablesInDirectoryAsync(dir, SearchOption.TopDirectoryOnly, cancellationToken)
            .ToListAsync(cancellationToken);

        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].Name).IsEqualTo("root.exe");
    }

    [Test]
    public async Task EnumerateInDirectory_DetectorFiltersNonExecutable(
        CancellationToken cancellationToken)
    {
        MockFileSystem fs = LocatorTestHelpers.CreateFileSystem(
            TestRoot, "a.exe", "b.exe", "c.txt");

        var detector = new LocatorTestHelpers.NeverExecutableDetector();
        var locator = new ExecutablesLocator(detector, fs);

        DirectoryInfo dir = new DirectoryInfo(TestRoot);
        List<FileInfo> results = await locator
            .EnumerateExecutablesInDirectoryAsync(dir, SearchOption.TopDirectoryOnly, cancellationToken)
            .ToListAsync(cancellationToken);

        await Assert.That(results).IsEmpty();
    }

    [Test]
    public async Task EnumerateInDirectory_AllDetected_ReturnsAll(
        CancellationToken cancellationToken)
    {
        MockFileSystem fs = LocatorTestHelpers.CreateFileSystem(
            TestRoot, "a.exe", "b.txt", "c.dll");

        var detector = LocatorTestHelpers.AlwaysExecutableDetector.Instance;
        var locator = new ExecutablesLocator(detector, fs);

        DirectoryInfo dir = new DirectoryInfo(TestRoot);
        List<FileInfo> results = await locator
            .EnumerateExecutablesInDirectoryAsync(dir, SearchOption.TopDirectoryOnly, cancellationToken)
            .ToListAsync(cancellationToken);

        await Assert.That(results.Count).IsEqualTo(3);
    }

    // ── Unauthorized entry skipping (D010) ───────────────────────────────

    [Test]
    public async Task EnumerateInDirectory_UnauthorizedEntry_IsSkipped(
        CancellationToken cancellationToken)
    {
        MockFileSystem fs = LocatorTestHelpers.CreateFileSystem(
            TestRoot, "good.exe", "unauthorized_file.exe");

        var detector = LocatorTestHelpers.UnauthorizedThrowingDetector.Instance;
        var locator = new ExecutablesLocator(detector, fs);

        DirectoryInfo dir = new DirectoryInfo(TestRoot);
        List<FileInfo> results = await locator
            .EnumerateExecutablesInDirectoryAsync(dir, SearchOption.TopDirectoryOnly, cancellationToken)
            .ToListAsync(cancellationToken);

        // Only the non-unauthorized file should be yielded.
        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].Name).IsEqualTo("good.exe");
    }

    // ── Across-drives sugar (D017) ───────────────────────────────────────

    [Test]
    public async Task EnumerateAcrossDrives_DoesNotThrow(CancellationToken cancellationToken)
    {
        // Across-drives enumerates real drives; just verify it doesn't throw.
        var detector = LocatorTestHelpers.NeverExecutableDetector.Instance;
        var fs = new MockFileSystem();
        var locator = new ExecutablesLocator(detector, fs);

        // Should complete without error (even if no drives match).
        List<FileInfo> results = await locator
            .EnumerateExecutablesAcrossDrivesAsync(SearchOption.TopDirectoryOnly, cancellationToken)
            .ToListAsync(cancellationToken);

        // Result count depends on real drives; just verify it runs.
        await Assert.That(results).IsNotNull();
    }

    // ── Consistent casing (D010) ─────────────────────────────────────────

    [Test]
    public async Task EnumerateInDirectory_CaseInsensitiveFindsFile(
        CancellationToken cancellationToken)
    {
        // On Windows, EnumerateFiles is case-insensitive by default.
        MockFileSystem fs = LocatorTestHelpers.CreateFileSystem(
            TestRoot, "MyApp.EXE");

        var detector = new LocatorTestHelpers.MockExecutableDetector(".exe");
        var locator = new ExecutablesLocator(detector, fs);

        DirectoryInfo dir = new DirectoryInfo(TestRoot);
        List<FileInfo> results = await locator
            .EnumerateExecutablesInDirectoryAsync(dir, SearchOption.TopDirectoryOnly, cancellationToken)
            .ToListAsync(cancellationToken);

        await Assert.That(results.Count).IsEqualTo(1);
    }

    // ── CancellationToken support ─────────────────────────────────────────

    [Test]
    public async Task EnumerateInDirectory_CancelledToken_ThrowsOperationCanceled(
        CancellationToken cancellationToken)
    {
        MockFileSystem fs = LocatorTestHelpers.CreateFileSystem(
            TestRoot, "app.exe");

        var detector = new LocatorTestHelpers.AlwaysExecutableDetector();
        var locator = new ExecutablesLocator(detector, fs);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        DirectoryInfo dir = new DirectoryInfo(TestRoot);

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (FileInfo _ in locator
                .EnumerateExecutablesInDirectoryAsync(dir, SearchOption.TopDirectoryOnly, cts.Token))
            {
                // Should not reach here.
            }
        });
    }
}

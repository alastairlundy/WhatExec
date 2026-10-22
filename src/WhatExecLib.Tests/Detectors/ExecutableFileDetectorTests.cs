using System.IO.Abstractions.TestingHelpers;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using TUnit.Core.Enums;
using WhatExec.Lib.Detectors;

namespace WhatExecLib.Tests.Detectors;

/// <summary>
/// Unit tests for <see cref="ExecutableFileDetector"/> using a System.IO.Abstractions
/// MockFileSystem.
/// </summary>
/// <remarks>
/// <para>
/// These tests trust the mock's own access-rule semantics: the detector reads the security
/// descriptor through the mock stream's <c>GetAccessControl()</c>, so the verdict depends on how
/// the mock surfaces the seeded <see cref="FileSecurity"/> rules. A defect in the fake's ACL
/// plumbing would not surface as a test failure here; the tests pin the detector's composition
/// (extension set, magic number, access-check wiring) against the mock's behaviour, not against
/// a real-Windows access check.
/// </para>
/// <para>
/// No real access control list is ever written: every rule below lives only in the mock file
/// system, never on disk.
/// </para>
/// </remarks>
public class ExecutableFileDetectorTests
{
    private const string TestRoot = @"C:\test";

    /// <summary>
    /// The detector's sixteen hardcoded executable extensions, duplicated here only so the
    /// PATHEXT test can find an extension the machine's PATHEXT names that is NOT hardcoded.
    /// </summary>
    private static readonly string[] HardcodedExtensions =
    [
        ".exe", ".msi", ".appx", ".com", ".sys", ".drv", ".mui", ".ocx",
        ".ax", ".msstyles", ".scr", ".cpl", ".acm", ".efi", ".dll", ".tsp"
    ];

    /// <summary>
    /// The MZ magic number followed by padding, long enough to fill the detector's four-byte
    /// header read.
    /// </summary>
    private static readonly byte[] MzHeader = [0x4D, 0x5A, 0x90, 0x00];

    // ── Access-check composition (extension + effective execute access) ──

    [Test]
    [RunOn(OS.Windows)]
    [SupportedOSPlatform("windows")]
    public async Task IsFileExecutableAsync_MsiWithAllowExecuteForCurrentUser_ReturnsTrue(
        CancellationToken cancellationToken)
    {
        string filePath = Path.Combine(TestRoot, "installer.msi");
        MockFileSystem fs = new();
        fs.AddFile(filePath, CreateFile([], AllowExecute(CurrentUserSid())));

        var detector = new ExecutableFileDetector(fs);
        bool result = await detector.IsFileExecutableAsync(new FileInfo(filePath), cancellationToken);

        await Assert.That(result).IsTrue();
    }

    [Test]
    [RunOn(OS.Windows)]
    [SupportedOSPlatform("windows")]
    public async Task IsFileExecutableAsync_DenyExecuteForCurrentUser_WinsOverAllow(
        CancellationToken cancellationToken)
    {
        // Deny and allow both target the current user; deny entries are applied before allow
        // entries, so the deny must win regardless of the order they were added in.
        SecurityIdentifier userSid = CurrentUserSid();
        FileSecurity security = new();
        security.AddAccessRule(new FileSystemAccessRule(
            userSid, FileSystemRights.ExecuteFile, AccessControlType.Deny));
        security.AddAccessRule(new FileSystemAccessRule(
            userSid, FileSystemRights.ExecuteFile, AccessControlType.Allow));

        string filePath = Path.Combine(TestRoot, "denied.msi");
        MockFileSystem fs = new();
        fs.AddFile(filePath, CreateFile([], security));

        var detector = new ExecutableFileDetector(fs);
        bool result = await detector.IsFileExecutableAsync(new FileInfo(filePath), cancellationToken);

        await Assert.That(result).IsFalse();
    }

    [Test]
    [RunOn(OS.Windows)]
    [SupportedOSPlatform("windows")]
    public async Task IsFileExecutableAsync_AllowExecuteForForeignSid_ReturnsFalse(
        CancellationToken cancellationToken)
    {
        // LOCAL SERVICE: a well-known SID the current user does not belong to, so the allow
        // rule cannot be trusted by the current identity.
        SecurityIdentifier foreignSid = new("S-1-5-19");
        await Assert.That(IsMemberOfCurrentIdentity(foreignSid)).IsFalse();

        string filePath = Path.Combine(TestRoot, "foreign.msi");
        MockFileSystem fs = new();
        fs.AddFile(filePath, CreateFile([], AllowExecute(foreignSid)));

        var detector = new ExecutableFileDetector(fs);
        bool result = await detector.IsFileExecutableAsync(new FileInfo(filePath), cancellationToken);

        await Assert.That(result).IsFalse();
    }

    // ── Windows verdict composition: magic number rules ──────────────────

    [Test]
    [RunOn(OS.Windows)]
    public async Task IsFileExecutableAsync_ExeWithMzMagicNumber_ReturnsTrue(
        CancellationToken cancellationToken)
    {
        // The hardcoded .exe rule plus the MZ magic number decides; no access rules are seeded.
        string filePath = Path.Combine(TestRoot, "tool.exe");
        MockFileSystem fs = new();
        fs.AddFile(filePath, new MockFileData(MzHeader));

        var detector = new ExecutableFileDetector(fs);
        bool result = await detector.IsFileExecutableAsync(new FileInfo(filePath), cancellationToken);

        await Assert.That(result).IsTrue();
    }

    [Test]
    [RunOn(OS.Windows)]
    [SupportedOSPlatform("windows")]
    public async Task IsFileExecutableAsync_PathExtDerivedExtensionWithAllowExecute_ReturnsTrue(
        CancellationToken cancellationToken)
    {
        // Use an extension this machine's own PATHEXT names that is not one of the sixteen
        // hardcoded entries (typically .BAT or .CMD), so the PATHEXT-derived branch runs.
        string? pathExt = Environment.GetEnvironmentVariable("PATHEXT");
        string? pathExtOnlyExtension = pathExt?
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(entry =>
                !HardcodedExtensions.Contains(entry, StringComparer.OrdinalIgnoreCase));

        if (pathExtOnlyExtension is null)
        {
            // The machine's PATHEXT names no extension beyond the hardcoded set, so there is
            // no PATHEXT-derived extension to exercise on this platform.
            return;
        }

        string filePath = Path.Combine(TestRoot, $"script{pathExtOnlyExtension}");
        MockFileSystem fs = new();
        fs.AddFile(filePath, CreateFile([], AllowExecute(CurrentUserSid())));

        var detector = new ExecutableFileDetector(fs);
        bool result = await detector.IsFileExecutableAsync(new FileInfo(filePath), cancellationToken);

        await Assert.That(result).IsTrue();
    }

    // ── Extension allow-list filtering ───────────────────────────────────

    [Test]
    [RunOn(OS.Windows)]
    public async Task IsFileExecutableAsync_ExtensionNotInAllowList_ReturnsFalse(
        CancellationToken cancellationToken)
    {
        // .txt is neither hardcoded nor named by PATHEXT, so the verdict is false without any
        // access rules being consulted at all.
        string filePath = Path.Combine(TestRoot, "notes.txt");
        MockFileSystem fs = new();
        fs.AddFile(filePath, new MockFileData([]));

        var detector = new ExecutableFileDetector(fs);
        bool result = await detector.IsFileExecutableAsync(new FileInfo(filePath), cancellationToken);

        await Assert.That(result).IsFalse();
    }

    // ── Open-failure behaviour ───────────────────────────────────────────

    [Test]
    public async Task IsFileExecutableAsync_MissingFile_ThrowsFileNotFoundException(
        CancellationToken cancellationToken)
    {
        MockFileSystem fs = new();
        fs.AddDirectory(TestRoot);

        var detector = new ExecutableFileDetector(fs);
        FileInfo missing = new(Path.Combine(TestRoot, "ghost.msi"));

        await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            await detector.IsFileExecutableAsync(missing, cancellationToken));
    }

    [Test]
    public async Task IsFileExecutableAsync_ShareDisallowsOpen_ReturnsFalseWithoutThrowing(
        CancellationToken cancellationToken)
    {
        // The detector opens with FileShare.ReadWrite | FileShare.Delete; AllowedFileShare of
        // None makes the mock throw IOException on that open, so the candidate is skipped.
        string filePath = Path.Combine(TestRoot, "locked.msi");
        MockFileSystem fs = new();
        MockFileData lockedFile = new([]);
        lockedFile.AllowedFileShare = FileShare.None;
        fs.AddFile(filePath, lockedFile);

        var detector = new ExecutableFileDetector(fs);
        bool result = await detector.IsFileExecutableAsync(new FileInfo(filePath), cancellationToken);

        await Assert.That(result).IsFalse();
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Builds mock file contents carrying the given rules. No real access control list is
    /// written: the rules live only in the mock file system.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private static MockFileData CreateFile(byte[] contents, FileSecurity security)
        => new(contents) { AccessControl = security };

    /// <summary>
    /// A security descriptor with a single allow entry granting
    /// <see cref="FileSystemRights.ExecuteFile"/> to the given trustee.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private static FileSecurity AllowExecute(SecurityIdentifier trustee)
    {
        FileSecurity security = new();
        security.AddAccessRule(new FileSystemAccessRule(
            trustee, FileSystemRights.ExecuteFile, AccessControlType.Allow));
        return security;
    }

    /// <summary>
    /// The current user's SID, resolved independently of the detector under test.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private static SecurityIdentifier CurrentUserSid()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        return identity.User
               ?? throw new InvalidOperationException("The current Windows identity has no user SID.");
    }

    /// <summary>
    /// Determines whether the given SID is the current identity's user SID or one of its group
    /// SIDs, asserted before the foreign-SID test so that test means what it claims.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private static bool IsMemberOfCurrentIdentity(SecurityIdentifier sid)
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();

        if (identity.User?.Equals(sid) == true)
            return true;

        return identity.Groups?.OfType<SecurityIdentifier>().Any(group => group.Equals(sid)) == true;
    }
}

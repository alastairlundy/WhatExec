using System.IO.Abstractions.TestingHelpers;
using WhatExec.Lib.Locators;

namespace WhatExecLib.Tests.Locators;

/// <summary>
/// Shared test support for locator tests.
/// One mock detector covering every executability test (D014).
/// Uses System.IO.Abstractions test helper fake (D009).
/// </summary>
internal static class LocatorTestHelpers
{
    /// <summary>
    /// Creates a <see cref="MockFileSystem"/> with the given files under the specified root path.
    /// </summary>
    public static MockFileSystem CreateFileSystem(string rootPath, params string[] fileRelativePaths)
    {
        var mockFs = new MockFileSystem();

        // Ensure the root directory exists in the mock.
        mockFs.AddDirectory(rootPath);

        foreach (string relativePath in fileRelativePaths)
        {
            string fullPath = Path.Combine(rootPath, relativePath);
            mockFs.AddFile(fullPath, new MockFileData(string.Empty));
        }

        return mockFs;
    }

    /// <summary>
    /// A detector that returns <c>true</c> for files whose extension is in the
    /// specified set (case-insensitive), and <c>false</c> otherwise.
    /// Covers every executability test with one mock (D014).
    /// </summary>
    internal sealed class MockExecutableDetector : IExecutableFileDetector
    {
        private readonly HashSet<string> _executableExtensions;

        public MockExecutableDetector(params string[] executableExtensions)
        {
            _executableExtensions = new HashSet<string>(
                executableExtensions.Select(e => e.ToLowerInvariant()),
                StringComparer.OrdinalIgnoreCase);
        }

        public bool IsFileExecutable(FileInfo file)
            => _executableExtensions.Contains(file.Extension.ToLowerInvariant());

        public Task<bool> IsFileExecutableAsync(FileInfo file, CancellationToken cancellationToken)
            => Task.FromResult(IsFileExecutable(file));
    }

    /// <summary>
    /// A detector that always returns <c>true</c> (every file is executable).
    /// </summary>
    internal sealed class AlwaysExecutableDetector : IExecutableFileDetector
    {
        public static readonly AlwaysExecutableDetector Instance = new();

        public bool IsFileExecutable(FileInfo file) => true;

        public Task<bool> IsFileExecutableAsync(FileInfo file, CancellationToken cancellationToken)
            => Task.FromResult(true);
    }

    /// <summary>
    /// A detector that always returns <c>false</c> (no file is executable).
    /// </summary>
    internal sealed class NeverExecutableDetector : IExecutableFileDetector
    {
        public static readonly NeverExecutableDetector Instance = new();

        public bool IsFileExecutable(FileInfo file) => false;

        public Task<bool> IsFileExecutableAsync(FileInfo file, CancellationToken cancellationToken)
            => Task.FromResult(false);
    }

    /// <summary>
    /// A detector that throws <see cref="UnauthorizedAccessException"/> for files
    /// whose name contains "unauthorized" (case-insensitive).
    /// Used to verify Skip-unauthorized-entry behavior (D010).
    /// </summary>
    internal sealed class UnauthorizedThrowingDetector : IExecutableFileDetector
    {
        public static readonly UnauthorizedThrowingDetector Instance = new();

        public bool IsFileExecutable(FileInfo file)
        {
            if (file.Name.Contains("unauthorized", StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException();
            return true;
        }

        public Task<bool> IsFileExecutableAsync(FileInfo file, CancellationToken cancellationToken)
        {
            if (file.Name.Contains("unauthorized", StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException();
            return Task.FromResult(true);
        }
    }
}

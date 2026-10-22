/*
    WhatExec.Lib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

// ReSharper disable InconsistentNaming
// ReSharper disable UseUtf8StringLiteral

using System.IO.Abstractions;
using WhatExec.Lib.Permissions;

namespace WhatExec.Lib.Detectors;

/// <summary>
/// Provides functionality to detect whether a file is executable on the current operating system.
/// </summary>
/// <remarks>
/// <para>
/// The Executable verdict is derived from evidence read about the file itself. Each candidate is
/// opened once and its header bytes are read from that open handle, so no separate existence
/// probe stands between the enumeration and the read. The security descriptor, where the verdict
/// depends on the access check, is read from the same open handle and is not cached across files.
/// </para>
/// <para>
/// The extension set and the current user's identity are resolved once, when the detector is
/// constructed, so no environment read and no identity read happens on the per-file detection
/// path.
/// </para>
/// </remarks>
public class ExecutableFileDetector : IExecutableFileDetector
{
    /// <summary>
    /// The number of header bytes read from the open handle. It covers the longest magic number
    /// the detector compares, which is four bytes.
    /// </summary>
    private const int HeaderLength = 4;

    /// <summary>
    /// The value returned by the header read when the read faulted, so a faulted read is
    /// distinguishable from a short read.
    /// </summary>
    private const int ReadFaulted = -1;

    private static readonly byte[] MzMagicNumber = [0x4D, 0x5A];

    private static readonly byte[] MachO64BitMagicNumber = [0xFE, 0xED, 0xFA, 0xCF];

    private static readonly byte[] ElfMagicNumber = [0x7F, 0x45, 0x4C, 0x46];

    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// The executable extension set, held for the life of the instance so that no environment
    /// read happens per file.
    /// </summary>
    private readonly ExecutableExtensionSet _extensionSet;

    /// <summary>
    /// The current-user effective-access evaluator, holding the identity resolved once when the
    /// detector was constructed.
    /// </summary>
    private readonly WindowsEffectiveAccessEvaluator _accessEvaluator;

    private readonly bool _isMac;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutableFileDetector"/> class
    /// using the specified filesystem abstraction for all file access.
    /// </summary>
    /// <param name="fileSystem">The filesystem abstraction to read files through.</param>
    /// <exception cref="PlatformNotSupportedException"></exception>
    public ExecutableFileDetector(IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);

        if (OperatingSystem.IsBrowser() || OperatingSystem.IsTvOS())
            throw new PlatformNotSupportedException();

        _fileSystem = fileSystem;

        // The extension set reads PATHEXT once, here, and the evaluator resolves the current
        // user's identity once, here, so neither read happens on the per-file detection path.
        _extensionSet = new ExecutableExtensionSet();
        _accessEvaluator = new WindowsEffectiveAccessEvaluator();

        _isMac = OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutableFileDetector"/> class.
    /// </summary>
    /// <exception cref="PlatformNotSupportedException"></exception>
    public ExecutableFileDetector() : this(new FileSystem())
    {
    }

    /// <summary>
    /// Determines whether the specified file can be executed on the current operating system.
    /// </summary>
    /// <param name="file">The file to be checked for executability.</param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException">Thrown if the specified file does not exist.</exception>
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public bool IsFileExecutable(FileInfo file)
    {
        return IsFileExecutableAsync(file, CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Determines whether the specified file can be executed on the current operating system.
    /// </summary>
    /// <remarks>
    /// The candidate is opened once and the header bytes are read from that open handle. A file
    /// that cannot be opened is skipped rather than reported, while a missing file raises
    /// <see cref="FileNotFoundException"/> from the open call, which preserves the documented
    /// contract without an existence probe standing in front of it.
    /// </remarks>
    /// <param name="file">The file to be checked for executability.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use to cancel the detection.</param>
    /// <returns>True if the file is executable, false otherwise.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the specified file does not exist.</exception>
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<bool> IsFileExecutableAsync(FileInfo file, CancellationToken cancellationToken)
    {
        FileSystemStream? stream = OpenCandidate(file);

        if (stream is null)
        {
            // The candidate cannot be opened, so it is skipped rather than reported.
            return false;
        }

        using (stream)
        {
            byte[] header = new byte[HeaderLength];

            int bytesRead = await ReadHeaderAsync(stream, header, cancellationToken).ConfigureAwait(false);

            if (OperatingSystem.IsWindows())
            {
                return EvaluateWindows(file.Extension, stream, header, bytesRead);
            }

            if (_isMac || OperatingSystem.IsIOS())
            {
                return bytesRead != ReadFaulted && Matches(header, bytesRead, MachO64BitMagicNumber);
            }

            if (OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD())
            {
                return bytesRead != ReadFaulted && Matches(header, bytesRead, ElfMagicNumber);
            }

            // No magic-number check applies here, so the Unix execute verdict decides.
            return UnixExecuteVerdict.HasExecutePermission(stream);
        }
    }

    /// <summary>
    /// Opens the candidate file once, returning <see langword="null"/> when the file cannot be
    /// opened.
    /// </summary>
    /// <remarks>
    /// A missing file raises <see cref="FileNotFoundException"/> from the open call itself, and
    /// a missing directory raises <see cref="DirectoryNotFoundException"/>, so the documented
    /// contract holds without a prior existence probe. Every other failure to open means the
    /// candidate cannot be read, so it is skipped rather than reported.
    /// </remarks>
    /// <param name="file">The candidate to open.</param>
    /// <returns>The open stream, or <see langword="null"/> when the file cannot be opened.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the specified file does not exist.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown if the file's directory does not exist.</exception>
    private FileSystemStream? OpenCandidate(FileInfo file)
    {
        try
        {
            return _fileSystem.FileStream.New(file.FullName, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete, 4096, useAsync: true);
        }
        catch (FileNotFoundException)
        {
            throw;
        }
        catch (DirectoryNotFoundException)
        {
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    /// <summary>
    /// Reads the candidate's header bytes from the open handle.
    /// </summary>
    /// <remarks>
    /// A faulted read is reported as <see cref="ReadFaulted"/> rather than as a short read,
    /// because the caller decides whether a fault sends the verdict to the access check or ends
    /// it. A missing file and a missing directory still surface, and cancellation is never
    /// swallowed.
    /// </remarks>
    /// <param name="stream">The open handle the header bytes are read from.</param>
    /// <param name="header">The buffer the header bytes are read into.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use to cancel the read.</param>
    /// <returns>The number of bytes read, or <see cref="ReadFaulted"/> when the read faulted.</returns>
    private static async Task<int> ReadHeaderAsync(
        FileSystemStream stream,
        byte[] header,
        CancellationToken cancellationToken)
    {
        try
        {
            return await stream.ReadAsync(header, 0, header.Length, cancellationToken).ConfigureAwait(false);
        }
        catch (FileNotFoundException)
        {
            throw;
        }
        catch (DirectoryNotFoundException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return ReadFaulted;
        }
    }

    /// <summary>
    /// Applies the Windows verdict composition to the open candidate.
    /// </summary>
    /// <remarks>
    /// The composition keeps its three cases. The MZ magic number decides the verdict for
    /// <c>.exe</c> and <c>.dll</c>, with the access check as the fallback reached only when the
    /// magic-number read faults. The MZ magic number decides the verdict for <c>.com</c> on a
    /// 64-bit operating system, while the extension alone decides it on a 32-bit operating
    /// system, where no access check is applied. The extension plus the access check decides the
    /// verdict for the remaining allow-listed extensions, and for the <c>PATHEXT</c>-derived
    /// extensions, which carry no PE header and so are judged without a magic-number check.
    /// </remarks>
    /// <param name="extension">The candidate's file extension, including the leading dot.</param>
    /// <param name="stream">The open handle the descriptor is read from.</param>
    /// <param name="header">The header bytes read from the open handle.</param>
    /// <param name="bytesRead">The number of header bytes read, or <see cref="ReadFaulted"/>.</param>
    /// <returns><see langword="true"/> if the file is executable; otherwise <see langword="false"/>.</returns>
    private bool EvaluateWindows(
        string extension,
        FileSystemStream stream,
        byte[] header,
        int bytesRead)
    {
        if (!_extensionSet.Contains(extension))
        {
            return false;
        }

        if (!_extensionSet.IsHardcodedExtension(extension))
        {
            // A PATHEXT-derived extension has no PE header, so it is judged on its extension
            // and on the access check alone.
            return HasEffectiveExecuteAccess(stream);
        }

        if (string.Equals(extension, ".exe", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".dll", StringComparison.OrdinalIgnoreCase))
        {
            // The magic-number read is the verdict, and the access check is the fallback
            // reached only when that read faults.
            return bytesRead == ReadFaulted
                ? HasEffectiveExecuteAccess(stream)
                : Matches(header, bytesRead, MzMagicNumber);
        }

        if (string.Equals(extension, ".com", StringComparison.OrdinalIgnoreCase))
        {
            if (!Environment.Is64BitOperatingSystem)
            {
                // On a 32-bit operating system the extension alone decides the verdict, and
                // no access check is applied.
                return true;
            }

            return bytesRead != ReadFaulted && Matches(header, bytesRead, MzMagicNumber);
        }

        // The remaining allow-listed extensions are judged on their extension and on the
        // access check.
        return HasEffectiveExecuteAccess(stream);
    }

    /// <summary>
    /// Evaluates whether the current user holds effective execute access to the open candidate.
    /// </summary>
    /// <remarks>
    /// The descriptor is read from the open handle, so the descriptor and the header bytes
    /// describe the same file, and it is not cached across files. An unreadable descriptor means
    /// "cannot confirm", not "not executable", so it yields <see langword="false"/> and leaves
    /// the surrounding walk running.
    /// </remarks>
    /// <param name="stream">The open handle whose file descriptor is evaluated.</param>
    /// <returns><see langword="true"/> if the current user holds effective execute access; otherwise <see langword="false"/>.</returns>
    private bool HasEffectiveExecuteAccess(FileSystemStream stream)
    {
        try
        {
            return _accessEvaluator.HasEffectiveExecuteAccess(stream);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // Unreadable descriptor or sharing violation: cannot confirm, so do not claim
            // executable.
            return false;
        }
    }

    /// <summary>
    /// Determines whether the header bytes match the given magic number.
    /// </summary>
    /// <param name="header">The header bytes read from the open handle.</param>
    /// <param name="bytesRead">The number of header bytes read.</param>
    /// <param name="magicNumber">The magic number to compare against.</param>
    /// <returns><see langword="true"/> if the header carries the magic number; otherwise <see langword="false"/>.</returns>
    private static bool Matches(byte[] header, int bytesRead, byte[] magicNumber)
    {
        if (bytesRead < magicNumber.Length)
        {
            return false;
        }

        for (int i = 0; i < magicNumber.Length; i++)
        {
            if (header[i] != magicNumber[i])
            {
                return false;
            }
        }

        return true;
    }
}

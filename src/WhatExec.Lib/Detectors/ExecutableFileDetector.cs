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

namespace WhatExec.Lib.Detectors;

/// <summary>
/// Provides functionality to detect whether a file is executable on the current operating system.
/// </summary>
public class ExecutableFileDetector : IExecutableFileDetector
{
    private readonly IFileSystem _fileSystem;
    #region Magic Number helper code

    private static readonly byte[] MzMagicNumber = [0x4D, 0x5A];

    private static readonly byte[] MachO32BitMagicNumber = [0xFE, 0xED, 0xFA, 0xCE];
    private static readonly byte[] MachO64BitMagicNumber = [0xFE, 0xED, 0xFA, 0xCF];

    private static readonly byte[] ElfMagicNumber = [0x7F, 0x45, 0x4C, 0x46];
    
    private async Task<bool> ReadMagicNumberAsync(FileInfo file, byte[] magicNumberToCompare, CancellationToken cancellationToken)
    {
        try
        {
            using Stream fileStream = _fileSystem.FileStream.New(file.FullName, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete, 4096, useAsync: true);

            byte[] buffer = new byte[magicNumberToCompare.Length];

            int bytesRead = await fileStream.ReadAsync(buffer, 0, magicNumberToCompare.Length, cancellationToken).ConfigureAwait(false);

            return bytesRead == magicNumberToCompare.Length && buffer.SequenceEqual(magicNumberToCompare);
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
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }
    #endregion

    /// <summary>
    /// Probes whether a file has the execute permission, without letting a failed ACL read
    /// fail detection.
    /// </summary>
    /// <remarks>
    /// The permission probe is best-effort: it reads the file's security descriptor, which
    /// fails with <see cref="InvalidOperationException"/> on a sharing violation or when the
    /// descriptor is otherwise unreadable. Those conditions mean "cannot confirm", not
    /// "not executable" and not "detection failed", so they yield <see langword="false"/>
    /// and leave the surrounding walk running. A missing file still surfaces as
    /// <see cref="FileNotFoundException"/> to honour the documented contract, and
    /// cancellation is never swallowed.
    /// </remarks>
    /// <param name="file">The file whose execute permission is required.</param>
    /// <returns><see langword="true"/> if the permission is present; otherwise <see langword="false"/>.</returns>
    private static bool TryGetExecutePermission(FileInfo file)
    {
        try
        {
            return file.HasExecutePermission();
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
            // Unreadable ACL / sharing violation: cannot confirm, so do not claim executable.
            return false;
        }
    }

    private bool IsMac { get; }

    /// <summary>
    ///
    /// </summary>
    /// <exception cref="PlatformNotSupportedException"></exception>
    public ExecutableFileDetector() : this(new FileSystem())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutableFileDetector"/> class
    /// using the specified filesystem abstraction for all file access.
    /// </summary>
    /// <param name="fileSystem">The filesystem abstraction to read files through.</param>
    /// <exception cref="PlatformNotSupportedException"></exception>
    public ExecutableFileDetector(IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);

        _fileSystem = fileSystem;

        IsMac = OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst();

        if (OperatingSystem.IsBrowser() || OperatingSystem.IsTvOS())
            throw new PlatformNotSupportedException();
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
    /// <param name="file">The file to be checked for executability.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use to cancel the detection.</param>
    /// <returns>True if the file is executable, false otherwise.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the specified file does not exist.</exception>
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<bool> IsFileExecutableAsync(FileInfo file, CancellationToken cancellationToken)
    {
        if (!_fileSystem.File.Exists(file.FullName))
            throw new FileNotFoundException();

        if (OperatingSystem.IsWindows())
        {
            bool hasExecutableExtension = file.Extension.ToLowerInvariant() switch
            {
                // ReSharper disable once StringLiteralTypo
                ".exe" or ".msi" or ".appx" or ".com" or ".sys" or ".drv" or ".mui" or ".ocx" or ".ax" or ".msstyles" or ".scr"
                    or ".cpl" or ".acm" or ".efi" or ".dll" or ".tsp" => true,
                _ => false
            };

            switch (file.Extension.ToLowerInvariant())
            {
                case ".exe" or ".dll":
                {
                    try
                    {
                        bool magicNumberMatch = await ReadMagicNumberAsync(file, MzMagicNumber, cancellationToken).ConfigureAwait(false);

                        return hasExecutableExtension
                               && magicNumberMatch;
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
                        return hasExecutableExtension && TryGetExecutePermission(file);
                    }
                }
                case ".com":
                {
                    if (Environment.Is64BitOperatingSystem)
                    {
                        try
                        {
                            return hasExecutableExtension && await ReadMagicNumberAsync(file, MzMagicNumber, cancellationToken).ConfigureAwait(false);
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
                            return false;
                        }
                    }

                    return hasExecutableExtension;
                }
            }

            return hasExecutableExtension && TryGetExecutePermission(file);
        }
        if (IsMac || OperatingSystem.IsIOS())
        {
            try
            {
                return await ReadMagicNumberAsync(file, MachO64BitMagicNumber, cancellationToken).ConfigureAwait(false);
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
                return false;
            }
        }

        if (OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD())
        {
            try
            {
                return await ReadMagicNumberAsync(file, ElfMagicNumber, cancellationToken).ConfigureAwait(false);
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
                return false;
            }
        }

        return TryGetExecutePermission(file);
    }
}
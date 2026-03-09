/*
    WhatExec.Lib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

// ReSharper disable InconsistentNaming
// ReSharper disable UseUtf8StringLiteral

using WhatExec.Lib.Abstractions.Detectors;

namespace WhatExec.Lib.Detectors;

/// <summary>
/// Provides functionality to detect whether a file is executable on the current operating system.
/// </summary>
public class ExecutableFileDetector : IExecutableFileDetector
{
    #region Magic Number helper code

    private static readonly byte[] PEMagicNumber = "MZPE\0\0"u8.ToArray();

    private static readonly byte[] MzMagicNumber = [0x4D, 0x5A];

    private static readonly byte[] MachO32BitMagicNumber = [0xFE, 0xED, 0xFA, 0xCE];
    private static readonly byte[] MachO64BitMagicNumber = [0xFE, 0xED, 0xFA, 0xCF];

    private static readonly byte[] ElfMagicNumber = [0x7F, 0x45, 0x4C, 0x46];
    
    private async Task<bool> ReadMagicNumberAsync(FileInfo file, byte[] magicNumberToCompare, CancellationToken cancellationToken)
    {
        FileStream fileStream = new(file.FullName, FileMode.Open);
        
#if NET8_0_OR_GREATER
        await using (fileStream.ConfigureAwait(false))
#else
        using (fileStream)
#endif
        {
            byte[] buffer = new byte[magicNumberToCompare.Length];

            int bytesRead = await fileStream.ReadAsync(buffer, 0, magicNumberToCompare.Length, cancellationToken).ConfigureAwait(false);

#if DEBUG
            Console.WriteLine(Resources.Errors_ExecutableDetection_MagicNumberIssue, string.Join("", buffer), string.Join("", magicNumberToCompare));
#endif

            return buffer.SequenceEqual(magicNumberToCompare) && bytesRead == magicNumberToCompare.Length;
        }
    }
    #endregion

    private bool IsMac { get; }

    /// <summary>
    ///
    /// </summary>
    /// <exception cref="PlatformNotSupportedException"></exception>
    public ExecutableFileDetector()
    {
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
        Task<bool> isFileExecutable = IsFileExecutableAsync(file, CancellationToken.None);

        isFileExecutable.Wait();
        
        return isFileExecutable.Result;
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
        if (!file.Exists)
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

            if (string.Equals(file.Extension.ToLowerInvariant(), ".exe", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    bool magicNumberMatch = await ReadMagicNumberAsync(file, PEMagicNumber, cancellationToken).ConfigureAwait(false) ||
                                            await ReadMagicNumberAsync(file, MzMagicNumber, cancellationToken).ConfigureAwait(false);

                    return file.HasExecutePermission() &&
                           hasExecutableExtension
                           && magicNumberMatch;
                }
                catch
                {
                    return file.HasExecutePermission() && hasExecutableExtension;
                }
            }

            return file.HasExecutePermission() && hasExecutableExtension;
        }
        if (IsMac)
        {
            byte[] machOMagicNumber = Environment.Is64BitOperatingSystem ? MachO64BitMagicNumber : MachO32BitMagicNumber;

            return file.HasExecutePermission() && await ReadMagicNumberAsync(file, machOMagicNumber, cancellationToken).ConfigureAwait(false);
        }
        if (OperatingSystem.IsIOS())
        {
            return await ReadMagicNumberAsync(file, MachO64BitMagicNumber, cancellationToken).ConfigureAwait(false);
        }
        if (OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD())
        {
            return file.HasExecutePermission() && await ReadMagicNumberAsync(file, ElfMagicNumber, cancellationToken).ConfigureAwait(false);
        }

        return file.HasExecutePermission();
    }
}
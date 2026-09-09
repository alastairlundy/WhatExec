/*
    WhatExec.Lib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.ObjectModel;
using System.Globalization;

namespace WhatExec.Lib.Resolvers;

/// <summary>
/// Provides functionality to resolve the path of an executable file based on the system's PATH environment variable.
/// </summary>
public class PathEnvironmentVariableResolver : IPathEnvironmentVariableResolver
{
    private readonly IExecutableFileDetector _executableFileDetector;

    private readonly StringComparison _stringComparison;
    private readonly StringComparer _stringComparer;

    /// <summary>
    /// Represents a class that resolves file paths based on the system's PATH environment variable.
    /// </summary>
    /// <param name="executableFileDetector">The executable file detector to use.</param>
    public PathEnvironmentVariableResolver(IExecutableFileDetector executableFileDetector)
    {
        _executableFileDetector = executableFileDetector;

        _stringComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        _stringComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    }

    #region Internal Helpers

    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    internal async Task<(bool success, FileInfo? file)> CheckFileExistsAndIsExecutable(
        string filePath,
        CancellationToken cancellationToken)
    {
        if (!Path.IsPathRooted(filePath))
        {
            return (false, null);
        }

        if (!File.Exists(filePath)) return (false, null);

        FileInfo file = new(filePath);

        if (file.Exists && await _executableFileDetector.IsFileExecutableAsync(file, cancellationToken)
                .ConfigureAwait(false))
        {
            return (true, file);
        }

        return (false, null);
    }

    internal static string[] GetPathExtensions()
    {
        if (!OperatingSystem.IsWindows())
        {
            return [""];
        }

        char separator = OperatingSystem.IsWindows() ? ';' : ':';

        return Environment.GetEnvironmentVariable("PATHEXT")
                   ?.Split(separator, StringSplitOptions.RemoveEmptyEntries)
                   .Where(p => !string.IsNullOrWhiteSpace(p))
                   .Select(x =>
                   {
                       x = x.Trim();
                       x = x.Trim('"');
                       if (!x.StartsWith('.'))
                           x = x.Insert(0, ".");
                       return x;
                   })
                   .Distinct(StringComparer.OrdinalIgnoreCase)
                   .ToArray()
               ?? [".COM", ".EXE", ".BAT", ".CMD"];
    }

    internal static string[] GetPathContents()
    {
        char separator = OperatingSystem.IsWindows() ? ';' : ':';

        return Environment.GetEnvironmentVariable("PATH")
                   ?.Split(separator, StringSplitOptions.RemoveEmptyEntries)
                   .Where(p => !string.IsNullOrWhiteSpace(p))
                   .Select(x =>
                   {
                       x = x.Trim();
                       x = Environment.ExpandEnvironmentVariables(x);
                       x = x.Trim('"');
                       const string homeToken = "$HOME";
                       string userProfile = Environment.GetFolderPath(
                           Environment.SpecialFolder.UserProfile);

                       int homeTokenIndex = x.IndexOf(
                           homeToken,
                           StringComparison.CurrentCultureIgnoreCase
                       );

                       if (x.StartsWith("~/", StringComparison.Ordinal)
                           || x.StartsWith("~\\", StringComparison.Ordinal))
                       {
                           x =
                               $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}{x.Substring(1)}";
                       }

                       if (homeTokenIndex != -1)
                       {
                           return
                               $"{x.Substring(0, homeTokenIndex)}{userProfile}{x.Substring(homeTokenIndex + homeToken.Length)}";
                       }

                       x = x.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                       return x;
                   })
                   .ToArray()
               ?? [];
    }

    #endregion

    #region Single-name overloads over the batch core

    /// <inheritdoc/>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public IAsyncEnumerable<KeyValuePair<string, FileInfo>> EnumerateExecutableFilePathsAsync(
        string executableName, CancellationToken cancellationToken)
    {
        return EnumerateExecutableFilePathsAsync([executableName], cancellationToken);
    }

    /// <inheritdoc/>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public Task<IReadOnlyDictionary<string, FileInfo>> TryGetExecutableFilePathsAsync(
        string executableName, CancellationToken cancellationToken)
    {
        return TryGetExecutableFilePathsAsync([executableName], cancellationToken);
    }

    #endregion

    #region Batch core — Enumerate

    /// <inheritdoc/>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async IAsyncEnumerable<KeyValuePair<string, FileInfo>> EnumerateExecutableFilePathsAsync(
        IEnumerable<string> executableNames,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Materialize one snapshot for repeat passes and Count sizing
        IReadOnlyList<string> snapshot = executableNames as IReadOnlyList<string>
                                         ?? executableNames.ToArray();

        string[] pathExtensions = GetPathExtensions();
        string[] pathContents = GetPathContents();

        foreach (string name in snapshot)
        {
            KeyValuePair<string, FileInfo>? result =
                await FindFirstPathMatchAsync(name, pathContents, pathExtensions, cancellationToken)
                    .ConfigureAwait(false);

            if (result.HasValue)
            {
                yield return result.Value;
            }
        }
    }

    #endregion

    #region Batch core — TryGet

    /// <inheritdoc/>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<IReadOnlyDictionary<string, FileInfo>> TryGetExecutableFilePathsAsync(
        IEnumerable<string> executableNames, CancellationToken cancellationToken)
    {
        // Materialize one snapshot for repeat passes and Count sizing
        IReadOnlyList<string> snapshot = executableNames as IReadOnlyList<string>
                                         ?? executableNames.ToArray();

        string[] pathExtensions = GetPathExtensions();
        string[] pathContents = GetPathContents();

        Dictionary<string, FileInfo> output = new(snapshot.Count, _stringComparer);

        foreach (string name in snapshot)
        {
            KeyValuePair<string, FileInfo>? result =
                await FindFirstPathMatchAsync(name, pathContents, pathExtensions, cancellationToken)
                    .ConfigureAwait(false);

            if (result.HasValue)
            {
                output.TryAdd(result.Value.Key, result.Value.Value);
            }
        }

        return new ReadOnlyDictionary<string, FileInfo>(output);
    }

    #endregion

    #region Single PATH-walk core

    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    private async Task<KeyValuePair<string, FileInfo>?> FindFirstPathMatchAsync(
        string executableName,
        string[] pathContents,
        string[] pathExtensions,
        CancellationToken cancellationToken)
    {
        // If the name is rooted or contains a directory separator, check it directly
        if (Path.IsPathRooted(executableName)
            || executableName.Contains(Path.DirectorySeparatorChar, _stringComparison)
            || executableName.Contains(Path.AltDirectorySeparatorChar, _stringComparison))
        {
            (bool success, FileInfo? file) checkResult =
                await CheckFileExistsAndIsExecutable(executableName, cancellationToken).ConfigureAwait(false);

            if (checkResult.success && checkResult.file is not null)
            {
                return new KeyValuePair<string, FileInfo>(executableName, checkResult.file);
            }

            return null;
        }

        bool fileHasExtension = Path.GetExtension(executableName) != string.Empty;

        // Walk each PATH directory — first-match semantics: return on first hit
        foreach (string pathEntry in pathContents)
        {
            if (!fileHasExtension && OperatingSystem.IsWindows())
            {
                foreach (string pathExtension in pathExtensions)
                {
                    string filePath = Path.Combine(pathEntry,
                        $"{Path.GetFileNameWithoutExtension(executableName)}{pathExtension.ToLower(CultureInfo.InvariantCulture)}");

                    (bool success, FileInfo? file) result =
                        await CheckFileExistsAndIsExecutable(filePath, cancellationToken).ConfigureAwait(false);

                    if (result.success && result.file is not null)
                    {
                        return new KeyValuePair<string, FileInfo>(executableName, result.file);
                    }
                }
            }
            else
            {
                string filePath = Path.Combine(pathEntry, Path.GetFileName(executableName));

                (bool success, FileInfo? file) result =
                    await CheckFileExistsAndIsExecutable(filePath, cancellationToken).ConfigureAwait(false);

                if (result.success && result.file is not null)
                {
                    return new KeyValuePair<string, FileInfo>(executableName, result.file);
                }
            }
        }

        return null;
    }

    #endregion
}

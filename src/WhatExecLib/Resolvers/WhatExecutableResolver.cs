/*
    WhatExecLib
    Copyright (c) 2025 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using AlastairLundy.WhatExecLib.Abstractions;
using AlastairLundy.WhatExecLib.Abstractions.Locators;

namespace AlastairLundy.WhatExecLib;

/// <summary>
/// Provides functionality to resolve the full file path of an executable based on a given input path.
/// </summary>
public class WhatExecutableResolver : IWhatExecutableResolver
{
    private readonly IPathExecutableResolver _pathExecutableResolver;
    private readonly IExecutableFileInstancesLocator _executableFileInstancesLocator;

    /// <summary>
    ///
    /// </summary>
    /// <param name="pathExecutableResolver"></param>
    /// <param name="executableFileInstancesLocator"></param>
    public WhatExecutableResolver(
        IPathExecutableResolver pathExecutableResolver,
        IExecutableFileInstancesLocator executableFileInstancesLocator
    )
    {
        _pathExecutableResolver = pathExecutableResolver;
        _executableFileInstancesLocator = executableFileInstancesLocator;
    }

    /// <summary>
    /// Resolves the full file path to an executable based on the provided input path.
    /// </summary>
    /// <param name="inputFilePath">The path or name of the file to resolve. This can be a full path, a relative path, or just the file name.</param>
    /// <returns>The resolved <see cref="FileInfo"/> object representing the full path to the file.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file could not be resolved or located.</exception>
    public FileInfo ResolveExecutableFilePath(string inputFilePath)
    {
        try
        {
            FileInfo pathEnvFile = _pathExecutableResolver.ResolvePathEnvironmentExecutableFile(
                inputFilePath
            );

#if NETSTANDARD2_0
            if (PathPolyfill.Exists(pathEnvFile.FullName))
#else
            if (Path.Exists(pathEnvFile.FullName))
#endif
            {
                return pathEnvFile;
            }

            throw new FileNotFoundException();
        }
        catch
        {
            string fileName = Path.GetFileName(inputFilePath);

            IEnumerable<FileInfo> results =
                _executableFileInstancesLocator.LocateExecutableInstances(
                    fileName,
                    SearchOption.TopDirectoryOnly
                );

            return results.First();
        }
    }

    /// <summary>
    /// Attempts to resolve the full file path to an executable based on the provided input path.
    /// </summary>
    /// <param name="inputFilePath">The path or name of the file to resolve. This can be a full path, a relative path, or just the file name.</param>
    /// <param name="fileInfo">When the method returns, contains the resolved <see cref="FileInfo"/> object representing the file's full path, if the resolution was successful. Otherwise, it is null.</param>
    /// <returns>True if the file path was resolved successfully; otherwise, false.</returns>
    public bool TryResolveExecutableFilePath(string inputFilePath, out FileInfo? fileInfo)
    {
        bool foundPathResult = _pathExecutableResolver.TryResolvePathEnvironmentExecutableFile(
            inputFilePath,
            out FileInfo? pathOutput
        );

        if (foundPathResult && pathOutput is not null)
        {
            fileInfo = pathOutput;
            return true;
        }

        string fileName = Path.GetFileName(inputFilePath);

        IEnumerable<FileInfo> results = _executableFileInstancesLocator.LocateExecutableInstances(
            fileName,
            SearchOption.TopDirectoryOnly
        );

        fileInfo = results.FirstOrDefault();
        return pathOutput is not null;
    }
}

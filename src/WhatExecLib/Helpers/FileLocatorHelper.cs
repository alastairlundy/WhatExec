using WhatExec.Lib.Abstractions.Detectors;

namespace WhatExec.Lib.Helpers;

internal class FileLocatorHelper
{
    internal static async Task<FileInfo?> HandleRootedPath(IExecutableFileDetector executableFileDetector, string executableFileName, CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(executableFileName))
                return null;

            FileInfo file = new FileInfo(executableFileName);

            return await executableFileDetector.IsFileExecutableAsync(file, cancellationToken) ? file : null;
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }
}
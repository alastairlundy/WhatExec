namespace WhatExecLib.Helpers;

internal class FileLocatorHelper
{
    internal static FileInfo? HandleRootedPath(IExecutableFileDetector executableFileDetector, string executableFileName)
    {
        try
        {
            if (!File.Exists(executableFileName))
                return null;

            FileInfo file = new FileInfo(executableFileName);

            return executableFileDetector.IsFileExecutable(file) ? file : null;
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }
}
namespace WhatExec.Lib.Extensions;

internal static class SearchPatternExtensions
{
    internal static IEnumerable<string> GetSearchPatterns(this string executableFileName)
    {
        FileInfo fileInfo = new FileInfo(executableFileName);

        if (Path.HasExtension(executableFileName))
        {
            yield return fileInfo.Extension;
        }

        yield return fileInfo.Name;
    }
}

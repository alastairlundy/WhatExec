namespace WhatExecLib.Extensions;

public static class SearchPatternExtensions
{
    public static IEnumerable<string> GetSearchPatterns(this string executableFileName)
    {
        FileInfo fileInfo = new FileInfo(executableFileName);

        if (Path.HasExtension(executableFileName))
        {
            yield return fileInfo.Extension;
        }

        yield return fileInfo.Name;
    }
}

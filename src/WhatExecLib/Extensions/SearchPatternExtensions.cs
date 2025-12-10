namespace WhatExecLib.Extensions;

internal static class SearchPatternExtensions
{
    extension(string executableFileName)
    {
        internal IEnumerable<string> GetSearchPatterns()
        {
            FileInfo fileInfo = new FileInfo(executableFileName);

            if (Path.HasExtension(executableFileName))
            {
                yield return fileInfo.Extension;
            }

            yield return fileInfo.Name;
        }
    }
}

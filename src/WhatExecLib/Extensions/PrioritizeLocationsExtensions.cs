namespace WhatExecLib.Extensions;

public static class PrioritizeLocationsExtensions
{
    private static int ComputeDirectoryPriorityScore(DirectoryInfo directory)
    {
        string dirPathName = directory.FullName.ToLower();

        if (dirPathName.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.Programs)))
            return 0;

        if (OperatingSystem.IsWindows())
        {
            if (
                dirPathName.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.Windows))
            )
                return 1;

            if (
                dirPathName.StartsWith(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                )
            )
                return 2;

            if (
                dirPathName.StartsWith(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
                )
            )
                return 2;

            if (
                dirPathName.StartsWith(
                    Environment.GetFolderPath(Environment.SpecialFolder.AdminTools)
                )
            )
                return 3;
        }

        if (dirPathName.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.System)))
            return 2;

        if (
            dirPathName.StartsWith(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
            )
        )
            return 4;

        return 10;
    }

    internal static IEnumerable<DirectoryInfo> PrioritizeLocations(
        this IEnumerable<DirectoryInfo> directories
    )
    {
        return directories.OrderBy(x => ComputeDirectoryPriorityScore(x));
    }
}

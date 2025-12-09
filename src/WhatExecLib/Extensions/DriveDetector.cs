namespace WhatExecLib.Extensions;

internal static class DriveDetector
{
    internal static IEnumerable<DriveInfo> EnumerateDrives()
    {
        return Environment
            .GetLogicalDrives()
            .Select(d => new DriveInfo(d))
            .Where(drive => drive.IsReady && !drive.DriveType.HasFlag(DriveType.Network))
            .Where(d =>
            {
                if (OperatingSystem.IsWindows())
                    if (d.DriveType.HasFlag(DriveType.Ram))
                        return false;

                try
                {
                    return d.TotalSize > 0 && d.TotalFreeSpace > 0;
                }
                catch
                {
                    return false;
                }
            })
            .Where(d =>
            {
                try
                {
                    if (d.Name.Length == 1)
                        return false;

                    if (
                        d.Name.ToLower().StartsWith("/sys")
                        || d.Name.ToLower().StartsWith("/run")
                        || d.Name.ToLower().StartsWith("/proc")
                        || d.Name.ToLower().StartsWith("/tmp")
                    )
                        return false;

                    return true;
                }
                catch
                {
                    return false;
                }
            });
    }
}

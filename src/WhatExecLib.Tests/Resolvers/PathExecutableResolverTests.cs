namespace WhatExecLib.Tests.Resolvers;

public class PathExecutableResolverTests
{
    private string ProgramFilesDirectory => Environment.GetFolderPath(Environment.Is64BitOperatingSystem
        ? Environment.SpecialFolder.ProgramFiles
        : Environment.SpecialFolder.ProgramFilesX86);

    [Test]
    public async Task Resolve_Dotnet_Path_Executable()
    {
        PathExecutableResolver filePathResolver = new PathExecutableResolver();

        FileInfo actual =
            filePathResolver.ResolveExecutableFilePath(OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet")
                .Value;
        
        FileInfo expected;

        if (OperatingSystem.IsWindows())
        {
            expected = new DirectoryInfo(ProgramFilesDirectory)
                .EnumerateFiles("*", new EnumerationOptions()
                {
                    IgnoreInaccessible = true,
                    MatchCasing = MatchCasing.CaseInsensitive,
                    RecurseSubdirectories = true
                })
                .First(f => f.Name.Equals("dotnet.exe"));
        }
        else
        {
            expected = new  FileInfo("/usr/bin/dotnet");
        }

        
        await Assert.That(expected.FullName).
            IsEqualTo(actual.FullName);
    }
}
using WhatExec.Lib;
using WhatExec.Lib.Resolvers;

namespace WhatExecLib.Tests.Resolvers;

public class PathExecutableResolverTests
{
    private readonly IPathEnvironmentVariableResolver _pathVariableResolver;

    public PathExecutableResolverTests()
    {
        IExecutableFileDetector executableFileDetector = new ExecutableFileDetector();
        _pathVariableResolver = new PathEnvironmentVariableResolver(executableFileDetector);
    }
    
    private string ProgramFilesDirectory => Environment.GetFolderPath(Environment.Is64BitOperatingSystem
        ? Environment.SpecialFolder.ProgramFiles
        : Environment.SpecialFolder.ProgramFilesX86);

    [Test]
    public async Task Resolve_Dotnet_Path_Executable()
    {
        string executableName = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";
        
        IReadOnlyDictionary<string, FileInfo> actual =
            await _pathVariableResolver.TryGetExecutableFilePathsAsync(
                executableName, CancellationToken.None);
        
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

        await Assert.That(actual.ContainsKey(executableName)).IsTrue();
        await Assert.That(expected.FullName).IsEqualTo(actual[executableName].FullName);
    }
}

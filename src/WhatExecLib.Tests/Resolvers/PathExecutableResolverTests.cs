using WhatExecLib.Abstractions;
using WhatExecLib.Abstractions.Detectors;
using WhatExecLib.Detectors;

namespace WhatExecLib.Tests.Resolvers;

public class PathExecutableResolverTests
{
    private readonly IPathEnvironmentVariableResolver _pathVariableResolver;

    public PathExecutableResolverTests()
    {
        IPathEnvironmentVariableDetector pathVariableDetector = new PathEnvironmentVariableDetector();
        _pathVariableResolver = new PathEnvironmentVariableResolver(pathVariableDetector);
    }
    
    private string ProgramFilesDirectory => Environment.GetFolderPath(Environment.Is64BitOperatingSystem
        ? Environment.SpecialFolder.ProgramFiles
        : Environment.SpecialFolder.ProgramFilesX86);

    [Test]
    public async Task Resolve_Dotnet_Path_Executable()
    {
        FileInfo actual =
            _pathVariableResolver.ResolveExecutableFilePath(OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet")
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
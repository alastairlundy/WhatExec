using WhatExec.Lib;
using WhatExec.Lib.Abstractions;
using WhatExec.Lib.Abstractions.Detectors;
using WhatExec.Lib.Detectors;

namespace WhatExecLib.Tests.Resolvers;

public class PathExecutableResolverTests
{
    private readonly IPathEnvironmentVariableResolver _pathVariableResolver;

    public PathExecutableResolverTests()
    {
        IExecutableFileDetector executableFileDetector = new ExecutableFileDetector();
        IPathEnvironmentVariableDetector pathVariableDetector = new PathEnvironmentVariableDetector();
        _pathVariableResolver = new PathEnvironmentVariableResolver(pathVariableDetector, executableFileDetector);
    }
    
    private string ProgramFilesDirectory => Environment.GetFolderPath(Environment.Is64BitOperatingSystem
        ? Environment.SpecialFolder.ProgramFiles
        : Environment.SpecialFolder.ProgramFilesX86);

    [Test]
    public async Task Resolve_Dotnet_Path_Executable()
    {
        KeyValuePair<string, FileInfo> actual =
            await _pathVariableResolver.ResolveExecutableFilePathAsync(
                OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet", CancellationToken.None);
        
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
            IsEqualTo(actual.Value.FullName);
    }
}
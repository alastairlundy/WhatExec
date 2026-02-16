using WhatExec.Lib;
using WhatExec.Lib.Abstractions;
using WhatExec.Lib.Abstractions.Detectors;
using WhatExec.Lib.Detectors;

namespace WhatExecLib.Tests.Resolvers;

public class ExecutableFileResolverTests
{
    private readonly IExecutableFileResolver _executableFileResolver;

    public ExecutableFileResolverTests()
    {
        IExecutableFileDetector executableFileDetector = new ExecutableFileDetector();
        _executableFileResolver = new ExecutableFileResolver(new ExecutableFileDetector(),
            new PathEnvironmentVariableResolver(new PathEnvironmentVariableDetector(), executableFileDetector));
    }

    [Test]
    public async Task Resolve_VsCode_ExecutableFile()
    {
        string codeExecutableName = OperatingSystem.IsWindows() ? "Code.exe" : "code";
        
        FileInfo actual = await _executableFileResolver.LocateExecutableAsync(codeExecutableName, SearchOption.AllDirectories, CancellationToken.None);
        
        await Assert.That(actual)
            .IsNotNull()
            .And
            .IsNotEmpty();
    }
}
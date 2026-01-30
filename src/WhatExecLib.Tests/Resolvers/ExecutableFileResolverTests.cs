using WhatExecLib.Abstractions;
using WhatExecLib.Detectors;

namespace WhatExecLib.Tests.Resolvers;

public class ExecutableFileResolverTests
{
    private readonly IExecutableFileResolver _executableFileResolver;

    public ExecutableFileResolverTests()
    {
        _executableFileResolver = new ExecutableFileResolver(new ExecutableFileDetector(),
            new PathEnvironmentVariableResolver(new PathEnvironmentVariableDetector()));
    }

    [Test]
    public async Task Resolve_VsCode_ExecutableFile()
    {
        FileInfo? actual =  _executableFileResolver.LocateExecutable("code.exe", SearchOption.AllDirectories);
        
        await Assert.That(actual)
            .IsNotNull()
            .And
            .IsNotEmpty();
    }
}
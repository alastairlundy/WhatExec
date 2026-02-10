using WhatExecLib.Abstractions;
using WhatExecLib.Abstractions.Detectors;
using WhatExecLib.Detectors;

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
        FileInfo actual = await _executableFileResolver.LocateExecutableAsync("Code.exe", SearchOption.AllDirectories, CancellationToken.None);
        
        await Assert.That(actual)
            .IsNotNull()
            .And
            .IsNotEmpty();
    }
}
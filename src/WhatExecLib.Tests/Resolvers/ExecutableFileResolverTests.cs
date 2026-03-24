using System.Collections.Concurrent;
using System.Diagnostics;
using WhatExec.Lib.Abstractions.Detectors;
using WhatExec.Lib.Abstractions.Resolvers;
using WhatExec.Lib.Detectors;
using WhatExec.Lib.Resolvers;

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

        FileInfo actual = await _executableFileResolver.LocateExecutableAsync(codeExecutableName,
            SearchOption.AllDirectories, CancellationToken.None);

        await Assert.That(actual)
            .IsNotNull()
            .And
            .IsNotEmpty();
    }

    [Test]
    public async Task Resolve_Random_Running_Process_ExecutableFiles()
    {
        string[] executableNames = Process.GetProcesses().Select(p => p.ProcessName).Distinct().ToArray();
        
        int randomProcess = Random.Shared.Next(0, executableNames.Length - 1);

        FileInfo result = await _executableFileResolver.LocateExecutableAsync(executableNames[randomProcess], SearchOption.AllDirectories
            ,CancellationToken.None);

        await Assert.That(result.Exists)
            .IsTrue();

        await Assert.That(result.FullName)
            .IsNotEqualTo(string.Empty);
    }
}
using AlastairLundy.WhatExecLib.Abstractions;
using AlastairLundy.WhatExecLib.Caching;
using ConsoleAppFramework;

namespace WhatExecLite;

public partial class CliCommands
{
    [Command("")]
    public int Run(
        [FromServices] IPathExecutableResolver pathExecutableResolver,
        [FromServices] ICachedPathExecutableResolver cachedPathExecutableResolver,
        [HideDefaultValue] bool useCaching = true,
        bool verbose = false,
        [Argument] params string[] commands
    )
    {
        try
        {
            IEnumerable<FileInfo> resolvedCommands = ResolveCommands(
                pathExecutableResolver,
                cachedPathExecutableResolver,
                commands,
                useCaching
            );

            foreach (FileInfo resolvedCommand in resolvedCommands)
            {
                Console.Out.WriteLine(resolvedCommand.FullName);
            }

            return 0;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(
                "We ran into a problem. Here's the Exception details in case you need it: "
            );
            Console.Error.WriteLine(e.Message);

            if (verbose)
            {
                Console.Error.Write(e.StackTrace);
            }

            return 1;
        }
    }

    private IEnumerable<FileInfo> ResolveCommands(
        IPathExecutableResolver pathExecutableResolver,
        ICachedPathExecutableResolver cachedPathExecutableResolver,
        string[] commands,
        bool useCaching
    )
    {
        foreach (string command in commands)
        {
            bool found = TryResolveCommand(
                pathExecutableResolver,
                cachedPathExecutableResolver,
                useCaching,
                command,
                out FileInfo? info
            );

            if (found && info is not null)
                yield return info;
        }
    }

    private static bool TryResolveCommand(
        IPathExecutableResolver pathExecutableResolver,
        ICachedPathExecutableResolver cachedPathExecutableResolver,
        bool useCaching,
        string command,
        out FileInfo? info
    )
    {
        bool found;
        if (useCaching)
        {
            found = cachedPathExecutableResolver.TryResolvePathEnvironmentExecutableFile(
                command,
                out info
            );
        }
        else
        {
            found = pathExecutableResolver.TryResolvePathEnvironmentExecutableFile(
                command,
                out info
            );
        }

        return found;
    }
}

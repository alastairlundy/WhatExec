namespace AlastairLundy.WhatExec.Cli.Commands.SingleSearch;

public class PathOnlySearchCommand : AsyncCommand<PathOnlySearchCommand.Settings>
{
    private readonly IPathExecutableResolver _pathExecutableResolver;
    private readonly ICachedPathExecutableResolver _cachedPathExecutableResolver;

    public PathOnlySearchCommand(
        IPathExecutableResolver pathExecutableResolver,
        ICachedPathExecutableResolver cachedPathExecutableResolver
    )
    {
        _pathExecutableResolver = pathExecutableResolver;
        _cachedPathExecutableResolver = cachedPathExecutableResolver;
    }

    public class Settings : SingleSearchBaseCommandSettings { }

    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
        string[] commands = settings.Commands ?? UserInputHelper.GetCommandInput();

        try
        {
            IList<FileInfo> ResolveCommands()
            {
                List<FileInfo> output = new List<FileInfo>();

                foreach (string command in commands)
                {
                    bool found;
                    FileInfo? info;

                    if (settings.UseCaching)
                    {
                        found =
                            _cachedPathExecutableResolver.TryResolvePathEnvironmentExecutableFile(
                                command,
                                out info
                            );
                    }
                    else
                    {
                        found = _pathExecutableResolver.TryResolvePathEnvironmentExecutableFile(
                            command,
                            out info
                        );
                    }

                    if (found && info is not null)
                        output.Add(info);
                }

                return output;
            }

            Task<IList<FileInfo>> resolverTask = Task.Run(
                () => ResolveCommands(),
                cancellationToken
            );

            IList<FileInfo> resolvedCommands = await resolverTask;

            foreach (FileInfo resolvedCommand in resolvedCommands)
            {
                Console.WriteLine(resolvedCommand.FullName);
            }
        }
        catch (Exception e)
        {
            ExceptionFormats formats = settings.ShowErrorsAndBeVerbose
                ? ExceptionFormats.Default
                : ExceptionFormats.ShortenEverything;

            AnsiConsole.WriteException(e, formats);
            return 1;
        }

        return 0;
    }
}

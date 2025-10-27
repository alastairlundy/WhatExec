using System.Threading;
using Spectre.Console.Cli;
using XpWhich.Cli.Settings;

namespace XpWhich.Cli.Commands;

public class WhichCompatCommand : Command<WhichCompatCommandSettings>
{
    public override int Execute(CommandContext context, WhichCompatCommandSettings settings, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}
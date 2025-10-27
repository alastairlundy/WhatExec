using System.Threading;
using Spectre.Console.Cli;
using XpWhich.Cli.Settings;

namespace XpWhich.Cli.Commands;

public class PrettyXpWhichCommand : Command<XpWhichCommandSettings>
{
    public override int Execute(CommandContext context, XpWhichCommandSettings settings, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}
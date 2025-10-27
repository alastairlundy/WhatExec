using System.Threading;
using Spectre.Console.Cli;
using WhatExec.Cli.Settings;

namespace WhatExec.Cli.Commands;

public class WhatExecCommand : Command<XpWhichCommandSettings>
{
    public override int Execute(CommandContext context, XpWhichCommandSettings settings,
        CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}
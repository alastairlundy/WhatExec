namespace WhatExec.Cli.Helpers;

public class ResultHelper
{
    
    public static int PrintResults(Dictionary<string, List<FileInfo>> results, int limit)
    {
        foreach (KeyValuePair<string, List<FileInfo>> result in results)
        {
            IEnumerable<string> allowedResults = result.Value.Take(limit)
                .Select(f => f.FullName);

            string joinedString = string.Join(Environment.NewLine, allowedResults);

            AnsiConsole.WriteLine(joinedString);
        }

        return 0;
    }
}
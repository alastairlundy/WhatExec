using System.Text;

namespace WhatExec.Cli.Helpers;

public class ResultHelper
{
    public static int PrintException<TException>(TException exception, bool isVerbose, int exitCode)
        where TException : Exception
    {
        Console.Error.WriteLine(exception.Message);

        if (isVerbose)
        {
            Console.Error.Write(exception.StackTrace);
        }

        return exitCode;
    }
    
    public static async Task<int> PrintFileSearchResultsAsync(IAsyncEnumerable<FileInfo> results, int limit)
    {
        int total = 0;
        
        await foreach (FileInfo result in results)
        {
            if (total < limit)
            {
                Console.WriteLine(result.FullName.TrimEnd(" ").TrimEnd(Environment.NewLine));
                Interlocked.Increment(ref total);
            }

            if (total == limit)
            {
                break;
            }
        }

        return total > 0 ? 0 : -1;
    }
    
    public static int PrintFileSearchResults(IEnumerable<FileInfo> results, int limit)
    {
        string[] allowedResults = results.Take(limit)
            .Select(f => f.FullName)
            .ToArray();
        
        string joinedString = string.Join(Environment.NewLine, allowedResults);

        Console.WriteLine(joinedString);

        return allowedResults.Length > 0 ? 0 : -1;
    }

    private static int HandleIncompleteResults(IEnumerable<string> missedCommands, int resultsCount)
    {
        Console.WriteLine(Resources.Errors_Results_CommandsNotFound.Replace("{x}", string.Join(", ", missedCommands)).TrimEnd(", "));
        return resultsCount > 0 ? 1 : -1;
    }
    
    public static int PrintResults(Dictionary<string, List<FileInfo>> results, string[] commands, int limit)
    {
        foreach (KeyValuePair<string, List<FileInfo>> result in results)
        {
            IEnumerable<string> allowedResults = result.Value.Take(limit)
                .Select(f => f.FullName);

            string joinedString = string.Join(Environment.NewLine, allowedResults);

            Console.WriteLine(joinedString);
        }
        
        if (results.Keys.Count == commands.Length)
        {
            return 0;
        }

        IEnumerable<string> missedCommands = commands.Where(c => !results.ContainsKey(c));

        return HandleIncompleteResults(missedCommands, results.Count);
    }
    
    public static int PrintResults(Dictionary<string, FileInfo> results, string[] commands)
    {
        StringBuilder stringBuilder = new StringBuilder();

        if (results.Count > 0)
        {
            foreach (KeyValuePair<string, FileInfo> result in results)
            {
                stringBuilder.AppendLine(result.Value.FullName);
            }
            
            Console.Write(stringBuilder.ToString());
        }

        if (results.Keys.Count == commands.Length)
        {
            return 0;
        }

        IEnumerable<string> missedCommands = commands.Where(c => !results.ContainsKey(c));
        
        return HandleIncompleteResults(missedCommands, results.Count);
    }
}
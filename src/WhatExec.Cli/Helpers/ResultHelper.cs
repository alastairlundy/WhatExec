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
    
    public static async Task<int> PrintResults(IAsyncEnumerable<FileInfo> results, int limit)
    {
        int current = 0;
        
        await foreach (FileInfo result in results)
        {
            if (current < limit)
            {
                AnsiConsole.WriteLine(result.FullName);
                
                Interlocked.Increment(ref current);
            }

            if (current == limit)
            {
                break;
            }
        }

        return 0;
    }
    
    public static int PrintResults(IEnumerable<FileInfo> results, int limit)
    {
        IEnumerable<string> allowedResults = results.Take(limit)
            .Select(f => f.FullName);

        string joinedString = string.Join(Environment.NewLine, allowedResults);

        AnsiConsole.WriteLine(joinedString);

        return 0;
    }
    
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
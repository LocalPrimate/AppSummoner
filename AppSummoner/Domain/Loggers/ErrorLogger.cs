namespace AppSummoner.Domain.Loggers;

public static class ErrorLogger
{
    // 1. Log errors at application startup
    // 2. Log errors at application shutdown (cleanups and stuff)
    // 3. Log every exception throughout the application lifecycle
    // 4. One log file per month?
    //   a. Look up the general/good practice for log frequencies

    public static void LogException(Exception ex)
    {
        // Not implemented yet
        // Todo:
        //   1. Get datetime
        
        Console.WriteLine(ex);
    }
    
    public static void LogException(Exception ex, string message)
    {
        // Not implemented yet
        // Todo:
        //   1. Get datetime
        
        Console.WriteLine($"{message}: {ex}");
    }

    public static void Log(string message)
    {
        // Todo:
        //   1. Get datetime
        
        Console.WriteLine(message);
    }
}

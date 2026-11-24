namespace MyCli;

internal static class Handlers
{
    public static async Task<int> ScanAsync(string path, CancellationToken token)
    {
        try
        {
            var count = await LineCounter.CountAsync(Path.GetFullPath(path), token);
            Console.WriteLine(count);
            return 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            Console.Error.WriteLine($"Cannot scan directory: {ex.Message}");
            return 2;
        }
    }

    public static async Task<int> SleepAsync(int milliseconds, CancellationToken token)
    {
        if (milliseconds < 0)
        {
            Console.Error.WriteLine("Milliseconds must be non-negative.");
            return 2;
        }
        try
        {
            await Task.Delay(milliseconds, token);
            return 0;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            Console.Error.WriteLine("Cancelled.");
            return 3;
        }
    }
}

namespace MyCli;

internal static class LineCounter
{
    public static async Task<long> CountAsync(string path, CancellationToken token)
    {
        long count = 0;
        foreach (var file in Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories))
        {
            token.ThrowIfCancellationRequested();
            using var reader = File.OpenText(file);
            while (await reader.ReadLineAsync(token) is { } line)
                if (!string.IsNullOrWhiteSpace(line)) count++;
        }
        return count;
    }
}

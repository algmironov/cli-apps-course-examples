using System.Diagnostics;

namespace MyCli.Tests;

internal sealed record CliResult(int ExitCode, string StandardOutput, string StandardError, bool TimedOut);

internal static class CliProcessRunner
{
    public static async Task<CliResult> RunAsync(string[] arguments, string? workingDirectory = null,
        string? input = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        var info = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            WorkingDirectory = workingDirectory ?? AppContext.BaseDirectory
        };
        info.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "mycli.dll"));
        foreach (var argument in arguments) info.ArgumentList.Add(argument);

        using var process = Process.Start(info) ?? throw new InvalidOperationException("Could not start CLI.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var deadline = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(15));
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(deadline.Token, cancellationToken);
        var timedOut = false;
        try
        {
            if (input is not null) await process.StandardInput.WriteAsync(input.AsMemory(), stop.Token);
            process.StandardInput.Close();
            await process.WaitForExitAsync(stop.Token);
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested)
        {
            timedOut = deadline.IsCancellationRequested && !cancellationToken.IsCancellationRequested;
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
            await process.WaitForExitAsync();
            await Task.WhenAll(stdout, stderr);
            if (!timedOut) cancellationToken.ThrowIfCancellationRequested();
        }
        return new CliResult(process.ExitCode, await stdout, await stderr, timedOut);
    }
}

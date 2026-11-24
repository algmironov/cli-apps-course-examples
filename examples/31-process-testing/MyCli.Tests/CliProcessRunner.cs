using System.Diagnostics;

namespace MyCli.Tests;

internal sealed record CliResult(int ExitCode, string StandardOutput, string StandardError, bool TimedOut);

internal static class CliProcessRunner
{
    private static readonly TimeSpan CleanupTimeout = TimeSpan.FromSeconds(5);

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

        using var deadline = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(15));
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(deadline.Token, cancellationToken);
        using var readStop = new CancellationTokenSource();
        using var process = Process.Start(info) ?? throw new InvalidOperationException("Could not start CLI.");
        var stdout = process.StandardOutput.ReadToEndAsync(readStop.Token);
        var stderr = process.StandardError.ReadToEndAsync(readStop.Token);
        var completion = Task.WhenAll(process.WaitForExitAsync(), stdout, stderr);

        try
        {
            if (input is not null) await process.StandardInput.WriteAsync(input.AsMemory(), stop.Token);
            process.StandardInput.Close();
            await completion.WaitAsync(stop.Token);
        }
        catch (Exception error)
        {
            try
            {
                // The process can exit between HasExited and Kill.
                try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
                catch (InvalidOperationException) when (process.HasExited) { }

                // This also bounds the wait for inherited pipe handles to close.
                await completion.WaitAsync(CleanupTimeout);
            }
            catch (Exception cleanupError)
            {
                readStop.Cancel();
                // A late fault must still be observed if cleanup could not finish.
                _ = completion.ContinueWith(t => _ = t.Exception,
                    CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
                throw new AggregateException("CLI execution and cleanup both failed.", error, cleanupError);
            }

            if (error is OperationCanceledException && cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException(cancellationToken);

            if (error is OperationCanceledException && deadline.IsCancellationRequested)
                return new CliResult(process.ExitCode, await stdout, await stderr, true);

            throw;
        }

        return new CliResult(process.ExitCode, await stdout, await stderr, false);
    }
}

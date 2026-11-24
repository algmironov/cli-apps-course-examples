using System.Diagnostics;
using Xunit;

namespace MyCli.Tests;

public sealed class ProcessTests
{
    [Fact]
    public async Task Help_ReturnsUsage()
    {
        var result = await CliProcessRunner.RunAsync(["--help"]);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("scan", result.StandardOutput);
        Assert.Equal("", result.StandardError);
    }

    [Fact]
    public async Task Echo_PreservesArgumentsWithSpaces()
    {
        var result = await CliProcessRunner.RunAsync(["echo", "--text", "hello world"]);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("hello world", result.StandardOutput.TrimEnd());
        Assert.Equal("", result.StandardError);
    }

    [Theory]
    [InlineData("echo")]
    [InlineData("unknown-command")]
    [InlineData("sleep", "--milliseconds", "oops")]
    public async Task InvalidArguments_ReturnError(params string[] args)
    {
        var result = await CliProcessRunner.RunAsync(args);
        Assert.Equal(1, result.ExitCode);
        Assert.NotEmpty(result.StandardError);
    }

    [Fact]
    public async Task Fail_WritesOnlyToStderr()
    {
        var result = await CliProcessRunner.RunAsync(["fail"]);
        Assert.Equal(2, result.ExitCode);
        Assert.Equal("", result.StandardOutput);
        Assert.Contains("Requested failure.", result.StandardError);
    }

    [Fact]
    public async Task Scan_UsesWorkingDirectoryAndPreservesFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), "cli test " + Guid.NewGuid().ToString("N"));
        var nested = Path.Combine(root, "path with spaces");
        var file = Path.Combine(nested, "sample.cs");
        var original = "first\n  \nsecond\n";
        try
        {
            Directory.CreateDirectory(nested);
            await File.WriteAllTextAsync(file, original);
            await File.WriteAllTextAsync(Path.Combine(nested, "ignore.txt"), "ignored");
            foreach (var path in new[] { "path with spaces", nested })
            {
                var result = await CliProcessRunner.RunAsync(["scan", "--path", path], root);
                Assert.Equal(0, result.ExitCode);
                Assert.Equal("2", result.StandardOutput.Trim());
                Assert.Equal("", result.StandardError);
            }
            Assert.Equal(original, await File.ReadAllTextAsync(file));
            Assert.Equal("ignored", await File.ReadAllTextAsync(Path.Combine(nested, "ignore.txt")));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public async Task Scan_MissingDirectory_ReturnsError()
    {
        var result = await CliProcessRunner.RunAsync(["scan", "--path", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())]);
        Assert.Equal(2, result.ExitCode);
        Assert.NotEmpty(result.StandardError);
    }

    [Fact]
    public async Task Read_ReceivesStandardInput()
    {
        var result = await CliProcessRunner.RunAsync(["read"], input: "hello input\n");
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("hello input", result.StandardOutput.TrimEnd());
    }

    [Fact]
    public async Task Sleep_Timeout_KillsProcess()
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await CliProcessRunner.RunAsync(["sleep", "--milliseconds", "30000"], timeout: TimeSpan.FromSeconds(1));
        stopwatch.Stop();
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(10), $"Elapsed: {stopwatch.Elapsed}");
        Assert.True(result.TimedOut);
        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    public async Task Sleep_ExternalCancellation_ThrowsOperationCanceledException()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var stopwatch = Stopwatch.StartNew();
        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CliProcessRunner.RunAsync(["sleep", "--milliseconds", "30000"],
                timeout: TimeSpan.FromSeconds(20), cancellationToken: cancellation.Token));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(10), $"Elapsed: {stopwatch.Elapsed}");
    }

    [Fact]
    public async Task Sleep_NegativeDuration_ReturnsError()
    {
        var result = await CliProcessRunner.RunAsync(["sleep", "--milliseconds", "-1"]);
        Assert.Equal(2, result.ExitCode);
        Assert.NotEmpty(result.StandardError);
    }

    [Fact]
    public async Task Streams_LargeOutputOnBothStreams_DoesNotDeadlock()
    {
        string[] args = ["streams", "--lines", "20000"];
        var result = await CliProcessRunner.RunAsync(args, timeout: TimeSpan.FromSeconds(30));
        Assert.False(result.TimedOut);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("diagnostic 19999", result.StandardError);
        Assert.Contains("output 19999", result.StandardOutput);
    }
}

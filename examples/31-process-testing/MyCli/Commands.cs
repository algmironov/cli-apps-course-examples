using System.CommandLine;

namespace MyCli;

internal static class Commands
{
    public static RootCommand Create()
    {
        var root = new RootCommand("A small CLI for process testing.");
        var path = new Option<string>("--path") { Required = true, Description = "Directory to scan." };
        var scan = new Command("scan", "Count non-empty lines in .cs files recursively.") { path };
        scan.SetAction((parse, token) => Handlers.ScanAsync(parse.GetValue(path)!, token));

        var text = new Option<string>("--text") { Required = true, Description = "Text to print." };
        var echo = new Command("echo", "Print text.") { text };
        echo.SetAction(parse => { Console.WriteLine(parse.GetValue(text)); return 0; });

        var fail = new Command("fail", "Return a predictable error.");
        fail.SetAction(_ => { Console.Error.WriteLine("Requested failure."); return 2; });

        var milliseconds = new Option<int>("--milliseconds") { Required = true, Description = "Non-negative delay in milliseconds." };
        var sleep = new Command("sleep", "Wait for a duration.") { milliseconds };
        sleep.SetAction((parse, token) => Handlers.SleepAsync(parse.GetValue(milliseconds), token));

        var read = new Command("read", "Read one line from standard input.");
        read.SetAction(async (_, token) => { var line = await Console.In.ReadLineAsync(token); Console.WriteLine(line ?? ""); return 0; });

        var lines = new Option<int>("--lines") { Required = true, Description = "Number of lines to write to both streams." };
        var streams = new Command("streams", "Write data and diagnostics to both streams.") { lines };
        streams.SetAction(parse =>
        {
            var count = parse.GetValue(lines);
            if (count < 0 || count > 100000) { Console.Error.WriteLine("Lines must be between 0 and 100000."); return 2; }
            for (var i = 0; i < count; i++) { Console.WriteLine($"output {i}"); Console.Error.WriteLine($"diagnostic {i}"); }
            return 0;
        });

        root.Add(streams); root.Add(scan); root.Add(echo); root.Add(fail); root.Add(sleep); root.Add(read);
        return root;
    }
}

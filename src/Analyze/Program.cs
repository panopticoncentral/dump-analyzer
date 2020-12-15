using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Globalization;

using Analyze;

using Spectre.Console;

var rootCommand = new RootCommand("Analyzes memory dumps.")
{
    new Option<string>(new string[] {"-p", "--path" }, "Path of the dump to analyze.")
};

static void Log(string message) => AnsiConsole.MarkupLine(CultureInfo.CurrentCulture, $"[grey][[{DateTime.Now}]][/] {message}");

rootCommand.Handler = CommandHandler.Create<string>(path =>
{
    var dump = Analyzer.Analyze(path);
    Log($"Dump path: '{path}'");
    Log($"Timestamp: {dump.Timestamp}");
    Log($"Flags: {dump.Type}");
    Log($"Architecture: {dump.Architecture}");
    Log($"Number of modules: {dump.Modules.Count}");
    Log($"Number of threads: {dump.Threads.Count}");
    Log($"Number of memory blocks: {dump.MemoryInfos.Count}");
});

return rootCommand.InvokeAsync(args).Result;

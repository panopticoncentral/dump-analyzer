using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Globalization;
using System.IO;

using Analyze;

using Spectre.Console;

var rootCommand = new RootCommand("Analyzes memory dumps.")
{
    new Option<string>(new string[] { "-p", "--path" }, "Path of the dump to analyze."),
    new Option<string>(new string[] { "-o", "--output" }, "File to output the analysis to."),
    new Option<bool>(new string[] { "-f", "--force" }, "Overwrite the analysis file if it exists.")
};

static void Log(string message) => AnsiConsole.MarkupLine(CultureInfo.CurrentCulture, $"[grey][[{DateTime.Now}]][/] {message}");

rootCommand.Handler = CommandHandler.Create<string, string, bool>((path, output, force) =>
{
    var dump = Analyzer.Analyze(path);
    Log($"Dump path: '{path}'");
    Log($"Timestamp: {dump.Timestamp}");
    Log($"Flags: {dump.Type}");
    Log($"Architecture: {dump.Architecture}");
    Log($"Number of modules: {dump.Modules.Count}");
    Log($"Number of threads: {dump.Threads.Count}");
    Log($"Number of memory blocks: {dump.MemoryInfos.Count}");

    if (string.IsNullOrEmpty(output))
    {
        output = Path.GetFileNameWithoutExtension(path);
    }

    if (Path.GetExtension(output) != ".dmpanalysis")
    {
        output = $"{output}.dmpanalysis";
    }

    if (File.Exists(output) && !force)
    {
        Log($"Analysis file '{output}' already exists.");
        return;
    }

    var serializer = new Serializer(output);
    serializer.Serialize(dump);
});

return rootCommand.InvokeAsync(args).Result;

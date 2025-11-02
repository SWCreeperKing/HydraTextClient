using System;
using System.Linq;
using ArchipelagoMultiTextClient.Scripts.Console;
using CreepyUtil.Archipelago;
using Godot;
using Godot.Collections;

public partial class AppLogger(LoggerLabel label) : Logger
{
    private LoggerLabel _Label = label;
    private LimitedQueue<string> _Messages = new(200);
    private const string BLOCK = "          ";

    public override void _LogError(string function, string file, int line, string code, string rationale,
        bool editorNotify, int errorType,
        Array<ScriptBacktrace> scriptBacktraces)
    {
        _LogMessage($"""
                     file [{file}] in func [{function}] on line [{line}], [{errorType}]
                     |{code}|
                     ={rationale}=
                     STACK TRACE
                     {string.Join("\n", scriptBacktraces.Select((s, i) => $"[{i}]: [{s.Format()}]"))}
                     """, true);
    }

    public override void _LogMessage(string message, bool error)
    {
        if (message.Length == 0) return;
        var timeStamp = DateTime.Now.ToString("[HH:mm:ss]");
        var split = message.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var text = $"[color=darkgray]{timeStamp}[/color] [color={(error ? "red" : "white")}]{split[0]}";

        if (split.Length > 1)
        {
            text += $"\n{BLOCK}{string.Join($"\n{BLOCK} ", split.Skip(1))}";
        }
        
        _Messages.Add($"{text}[/color]");
 
        _Label.RefreshUI = true;
    }

    public string[] Messages => _Messages.GetQueue.ToArray();
}
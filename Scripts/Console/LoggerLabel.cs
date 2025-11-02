using Godot;

namespace ArchipelagoMultiTextClient.Scripts.Console;

public partial class LoggerLabel :RichTextLabel
{
    [Export] public bool RefreshUI;
    public AppLogger Logger;

    public void Init()
    {
        Logger = new AppLogger(this);
        Logger._LogMessage("Logger Init", false);
    }

    public override void _Process(double delta)
    {
        if (!RefreshUI) return;
        Text = string.Join("\n", Logger.Messages);
        RefreshUI = false;
    }
}
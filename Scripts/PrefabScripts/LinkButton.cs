using Godot;

namespace ArchipelagoMultiTextClient.Scripts.PrefabScripts;

public partial class LinkButton : Button
{
    [Export] private string _Link;

    public override void _Ready() => Pressed += () => OS.ShellOpen(_Link);
}
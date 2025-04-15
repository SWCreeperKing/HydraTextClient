using Godot;
using System;
using ArchipelagoMultiTextClient.Scripts;
using CreepyUtil.Archipelago;

public partial class HintDialog : ConfirmationDialog
{
    public ApClient Client;
    private string _Item;

    public string Item
    {
        get => _Item;
        set => DialogText = $"Hint for item:\n{_Item = value}?";
    }

    public override void _Ready() => Confirmed += () =>
    {
        Client.Say($"!hint {_Item}");
        MainController.MoveToTab = 1;
    };
}
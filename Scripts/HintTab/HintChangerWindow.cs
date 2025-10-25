using System.Linq;
using Archipelago.MultiClient.Net.Enums;
using ArchipelagoMultiTextClient.Scripts.TextClientTab;
using Godot;
using static ArchipelagoMultiTextClient.Scripts.MainController;

namespace ArchipelagoMultiTextClient.Scripts.HintTab;

public partial class HintChangerWindow : Window
{
    [Export] private RichTextLabel _Label;
    [Export] private OptionButton _Options;
    [Export] private Button _Apply;
    [Export] private Button _Cancel;
    private int _CurrentSlot;
    private long _CurrentLocation;
    private int _PlayerSlot;

    public override void _Ready()
    {
        _Apply.Pressed += () =>
        {
            var client = ClientList[PlayerSlots[_CurrentSlot]].Client;
            client.UpdateHint(_PlayerSlot, _CurrentLocation, (HintStatus)_Options.GetSelectedId());
            Hide();
        };
        _Cancel.Pressed += Hide;
    }

    public void ShowWindow(int findingPlayer, int receiverPlayer, string item, string color, long location)
    {
        _Label.Text = $"Change hint status for [bgcolor=00000066][color={color}]{item.Clean()}[/color][/bgcolor]?";
        _CurrentSlot = receiverPlayer;
        _CurrentLocation = location;
        _Options.Selected = 0;
        _PlayerSlot = findingPlayer;
        Show();
    }
}
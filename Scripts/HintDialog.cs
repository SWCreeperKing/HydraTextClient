using Godot;
using System.Threading.Tasks;
using ArchipelagoMultiTextClient.Scripts;
using CreepyUtil.Archipelago;

public partial class HintDialog : ConfirmationDialog
{
    public ApClient Client;
    private string _Item;
    private string _Location;
    private bool _IsLocation;

    public string Item
    {
        get => _Item;
        set
        {
            _IsLocation = false;
            DialogText = $"Hint for item:\n{_Item = value}?";
        }
    }

    public string Location
    {
        get => _Location;
        set
        {
            _IsLocation = true;
            DialogText = $"Hint locations:\n{_Location = value}?";
        }
    }

    public override void _Ready()
        => Confirmed += () =>
        {
            Task.Delay(500).GetAwaiter().GetResult();
            Client.Say(_IsLocation ? $"!hint_location {_Location}" : $"!hint {_Item}");
            MainController.MoveToTab = 1;
        };
}
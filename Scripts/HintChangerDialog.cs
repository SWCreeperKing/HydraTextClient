using Godot;
using Archipelago.MultiClient.Net.Enums;
using CreepyUtil.Archipelago;
using static ArchipelagoMultiTextClient.Scripts.MainController;

public partial class HintChangerDialog : ConfirmationDialog
{
    public ApClient Client;
    private long _Location;
    private HintStatus _Status;
    public override void _Ready()
    {
        Confirmed += () =>
        {
            Client.UpdateHint(Client.PlayerSlot, _Location, _Status);
            RefreshUIColors();
        };
        Canceled += () => HintManager.RefreshUI = true;
    }

    public void SetItemText(string item, long location, HintStatus status)
    {
        _Location = location;
        DialogText = $"Hint Status for item:\n{item}\nto {HintStatusText[_Status = status]}?";
    }
}
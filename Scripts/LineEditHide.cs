using ArchipelagoMultiTextClient.Scripts;
using Godot;

public partial class LineEditHide : LineEdit
{
    [Export] private bool NumberOnly;
    [Export] private bool IsPort;
    private string LastText = "";

    public override void _Ready()
    {
        if (!NumberOnly) return;
        TextChanged += s =>
        {
            if (s.Trim() == "" || s.IsValidInt())
            {
                if (IsPort)
                {
                    MainController.Data.Port = int.TryParse(s.Trim(), out var port) ? port : 12345;
                    LastText = $"{MainController.Data.Port}";
                }
                else
                {
                    LastText = s.Trim();
                }

                return;
            }

            Text = LastText;
        };
    }

    public void TogglePassword(bool toggle) => Secret = toggle;
}
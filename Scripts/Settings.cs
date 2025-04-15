using ArchipelagoMultiTextClient.Scripts;
using Godot;

public partial class Settings : ScrollContainer
{
    [Export] private Font _Font;
    [Export] private VBoxContainer _ColorContainer;

    public override void _Ready()
    {
        foreach (var (key, setting) in MainController.Data.ColorSettings)
        {
            var box = new HBoxContainer();
            var label = new Label();
            var picker = new ColorPickerButton();
            _ColorContainer.AddChild(box);
            box.AddChild(picker);
            box.AddChild(label);

            box.AddThemeConstantOverride("separation", 15);
            label.Text = setting.SettingName;
            label.AddThemeFontOverride("font", _Font);
            label.AddThemeFontSizeOverride("font_size", 24);
            picker.Color = setting;
            picker.Text = "Color Picker";
            var locKey = key;
            picker.PopupClosed += () =>
            {
                MainController.Data.ColorSettings[locKey] =
                    new ColorSetting(MainController.Data.ColorSettings[locKey].SettingName, picker.Color);
                MainController.RefreshUIColors();
            };
        }
    }
}
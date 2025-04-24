using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

namespace ArchipelagoMultiTextClient.Scripts;

public partial class Settings : Control
{
    public static ItemFilterDialog ItemFilterDialog;
    
    [Export] private Font _Font;
    [Export] private VBoxContainer _ColorContainer;
    [Export] private Button _ExportColors;
    [Export] private Button _ImportColors;
    [Export] private ItemFilterDialog _ItemFilter;
    private List<ColorPickerButton> _Buttons = [];

    public override void _Ready()
    {
        ItemFilterDialog = _ItemFilter;
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
            label.AddThemeFontSizeOverride("font_size", 20);
            picker.Color = setting;
            picker.Text = "Color Picker";
            picker.Name = key;
            var locKey = key;
            picker.PopupClosed += () =>
            {
                MainController.Data[locKey] =
                    new ColorSetting(MainController.Data[locKey].SettingName, picker.Color);
                MainController.RefreshUIColors();
            };
            _Buttons.Add(picker);
        }

        _ExportColors.Pressed += ()
            => DisplayServer.ClipboardSet(JsonConvert.SerializeObject(MainController.Data.ColorSettings));
        _ImportColors.Pressed += () =>
        {
            try
            {
                var colors =
                    JsonConvert.DeserializeObject<Dictionary<string, ColorSetting>>(DisplayServer.ClipboardGet());
                MainController.Data.ColorSettings = colors;
                RefreshPickers();
            }
            catch
            {
                //ignored
            }
        };
    }

    public void RefreshPickers()
    {
        foreach (var picker in _Buttons)
        {
            picker.Color = MainController.Data[picker.Name];
        }
    }
}
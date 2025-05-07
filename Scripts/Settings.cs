using System.Collections.Generic;
using System.Linq;
using Godot;
using GodotPlugins.Game;
using Newtonsoft.Json;

namespace ArchipelagoMultiTextClient.Scripts;

public partial class Settings : Control
{
    public static ItemFilterDialog ItemFilterDialog;

    [Export] private Font _Font;
    [Export] private VBoxContainer _ColorContainer;
    [Export] private Button _ExportColors;
    [Export] private Button _ImportColors;
    [Export] private CheckBox _ShowFoundHints;
    [Export] private ItemFilterDialog _ItemFilter;
    private List<ColorPickerButton> _Buttons = [];

    public override void _Ready()
    {
        _ShowFoundHints.ButtonPressed = MainController.Data.ShowFoundHints;
        _ShowFoundHints.Pressed += () => MainController.Data.ShowFoundHints = _ShowFoundHints.ButtonPressed;
        
        ItemFilterDialog = _ItemFilter;
        foreach (var (key, setting) in MainController.Data.GetColors())
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

        _ExportColors.Pressed += () => DisplayServer.ClipboardSet(MainController.Data.Colors);
        _ImportColors.Pressed += () =>
        {
            MainController.Data.Colors = DisplayServer.ClipboardGet();
            MainController.RefreshUIColors();
            RefreshPickers();
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
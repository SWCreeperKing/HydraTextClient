using System.Collections.Generic;
using Godot;

namespace ArchipelagoMultiTextClient.Scripts.Settings;

public partial class Settings : Control
{
    public static ItemFilterDialog ItemFilterDialog;

    [Export] private Theme _Theme;
    [Export] private VBoxContainer _ColorContainer;
    [Export] private Button _ExportColors;
    [Export] private Button _ImportColors;
    [Export] private CheckBox _ShowFoundHints;
    [Export] private CheckBox _AlwaysOnTop;
    [Export] private ItemFilterDialog _ItemFilter;
    [Export] private SpinBox _GlobalUiSize;
    private List<ColorPickerButton> _Buttons = [];

    public override void _Ready()
    {
        _ShowFoundHints.ButtonPressed = MainController.Data.ShowFoundHints;
        _ShowFoundHints.Pressed += () =>
        {
            MainController.Data.ShowFoundHints = _ShowFoundHints.ButtonPressed;
            TextClientTab.TextClient.RefreshText = true;
        };

        _AlwaysOnTop.ButtonPressed = MainController.Data.AlwaysOnTop;
        _AlwaysOnTop.Pressed += () => MainController.SetAlwaysOnTop(_AlwaysOnTop.ButtonPressed);

        ItemFilterDialog = _ItemFilter;
        var order = DataConstant.DefaultDict.Keys;
        foreach (var key in order)
        {
            var setting = MainController.Data[key];
            var box = new HBoxContainer();
            var label = new Label();
            var picker = new ColorPickerButton();
            _ColorContainer.AddChild(box);
            box.AddChild(picker);
            box.AddChild(label);

            box.AddThemeConstantOverride("separation", 15);
            label.Text = setting.SettingName;
            label.Theme = _Theme;
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

        _GlobalUiSize.Value = MainController.Data.GlobalFontSize;
        _GlobalUiSize.ValueChanged += d => SetThemeFontSize((int)d);
    }

    public void RefreshPickers()
    {
        foreach (var picker in _Buttons)
        {
            picker.Color = MainController.Data[picker.Name];
        }
    }

    public void SetThemeFontSize(int newSize) => MainController.Data.GlobalFontSize = _Theme.DefaultFontSize = newSize;
}
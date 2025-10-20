using System.Collections.Generic;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Godot;
using static ArchipelagoMultiTextClient.Scripts.MainController;

namespace ArchipelagoMultiTextClient.Scripts.SettingsTab;

public partial class Settings : Control
{
    public static Extra.ConfirmationWindow ItemFilterDialog;

    [Export] private VBoxContainer _ColorContainer;
    [Export] private Button _ExportColors;
    [Export] private Button _ImportColors;
    [Export] private CheckBox _ShowFoundHints;
    [Export] private CheckBox _AlwaysOnTop;
    [Export] private CheckBox _ShowNewItems;
    [Export] private Extra.ConfirmationWindow _ItemFilter;
    [Export] private SpinBox _GlobalUiSize;
    private List<ColorPickerButton> _Buttons = [];

    public override void _Ready()
    {
        _ShowFoundHints.ButtonPressed = Data.ShowFoundHints;
        _ShowFoundHints.Pressed += () =>
        {
            Data.ShowFoundHints = _ShowFoundHints.ButtonPressed;
            TextClientTab.TextClient.RefreshText = true;
        };

        _AlwaysOnTop.ButtonPressed = Data.AlwaysOnTop;
        _AlwaysOnTop.Pressed += () => SetAlwaysOnTop(_AlwaysOnTop.ButtonPressed);
        
        _ShowNewItems.ButtonPressed = Data.ShowNewItems;
        _ShowNewItems.Pressed += () => Data.ShowNewItems = _ShowNewItems.ButtonPressed;

        ItemFilterDialog = _ItemFilter;
        var order = DataConstant.DefaultDict.Keys;
        foreach (var key in order)
        {
            var setting = Data[key];
            var box = new HBoxContainer();
            var label = new Label();
            var picker = new ColorPickerButton();
            _ColorContainer.AddChild(box);
            box.AddChild(picker);
            box.AddChild(label);

            box.AddThemeConstantOverride("separation", 15);
            label.Text = setting.SettingName;
            label.Theme = GlobalTheme;
            picker.Color = setting;
            picker.Text = "Color Picker";
            picker.Name = key;
            var locKey = key;
            picker.PopupClosed += () =>
            {
                Data[locKey] =
                    new ColorSetting(Data[locKey].SettingName, picker.Color);
                RefreshUIColors();
            };
            _Buttons.Add(picker);
        }

        _ExportColors.Pressed += () => DisplayServer.ClipboardSet(Data.Colors);
        _ImportColors.Pressed += () =>
        {
            Data.Colors = DisplayServer.ClipboardGet();
            RefreshUIColors();
            RefreshPickers();
        };

        _GlobalUiSize.Value = Data.GlobalFontSize;
        _GlobalUiSize.ValueChanged += d => SetThemeFontSize((int)d);
        SetThemeFontSize(Data.GlobalFontSize);
    }

    public void RefreshPickers()
    {
        foreach (var picker in _Buttons)
        {
            picker.Color = Data[picker.Name];
        }
    }

    public void SetThemeFontSize(int newSize) => Data.GlobalFontSize = GlobalTheme.DefaultFontSize = newSize;

    public static string GetMetaString(string itemName, string gameName, long itemId, ItemFlags flags)
        => $"itemdialog{itemName}&-&{gameName}&-&{itemId}&-&{(int)flags}".Replace("\"", "'");

    public static string GetMetaString(ItemInfo info)
        => $"itemdialog{info.ItemName}&-&{info.ItemGame}&-&{info.ItemId}&-&{(int)info.Flags}".Replace("\"", "'");

    public static void SetAndShowItemFilterDialogue(string itemName, string gameName, long itemId, ItemFlags flags)
    {
        var item = FormatItemColor(itemName, gameName, itemId, flags, false);
        ItemFilterDialog.SetAndShow("Add item to the Item Filter?",
            $"Add [{item}]\nfrom [{gameName}]\nto the Item Filter?",
            () =>
            {
                var filter = new ItemFilter(itemId, itemName, gameName, flags);
                Data.ItemFilters.Add(filter.UidCode, filter);
                ItemFilterer.RefreshUI = true;
            });
    }
    public static void SetAndShowItemFilterDialogue(string meta)
    {
        var split = meta[10..].Split("&-&");
        SetAndShowItemFilterDialogue(split[0], split[1], long.Parse(split[2]), (ItemFlags)int.Parse(split[3]));
    }
}
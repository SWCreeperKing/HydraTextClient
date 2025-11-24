using System;
using System.Linq;
using ArchipelagoMultiTextClient.Scripts.LoginTab;
using Godot;

namespace ArchipelagoMultiTextClient.Scripts.Extra;

public partial class GameConfigWindow : ConfirmationWindow
{
    [Export] private LineEdit _SlotName;
    [Export] private LineEdit _AlternateSlotName;
    [Export] private LineEdit _AlternatePassword;
    [Export] private OptionButton _GameImages;
    [Export] private Button _SetPackPath;
    [Export] private Label _PackPath;
    [Export] private LineEdit _StartCommandsEdit;
    [Export] private Button _StartCommandsAdd;
    [Export] private CheckBox _DeleteSlotCheck;
    [Export] private Button _DeleteSlotButton;
    private Action? _DeleteAction;

    public override void _Ready()
    {
        base._Ready();
        _DeleteSlotButton.Pressed += () => _DeleteAction?.Invoke();
        _DeleteSlotButton.Pressed += Hide;
        _DeleteSlotButton.Visible = false;

        _DeleteSlotCheck.Toggled += b => _DeleteSlotButton.Visible = b;
    }

    public void ShowConfig(global::LoginTab view, GamePortrait portrait = null)
    {
        _DeleteSlotCheck.SetPressed(false);
        _DeleteSlotCheck.Visible = portrait is not null;
        MainController.LoadGamePortraits();
        _GameImages.Clear();
        _GameImages.AddItem("Unknown");

        var games = MainController.GamePortraits.Keys.OrderBy(s => s).ToArray();
        // foreach (var (game, icon) in MainController.GamePortraits.OrderBy(kv => kv.Key))
        foreach (var game in games)
        {
            // _GameImages.AddIconItem(icon, game);
            _GameImages.AddItem(game);
        }

        if (portrait is not null && MainController.Data.GameData.TryGetValue(portrait.SlotName, out var locData))
        {
            _GameImages.Selected = Array.IndexOf(games, locData.GameName) + 1;
            _SlotName.Text = locData.SlotName;
        }
        else
        {
            _GameImages.Selected = 0;
            _SlotName.Text = "";
        }

        SetAndShow("Slot Edit/Creation", "", () =>
        {
            var selectedGameId = _GameImages.Selected;
            var slotName = _SlotName.Text;
            if (slotName == "" || (view.HasSlotName(slotName) && portrait!.SlotName != slotName)) return;

            if (portrait is null)
            {
                GameData newData = new(slotName)
                {
                    GameName = selectedGameId == 0 ? null : games[selectedGameId - 1]
                };

                MainController.Data.GameData[newData.SlotName] = newData;
                view.TryAddSlot(newData);
                return;
            }

            MainController.Data.GameData.Remove(portrait.SlotName, out var data);
            data ??= new GameData(slotName);
            data.GameName = selectedGameId == 0 ? null : games[selectedGameId - 1];
            data.SlotName = slotName;
            MainController.Data.GameData[data.SlotName] = data;

            portrait.UpdateFromGameData(data);
        });

        _DeleteAction = () =>
        {
            view.RemoveSlot(_SlotName.Text);
            MainController.Data.GameData.Remove(portrait.SlotName);
        };
    }
}
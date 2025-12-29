using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CreepyUtil.Archipelago.ApClient;
using Godot;

namespace ArchipelagoMultiTextClient.Scripts.UtilitiesTab;

public partial class HintManager : SplitContainer
{
    [Export] private VBoxContainer _HintSender;
    [Export] private VBoxContainer _HintLocationSender;
    [Export] private PackedScene _PlayerBox;
    [Export] private Extra.ConfirmationWindow _SendHintConfirmation;
    private Dictionary<ApClient, PlayerBox> _HintSenderBoxes = [];
    private Dictionary<ApClient, PlayerBox> _HintLocationSenderBoxes = [];
    private Dictionary<int, Dictionary<string, Button>> _LocationButtons = [];

    public static event Action<int, string[]>? LocationChangeEvent;

    public void RegisterPlayer(ApClient client)
    {
        var items = client.Items.Select(kv => kv.Key);
        var locations = client.Locations
                              .Where(kv => client.MissingLocations.Contains(kv.Key));

        SetupBoxes(items, client, _HintSenderBoxes, false, _HintSender);
        SetupBoxes(locations.Select(kv => kv.Key), client, _HintLocationSenderBoxes, true, _HintLocationSender);
    }

    public void SetupBoxes(IEnumerable<string> arr, ApClient client, Dictionary<ApClient, PlayerBox> dict,
        bool locations, VBoxContainer parent)
    {
        var playerBox = (PlayerBox)_PlayerBox.Instantiate();
        playerBox.PlayerName = client.PlayerName;
        var playerButtons = _LocationButtons[client.PlayerSlot] = new Dictionary<string, Button>();

        List<Button> buttons = [];
        foreach (var item in arr.Order())
        {
            Button hintButton = new();
            hintButton.Theme = MainController.GlobalTheme;
            hintButton.AddThemeFontSizeOverride("font_size", 18);
            if (locations)
            {
                hintButton.Pressed += () => HintLocation(item, client);
                playerButtons.Add(item, hintButton);
            }
            else hintButton.Pressed += () => HintItem(item, client);

            hintButton.Text = item;

            playerBox.AddNode(hintButton, true);
            buttons.Add(hintButton);
        }

        LineEdit searchBar = new();
        searchBar.Theme = MainController.GlobalTheme;
        searchBar.AddThemeFontSizeOverride("font_size", 24);
        searchBar.PlaceholderText = "Search Items";
        searchBar.TextChanged += text =>
        {
            var split = text.Split(" ");
            FilterButtons(buttons, s => split.All(word => s.Contains(word, StringComparison.CurrentCultureIgnoreCase)));
        };

        HBoxContainer filterContainer = new();
        Label text = new() { Theme = MainController.GlobalTheme, Text = "Show items that you don't have any of: " };
        CheckBox showIfNotAny = new() { Theme = MainController.GlobalTheme };
        showIfNotAny.Pressed += () =>
        {
            if (!showIfNotAny.ButtonPressed)
            {
                FilterButtons(buttons, _ => true);
                return;
            }

            var inventory = InventoryManager.Inventories[client.PlayerName];
            FilterButtons(buttons, s => inventory.Items.All(info => info.ItemName != s));
        };

        filterContainer.AddChild(text);
        filterContainer.AddChild(showIfNotAny);

        playerBox.AddNode(searchBar, false);
        playerBox.AddNode(filterContainer, false);

        parent.AddChild(playerBox);
        dict.Add(client, playerBox);
    }

    public void FilterButtons(List<Button> buttons, Func<string, bool> filter)
    {
        foreach (var button in buttons)
        {
            try
            {
                button.Visible = filter(button.Text);
            }
            catch (Exception e)
            {
                GD.PrintErr(e);
            }
        }
    }

    public void UnregisterPlayer(ApClient client)
    {
        var playerBox = _HintSenderBoxes[client];
        var playerBox2 = _HintLocationSenderBoxes[client];
        _HintSender.RemoveChild(playerBox);
        _HintLocationSender.RemoveChild(playerBox2);
        playerBox.QueueFree();
        playerBox2.QueueFree();
        _HintSenderBoxes.Remove(client);
        _HintLocationSenderBoxes.Remove(client);
        _LocationButtons.Remove(client.PlayerSlot);
    }

    public void HintLocation(string location, ApClient client)
    {
        _SendHintConfirmation.SetAndShow("Request a Hint?", $"Hint to see what item is at:\n[{location}]?", () =>
        {
            Task.Delay(300).GetAwaiter().GetResult();
            client.Say($"!hint_location {location}");
            MainController.MoveToTab = 1;
        });
    }

    public void HintItem(string item, ApClient client)
    {
        _SendHintConfirmation.SetAndShow("Request a Hint?", $"Hint to see where the following item is?\n[{item}]", () =>
        {
            Task.Delay(300).GetAwaiter().GetResult();
            client.Say($"!hint {item}");
            MainController.MoveToTab = 1;
        });
    }

    public void LocationCheck(long[] newLocations, int playerSlot)
    {
        var found = newLocations.Select(l => MainController.LocationIdToLocationName(l, playerSlot)).ToArray();
        foreach (var item in found)
        {
            CallDeferred("RemoveButton", item, playerSlot);
        }

        LocationChangeEvent?.Invoke(playerSlot, found);
    }

    public void RemoveButton(string buttonName, int slot)
    {
        if (!_LocationButtons[slot].TryGetValue(buttonName, out var button)) return;

        _LocationButtons[slot].Remove(buttonName);
        button.GetParent().RemoveChild(button);
        button.QueueFree();
    }
}
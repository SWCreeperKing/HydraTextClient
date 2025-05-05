using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.DataPackage;
using Archipelago.MultiClient.Net.Helpers;
using CreepyUtil.Archipelago;
using Godot;

namespace ArchipelagoMultiTextClient.Scripts;

public partial class HintManager : MarginContainer
{
    [Export] private VBoxContainer _HintSender;
    [Export] private VBoxContainer _HintLocationSender;
    [Export] private PackedScene _PlayerBox;
    [Export] private HintDialog _SendHintConfirmation;
    private Dictionary<ApClient, PlayerBox> _HintSenderBoxes = [];
    private Dictionary<ApClient, PlayerBox> _HintLocationSenderBoxes = [];
    private Dictionary<string, Button> _LocationButtons = [];

    public void RegisterPlayer(ApClient client)
    {
        var itemReceivedResolver = (ReceivedItemsHelper)client.Session.Items;
        var itemResolver = (ItemInfoResolver)itemReceivedResolver.itemInfoResolver;
        var cache = ((DataPackageCache)itemResolver.cache).inMemoryCache.ToDictionary(kv => kv.Key,
            kv => (GameDataLookup)kv.Value);
        var largeCache = cache[client.PlayerGames[client.PlayerSlot]];
        var items = largeCache.Items.Select(kv => kv.Key);
        var locations = largeCache.Locations
                                  .Where(kv => client.Session.Locations.AllMissingLocations.Contains(kv.Value));
        SetupBoxes(items, client, _HintSenderBoxes, false, _HintSender);
        SetupBoxes(locations.Select(kv => kv.Key), client, _HintLocationSenderBoxes, true, _HintLocationSender);
    }

    public void SetupBoxes(IEnumerable<string> arr, ApClient client, Dictionary<ApClient, PlayerBox> dict,
        bool locations, VBoxContainer parent)
    {
        var playerBox = (PlayerBox)_PlayerBox.Instantiate();
        playerBox.PlayerName = client.PlayerName;

        List<Button> buttons = [];
        foreach (var item in arr.Order())
        {
            Button hintButton = new();
            hintButton.Theme = MainController.GlobalTheme;
            hintButton.AddThemeFontSizeOverride("font_size", 18);
            if (locations)
            {
                hintButton.Pressed += () => HintLocation(item, client);
                _LocationButtons.Add(item, hintButton);
            }
            else
            {
                hintButton.Pressed += () => HintItem(item, client);
            }

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
            Queue<Button> toRemove = [];
            foreach (var button in buttons)
            {
                try
                {
                    button.Visible =
                        split.All(word => button.Text.Contains(word, StringComparison.CurrentCultureIgnoreCase));
                }
                catch
                {
                    toRemove.Enqueue(button);
                }
            }

            if (toRemove.Count == 0) return;
            foreach (var button in toRemove)
            {
                buttons.Remove(button);
            }
        };

        playerBox.AddNode(searchBar, false);

        parent.AddChild(playerBox);
        dict.Add(client, playerBox);
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
    }

    public void HintLocation(string location, ApClient client)
    {
        _SendHintConfirmation.Client = client;
        _SendHintConfirmation.Location = location;
        _SendHintConfirmation.Show();
    }

    public void HintItem(string item, ApClient client)
    {
        _SendHintConfirmation.Client = client;
        _SendHintConfirmation.Item = item;
        _SendHintConfirmation.Show();
    }

    public void LocationCheck(long[] newLocations, int playerSlot)
    {
        var found = newLocations.Select(l => MainController.LocationIdToLocationName(l, playerSlot));
        foreach (var (key, button) in _LocationButtons.Where(kv => found.Contains(kv.Key)))
        {
            _LocationButtons.Remove(key);

            button.GetParent().RemoveChild(button);
            button.QueueFree();
        }
    }
}
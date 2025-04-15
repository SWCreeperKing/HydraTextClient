using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.DataPackage;
using Archipelago.MultiClient.Net.Helpers;
using ArchipelagoMultiTextClient.Scripts;
using CreepyUtil.Archipelago;
using Godot;

public partial class HintManager : MarginContainer
{
    [Export] private VBoxContainer _HintSender;
    [Export] private PackedScene _PlayerBox;
    [Export] private HintDialog _SendHintConfirmation;
    private Dictionary<ApClient, PlayerBox> _HintSenderBoxes = [];
    
    public void RegisterPlayer(ApClient client)
    {
        var itemReceivedResolver = (ReceivedItemsHelper)client.Session.Items;
        var itemResolver = (ItemInfoResolver)itemReceivedResolver.itemInfoResolver;
        var cache = ((DataPackageCache)itemResolver.cache).inMemoryCache.ToDictionary(kv => kv.Key,
            kv => (GameDataLookup)kv.Value);
        var items = cache[client.PlayerGames[client.PlayerSlot]].Items.Select(kv => kv.Key);
        var playerBox = (PlayerBox)_PlayerBox.Instantiate();
        playerBox.PlayerName = client.PlayerName;

        List<Button> buttons = [];
        foreach (var item in items.Order())
        {
            Button hintButton = new();
            hintButton.AddThemeFontOverride("font", MainController.Font);
            hintButton.AddThemeFontSizeOverride("font_size", 18);
            hintButton.Pressed += () => HintItem(item, client);
            hintButton.Text = item;

            playerBox.AddNode(hintButton, true);
            buttons.Add(hintButton);
        }

        LineEdit searchBar = new();
        searchBar.AddThemeFontOverride("font", MainController.Font);
        searchBar.AddThemeFontSizeOverride("font_size", 24);
        searchBar.PlaceholderText = "Search Items";
        searchBar.TextChanged += text =>
        {
            var split = text.Split(" ");
            foreach (var button in buttons)
            {
                button.Visible = split.All(word => button.Text.Contains(word, StringComparison.CurrentCultureIgnoreCase));
            }
        };

        playerBox.AddNode(searchBar, false);

        _HintSender.AddChild(playerBox);
        _HintSenderBoxes.Add(client, playerBox);
    }

    public void UnregisterPlayer(ApClient client)
    {
        var playerBox = _HintSenderBoxes[client];
        _HintSender.RemoveChild(playerBox);
        playerBox.QueueFree();
        _HintSenderBoxes.Remove(client);
    }

    public void HintItem(string item, ApClient client)
    {
        _SendHintConfirmation.Client = client;
        _SendHintConfirmation.Item = item;
        _SendHintConfirmation.Show();
    }
}
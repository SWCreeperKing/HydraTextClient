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
    public static bool RefreshUI;
    
    [Export] private VBoxContainer _HintSender;
    [Export] private VBoxContainer _HintManager;
    [Export] private PackedScene _PlayerBox;
    [Export] private PackedScene _HintChangerBox;
    [Export] private HintDialog _SendHintConfirmation;
    [Export] private HintChangerDialog _HintChangeConfirmation;
    private Dictionary<ApClient, PlayerBox> _HintSenderBoxes = [];
    private Dictionary<ApClient, HintStatusChanger> _HintChangerBoxes = [];
    
    public override void _Process(double delta)
    {
        if (!RefreshUI) return;

        foreach (var changer in _HintChangerBoxes.Values)
        {
            changer.RefreshUI = true;
        }
        
        RefreshUI = false;
    }

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
            foreach (var button in buttons)
            {
                button.Visible = button.Text.Contains(text, StringComparison.CurrentCultureIgnoreCase);
            }
        };

        playerBox.AddNode(searchBar, false);

        _HintSender.AddChild(playerBox);
        _HintSenderBoxes.Add(client, playerBox);

        var hintChangerBox = (HintStatusChanger)_HintChangerBox.Instantiate();
        hintChangerBox.Init(client, _HintChangeConfirmation);
        _HintManager.AddChild(hintChangerBox);
        _HintChangerBoxes.Add(client, hintChangerBox);
    }

    public void UnregisterPlayer(ApClient client)
    {
        var playerBox = _HintSenderBoxes[client];
        _HintSender.RemoveChild(playerBox);
        playerBox.QueueFree();
        _HintSenderBoxes.Remove(client);

        var hintChanger = _HintChangerBoxes[client];
        _HintManager.RemoveChild(hintChanger);
        hintChanger.QueueFree();
        _HintChangerBoxes.Remove(client);
    }

    public void HintItem(string item, ApClient client)
    {
        _SendHintConfirmation.Client = client;
        _SendHintConfirmation.Item = item;
        _SendHintConfirmation.Show();
    }
}
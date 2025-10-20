using System.Collections.Concurrent;
using System.Collections.Generic;
using Archipelago.MultiClient.Net.Models;
using Godot;

namespace ArchipelagoMultiTextClient.Scripts.UtilitiesTab;

public partial class InventoryManager : Control
{
    public static bool RefreshUI = false;
    private static Dictionary<string, Inventory> Inventories = [];
    private static ConcurrentQueue<string> AwaitingInventories = [];
    private static ConcurrentQueue<string> RemovingInventories = [];
    private static ConcurrentQueue<(string, ItemInfo[], bool)> AwaitingItems = [];

    public override void _Process(double delta)
    {
        while (!AwaitingInventories.IsEmpty)
        {
            AwaitingInventories.TryDequeue(out var playerName);
            Inventory inventory = new();
            inventory._Columns = ["Count", "Items"];
            inventory.Theme = MainController.GlobalTheme;

            FoldableContainer foldContainer = new();
            foldContainer.Theme = MainController.GlobalTheme;
            foldContainer.Title = playerName;
            foldContainer.AddChild(inventory);

            Inventories[playerName] = inventory;
            AddChild(foldContainer);
        }

        while (!RemovingInventories.IsEmpty)
        {
            RemovingInventories.TryDequeue(out var playerName);
            RemoveChild(Inventories[playerName].GetParent());
            Inventories.Remove(playerName);
        }

        while (!AwaitingItems.IsEmpty)
        {
            AwaitingItems.TryDequeue(out var tuple);
            var (playerName, items, firstSend) = tuple;
            Inventories[playerName].AddItems(items, firstSend);
            if (firstSend) GD.Print($"gained [{items.Length}]");
        }

        if (!RefreshUI) return;
        RefreshUI = false;
        foreach (var (_, inv) in Inventories)
        {
            inv.RefreshUI = true;
        }
    }

    public static void AddItems(string player, ItemInfo[] items, bool firstSend) => AwaitingItems.Enqueue((player, items, firstSend));

    public static void AddInventory(string player) => AwaitingInventories.Enqueue(player);

    public static void RemoveInventory(string player) => RemovingInventories.Enqueue(player);
}
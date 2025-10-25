using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Models;
using ArchipelagoMultiTextClient.Scripts.Extra;
using ArchipelagoMultiTextClient.Scripts.LoginTab;
using Godot;

namespace ArchipelagoMultiTextClient.Scripts.UtilitiesTab;

public partial class InventoryManager : Control
{
    public static ItemWindow ItemWindow;
    
    [Export] private ItemWindow _ItemWindow;
    public static bool RefreshUI = false;
    private static Dictionary<string, Inventory> Inventories = [];
    private static ConcurrentQueue<string> AwaitingInventories = [];
    private static ConcurrentQueue<string> RemovingInventories = [];
    private static ConcurrentQueue<(string, ItemInfo[], bool)> AwaitingItems = [];

    public override void _Ready()
    {
        ItemWindow = _ItemWindow;
    }

    public override void _Process(double delta)
    {
        while (!AwaitingInventories.IsEmpty)
        {
            AwaitingInventories.TryDequeue(out var playerName);
            Inventory inventory = new();
            inventory._Columns = ["Count", "Items"];
            inventory.Theme = MainController.GlobalTheme;
            
            Button button = new();
            button.Text = "View History";
            button.Theme = MainController.GlobalTheme;
            button.Pressed += () => ItemWindow.SetAndShowItemHistory($"Item history for [{playerName}]", inventory.Items.ToArray());

            Label label = new();
            label.Theme = MainController.GlobalTheme;
            inventory.CheatedLabel = label;
            
            VBoxContainer vBox = new();
            vBox.AddChild(button);
            vBox.AddChild(label);
            vBox.AddChild(inventory);

            FoldableContainer foldContainer = new();
            foldContainer.Theme = MainController.GlobalTheme;
            foldContainer.Title = playerName;
            foldContainer.AddChild(vBox);

            Inventories[playerName] = inventory;
            AddChild(foldContainer);
        }

        while (!RemovingInventories.IsEmpty)
        {
            RemovingInventories.TryDequeue(out var playerName);
            RemoveChild(Inventories[playerName].GetParent().GetParent());
            Inventories.Remove(playerName);
        }

        while (!AwaitingItems.IsEmpty)
        {
            AwaitingItems.TryDequeue(out var tuple);
            var (playerName, items, firstSend) = tuple;
            if (MultiworldName.CurrentWorld is not null)
            {
                if (firstSend)
                {
                    var remainder = items.Skip(MultiworldName.CurrentWorld.GetLastItemCount(playerName)).ToArray();
                    if (remainder.Length != 0)
                    {
                        if (MainController.Data.ShowNewItems)
                        {
                            _ItemWindow.SetAndShowItems($"New items for [{playerName}]", remainder);
                        }

                        MultiworldName.CurrentWorld.PreviousInventoryCount[playerName] += remainder.Length;
                    }
                }
                else
                {
                    MultiworldName.CurrentWorld.PreviousInventoryCount[playerName] += items.Length;
                }
            }

            Inventories[playerName].AddItems(items);
        }

        if (!RefreshUI) return;
        RefreshUI = false;
        foreach (var (_, inv) in Inventories)
        {
            inv.RefreshUI = true;
        }
    }

    public static void AddItems(string player, ItemInfo[] items, bool firstSend)
        => AwaitingItems.Enqueue((player, items, firstSend));

    public static void AddInventory(string player) => AwaitingInventories.Enqueue(player);

    public static void RemoveInventory(string player) => RemovingInventories.Enqueue(player);
}
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArchipelagoMultiTextClient.Scripts.TextClientTab;
using Godot;

namespace ArchipelagoMultiTextClient.Scripts.UtilitiesTab;

public partial class LocationTree : Tree
{
    [Export] private Label CurrentClient;
    public static ConcurrentDictionary<string, List<TreeItem>> Locations = [];
    public bool Running = false;

    public override void _Ready()
    {
        HideRoot = true;
        HintManager.LocationChangeEvent += LocationCheck;
        TextClient.SelectedClientChangedEvent += _ => LocationCheck();
    }


    public void LoadList(string file)
    {
        Locations.Clear();
        Clear();
        var list = File.ReadAllText(file).Replace("\r", "").Split("\n");
        Stack<TreeItem> items = [];
        items.Push(CreateItem());
        var lastNodeWasNote = false;
        var lastNodeWasLocation = false;
        foreach (var rawLine in list)
        {
            var line = rawLine.Trim();
            var split = line.Split(' ');
            switch (split[0].ToLower())
            {
                case "closefolder":
                    if (lastNodeWasNote) items.Pop();
                    if (lastNodeWasLocation) items.Pop();
                    lastNodeWasLocation = lastNodeWasNote = false;
                    if (items.Count == 1) break;
                    items.Pop();
                    break;
                case "openfolder" when split.Length > 1:
                    if (lastNodeWasNote) items.Pop();
                    if (lastNodeWasLocation) items.Pop();
                    lastNodeWasLocation = lastNodeWasNote = false;
                    var folder = CreateItem(items.Peek());
                    folder.SetText(0, string.Join(' ', split[1..]));
                    folder.SetCustomColor(0, Colors.White);
                    items.Push(folder);
                    break;
                case "location" when split.Length > 1:
                    if (lastNodeWasNote) items.Pop();
                    if (lastNodeWasLocation) items.Pop();
                    var location = string.Join(' ', split[1..]);
                    var locationItem = CreateItem(items.Peek());
                    locationItem.SetText(0, location);
                    locationItem.SetCustomColor(0, Colors.Red);
                    if (!Locations.ContainsKey(location)) Locations[location] = [];
                    Locations[location].Add(locationItem);
                    items.Push(locationItem);
                    lastNodeWasNote = false;
                    lastNodeWasLocation = true;
                    break;
                case "note" when lastNodeWasLocation && split.Length > 1:
                    var noteItem = CreateItem(items.Peek());
                    noteItem.SetText(0, string.Join(' ', split[1..]));
                    noteItem.SetAutowrapMode(0, TextServer.AutowrapMode.WordSmart);
                    items.Push(noteItem);
                    lastNodeWasNote = true;
                    break;
                default:
                    if (!lastNodeWasNote) break;
                    var note = items.Peek();
                    note.SetText(0, $"{note.GetText(0)}\n{line}");
                    break;
            }
        }
        Running = true;
        LocationCheck();
    }

    public void LocationCheck(int slot, string[] newLocations)
    {
        if (!Running) return;
        var chosen = MainController.ChosenTextClient;
        if (chosen.PlayerSlot != slot) return;
        LocationCheck(newLocations);
    }
    
    public void LocationCheck(params string[] newLocations)
    {
        if (!Running) return;
        var chosen = MainController.ChosenTextClient;
        
        CurrentClient.Text = $"Current Client: [{chosen.PlayerName}]";

        foreach (var (loc, items) in Locations)
        {
            foreach (var item in items)
            {
                // var locMissing = missingNormal.Contains(loc) || missingDisplay.Contains(loc);
                // if (newLocations.Contains(loc)) locMissing = false;
                // if (newLocations.Contains(loc)) missing = false;
                // GD.Print($"[{loc}] [{missingNormal.Contains(loc)}] [{missingDisplay.Contains(loc)}] [{newLocations.Contains(loc)}]");
                // GD.Print($"[{loc}] [{missing.Contains(loc)}] [{newLocations.Contains(loc)}]");
                item.SetCustomColor(0, chosen.MissingLocations.Contains(loc) && !newLocations.Contains(loc) ? Colors.Red : Colors.Green);
            }
        }
    }
}
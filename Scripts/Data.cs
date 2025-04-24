using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Archipelago.MultiClient.Net.Enums;
using Godot;
using static ArchipelagoMultiTextClient.Scripts.DataConstant;

namespace ArchipelagoMultiTextClient.Scripts;

file class DataConstant
{
    public static ReadOnlyDictionary<string, ColorSetting> DefaultDict = new(new Dictionary<string, ColorSetting>
    {
        ["background_color"] = new("UI Background", new Color(0.104f, 0.157f, 0.18f)),
        ["player_server"] = new("Player (Server)", Colors.Yellow),
        ["player_generic"] = new("Player (Generic)", Colors.Beige),
        ["player_color"] = new("Player (Connected To)", new Color(0.89f, 0.01f, 0.89f)),
        ["item_progressive"] = new("Item (Progressive)", Colors.Gold),
        ["item_useful"] = new("Item (Useful)", Colors.Teal),
        ["item_normal"] = new("Item (Normal)", Colors.Beige),
        ["item_trap"] = new("Item (Trap)", Colors.OrangeRed),
        ["location"] = new("Location", Colors.Green),
        ["entrance"] = new("Entrance", new Color(0.39f, 0.58f, 0.91f)),
        ["entrance_vanilla"] = new("Entrance (Vanilla)", Colors.Beige),
        ["hint_found"] = new("Hint (Found)", Colors.Green),
        ["hint_priority"] = new("Hint (Priority)", Colors.Purple),
        ["hint_unspecified"] = new("Hint (Unspecified)", Colors.Beige),
        ["hint_no_priority"] = new("Hint (No Priority)", Colors.SkyBlue),
        ["hint_avoid"] = new("Hint (Avoid)", Colors.OrangeRed),
        ["connection_goal"] = new("Connection (Goal)", Colors.Gold),
        ["connection_playing"] = new("Connection (Playing)", Colors.Green),
        ["connection_ready"] = new("Connection (Ready)", Colors.Azure),
        ["connection_connected"] = new("Connection (Connected)", Colors.Teal),
        ["connection_disconnected"] = new("Connection (Disconnected)", Colors.OrangeRed),
        ["tooltip_bgcolor"] = new("Tooltip Background Color", new Color(0.055f, 0.063f, 0.071f, 0.749f))
    });
}

public class Data
{
    public string Address = "archipelago.gg";
    public string Password;
    public int Port = 12345;
    public List<string> SlotNames = [];

    public Dictionary<string, ColorSetting> ColorSettings = new(DefaultDict);

    public Dictionary<string, ItemFilter> ItemFilters = [];
    public Dictionary<string, int> FontSizes = [];
    public List<SortObject> HintSortOrder = [];
    public bool[] HintOptions = [false, true, true, true, true];
    public bool[] ItemLogOptions = [true, true, true, true];
    public long WordWrap = 0;
    public long Content = 0;

    public ColorSetting this[string colorId]
    {
        get => ColorSettings.GetValueOrDefault(colorId, DefaultDict[colorId]);
        set => ColorSettings[colorId] = value;
    }
}

public readonly struct ColorSetting(string settingName, Color color)
{
    public readonly string SettingName = settingName;
    public readonly Color Color = color;
    public readonly string Hex = color.ToHtml();
    public static implicit operator Color(ColorSetting setting) => setting.Color;
    public static implicit operator string(ColorSetting setting) => setting.Hex;
}

public class ItemFilter(long id, string name, string game, ItemFlags flags)
{
    public readonly long Id = id;
    public readonly string Name = name;
    public readonly string Game = game;
    public readonly ItemFlags Flags = flags;
    public readonly string UidCode = MakeUidCode(id, name, game, flags); 
    public bool ShowInItemLog = true;
    public bool ShowInHintsTable = true;

    public static string MakeUidCode(long id, string name, string game, ItemFlags flags) => $"{id}{name}{game}{flags}";
}
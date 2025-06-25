using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Archipelago.MultiClient.Net.Enums;
using ArchipelagoMultiTextClient.Scripts.Tables;
using Godot;
using static ArchipelagoMultiTextClient.Scripts.DataConstant;

namespace ArchipelagoMultiTextClient.Scripts;

public class DataConstant
{
    public static ReadOnlyDictionary<string, ColorSetting> DefaultDict = new(new Dictionary<string, ColorSetting>
    {
        ["background_color"] = new("UI Background", new Color(0.104f, 0.157f, 0.18f)),
        ["player_server"] = new("Player (Server)", Colors.Yellow),
        ["player_generic"] = new("Player (Generic)", Colors.Beige),
        ["player_color"] = new("Player (Connected To)", new Color(0.89f, 0.01f, 0.89f)),
        ["player_color_offline"] = new("Player (Not Connected To)", new Color(0.89f, 0.01f, 0.89f)),
        ["item_special"] = new("Item (Special)", Colors.ForestGreen),
        ["item_progressive"] = new("Item (Progressive)", Colors.Gold),
        ["item_useful"] = new("Item (Useful)", Colors.Teal),
        ["item_normal"] = new("Item (Normal)", Colors.Beige),
        ["item_trap"] = new("Item (Trap)", Colors.OrangeRed),
        ["item_bg_special"] = new("Item Background (Special)", Colors.Transparent),
        ["item_bg_progressive"] = new("Item Background (Progressive)", Colors.Transparent),
        ["item_bg_useful"] = new("Item Background (Useful)", Colors.Transparent),
        ["item_bg_normal"] = new("Item Background (Normal)", Colors.Transparent),
        ["item_bg_trap"] = new("Item Background (Trap)", Colors.Transparent),
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

public class UserData
{
    private Dictionary<string, ColorSetting> _UiColorSettings = new(DefaultDict);

    public string Address = "archipelago.gg";
    public string Password;
    public int Port = 12345;
    public List<string> SlotNames = [];
    public Dictionary<string, ItemFilter> ItemFilters = [];
    public Dictionary<string, int> FontSizes = [];
    public List<SortObject> HintSortOrder = [];
    public bool[] HintOptions = [false, true, true, true, true];
    public bool[] ItemLogOptions = [true, true, true, true, true];
    public bool ShowFoundHints = false;
    public bool AlwaysOnTop = false;
    public bool ClearTextWhenDisconnect = true;
    public long WordWrap = 3;
    public long AliasDisplay = 0;
    public long Content = 2;
    public long ItemLogStyle = 1;
    public int TextClientLineSeparation = 0;
    public int GlobalFontSize = 20;
    public Vector2I WindowSize = new(1152, 648);
    public Vector2I? WindowPosition = null;
    public Dictionary<string, float> UiSettingsSave = new();

    public string Colors
    {
        get => string.Join(";;;", _UiColorSettings.Select(kv => $"{kv.Key}==={kv.Value.SettingName}==={kv.Value.Hex}"));
        set
        {
            var raw = value
                     .Split(";;;")
                     .Select(item => item.Split("==="))
                     .ToDictionary(item => item[0], item => new ColorSetting(item[1], new Color(item[2])));

            if (raw is null || raw.Count == 0) return;

            foreach (var key in _UiColorSettings.Keys.Where(key => !raw.ContainsKey(key)))
            {
                raw.Add(key, _UiColorSettings[key]);
            }

            _UiColorSettings = raw;
        }
    }

    public Dictionary<string, ColorSetting> ColorSettings // backwards compatability
    {
        set => _UiColorSettings = value;
    }

    public void NullCheck()
    {
        if (_UiColorSettings is not null) return;
        _UiColorSettings = new Dictionary<string, ColorSetting>(DefaultDict);
    }

    public ColorSetting this[string colorId]
    {
        get => _UiColorSettings.GetValueOrDefault(colorId, DefaultDict[colorId]);
        set => _UiColorSettings[colorId] = value;
    }

    public Dictionary<string, ColorSetting> GetColors() => _UiColorSettings;
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
    public bool IsSpecial = false;

    public static string MakeUidCode(long id, string name, string game, ItemFlags flags) => $"{id}{name}{game}{flags}";
}
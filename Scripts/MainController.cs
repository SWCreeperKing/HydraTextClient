using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using ArchipelagoMultiTextClient.Scripts.Console;
using ArchipelagoMultiTextClient.Scripts.HintTab;
using ArchipelagoMultiTextClient.Scripts.LoginTab;
using ArchipelagoMultiTextClient.Scripts.SettingsTab;
using ArchipelagoMultiTextClient.Scripts.TextClientTab;
using ArchipelagoMultiTextClient.Scripts.UtilitiesTab;
using CreepyUtil.Archipelago;
using CreepyUtil.Archipelago.ApClient;
using CreepyUtil.DiscordRpc;
using Godot;
using Newtonsoft.Json;
using static Archipelago.MultiClient.Net.Enums.HintStatus;
using static Archipelago.MultiClient.Net.Enums.ItemFlags;
using static ArchipelagoMultiTextClient.Scripts.PlayerTable;
using Environment = System.Environment;
using HintTable = ArchipelagoMultiTextClient.Scripts.HintTab.HintTable;
using ItemFilterer = ArchipelagoMultiTextClient.Scripts.SettingsTab.ItemFilterer;
using MultiworldName = ArchipelagoMultiTextClient.Scripts.LoginTab.MultiworldName;
using SlotClient = ArchipelagoMultiTextClient.Scripts.LoginTab.SlotClient;

namespace ArchipelagoMultiTextClient.Scripts;

public partial class MainController : Control
{
    public static MainController Main;

    public static string SaveDir =
        $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/HydraTextClient";

    public static Theme GlobalTheme;
    public static UserData Data;
    public static List<ApClient> ActiveClients = [];
    public static Dictionary<int, string> PlayerSlots = [];
    public static string[] Players = [];
    public static string[] PlayerGames = [];
    public static int MoveToTab;
    public static ApClient ChosenTextClient = null;
    public static double ConnectionCooldown;
    public static Dictionary<ApClient, HashSet<Hint>> HintsMap = [];
    public static string LastLocationChecked = null;
    public static ConcurrentBag<SlotClient> ClientsToConnect = [];
    public static ConcurrentBag<SlotClient> ClientsToDisconnect = [];
    private static readonly Dictionary<ItemFlags, string> ItemColorHexCache = [];
    private static readonly Dictionary<ItemFlags, string> ItemBgColorHexCache = [];
    private static readonly Dictionary<string, TwoWayLookup<long, string>> ItemIdToName = [];
    private static readonly Dictionary<string, TwoWayLookup<long, string>> LocationIdToName = [];
    private static bool _UpdateHints;
    private static HintDataComparer _HintDataComparer = new();
    private static HintComparer _HintComparer = new();
    private static StyleBoxFlat Background = new();
    
    public static Dictionary<HintStatus, string> HintStatusColor = new()
    {
        [Found] = "hint_found",
        [Unspecified] = "hint_unspecified",
        [NoPriority] = "hint_no_priority",
        [Avoid] = "hint_avoid",
        [Priority] = "hint_priority",
    };

    public static Dictionary<HintStatus, int> HintStatusNumber = new()
    {
        [Priority] = 0,
        [Avoid] = 1,
        [NoPriority] = 2,
        [Unspecified] = 3,
        [Found] = 4,
    };

    public static HintStatus[] HintStatuses =
        Enum.GetValues<HintStatus>().OrderBy(status => HintStatusNumber[status]).ToArray();

    public static Dictionary<HintStatus, string> HintStatusText =
        HintStatuses.ToDictionary(hs => hs, hs => Enum.GetName(hs)!);

    public delegate void SaveHandler();

    public delegate void ClientConnectHandler(ApClient client);

    public delegate void ClientDisconnectHandler(ApClient client);

    public static event SaveHandler OnSave;
    public static event ClientConnectHandler? ClientConnectEvent;
    public static event ClientDisconnectHandler? ClientDisconnectEvent;

    [Export] public string Version;
    [Export] private Theme _UITheme;
    [Export] private LoggerLabel _AppLogger;
    [Export] private HintManager _HintManager;
    [Export] private TabContainer _TabContainer;
    [Export] private TextClient _TextClient;
    [Export] private global::LoginTab _LoginTab;
    [Export] private TabContainer[] _BackgroundOverrides = [];

    public Dictionary<string,SlotClient>.ValueCollection Clients => _LoginTab.Clients;
    public bool HasSlotName(string name) => _LoginTab.HasSlotName(name);
    public string Address => Data.Address;
    public string Password => Data.Password;
    public int Port => Data.Port;

    public override void _EnterTree()
    {
        Main = this;
        _AppLogger.Init();
        OS.AddLogger(_AppLogger.Logger);
        GD.Print("Godot Logger Added");
        GetViewport().TransparentBg = true;
        GlobalTheme = _UITheme;
        Data = new UserData();
        if (!Directory.Exists(SaveDir))
        {
            Directory.CreateDirectory(SaveDir);
        }
        else if (File.Exists($"{SaveDir}/data.json"))
        {
            Data = JsonConvert.DeserializeObject<UserData>(File.ReadAllText($"{SaveDir}/data.json")
                                                               .Replace("\r", "")
                                                               .Replace("\n", ""));
            GetWindow().Size = Data.WindowSize;
            if (Data.WindowPosition is null) return;
            GetWindow().Position = Data.WindowPosition!.Value;
        }

        if (!Directory.Exists($"{SaveDir}/Game Portraits"))
        {
            Directory.CreateDirectory($"{SaveDir}/Game Portraits");
        }
    }

    public override void _Ready()
    {
        Data.NullCheck();
        RichPresenceController.Init();
        SetAlwaysOnTop(Data.AlwaysOnTop);

        Background.BgColor = Data["background_color"];

        foreach (var container in _BackgroundOverrides)
        {
            container.AddThemeStyleboxOverride("panel", Background);
        }

        OnSave += () => Data.WindowSize = GetWindow().Size;
        OnSave += () => Data.WindowPosition = GetWindow().Position;

        if (new Random().Next(100) != 1) return;
    }

    public override void _Process(double delta)
    {
        if (MoveToTab != -1)
        {
            _TabContainer.CurrentTab = MoveToTab;
            MoveToTab = -1;
        }

        foreach (var client in ClientsToConnect)
        {
            ConnectClient(client);
        }
        ClientsToConnect.Clear();
        
        foreach (var client in ClientsToDisconnect)
        {
            DisconnectClient(client);
        }
        ClientsToDisconnect.Clear();
        
        var updateRequest = ActiveClients.Any(client => client.HintsAwaitingUpdate);
        if ((updateRequest || _UpdateHints || TextClient.HintRequest) && MultiworldName.CurrentWorld is not null)
        {
            List<Hint> hints = [];
            foreach (var client in ActiveClients)
            {
                client.PushUpdatedVariables(false, out var newHints);
                client.Hints = newHints.ToHashSet();
                hints.AddRange(newHints);

                if (HintsMap[client] is not null && HintsMap[client].Count != client.Hints.Count)
                {
                    foreach (var difference in client.Hints.Except(HintsMap[client], _HintComparer))
                    {
                        if (ChosenTextClient.PlayerSlot == difference.ReceivingPlayer ||
                            ChosenTextClient.PlayerSlot == difference.FindingPlayer || (
                                PlayerSlots.ContainsKey(difference.FindingPlayer) &&
                                PlayerSlots.ContainsKey(difference.ReceivingPlayer) &&
                                client.PlayerSlot == difference.ReceivingPlayer
                            )) continue;

                        TextClient.Messages.Enqueue(difference);
                    }
                }

                HintsMap[client] = client.Hints;
            }

            TextClient.HintRequest = false;
            if (MultiworldName.CurrentWorld is not null)
            {
                MultiworldName.CurrentWorld.MergeHints(hints.Select(hint => new HintData(hint)).ToArray());
            }

            HintTable.RefreshUI = true;
            _UpdateHints = false;
        }
    }

    public static string ItemIdToItemName(long id, int playerSlot)
    {
        var game = PlayerGames[playerSlot];
        if (!ItemIdToName.TryGetValue(game, out var itemNameDict))
        {
            GetLookups(game, out _, out itemNameDict);
        }

        return itemNameDict[id];
    }

    public static string LocationIdToLocationName(long id, int playerSlot)
    {
        var game = PlayerGames[playerSlot];
        if (!LocationIdToName.TryGetValue(game, out var locNameDict))
        {
            GetLookups(game, out locNameDict, out _);
        }

        return locNameDict[id];
    }

    public static void GetLookups(string game, out TwoWayLookup<long, string> locations,
        out TwoWayLookup<long, string> items)
    {
        ActiveClients[0].GetLookups(game, out locations, out items);
        LocationIdToName[game] = locations;
        ItemIdToName[game] = items;
    }

    public void ConnectClient(ApClient client)
    {
        if (ActiveClients.Contains(client)) return;

        if (ChosenTextClient is not null && !client.Tags[ArchipelagoTag.NoText])
        {
            client.Tags.SetTags(ArchipelagoTag.TextOnly, ArchipelagoTag.NoText);
        }

        ChosenTextClient ??= client;

        if (ActiveClients.Count == 0)
        {
            Clear();
            SetupPlayerList(client);
        }

        if (PlayerGames.Length == 0)
        {
            PlayerGames = client.PlayerGames;
        }

        if (Players.Length == 0)
        {
            Players = client.PlayerNames;
        }

        client.CheckedLocationsUpdated += locations
            => _HintManager.CallDeferred("LocationCheck", locations.ToArray(), client.PlayerSlot);

        PlayerSlots[client.PlayerSlot] = client.PlayerName;
        ActiveClients.Add(client);
        HintsMap.Add(client, null);
        _HintManager.RegisterPlayer(client);
        ClientConnectEvent?.Invoke(client);
        _LoginTab.ChangeMultiworldState(MultiworldState.Load);

        RefreshUIColors();
    }

    public void DisconnectClient(ApClient client)
    {
        if (!ActiveClients.Contains(client)) return;
        ActiveClients.Remove(client);
        PlayerSlots.Remove(client.PlayerSlot);
        ClientDisconnectEvent?.Invoke(client);

        if (ChosenTextClient == client)
        {
            ChosenTextClient = null;
        }

        if (client.HasPlayerListSetup && ActiveClients.Count != 0)
        {
            SetupPlayerList(ActiveClients[0]);
        }

        if (ActiveClients.Count == 0)
        {
            _LoginTab.ChangeMultiworldState(MultiworldState.None);
            Clear();
        }
        else if (ChosenTextClient is null)
        {
            ChosenTextClient = ActiveClients[0];
            ChosenTextClient!.Tags.SetTags(ArchipelagoTag.TextOnly, ArchipelagoTag.DeathLink, ArchipelagoTag.TrapLink);
        }

        _HintManager.UnregisterPlayer(client);
        HintsMap.Remove(client);
        InventoryManager.RemoveInventory(client.PlayerName);
        RefreshUIColors();
    }

    public static void Clear()
    {
        Datas = [];
        TextClient.ClearClient = true;
        Players = [];
        PlayerGames = [];
    }

    public static void SetupPlayerList(ApClient client)
    {
        client.OnPlayerStateChanged += slot =>
        {
            Datas[slot].SetPlayerStatus(client.PlayerStates[slot]);
            RefreshUI = true;
        };

        client.SetupPlayerList();
        Datas = client.PlayerStates
                      .Select((state, i)
                           => new PlayerData(i, client.PlayerGames[i], state))
                      .ToArray();
    }

    public bool IsLocalHosted() => Address.ToLower() is "localhost" or "127.0.0.1";
    public static void UpdateDiscord() => DiscordIntegration.UpdateActivity();

    public static void TryReconnectDiscord()
    {
        if (DiscordIntegration.DiscordAlive) return;
        DiscordIntegration.CheckDiscord(DiscordIntegration.DiscordAppId);
    }

    public static void OpenSaveDir() => OS.ShellOpen(SaveDir);

    public static ColorSetting PlayerColor(int playerSlot)
    {
        try
        {
            var playerName = ActiveClients[0].PlayerNames[playerSlot];
            return Data
            [
                playerName == "Server"
                    ? "player_server"
                    : PlayerSlots.ContainsValue(playerName) // connected slots
                        ? "player_color"
                        : Main._LoginTab.HasSlotName(playerName) // all login slots
                            ? "player_color_offline"
                            : "player_generic"
            ];
        }
        catch
        {
            return Data["player_generic"];
        }
    }

    public static bool IsPlayerSlotALoginSlot(int playerSlot)
        => Main._LoginTab.HasSlotName(ActiveClients[0].PlayerNames[playerSlot]);

    public static string GetItemHexColor(ItemFlags flags, string metaData)
    {
        if (Data.ItemFilters.TryGetValue(metaData, out var filter) && filter.IsSpecial) return Data["item_special"];
        return Data[GetItemColorString(flags)].Hex;
    }

    public static string GetItemHexBgColor(ItemFlags flags, string metaData)
    {
        if (Data.ItemFilters.TryGetValue(metaData, out var filter) && filter.IsSpecial) return Data["item_bg_special"];
        return Data[GetItemBgColorString(flags)].Hex;
    }

    public static string FormatItemColor(ItemInfo info, bool withMetaString)
        => FormatItemColor(info.ItemName, info.ItemGame, info.ItemId, info.Flags, withMetaString);

    public static string FormatItemColor(string itemName, string gameName, long itemId, ItemFlags flags,
        bool withMetaString)
    {
        var metaString = Settings.GetMetaString(itemName, gameName, itemId, flags);
        var color = GetItemHexColor(flags, metaString);
        var bgColor = GetItemHexBgColor(flags, metaString);
        return withMetaString
            ? $"[bgcolor={bgColor}][color={color}][url=\"{metaString}\"]{itemName.Clean()}[/url][/color][/bgcolor]"
            : $"[bgcolor={bgColor}][color={color}]{itemName.Clean()}[/color][/bgcolor]";
    }

    private static string GetItemColorString(ItemFlags flags)
    {
        if (ItemColorHexCache.TryGetValue(flags, out var color)) return color;
        if ((flags & Advancement) == Advancement)
        {
            color = "item_progressive";
        }
        else if ((flags & NeverExclude) == NeverExclude)
        {
            color = "item_useful";
        }
        else if ((flags & Trap) == Trap)
        {
            color = "item_trap";
        }
        else
        {
            color = "item_normal";
        }

        return ItemColorHexCache[flags] = color;
    }

    private static string GetItemBgColorString(ItemFlags flags)
    {
        if (ItemBgColorHexCache.TryGetValue(flags, out var color)) return color;
        if ((flags & Advancement) == Advancement)
        {
            color = "item_bg_progressive";
        }
        else if ((flags & NeverExclude) == NeverExclude)
        {
            color = "item_bg_useful";
        }
        else if ((flags & Trap) == Trap)
        {
            color = "item_bg_trap";
        }
        else
        {
            color = "item_bg_normal";
        }

        return ItemBgColorHexCache[flags] = color;
    }

    public static void RefreshUIColors()
    {
        Background.BgColor = Data["background_color"];
        TextClient.RefreshText = true;
        ItemFilterer.RefreshUI = true;
        InventoryManager.RefreshUI = true;
        PlayerTable.RefreshUI = true;
        _UpdateHints = true;
    }

    public static void Save()
    {
        OnSave?.Invoke();
        File.WriteAllText($"{SaveDir}/data.json", JsonConvert.SerializeObject(Data));
    }

    public static string GetAlias(int slot, bool additionalInfo = false)
    {
        if (ActiveClients.Count == 0) return "Not loaded";
        try
        {
            var name = ActiveClients[0].PlayerNames[slot];
            var alias = ActiveClients[0].GetAlias(slot)!.Replace($" ({name})", "").Clean();
            return !additionalInfo
                ? GetAliasFormated(alias, name)
                : $"[hint=\"Name: {name}\nGame: {PlayerGames[slot]}\"]{alias}[/hint]";
        }
        catch (IndexOutOfRangeException)
        {
            return $"{slot}";
        }
    }

    private static string GetAliasFormated(string alias, string name)
    {
        if (alias == name) return name;
        return Data.AliasDisplay switch
        {
            0 => $"{alias} ({name})",
            1 => alias,
            2 => name,
            3 => $"{name} ({alias})"
        };
    }

    public static void SetAlwaysOnTop(bool b) => Data.AlwaysOnTop = Main.GetWindow().AlwaysOnTop = b;

    public override void _Notification(int what)
    {
        if (what != NotificationWMCloseRequest) return;
        DiscordIntegration.Discord.Dispose();
        Save();
        GetTree().Quit();
    }
}

public class HintComparer : IEqualityComparer<Hint>
{
    public bool Equals(Hint x, Hint y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        if (x.GetType() != y.GetType()) return false;
        return x.ReceivingPlayer == y.ReceivingPlayer && x.FindingPlayer == y.FindingPlayer && x.ItemId == y.ItemId &&
               x.LocationId == y.LocationId && x.ItemFlags == y.ItemFlags && x.Found == y.Found &&
               x.Entrance == y.Entrance && x.Status == y.Status;
    }

    public int GetHashCode(Hint obj)
    {
        return HashCode.Combine(obj.ReceivingPlayer, obj.FindingPlayer, obj.ItemId, obj.LocationId, (int)obj.ItemFlags,
            obj.Found, obj.Entrance, (int)obj.Status);
    }
}

public class HintDataComparer : IEqualityComparer<HintData>
{
    public bool Equals(HintData x, HintData y)
    {
        return x.ReceivingPlayer == y.ReceivingPlayer && x.ReceivingPlayerSlot == y.ReceivingPlayerSlot &&
               x.Item == y.Item && x.ItemFlags == y.ItemFlags && x.FindingPlayer == y.FindingPlayer &&
               x.FindingPlayerSlot == y.FindingPlayerSlot && x.HintStatus == y.HintStatus && x.Location == y.Location &&
               x.LocationId == y.LocationId && x.Entrance == y.Entrance;
    }

    public int GetHashCode(HintData obj)
    {
        var hashCode = new HashCode();
        hashCode.Add(obj.ReceivingPlayer);
        hashCode.Add(obj.ReceivingPlayerSlot);
        hashCode.Add(obj.Item);
        hashCode.Add((int)obj.ItemFlags);
        hashCode.Add(obj.FindingPlayer);
        hashCode.Add(obj.FindingPlayerSlot);
        hashCode.Add((int)obj.HintStatus);
        hashCode.Add(obj.Location);
        hashCode.Add(obj.LocationId);
        hashCode.Add(obj.Entrance);
        return hashCode.ToHashCode();
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using CreepyUtil.Archipelago;
using CreepyUtil.DiscordRpc;
using Godot;
using Newtonsoft.Json;
using static Archipelago.MultiClient.Net.Enums.HintStatus;
using static Archipelago.MultiClient.Net.Enums.ItemFlags;
using Environment = System.Environment;

namespace ArchipelagoMultiTextClient.Scripts;

public partial class MainController : Control
{
    public static string SaveDir =
        $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/HydraTextClient";

    public static Theme GlobalTheme;
    public static Data Data;
    public static Dictionary<string, SlotClient> ClientList = [];
    public static List<ApClient> ActiveClients = [];
    public static Dictionary<int, string> PlayerSlots = [];
    public static string[] Players = [];
    public static string[] PlayerGames = [];
    public static int MoveToTab;
    public static ApClient ChosenTextClient = null;
    public static double ConnectionCooldown;
    public static Dictionary<ApClient, HashSet<Hint>> HintsMap = [];
    public static string LastLocationChecked = null;
    private static readonly Dictionary<ItemFlags, string> ItemColorHexCache = [];
    private static readonly Dictionary<string, Dictionary<long, string>> ItemIdToName = [];
    private static readonly Dictionary<long, string> LocationIdToName = [];
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

    [Export] private Theme _UITheme;
    [Export] private LineEdit _AddressField;
    [Export] private LineEdit _PasswordField;
    [Export] private LineEdit _PortField;
    [Export] private LineEdit _SlotField;
    [Export] private VBoxContainer _SlotContainer;
    [Export] private Button _SlotAddButton;
    [Export] private PackedScene _SlotPackedScene;
    [Export] private HintManager _HintManager;
    [Export] private TabContainer _TabContainer;
    [Export] private Label _ConnectionTimer;
    [Export] private TextClient _TextClient;
    [Export] private Timer _DiscordTimer;
    [Export] private Label _DiscordText;
    [Export] private Button _DiscordReconnect;

    public string Address => Data.Address;
    public string Password => Data.Password;
    public int Port => Data.Port;
    public string Slot => _SlotField.Text;

    public override void _EnterTree()
    {
        GlobalTheme = _UITheme;
        Data = new Data();
        if (!Directory.Exists(SaveDir))
        {
            Directory.CreateDirectory(SaveDir);
        }
        else if (File.Exists($"{SaveDir}/data.json"))
        {
            Data = JsonConvert.DeserializeObject<Data>(File.ReadAllText($"{SaveDir}/data.json")
                                                           .Replace("\r", "")
                                                           .Replace("\n", ""));
        }
    }

    public override void _Ready()
    {
        Data.NullCheck();
        RichPresenceController.Init();
        _DiscordTimer.Start();
        _SlotField.TextSubmitted += TryAddSlot;
        _SlotAddButton.Pressed += () => TryAddSlot(Slot);
        _AddressField.TextChanged += s => Data.Address = s;
        _PasswordField.TextChanged += s => Data.Password = s;
        _AddressField.Text = Data.Address;
        _PasswordField.Text = Data.Password;
        _PortField.Text = $"{Data.Port}";

        foreach (var player in Data.SlotNames)
        {
            AddSlot(player);
        }

        Background.BgColor = Data["background_color"];
        _TabContainer.AddThemeStyleboxOverride("panel", Background);
    }

    public override void _Process(double delta)
    {
        _DiscordText.Visible = DiscordIntegration.DiscordAlive;
        _DiscordReconnect.Visible = !DiscordIntegration.DiscordAlive;
        
        // ReSharper disable once AssignmentInConditionalExpression
        if (_ConnectionTimer.Visible = ConnectionCooldown > 0) // intentional (because funny)
        {
            _ConnectionTimer.Text = $"Connection Cooldown: {ConnectionCooldown:0.00}s (as to not spam the server)";
            ConnectionCooldown -= delta;
        }

        if (MoveToTab != -1)
        {
            _TabContainer.CurrentTab = MoveToTab;
            MoveToTab = -1;
        }

        var updateRequest = ActiveClients.Any(client => client.HintsAwaitingUpdate);
        if (updateRequest || _UpdateHints || TextClient.HintRequest)
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
            HintTable.Datas = hints.Select(hint => new HintData(hint)).ToHashSet(_HintDataComparer);
            HintTable.RefreshUI = true;

            _UpdateHints = false;
        }

        var anyRunning = ClientList.Values.Any(client => client.IsRunning);
        ToggleLockInput(!anyRunning);
    }

    public void TryAddSlot(string slot)
    {
        if (slot.Trim() == "" || ClientList.ContainsKey(slot.Trim())) return;
        AddSlot(slot.Trim());
        Data.SlotNames.Add(slot.Trim());
        _SlotField.Text = "";
    }

    public void AddSlot(string playerName)
    {
        var client = (SlotClient)_SlotPackedScene.Instantiate();
        client.Init(this, playerName);
        ClientList.Add(playerName, client);
        _SlotContainer.AddChild(client);
    }

    public void RemoveSlot(string playerName)
    {
        var client = ClientList[playerName];
        _SlotContainer.RemoveChild(client);
        ClientList.Remove(playerName);
        Data.SlotNames.Remove(playerName);
        client.QueueFree();
    }

    public static string ItemIdToItemName(long id, int playerSlot)
    {
        var game = PlayerGames[playerSlot];
        if (!ItemIdToName.TryGetValue(game, out var itemNameDict))
        {
            ItemIdToName[game] = itemNameDict = new Dictionary<long, string>();
        }

        if (!itemNameDict.TryGetValue(id, out var itemName))
        {
            itemName = ItemIdToName[game][id] = ActiveClients[0].Session.Items.GetItemName(id, game);
        }

        return itemName;
    }

    public static string LocationIdToLocationName(long id, int playerSlot)
    {
        if (!LocationIdToName.TryGetValue(id, out var location))
        {
            location = LocationIdToName[id] =
                ActiveClients[0].Session.Locations.GetLocationNameFromId(id, PlayerGames[playerSlot]);
        }

        return location;
    }

    public void ConnectClient(ApClient client)
    {
        if (ActiveClients.Contains(client)) return;

        if (ChosenTextClient is not null && !client.Session.ConnectionInfo.Tags.Contains("NoText"))
        {
            client.Session.ConnectionInfo.UpdateConnectionOptions(["TextOnly", "NoText"]);
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

        PlayerSlots.Add(client.PlayerSlot, client.PlayerName);
        ActiveClients.Add(client);
        HintsMap.Add(client, null);
        _HintManager.RegisterPlayer(client);
        RefreshUIColors();
    }

    public void DisconnectClient(ApClient client)
    {
        if (!ActiveClients.Contains(client)) return;
        ActiveClients.Remove(client);
        PlayerSlots.Remove(client.PlayerSlot);

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
            Clear();
        }
        else if (ChosenTextClient is null)
        {
            ChosenTextClient = ActiveClients[0];
            ChosenTextClient!.Session.ConnectionInfo.UpdateConnectionOptions(["TextOnly"]);
        }

        _HintManager.UnregisterPlayer(client);
        HintsMap.Remove(client);
        RefreshUIColors();
    }

    public void SetupPlayerList(ApClient client)
    {
        client.OnPlayerStateChanged += (_, slot) =>
        {
            PlayerTable.Datas[slot].SetPlayerStatus(client.PlayerStates[slot]);
            PlayerTable.RefreshUI = true;
        };
        client.SetupPlayerList();
        PlayerTable.Datas = client.PlayerStates
                                  .Select((state, i)
                                       => new PlayerData(i, client.PlayerNames[i], client.PlayerGames[i], state))
                                  .ToArray();
        PlayerTable.RefreshUI = true;
    }

    public void Clear()
    {
        PlayerTable.Datas = [];
        TextClient.ClearClient = true;
        Players = [];
        PlayerGames = [];
    }

    public void ToggleLockInput(bool toggle)
    {
        _AddressField.Editable = toggle;
        _PasswordField.Editable = toggle;
        _PortField.Editable = toggle;
    }

    public void UpdateDiscord()
    {
        DiscordIntegration.UpdateActivity();
    }

    public void TryReconnectDiscord()
    {
        if (DiscordIntegration.DiscordAlive) return;
        DiscordIntegration.CheckDiscord(DiscordIntegration.DiscordAppId);
    }

    public static ColorSetting PlayerColor(string playerName)
        => Data
        [
            playerName == "Server"
                ? "player_server"
                : PlayerSlots.ContainsValue(playerName)
                    ? "player_color"
                    : "player_generic"];

    public static ColorSetting PlayerColor(int playerSlot)
        => Data
        [
            playerSlot == 0
                ? "player_server"
                : PlayerSlots.ContainsKey(playerSlot)
                    ? "player_color"
                    : "player_generic"];

    public static string GetItemHexColor(ItemFlags flags) => Data[GetItemColorString(flags)].Hex;

    public static string GetItemColorString(ItemFlags flags)
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

    public static void RefreshUIColors()
    {
        Background.BgColor = Data["background_color"];
        TextClient.RefreshText = true;
        PlayerTable.RefreshUI = true;
        ItemFilterer.RefreshUI = true;
        _UpdateHints = true;
    }

    public static void Save() => File.WriteAllText($"{SaveDir}/data.json", JsonConvert.SerializeObject(Data));

    public static string GetAlias(int slot, bool additionalInfo)
    {
        var alias = ActiveClients[0].Session.Players.AllPlayers.ElementAt(slot).Alias.Clean();
        if (!additionalInfo) return alias;
        return $"[hint=\"Name: {Players[slot]}\nGame: {PlayerGames[slot]}\"]{alias}[/hint]";
    }

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
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using ArchipelagoMultiTextClient.Scripts.TextClientTab;
using ArchipelagoMultiTextClient.Scripts.UtilitiesTab;
using CreepyUtil.Archipelago;
using CreepyUtil.Archipelago.ApClient;
using Godot;
using Newtonsoft.Json;
using static ArchipelagoMultiTextClient.Scripts.MainController;
using static ArchipelagoMultiTextClient.Scripts.TextClientTab.TextClient;
using static CreepyUtil.Archipelago.ArchipelagoTag;

namespace ArchipelagoMultiTextClient.Scripts.LoginTab;

public partial class SlotClient : Control
{
    // todo: set multiworld.gg's max to 15
    public bool? IsRunning = false;

    public MainController Main;
    public ApClient Client = new();
    public bool IsTextClient = false;
    private string[]? _Error;
    private double NullTimer;

    public string PlayerName { get; set; }

    public event Action<ConnectionStatus, string[]?>? ConnectionStatusChanged;

    public override void _EnterTree()
    {
        ConnectionStatusChanged += (status, _) =>
        {
            IsRunning = status switch
            {
                ConnectionStatus.NotConnected => false,
                ConnectionStatus.Connecting => null,
                ConnectionStatus.Connected => true,
                ConnectionStatus.Error => false,
            };
        };

        Client.OnConnectionEvent += _ => ConnectionStatusChanged?.Invoke(ConnectionStatus.Connected, _Error);
        Client.OnConnectionLost += () => ConnectionStatusChanged?.Invoke(ConnectionStatus.NotConnected, _Error);
    }

    public override void _Process(double delta)
    {
        if (IsRunning is null) NullTimer += delta;
        else NullTimer = 0;

        if (NullTimer >= 30 && !Client.IsConnected)
        {
            NullTimer = 0;
            ConnectionStatusChanged?.Invoke(ConnectionStatus.NotConnected, _Error);
        }

        if (IsRunning is null or false) return;
        Client?.UpdateConnection();

        if (!(bool)Client?.IsConnected!) return;
        var items = Client?.GetOutstandingItems();
        InventoryManager.AddItems(Client?.PlayerName, items, false);
    }

    public void TryConnection()
    {
        if (!Main.IsLocalHosted())
        {
            if (ActiveClients.Count >= 7)
            {
                ConnectionFailed(["Can only have 7 slots connected"], false);
                return;
            }

            if (ConnectionCooldown > 0)
            {
                ConnectionFailed(["Please wait after connecting/changing slots to do so again"], false);
                return;
            }

            ConnectionCooldown = 4;
        }

        _Error = null;
        ConnectionStatusChanged?.Invoke(ConnectionStatus.Connecting, _Error);
        LoginInfo login = new(Main.Port, PlayerName, Main.Address, Main.Password);

        ArchipelagoTag[] tags = ChosenTextClient is null ? [TextOnly, DeathLink, TrapLink] : [TextOnly, NoText];

        Task.Run(() =>
        {
            try
            {
                string[] error;
                lock (Client)
                {
                    error = Client.TryConnect(login, "", ItemsHandlingFlags.AllItems, tags: tags);
                }

                if (error is not null && error.Length > 0)
                {
                    GD.PrintErr($"Connection [For;{login.Slot}] Failed:\n{string.Join("\n", error)}");
                    CallDeferred("ConnectionFailed", error);
                }
                else
                {
                    GD.Print($"Connection [For;{login.Slot}] Succeeded");
                    CallDeferred("HasConnected");
                }
            }
            catch (Exception e)
            {
                CallDeferred("ConnectionFailed", [e.Message, e.StackTrace]);
            }
        });
    }

    public void TryDisconnection()
    {
        Task.Run(() =>
        {
            Client?.TryDisconnect();
            CallDeferred("HasDisconnected");
            GD.Print($"Connection [For;{PlayerName}] Ended");
        });
    }

    public void ConnectionFailed() => ConnectionFailed(["No Error Given"], true);
    public void ConnectionFailed(string[] error) => ConnectionFailed(error, true);

    public void ConnectionFailed(string[] error, bool disconnect)
    {
        ConnectionStatusChanged?.Invoke(ConnectionStatus.Error, _Error = error);
        if (!disconnect) return;
        ConnectionCooldown = 0;
        TryDisconnection();
    }

    public static readonly Regex RemoveNickName = new(@".+ \((.+)\)");

    public void HasConnected()
    {
        var playerName = Client.PlayerName;
        Client.OnConnectionErrorReceived += (err, _) =>
        {
            switch (err)
            {
                case WebSocketException { WebSocketErrorCode: WebSocketError.ConnectionClosedPrematurely }:
                    GD.PrintErr($"From [{playerName}] Archipelago connection closed (should be handled, ignore)");
                    return;
                case JsonSerializationException:
                    return;
            }

            GD.Print("=== THE FOLLOWING IS A CONNECTION ERROR =====================");
            GD.Print($"Error from: [{playerName}]");
            GD.PrintErr(err);
            GD.Print("============================================================");
        };

        Client.OnHintPrintJsonPacketReceived += packet
            => Messages.Enqueue(new ClientMessage(packet.Data, MessageSender.Hint, copyText: packet.GetCopy()));

        Client.OnChatPrintPacketReceived += packet
            => Messages.Enqueue(new ClientMessage(packet.Data, chatPrintJsonPacket: packet));

        Client.OnServerMessagePacketReceived += packet =>
        {
            var text = packet.Data[0].Text;
            switch (packet)
            {
                case JoinPrintJsonPacket join:
                    EnqueueJoinLeaveMessage(join.Slot, MessageSender.Joined);
                    return;
                case LeavePrintJsonPacket leave:
                    EnqueueJoinLeaveMessage(leave.Slot, MessageSender.Left);
                    return;
                case TagsChangedPrintJsonPacket tagsChanged:
                {
                    var secondSplit = text.Split(" has changed tags from ")[1].Split(" to ");
                    Messages.Enqueue(new ClientMessage(
                        [
                            new JsonMessagePart
                            {
                                Text = $"{tagsChanged.Slot}",
                                Type = JsonMessagePartType.PlayerId
                            },
                            new JsonMessagePart { Text = $" {secondSplit[0]} → {secondSplit[1]}" }
                        ],
                        MessageSender.TagsChanged));
                    return;
                }
            }

            Messages.Enqueue(new ClientMessage(packet.Data, MessageSender.Server));
            return;

            void EnqueueJoinLeaveMessage(int player, MessageSender sender)
            {
                var tagsStart = text.LastIndexOf('[') + 1;
                var tagsEnd = text.LastIndexOf(']');
                Messages.Enqueue(new ClientMessage(
                [
                    new JsonMessagePart
                    {
                        Text = $"{player}",
                        Type = JsonMessagePartType.PlayerId
                    },
                    new JsonMessagePart { Text = $" [{text[tagsStart..tagsEnd]}]" }
                ], sender));
            }
        };

        Client.OnDeathLinkPacketReceived += (source, cause) =>
        {
            var player = RemoveNickName.IsMatch(source)
                ? RemoveNickName.Match(source).Groups[1].Value
                : source;
            var playerId = Array.IndexOf(ChosenTextClient.PlayerNames, player);
            JsonMessagePart playerPart = new() { Text = playerId == -1 ? source : $"{playerId}" };
            if (playerId != -1)
            {
                playerPart.Type = JsonMessagePartType.PlayerId;
            }

            if (!cause.Contains(source))
            {
                Messages.Enqueue(new ClientMessage(
                [
                    playerPart,
                    new JsonMessagePart { Text = $" {cause}" }
                ], MessageSender.DeathLink));
                return;
            }

            var split = cause.Split(source);
            List<JsonMessagePart> list = [];
            for (var i = 0; i < split.Length; i++)
            {
                if (i != 0)
                {
                    list.Add(playerPart);
                }

                list.Add(new JsonMessagePart { Text = split[i] });
            }

            Messages.Enqueue(new ClientMessage(list.ToArray(), MessageSender.DeathLink));
        };

        Client.OnUnregisteredTrapLinkReceived += (source, trap) =>
        {
            var player = RemoveNickName.IsMatch(source)
                ? RemoveNickName.Match(source).Groups[1].Value
                : source;
            var playerId = Array.IndexOf(ChosenTextClient.PlayerNames, player);
            JsonMessagePart playerPart = new() { Text = playerId == -1 ? source : $"{playerId}" };
            if (playerId != -1)
            {
                playerPart.Type = JsonMessagePartType.PlayerId;
            }

            Messages.Enqueue(new ClientMessage([
                playerPart,
                new JsonMessagePart { Text = $" sent a [{trap}]" }
            ], MessageSender.TrapLink));
        };

        Client.OnItemLogPacketReceived += packet =>
        {
            var playerSlot = int.Parse(packet.Data[0].Text);
            if (playerSlot == ChosenTextClient.PlayerSlot)
            {
                var locationId = long.Parse(packet.Data[^2].Text);
                LastLocationChecked = LocationIdToLocationName(locationId, playerSlot);
            }

            Messages.Enqueue(new ClientMessage(packet.Data, MessageSender.ItemLog,
                copyText: packet.Data.GetItemLogCopy()));
        };

        Client.OnConnectionLost += () => { ConnectionFailed(["Lost Connection to Server"]); };

        InventoryManager.AddInventory(Client.PlayerName);
        InventoryManager.AddItems(Client?.PlayerName, Client!.GetOutstandingItems(), true);

        Client.ExcludeBouncedPacketsFromSelf = false;

        Main.ConnectClient(Client);
    }

    public void HasDisconnected()
    {
        Main.DisconnectClient(Client);
        ConnectionStatusChanged?.Invoke(_Error is null ? ConnectionStatus.NotConnected : ConnectionStatus.Error,
            _Error);
        Client = new ApClient();
    }

    public void Say(string message) => Client.Say(message);
}
using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net.Enums;
using CreepyUtil.Archipelago;
using Godot;
using Newtonsoft.Json;
using static ArchipelagoMultiTextClient.Scripts.MainController;
using static ArchipelagoMultiTextClient.Scripts.TextClient;

namespace ArchipelagoMultiTextClient.Scripts;

public partial class SlotClient : PanelContainer
{
    private static int ClientCount;
    public bool? IsRunning = false;

    [Export] private RichTextLabel _SlotDisplay;
    public MainController Main;
    public ApClient Client = new();
    public bool IsTextClient = false;
    private string[]? _Error;

    public string PlayerName { get; set; }

    public override void _Ready()
    {
        _SlotDisplay.MetaClicked += v =>
        {
            var s = (string)v;
            switch (s)
            {
                case "disconnect":
                    TryDisconnection();
                    break;
                case "connect":
                    TryConnection();
                    break;
                case "delete":
                    Main.RemoveSlot(PlayerName);
                    break;
            }
        };
    }

    public override void _Process(double delta) => Client.UpdateConnection();

    public void TryConnection()
    {
        if (!Main.IsLocalHosted())
        {
            if (ClientCount >= 7)
            {
                ConnectionFailed(["Can only have 7 slots connected"], false);
                return;
            }

            ClientCount++;
            
            if (ConnectionCooldown > 0)
            {
                ConnectionFailed(["Please wait after connecting/changing slots to do so again"], false);
                return;
            }

            ConnectionCooldown = 4;
        }

        IsRunning = null;
        _Error = null;
        RefreshUi();
        LoginInfo login = new(Main.Port, PlayerName, Main.Address, Main.Password);

        string[] tags = ChosenTextClient is null ? ["TextOnly"] : ["TextOnly", "NoText"];

        Task.Run(() =>
        {
            try
            {
                string[] error;
                lock (Client)
                {
                    error = Client.TryConnect(login, 0, "", ItemsHandlingFlags.NoItems, tags: tags);
                }

                if (error is not null && error.Length > 0)
                {
                    CallDeferred("ConnectionFailed", error);
                }
                else
                {
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
        });
    }

    public void ConnectionFailed(string[] error, bool disconnect = true)
    {
        IsRunning = false;
        _Error = error;
        if (!disconnect) return;
        ConnectionCooldown = 0;
        TryDisconnection();
    }

    public void HasConnected()
    {
        IsRunning = true;
        var playerName = Client.PlayerName;
        Client.OnConnectionErrorReceived += (err, _)
            =>
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

        Client.OnHintPrintJsonPacketReceived += (_, packet)
            => Messages.Enqueue(new ClientMessage(packet.Data, isHint: true, copyText: packet.GetCopy()));

        Client.OnChatPrintPacketReceived += (_, packet)
            => Messages.Enqueue(new ClientMessage(packet.Data, chatPrintJsonPacket: packet));

        Client.OnServerMessagePacketReceived += (_, packet)
            => Messages.Enqueue(new ClientMessage(packet.Data, isServer: true));

        Client.OnItemLogPacketReceived += (_, packet) =>
        {
            var playerSlot = int.Parse(packet.Data[0].Text);
            if (playerSlot == ChosenTextClient.PlayerSlot)
            {
                var locationId = long.Parse(packet.Data[^2].Text);
                LastLocationChecked = LocationIdToLocationName(locationId, playerSlot);
            }

            Messages.Enqueue(new ClientMessage(packet.Data, true,
                copyText: packet.Data.GetItemLogCopy()));
        };

        Client.OnConnectionLost += (_, _) => { ConnectionFailed(["Lost Connection to Server"]); };

        Main.ConnectClient(Client);
        RefreshUi();
    }

    public void HasDisconnected()
    {
        IsRunning = false;
        Main.DisconnectClient(Client);
        Client = new ApClient();
        RefreshUi();
        if (Main.IsLocalHosted()) return;
        ClientCount--;
    }

    public void Say(string message) => Client.Say(message);

    public void RefreshUi()
    {
        var connectText = IsRunning switch
        {
            false => "[url=\"connect\"][color=green][bgcolor=darkgreen]  Connect   [/bgcolor][/color][/url]",
            true => "[url=\"disconnect\"][color=orangered][bgcolor=darkred] Disconnect [/bgcolor][/color][/url]",
            null => "Connecting. . ."
        };

        var deleteText = IsRunning is not null && !IsRunning!.Value
            ? "[url=\"delete\"][color=orangered][bgcolor=darkred] X [/bgcolor][/color][/url]"
            : "   ";

        var errorText = _Error is not null && _Error.Length > 0
            ? $"\n[color=red]{string.Join('\n', _Error)}[/color]"
            : "";

        _SlotDisplay.Text = $"""
                             [table=3]
                             [cell]{deleteText}[/cell]
                             [cell]    {connectText}    [/cell]
                             [cell]{PlayerName}[/cell]
                             [/table]{errorText}
                             """;
    }
}
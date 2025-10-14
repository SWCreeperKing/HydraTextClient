using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net.Enums;
using ArchipelagoMultiTextClient.Scripts.TextClientTab;
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
    public bool? IsRunning = false;

    public MainController Main;
    public ApClient Client = new();
    public bool IsTextClient = false;
    private string[]? _Error;
    private string ConnectForeground = "#00ae00";
    private string ConnectBackground = "#003000";
    private string DeleteForeground = "orangered";
    private string DeleteBackground = "#570000";
    private double NullTimer;

    public string PlayerName { get; set; }
    
    public override void _Process(double delta)
    {
        if (IsRunning is null) NullTimer += delta;
        else NullTimer = 0;
        
        if (NullTimer >= 30 && !Client.IsConnected)
        {
            NullTimer = 0;
            IsRunning = false;
            SlotTable.RefreshUI = true;
        }
        
        if (IsRunning is null or false) return;
        Client?.UpdateConnection();
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

        IsRunning = null;
        _Error = null;
        SlotTable.RefreshUI = true;
        LoginInfo login = new(Main.Port, PlayerName, Main.Address, Main.Password);

        ArchipelagoTag[] tags = ChosenTextClient is null ? [TextOnly] : [TextOnly, NoText];

        Task.Run(() =>
        {
            try
            {
                string[] error;
                lock (Client)
                {
                    error = Client.TryConnect(login,  "", ItemsHandlingFlags.NoItems, tags: tags);
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

    public void ConnectionFailed() => ConnectionFailed(["No Error Given"], true);
    public void ConnectionFailed(string[] error) => ConnectionFailed(error, true);
    public void ConnectionFailed(string[] error, bool disconnect)
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

        Client.OnHintPrintJsonPacketReceived += packet
            => Messages.Enqueue(new ClientMessage(packet.Data, isHint: true, copyText: packet.GetCopy()));

        Client.OnChatPrintPacketReceived += packet
            => Messages.Enqueue(new ClientMessage(packet.Data, chatPrintJsonPacket: packet));

        Client.OnServerMessagePacketReceived += packet
            => Messages.Enqueue(new ClientMessage(packet.Data, isServer: true));

        Client.OnItemLogPacketReceived += packet =>
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

        Client.OnConnectionLost += () => { ConnectionFailed(["Lost Connection to Server"]); };

        Main.ConnectClient(Client);
        SlotTable.RefreshUI = true;
    }

    public void HasDisconnected()
    {
        IsRunning = false;
        Main.DisconnectClient(Client);
        Client = new ApClient();
        SlotTable.RefreshUI = true;
    }

    public void Say(string message) => Client.Say(message);

    public string[] GrabUI()
    {
        var connectText = IsRunning switch
        {
            false => $"[url=\"connect {PlayerName}\"][color={ConnectForeground}][bgcolor={ConnectBackground}]  Connect   [/bgcolor][/color][/url]",
            true => $"[url=\"disconnect {PlayerName}\"][color={DeleteForeground}][bgcolor={DeleteBackground}] Disconnect [/bgcolor][/color][/url]",
            null => "Connecting. . ."
        };

        var deleteText = IsRunning is not null && !IsRunning!.Value
            ? $"[url=\"delete {PlayerName}\"][color={DeleteForeground}][bgcolor={DeleteBackground}] X [/bgcolor][/color][/url]"
            : "   ";

        var errorText = _Error is not null && _Error.Length > 0
            ? $"\n[color=red]{string.Join('\n', _Error)}[/color]"
            : "";
        
        return [connectText, $"   {PlayerName}   {errorText}", deleteText];
    }
}
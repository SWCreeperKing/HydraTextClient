using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using CreepyUtil.Archipelago;
using Godot;
using Newtonsoft.Json;
using static ArchipelagoMultiTextClient.Scripts.MainController;
using static ArchipelagoMultiTextClient.Scripts.TextClient;

namespace ArchipelagoMultiTextClient.Scripts;

public partial class SlotClient : PanelContainer
{
    private static int ClientCount;
    public bool IsRunning;

    [Export] private Button _ConnectButton;
    [Export] private Label _ConnectingLabel;
    [Export] private Button _DisconnectButton;
    [Export] private Label _PlayerNameLabel;
    [Export] private Button _DeleteButton;
    [Export] private RichTextLabel _ErrorLabel;
    public ApClient Client = new();
    public bool IsTextClient = false;
    private MainController _Main;

    public string PlayerName { get; private set; }

    public void Init(MainController main, string playerName)
    {
        _Main = main;
        PlayerName = playerName;
        _PlayerNameLabel.Text = playerName;
        _DeleteButton.Pressed += () => _Main.RemoveSlot(PlayerName);
    }

    public override void _Ready()
    {
        _ConnectButton.Pressed += TryConnection;
        _DisconnectButton.Pressed += TryDisconnection;
    }

    public override void _Process(double delta) => Client.UpdateConnection();

    public void TryConnection()
    {
        if (!_Main.IsLocalHosted())
        {
            ClientCount++;
            if (ClientCount >= 7)
            {
                ConnectionFailed(["Can only have 7 slots connected"]);
                return;
            }

            if (ConnectionCooldown > 0)
            {
                ConnectionFailed(["Please wait after connecting/changing slots to do so again"]);
                return;
            }

            ConnectionCooldown = 4;
        }

        IsRunning = true;
        _ErrorLabel.Visible = false;
        _ConnectButton.Visible = false;
        _DeleteButton.Visible = false;
        _ConnectingLabel.Visible = true;
        LoginInfo login = new(_Main.Port, PlayerName, _Main.Address, _Main.Password);

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
            Client.TryDisconnect();
            CallDeferred("HasDisconnected");
        });
    }

    public void ConnectionFailed(string[] error)
    {
        IsRunning = false;
        _ErrorLabel.Visible = true;
        _ErrorLabel.Text = string.Join("\n", error);
        ConnectionCooldown = 0;
        TryDisconnection();
    }

    public void HasConnected()
    {
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

        _ConnectingLabel.Visible = false;
        _ConnectButton.Visible = false;
        _DisconnectButton.Visible = true;
        _Main.ConnectClient(Client);
    }

    public void HasDisconnected()
    {
        IsRunning = false;
        _ConnectingLabel.Visible = false;
        _ConnectButton.Visible = true;
        _DeleteButton.Visible = true;
        _DisconnectButton.Visible = false;
        _Main.DisconnectClient(Client);
        Client = new ApClient();
        if (_Main.IsLocalHosted()) return;
        ClientCount--;
    }


    public void Say(string message) => Client.Say(message);
}
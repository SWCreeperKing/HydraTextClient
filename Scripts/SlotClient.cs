using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using ArchipelagoMultiTextClient.Scripts;
using CreepyUtil.Archipelago;
using static TextClient;

public partial class SlotClient : PanelContainer
{
    public bool IsRunning;
    
    [Export] private Button _ConnectButton;
    [Export] private Label _ConnectingLabel;
    [Export] private Button _DisconnectButton;
    [Export] private Label _PlayerNameLabel;
    [Export] private Button _DeleteButton;
    [Export] private RichTextLabel _ErrorLabel;
    public ApClient Client = new();
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

    public void TryConnection()
    {
        IsRunning = true;
        _ErrorLabel.Visible = false;
        _ConnectButton.Visible = false;
        _DeleteButton.Visible = false;
        _ConnectingLabel.Visible = true;
        LoginInfo login = new(_Main.Port, PlayerName, _Main.Address, _Main.Password);

        Task.Run(() =>
        {
            try
            {
                string[]? error = null;
                lock (Client)
                {
                    error = Client.TryConnect(login, 0, "", ItemsHandlingFlags.NoItems, tags: ["TextOnly"]);
                    Client.Session.Socket.PacketReceived += packet =>
                    {
                        switch (packet)
                        {
                            case ChatPrintJsonPacket message:
                                AwaitingMessages.Enqueue(new ClientMessage(message.Data, chatPrintJsonPacket: message));
                                break;
                            case BouncedPacket:
                                break;
                            case PrintJsonPacket updatePacket:
                                if (updatePacket.Data.Length == 1)
                                {
                                    AwaitingMessages.Enqueue(new ClientMessage(updatePacket.Data, isServer: true));
                                }

                                if (updatePacket.Data.Length < 2) break;
                                if (updatePacket.Data.First().Text!.StartsWith("[Hint]: "))
                                {
                                    if (updatePacket.Data.Last().HintStatus!.Value == HintStatus.Found) break;
                                    AwaitingMessages.Enqueue(new ClientMessage(updatePacket.Data));
                                }
                                else if (updatePacket.Data[1].Text is " found their " or " sent ")
                                {
                                    AwaitingMessages.Enqueue(new ClientMessage(updatePacket.Data, true));
                                }

                                break;
                        }
                    };
                }

                if (error is not null)
                {
                    CallDeferred("ConnectionFailed", error);
                    return;
                }

                CallDeferred("HasConnected");
            }
            catch (Exception e)
            {
                CallDeferred("ConnectionFailed", new[] { e.Message, e.StackTrace });
            }
        });
    }

    public void TryDisconnection()
    {
        Task.Run(() =>
        {
            lock (Client)
            {
                Client.TryDisconnect();
            }

            CallDeferred("HasDisconnected");
        });
    }

    public void ConnectionFailed(string[] error)
    {
        IsRunning = false;
        _ErrorLabel.Visible = true;
        _ErrorLabel.Text = string.Join("\n", error);
        TryDisconnection();
    }

    public void HasConnected()
    {
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
    }


    public void Say(string message) { Client.Say(message); }
}
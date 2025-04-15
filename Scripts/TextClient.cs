using System;
using Godot;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using ArchipelagoMultiTextClient.Scripts;
using static ArchipelagoMultiTextClient.Scripts.MainController;

public partial class TextClient : VBoxContainer
{
    public static bool RefreshText;
    public static bool ClearClient;
    public static ConcurrentQueue<ClientMessage> AwaitingMessages = [];

    [Export] private MainController _Main;
    [Export] private OptionButton _SelectedClient;
    [Export] private OptionButton _WordWrap;
    [Export] private OptionButton _Content;
    [Export] private RichTextLabel _Messages;
    [Export] private LineEdit _SendMessage;
    [Export] private Button _SendMessageButton;
    [Export] private SpinBox _Latency;
    [Export] private Button _ScrollToBottom;
    [Export] private ScrollContainer _ScrollContainer;
    private Queue<ClientMessage> _ChatMessages = new(500);
    private Queue<ClientMessage> _ItemLog = new(500);
    private Queue<ClientMessage> _Both = new(1000);
    private List<string> _SentMessages = new(150);
    private MessageGateKeeper _Keeper = new();
    private ScrollBar _VScrollBar;
    private string[] _CurrentPlayers = [];
    private int _ScrollBackNum;
    private bool _ToScroll;

    public override void _Ready()
    {
        MessageGateKeeper.Stopwatch.Start();
        _VScrollBar = _ScrollContainer.GetVScrollBar();
        
        _Latency.ValueChanged += val => MainController.Data.Latency = MessageGateKeeper.Latency = (long)val;
        _Latency.Value = MessageGateKeeper.Latency = MainController.Data.Latency;
        
        _WordWrap.ItemSelected += i => _Messages.AutowrapMode = (TextServer.AutowrapMode)(MainController.Data.WordWrap = i);
        _Content.ItemSelected += i =>
        {
            MainController.Data.Content = i;
            RefreshText = true;
        };
        _WordWrap.Selected = (int)MainController.Data.WordWrap;
        _Content.Selected = (int)MainController.Data.Content;
        
        _SendMessage.TextSubmitted += SendMessage;
        _SendMessage.GuiInput += input =>
        {
            if (_SendMessage.Text != "") return;
            if (_SentMessages.Count == 0) return;
            if (input is not InputEventKey key) return;
            if (!key.IsPressed()) return;
            switch (key.Keycode)
            {
                case Key.Up:
                    _ScrollBackNum++;
                    break;
                case Key.Down:
                    _ScrollBackNum--;
                    break;
                default:
                    return;
            }
            _ScrollBackNum = Math.Clamp(_ScrollBackNum, 0, _SentMessages.Count - 1);
            _SendMessage.Text = _SentMessages[_ScrollBackNum];
        };
        _SendMessage.FocusEntered += () => _ScrollBackNum = 0;
        
        _SendMessageButton.Pressed += () => SendMessage(_SendMessage.Text);
        _ScrollToBottom.Pressed += () => _ScrollContainer.ScrollVertical = (int)_ScrollContainer.GetVScrollBar().MaxValue;
        _VScrollBar.Changed += () =>
        {
            if (!_ToScroll) return;
            _ScrollContainer.ScrollVertical = (int)_ScrollContainer.GetVScrollBar().MaxValue;
            _ToScroll = false;
        };
    }

    public override void _Process(double delta)
    {
        if (ClearClient)
        {
            Clear();
            ClearClient = false;
        }

        if (_CurrentPlayers.Length != ActiveClients.Count)
        {
            _CurrentPlayers = ActiveClients.Select(client => client.PlayerName).ToArray();
            _SelectedClient.Clear();
            for (var i = 0; i < _CurrentPlayers.Length; i++)
            {
                _SelectedClient.AddItem(_CurrentPlayers[i], i);
            }
        }

        while (!AwaitingMessages.IsEmpty)
        {
            _ToScroll = _VScrollBar.Value >= _VScrollBar.MaxValue - _ScrollContainer.Size.Y;
            AwaitingMessages.TryDequeue(out var message);
            if (!_Keeper.CanAdd(message.HashCode)) continue;
            _Both.Enqueue(message);

            if (message.IsItemLog)
            {
                _ItemLog.Enqueue(message);
            }
            else
            {
                _ChatMessages.Enqueue(message);
            }

            RefreshText = true;
        }

        if (!RefreshText) return;

        switch (_Content.Selected)
        {
            case 0: // text only
                _Messages.Text = string.Join("\n", _ChatMessages.Select(msg => msg.GenString()));
                break;
            case 1: // items only
                _Messages.Text = string.Join("\n", _ItemLog.Select(msg => msg.GenString()));
                break;
            case 2: // both
                _Messages.Text = string.Join("\n", _Both.Select(msg => msg.GenString()));
                break;
        }

        RefreshText = false;
    }

    public void SendMessage(string message)
    {
        var trim = message.Trim();
        if (trim == "") return;
        if (_CurrentPlayers.Length == 0) return;
        _ScrollBackNum = 0;
        _SentMessages.Add(trim);
        ActiveClients[_SelectedClient.Selected].Say(message);
        _SendMessage.Text = "";
    }

    public void Clear()
    {
        _Messages.Text = "";
        _ChatMessages.Clear();
        _ItemLog.Clear();
        _Both.Clear();
        AwaitingMessages.Clear();
        _Keeper.PacketMessageQueue.Clear();
        _Keeper.MessageSet.Clear();
    }
}

public readonly struct ClientMessage(
    JsonMessagePart[] messageParts,
    bool isItemLog = false,
    bool isServer = false,
    ChatPrintJsonPacket? chatPrintJsonPacket = null)
{
    public readonly bool IsItemLog = isItemLog;
    public readonly bool IsServer = isServer;
    public readonly JsonMessagePart[] MessageParts = messageParts;
    public readonly int HashCode = string.Join("", messageParts.Select(msg => msg.Text)).GetHashCode();
    public readonly ChatPrintJsonPacket? ChatPacket = chatPrintJsonPacket;

    public string GenString()
    {
        string color;
        if (ChatPacket is not null)
        {
            color =  PlayerColor(ChatPacket.Slot);
            return $"[color={color}]{Players[ChatPacket.Slot]}[/color]: {ChatPacket.Message}";
        }

        StringBuilder messageBuilder = new();

        if (IsServer)
        {
            messageBuilder.Append($"[color={MainController.Data.ColorSettings["player_server"].Hex}]Server[/color]: ");
        }

        foreach (var part in MessageParts)
        {
            switch (part.Type)
            {
                case JsonMessagePartType.PlayerId:
                    var slot = int.Parse(part.Text);
                    color = PlayerColor(slot);
                    messageBuilder.Append($"[color={color}]{Players[slot]}[/color]");
                    break;
                case JsonMessagePartType.ItemId:
                    var item = ItemIdToItemName(long.Parse(part.Text), part.Player!.Value);
                    color = GetItemHexColor(part.Flags!.Value);
                    messageBuilder.Append($"[color={color}]{item}[/color]");
                    break;
                case JsonMessagePartType.LocationId:
                    var location = LocationIdToLocationName(long.Parse(part.Text), part.Player!.Value);
                    color = MainController.Data.ColorSettings["location"];
                    messageBuilder.Append($"[color={color}]{location}[/color]");
                    break;
                case JsonMessagePartType.EntranceName:
                    var entranceName = part.Text.Trim();
                    color = MainController.Data.ColorSettings[entranceName == "" ? "entrance_vanilla" : "entrance"];
                    messageBuilder.Append($"[color={color}]{(entranceName == "" ? "Vanilla" : entranceName)}[/color]");
                    break;
                case JsonMessagePartType.HintStatus:
                    var name = HintStatusText[(HintStatus)part.HintStatus!];
                    color = MainController.Data.ColorSettings[HintStatusColor[(HintStatus)part.HintStatus!]];
                    messageBuilder.Append($"[color={color}]{name}[/color]");
                    break;
                default:
                    messageBuilder.Append(part.Text ?? "");
                    break;
            }
        }

        return messageBuilder.ToString();
    }
}

public class MessageGateKeeper
{
    public static long Latency = 50;
    public static Stopwatch Stopwatch = new();
    public PriorityQueue<DataTimestamp, long> PacketMessageQueue = new();
    public HashSet<int> MessageSet = [];

    public bool CanAdd(int hashData)
    {
        var currentTimestamp = Stopwatch.ElapsedMilliseconds;
        while (PacketMessageQueue.Count != 0 && currentTimestamp - PacketMessageQueue.Peek().Timestamp > Latency)
        {
            var dequeued = PacketMessageQueue.Dequeue();
            MessageSet.Remove(dequeued.HashCode);
        }

        if (!MessageSet.Add(hashData)) return false;

        PacketMessageQueue.Enqueue(new DataTimestamp(currentTimestamp, hashData), currentTimestamp);
        return true;
    }
}

public readonly struct DataTimestamp(long timestamp, int hashCode)
{
    public readonly long Timestamp = timestamp;
    public readonly int HashCode = hashCode;
}
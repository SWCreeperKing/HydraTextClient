using System;
using System.Collections.Concurrent;
using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using ArchipelagoMultiTextClient.Scripts;
using static ArchipelagoMultiTextClient.Scripts.MainController;

public partial class TextClient : VBoxContainer
{
    public static ConcurrentQueue<ClientMessage> Messages = [];
    public static bool HintRequest;
    public static double HintRequestTimer;
    public static bool RefreshText;
    public static bool ClearClient;
    public static List<string> CopyList = [];

    [Export] private MainController _Main;
    [Export] private OptionButton _SelectedClient;
    [Export] private OptionButton _WordWrap;
    [Export] private OptionButton _Content;
    [Export] private RichTextLabel _Messages;
    [Export] private LineEdit _SendMessage;
    [Export] private Button _SendMessageButton;
    [Export] private Button _ScrollToBottom;
    [Export] private ScrollContainer _ScrollContainer;
    private Queue<ClientMessage> _ChatMessages = new(250);
    private Queue<ClientMessage> _ItemLog = new(250);
    private Queue<ClientMessage> _Both = new(500);
    private List<string> _SentMessages = new(50);
    private ScrollBar _VScrollBar;
    private string[] _CurrentPlayers = [];
    private int _ScrollBackNum;
    private bool _ToScroll;
    private double _MessageCooldown;
    private long _LastSelected;

    public override void _Ready()
    {
        _VScrollBar = _ScrollContainer.GetVScrollBar();

        _WordWrap.ItemSelected += i
            => _Messages.AutowrapMode = (TextServer.AutowrapMode)(MainController.Data.WordWrap = i);
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
        _SendMessage.FocusEntered += () => _ScrollBackNum = _SentMessages.Count - 1;

        _SendMessageButton.Pressed += () => SendMessage(_SendMessage.Text);
        _ScrollToBottom.Pressed +=
            () => _ScrollContainer.ScrollVertical = (int)_ScrollContainer.GetVScrollBar().MaxValue;
        _VScrollBar.Changed += () =>
        {
            if (!_ToScroll) return;
            _ScrollContainer.ScrollVertical = (int)_ScrollContainer.GetVScrollBar().MaxValue;
            _ToScroll = false;
        };

        _SelectedClient.ItemSelected += l =>
        {
            if (ConnectionCooldown > 0)
            {
                _SelectedClient.Selected = (int)_LastSelected;
                return;
            }

            ChosenTextClient.Session.ConnectionInfo.UpdateConnectionOptions(["TextOnly", "NoText"]);
            Task.Delay(750).GetAwaiter().GetResult();
            ChosenTextClient = null;
            ChosenTextClient = ActiveClients[(int)l];
            ChosenTextClient.Session.ConnectionInfo.UpdateConnectionOptions(["TextOnly"]);
            ConnectionCooldown = 7 + 3 * ActiveClients.Count;
            _LastSelected = l;
        };

        _Messages.MetaClicked += s => DisplayServer.ClipboardSet(CopyList[int.Parse((string)s)]);
    }

    public override void _Process(double delta)
    {
        _SendMessageButton.Disabled = _MessageCooldown > 0;

        if (_MessageCooldown > 0)
        {
            _MessageCooldown -= delta;
        }

        if (ClearClient)
        {
            Clear();
            ClearClient = false;
        }

        if (HintRequest)
        {
            HintRequestTimer += delta;
        }

        if (!HintRequest && !Messages.IsEmpty)
        {
            while (!HintRequest && !Messages.IsEmpty)
            {
                Messages.TryDequeue(out var message);
                _ToScroll = _VScrollBar.Value >= _VScrollBar.MaxValue - _ScrollContainer.Size.Y;
                _Both.Enqueue(message);

                if (message.IsItemLog)
                {
                    _ItemLog.Enqueue(message);
                }
                else
                {
                    _ChatMessages.Enqueue(message);
                }

                if (message.IsHintRequest)
                {
                    HintRequest = true;
                }
            }

            RefreshText = true;
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

        if (!RefreshText) return;

        CopyList.Clear();
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
        if (_MessageCooldown > 0) return;
        var trim = message.Trim();
        if (trim == "") return;
        if (_CurrentPlayers.Length == 0) return;
        _MessageCooldown = .3f;
        _SentMessages.Add(trim);
        ChosenTextClient!.Say(message);
        _SendMessage.Text = "";
        _ScrollBackNum = _SentMessages.Count - 1;
    }

    public void Clear()
    {
        _Messages.Text = "";
        _ChatMessages.Clear();
        _ItemLog.Clear();
        _Both.Clear();
        Messages.Clear();
        HintRequest = false;
    }
}

public readonly struct ClientMessage(
    JsonMessagePart[] messageParts,
    bool isItemLog = false,
    bool isServer = false,
    ChatPrintJsonPacket chatPrintJsonPacket = null)
{
    public readonly bool IsItemLog = isItemLog;
    public readonly bool IsServer = isServer;
    public readonly JsonMessagePart[] MessageParts = messageParts;
    public readonly ChatPrintJsonPacket ChatPacket = chatPrintJsonPacket;

    public readonly bool IsHintRequest =
        chatPrintJsonPacket is not null && chatPrintJsonPacket.Message.StartsWith("!hint");

    public string GenString()
    {
        string color;
        var copyId = TextClient.CopyList.Count;
        if (ChatPacket is not null)
        {
            color = PlayerColor(ChatPacket.Slot);
            TextClient.CopyList.Add($"{GetAlias(ChatPacket.Slot, false)}: {ChatPacket.Message}");
            return $"[color={color}][url={copyId}]{GetAlias(ChatPacket.Slot, true)}[/url][/color]: {ChatPacket.Message.Clean()}";
        }

        StringBuilder messageBuilder = new();

        if (IsServer)
        {
            messageBuilder.Append(
                $"[color={MainController.Data["player_server"].Hex}][url={copyId}]Server[/url][/color]: ");
        }

        foreach (var part in MessageParts)
        {
            switch (part.Type)
            {
                case JsonMessagePartType.PlayerId:
                    var slot = int.Parse(part.Text);
                    color = PlayerColor(slot);
                    messageBuilder.Append($"[color={color}]{GetAlias(slot, true)}[/color]");
                    break;
                case JsonMessagePartType.ItemId:
                    var item = ItemIdToItemName(long.Parse(part.Text), part.Player!.Value);
                    color = GetItemHexColor(part.Flags!.Value);
                    messageBuilder.Append($"[color={color}]{item.Clean()}[/color]");
                    break;
                case JsonMessagePartType.LocationId:
                    var location = LocationIdToLocationName(long.Parse(part.Text), part.Player!.Value);
                    color = MainController.Data["location"];
                    messageBuilder.Append($"[color={color}]{location.Clean()}[/color]");
                    break;
                case JsonMessagePartType.EntranceName:
                    var entranceName = part.Text.Trim();
                    color = MainController.Data[entranceName == "" ? "entrance_vanilla" : "entrance"];
                    messageBuilder.Append(
                        $"[color={color}]{(entranceName == "" ? "Vanilla" : entranceName).Clean()}[/color]");
                    break;
                case JsonMessagePartType.HintStatus:
                    var name = HintStatusText[(HintStatus)part.HintStatus!];
                    color = MainController.Data[HintStatusColor[(HintStatus)part.HintStatus!]];
                    messageBuilder.Append($"[color={color}]{name.Clean()}[/color]");
                    break;
                default:
                    var text = (part.Text ?? "").Clean();
                    messageBuilder.Append(text);
                    if (IsServer)
                    {
                        TextClient.CopyList.Add(text);
                    }

                    break;
            }
        }

        return messageBuilder.ToString();
    }

    public static readonly JsonMessagePart[] TextParts =
    [
        new() { Text = "[Hint]: " }, new() { Text = "'s " }, new() { Text = " is at " }, new() { Text = " in " },
        new() { Text = "'s World. " },
    ];

    public static implicit operator ClientMessage(Hint hint)
    {
        return new ClientMessage([
            TextParts[0],
            new JsonMessagePart
            {
                Type = JsonMessagePartType.PlayerId,
                Text = $"{hint.ReceivingPlayer}"
            },
            TextParts[1],
            new JsonMessagePart
            {
                Type = JsonMessagePartType.ItemId,
                Text = $"{hint.ItemId}",
                Flags = hint.ItemFlags,
                Player = hint.ReceivingPlayer
            },
            TextParts[2],
            new JsonMessagePart
            {
                Type = JsonMessagePartType.LocationId,
                Text = $"{hint.LocationId}",
                Player = hint.FindingPlayer
            },
            TextParts[3],
            new JsonMessagePart
            {
                Type = JsonMessagePartType.PlayerId,
                Text = $"{hint.FindingPlayer}"
            },
            TextParts[4],
            new JsonMessagePart
            {
                Type = JsonMessagePartType.HintStatus,
                HintStatus = hint.Status
            }
        ]);
    }
}

public static class TextHelper
{
    public static string Clean(this string text) => text.Replace("[", "[lb]");
    public static string CleanRb(this string text) => text.Replace("]", "[rb]");
    public static string ReplaceB(this string text) => text.Replace("[", "<").Replace("]", ">");
}
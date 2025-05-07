using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using Godot;
using Newtonsoft.Json;
using static ArchipelagoMultiTextClient.Scripts.MainController;
using static ArchipelagoMultiTextClient.Scripts.Settings;

namespace ArchipelagoMultiTextClient.Scripts;

public partial class TextClient : VBoxContainer
{
    public static ConcurrentQueue<ClientMessage> Messages = [];
    public static bool HintRequest;
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
    [Export] private CheckBox _ShowProgressive;
    [Export] private CheckBox _ShowUseful;
    [Export] private CheckBox _ShowNormal;
    [Export] private CheckBox _ShowTraps;
    [Export] private CheckBox _ShowOnlyYou;
    [Export] private SpinBox _LineSeparation;
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
        _Messages.AutowrapMode = (TextServer.AutowrapMode)MainController.Data.WordWrap;
        
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

            LastLocationChecked = null;
            ChosenTextClient.Session.ConnectionInfo.UpdateConnectionOptions(["TextOnly", "NoText"]);
            ChosenTextClient = null;
            ChosenTextClient = ActiveClients[(int)l];
            ChosenTextClient.Session.ConnectionInfo.UpdateConnectionOptions(["TextOnly"]);
            ConnectionCooldown = 5 + 1 * ActiveClients.Count;
            _LastSelected = l;
        };

        _Messages.MetaClicked += meta =>
        {
            var s = (string)meta;
            if (s.StartsWith("itemdialog"))
            {
                Settings.ItemFilterDialog.SetItem(s);
                return;
            }

            DisplayServer.ClipboardSet(CopyList[int.Parse(s)]);
        };

        if (MainController.Data.ItemLogOptions.Length < 5)
        {
            bool[] corrected = [true, true, true, true, true];
            for (var i = 0; i < MainController.Data.ItemLogOptions.Length; i++)
            {
                corrected[i] = MainController.Data.ItemLogOptions[i];
            }

            MainController.Data.ItemLogOptions = corrected;
        }

        _ShowProgressive.Pressed += () =>
        {
            MainController.Data.ItemLogOptions[0] = _ShowProgressive.ButtonPressed;
            RefreshText = true;
        };
        _ShowUseful.Pressed += () =>
        {
            MainController.Data.ItemLogOptions[1] = _ShowUseful.ButtonPressed;
            RefreshText = true;
        };
        _ShowNormal.Pressed += () =>
        {
            MainController.Data.ItemLogOptions[2] = _ShowNormal.ButtonPressed;
            RefreshText = true;
        };
        _ShowTraps.Pressed += () =>
        {
            MainController.Data.ItemLogOptions[3] = _ShowTraps.ButtonPressed;
            RefreshText = true;
        };
        _ShowOnlyYou.Pressed += () =>
        {
            MainController.Data.ItemLogOptions[4] = _ShowOnlyYou.ButtonPressed;
            RefreshText = true;
        };
        _ShowProgressive.ButtonPressed = MainController.Data.ItemLogOptions[0];
        _ShowUseful.ButtonPressed = MainController.Data.ItemLogOptions[1];
        _ShowNormal.ButtonPressed = MainController.Data.ItemLogOptions[2];
        _ShowTraps.ButtonPressed = MainController.Data.ItemLogOptions[3];
        _ShowOnlyYou.ButtonPressed = MainController.Data.ItemLogOptions[4];
        
        _LineSeparation.Value = MainController.Data.TextClientLineSeparation;
        _LineSeparation.ValueChanged += d =>
        {
            MainController.Data.TextClientLineSeparation = (int)d;
            _Messages.RemoveThemeConstantOverride("line_separation");
            _Messages.AddThemeConstantOverride("line_separation", MainController.Data.TextClientLineSeparation);
        };
        _Messages.AddThemeConstantOverride("line_separation", MainController.Data.TextClientLineSeparation);
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

        if (!HintRequest && !Messages.IsEmpty)
        {
            while (!HintRequest && !Messages.IsEmpty)
            {
                Messages.TryDequeue(out var message);
                if (!Filter(message)) continue;

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
                _Messages.Text = string.Join("\n", _ChatMessages.Where(Filter).Select(msg => msg.GenString()));
                break;
            case 1: // items only
                _Messages.Text = string.Join("\n", _ItemLog.Where(Filter).Select(msg => msg.GenString()));
                break;
            case 2: // both
                _Messages.Text = string.Join("\n", _Both.Where(Filter).Select(msg => msg.GenString()));
                break;
        }

        RefreshText = false;
    }

    public bool Filter(ClientMessage message)
    {
        if (message.IsHint && message.MessageParts[^1].HintStatus is HintStatus.Found && !MainController.Data.ShowFoundHints) return false;
        if (!message.IsItemLog) return true;

        if (MainController.Data.ItemLogOptions[4] &&
            !message.MessageParts.Any(part => ActiveClients.Any(client => client.PlayerSlot == part.Player)))
            return false;

        var itemMessagePart = message.MessageParts[2];
        var flags = itemMessagePart.Flags!.Value;
        var id = long.Parse(itemMessagePart.Text);
        var playerSlot = itemMessagePart.Player!.Value;

        var uid = ItemFilter.MakeUidCode(id, ItemIdToItemName(id, playerSlot), PlayerGames[playerSlot], flags);

        if (MainController.Data.ItemFilters.TryGetValue(uid, out var itemFilter) &&
            !itemFilter.ShowInItemLog) return false;

        if ((flags & ItemFlags.Advancement) != 0)
            return MainController.Data.ItemLogOptions[0];

        if ((flags & ItemFlags.NeverExclude) != 0)
            return MainController.Data.ItemLogOptions[1];

        return (flags & ItemFlags.Trap) != 0
            ? MainController.Data.ItemLogOptions[3]
            : MainController.Data.ItemLogOptions[2];
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

    public void MetaClicked(string meta) { }
}

public readonly struct ClientMessage(
    JsonMessagePart[] messageParts,
    bool isItemLog = false,
    bool isServer = false,
    ChatPrintJsonPacket chatPrintJsonPacket = null,
    bool isHint = false,
    string copyText = null)
{
    public readonly bool IsItemLog = isItemLog;
    public readonly bool IsServer = isServer;
    public readonly bool IsHint = isHint;
    public readonly JsonMessagePart[] MessageParts = messageParts;
    public readonly ChatPrintJsonPacket ChatPacket = chatPrintJsonPacket;
    public readonly string CopyText = copyText;

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
            return
                $"[color={color}][url=\"{copyId}\"]{GetAlias(ChatPacket.Slot, true)}[/url][/color]: {ChatPacket.Message.Clean()}";
        }

        StringBuilder messageBuilder = new();

        if (IsServer)
        {
            messageBuilder.Append(
                $"[color={MainController.Data["player_server"].Hex}][url=\"{copyId}\"]Server[/url][/color]: ");
        }

        if (IsItemLog)
        {
            var fontSize = MainController.Data.FontSizes["text_client"];
            messageBuilder.Append(
                $"[hint=\"Click to Copy\"][url=\"{copyId}\"][img={fontSize}x{fontSize}]res://Assets/Images/UI/Copy.png[/img][/url][/hint] ");
            TextClient.CopyList.Add(CopyText);
        }

        for (var i = 0; i < MessageParts.Length; i++)
        {
            var part = MessageParts[i];
            switch (part.Type)
            {
                case JsonMessagePartType.PlayerId:
                    var slot = int.Parse(part.Text);
                    color = PlayerColor(slot);
                    messageBuilder.Append($"[color={color}]{GetAlias(slot, true)}[/color]");
                    break;
                case JsonMessagePartType.ItemId:
                    var itemId = long.Parse(part.Text);
                    var game = PlayerGames[part.Player!.Value];
                    var item = ItemIdToItemName(itemId, part.Player!.Value);
                    var flags = part.Flags!.Value;
                    color = GetItemHexColor(flags);
                    messageBuilder.Append(
                        $"[color={color}][url=\"{Settings.ItemFilterDialog.GetMetaString(item, game, itemId, flags)}\"]{item.Clean()}[/url][/color]");
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

                    if (IsHint && i == 0)
                    {
                        messageBuilder.Append($"[url=\"{copyId}\"][Hint][/url]: ");
                        TextClient.CopyList.Add(CopyText);
                        break;
                    }

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
        => new([
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
        ], isHint: true, copyText: hint.GetCopy());
}

public static class TextHelper
{
    public static string Clean(this string text) => text.Replace("[", "[lb]");
    public static string CleanRb(this string text) => text.Replace("]", "[rb]");
    public static string ReplaceB(this string text) => text.Replace("[", "<").Replace("]", ">");

    public static string GetCopy(this Hint hint)
    {
        var receivingPlayer = Players[hint.ReceivingPlayer];
        var item = ItemIdToItemName(hint.ItemId, hint.ReceivingPlayer);
        var findingPlayer = Players[hint.FindingPlayer];
        var location = LocationIdToLocationName(hint.LocationId, hint.FindingPlayer);
        var entrance = hint.Entrance.Trim() == "" ? "Vanilla" : hint.Entrance;

        return $"`{receivingPlayer}`'s __{item}__ is in `{findingPlayer}`'s world at **{location}**\n-# {entrance}";
    }

    public static string GetCopy(this HintPrintJsonPacket hint)
    {
        var receivingPlayer = Players[hint.ReceivingPlayer];
        var item = ItemIdToItemName(hint.Item.Item, hint.ReceivingPlayer);
        var findingPlayerSlot = int.Parse(hint.Data[7].Text);
        var findingPlayer = Players[findingPlayerSlot];
        var location = LocationIdToLocationName(long.Parse(hint.Data[5].Text), findingPlayerSlot);
        var entrance = hint.Data.Length == 11 ? "Vanilla" : hint.Data[9].Text;

        return $"`{receivingPlayer}`'s __{item}__ is in `{findingPlayer}`'s world at **{location}**\n-# {entrance}";
    }

    public static string GetItemLogCopy(this JsonMessagePart[] parts)
    {
        string item;
        string location;
        var firstPlayerSlot = int.Parse(parts[0].Text);
        var firstPlayer = Players[firstPlayerSlot];
        var itemId = long.Parse(parts[2].Text);
        var locationId = long.Parse(parts[^2].Text);
        location = LocationIdToLocationName(locationId, firstPlayerSlot);

        if (parts[1].Text is " found their ")
        {
            item = ItemIdToItemName(itemId, firstPlayerSlot);

            return $"`{firstPlayer}` found their __{item}__ (**{location}**)";
        }

        var secondPlayerSlot = int.Parse(parts[4].Text);
        var secondPlayer = Players[secondPlayerSlot];
        item = ItemIdToItemName(itemId, secondPlayerSlot);

        return $"`{firstPlayer}` sent __{item}__ to `{secondPlayer}` (**{location}**)";
    }
}
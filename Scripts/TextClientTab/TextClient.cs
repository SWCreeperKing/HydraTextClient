using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using ArchipelagoMultiTextClient.Scripts.LoginTab;
using CreepyUtil.Archipelago;
using Godot;
using static System.StringComparison;
using static ArchipelagoMultiTextClient.Scripts.MainController;
using static ArchipelagoMultiTextClient.Scripts.SettingsTab.Settings;

namespace ArchipelagoMultiTextClient.Scripts.TextClientTab;

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
    [Export] private OptionButton _ItemLogStyle;
    [Export] private OptionButton _AliasDisplay;
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
    [Export] private CheckBox _ClearTextOnDisconnect;
    [Export] private SpinBox _LineSeparation;
    [Export] private Label _History;
    private LimitedQueue<ClientMessage> _ChatMessages = new(250);
    private LimitedQueue<ClientMessage> _ItemLog = new(250);
    private LimitedQueue<ClientMessage> _Both = new(500);
    private LimitedList<string> _SentMessages = new(50);
    private ScrollBar _VScrollBar;
    private string[] _CurrentPlayers = [];
    private int _ScrollBackNum;
    private bool _ToScroll;
    private double _MessageCooldown;
    private long _LastSelected;
    private bool _WasLastMessageHintLocation = false;
    private string _HeldText;

    public delegate void SelectedClientChangedHandler(ApClient client);

    public static SelectedClientChangedHandler? SelectedClientChangedEvent;

    public override void _Ready()
    {
        _VScrollBar = _ScrollContainer.GetVScrollBar();

        _WordWrap.ItemSelected += i
            => _Messages.AutowrapMode = (TextServer.AutowrapMode)(Data.WordWrap = i);
        _Messages.AutowrapMode = (TextServer.AutowrapMode)Data.WordWrap;

        _AliasDisplay.ItemSelected += i
            =>
        {
            Data.AliasDisplay = i;
            RefreshUIColors();
        };

        _Content.ItemSelected += i =>
        {
            Data.Content = i;
            RefreshText = true;
        };

        _ItemLogStyle.ItemSelected += i =>
        {
            Data.ItemLogStyle = i;
            RefreshText = true;
        };

        _WordWrap.Selected = (int)Data.WordWrap;
        _AliasDisplay.Selected = (int)Data.AliasDisplay;
        _Content.Selected = (int)Data.Content;
        _ItemLogStyle.Selected = (int)Data.ItemLogStyle;

        _SendMessage.TextSubmitted += SendMessage;
        _SendMessage.FocusExited += () =>
        {
            if (_ScrollBackNum == -1) return;
            _SendMessage.Text = "";
            _HeldText = "";
            _ScrollBackNum = -1;
        };

        _SendMessage.GuiInput += input =>
        {
            if (_SentMessages.Count == 0 || input is InputEventMouseMotion) return;
            if (input is InputEventMouseButton iemb && GetRect().HasPoint(iemb.Position)) return;
            if (input is not InputEventKey key)
            {
                if (_ScrollBackNum != -1) return;
                _SendMessage.Text = "";
                _HeldText = "";
                _ScrollBackNum = -1;
                return;
            }

            if (!key.IsPressed()) return;

            if (_ScrollBackNum == -1 && _SendMessage.Text != "" && _SendMessage.Text != _HeldText)
            {
                _HeldText = _SendMessage.Text;
            }

            switch (key.Keycode)
            {
                case Key.Up:
                    _ScrollBackNum--;
                    break;
                case Key.Down:
                    _ScrollBackNum++;
                    break;
                default:
                    return;
            }

            if (_ScrollBackNum == -2)
            {
                _ScrollBackNum = _SentMessages.Count - 1;
            }
            else if (_ScrollBackNum > _SentMessages.Count - 1)
            {
                _ScrollBackNum = -1;
            }

            _SendMessage.Text = _ScrollBackNum == -1 ? _HeldText : _SentMessages[_ScrollBackNum];
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
            if (!_Main.IsLocalHosted())
            {
                if (ConnectionCooldown > 0)
                {
                    _SelectedClient.Selected = (int)_LastSelected;
                    return;
                }

                ConnectionCooldown = 4;
            }

            LastLocationChecked = null;
            ChosenTextClient.Session.ConnectionInfo.UpdateConnectionOptions(["TextOnly", "NoText"]);
            ChosenTextClient = null;
            ChosenTextClient = ActiveClients[(int)l];
            ChosenTextClient.Session.ConnectionInfo.UpdateConnectionOptions(["TextOnly"]);
            SelectedClientChangedEvent?.Invoke(ChosenTextClient);
            _LastSelected = l;
        };

        _Messages.MetaClicked += meta =>
        {
            var s = (string)meta;
            if (s.StartsWith("itemdialog"))
            {
                ItemFilterDialog.SetItem(s);
                return;
            }

            DisplayServer.ClipboardSet(CopyList[int.Parse(s)]);
        };

        if (Data.ItemLogOptions.Length < 5)
        {
            bool[] corrected = [true, true, true, true, true];
            for (var i = 0; i < Data.ItemLogOptions.Length; i++)
            {
                corrected[i] = Data.ItemLogOptions[i];
            }

            Data.ItemLogOptions = corrected;
        }

        _ShowProgressive.Pressed += () =>
        {
            Data.ItemLogOptions[0] = _ShowProgressive.ButtonPressed;
            RefreshText = true;
        };
        _ShowUseful.Pressed += () =>
        {
            Data.ItemLogOptions[1] = _ShowUseful.ButtonPressed;
            RefreshText = true;
        };
        _ShowNormal.Pressed += () =>
        {
            Data.ItemLogOptions[2] = _ShowNormal.ButtonPressed;
            RefreshText = true;
        };
        _ShowTraps.Pressed += () =>
        {
            Data.ItemLogOptions[3] = _ShowTraps.ButtonPressed;
            RefreshText = true;
        };
        _ShowOnlyYou.Pressed += () =>
        {
            Data.ItemLogOptions[4] = _ShowOnlyYou.ButtonPressed;
            RefreshText = true;
        };
        _ClearTextOnDisconnect.ButtonPressed = Data.ClearTextWhenDisconnect;
        _ClearTextOnDisconnect.Pressed += () =>
        {
            Data.ClearTextWhenDisconnect = _ClearTextOnDisconnect.ButtonPressed;
            RefreshText = true;
        };
        _ShowProgressive.ButtonPressed = Data.ItemLogOptions[0];
        _ShowUseful.ButtonPressed = Data.ItemLogOptions[1];
        _ShowNormal.ButtonPressed = Data.ItemLogOptions[2];
        _ShowTraps.ButtonPressed = Data.ItemLogOptions[3];
        _ShowOnlyYou.ButtonPressed = Data.ItemLogOptions[4];

        _LineSeparation.Value = Data.TextClientLineSeparation;
        _LineSeparation.ValueChanged += d =>
        {
            Data.TextClientLineSeparation = (int)d;
            _Messages.RemoveThemeConstantOverride("line_separation");
            _Messages.AddThemeConstantOverride("line_separation", Data.TextClientLineSeparation);
        };
        _Messages.AddThemeConstantOverride("line_separation", Data.TextClientLineSeparation);
    }

    public override void _Process(double delta)
    {
        if (_History.IsVisible())
        {
            _History.Text = $"[{_ScrollBackNum}] | [{_SentMessages.Count}]";
        }

        _SendMessageButton.Disabled = _MessageCooldown > 0;

        if (_MessageCooldown > 0) _MessageCooldown -= delta;

        if (ClearClient)
        {
            ClearClient = false;
            Clear();
        }

        while (!HintRequest && !Messages.IsEmpty)
        {
            Messages.TryDequeue(out var message);

            if (message.IsItemLog) MultiworldName.CurrentWorld.ItemLogItemReceived(message.MessageParts);
            if (!Filter(message, _WasLastMessageHintLocation, out _WasLastMessageHintLocation)) continue;

            _ToScroll = _VScrollBar.Value >= _VScrollBar.MaxValue - _ScrollContainer.Size.Y;
            _Both.Enqueue(message);

            if (message.IsItemLog) _ItemLog.Enqueue(message);
            else _ChatMessages.Enqueue(message);

            if (message.IsHintRequest) HintRequest = true;

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
        _Messages.Text = _Content.Selected switch
        {
            0 => FilterMessages(_ChatMessages), // text only
            1 => FilterMessages(_ItemLog), // items only
            2 => FilterMessages(_Both), // both
            _ => _Messages.Text
        };

        RefreshText = false;
        return;

        string FilterMessages(LimitedQueue<ClientMessage> messages)
        {
            var hintLocationBool = false;
            return string.Join("\n", messages.GetQueue
                                             .Where(message
                                                  => Filter(message, hintLocationBool, out hintLocationBool))
                                             .Select(msg => msg.GenString()));
        }
    }

    public bool Filter(ClientMessage message, bool wasHintLocation, out bool isHintLocation)
    {
        isHintLocation = false;
        var split = message.MessageParts[0].Text.Split(": ");
        if (split.Length > 1)
        {
            isHintLocation = split[1].StartsWith("!hint_location ", CurrentCultureIgnoreCase);
        }

        if (message.IsHint && message.MessageParts[^1].HintStatus is HintStatus.Found &&
            !Data.ShowFoundHints && !wasHintLocation) return false;
        if (!message.IsItemLog) return true;

        if (Data.ItemLogOptions[4] &&
            !message.MessageParts.Any(part => ActiveClients.Any(client => client.PlayerSlot == part.Player)))
            return false;

        var itemMessagePart = message.MessageParts[2];
        var flags = itemMessagePart.Flags!.Value;
        var id = long.Parse(itemMessagePart.Text);
        var playerSlot = itemMessagePart.Player!.Value;

        try
        {
            var uid = ItemFilter.MakeUidCode(id, ItemIdToItemName(id, playerSlot), PlayerGames[playerSlot], flags);

            if (Data.ItemFilters.TryGetValue(uid, out var itemFilter) &&
                !itemFilter.ShowInItemLog) return false;
        }
        catch
        {
            //ignore
        }

        if ((flags & ItemFlags.Advancement) != 0)
            return Data.ItemLogOptions[0];

        if ((flags & ItemFlags.NeverExclude) != 0)
            return Data.ItemLogOptions[1];

        return (flags & ItemFlags.Trap) != 0
            ? Data.ItemLogOptions[3]
            : Data.ItemLogOptions[2];
    }

    public void SendMessage(string message)
    {
        if (_MessageCooldown > 0) return;
        var trim = message.Trim();
        if (trim == "") return;
        if (_CurrentPlayers.Length == 0) return;
        _MessageCooldown = .3f;
        _SentMessages.Add(trim, false);
        ChosenTextClient!.Say(message);
        _SendMessage.Text = "";
        _HeldText = "";
        _ScrollBackNum = -1;
    }

    public void Clear()
    {
        if (!Data.ClearTextWhenDisconnect) return;
        _ScrollBackNum = -1;
        _Messages.Text = "";
        _HeldText = "";
        _ChatMessages.Clear();
        _ItemLog.Clear();
        _Both.Clear();
        _SentMessages.Clear();
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
            TextClient.CopyList.Add($"{GetAlias(ChatPacket.Slot)}: {ChatPacket.Message}");
            return
                $"[color={color}][url=\"{copyId}\"]{GetAlias(ChatPacket.Slot, true)}[/url][/color]: {ChatPacket.Message.Clean()}";
        }

        StringBuilder messageBuilder = new();

        if (IsServer)
        {
            messageBuilder.Append(
                $"[color={Data["player_server"].Hex}][url=\"{copyId}\"]Server[/url][/color]: ");
        }

        if (IsItemLog)
        {
            var fontSize = Data.FontSizes["text_client"];
            var copyStyle = Data.ItemLogStyle switch
            {
                0 => "",
                1 => $"[img={fontSize}x{fontSize}]res://Assets/Images/UI/Copy.png[/img]",
                2 => "[Copy] "
            };

            messageBuilder.Append(
                $"[hint=\"Click to Copy\"][url=\"{copyId}\"]{copyStyle}[/url][/hint] ");
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
                    var game = PlayerGames is null ? "Unknown" :
                        PlayerGames.Length <= part.Player!.Value ? "Unknown" : PlayerGames[part.Player!.Value];
                    var item = ItemIdToItemName(itemId, part.Player!.Value);
                    var flags = part.Flags!.Value;
                    var metaString = ItemFilterDialog.GetMetaString(item, game, itemId, flags);
                    color = GetItemHexColor(flags, metaString);
                    var bgColor = GetItemHexBgColor(flags, metaString);
                    messageBuilder.Append(
                        $"[bgcolor={bgColor}][color={color}][url=\"{metaString}\"]{item.Clean()}[/url][/color][/bgcolor]");
                    break;
                case JsonMessagePartType.LocationId:
                    var location = LocationIdToLocationName(long.Parse(part.Text), part.Player!.Value);
                    color = Data["location"];
                    messageBuilder.Append($"[color={color}]{location.Clean()}[/color]");
                    break;
                case JsonMessagePartType.EntranceName:
                    var entranceName = part.Text.Trim();
                    color = Data[entranceName == "" ? "entrance_vanilla" : "entrance"];
                    messageBuilder.Append(
                        $"[color={color}]{(entranceName == "" ? "Vanilla" : entranceName).Clean()}[/color]");
                    break;
                case JsonMessagePartType.HintStatus:
                    var name = HintStatusText[(HintStatus)part.HintStatus!];
                    color = Data[HintStatusColor[(HintStatus)part.HintStatus!]];
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
        var receivingPlayer = GetAlias(hint.ReceivingPlayer);
        var findingPlayer = GetAlias(hint.FindingPlayer);
        var item = ItemIdToItemName(hint.ItemId, hint.ReceivingPlayer);
        var location = LocationIdToLocationName(hint.LocationId, hint.FindingPlayer);
        var entrance = hint.Entrance.Trim() == "" ? "Vanilla" : hint.Entrance;

        return $"`{receivingPlayer}`'s __{item}__ is in `{findingPlayer}`'s world at **{location}**\n-# {entrance}";
    }

    public static string GetCopy(this HintPrintJsonPacket hint)
    {
        var receivingPlayer = GetAlias(hint.ReceivingPlayer);
        var item = ItemIdToItemName(hint.Item.Item, hint.ReceivingPlayer);
        var findingPlayerSlot = int.Parse(hint.Data[7].Text);
        var findingPlayer = GetAlias(findingPlayerSlot);
        var location = LocationIdToLocationName(long.Parse(hint.Data[5].Text), findingPlayerSlot);
        var entrance = hint.Data.Length == 11 ? "Vanilla" : hint.Data[9].Text;

        return $"`{receivingPlayer}`'s __{item}__ is in `{findingPlayer}`'s world at **{location}**\n-# {entrance}";
    }

    public static string GetItemLogCopy(this JsonMessagePart[] parts)
    {
        string item;
        string location;
        var firstPlayerSlot = int.Parse(parts[0].Text);
        var firstPlayer = GetAlias(firstPlayerSlot);
        var itemId = long.Parse(parts[2].Text);
        var locationId = long.Parse(parts[^2].Text);
        location = LocationIdToLocationName(locationId, firstPlayerSlot);

        if (parts[1].Text is " found their ")
        {
            item = ItemIdToItemName(itemId, firstPlayerSlot);

            return $"`{firstPlayer}` found their __{item}__ (**{location}**)";
        }

        var secondPlayerSlot = int.Parse(parts[4].Text);
        var secondPlayer = GetAlias(secondPlayerSlot);
        item = ItemIdToItemName(itemId, secondPlayerSlot);
        return $"`{firstPlayer}` sent __{item}__ to `{secondPlayer}` (**{location}**)";
    }

    public static string GetItemLogToHintId(this JsonMessagePart[] parts)
    {
        var firstPlayerSlot = int.Parse(parts[0].Text);
        var itemId = long.Parse(parts[2].Text);
        var locationId = long.Parse(parts[^2].Text);

        if (parts[1].Text is " found their ")
        {
            return $"{firstPlayerSlot},,{firstPlayerSlot},,{itemId},,{locationId}";
        }

        var secondPlayerSlot = int.Parse(parts[4].Text);
        return $"{secondPlayerSlot},,{firstPlayerSlot},,{itemId},,{locationId}";
    }
}
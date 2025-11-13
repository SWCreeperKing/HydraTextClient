using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Enums;
using ArchipelagoMultiTextClient.Scripts.LoginTab;
using CreepyUtil.Archipelago;
using CreepyUtil.Archipelago.ApClient;
using Godot;
using static System.StringComparison;
using static ArchipelagoMultiTextClient.Scripts.MainController;
using static ArchipelagoMultiTextClient.Scripts.SettingsTab.Settings;
using static ArchipelagoMultiTextClient.Scripts.TextClientTab.MessageSender;

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
    [Export] private CheckBox _ShowTimestamp;
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

    public static event Action<ApClient>? SelectedClientChangedEvent;

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
            if (input is InputEventJoypadMotion) return;
            if (input is InputEventJoypadButton) return;
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
            ChosenTextClient.Tags.SetTags(ArchipelagoTag.TextOnly, ArchipelagoTag.NoText);
            ChosenTextClient = null;
            ChosenTextClient = ActiveClients[(int)l];
            ChosenTextClient.Tags.SetTags(ArchipelagoTag.TextOnly, ArchipelagoTag.DeathLink, ArchipelagoTag.TrapLink);
            SelectedClientChangedEvent?.Invoke(ChosenTextClient);
            _LastSelected = l;
        };

        _Messages.MetaClicked += meta =>
        {
            var s = (string)meta;
            if (s.StartsWith("itemdialog"))
            {
                SetAndShowItemFilterDialogue(s);
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

        _ShowTimestamp.Pressed += () =>
        {
            Data.ShowTimestamp = _ShowTimestamp.ButtonPressed;
            RefreshText = true;
        };
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
        _ShowTimestamp.ButtonPressed = Data.ShowTimestamp;
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

            if (message.Sender == ItemLog) MultiworldName.CurrentWorld.ItemLogItemReceived(message.MessageParts);
            if (!Filter(message, _WasLastMessageHintLocation, out _WasLastMessageHintLocation)) continue;

            _ToScroll = _VScrollBar.Value >= _VScrollBar.MaxValue - _ScrollContainer.Size.Y;
            _Both.Enqueue(message);

            if (message.Sender == ItemLog) _ItemLog.Enqueue(message);
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

        if (message.Sender == Hint && message.MessageParts[^1].HintStatus is HintStatus.Found &&
            !Data.ShowFoundHints && !wasHintLocation) return false;
        if (message.Sender != ItemLog) return true;

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
using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using CreepyUtil.Archipelago;
using Godot;
using static ArchipelagoMultiTextClient.Scripts.MainController;

namespace ArchipelagoMultiTextClient.Scripts;

public partial class HintStatusChanger : Control
{
    [Export] private PlayerBox _PlayerBox;
    public bool RefreshUI;
    private HintChangerDialog _Dialog;
    private ApClient _Client;
    private List<HintStatusChangerRow> _Rows = [];

    public void Init(ApClient client, HintChangerDialog dialog)
    {
        _Dialog = dialog;
        _PlayerBox.PlayerName = client.PlayerName;
        _Client = client;
    }

    public override void _Process(double delta)
    {
        if (!RefreshUI) return;

        var receiverHints = _Client.Hints.Where(hint => hint.ReceivingPlayer == _Client.PlayerSlot)
                                   .OrderBy(hint => ItemIdToItemName(hint.ItemId, _Client.PlayerSlot))
                                   .OrderBy(hint => HintStatusNumber[hint.Status])
                                   .ToArray();
        
        for (var i = 0; i < Math.Max(receiverHints.Length, _Rows.Count); i++)
        {
            if (_Rows.Count == i)
            {
                HintStatusChangerRow row = new(this);
                _Rows.Add(row);
                _PlayerBox.AddNode(row.Container, false);
            }
            
            _Rows[i].UpdateData(receiverHints.Length <= i ? null : receiverHints[i], _Client.PlayerSlot);
        }

        RefreshUI = false;
    }

    public void ChangeHintPriority(Hint hint, HintStatus status)
    {
        _Dialog.Client = _Client;
        _Dialog.SetItemText(ItemIdToItemName(hint.ItemId, _Client.PlayerSlot), hint.LocationId, status);
        _Dialog.Show();
    }
}

public class HintStatusChangerRow
{
    public HBoxContainer Container = new();
    private OptionButton _Options = new();
    private Label _Label = new();
    private Hint? _Hint;

    public HintStatusChangerRow(HintStatusChanger changer)
    {
        foreach (var status in HintStatuses)
        {
            _Options.AddItem(HintStatusText[status]);
        }

        _Options.ItemSelected += i =>
        {
            if (_Hint is null) return;
            changer.ChangeHintPriority(_Hint, HintStatuses[i]);
        };
        
        Container.AddChild(_Label);
        Container.AddChild(_Options);
        Container.Alignment = BoxContainer.AlignmentMode.End;
    }
    
    public void UpdateData(Hint? hint, int playerSlot)
    {
        _Hint = hint;
        Container.Visible = hint is not null;
        if (hint is null) return;
        _Label.Text = ItemIdToItemName(hint.ItemId, playerSlot);
        _Options.Selected = Array.IndexOf(HintStatuses, hint.Status);
    }
}
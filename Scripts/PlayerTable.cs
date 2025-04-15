using System;
using System.Linq;
using Archipelago.MultiClient.Net.Enums;
using Godot;
using static ArchipelagoMultiTextClient.Scripts.MainController;

namespace ArchipelagoMultiTextClient.Scripts;

public partial class PlayerTable : RecyclingTable<PlayerItem, PlayerData>
{
    public static bool RefreshUI;
    public static PlayerData[] Datas = [];

    public override void _Process(double delta)
    {
        if (!RefreshUI) return;
        RefreshUI = false;
        if (ActiveClients.Count == 0)
        {
            UpdateData([]);
            return;
        }

        var client = ActiveClients[0];
        UpdateData(client.PlayerStates.Select((state, i)
                              => new PlayerData(i, client.PlayerNames[i], client.PlayerGames[i], state))
                         .ToHashSet());
    }

    protected override PlayerItem CreateRow() => new();
}

public partial class PlayerItem : RowItem<PlayerData>
{
    private Label _Slot = new();
    private Label _PlayerName = new();
    private Label _Game = new();
    private Label _Status = new();

    public PlayerItem()
    {
        SetTheme(_Slot);
        SetTheme(_PlayerName);
        SetTheme(_Game);
        SetTheme(_Status);
    }

    public override void RefreshData(PlayerData data)
    {
        _Slot.Text = data.PlayerSlot;
        _PlayerName.Text = data.PlayerName;
        _PlayerName.Modulate = PlayerColor(data.PlayerName);
        _Game.Text = data.PlayerGame;
        _Status.Text = data.PlayerStatus;
        _Status.Modulate =
            MainController.Data.ColorSettings[
                data.PlayerStatus switch
                {
                    "Disconnected" => "connection_disconnected",
                    "Connected" => "connection_connected",
                    "Ready" => "connection_ready",
                    "Playing" => "connection_playing",
                    "Goal" => "connection_goal"
                }
            ];
    }

    public override void SetVisibility(bool isVisible)
    {
        _Slot.Visible = isVisible;
        _PlayerName.Visible = isVisible;
        _Game.Visible = isVisible;
        _Status.Visible = isVisible;
    }

    public override void SetParent(GridContainer toParent)
    {
        toParent.AddChild(_Slot);
        toParent.AddChild(_PlayerName);
        toParent.AddChild(_Game);
        toParent.AddChild(_Status);
    }

    public void SetTheme(Label label)
    {
        label.AddThemeFontSizeOverride("font_size", 24);
        label.AddThemeFontOverride("font", MainController.Font);
        label.HorizontalAlignment = HorizontalAlignment.Center;
    }
}

public struct PlayerData(int slot, string name, string game, ArchipelagoClientState state) : IEquatable<PlayerData>
{
    public readonly string PlayerSlot = $"{slot}";
    public readonly string PlayerName = name;
    public readonly string PlayerGame = game;
    public string PlayerStatus = ConvertStatus(state);

    public void SetPlayerStatus(ArchipelagoClientState state) => PlayerStatus = ConvertStatus(state);

    public static string ConvertStatus(ArchipelagoClientState state)
        => Enum.GetName(state)!.Replace("Client", "").Replace("Unknown", "Disconnected");

    public bool Equals(PlayerData other)
    {
        return PlayerSlot == other.PlayerSlot && PlayerName == other.PlayerName && PlayerGame == other.PlayerGame && PlayerStatus == other.PlayerStatus;
    }

    public override bool Equals(object obj)
    {
        return obj is PlayerData other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(PlayerSlot, PlayerName, PlayerGame, PlayerStatus);
    }
}
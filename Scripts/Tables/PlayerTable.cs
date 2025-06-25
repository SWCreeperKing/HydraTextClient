using System;
using System.Linq;
using Archipelago.MultiClient.Net.Enums;
using Godot;
using static ArchipelagoMultiTextClient.Scripts.MainController;

namespace ArchipelagoMultiTextClient.Scripts.Tables;

public partial class PlayerTable : TextTable
{
    public static bool RefreshUI;
    public static PlayerData[] Datas = [];

    public override void _Process(double delta)
    {
        if (!RefreshUI) return;
        try
        {
            RefreshUI = false;
            if (ActiveClients.Count == 0)
            {
                UpdateData([]);
                return;
            }

            var client = ActiveClients[0];
            UpdateData(client.PlayerStates
                             .Select((state, i) => new PlayerData(i, client.PlayerGames[i], state).GetData())
                             .ToList());
        }
        catch (Exception e)
        {
            GD.PrintErr(e);
            RefreshUI = true;
        }
    }
}

public struct PlayerData(int slot, string game, ArchipelagoClientState state) : IEquatable<PlayerData>
{
    public readonly int PlayerSlot = slot;
    public readonly string PlayerName = GetAlias(slot);
    public readonly string PlayerGame = game;
    public string PlayerStatus = ConvertStatus(state);

    public void SetPlayerStatus(ArchipelagoClientState state) => PlayerStatus = ConvertStatus(state);

    public static string ConvertStatus(ArchipelagoClientState state)
        => Enum.GetName(state)!.Replace("Client", "").Replace("Unknown", "Disconnected");

    public bool Equals(PlayerData other)
    {
        return PlayerSlot == other.PlayerSlot && PlayerName == other.PlayerName && PlayerGame == other.PlayerGame &&
               PlayerStatus == other.PlayerStatus;
    }

    public override bool Equals(object obj) { return obj is PlayerData other && Equals(other); }

    public override int GetHashCode() { return HashCode.Combine(PlayerSlot, PlayerName, PlayerGame, PlayerStatus); }

    public string[] GetData()
    {
        var statusColor =
            Data[
                PlayerStatus switch
                {
                    "Disconnected" => "connection_disconnected",
                    "Connected" => "connection_connected",
                    "Ready" => "connection_ready",
                    "Playing" => "connection_playing",
                    "Goal" => "connection_goal",
                    _ => throw new ArgumentOutOfRangeException()
                }
            ];
        return
        [
            $"{PlayerSlot}", $"[color={PlayerColor(PlayerSlot).Hex}]{PlayerName}[/color]", PlayerGame,
            $"[color={statusColor.Hex}]{PlayerStatus}[/color]"
        ];
    }
}
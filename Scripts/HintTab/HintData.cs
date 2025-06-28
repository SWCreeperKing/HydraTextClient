using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using ArchipelagoMultiTextClient.Scripts.TextClientTab;
using Newtonsoft.Json;
using static ArchipelagoMultiTextClient.Scripts.MainController;

namespace ArchipelagoMultiTextClient.Scripts.HintTab;

public readonly struct HintData(Hint hint)
{
    public readonly string ReceivingPlayer = GetAlias(hint.ReceivingPlayer);
    public readonly int ReceivingPlayerSlot = hint.ReceivingPlayer;
    public readonly long ItemId = hint.ItemId;
    public readonly string Item = ItemIdToItemName(hint.ItemId, hint.ReceivingPlayer);
    public readonly ItemFlags ItemFlags = hint.ItemFlags;
    public readonly string FindingPlayer = Players[hint.FindingPlayer];
    public readonly int FindingPlayerSlot = hint.FindingPlayer;
    public readonly HintStatus HintStatus = hint.Status;
    public readonly string Location = LocationIdToLocationName(hint.LocationId, hint.FindingPlayer);
    public readonly long LocationId = hint.LocationId;
    public readonly string Entrance = hint.Entrance.Trim() == "" ? "Vanilla" : hint.Entrance;
    public readonly string GetCopy = hint.GetCopy();
    public readonly string Id = $"{hint.ReceivingPlayer},,{hint.FindingPlayer},,{hint.ItemId},,{hint.LocationId}";
    public readonly Hint RawHint = hint;

    public readonly string ItemUid = ItemFilter.MakeUidCode(hint.ItemId,
        ItemIdToItemName(hint.ItemId, hint.ReceivingPlayer),
        PlayerGames[hint.ReceivingPlayer], hint.ItemFlags);

    public string[] GetData()
    {
        var receivingPlayerColor = PlayerColor(ReceivingPlayerSlot).Hex;
        var metaString = SettingsTab.Settings.ItemFilterDialog.GetMetaString(Item, PlayerGames[ReceivingPlayerSlot], ItemId, ItemFlags);
        var itemColor = GetItemHexColor(ItemFlags, metaString);
        var itemBgColor = GetItemHexBgColor(ItemFlags, metaString);
        var findingPlayerColor = PlayerColor(FindingPlayerSlot).Hex;
        var hintColor = Data[HintStatusColor[HintStatus]].Hex;
        var locationColor = Data["location"].Hex;
        var entranceColor = Data[Entrance == "Vanilla" ? "entrance_vanilla" : "entrance"].Hex;

        var hintStatus = HintStatusText[HintStatus];
        if (PlayerSlots.ContainsKey(ReceivingPlayerSlot) && HintStatus is not HintStatus.Found)
        {
            hintStatus =
                $"[url=\"change&{ReceivingPlayer}&_&{FindingPlayerSlot}&_&{Item}&_&{itemColor}&_&{LocationId}\"]{hintStatus}[/url]";
        }

        return
        [
            $"[url=\"{GetCopy}\"]Copy[/url]",
            $"[color={receivingPlayerColor}]{ReceivingPlayer.Clean()}[/color]",
            $"[bgcolor={itemBgColor}][color={itemColor}][url=\"{metaString}\"]{Item.Clean()}[/url][/color][/bgcolor]",
            $"[color={findingPlayerColor}]{FindingPlayer.Clean()}[/color]",
            $"[color={hintColor}]{hintStatus}[/color]",
            $"[color={locationColor}]{Location.Clean()}[/color]",
            $"[color={entranceColor}]{Entrance.Clean()}[/color]"
        ];
    }
}
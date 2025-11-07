using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;

namespace ArchipelagoMultiTextClient.Scripts.TextClientTab;

public static class TextHelper
{
    public static string Clean(this string text) => text.Replace("[", "[lb]");
    public static string CleanRb(this string text) => text.Replace("]", "[rb]");
    public static string ReplaceB(this string text) => text.Replace("[", "<").Replace("]", ">");

    public static string GetCopy(this Hint hint)
    {
        var receivingPlayer = MainController.GetAlias(hint.ReceivingPlayer);
        var findingPlayer = MainController.GetAlias(hint.FindingPlayer);
        var item = MainController.ItemIdToItemName(hint.ItemId, hint.ReceivingPlayer);
        var location = MainController.LocationIdToLocationName(hint.LocationId, hint.FindingPlayer);
        var entrance = hint.Entrance.Trim() == "" ? "Vanilla" : hint.Entrance;

        return $"`{receivingPlayer}`'s __{item}__ is in `{findingPlayer}`'s world at **{location}**\n-# {entrance}";
    }

    public static string GetCopy(this HintPrintJsonPacket hint)
    {
        var receivingPlayer = MainController.GetAlias(hint.ReceivingPlayer);
        var item = MainController.ItemIdToItemName(hint.Item.Item, hint.ReceivingPlayer);
        var findingPlayerSlot = int.Parse(hint.Data[7].Text);
        var findingPlayer = MainController.GetAlias(findingPlayerSlot);
        var location = MainController.LocationIdToLocationName(long.Parse(hint.Data[5].Text), findingPlayerSlot);
        var entrance = hint.Data.Length == 11 ? "Vanilla" : hint.Data[9].Text;

        return $"`{receivingPlayer}`'s __{item}__ is in `{findingPlayer}`'s world at **{location}**\n-# {entrance}";
    }

    public static string GetItemLogCopy(this JsonMessagePart[] parts)
    {
        string item;
        string location;
        var firstPlayerSlot = int.Parse(parts[0].Text);
        var firstPlayer = MainController.GetAlias(firstPlayerSlot);
        var itemId = long.Parse(parts[2].Text);
        var locationId = long.Parse(parts[^2].Text);
        location = MainController.LocationIdToLocationName(locationId, firstPlayerSlot);

        if (parts[1].Text is " found their ")
        {
            item = MainController.ItemIdToItemName(itemId, firstPlayerSlot);

            return $"`{firstPlayer}` found their __{item}__ (**{location}**)";
        }

        var secondPlayerSlot = int.Parse(parts[4].Text);
        var secondPlayer = MainController.GetAlias(secondPlayerSlot);
        item = MainController.ItemIdToItemName(itemId, secondPlayerSlot);
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
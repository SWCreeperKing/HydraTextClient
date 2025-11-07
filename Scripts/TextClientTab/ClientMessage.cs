using System;
using System.Text;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using Godot;

namespace ArchipelagoMultiTextClient.Scripts.TextClientTab;

public enum MessageSender
{
    None,
    ItemLog,
    Server,
    Hint,
    DeathLink,
    TrapLink,
    Joined,
    Left,
    TagsChanged
}

public readonly struct ClientMessage(
    JsonMessagePart[] messageParts,
    MessageSender sender = MessageSender.None,
    ChatPrintJsonPacket chatPrintJsonPacket = null,
    string copyText = null)
{
    public readonly MessageSender Sender = sender;
    public readonly JsonMessagePart[] MessageParts = messageParts;
    public readonly ChatPrintJsonPacket ChatPacket = chatPrintJsonPacket;
    public readonly string CopyText = copyText;
    public readonly string TimeStamp = DateTime.Now.ToString("[HH:mm:ss]");

    public readonly bool IsHintRequest =
        chatPrintJsonPacket is not null && chatPrintJsonPacket.Message.StartsWith("!hint");

    public string GenString()
    {
        string color;
        var copyId = TextClient.CopyList.Count;

        StringBuilder messageBuilder = new();
        if (MainController.Data.ShowTimestamp)
        {
            messageBuilder.Append($"[color=darkgray]{TimeStamp}[/color] ");
        }

        if (ChatPacket is not null)
        {
            color = MainController.PlayerColor(ChatPacket.Slot);
            TextClient.CopyList.Add($"{MainController.GetAlias(ChatPacket.Slot)}: {ChatPacket.Message}");
            messageBuilder.Append(
                $"[color={color}][url=\"{copyId}\"]{MainController.GetAlias(ChatPacket.Slot, true)}[/url][/color]: {ChatPacket.Message.Clean()}");
            return messageBuilder.ToString();
        }

        switch (Sender)
        {
            case MessageSender.Server:
                messageBuilder.Append(
                    $"[color={MainController.Data["player_server"].Hex}][url=\"{copyId}\"]Server[/url][/color]: ");
                break;
            case MessageSender.DeathLink:
                messageBuilder.Append(
                    $"[color={MainController.Data["item_trap"].Hex}]💀[/color]  ");
                break;
            case MessageSender.TrapLink:
                messageBuilder.Append(
                    $"[color={MainController.Data["item_trap"].Hex}]🪤[/color]  ");
                break;
            case MessageSender.Joined:
                messageBuilder.Append(
                    $"[color={MainController.Data["item_progressive"].Hex}] →[/color] ");
                break;
            case MessageSender.Left:
                messageBuilder.Append(
                    $"[color={MainController.Data["item_trap"].Hex}]← [/color] ");
                break;
            case MessageSender.TagsChanged:
                messageBuilder.Append(
                    $"[color={MainController.Data["item_useful"].Hex}]←→[/color] ");
                break;
            case MessageSender.ItemLog:
            {
                var fontSize = MainController.Data.FontSizes["text_client"];
                var copyStyle = MainController.Data.ItemLogStyle switch
                {
                    0 => "",
                    1 => $"[img={fontSize}x{fontSize}]res://Assets/Images/UI/Copy.png[/img]",
                    2 => "[Copy] "
                };

                messageBuilder.Append(
                    $"[hint=\"Click to Copy\"][url=\"{copyId}\"]{copyStyle}[/url][/hint] ");
                TextClient.CopyList.Add(CopyText);
                break;
            }
        }

        for (var i = 0; i < MessageParts.Length; i++)
        {
            var part = MessageParts[i];
            switch (part.Type)
            {
                case JsonMessagePartType.PlayerId:
                    try
                    {
                        var slot = int.Parse(part.Text);
                        color = MainController.PlayerColor(slot);
                        messageBuilder.Append($"[color={color}]{MainController.GetAlias(slot, true)}[/color]");
                    }
                    catch
                    {
                        messageBuilder.Append($"[Unknown: [{part.Text}]]");
                        GD.Print(CopyText);
                    }

                    break;
                case JsonMessagePartType.ItemId:
                    var itemId = long.Parse(part.Text);
                    var game = MainController.PlayerGames is null ? "Unknown" :
                        MainController.PlayerGames.Length <= part.Player!.Value ? "Unknown" : MainController.PlayerGames[part.Player!.Value];
                    messageBuilder.Append(MainController.FormatItemColor(MainController.ItemIdToItemName(itemId, part.Player!.Value), game, itemId,
                        part.Flags!.Value, true));
                    break;
                case JsonMessagePartType.LocationId:
                    var location = MainController.LocationIdToLocationName(long.Parse(part.Text), part.Player!.Value);
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
                    var name = MainController.HintStatusText[(HintStatus)part.HintStatus!];
                    color = MainController.Data[MainController.HintStatusColor[(HintStatus)part.HintStatus!]];
                    messageBuilder.Append($"[color={color}]{name.Clean()}[/color]");
                    break;
                default:
                    var text = (part.Text ?? "").Clean();

                    if (Sender is MessageSender.Hint && i == 0)
                    {
                        messageBuilder.Append($"[url=\"{copyId}\"][Hint][/url]: ");
                        TextClient.CopyList.Add(CopyText);
                        break;
                    }

                    messageBuilder.Append(text);
                    if (Sender is MessageSender.Server or MessageSender.Joined or MessageSender.Left or MessageSender.TagsChanged or MessageSender.DeathLink or MessageSender.TrapLink)
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
        ], MessageSender.Hint, copyText: hint.GetCopy());
}
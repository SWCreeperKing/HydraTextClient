using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using ArchipelagoMultiTextClient.Scripts.TextClientTab;
using Newtonsoft.Json;

namespace ArchipelagoMultiTextClient.Scripts.HintTab;

public class HydraMultiworld(string hash)
{
    public string Hash = hash;
    public string Name = "Temporary (Unnamed) Multiworld";
    [JsonIgnore] public bool Changed;
    [JsonIgnore] public Dictionary<string, HintData> HintDatas = [];
    [JsonIgnore] public IEnumerable<Hint>? RawList = null;

    public delegate void HintChangedHandler(HintData old, HintData @new);
    public event HintChangedHandler? OnHintChanged;

    public string[] ItemOrder = [];

    public Hint[] HintData_RW
    {
        get => HintDatas.Select(data => data.Value.RawHint).ToArray();
        set => RawList = value;
    }

    public void WorldChosen()
    {
        if (RawList is null) return;

        HintDatas = RawList.Select(hint => new HintData(hint))
                           .ToDictionary(hd => hd.Id, hd => hd);
    }

    public void MergeHints(params HintData[] datas)
    {
        foreach (var data in datas)
        {
            if (HintDatas.TryGetValue(data.Id, out var value)) OnHintChanged?.Invoke(value, data);
            HintDatas[data.Id] = data;
        }
    }

    public void ItemLogItemReceived(JsonMessagePart[] parts)
    {
        var id = parts.GetItemLogToHintId();
        
        if (!HintDatas.TryGetValue(id, out var hintData)) return;
        var hint = hintData.RawHint;
        hint.Status = HintStatus.Found;
        HintDatas[id] = new HintData(hint);
        OnHintChanged?.Invoke(hintData, HintDatas[id]);
    }
}
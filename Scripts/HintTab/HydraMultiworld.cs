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
    public string Name = "None";
    [JsonIgnore] public bool Changed;
    [JsonIgnore] public Dictionary<string, HintData> HintDatas = [];
    [JsonIgnore] public IEnumerable<Hint>? RawList = null;

    public delegate void HintChangedHandler(HintData old, HintData @new);
    public event HintChangedHandler? OnHintChanged;

    public string[] ItemOrder = [];

    public string HintData_RW
    {
        get => string.Join('\n', HintDatas.Select(data => JsonConvert.SerializeObject(data.Value.RawHint)));
        set => RawList = value.Split('\n').Select(JsonConvert.DeserializeObject<Hint>);
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
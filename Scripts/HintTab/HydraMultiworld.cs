using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Models;
using Newtonsoft.Json;

namespace ArchipelagoMultiTextClient.Scripts.HintTab;

public class HydraMultiworld(string hash)
{
    public string Hash = hash;
    public string Name = "None";
    [JsonIgnore] public bool Changed;
    [JsonIgnore] public Dictionary<string, HintData> HintDatas = [];
    [JsonIgnore] public IEnumerable<Hint>? RawList = null;

    public string HintData_RW
    {
        get => string.Join('\n', HintDatas.Select(data => data.Value.Serialize));
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
            HintDatas[data.Id] = data;
        }
    }
}
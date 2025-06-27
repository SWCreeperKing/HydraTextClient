using System.Linq;
using ArchipelagoMultiTextClient.Scripts.HintTab;
using ArchipelagoMultiTextClient.Scripts.TextClientTab;
using Godot;
using static ArchipelagoMultiTextClient.Scripts.MainController;

namespace ArchipelagoMultiTextClient.Scripts.Settings;

public partial class ItemFilterer : TextTable
{
    public static bool RefreshUI = true;
    private static string Red = Colors.Red.ToHtml();
    private static string Green = Colors.Green.ToHtml();


    public override void _Ready()
    {
        MetaClicked += meta =>
        {
            var s = (string)meta;

            if (s.StartsWith("&&&"))
            {
                Data.ItemFilters.Remove(s[3..]);
                TextClientTab.TextClient.RefreshText = true;
                HintTable.RefreshUI = true;
                RefreshUI = true;
                return;
            }

            var hash = s[3..];
            var filter = Data.ItemFilters[hash];

            if (s.StartsWith("_&_"))
            {
                filter.IsSpecial = !filter.IsSpecial;
                HintTable.RefreshUI = true;
                TextClientTab.TextClient.RefreshText = true;
            }
            else if (s.StartsWith("&_&"))
            {
                filter.ShowInHintsTable = !filter.ShowInHintsTable;
                HintTable.RefreshUI = true;
            }
            else
            {
                filter.ShowInItemLog = !filter.ShowInItemLog;
                TextClientTab.TextClient.RefreshText = true;
            }

            RefreshUI = true;
        };
    }

    public override void _Process(double delta)
    {
        if (!RefreshUI) return;

        UpdateData(Data.ItemFilters.Values
                  .OrderBy(item => item.Game)
                  .ThenBy(item => HintTable.SortNumber(item.Flags))
                  .ThenBy(item => item.Name)
                  .Select(item =>
                   {
                       var color = GetItemHexColor(item.Flags, item.UidCode);
                       var bgColor = GetItemHexBgColor(item.Flags, item.UidCode);
                       var hash = item.UidCode;
                       return (string[])
                       [
                           $"[bgcolor={bgColor}][color={color}]{item.Name}[/color][/bgcolor]", item.Game,
                           GetShowHideText(hash, item.ShowInItemLog, false),
                           GetShowHideText(hash, item.ShowInHintsTable, true),
                           SpecialMarkText(hash, item.IsSpecial),
                           $"[url=\"&&&{hash}\"]Remove[/url]"
                       ];
                   })
                  .ToList());

        RefreshUI = false;
    }

    public string SpecialMarkText(string hash, bool option)
        => option
            ? $"[url=\"_&_{hash}\"][color={Red}]Unmark[/color][/url]"
            : $"[url=\"_&_{hash}\"][color={Green}]Mark[/color][/url]";

    public string GetShowHideText(string hash, bool option, bool isShowHint)
    {
        var meta = isShowHint ? $"&_&{hash}" : $"___{hash}";
        return option
            ? $"[url=\"{meta}\"][color={Red}]Hide[/color][/url]"
            : $"[url=\"{meta}\"][color={Green}]Show[/color][/url]";
    }
}
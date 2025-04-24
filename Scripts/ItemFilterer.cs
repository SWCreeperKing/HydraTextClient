using System;
using System.Linq;
using Godot;

namespace ArchipelagoMultiTextClient.Scripts;

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
                MainController.Data.ItemFilters.Remove(s[3..]);
                TextClient.RefreshText = true;
                HintTable.RefreshUI = true;
                RefreshUI = true;
                return;
            }
            
            var isShowHint = s.StartsWith("&_&");
            var hash = s[3..];
            var filter = MainController.Data.ItemFilters[hash];

            if (isShowHint)
            {
                filter.ShowInHintsTable = !filter.ShowInHintsTable;
                HintTable.RefreshUI = true;
            }
            else
            {
                filter.ShowInItemLog = !filter.ShowInItemLog;
                TextClient.RefreshText = true;
            }

            RefreshUI = true;
        };
    }

    public override void _Process(double delta)
    {
        if (!RefreshUI) return;

        UpdateData(MainController
                  .Data.ItemFilters.Values
                  .OrderBy(item => item.Game)
                  .ThenBy(item => HintTable.SortNumber(item.Flags))
                  .ThenBy(item => item.Name)
                  .Select(item =>
                   {
                       var color = MainController.GetItemHexColor(item.Flags);
                       var hash = item.UidCode;
                       return (string[])
                       [
                           $"[color={color}]{item.Name}[/color]", item.Game,
                           GetShowHideText(hash, item.ShowInItemLog, false),
                           GetShowHideText(hash, item.ShowInHintsTable, true),
                           $"[url=&&&{hash}]Remove[/url]"
                       ];
                   })
                  .ToList());

        RefreshUI = false;
    }

    public string GetShowHideText(string hash, bool option, bool isShowHint)
    {
        var meta = isShowHint ? $"&_&{hash}" : $"___{hash}";
        return option
            ? $"[url=\"{meta}\"][color={Red}]Hide[/color][/url]"
            : $"[url=\"{meta}\"][color={Green}]Show[/color][/url]";
    }
}
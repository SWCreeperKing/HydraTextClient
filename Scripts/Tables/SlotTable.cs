using System.Linq;
using static ArchipelagoMultiTextClient.Scripts.MainController;

namespace ArchipelagoMultiTextClient.Scripts.Tables;

public partial class SlotTable : TextTable
{
    public static bool RefreshUI;

    public override void _Ready()
    {
        Padding = 6;
        MetaClicked += v =>
        {
            var s = (string)v;
            var split = s.Split(' ');
            var playerName = string.Join(' ', split[1..]);
            switch (split[0])
            {
                case "connect":
                    ClientList[playerName].TryConnection();
                    break;
                case "disconnect":
                    ClientList[playerName].TryDisconnection();
                    break;
                case "delete":
                    Main.RemoveSlot(playerName);
                    break;
            }
        };
    }

    public override void _Process(double delta)
    {
        if (!RefreshUI) return;
        RefreshUI = false;
        UpdateData(ClientList.OrderBy(kv => kv.Key).Select(kv => kv.Value.GrabUI()).ToList());
    }
}
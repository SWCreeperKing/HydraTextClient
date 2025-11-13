using System.Collections.Generic;

namespace ArchipelagoMultiTextClient.Scripts.LoginTab;

public class GameData(string name)
{
    public string SlotName = name;
    public string GameName = null;
    public string PackPath = null;
    public List<string> StartupCommands = null;
}
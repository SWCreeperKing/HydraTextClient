using System.Collections.Generic;

namespace ArchipelagoMultiTextClient.Scripts.PopTracker;

public class Location
{
    private string _Name;
    private string _ParentId;
    private bool _ClearAsGroup = true;
    private string _ClosedImg;
    private string _OpenedImg;
    private int _ItemCount = 0;
    private int _ItemCleared = 0;
    private List<string> _HostedItems = [];
    private List<List<string>> _AccessRules;
    private List<List<string>> _VisibilityRues;
    private string _OverlayBackground;
    private string _Ref;
}
using System.Collections.Generic;

namespace ArchipelagoMultiTextClient.Scripts.PopTracker;

public class LocationSection
{
    protected string Name;
    protected string ParentId;
    protected bool ClearAsGroup = true;
    protected string ClosedImg;
    protected string OpenedImg;
    protected int ItemCount = 0;
    protected int ItemCleared = 0;
    protected List<string> HostedItems;
    protected List<List<string>> AccessRules;
    protected List<List<string>> VisibilityRules;
    protected string OverlayBackground;
    protected string Ref;

    public string getName() => Name;
    public List<List<string>> getAccessRules() => AccessRules;
    public List<List<string>> getVisibilityRules() => VisibilityRules;
    public int getItemCount() => ItemCount;
    public int getItemCleared() => ItemCleared;
    public string getClosedImage() => ClosedImg;
    public string getOpenedImage() => OpenedImg;
    public List<string> getHostedItems() => HostedItems;
    public string getOverlayBackground() => OverlayBackground;
    public string getParentId() => ParentId;
    public string getFullID() => $"{ParentId}/{Name}";
    public string getRef() => Ref;
}
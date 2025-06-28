using ArchipelagoMultiTextClient.Scripts.PrefabScripts;
using Godot;

namespace ArchipelagoMultiTextClient.Scripts.HintTab;

public partial class HintDragable : Control
{
    public static bool IsDragging;

    public override Variant _GetDragData(Vector2 atPosition)
    {
        IsDragging = true;
        return ((PanelText)GetParent()).Id;
    }

    public override void _Notification(int what)
    {
        if (what != NotificationDragEnd) return;
        IsDragging = false;
    }
}
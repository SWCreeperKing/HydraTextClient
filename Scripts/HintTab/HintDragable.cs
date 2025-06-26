using Godot;

namespace ArchipelagoMultiTextClient.Scripts.HintTab;

public partial class HintDragable : PanelContainer
{
    public static bool IsDragging;
    
    public override bool _CanDropData(Vector2 atPosition, Variant data) => true;

    public override Variant _GetDragData(Vector2 atPosition)
    {
        IsDragging = true;
        return 1;
    }

    public override void _Notification(int what)
    {
        if (what != NotificationDragEnd) return;
        IsDragging = false;
    }
}
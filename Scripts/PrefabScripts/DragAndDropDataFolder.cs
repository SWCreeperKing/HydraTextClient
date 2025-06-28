using Godot;

namespace ArchipelagoMultiTextClient.Scripts.PrefabScripts;

public partial class DragAndDropDataFolder : FoldableContainer
{
    [Export] private DragAndDropData _IndexFollower;

    public override void _Process(double delta)
    {
        var mouse = GetGlobalMousePosition();
        var indexRect = _IndexFollower.GetGlobalRect();
        var isMouseOut = mouse.X + 10 < indexRect.Position.X;
        _IndexFollower.Visible = HintDragable.IsDragging && !isMouseOut;

        if (!HintDragable.IsDragging) return;
        if (isMouseOut) return;
        if (indexRect.HasPoint(mouse)) return;

        var childrenCount = _ListContainer.GetChildren();
        var index = childrenCount.IndexOf(_IndexFollower);

        if (index == childrenCount.Count - 1 && mouse.Y > indexRect.Position.Y) return;
        if (index == 0 && mouse.Y < indexRect.Position.Y) return;
        var nextIndex = index + (mouse.Y > indexRect.Position.Y ? 1 : -1);
        var next = childrenCount[nextIndex];
        var hover = (Control)childrenCount.FirstOrDefault(child => ((Control)child).GetGlobalRect().HasPoint(mouse),
            null);

        if (hover is not null and not PanelText)
        {
            _IndexFollower.Visible = false;
            if (next == hover) return;
        }

        _ListContainer.MoveChild(_IndexFollower, nextIndex);
    }
}
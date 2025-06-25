using Godot;

namespace ArchipelagoMultiTextClient.Scripts.HintTab;

public partial class HintOrganizer : HSplitContainer
{
    [Export] private PanelContainer IndexFollower;
    [Export] private VBoxContainer ListContainer;

    public override void _Process(double delta)
    {
        var mouse = GetGlobalMousePosition();
        var indexRect = IndexFollower.GetGlobalRect();
        var isMouseOut = mouse.X + 10 < indexRect.Position.X;
        IndexFollower.Visible = HintDragable.IsDragging && !isMouseOut;

        if (!HintDragable.IsDragging) return;
        if (isMouseOut) return;
        if (indexRect.HasPoint(mouse)) return;

        var childrenCount = ListContainer.GetChildren();
        var index = childrenCount.IndexOf(IndexFollower);

        if (index == childrenCount.Count - 1 && mouse.Y > indexRect.Position.Y) return;
        if (index == 0 && mouse.Y < indexRect.Position.Y) return;

        if (mouse.Y > indexRect.Position.Y)
        {
            ListContainer.MoveChild(IndexFollower, index + 1);
        }
        else
        {
            ListContainer.MoveChild(IndexFollower, index - 1);
        }
    }
}
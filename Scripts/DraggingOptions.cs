using Godot;
using ArchipelagoMultiTextClient.Scripts;

public partial class DraggingOptions : PanelContainer
{
    [Export] private Label _Display;
    [Export] private CheckBox _Descending;
    [Export] private CheckBox _IsActive;
    public int DataIndex;
    public Node Parent;

    public bool IsDescending
    {
        get => _Descending.ButtonPressed;
        set
        {
            _Descending.ButtonPressed = value;
            HintTable.RefreshUI = true;
        }
    }

    public bool IsActive
    {
        get => _IsActive.ButtonPressed;
        set
        {
            _IsActive.ButtonPressed = value;
            HintTable.RefreshUI = true;
        }
    }
    
    public OptionData OptionData => MainController.Data.SortOrder[DataIndex];

    public override void _Ready()
    {
        Parent = GetParent();
        _IsActive.Pressed += () =>
        {
            OptionData.IsActive = IsActive;
            HintTable.RefreshUI = true;
        };
        _Descending.Pressed += () =>
        {
            OptionData.IsDescending = IsDescending;
            HintTable.RefreshUI = true;
        };
        _IsActive.ButtonPressed = OptionData.IsActive;
        _Descending.ButtonPressed = OptionData.IsDescending;
        _Display.Text = OptionData.Name;

        if (OptionData.Index != -1) return;
        OptionData.Index = GetIndex();
    }

    public override Variant _GetDragData(Vector2 atPosition)
    {
        SetDragPreview((Control)_Display.Duplicate());
        return this;
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data) => true;

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        var drag = (DraggingOptions)data;
        Parent.MoveChild(drag, OptionData.Index);
        Parent.MoveChild(this, drag.OptionData.Index);
        drag.OptionData.Index = drag.GetIndex();
        OptionData.Index = GetIndex();
        HintTable.RefreshUI = true;
    }
}

public class OptionData(string name, bool isDescending = false, bool isActive = false)
{
    public string Name = name;
    public bool IsDescending = isDescending;
    public bool IsActive = isActive;
    public int Index = -1;
}
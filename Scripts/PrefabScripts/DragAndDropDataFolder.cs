using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Godot;

namespace ArchipelagoMultiTextClient.Scripts.PrefabScripts;

public partial class DragAndDropDataFolder : FoldableContainer
{
    [Export] private DragAndDropData _IndexFollower;
    [Export] private DragAndDropData _ListContainer;
    [Export] private TextureRect _MoveHandle;
    [Export] private LineEdit _NameEdit;
    [Export] private Button _DeleteButton;
    private bool _IsEditable;
    private bool _IsDeletable;
    private bool _IsMovable;

    public Func<bool>? IsDragging = null;

    public int IndexOfFollower => _ListContainer.GetChildren().IndexOf(_IndexFollower);

    public void SetOnDropData(Action<DragAndDropDataFolder, string> action)
    {
        _ListContainer.OnDropData += (folder, s) => action(folder, s);
        _IndexFollower.OnDropData += (folder, s) => action(folder, s);
    }

    public override void _Ready()
    {
        _MoveHandle.Visible = false;
        _NameEdit.Visible = false;
        _DeleteButton.Visible = false;
        _ListContainer.CurrentFolder = this;
        _IndexFollower.CurrentFolder = this;
    }

    public override void _Process(double delta)
    {
        if (IsDragging is null)
        {
            GD.Print("WARNING: folder does not have is dragging");
            return;
        }
        
        var mouse = GetGlobalMousePosition();
        var indexRect = _IndexFollower.GetGlobalRect();
        var isMouseOut = mouse.X + 10 < indexRect.Position.X;
        _IndexFollower.Visible = IsDragging() && !isMouseOut;

        if (!IsDragging() || isMouseOut || indexRect.HasPoint(mouse)) return;

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

    public void MoveChildToFollower(Control node)
    {
        var parent = node.GetParent<Control>();
        if (parent != _ListContainer)
        {
            parent.RemoveChild(node);
            AddChildToList(node);
        }

        _ListContainer.MoveChild(node, IndexOfFollower);
    }

    public void AddChildToList(Control child) => _ListContainer.AddChild(child);

    public void ClearFolder()
    {
        foreach (var child in _ListContainer.GetChildren().Where(child => child is PanelText))
        {
            _ListContainer.RemoveChild(child);
            child.QueueFree();
        }
    }

    public string[] GetTextIdsInChildren()
    {
        return _ListContainer.GetChildren()
                             .Where(child => child is PanelText)
                             .Select(child => ((PanelText)child).Id)
                             .ToArray();
    }

    public void SetupNameEdit(Action<string> onTextChange)
    {
        if (_NameEdit.Visible) return;
        _NameEdit.TextChanged += s => onTextChange(s);
        _NameEdit.Visible = true;
    }

    public void SetupDeleteButton(Action pressed)
    {
        if (_DeleteButton.Visible) return;
        _DeleteButton.Pressed += pressed;
        _DeleteButton.Visible = true;
    }

    public void SetupMoveHandle(string scriptPath)
    {
        if (_MoveHandle.Visible) return;
        _MoveHandle.SetScript(GD.Load($"res://Scripts/{scriptPath}.cs"));
        _MoveHandle.Visible = true;
    }
}
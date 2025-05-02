using Godot;

namespace ArchipelagoMultiTextClient.Scripts;

public partial class PlayerBox : FoldableContainer
{
    [Export] private VBoxContainer _BoxContainer;
    [Export] private HFlowContainer _FlowContainer;

    public string PlayerName
    {
        get => Text;
        set => Text = value;
    }

    public void AddNode(Control node, bool appendToFlow)
    {
        if (appendToFlow)
        {
            _FlowContainer.AddChild(node);
            return;
        }

        _BoxContainer.AddChild(node);
    }
}
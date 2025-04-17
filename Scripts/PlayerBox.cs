using Godot;

public partial class PlayerBox : Control
{
    [Export] private Label _PlayerName;
    [Export] private VBoxContainer _BoxContainer;
    [Export] private HFlowContainer _FlowContainer;
    [Export] private Button _ShowContainers;

    public string PlayerName
    {
        get => _PlayerName.Text;
        set => _PlayerName.Text = value;
    }

    public override void _Ready()
    {
        _BoxContainer.Visible = false;
        _FlowContainer.Visible = false;
        _ShowContainers.Pressed += () =>
        {
            _BoxContainer.Visible = _FlowContainer.Visible = !_BoxContainer.Visible;
            _ShowContainers.Text = _BoxContainer.Visible ? "Hide" : "Show";
        };
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
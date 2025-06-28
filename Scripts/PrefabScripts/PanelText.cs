using Godot;

namespace ArchipelagoMultiTextClient.Scripts.PrefabScripts;

public partial class PanelText : PanelContainer
{
    private Vector2 MinPanel = new(175, 175);
    [Export] private RichTextLabel Label;
    [Export] private MarginContainer MarginContainer;
    public string Id;
    public string NormalText;
    public string SquareText;

    private bool _IsSquarePanel;
    public bool IsSquarePanel
    {
        get => _IsSquarePanel; 
        set
        {
            _IsSquarePanel = value;
            if (value)
            {
                Label.Text = SquareText;
                CustomMinimumSize = MinPanel;
                Label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
                Label.HorizontalAlignment = HorizontalAlignment.Center;
            }
            else
            {
                Label.Text = NormalText;
                CustomMinimumSize = Vector2.Zero;
                Label.AutowrapMode = TextServer.AutowrapMode.Off;
                Label.HorizontalAlignment = HorizontalAlignment.Left;
            }
        }
    }

    public void SetScript(string path) => MarginContainer.SetScript(GD.Load($"res://Scripts/{path}.cs"));
}
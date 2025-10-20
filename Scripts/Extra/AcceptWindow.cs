using Godot;

namespace ArchipelagoMultiTextClient.Scripts.Extra;

public partial class AcceptWindow : Window
{
    [Export] private RichTextLabel? _Label;
    [Export] private Button _Confirm;

    public override void _Ready()
    {
        _Confirm.Pressed += Reset;
        CloseRequested += Reset;
    }

    public string Text
    {
        get => _Label?.Text;
        set
        {
            if (_Label is null) return;
            _Label!.Text = value;
        }
    }

    public void SetAndShow(string title, string text)
    {
        Title = title;
        Text = text;
        Show();
    }
    
    public void Reset()
    {
        Title = "Notice";
        Text = "[information to notice]";
        Hide();
    }
}
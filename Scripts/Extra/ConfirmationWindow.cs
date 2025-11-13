using System;
using Godot;

namespace ArchipelagoMultiTextClient.Scripts.Extra;

public partial class ConfirmationWindow : Window
{
    [Export] private RichTextLabel _Label;
    [Export] private Button _Confirm;
    [Export] private Button _Decline;
    private Action? OnAction;

    public override void _Ready()
    {
        _Confirm.Pressed += () => ActionAndReset(true);
        _Decline.Pressed += () => ActionAndReset(false);
        CloseRequested += () => ActionAndReset(false);
    }

    public string Text
    {
        get => _Label.Text;
        set => _Label.Text = value;
    }

    public void SetAndShow(string title, string text, Action onAction)
    {
        Title = title;
        Text = text;
        OnAction = onAction;
        Show();
    }
    
    public void ActionAndReset(bool result)
    {
        if (result) OnAction?.Invoke();
        Title = "Confirmation";
        Text = "Are you sure?";
        OnAction = null;
        Hide();
    }

    public void SetConfirmDeclineText(string confirm, string decline)
    {
        _Confirm.Text = confirm;
        _Decline.Text = decline;
    }
}
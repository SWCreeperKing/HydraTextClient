using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace ArchipelagoMultiTextClient.Scripts.SettingsTab;

public partial class SizeManager : SpinBox
{
    [Export] private string Id;
    [Export] private int DefaultSize;
    [Export] private Array<Control> Nodes = [];

    private System.Collections.Generic.Dictionary<string, int> FontSizes => MainController.Data.FontSizes;

    public override void _Ready()
    {
        FontSizes.TryAdd(Id, DefaultSize);
        
        Value = FontSizes.GetValueOrDefault(Id, DefaultSize);

        ValueChanged += val =>
        {
            FontSizes[Id] = (int)val;
            UpdateFontSize();
        };
        UpdateFontSize();
    }

    public void UpdateFontSize()
    {
        var newSize = FontSizes[Id];
        foreach (var node in Nodes)
        {
            switch (node)
            {
                case RichTextLabel richTextLabel:
                    ReplaceOverride(richTextLabel, "bold_italic_font_size", newSize);
                    ReplaceOverride(richTextLabel, "italics_font_size", newSize);
                    ReplaceOverride(richTextLabel, "mono_font_size", newSize);
                    ReplaceOverride(richTextLabel, "normal_font_size", newSize);
                    ReplaceOverride(richTextLabel, "bold_font_size", newSize);
                    break;
                case Label label:
                    ReplaceOverride(label, "font_size", newSize);
                    break;
                case LineEdit lineEdit:
                    ReplaceOverride(lineEdit, "font_size", newSize);
                    break;
                case Button button:
                    ReplaceOverride(button, "font_size", newSize);
                    break;
            }
        }
    }

    public void ReplaceOverride(Control control, string name, int value)
    {
        if (control.HasThemeFontSizeOverride(name))
        {
            control.RemoveThemeFontSizeOverride(name);
        }

        control.AddThemeFontSizeOverride(name, value);
    }
}
using Godot;

namespace ArchipelagoMultiTextClient.Scripts;

public partial class RichToolTip : RichTextLabel
{
    
    public override GodotObject _MakeCustomTooltip(string forText)
    {
        if (forText == "") return null;
        PanelContainer container = new();
        
        MarginContainer margin = new();
        margin.AddThemeConstantOverride("margin_left", 3);
        margin.AddThemeConstantOverride("margin_top", 3);
        margin.AddThemeConstantOverride("margin_right", 3);
        margin.AddThemeConstantOverride("margin_bottom", 3);

        ColorRect panel = new(); 
        panel.Color = MainController.Data["tooltip_bgcolor"];
        
        Label tooltip = new();
        tooltip.Theme = MainController.GlobalTheme;
        tooltip.AddThemeFontSizeOverride("font_size", 18);
        tooltip.Text = forText;
        
        margin.AddChild(tooltip);
        container.AddChild(panel);
        container.AddChild(margin);
        
        return container;
    }
}
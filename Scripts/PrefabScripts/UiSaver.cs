using System;
using Godot;
using Godot.Collections;

namespace ArchipelagoMultiTextClient.Scripts.PrefabScripts;

public partial class UiSaver : Control
{
    [Export] private Dictionary<string, Control> Controls = [];

    public override void _Ready()
    {
        MainController.OnSave += SaveControls;
        LoadControls();
    }

    public void SaveControls()
    {
        foreach (var (id, control) in Controls)
        {
            switch (control)
            {
                case FoldableContainer fc:
                    Save(id, fc.Folded);
                    break;
                case SplitContainer sc:
                    Save(id, sc.SplitOffset);
                    break;
                case TabContainer tc:
                    Save(id, tc.CurrentTab);
                    break;
                case SpinBox sb:
                    Save(id, (float)sb.Value);
                    break;
                default:
                    GD.Print($"{control.GetType()} is not configured to save in UiSaver (can ignore if not dev)");
                    break;
            }
        }
    }

    public void LoadControls()
    {
        foreach (var (id, control) in Controls)
        {
            switch (control)
            {
                case FoldableContainer fc:
                    fc.Folded = Load(id, fc.Folded);
                    break;
                case SplitContainer sc:
                    sc.SplitOffset = (int)Load(id, sc.SplitOffset);
                    break;
                case SpinBox sb:
                    sb.Value = Load(id, (float)sb.Value);
                    break;
                case TabContainer tc:
                    try
                    {
                        tc.CurrentTab = (int)Load(id, 0);
                    }
                    catch
                    {
                        tc.CurrentTab = 0;
                    }
                    break;
                default:
                    GD.Print($"{control.GetType()} is not configured to save in UiSaver (can ignore if not dev)");
                    break;
            }
        }
    }

    public void AttachSave(Action save) => MainController.OnSave += () => save();
    public void AttachLoad(Action load) => MainController.OnSave += () => load();
    public void Save(string id, float data) => MainController.Data.UiSettingsSave[id] = data;
    public void Save(string id, int data) => MainController.Data.UiSettingsSave[id] = data;
    public void Save(string id, bool data) => MainController.Data.UiSettingsSave[id] = data ? 1 : 0;

    public float Load(string id, float def)
    {
        if (MainController.Data.UiSettingsSave.TryGetValue(id, out var val)) return val;
        return MainController.Data.UiSettingsSave[id] = def;
    }

    public bool Load(string id, bool def) => (int)Load(id, def ? 1 : 0) == 1;
}
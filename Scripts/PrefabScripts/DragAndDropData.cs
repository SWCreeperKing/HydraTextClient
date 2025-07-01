using Godot;

namespace ArchipelagoMultiTextClient.Scripts.PrefabScripts;

public partial class DragAndDropData : Control
{
    public delegate void DropDataHandler(DragAndDropDataFolder folder, string id);

    public event DropDataHandler? OnDropData;
    public DragAndDropDataFolder CurrentFolder;

    public override bool _CanDropData(Vector2 atPosition, Variant data) => true;
    public override void _DropData(Vector2 atPosition, Variant data) => OnDropData?.Invoke(CurrentFolder, (string)data);
}
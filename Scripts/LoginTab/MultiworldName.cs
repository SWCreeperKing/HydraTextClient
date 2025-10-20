using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ArchipelagoMultiTextClient.Scripts.HintTab;
using Godot;
using Newtonsoft.Json;
using static ArchipelagoMultiTextClient.Scripts.MainController;

namespace ArchipelagoMultiTextClient.Scripts.LoginTab;

public partial class MultiworldName : VBoxContainer
{
    public static HydraMultiworld CurrentWorld;

    [Export] private Label InfoLabel;
    [Export] private LineEdit NameEdit;
    [Export] private Button SaveButton;
    [Export] private Label NameLabel;
    private Dictionary<string, HydraMultiworld> AllWorlds = [];
    private Dictionary<string, HydraMultiworld> AllWorldsHash = [];

    public delegate void MultiworldChangedHandler(HydraMultiworld? newWorld);

    public static event MultiworldChangedHandler? OnMultiworldChanged;

    public override void _Ready()
    {
        OnSave += SaveWorlds;
        NameEdit.TextSubmitted += SubmitName;
        SaveButton.Pressed += () => SubmitName(NameEdit.Text);

        if (!Directory.Exists($"{SaveDir}/Multiworlds"))
        {
            Directory.CreateDirectory($"{SaveDir}/Multiworlds");
            return;
        }

        LoadWorlds();
    }

    public void ChangeState(MultiworldState state)
    {
        switch (state)
        {
            case MultiworldState.None:
                if (CurrentWorld is not null) SaveWorld(CurrentWorld);
                CurrentWorld = null;
                OnMultiworldChanged?.Invoke(null);
                InfoLabel.Visible = NameEdit.Visible = SaveButton.Visible = NameLabel.Visible = false;
                break;
            case MultiworldState.New:
                InfoLabel.Visible = NameEdit.Visible = SaveButton.Visible = true;
                NameLabel.Visible = false;
                break;
            case MultiworldState.Load:
                LoadName();
                break;
            case MultiworldState.Loaded:
                InfoLabel.Visible = NameEdit.Visible = SaveButton.Visible = false;
                NameLabel.Visible = true;
                break;
        }
    }

    public void SubmitName(string name)
    {
        CurrentWorld.Name = name;
        NameLabel.Text = $"Current Multiworld:\n{name}";
        NameEdit.Text = "";
        CurrentWorld.Changed = true;
        SetupMultiworld(CurrentWorld);
        ChangeState(MultiworldState.Loaded);
        SaveWorld(CurrentWorld);
    }

    public void LoadName()
    {
        if (CurrentWorld is not null) return;
        var info = ActiveClients[0].RoomState;
        var uuid =
            $"{info.Seed}{string.Join(",", ActiveClients[0].AllPlayers.Select(player => $"{player.Slot}{player.Name}{player.Game}"))}";
        uuid = string.Join(",", SHA256.HashData(Encoding.UTF8.GetBytes(uuid)));

        if (AllWorldsHash.TryGetValue(uuid, out CurrentWorld))
        {
            CurrentWorld.WorldChosen();
            NameLabel.Text = $"Current Multiworld:\n{CurrentWorld.Name}";
            OnMultiworldChanged?.Invoke(CurrentWorld);
            ChangeState(MultiworldState.Loaded);
            return;
        }

        CurrentWorld = new HydraMultiworld(uuid);
        OnMultiworldChanged?.Invoke(CurrentWorld);
        ChangeState(MultiworldState.New);
    }

    public void LoadWorlds()
    {
        foreach (var file in Directory.GetFiles($"{SaveDir}/Multiworlds"))
        {
            try
            {
                SetupMultiworld(JsonConvert.DeserializeObject<HydraMultiworld>(File.ReadAllText(file).Replace("\r", "")));
            }
            catch (Exception e)
            {
                GD.Print($"There was a problem loading a Multiworld: [{file}]\n{e}");
            }
        }
    }

    public void SetupMultiworld(HydraMultiworld world)
    {
        AllWorldsHash[world.Hash] = AllWorlds[world.Name] = world;
        world.OnHintChanged += (_, _) =>
        {
            HintTable.RefreshUI = true;
            HintOrganizer.RefreshUI = true;
        };
    }

    public void SaveWorlds()
    {
        foreach (var world in AllWorlds.Values) SaveWorld(world);
    }

    public void SaveWorld(HydraMultiworld world)
    {
        if (!world.Changed) return;
        File.WriteAllText($"{SaveDir}/Multiworlds/{world.Name}.json", JsonConvert.SerializeObject(world));
        GD.Print($"Saved: [{world.Name}]");   
    }
}

public enum MultiworldState
{
    None,
    Load,
    New,
    Loaded
}
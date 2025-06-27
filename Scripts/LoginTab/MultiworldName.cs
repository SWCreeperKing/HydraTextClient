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
    public static IEnumerable<HintData> Datas = [];
    
    [Export] private Label InfoLabel;
    [Export] private LineEdit NameEdit;
    [Export] private Button SaveButton;
    [Export] private Label NameLabel;
    private Dictionary<string, HydraMultiworld> AllWorlds = [];
    private Dictionary<string, HydraMultiworld> AllWorldsHash = [];

    public override void _Ready()
    {
        SaveCalled += (_, _) => SaveWorlds();
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
                CurrentWorld = null;
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
        AllWorldsHash[CurrentWorld.Hash] = AllWorlds[name] = CurrentWorld;
        CurrentWorld.Name = name;
        NameLabel.Text = $"Current Multiworld:\n{name}";
        NameEdit.Text = "";
        CurrentWorld.Changed = true;
        ChangeState(MultiworldState.Loaded);
    }

    public void LoadName()
    {
        if (CurrentWorld is not null) return;
        var info = ActiveClients[0].Session.RoomState;
        var uuid =
            $"{info.Seed}{string.Join(",", ActiveClients[0].Session.Players.AllPlayers.Select(player => $"{player.Slot}{player.Name}{player.Game}"))}";
        uuid = string.Join(",", SHA3_256.HashData(Encoding.UTF8.GetBytes(uuid)));
        
        if (AllWorldsHash.TryGetValue(uuid, out CurrentWorld))
        {
            CurrentWorld.WorldChosen();
            NameLabel.Text = $"Current Multiworld:\n{CurrentWorld.Name}";
            ChangeState(MultiworldState.Loaded);
            return;
        }

        CurrentWorld = new HydraMultiworld(uuid);
        ChangeState(MultiworldState.New);
    }

    public void LoadWorlds()
    {
        foreach (var file in Directory.GetFiles($"{SaveDir}/Multiworlds"))
        {
            try
            {
                var multiworld = File.ReadAllText(file);
                multiworld = multiworld.Replace("\r", "");
                var world = JsonConvert.DeserializeObject<HydraMultiworld>(multiworld);
                AllWorldsHash[world.Hash] = AllWorlds[world.Name] = world;
            }
            catch (Exception e)
            {
                GD.Print($"There was a problem loading a Multiworld: [{file}]\n{e}");
            }
        }
    }

    public void SaveWorlds()
    {
        foreach (var world in AllWorlds.Values.Where(world => world.Changed))
        {
            File.WriteAllText($"{SaveDir}/Multiworlds/{world.Name}.json", JsonConvert.SerializeObject(world));
        }
    }
}

public enum MultiworldState
{
    None,
    Load,
    New,
    Loaded
}
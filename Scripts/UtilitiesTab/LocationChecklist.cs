using System.Collections.Generic;
using System.IO;
using System.Linq;
using Archipelago.MultiClient.Net.DataPackage;
using Archipelago.MultiClient.Net.Helpers;
using Godot;
using static ArchipelagoMultiTextClient.Scripts.MainController;

namespace ArchipelagoMultiTextClient.Scripts.UtilitiesTab;

public partial class LocationChecklist : TabContainer
{
    [Export] private Label _Label;
    [Export] private LocationEdit _Edit;
    [Export] private OptionButton _Games;
    [Export] private Button _Button;
    [Export] private VBoxContainer _Right;
    [Export] private OptionButton _Presets;
    [Export] private Button _NewPreset;
    [Export] private Button _OpenPreset;
    [Export] private Button _EditPreset;
    [Export] private LineEdit _PresetName;
    [Export] private Button _CreatePreset;
    [Export] private Button _BackFromCreatePreset;
    [Export] private Button _BackFromOpenPreset;
    [Export] private Label _CreateError;
    [Export] private LocationTree _Tree;

    private string CurrentGame = "";
    private string[] GameChoices = [];
    private string[] PresetNames = [];
    private Dictionary<string, string[]> GamePresets = [];

    public override void _Ready()
    {
        CurrentTab = 0;
        LocationEdit.Checklist = this;

        if (!Directory.Exists($"{SaveDir}/Checklists")) Directory.CreateDirectory($"{SaveDir}/Checklists");
        else
        {
            foreach (var folder in Directory.GetDirectories($"{SaveDir}/Checklists"))
            {
                GamePresets[folder.Replace(@"\", "/").Split('/')[^1]] =
                    Directory.GetFiles(folder).Select(s => s.Replace(@"\", "/")).ToArray();
            }
        }

        _Right.Visible = false;

        ClientConnectEvent += client =>
        {
            if (GameChoices.Length != 0) return;
            GameChoices = client.PlayerGames.Skip(1).ToHashSet().Order().ToArray();
            foreach (var game in GameChoices) _Games.AddItem(game);
            _Games.Selected = GameChoices.ToList().IndexOf(client.PlayerGames[client.PlayerSlot]);
            ToggleGame();
        };

        ClientDisconnectEvent += _ =>
        {
            if (ActiveClients.Count != 0) return;
            GameChoices = [];
            _Games.Clear();
            if (CurrentGame != "") ToggleGame();
            if (!_Tree.Running) return;
            _Tree.Running = false;
            CurrentTab = 0;
        };

        _Button.Pressed += ToggleGame;
        _NewPreset.Pressed += () => CurrentTab = 2;
        _BackFromCreatePreset.Pressed += () =>
        {
            _PresetName.Text = "";
            _CreateError.Text = "";
            CurrentTab = 0;
        };
        _BackFromOpenPreset.Pressed += () =>
        {
            _Tree.Running = false;
            CurrentTab = 0;
        };
        _CreatePreset.Pressed += () =>
        {
            var name = _PresetName.Text.Trim().Replace("\t", " ").Replace(" ", "_");
            if (name == "")
            {
                _CreateError.Text = "You must provide a name for the preset";
                return;
            }

            if (PresetNames.Contains(name))
            {
                _CreateError.Text = "Preset already exists";
                return;
            }

            var file = $"{SaveDir}/Checklists/{CurrentGame}/{name}";
            File.Create(file).Dispose();
            GamePresets[CurrentGame] = GamePresets.TryGetValue(CurrentGame, out var value) ? [.. value, file] : [file];
            LoadGame();

            _PresetName.Text = "";
            _CreateError.Text = "";
            CurrentTab = 0;
        };
        _EditPreset.Pressed += () =>
        {
            _Edit.OpenFile(GamePresets[CurrentGame][_Presets.Selected]);
            CurrentTab = 1;
        };
        _OpenPreset.Pressed += () =>
        {
            _Tree.LoadList(GamePresets[CurrentGame][_Presets.Selected]);
            CurrentTab = 3;
        };
    }

    public void ToggleGame()
    {
        if (CurrentGame == "")
        {
            if (GameChoices.Length == 0) return;
            CurrentGame = GameChoices[_Games.Selected];
            _Label.Text = $"Selected Game: [{CurrentGame}]";
            _Games.Visible = false;
            _Games.Disabled = true;
            _Button.Text = "Unselect Game";
            _Right.Visible = true;

            LocationEdit.GameLocations = ActiveClients[0].Locations.Select(kv => kv.Key).ToArray();

            LoadGame();
        }
        else
        {
            CurrentGame = "";
            _Label.Text = "Game Selection";
            _Games.Visible = true;
            _Games.Disabled = false;
            _Button.Text = "Select Game";
            _Right.Visible = false;
        }
    }

    public void LoadGame()
    {
        _OpenPreset.Visible = false;
        _EditPreset.Visible = false;
        PresetNames = [];
        _Presets.Clear();
        if (!Directory.Exists($"{SaveDir}/Checklists/{CurrentGame}"))
            Directory.CreateDirectory($"{SaveDir}/Checklists/{CurrentGame}");
        else if (GamePresets[CurrentGame].Length > 0)
        {
            _OpenPreset.Visible = true;
            _EditPreset.Visible = true;
            PresetNames = GamePresets[CurrentGame].Select(s => s.Split('/')[^1]).Order().ToArray();
            foreach (var preset in PresetNames) _Presets.AddItem(preset);
        }
    }
}

/*
openfolder Overworld
    openfolder Overworld East
        location Overworld - [East] Pot near Slimes 1
            note slimes exist
    closefolder
    openfolder Overworld West
        location Overworld - [Northwest] Chest Near Turret
            note watch out for turret
        location overworldchestaft
    closefolder
closefolder
*/
using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ArchipelagoMultiTextClient.Scripts;
using ArchipelagoMultiTextClient.Scripts.Extra;
using Newtonsoft.Json;
using SpoilerSphereReader.Scripts;

// https://archipelago.gg/api/tracker/{id}
public partial class SpoilerReader : ScrollContainer
{
    public static SpoilerReader Singleton;

    [Export] private LineEdit RoomLink;
    [Export] private Button Browse;
    [Export] private FileDialog Dialog;
    [Export] private Button Connect;
    [Export] private Label Error;
    [Export] private HBoxContainer LoadContainer;
    [Export] private RichTextTable Display;

    public SpoilerLog? SpoilerLog;
    public Task GetDataTask;

    public override void _Ready()
    {
        Singleton = this;
        GetTree().GetRoot().FilesDropped += sArr =>
        {
            Error.Text = "";
            foreach (var file in sArr)
            {
                if (ReadFile(file)) return;
            }

            Error.Text = "No files matched the correct format";
        };

        Browse.Pressed += () => Dialog.Show();
        Dialog.FileSelected += s =>
        {
            Error.Text = "";
            if (ReadFile(s)) return;
            Error.Text = "File did not match the correct format";
        };

        Connect.Pressed += () =>
        {
            SetErrorText("");
            if (SpoilerLog is null || MainController.ActiveClients.Count == 0) return;
            GetDataTask = Task.Run(DownloadInfo);
        };

        LoadContainer.Visible = true;
        Display.Visible = false;
    }

    public async void DownloadInfo()
    {
        try
        {
            var urlSplit = RoomLink.Text.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (urlSplit[1] != MainController.Main.Address)
            {
                SetErrorText(
                    $"Host address ({urlSplit[1]}) and connected address ({MainController.Main.Address}) do not match"
                );
                return;
            }

            if (urlSplit[2] != "tracker")
            {
                SetErrorText("Url is not the tracker url, in the room click on multiworld tracker");
                return;
            }

            var id = urlSplit[3];
            var host = $"{urlSplit[0]}//{urlSplit[1]}";
            using HttpClient client = new();
            if (!TestPrintError(host, client.ConnectToHost(host))) return;
            SetErrorText("Connecting to host . . .");

            while (client.GetStatus() is HttpClient.Status.Connecting or HttpClient.Status.Resolving)
            {
                client.Poll();
                await Task.Delay(500);
            }

            if (client.GetStatus() is not HttpClient.Status.Connected)
            {
                SetErrorText("Failed to connect");
                return;
            }

            var headers = (string[])
            [
                "User-Agent: Pirulo/1.0 (Godot)",
                "Accept: */*"
            ];

            if (!TestPrintError(host, client.Request(HttpClient.Method.Get, $"/api/tracker/{id}", headers))) return;
            SetErrorText("Retrieving Data . . .");

            while (client.GetStatus() is HttpClient.Status.Requesting or HttpClient.Status.Resolving)
            {
                client.Poll();
                await Task.Delay(500);
            }

            if (client.GetStatus() is not (HttpClient.Status.Body or HttpClient.Status.Connected) ||
                !client.HasResponse())
            {
                SetErrorText($"Failed to retrieve Data | [{client.GetStatus()}]");
                return;
            }

            List<byte> rb = [];
            while (client.GetStatus() is HttpClient.Status.Body)
            {
                client.Poll();
                var chunk = client.ReadResponseBodyChunk();

                if (chunk.Length == 0) { await Task.Delay(500); }
                else { rb.AddRange(chunk); }
            }

            CallDeferred("ProcessData", Encoding.ASCII.GetString(rb.ToArray()));
            SetErrorText("");
            client.Close();
        }
        catch (Exception e) { GD.PrintErr(e); }
    }

    public bool TestPrintError(string host, Error error)
    {
        if (error is Godot.Error.Ok) return true;
        SetErrorText($"Host [{host}] responded: [{error}]");
        return false;
    }

    public void SetErrorText(string text)
    {
        Error.CallDeferred("set_text", text);
    }

    public override void _Process(double delta)
    {
        Connect.Disabled = GetDataTask is not null && !(GetDataTask.IsCanceled || GetDataTask.IsCompleted ||
                                                        GetDataTask.IsCompletedSuccessfully || GetDataTask.IsFaulted);

        Connect.Visible = SpoilerLog is not null && MainController.ActiveClients.Count != 0;
    }

    public void ProcessData(string data)
    {
        if (SpoilerLog is null) return;
        data = data.Replace("\r", "");
        var loadedData = JsonConvert.DeserializeObject<dynamic>(data);
        IEnumerable<dynamic> checksDone = loadedData.player_checks_done;

        Dictionary<int, int> playerMinsSpheres = [];
        Dictionary<int, string[]> unfinishedLocations = [];
        var playerItems = MainController.ActiveClients[0].PlayerGames.Select(((s, i) => (s, i))).ToDictionary(
            game =>  MainController.ActiveClients[0].PlayerNames[game.i], game =>
            {
                MainController.ActiveClients[0].GetLookups(game.s, out _, out var items);
                return items.Select(kv => kv.Key).ToArray();
            }
        );
        foreach (var player in checksDone)
        {
            int slot = player.player;
            var playerName = SpoilerLog.PlayerNames[slot - 1];
            var locationsDone =
                ((IEnumerable<object>)player.locations)
               .Select(l
                    => MainController.ActiveClients[0]
                                     .LocationIdToLocationName(
                                          long.Parse(l.ToString()!), slot
                                      )
                )
               .ToArray();
            
            var playerSpecificSpheres =
                SpoilerLog.SpoilerSpheres
                          .Select(sphere => sphere.PlayerLocations(playerName, playerItems)).ToArray();
            var index = 0;
            var finished = true;
            for (; index < playerSpecificSpheres.Length; index++)
            {
                var sphere = playerSpecificSpheres[index];
                if (sphere.Length == 0) continue;
                var unFinished = sphere.Where(loc => !locationsDone.Contains(loc)).ToArray();
                if (unFinished.Length == 0) continue;
                unfinishedLocations[slot - 1] = unFinished;
                finished = false;
                break;
            }

            playerMinsSpheres[slot] = finished ? -1 : SpoilerLog.SpoilerSpheres[index].SphereNumber;
        }

        LoadContainer.Visible = false;
        Display.Visible = true;
        Display.UpdateData(
            playerMinsSpheres
               .OrderBy(kv => kv.Value == -1 ? int.MaxValue : kv.Value)
               .ThenBy(kv => kv.Key)
               .Select(kv => (string[])
                    [
                        MainController.ActiveClients[0].PlayerNames[kv.Key],
                        kv.Value == -1 ? "Done" : $"Sphere [{kv.Value}]",
                        kv.Value == -1 ? "No Checks in Spheres"
                            : unfinishedLocations.TryGetValue(kv.Key - 1, out var location)
                                ? string.Join("\n ", location)
                                : "N/A",
                    ]
                )
               .ToList()
        );
    }

    public bool ReadFile(string file)
    {
        if (file.EndsWith(".zip"))
        {
            using var archive = ZipFile.OpenRead(file);
            var entries = archive.Entries;
            var spoiler = entries.FirstOrDefault(entry => entry.Name.EndsWith("_Spoiler.txt"), null);

            if (spoiler is not null) return ReadZipFile(spoiler);
            GD.Print("Zip has no Spoiler log");
            return false;
        }

        if (!file.EndsWith("_Spoiler.txt"))
        {
            GD.Print("Invalid File");
            return false;
        }

        GD.Print($"Try read: [{file}]");
        var log = new SpoilerLog(File.ReadAllText(file), out var succeeded);
        if (!succeeded) return false;
        SpoilerLog = log;
        return true;
    }

    public bool ReadZipFile(ZipArchiveEntry file)
    {
        using StreamReader reader = new(file.Open());
        var log = new SpoilerLog(reader.ReadToEnd(), out var succeeded);
        if (!succeeded) return false;
        SpoilerLog = log;
        return true;
    }
}
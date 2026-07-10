using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Godot;

namespace SpoilerSphereReader.Scripts;

public class SpoilerLog
{
    public static readonly Regex PlayerReg = new(@"Player (?:\d+): (.*)");
    public readonly Sphere[] SpoilerSpheres;
    public readonly string[] PlayerNames;
    
    public SpoilerLog(string fileData, out bool succeeded)
    {
        succeeded = false;
        
        var text_raw = fileData.Replace("\r", "");
        var text = text_raw.Split("\n");
        if (!text.Contains("Playthrough:"))
        {
            GD.Print("Invalid Playthrough");
            return;
        }

        PlayerNames = PlayerReg.Matches(text_raw).Select(match => match.Groups[1].Value).ToArray();
        var end = text.Contains("Paths:") ? Array.IndexOf(text, "Paths:") : text.Length;
        text = text[(Array.IndexOf(text, "Playthrough:") + 2)..end];

        List<Sphere> spoilLog = [];
        var sphere = 0;
        
        foreach (var line in text)
        {
            if (line == "}") continue;
            if (!line.StartsWith(' ') && line.EndsWith(": {"))
            {
                sphere = int.Parse(line[..^3]);
                if (sphere == 0) continue;
                spoilLog.Add(new Sphere(sphere));
                continue;
            } 
            if (sphere == 0) continue;
            
            spoilLog[^1].AddItem(PlayerNames, line);
        }
        
        SpoilerSpheres = spoilLog.ToArray();
        succeeded = true;
    }
}

public class Sphere(int num)
{
    public static readonly Regex SphereItemReg = new(@"^  (.+) \((.+)\): (.+) \((.+)\)$");
    public readonly List<SpoilerItem> SpoilerItems = [];
    public readonly int SphereNumber = num;

    public void AddItem(string[] players, string line)
    {
        var match = SphereItemReg.Match(line).Groups;
        
        var finder = match[2].Value;
        if (!players.Contains(finder))
        {
            GD.Print($"Incorrect line: [{line}], finder: [{finder}]");
            return;
        }
        
        var receiver = match[4].Value;
        if (!players.Contains(receiver))
        {
            GD.Print($"Incorrect line: [{line}], receiver: [{receiver}]");
            return;
        }
        
        var loc = match[1].Value;
        var item = match[3].Value;
        
        SpoilerItems.Add(new SpoilerItem(loc, finder, item, receiver));
    }

    public string[] PlayerLocations(string playerName, Dictionary<string, string[]> playerItems) => SpoilerItems.Where(item => item.Finder == playerName && playerItems[item.Receiver].Contains(item.Item)).Select(item => item.Location).ToArray();
}

public readonly struct SpoilerItem(string loc, string finder, string item, string receiver)
{
    public readonly string Location = loc;
    public readonly string Finder = finder;
    public readonly string Item = item;
    public readonly string Receiver = receiver;
}
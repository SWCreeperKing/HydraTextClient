using System;
using System.Collections.Generic;
using System.IO;
using Godot;

namespace ArchipelagoMultiTextClient.Scripts.UtilitiesTab;

public partial class LocationEdit : CodeEdit
{
    private Dictionary<string, string[][]> CodeCompltionDict = new()
    {
        ["openfolder"] = [["Open a Folder", "openfolder "]],
        ["closefolder"] = [["Close the Folder", "closefolder"]],
        ["location"] = [["Name of the Location", "location "]],
        ["note"] = [["Add a Note", "note "]]
    };

    public static LocationChecklist Checklist;
    public static string[] GameLocations = [];

    private string FileOpened = "";

    public override void _Ready()
    {
        AddCommentDelimiter("#", "", true);

        TextChanged += () => RequestCodeCompletion();
        CodeCompletionRequested += () =>
        {
            var textForCompletion = GetTextForCodeCompletion();
            var completionIndex = textForCompletion.Find((char)0xFFFF);
            var lastSpace = textForCompletion.RFind(" ", completionIndex);
            var lastTab = textForCompletion.RFind("\t", completionIndex);
            var lastNewline = textForCompletion.RFind("\n", completionIndex);
            var lastImportantCharacter = Math.Max(Math.Max(lastSpace, lastTab), lastNewline);

            var word = (lastImportantCharacter > 1
                ? textForCompletion.Substring(lastImportantCharacter + 1, completionIndex - lastImportantCharacter - 1)
                : textForCompletion[..completionIndex]).StripEdges();

            foreach (var (id, options) in CodeCompltionDict)
            {
                if (!word.StartsWith(id[..1], StringComparison.CurrentCultureIgnoreCase) ||
                    !word.IsSubsequenceOf(id, false)) continue;

                foreach (var option in options)
                {
                    AddCodeCompletionOption(CodeCompletionKind.PlainText, option[0], option[1]);
                }
            }

            if (lastSpace != -1 && textForCompletion.Substring(lastSpace - 8, 8) == "location")
            { 
                foreach (var location in GameLocations)
                {
                    if (!word.IsSubsequenceOf(location, false)) continue;
                    AddCodeCompletionOption(CodeCompletionKind.PlainText, location, location);
                }
            }

            UpdateCodeCompletionOptions(false);
        };
    }

    public void OpenFile(string file)
    {
        FileOpened = file;
        Text = File.ReadAllText(file).Replace("\r", "");
    }

    public void Save()
    {
        File.WriteAllText(FileOpened, Text);
    }

    public void SaveAndExit()
    {
        Save();
        FileOpened = "";
        Checklist.CurrentTab = 0;
    }
}
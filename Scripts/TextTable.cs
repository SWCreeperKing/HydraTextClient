using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using Godot.Collections;

namespace ArchipelagoMultiTextClient.Scripts;

public abstract partial class TextTable : RichTextLabel
{
    [Export] private Array<string> _Columns = [];

    // public void UpdateData(IEnumerable<string[]> data)
    public void UpdateData(List<string[]> data)
    {
        StringBuilder sb = new();
        sb.Append("[table=").Append(_Columns.Count).Append(']');

        foreach (var column in _Columns)
        {
            sb.Append("[cell bg=00000069] ").Append(column).Append(" [/cell]");
        }

        // for (var i = 0; i < data.Count(); i++)
        for (var i = 0; i < data.Count; i++)
        {
            // foreach (var item in data.ElementAt(i))
            foreach (var item in data[i])
            {
                if (i % 2 == 0)
                {
                    sb.Append("[cell padding=0,2,0,2] ").Append(item).Append(" [/cell]");
                }
                else
                {
                    sb.Append("[cell bg=00000044 padding=0,2,0,0] ").Append(item).Append(" [/cell]");
                }
            }
        }

        sb.Append("[/table]");

        Text = sb.ToString();
    }
}
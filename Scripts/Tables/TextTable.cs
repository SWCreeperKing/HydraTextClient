using System.Collections.Generic;
using System.Text;
using Godot;
using Godot.Collections;

namespace ArchipelagoMultiTextClient.Scripts;

public abstract partial class TextTable : RichTextLabel
{
    [Export] private Array<string> _Columns = [];
    public int Padding = 0;

    public void UpdateData(List<string[]> data)
    {
        StringBuilder sb = new();
        sb.Append("[table=").Append(_Columns.Count).Append(']');

        for (var i = 0; i < _Columns.Count; i++)
        {
            sb.Append("[cell bg=00000069] ").Append(GetColumnText(_Columns[i], i)).Append(" [/cell]");
        }

        for (var i = 0; i < data.Count; i++)
        {
            foreach (var item in data[i])
            {
                var extraPadding = i % 2 == 0 ? 0 : 3; 
                sb.Append(i % 2 == 0 ? "[cell padding=0," : "[cell bg=00000044 padding=0,")
                  .Append(Padding + extraPadding)
                  .Append(",0,")
                  .Append(Padding + extraPadding)
                  .Append("] ")
                  .Append(item)
                  .Append(" [/cell]");
            }
        }

        sb.Append("[/table]");

        Text = sb.ToString();
    }

    public virtual string GetColumnText(string columnText, int columnNum) => columnText;
}
using System;
using Godot;
using System.Collections.Generic;
using System.Linq;

public abstract partial class RecyclingTable<TRowItem, TGroupData> : GridContainer where TRowItem : RowItem<TGroupData>
{
    private List<TRowItem> Rows = [];

    public void UpdateData(HashSet<TGroupData> tableData)
    {
        var tableLimit = tableData.Count;
        for (var i = 0; i < Math.Max(Rows.Count, tableLimit); i++)
        {
            if (i >= tableLimit)
            {
                Rows[i].SetVisibility(false);
                continue;
            }
            
            if (Rows.Count == i)
            {
                var row = CreateRow();
                row.SetParent(this);
                Rows.Add(row);
            }
            
            for (var j = 0; j < Columns; j++)
            {
                Rows[i].SetVisibility(true);
                Rows[i].RefreshData(tableData.ElementAt(i));
            }
        }
    }
    
    protected abstract TRowItem CreateRow();
}

public abstract class RowItem<TGroupData>
{
    public abstract void RefreshData(TGroupData data);
    public abstract void SetVisibility(bool isVisible);
    public abstract void SetParent(GridContainer toParent);
}
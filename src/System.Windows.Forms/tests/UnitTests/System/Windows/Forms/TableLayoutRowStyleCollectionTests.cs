﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Forms.Tests;

public class TableLayoutRowStyleCollectionTests
{
    [WinFormsFact]
    public void TableLayoutRowStyleCollection_Add_RowStyle_Success()
    {
        using var toolStrip = new ToolStrip { LayoutStyle = ToolStripLayoutStyle.Table };
        TableLayoutSettings settings = Assert.IsType<TableLayoutSettings>(toolStrip.LayoutSettings);
        TableLayoutRowStyleCollection collection = settings.RowStyles;

        var style = new RowStyle();
        collection.Add(style);
        Assert.Equal(style, Assert.Single(collection));
    }

    [WinFormsFact]
    public void TableLayoutRowStyleCollection_Add_ColumnStyle_Success()
    {
        using var toolStrip = new ToolStrip { LayoutStyle = ToolStripLayoutStyle.Table };
        TableLayoutSettings settings = Assert.IsType<TableLayoutSettings>(toolStrip.LayoutSettings);
        TableLayoutRowStyleCollection collection = settings.RowStyles;

        var style = new ColumnStyle();
        collection.Add(style);
        Assert.Equal(style, Assert.Single(collection));
    }

    [WinFormsFact]
    public void TableLayoutRowStyleCollection_Insert_RowStyle_Success()
    {
        using var toolStrip = new ToolStrip { LayoutStyle = ToolStripLayoutStyle.Table };
        TableLayoutSettings settings = Assert.IsType<TableLayoutSettings>(toolStrip.LayoutSettings);
        TableLayoutRowStyleCollection collection = settings.RowStyles;

        var style = new RowStyle();
        collection.Insert(0, style);
        Assert.Equal(style, Assert.Single(collection));
    }

    [WinFormsFact]
    public void TableLayoutRowStyleCollection_Item_SetRowStyle_GetReturnsExpected()
    {
        using var toolStrip = new ToolStrip { LayoutStyle = ToolStripLayoutStyle.Table };
        TableLayoutSettings settings = Assert.IsType<TableLayoutSettings>(toolStrip.LayoutSettings);
        TableLayoutRowStyleCollection collection = settings.RowStyles;
        collection.Add(new RowStyle());

        var style = new RowStyle();
        collection[0] = style;
        Assert.Single(collection);
        Assert.Equal(style, collection[0]);
    }

    [WinFormsFact]
    public void TableLayoutRowStyleCollection_Item_GetNotRowStyle_ThrowsInvalidCastException()
    {
        using var toolStrip = new ToolStrip { LayoutStyle = ToolStripLayoutStyle.Table };
        TableLayoutSettings settings = Assert.IsType<TableLayoutSettings>(toolStrip.LayoutSettings);
        TableLayoutRowStyleCollection collection = settings.RowStyles;
        collection.Add(new ColumnStyle());
        Assert.Throws<InvalidCastException>(() => collection[0]);
    }

    [WinFormsFact]
    public void TableLayoutRowStyleCollection_Remove_RowStyle_Success()
    {
        using var toolStrip = new ToolStrip { LayoutStyle = ToolStripLayoutStyle.Table };
        TableLayoutSettings settings = Assert.IsType<TableLayoutSettings>(toolStrip.LayoutSettings);
        TableLayoutRowStyleCollection collection = settings.RowStyles;
        var style = new RowStyle();
        collection.Add(style);
        collection.Remove(style);
        Assert.Empty(collection);

        collection.Add(style);
        Assert.Equal(style, Assert.Single(collection));
    }

    [WinFormsFact]
    public void TableLayoutRowStyleCollection_Contains_RowStyle_ReturnsExpected()
    {
        using var toolStrip = new ToolStrip { LayoutStyle = ToolStripLayoutStyle.Table };
        TableLayoutSettings settings = Assert.IsType<TableLayoutSettings>(toolStrip.LayoutSettings);
        TableLayoutRowStyleCollection collection = settings.RowStyles;
        var style = new RowStyle();
        collection.Add(style);
        Assert.True(collection.Contains(style));
        Assert.False(collection.Contains(new RowStyle()));
        Assert.False(collection.Contains(null));
    }

    [WinFormsFact]
    public void TableLayoutRowStyleCollection_IndexOf_Invoke_ReturnsExpected()
    {
        using var toolStrip = new ToolStrip { LayoutStyle = ToolStripLayoutStyle.Table };
        TableLayoutSettings settings = Assert.IsType<TableLayoutSettings>(toolStrip.LayoutSettings);
        TableLayoutRowStyleCollection collection = settings.RowStyles;
        var style = new RowStyle();
        collection.Add(style);
        Assert.Equal(0, collection.IndexOf(style));
        Assert.Equal(-1, collection.IndexOf(new RowStyle()));
        Assert.Equal(-1, collection.IndexOf(null));
    }
}

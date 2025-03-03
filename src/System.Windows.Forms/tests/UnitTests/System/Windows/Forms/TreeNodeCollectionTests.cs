﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;

namespace System.Windows.Forms.Tests;

public class TreeNodeCollectionTests
{
    [WinFormsTheory]
    [NormalizedStringData]
    public void TreeNodeCollection_Add_String_Success(string text, string expectedText)
    {
        using var treeView = new TreeView();
        TreeNodeCollection collection = treeView.Nodes;
        TreeNode treeNode = collection.Add(text);
        Assert.Same(treeNode, collection[0]);
        Assert.Same(treeNode, Assert.Single(collection));
        Assert.Equal(Color.Empty, treeNode.BackColor);
        Assert.True(treeNode.Bounds.X > 0);
        Assert.Equal(0, treeNode.Bounds.Y);
        Assert.True(treeNode.Bounds.Width > 0);
        Assert.True(treeNode.Bounds.Height > 0);
        Assert.False(treeNode.Checked);
        Assert.Null(treeNode.ContextMenuStrip);
        Assert.Null(treeNode.FirstNode);
        Assert.Equal(Color.Empty, treeNode.ForeColor);
        Assert.Equal(expectedText, treeNode.FullPath);
        Assert.Equal(-1, treeNode.ImageIndex);
        Assert.Empty(treeNode.ImageKey);
        Assert.Equal(0, treeNode.Index);
        Assert.False(treeNode.IsEditing);
        Assert.False(treeNode.IsExpanded);
        Assert.False(treeNode.IsSelected);
        Assert.True(treeNode.IsVisible);
        Assert.Null(treeNode.LastNode);
        Assert.Equal(0, treeNode.Level);
        Assert.Empty(treeNode.Name);
        Assert.Null(treeNode.NextNode);
        Assert.Null(treeNode.NextVisibleNode);
        Assert.Null(treeNode.NodeFont);
        Assert.Empty(treeNode.Nodes);
        Assert.Same(treeNode.Nodes, treeNode.Nodes);
        Assert.Null(treeNode.Parent);
        Assert.Null(treeNode.PrevNode);
        Assert.Null(treeNode.PrevVisibleNode);
        Assert.Equal(-1, treeNode.SelectedImageIndex);
        Assert.Empty(treeNode.SelectedImageKey);
        Assert.Equal(-1, treeNode.StateImageIndex);
        Assert.Empty(treeNode.StateImageKey);
        Assert.Null(treeNode.Tag);
        Assert.Equal(expectedText, treeNode.Text);
        Assert.Empty(treeNode.ToolTipText);
        Assert.Same(treeView, treeNode.TreeView);
    }

    [WinFormsTheory]
    [InlineData(-1)]
    [InlineData(1)]
    public void TreeNodeCollection_Item_GetInvalidIndex_ThrowsArgumentOutOfRangeException(int index)
    {
        using var treeView = new TreeView();
        TreeNodeCollection collection = treeView.Nodes;
        collection.Add("text");
        Assert.Throws<ArgumentOutOfRangeException>("index", () => collection[index]);
    }

    [WinFormsTheory]
    [InlineData(-1)]
    [InlineData(1)]
    public void TreeNodeCollection_Item_SetInvalidIndex_ThrowsArgumentOutOfRangeException(int index)
    {
        using var treeView = new TreeView();
        TreeNodeCollection collection = treeView.Nodes;
        collection.Add("text");
        Assert.Throws<ArgumentOutOfRangeException>("index", () => collection[index] = new TreeNode());
    }

    [WinFormsFact]
    public void TreeNodeCollection_Item_SetNullTreeNode_ThrowsArgumentNullException()
    {
        using var treeView = new TreeView();
        TreeNodeCollection collection = treeView.Nodes;
        TreeNode node = new TreeNode("Node 0");
        collection.Add(node);
        Assert.Throws<ArgumentNullException>(() => collection[0] = null);
    }

    [WinFormsFact]
    public void TreeNodeCollection_Item_SetTreeNodeAlreadyAdded_Noop()
    {
        using var treeView = new TreeView();
        TreeNodeCollection collection = treeView.Nodes;
        TreeNode node = new TreeNode("Node 0");
        collection.Add(node);
        collection[0] = node;
        Assert.Equal(1, collection.Count);
    }

    [WinFormsFact]
    public void TreeNodeCollection_Item_SetExistentTreeNodeDifferentIndex_ThrowsArgumentException()
    {
        using var treeView = new TreeView();
        TreeNodeCollection collection = treeView.Nodes;
        collection.Add("Node 0");
        collection.Add("Node 1");
        TreeNode node = collection[0];
        Assert.Throws<ArgumentException>(() => collection[1] = node);
    }

    [WinFormsFact]
    public void TreeNodeCollection_Item_SetTreeNodeBoundToAnotherTreeView_ThrowsArgumentException()
    {
        using var anotherTreeView = new TreeView();
        anotherTreeView.Nodes.Add("Node 0");

        using var treeView = new TreeView();
        TreeNodeCollection collection = treeView.Nodes;
        collection.Add("Node 1");
        TreeNode nodeOfAnotherTreeView = anotherTreeView.Nodes[0];
        Assert.Throws<ArgumentException>(() => collection[0] = nodeOfAnotherTreeView);
    }

    [WinFormsFact]
    public void TreeNodeCollection_Item_SetTreeNodeReplacesExistingOne()
    {
        using var treeView = new TreeView();
        IntPtr forcedHandle = treeView.Handle;
        TreeNodeCollection collection = treeView.Nodes;
        collection.Add("Node 1");
        collection[0] = new TreeNode("New node 1");
        Assert.Equal(1, treeView._nodesByHandle.Count);
        Assert.Equal(1, collection.Count);
    }

    [WinFormsTheory]
    [InlineData("name2")]
    [InlineData("NAME2")]
    public void TreeNodeCollection_Find_InvokeKeyExists_ReturnsExpected(string key)
    {
        using var treeView = new TreeView();
        var child1 = new TreeNode
        {
            Name = "name1"
        };
        var child2 = new TreeNode
        {
            Name = "name2"
        };
        var child3 = new TreeNode
        {
            Name = "name2"
        };

        var grandchild1 = new TreeNode
        {
            Name = "name1"
        };
        var grandchild2 = new TreeNode
        {
            Name = "name2"
        };
        var grandchild3 = new TreeNode
        {
            Name = "name2"
        };
        child3.Nodes.Add(grandchild1);
        child3.Nodes.Add(grandchild2);
        child3.Nodes.Add(grandchild3);
        TreeNodeCollection collection = treeView.Nodes;
        collection.Add(child1);
        collection.Add(child2);
        collection.Add(child3);

        // Search all children.
        Assert.Equal(new TreeNode[] { child2, child3, grandchild2, grandchild3 }, collection.Find(key, searchAllChildren: true));

        // Call again.
        Assert.Equal(new TreeNode[] { child2, child3, grandchild2, grandchild3 }, collection.Find(key, searchAllChildren: true));

        // Don't search all children.
        Assert.Equal(new TreeNode[] { child2, child3 }, collection.Find(key, searchAllChildren: false));

        // Call again.
        Assert.Equal(new TreeNode[] { child2, child3 }, collection.Find(key, searchAllChildren: false));
    }

    [WinFormsTheory]
    [InlineData("NoSuchName")]
    [InlineData("abcd")]
    [InlineData("abcde")]
    [InlineData("abcdef")]
    public void TreeNodeCollection_Find_InvokeNoSuchKey_ReturnsEmpty(string key)
    {
        using var treeView = new TreeView();
        var child1 = new TreeNode
        {
            Name = "name1"
        };
        var child2 = new TreeNode
        {
            Name = "name2"
        };
        var child3 = new TreeNode
        {
            Name = "name2"
        };
        TreeNodeCollection collection = treeView.Nodes;
        collection.Add(child1);
        collection.Add(child2);
        collection.Add(child3);

        Assert.Empty(collection.Find(key, searchAllChildren: true));
        Assert.Empty(collection.Find(key, searchAllChildren: false));
    }

    [WinFormsTheory]
    [NullAndEmptyStringData]
    public void TreeNodeCollection_Find_NullOrEmptyKey_ThrowsArgumentNullException(string key)
    {
        using var treeView = new TreeView();
        var collection = treeView.Nodes;
        Assert.Throws<ArgumentNullException>("key", () => collection.Find(key, searchAllChildren: true));
        Assert.Throws<ArgumentNullException>("key", () => collection.Find(key, searchAllChildren: false));
    }
}

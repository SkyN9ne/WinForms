﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using static System.Windows.Forms.ToolStripDropDownButton;
using static Interop;

namespace System.Windows.Forms.Tests;

public class ToolStripDropDownButton_ToolStripDropDownButtonAccessibleObjectTests
{
    [WinFormsFact]
    public void ToolStripDropDownButtonAccessibleObject_Ctor_Default()
    {
        using ToolStripDropDownButton toolStripDropDownButton = new ToolStripDropDownButton();
        ToolStripDropDownButtonAccessibleObject accessibleObject = new ToolStripDropDownButtonAccessibleObject(toolStripDropDownButton);

        Assert.Equal(toolStripDropDownButton, accessibleObject.Owner);
    }

    [WinFormsFact]
    public void ToolStripDropDownButtonAccessibleObject_ControlType_IsButton_IfAccessibleRoleIsDefault()
    {
        using ToolStripDropDownButton toolStripDropDownButton = new ToolStripDropDownButton();
        // AccessibleRole is not set = Default

        object actual = toolStripDropDownButton.AccessibilityObject.GetPropertyValue(UiaCore.UIA.ControlTypePropertyId);

        Assert.Equal(UiaCore.UIA.ButtonControlTypeId, actual);
    }

    [WinFormsFact]
    public void ToolStripDropDownButtonAccessibleObject_Role_IsMenuItem_ByDefault()
    {
        using ToolStripDropDownButton toolStripDropDownButton = new ToolStripDropDownButton();
        // AccessibleRole is not set = Default

        AccessibleRole actual = toolStripDropDownButton.AccessibilityObject.Role;

        Assert.Equal(AccessibleRole.MenuItem, actual);
    }

    public static IEnumerable<object[]> ToolStripDropDownButtonAccessibleObject_GetPropertyValue_ControlType_IsExpected_ForCustomRole_TestData()
    {
        Array roles = Enum.GetValues(typeof(AccessibleRole));

        foreach (AccessibleRole role in roles)
        {
            if (role == AccessibleRole.Default)
            {
                continue; // The test checks custom roles
            }

            yield return new object[] { role };
        }
    }

    [WinFormsTheory]
    [MemberData(nameof(ToolStripDropDownButtonAccessibleObject_GetPropertyValue_ControlType_IsExpected_ForCustomRole_TestData))]
    public void ToolStripDropDownButtonAccessibleObject_GetPropertyValue_ControlType_IsExpected_ForCustomRole(AccessibleRole role)
    {
        using ToolStripDropDownButton toolStripDropDownButton = new ToolStripDropDownButton();
        toolStripDropDownButton.AccessibleRole = role;

        object actual = toolStripDropDownButton.AccessibilityObject.GetPropertyValue(UiaCore.UIA.ControlTypePropertyId);
        UiaCore.UIA expected = AccessibleRoleControlTypeMap.GetControlType(role);

        Assert.Equal(expected, actual);
    }

    [WinFormsFact]
    public void ToolStripDropDownButtonAccessibleObject_FragmentNavigate_Child_ReturnExpected()
    {
        using ToolStrip toolStrip = new();

        using ToolStripDropDownButton dropDownItem = new ToolStripDropDownButton();
        dropDownItem.DropDownItems.Add(string.Empty);

        toolStrip.Items.Add(dropDownItem);

        toolStrip.CreateControl();
        toolStrip.PerformLayout();

        AccessibleObject accessibleObject = toolStrip.Items[0].AccessibilityObject;

        Assert.Null(accessibleObject.FragmentNavigate(UiaCore.NavigateDirection.FirstChild));
        Assert.Null(accessibleObject.FragmentNavigate(UiaCore.NavigateDirection.LastChild));

        dropDownItem.DropDown.Show();

        AccessibleObject expected = dropDownItem.DropDown.AccessibilityObject;

        Assert.Equal(expected, accessibleObject.FragmentNavigate(UiaCore.NavigateDirection.FirstChild));
        Assert.Equal(expected, accessibleObject.FragmentNavigate(UiaCore.NavigateDirection.LastChild));
    }
}

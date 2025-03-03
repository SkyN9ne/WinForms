﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Comdlg32
    {
        [DllImport(Libraries.Comdlg32, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern BOOL PageSetupDlgW(ref PAGESETUPDLGW lppsd);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public unsafe struct PAGESETUPDLGW
        {
            public uint lStructSize;
            public IntPtr hwndOwner;
            public IntPtr hDevMode;
            public IntPtr hDevNames;
            public PSD Flags;
            public Point paperSize;
            public RECT rtMinMargin;
            public RECT rtMargin;
            public IntPtr hInstance;
            public IntPtr lCustData;
            public void* lpfnPageSetupHook;
            public void* lpfnPagePaintHook;
            public char* lpPageSetupTemplateName;
            public IntPtr hPageSetupTemplate;
        }
    }
}

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class ComCtl32
    {
        public struct NMSELCHANGE
        {
            public NMHDR nmhdr;
            public SYSTEMTIME stSelStart;
            public SYSTEMTIME stSelEnd;
        }
    }
}

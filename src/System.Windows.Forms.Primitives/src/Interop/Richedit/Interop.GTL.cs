﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal partial class Interop
{
    internal static partial class Richedit
    {
        [Flags]
        public enum GTL : uint
        {
            DEFAULT = 0,
            USECRLF = 1,
            PRECISE = 2,
            CLOSE = 4,
            NUMCHARS = 8,
            NUMBYTES = 16,
        }
    }
}

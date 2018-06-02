﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ZenCommon
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 0)]
    public unsafe struct STRUCT_ELEMENT
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
        public string id;
        public IntPtr ptr;
    }
}

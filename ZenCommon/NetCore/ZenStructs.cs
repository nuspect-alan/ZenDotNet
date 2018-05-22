using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ZenCommonNetCore
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct STRUCT_NODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
        public string id;
    }
}

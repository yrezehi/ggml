﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ggmlcs.Native.Binding.Entities
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct LlamaModelKvOverride
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string key;

        public LlamaModelKvOverrideType tag;

        [MarshalAs(UnmanagedType.Struct)]
        public LlamaModelKvOverrideValue value;
    }
}
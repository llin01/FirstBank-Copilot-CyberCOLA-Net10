using System.Runtime.InteropServices;

namespace Com.Cybersoft4u.CommonLib.NcccMessage
{
    /// <summary>
    /// Define Token ID C6 For EMV Status Token(Data Length 80 Byres))
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 80, CharSet = CharSet.Ansi)]
    public struct BicC6TokenStruct
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public byte[] XID;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] c6_arc;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] c6_sarc;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] c6_key;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] c6_cavvo;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] c6_un;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] c6_atn;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public byte[] c6_filler;
    }
}

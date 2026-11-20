using System.Runtime.InteropServices;

namespace Com.Cybersoft4u.CommonLib.NcccMessage
{
    /// <summary>
    /// Define Token ID CE For EMV Status Token(Data Length 202 Byres))
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 202, CharSet = CharSet.Ansi)]
    public struct BicCETokenStruct
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] authn_ind_flag;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 28)]
        public byte[] ce_28;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] ce_chip;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] ce_ctrl;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] ce_merh;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] ce_ind;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] ce_auth;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] ce_key;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] ce_tx;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public byte[] ce_mac;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public byte[] ce_filler;
    }
}

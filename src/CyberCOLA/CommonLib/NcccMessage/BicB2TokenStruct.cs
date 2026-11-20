using System.Runtime.InteropServices;

namespace Com.Cybersoft4u.CommonLib.NcccMessage
{
    /// <summary>
    /// Define Token ID B2 For EMV Request Data Token(Data Length 158 Byres))
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 158, CharSet = CharSet.Ansi)]
    public struct BicB2TokenStruct
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] bit_map;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] user_fld1;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] crypto_inf_data;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public byte[] tvr;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] arqc;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public byte[] amt_auth;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public byte[] amt_other;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] aip;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] atc;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] term_cntry_cde;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] term_crncy_cde;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] tran_dat;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] tran_type;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] unpredict_num;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] iss_appl_data_lgth;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] iss_appl_data;
    }
}

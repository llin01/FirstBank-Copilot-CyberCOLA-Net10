using System.Runtime.InteropServices;

namespace Com.Cybersoft4u.CommonLib.SMS
{
    /// <summary>
    /// Define SMS Outgoing Message Layout(Data Length 484 Byres))
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 484, CharSet = CharSet.Ansi)]
    public struct SMSOutgoingMessageStruct
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] identifier;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_1;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] mob_request;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_2;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 36)]
        public byte[] mob_key;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_3;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] mob_type;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_4;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public byte[] mob_msg_tp;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_5;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] mob_sel_risk;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_6;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] s2_mob_bank_cdt_1;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_7;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] s2_mob_bank_cdt_2;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_8;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] s2_mob_sub_yn;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_9;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 19)]
        public byte[] mob_card;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_10;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] mob_txdate_y4;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_11;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] mob_txdate_mm;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_12;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] mob_txdate_dd;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_13;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] mob_txhhmm;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_14;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] mob_open_status;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_15;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] mob_tx_amt;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_16;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] mob_birthday_yn;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_17;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] mob_mcc;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_18;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] mob_country;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_19;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
        public byte[] mob_aii;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_20;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
        public byte[] mob_merch_otp;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_21;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] mob_pos;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_22;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] mob_txmode;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_23;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_24;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] mob_branch_id;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_25;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] mob_mobile_aid;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_26;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] mob_mobile_sid;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_27;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 19)]
        public byte[] mob_sid;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_28;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] mob_time_limit;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_29;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 19)]
        public byte[] mob_aid_tpan;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_30;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] mob_from;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_31;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] mob_card_kind;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_32;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
        public byte[] mob_card_kind_nm;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_33;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        public byte[] mob_big_amt;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_34;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] mob_y4_b;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_35;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] mob_mm_b;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_36;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] mob_dd_b;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_37;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] mob_y4_e;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_38;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] mob_mm_e;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_39;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] mob_dd_e;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_40;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] mob_receive;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_41;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] mob_tmp;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_42;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] mob_foreign_yn;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_43;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public byte[] mob_101_1_amt_mm;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] mob_101_1_filler_1;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] mob_101_1_qty_mm;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] mob_101_1_filler_2;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public byte[] mob_101_1_amt_dd;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] mob_101_1_filler_3;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] mob_101_1_qty_dd;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] mob_101_1_filler_4;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public byte[] mob_101_2_amt_mm;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] mob_101_2_filler_1;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] mob_101_2_qty_mm;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] mob_101_2_filler_2;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public byte[] mob_101_2_amt_dd;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] mob_101_2_filler_3;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] mob_101_2_qty_dd;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] mob_101_2_filler_4;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public byte[] mob_101_3_amt_mm;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] mob_101_3_filler_1;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] mob_101_3_qty_mm;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] mob_101_3_filler_2;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public byte[] mob_101_3_amt_dd;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] mob_101_3_filler_3;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] mob_101_3_qty_dd;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] mob_101_3_filler_4;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] mob_aid_yn;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_44;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] push_aid_yn;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_45;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] mob_sid_yn;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_46;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] push_sid_yn;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_47;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] accu_id;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] filler_48;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
        public byte[] unuse;
    }
}

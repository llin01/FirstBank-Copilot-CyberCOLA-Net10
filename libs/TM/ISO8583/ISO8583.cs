// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ISO8583.cs" company="Cybersoft">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the FIELD type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583
{
    #region DEPENDENCY

    using Com.Cybersoft4u.CyberUtils.Exception;
    using Com.Cybersoft4u.CyberUtils.Utils;

    #endregion

    /// <summary>
    /// Enumeration of ISO 8583 data element fields.
    /// </summary>
    public enum FIELD
    {
        F_UKN,

        F_1,

        F_2,

        F_3,

        F_4,

        F_5,

        F_6,

        F_7,

        F_8,

        F_9,

        F_10,

        F_11,

        F_12,

        F_13,

        F_14,

        F_15,

        F_16,

        F_17,

        F_18,

        F_19,

        F_20,

        F_21,

        F_22,

        F_23,

        F_24,

        F_25,

        F_26,

        F_27,

        F_28,

        F_29,

        F_30,

        F_31,

        F_32,

        F_33,

        F_34,

        F_35,

        F_36,

        F_37,

        F_38,

        F_39,

        F_40,

        F_41,

        F_42,

        F_43,

        F_44,

        F_45,

        F_46,

        F_47,

        F_48,

        F_49,

        F_50,

        F_51,

        F_52,

        F_53,

        F_54,

        F_55,

        F_56,

        F_57,

        F_58,

        F_59,

        F_60,

        F_61,

        F_62,

        F_63,

        F_64,

        F_65,

        F_66,

        F_67,

        F_68,

        F_69,

        F_70,

        F_71,

        F_72,

        F_73,

        F_74,

        F_75,

        F_76,

        F_77,

        F_78,

        F_79,

        F_80,

        F_81,

        F_82,

        F_83,

        F_84,

        F_85,

        F_86,

        F_87,

        F_88,

        F_89,

        F_90,

        F_91,

        F_92,

        F_93,

        F_94,

        F_95,

        F_96,

        F_97,

        F_98,

        F_99,

        F_100,

        F_101,

        F_102,

        F_103,

        F_104,

        F_105,

        F_106,

        F_107,

        F_108,

        F_109,

        F_110,

        F_111,

        F_112,

        F_113,

        F_114,

        F_115,

        F_116,

        F_117,

        F_118,

        F_119,

        F_120,

        F_121,

        F_122,

        F_123,

        F_124,

        F_125,

        F_126,

        F_127,

        F_128,

        F_129,

        F_130,

        F_131,

        F_132,

        F_133,

        F_134,

        F_135,

        F_136,

        F_137,

        F_138,

        F_139,

        F_140,

        F_141,

        F_142,

        F_143,

        F_144,

        F_145,

        F_146,

        F_147,

        F_148,

        F_149,

        F_150,

        F_151,

        F_152,

        F_153,

        F_154,

        F_155,

        F_156,

        F_157,

        F_158,

        F_159,

        F_160,

        F_161,

        F_162,

        F_163,

        F_164,

        F_165,

        F_166,

        F_167,

        F_168,

        F_169,

        F_170,

        F_171,

        F_172,

        F_173,

        F_174,

        F_175,

        F_176,

        F_177,

        F_178,

        F_179,

        F_180,

        F_181,

        F_182,

        F_183,

        F_184,

        F_185,

        F_186,

        F_187,

        F_188,

        F_189,

        F_190,

        F_191,

        F_192
    }

    /// <summary>
    /// Interface for ISO 8583 message objects.
    /// </summary>
    public interface Iso8583 : ICodecable
    {
        /// <summary>Gets the core data of the message.</summary>
        Core Core();

        /// <summary>Gets the core data cast to a specific type.</summary>
        /// <typeparam name="TP">The target core type.</typeparam>
        TP Core<TP>() where TP : Core;

        /// <summary>Sets the core data of the message.</summary>
        Iso8583 Core(Core core);

        /// <summary>Gets the Message Type Indicator (MTI).</summary>
        string Mti();

        /// <summary>Sets the Message Type Indicator (MTI).</summary>
        Iso8583 Mti(string mti);

        /// <summary>Gets the message bitmap.</summary>
        byte[] Bitmap();

        /// <summary>Prepares the message for encoding.</summary>
        Iso8583 Prepare();

        /// <summary>Gets the string value of a field.</summary>
        string Val(FIELD field);

        /// <summary>Sets the string value of a field.</summary>
        Iso8583 Val(FIELD field, string value);

        /// <summary>Gets the raw object value of a field.</summary>
        object RawVal(FIELD field);

        /// <summary>Sets the raw object value of a field.</summary>
        Iso8583 RawVal(FIELD field, object value);

        /// <summary>Purges a field from the message.</summary>
        Iso8583 Purge(FIELD field);

        /// <summary>Purges all fields from the message.</summary>
        Iso8583 PurgeFields();

        /// <summary>Clears the message content.</summary>
        Iso8583 Clear();

        /// <summary>Gets the unique identity of the message.</summary>
        string Identity();
    }

    /// <summary>
    /// Helper methods for the <see cref="FIELD"/> enumeration.
    /// </summary>
    public static class FIELDMethods
    {
        /// <summary>
        /// Converts an integer value to a <see cref="FIELD"/> enum.
        /// </summary>
        /// <param name="val">The integer value.</param>
        /// <returns>The corresponding <see cref="FIELD"/>.</returns>
        public static FIELD Val(int val)
        {
            switch (val)
            {
                case -1:
                    return FIELD.F_UKN;
                case 1:
                    return FIELD.F_1;
                case 2:
                    return FIELD.F_2;
                case 3:
                    return FIELD.F_3;
                case 4:
                    return FIELD.F_4;
                case 5:
                    return FIELD.F_5;
                case 6:
                    return FIELD.F_6;
                case 7:
                    return FIELD.F_7;
                case 8:
                    return FIELD.F_8;
                case 9:
                    return FIELD.F_9;
                case 10:
                    return FIELD.F_10;
                case 11:
                    return FIELD.F_11;
                case 12:
                    return FIELD.F_12;
                case 13:
                    return FIELD.F_13;
                case 14:
                    return FIELD.F_14;
                case 15:
                    return FIELD.F_15;
                case 16:
                    return FIELD.F_16;
                case 17:
                    return FIELD.F_17;
                case 18:
                    return FIELD.F_18;
                case 19:
                    return FIELD.F_19;
                case 20:
                    return FIELD.F_20;
                case 21:
                    return FIELD.F_21;
                case 22:
                    return FIELD.F_22;
                case 23:
                    return FIELD.F_23;
                case 24:
                    return FIELD.F_24;
                case 25:
                    return FIELD.F_25;
                case 26:
                    return FIELD.F_26;
                case 27:
                    return FIELD.F_27;
                case 28:
                    return FIELD.F_28;
                case 29:
                    return FIELD.F_29;
                case 30:
                    return FIELD.F_30;
                case 31:
                    return FIELD.F_31;
                case 32:
                    return FIELD.F_32;
                case 33:
                    return FIELD.F_33;
                case 34:
                    return FIELD.F_34;
                case 35:
                    return FIELD.F_35;
                case 36:
                    return FIELD.F_36;
                case 37:
                    return FIELD.F_37;
                case 38:
                    return FIELD.F_38;
                case 39:
                    return FIELD.F_39;
                case 40:
                    return FIELD.F_40;
                case 41:
                    return FIELD.F_41;
                case 42:
                    return FIELD.F_42;
                case 43:
                    return FIELD.F_43;
                case 44:
                    return FIELD.F_44;
                case 45:
                    return FIELD.F_45;
                case 46:
                    return FIELD.F_46;
                case 47:
                    return FIELD.F_47;
                case 48:
                    return FIELD.F_48;
                case 49:
                    return FIELD.F_49;
                case 50:
                    return FIELD.F_50;
                case 51:
                    return FIELD.F_51;
                case 52:
                    return FIELD.F_52;
                case 53:
                    return FIELD.F_53;
                case 54:
                    return FIELD.F_54;
                case 55:
                    return FIELD.F_55;
                case 56:
                    return FIELD.F_56;
                case 57:
                    return FIELD.F_57;
                case 58:
                    return FIELD.F_58;
                case 59:
                    return FIELD.F_59;
                case 60:
                    return FIELD.F_60;
                case 61:
                    return FIELD.F_61;
                case 62:
                    return FIELD.F_62;
                case 63:
                    return FIELD.F_63;
                case 64:
                    return FIELD.F_64;
                case 65:
                    return FIELD.F_65;
                case 66:
                    return FIELD.F_66;
                case 67:
                    return FIELD.F_67;
                case 68:
                    return FIELD.F_68;
                case 69:
                    return FIELD.F_69;
                case 70:
                    return FIELD.F_70;
                case 71:
                    return FIELD.F_71;
                case 72:
                    return FIELD.F_72;
                case 73:
                    return FIELD.F_73;
                case 74:
                    return FIELD.F_74;
                case 75:
                    return FIELD.F_75;
                case 76:
                    return FIELD.F_76;
                case 77:
                    return FIELD.F_77;
                case 78:
                    return FIELD.F_78;
                case 79:
                    return FIELD.F_79;
                case 80:
                    return FIELD.F_80;
                case 81:
                    return FIELD.F_81;
                case 82:
                    return FIELD.F_82;
                case 83:
                    return FIELD.F_83;
                case 84:
                    return FIELD.F_84;
                case 85:
                    return FIELD.F_85;
                case 86:
                    return FIELD.F_86;
                case 87:
                    return FIELD.F_87;
                case 88:
                    return FIELD.F_88;
                case 89:
                    return FIELD.F_89;
                case 90:
                    return FIELD.F_90;
                case 91:
                    return FIELD.F_91;
                case 92:
                    return FIELD.F_92;
                case 93:
                    return FIELD.F_93;
                case 94:
                    return FIELD.F_94;
                case 95:
                    return FIELD.F_95;
                case 96:
                    return FIELD.F_96;
                case 97:
                    return FIELD.F_97;
                case 98:
                    return FIELD.F_98;
                case 99:
                    return FIELD.F_99;
                case 100:
                    return FIELD.F_100;
                case 101:
                    return FIELD.F_101;
                case 102:
                    return FIELD.F_102;
                case 103:
                    return FIELD.F_103;
                case 104:
                    return FIELD.F_104;
                case 105:
                    return FIELD.F_105;
                case 106:
                    return FIELD.F_106;
                case 107:
                    return FIELD.F_107;
                case 108:
                    return FIELD.F_108;
                case 109:
                    return FIELD.F_109;
                case 110:
                    return FIELD.F_110;
                case 111:
                    return FIELD.F_111;
                case 112:
                    return FIELD.F_112;
                case 113:
                    return FIELD.F_113;
                case 114:
                    return FIELD.F_114;
                case 115:
                    return FIELD.F_115;
                case 116:
                    return FIELD.F_116;
                case 117:
                    return FIELD.F_117;
                case 118:
                    return FIELD.F_118;
                case 119:
                    return FIELD.F_119;
                case 120:
                    return FIELD.F_120;
                case 121:
                    return FIELD.F_121;
                case 122:
                    return FIELD.F_122;
                case 123:
                    return FIELD.F_123;
                case 124:
                    return FIELD.F_124;
                case 125:
                    return FIELD.F_125;
                case 126:
                    return FIELD.F_126;
                case 127:
                    return FIELD.F_127;
                case 128:
                    return FIELD.F_128;
                case 129:
                    return FIELD.F_129;
                case 130:
                    return FIELD.F_130;
                case 131:
                    return FIELD.F_131;
                case 132:
                    return FIELD.F_132;
                case 133:
                    return FIELD.F_133;
                case 134:
                    return FIELD.F_134;
                case 135:
                    return FIELD.F_135;
                case 136:
                    return FIELD.F_136;
                case 137:
                    return FIELD.F_137;
                case 138:
                    return FIELD.F_138;
                case 139:
                    return FIELD.F_139;
                case 140:
                    return FIELD.F_140;
                case 141:
                    return FIELD.F_141;
                case 142:
                    return FIELD.F_142;
                case 143:
                    return FIELD.F_143;
                case 144:
                    return FIELD.F_144;
                case 145:
                    return FIELD.F_145;
                case 146:
                    return FIELD.F_146;
                case 147:
                    return FIELD.F_147;
                case 148:
                    return FIELD.F_148;
                case 149:
                    return FIELD.F_149;
                case 150:
                    return FIELD.F_150;
                case 151:
                    return FIELD.F_151;
                case 152:
                    return FIELD.F_152;
                case 153:
                    return FIELD.F_153;
                case 154:
                    return FIELD.F_154;
                case 155:
                    return FIELD.F_155;
                case 156:
                    return FIELD.F_156;
                case 157:
                    return FIELD.F_157;
                case 158:
                    return FIELD.F_158;
                case 159:
                    return FIELD.F_159;
                case 160:
                    return FIELD.F_160;
                case 161:
                    return FIELD.F_161;
                case 162:
                    return FIELD.F_162;
                case 163:
                    return FIELD.F_163;
                case 164:
                    return FIELD.F_164;
                case 165:
                    return FIELD.F_165;
                case 166:
                    return FIELD.F_166;
                case 167:
                    return FIELD.F_167;
                case 168:
                    return FIELD.F_168;
                case 169:
                    return FIELD.F_169;
                case 170:
                    return FIELD.F_170;
                case 171:
                    return FIELD.F_171;
                case 172:
                    return FIELD.F_172;
                case 173:
                    return FIELD.F_173;
                case 174:
                    return FIELD.F_174;
                case 175:
                    return FIELD.F_175;
                case 176:
                    return FIELD.F_176;
                case 177:
                    return FIELD.F_177;
                case 178:
                    return FIELD.F_178;
                case 179:
                    return FIELD.F_179;
                case 180:
                    return FIELD.F_180;
                case 181:
                    return FIELD.F_181;
                case 182:
                    return FIELD.F_182;
                case 183:
                    return FIELD.F_183;
                case 184:
                    return FIELD.F_184;
                case 185:
                    return FIELD.F_185;
                case 186:
                    return FIELD.F_186;
                case 187:
                    return FIELD.F_187;
                case 188:
                    return FIELD.F_188;
                case 189:
                    return FIELD.F_189;
                case 190:
                    return FIELD.F_190;
                case 191:
                    return FIELD.F_191;
                case 192:
                    return FIELD.F_192;
            }

            throw new InvalidValueException();
        }

        public static FIELD Val(string val)
        {
            switch (val)
            {
                case "-1":
                    return FIELD.F_UKN;
                case "1":
                    return FIELD.F_1;
                case "2":
                    return FIELD.F_2;
                case "3":
                    return FIELD.F_3;
                case "4":
                    return FIELD.F_4;
                case "5":
                    return FIELD.F_5;
                case "6":
                    return FIELD.F_6;
                case "7":
                    return FIELD.F_7;
                case "8":
                    return FIELD.F_8;
                case "9":
                    return FIELD.F_9;
                case "10":
                    return FIELD.F_10;
                case "11":
                    return FIELD.F_11;
                case "12":
                    return FIELD.F_12;
                case "13":
                    return FIELD.F_13;
                case "14":
                    return FIELD.F_14;
                case "15":
                    return FIELD.F_15;
                case "16":
                    return FIELD.F_16;
                case "17":
                    return FIELD.F_17;
                case "18":
                    return FIELD.F_18;
                case "19":
                    return FIELD.F_19;
                case "20":
                    return FIELD.F_20;
                case "21":
                    return FIELD.F_21;
                case "22":
                    return FIELD.F_22;
                case "23":
                    return FIELD.F_23;
                case "24":
                    return FIELD.F_24;
                case "25":
                    return FIELD.F_25;
                case "26":
                    return FIELD.F_26;
                case "27":
                    return FIELD.F_27;
                case "28":
                    return FIELD.F_28;
                case "29":
                    return FIELD.F_29;
                case "30":
                    return FIELD.F_30;
                case "31":
                    return FIELD.F_31;
                case "32":
                    return FIELD.F_32;
                case "33":
                    return FIELD.F_33;
                case "34":
                    return FIELD.F_34;
                case "35":
                    return FIELD.F_35;
                case "36":
                    return FIELD.F_36;
                case "37":
                    return FIELD.F_37;
                case "38":
                    return FIELD.F_38;
                case "39":
                    return FIELD.F_39;
                case "40":
                    return FIELD.F_40;
                case "41":
                    return FIELD.F_41;
                case "42":
                    return FIELD.F_42;
                case "43":
                    return FIELD.F_43;
                case "44":
                    return FIELD.F_44;
                case "45":
                    return FIELD.F_45;
                case "46":
                    return FIELD.F_46;
                case "47":
                    return FIELD.F_47;
                case "48":
                    return FIELD.F_48;
                case "49":
                    return FIELD.F_49;
                case "50":
                    return FIELD.F_50;
                case "51":
                    return FIELD.F_51;
                case "52":
                    return FIELD.F_52;
                case "53":
                    return FIELD.F_53;
                case "54":
                    return FIELD.F_54;
                case "55":
                    return FIELD.F_55;
                case "56":
                    return FIELD.F_56;
                case "57":
                    return FIELD.F_57;
                case "58":
                    return FIELD.F_58;
                case "59":
                    return FIELD.F_59;
                case "60":
                    return FIELD.F_60;
                case "61":
                    return FIELD.F_61;
                case "62":
                    return FIELD.F_62;
                case "63":
                    return FIELD.F_63;
                case "64":
                    return FIELD.F_64;
                case "65":
                    return FIELD.F_65;
                case "66":
                    return FIELD.F_66;
                case "67":
                    return FIELD.F_67;
                case "68":
                    return FIELD.F_68;
                case "69":
                    return FIELD.F_69;
                case "70":
                    return FIELD.F_70;
                case "71":
                    return FIELD.F_71;
                case "72":
                    return FIELD.F_72;
                case "73":
                    return FIELD.F_73;
                case "74":
                    return FIELD.F_74;
                case "75":
                    return FIELD.F_75;
                case "76":
                    return FIELD.F_76;
                case "77":
                    return FIELD.F_77;
                case "78":
                    return FIELD.F_78;
                case "79":
                    return FIELD.F_79;
                case "80":
                    return FIELD.F_80;
                case "81":
                    return FIELD.F_81;
                case "82":
                    return FIELD.F_82;
                case "83":
                    return FIELD.F_83;
                case "84":
                    return FIELD.F_84;
                case "85":
                    return FIELD.F_85;
                case "86":
                    return FIELD.F_86;
                case "87":
                    return FIELD.F_87;
                case "88":
                    return FIELD.F_88;
                case "89":
                    return FIELD.F_89;
                case "90":
                    return FIELD.F_90;
                case "91":
                    return FIELD.F_91;
                case "92":
                    return FIELD.F_92;
                case "93":
                    return FIELD.F_93;
                case "94":
                    return FIELD.F_94;
                case "95":
                    return FIELD.F_95;
                case "96":
                    return FIELD.F_96;
                case "97":
                    return FIELD.F_97;
                case "98":
                    return FIELD.F_98;
                case "99":
                    return FIELD.F_99;
                case "100":
                    return FIELD.F_100;
                case "101":
                    return FIELD.F_101;
                case "102":
                    return FIELD.F_102;
                case "103":
                    return FIELD.F_103;
                case "104":
                    return FIELD.F_104;
                case "105":
                    return FIELD.F_105;
                case "106":
                    return FIELD.F_106;
                case "107":
                    return FIELD.F_107;
                case "108":
                    return FIELD.F_108;
                case "109":
                    return FIELD.F_109;
                case "110":
                    return FIELD.F_110;
                case "111":
                    return FIELD.F_111;
                case "112":
                    return FIELD.F_112;
                case "113":
                    return FIELD.F_113;
                case "114":
                    return FIELD.F_114;
                case "115":
                    return FIELD.F_115;
                case "116":
                    return FIELD.F_116;
                case "117":
                    return FIELD.F_117;
                case "118":
                    return FIELD.F_118;
                case "119":
                    return FIELD.F_119;
                case "120":
                    return FIELD.F_120;
                case "121":
                    return FIELD.F_121;
                case "122":
                    return FIELD.F_122;
                case "123":
                    return FIELD.F_123;
                case "124":
                    return FIELD.F_124;
                case "125":
                    return FIELD.F_125;
                case "126":
                    return FIELD.F_126;
                case "127":
                    return FIELD.F_127;
                case "128":
                    return FIELD.F_128;
                case "129":
                    return FIELD.F_129;
                case "130":
                    return FIELD.F_130;
                case "131":
                    return FIELD.F_131;
                case "132":
                    return FIELD.F_132;
                case "133":
                    return FIELD.F_133;
                case "134":
                    return FIELD.F_134;
                case "135":
                    return FIELD.F_135;
                case "136":
                    return FIELD.F_136;
                case "137":
                    return FIELD.F_137;
                case "138":
                    return FIELD.F_138;
                case "139":
                    return FIELD.F_139;
                case "140":
                    return FIELD.F_140;
                case "141":
                    return FIELD.F_141;
                case "142":
                    return FIELD.F_142;
                case "143":
                    return FIELD.F_143;
                case "144":
                    return FIELD.F_144;
                case "145":
                    return FIELD.F_145;
                case "146":
                    return FIELD.F_146;
                case "147":
                    return FIELD.F_147;
                case "148":
                    return FIELD.F_148;
                case "149":
                    return FIELD.F_149;
                case "150":
                    return FIELD.F_150;
                case "151":
                    return FIELD.F_151;
                case "152":
                    return FIELD.F_152;
                case "153":
                    return FIELD.F_153;
                case "154":
                    return FIELD.F_154;
                case "155":
                    return FIELD.F_155;
                case "156":
                    return FIELD.F_156;
                case "157":
                    return FIELD.F_157;
                case "158":
                    return FIELD.F_158;
                case "159":
                    return FIELD.F_159;
                case "160":
                    return FIELD.F_160;
                case "161":
                    return FIELD.F_161;
                case "162":
                    return FIELD.F_162;
                case "163":
                    return FIELD.F_163;
                case "164":
                    return FIELD.F_164;
                case "165":
                    return FIELD.F_165;
                case "166":
                    return FIELD.F_166;
                case "167":
                    return FIELD.F_167;
                case "168":
                    return FIELD.F_168;
                case "169":
                    return FIELD.F_169;
                case "170":
                    return FIELD.F_170;
                case "171":
                    return FIELD.F_171;
                case "172":
                    return FIELD.F_172;
                case "173":
                    return FIELD.F_173;
                case "174":
                    return FIELD.F_174;
                case "175":
                    return FIELD.F_175;
                case "176":
                    return FIELD.F_176;
                case "177":
                    return FIELD.F_177;
                case "178":
                    return FIELD.F_178;
                case "179":
                    return FIELD.F_179;
                case "180":
                    return FIELD.F_180;
                case "181":
                    return FIELD.F_181;
                case "182":
                    return FIELD.F_182;
                case "183":
                    return FIELD.F_183;
                case "184":
                    return FIELD.F_184;
                case "185":
                    return FIELD.F_185;
                case "186":
                    return FIELD.F_186;
                case "187":
                    return FIELD.F_187;
                case "188":
                    return FIELD.F_188;
                case "189":
                    return FIELD.F_189;
                case "190":
                    return FIELD.F_190;
                case "191":
                    return FIELD.F_191;
                case "192":
                    return FIELD.F_192;
            }

            throw new InvalidValueException(new System.Exception());
        }

        public static int Val(FIELD f)
        {
            switch (f)
            {
                case FIELD.F_UKN:
                    return -1;
                case FIELD.F_1:
                    return 1;
                case FIELD.F_2:
                    return 2;
                case FIELD.F_3:
                    return 3;
                case FIELD.F_4:
                    return 4;
                case FIELD.F_5:
                    return 5;
                case FIELD.F_6:
                    return 6;
                case FIELD.F_7:
                    return 7;
                case FIELD.F_8:
                    return 8;
                case FIELD.F_9:
                    return 9;
                case FIELD.F_10:
                    return 10;
                case FIELD.F_11:
                    return 11;
                case FIELD.F_12:
                    return 12;
                case FIELD.F_13:
                    return 13;
                case FIELD.F_14:
                    return 14;
                case FIELD.F_15:
                    return 15;
                case FIELD.F_16:
                    return 16;
                case FIELD.F_17:
                    return 17;
                case FIELD.F_18:
                    return 18;
                case FIELD.F_19:
                    return 19;
                case FIELD.F_20:
                    return 20;
                case FIELD.F_21:
                    return 21;
                case FIELD.F_22:
                    return 22;
                case FIELD.F_23:
                    return 23;
                case FIELD.F_24:
                    return 24;
                case FIELD.F_25:
                    return 25;
                case FIELD.F_26:
                    return 26;
                case FIELD.F_27:
                    return 27;
                case FIELD.F_28:
                    return 28;
                case FIELD.F_29:
                    return 29;
                case FIELD.F_30:
                    return 30;
                case FIELD.F_31:
                    return 31;
                case FIELD.F_32:
                    return 32;
                case FIELD.F_33:
                    return 33;
                case FIELD.F_34:
                    return 34;
                case FIELD.F_35:
                    return 35;
                case FIELD.F_36:
                    return 36;
                case FIELD.F_37:
                    return 37;
                case FIELD.F_38:
                    return 38;
                case FIELD.F_39:
                    return 39;
                case FIELD.F_40:
                    return 40;
                case FIELD.F_41:
                    return 41;
                case FIELD.F_42:
                    return 42;
                case FIELD.F_43:
                    return 43;
                case FIELD.F_44:
                    return 44;
                case FIELD.F_45:
                    return 45;
                case FIELD.F_46:
                    return 46;
                case FIELD.F_47:
                    return 47;
                case FIELD.F_48:
                    return 48;
                case FIELD.F_49:
                    return 49;
                case FIELD.F_50:
                    return 50;
                case FIELD.F_51:
                    return 51;
                case FIELD.F_52:
                    return 52;
                case FIELD.F_53:
                    return 53;
                case FIELD.F_54:
                    return 54;
                case FIELD.F_55:
                    return 55;
                case FIELD.F_56:
                    return 56;
                case FIELD.F_57:
                    return 57;
                case FIELD.F_58:
                    return 58;
                case FIELD.F_59:
                    return 59;
                case FIELD.F_60:
                    return 60;
                case FIELD.F_61:
                    return 61;
                case FIELD.F_62:
                    return 62;
                case FIELD.F_63:
                    return 63;
                case FIELD.F_64:
                    return 64;
                case FIELD.F_65:
                    return 65;
                case FIELD.F_66:
                    return 66;
                case FIELD.F_67:
                    return 67;
                case FIELD.F_68:
                    return 68;
                case FIELD.F_69:
                    return 69;
                case FIELD.F_70:
                    return 70;
                case FIELD.F_71:
                    return 71;
                case FIELD.F_72:
                    return 72;
                case FIELD.F_73:
                    return 73;
                case FIELD.F_74:
                    return 74;
                case FIELD.F_75:
                    return 75;
                case FIELD.F_76:
                    return 76;
                case FIELD.F_77:
                    return 77;
                case FIELD.F_78:
                    return 78;
                case FIELD.F_79:
                    return 79;
                case FIELD.F_80:
                    return 80;
                case FIELD.F_81:
                    return 81;
                case FIELD.F_82:
                    return 82;
                case FIELD.F_83:
                    return 83;
                case FIELD.F_84:
                    return 84;
                case FIELD.F_85:
                    return 85;
                case FIELD.F_86:
                    return 86;
                case FIELD.F_87:
                    return 87;
                case FIELD.F_88:
                    return 88;
                case FIELD.F_89:
                    return 89;
                case FIELD.F_90:
                    return 90;
                case FIELD.F_91:
                    return 91;
                case FIELD.F_92:
                    return 92;
                case FIELD.F_93:
                    return 93;
                case FIELD.F_94:
                    return 94;
                case FIELD.F_95:
                    return 95;
                case FIELD.F_96:
                    return 96;
                case FIELD.F_97:
                    return 97;
                case FIELD.F_98:
                    return 98;
                case FIELD.F_99:
                    return 99;
                case FIELD.F_100:
                    return 100;
                case FIELD.F_101:
                    return 101;
                case FIELD.F_102:
                    return 102;
                case FIELD.F_103:
                    return 103;
                case FIELD.F_104:
                    return 104;
                case FIELD.F_105:
                    return 105;
                case FIELD.F_106:
                    return 106;
                case FIELD.F_107:
                    return 107;
                case FIELD.F_108:
                    return 108;
                case FIELD.F_109:
                    return 109;
                case FIELD.F_110:
                    return 110;
                case FIELD.F_111:
                    return 111;
                case FIELD.F_112:
                    return 112;
                case FIELD.F_113:
                    return 113;
                case FIELD.F_114:
                    return 114;
                case FIELD.F_115:
                    return 115;
                case FIELD.F_116:
                    return 116;
                case FIELD.F_117:
                    return 117;
                case FIELD.F_118:
                    return 118;
                case FIELD.F_119:
                    return 119;
                case FIELD.F_120:
                    return 120;
                case FIELD.F_121:
                    return 121;
                case FIELD.F_122:
                    return 122;
                case FIELD.F_123:
                    return 123;
                case FIELD.F_124:
                    return 124;
                case FIELD.F_125:
                    return 125;
                case FIELD.F_126:
                    return 126;
                case FIELD.F_127:
                    return 127;
                case FIELD.F_128:
                    return 128;
                case FIELD.F_129:
                    return 129;
                case FIELD.F_130:
                    return 130;
                case FIELD.F_131:
                    return 131;
                case FIELD.F_132:
                    return 132;
                case FIELD.F_133:
                    return 133;
                case FIELD.F_134:
                    return 134;
                case FIELD.F_135:
                    return 135;
                case FIELD.F_136:
                    return 136;
                case FIELD.F_137:
                    return 137;
                case FIELD.F_138:
                    return 138;
                case FIELD.F_139:
                    return 139;
                case FIELD.F_140:
                    return 140;
                case FIELD.F_141:
                    return 141;
                case FIELD.F_142:
                    return 142;
                case FIELD.F_143:
                    return 143;
                case FIELD.F_144:
                    return 144;
                case FIELD.F_145:
                    return 145;
                case FIELD.F_146:
                    return 146;
                case FIELD.F_147:
                    return 147;
                case FIELD.F_148:
                    return 148;
                case FIELD.F_149:
                    return 149;
                case FIELD.F_150:
                    return 150;
                case FIELD.F_151:
                    return 151;
                case FIELD.F_152:
                    return 152;
                case FIELD.F_153:
                    return 153;
                case FIELD.F_154:
                    return 154;
                case FIELD.F_155:
                    return 155;
                case FIELD.F_156:
                    return 156;
                case FIELD.F_157:
                    return 157;
                case FIELD.F_158:
                    return 158;
                case FIELD.F_159:
                    return 159;
                case FIELD.F_160:
                    return 160;
                case FIELD.F_161:
                    return 161;
                case FIELD.F_162:
                    return 162;
                case FIELD.F_163:
                    return 163;
                case FIELD.F_164:
                    return 164;
                case FIELD.F_165:
                    return 165;
                case FIELD.F_166:
                    return 166;
                case FIELD.F_167:
                    return 167;
                case FIELD.F_168:
                    return 168;
                case FIELD.F_169:
                    return 169;
                case FIELD.F_170:
                    return 170;
                case FIELD.F_171:
                    return 171;
                case FIELD.F_172:
                    return 172;
                case FIELD.F_173:
                    return 173;
                case FIELD.F_174:
                    return 174;
                case FIELD.F_175:
                    return 175;
                case FIELD.F_176:
                    return 176;
                case FIELD.F_177:
                    return 177;
                case FIELD.F_178:
                    return 178;
                case FIELD.F_179:
                    return 179;
                case FIELD.F_180:
                    return 180;
                case FIELD.F_181:
                    return 181;
                case FIELD.F_182:
                    return 182;
                case FIELD.F_183:
                    return 183;
                case FIELD.F_184:
                    return 184;
                case FIELD.F_185:
                    return 185;
                case FIELD.F_186:
                    return 186;
                case FIELD.F_187:
                    return 187;
                case FIELD.F_188:
                    return 188;
                case FIELD.F_189:
                    return 189;
                case FIELD.F_190:
                    return 190;
                case FIELD.F_191:
                    return 191;
                case FIELD.F_192:
                    return 192;
            }

            throw new InvalidTypeException(new System.Exception());
        }
    }
}

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OutField_44.cs" company="Cybersoft">
//   Author: Kaven
// </copyright>
// <summary>
//   Defines the OutField_44 type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.TM.VISA.Converter.OutField
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.MCOLA;
    using Com.Cybersoft4u.TM.ISO8583.Nccc;
    using Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens;

    #endregion

    internal class OutField_44 : ConvertUnitBase
    {
        protected internal static void ToVisaF44(VISA origin, MCOLA target)
        {
            try
            {
                var token = new TokenGroup();

                if (!ObjectUtils.IsNull(target.RawVal(FIELD.F_63)))
                {
                    token.Decode(target.RawVal(FIELD.F_63).ToString());
                }
                else
                {
                    return;
                }

                Token04 token63_04 = null;
                try
                {
                    token63_04 = (Token04)token.Tokens["04"];
                }
                catch (Exception)
                {
                    WriteLogCodecFail(typeof(OutField_44), string.Format("OutField_44(): Token 04 cannot found."));
                }

                TokenC0 token63_C0 = null;
                try
                {
                    token63_C0 = (TokenC0)token.Tokens["C0"];
                }
                catch (Exception)
                {
                    WriteLogCodecFail(typeof(OutField_44), string.Format("OutField_44(): Token C0 cannot found."));
                }

                TokenB4 token63_B4 = null;
                try
                {
                    token63_B4 = (TokenB4)token.Tokens["B4"];
                }
                catch (Exception)
                {
                    WriteLogCodecFail(typeof(OutField_44), string.Format("OutField_44(): Token B4 cannot found."));
                }

                var visaF44 = string.Empty;
                var visaF44Length = 0;
                if (ObjectUtils.IsNull(origin.RawVal(FIELD.F_44)))
                {
                    visaF44 = string.Empty.PadLeft(24, ' ');
                }
                else
                {
                    visaF44 = origin.RawVal(FIELD.F_44).ToString().PadLeft(24, ' ');
                    visaF44Length = origin.RawVal(FIELD.F_44).ToString().Length;
                }

                var f44_1 = visaF44.Substring(0, 1);
                var f44_2 = visaF44.Substring(1, 1);
                var f44_3 = visaF44.Substring(2, 1);
                var f44_4 = visaF44.Substring(3, 1);
                var f44_5 = visaF44.Substring(4, 1);
                var f44_6 = visaF44.Substring(5, 2);
                var f44_7 = visaF44.Substring(7, 1);
                var f44_8 = visaF44.Substring(8, 1);
                var f44_9 = visaF44.Substring(9, 1);
                var f44_10 = visaF44.Substring(10, 1);
                var f44_11 = visaF44.Substring(11, 2);
                var f44_12 = visaF44.Substring(13, 1);
                var f44_13 = visaF44.Substring(14, 1);
                var f44_14 = visaF44.Substring(15, 4);
                var f44_15 = visaF44.Substring(19, 4);
                var f44_16 = visaF44.Substring(23, 1);

                // 44.10
                var isF44_10 = false;
                if (!ObjectUtils.IsNull(token63_04) && !ObjectUtils.IsNull(token63_C0))
                {
                    var i = 0;
                    if (int.TryParse(token63_C0.CvdFld, out i))
                    {
                        switch (token63_C0.CvdFldPresent)
                        {
                            case "1":
                            case "2":
                                switch (token63_04.CrdVeryFlg)
                                {
                                    case "D":
                                        f44_10 = "N";
                                        break;
                                    case "N":
                                        f44_10 = "P";
                                        break;
                                    case "Y":
                                        f44_10 = "M";
                                        break;
                                    case " ":
                                        break;
                                }

                                isF44_10 = true;
                                break;
                        }
                    }
                }

                // 44.5
                if (!ObjectUtils.IsNull(token63_04) && !isF44_10)
                {
                    switch (token63_04.CrdVeryFlg)
                    {
                        case " ":
                            f44_5 = " ";
                            visaF44Length = (visaF44Length < 5) ? 5 : visaF44Length;
                            break;
                        case "C":
                        case "D":
                            f44_5 = "1";
                            break;
                        case "N":
                            f44_5 = " ";
                            visaF44Length = (visaF44Length < 5) ? 5 : visaF44Length;
                            break;
                        case "Y":
                            f44_5 = "2";
                            break;
                    }
                }

                // 44.8
                if (!ObjectUtils.IsNull(token63_B4))
                {
                    f44_8 = token63_B4.ArqcVerify;
                    visaF44Length = (visaF44Length < 9) ? 9 : visaF44Length;
                }

                // 44.13
                if (!ObjectUtils.IsNull(token63_C0))
                {
                    f44_13 = token63_C0.CavvAavResults;
                    visaF44Length = (visaF44Length < 15) ? 15 : visaF44Length;
                }

                var f44 = f44_1 + f44_2 + f44_3 + f44_4 + f44_5 + f44_6 + f44_7 + f44_8 + f44_9 + f44_10 + f44_11
                          + f44_12 + f44_13 + f44_14 + f44_15 + f44_16;

                if (!(f44.TrimEnd().Length == 0 && visaF44Length == 0))
                {
                    if (f44.TrimEnd().Length >= visaF44Length)
                    {
                        origin.RawVal(FIELD.F_44, f44.TrimEnd());
                    }
                    else
                    {
                        origin.RawVal(FIELD.F_44, f44.Substring(0, visaF44Length));
                    }
                }
            }
            catch (Exception e)
            {
                WriteLogException(typeof(OutField_44), string.Format("Error occur(OutField_44): {0}", e));
            }
        }
    }
}

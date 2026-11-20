// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InField_48.cs" company="Cybersoft">
//   Author: Kaven
// </copyright>
// <summary>
//   Defines the InField_48 type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.TM.VISA.Converter.InField
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.MCOLA;
    using Com.Cybersoft4u.TM.VISA.SubFieldCodec;
    using Com.Cybersoft4u.TM.VISA.SubFieldCodec.F48;

    #endregion

    internal class InField_48 : ConvertUnitBase
    {
        protected internal static void ToAdditionalData(VISA origin, MCOLA target)
        {
            try
            {
                switch (origin.Mti())
                {
                    case "0100":
                    case "0200":
                    case "0101":
                    case "0400":
                        if (ObjectUtils.IsNull(origin.RawVal(FIELD.F_23))
                            && ObjectUtils.IsNull(origin.RawVal(FIELD.F_62))
                            && ObjectUtils.IsNull(origin.RawVal(FIELD.F_63)))
                        {
                            return;
                        }

                        break;

                    case "0120":
                    case "0220":
                    case "0420":
                        if (ObjectUtils.IsNull(origin.RawVal(FIELD.F_23))
                            && ObjectUtils.IsNull(origin.RawVal(FIELD.F_44))
                            && ObjectUtils.IsNull(origin.RawVal(FIELD.F_62))
                            && ObjectUtils.IsNull(origin.RawVal(FIELD.F_63)))
                        {
                            return;
                        }

                        break;

                    case "0312":
                        if (ObjectUtils.IsNull(origin.RawVal(FIELD.F_48))
                            && ObjectUtils.IsNull(origin.RawVal(FIELD.F_63)))
                        {
                            return;
                        }

                        break;

                    case "0800":
                    case "0810":
                        if (ObjectUtils.IsNull(origin.RawVal(FIELD.F_48)))
                        {
                            return;
                        }

                        break;

                    case "0600":
                    case "0620":
                        if (ObjectUtils.IsNull(origin.RawVal(FIELD.F_63)))
                        {
                            return;
                        }

                        break;
                }

                string additionalData = null;
                var f63 = origin.RawVal(FIELD.F_63);
                var isChange = false;

                // PAN-SEP-NO
                if (!ObjectUtils.IsNull(origin.RawVal(FIELD.F_23)))
                {
                    var panSepNo = "0"
                                   + origin.RawVal(FIELD.F_23)
                                         .ToString()
                                         .PadLeft(4, '0')
                                         .Substring(2, 2)
                                         .Trim()
                                         .PadRight(2, ' ');
                    additionalData += panSepNo;
                    isChange = true;
                }
                else
                {
                    additionalData += "000";
                }

                // FIID
                additionalData += "BK00";

                // ACCT-TYPE
                additionalData += "31";

                // ERROR-CODE
                if ("0312".Equals(origin.Mti()) && !ObjectUtils.IsNull(origin.RawVal(FIELD.F_48)))
                {
                    if ("*".Equals(F_48Field.Default.Decode((byte[])origin.RawVal(FIELD.F_48)).Substring(0, 1)))
                    {
                        additionalData += string.Empty.PadRight(4, ' ');
                    }
                    else
                    {
                        var f48 = F_48FieldUsage1b.Default.Decode((byte[])origin.RawVal(FIELD.F_48));
                        additionalData += f48.Trim().PadRight(4, ' ');
                    }
                    isChange = true;
                }
                else
                {
                    additionalData += string.Empty.PadRight(4, ' ');
                }

                // VISA Message Reason Codes
                switch (origin.Mti().Substring(0, 2))
                {
                    case "01":
                    case "02":
                    case "03":
                    case "06":
                        var f63_3 = string.Empty;
                        if (!ObjectUtils.IsNull(f63))
                        {
                            var result = (string)new F_63VISABitmapField((byte[])f63).Field(FIELD.F_3);
                            if (!string.IsNullOrEmpty(result))
                            {
                                if (!string.IsNullOrEmpty(result.Trim()))
                                {
                                    f63_3 = result;
                                    isChange = true;
                                }
                            }
                        }

                        additionalData += f63_3.PadRight(4, ' ');
                        break;

                    default:
                        additionalData += string.Empty.PadRight(4, ' ');
                        break;
                }

                // response source/reason code
                var f44_1 = string.Empty;
                var f44Condition = !ObjectUtils.IsNull(origin.RawVal(FIELD.F_44))
                                   && origin.RawVal(FIELD.F_44).ToString().Length >= 1;
                if (("0120".Equals(origin.Mti()) || "0220".Equals(origin.Mti())) && f44Condition)
                {
                    f44_1 = origin.RawVal(FIELD.F_44).ToString()[0].ToString().Trim().PadRight(1, ' ');
                    isChange = true;
                }

                additionalData += f44_1.PadRight(1, ' ');

                // STIP/Switch Reason Code
                var f63_4 = string.Empty;
                if (("0120".Equals(origin.Mti()) || "0220".Equals(origin.Mti())) && !ObjectUtils.IsNull(f63))
                {
                    var result = (string)new F_63VISABitmapField((byte[])f63).Field(FIELD.F_4);
                    if (!string.IsNullOrEmpty(result))
                    {
                        if (!string.IsNullOrEmpty(result.Trim()))
                        {
                            f63_4 = result;
                            isChange = true;
                        }
                    }
                }

                additionalData += f63_4.PadRight(4, ' ');

                // TCC
                additionalData += string.Empty.PadRight(1, ' ');

                // Account Number Indicator
                additionalData += string.Empty.PadRight(1, ' ');

                // Chip CVR/TVR Bit Error Results
                additionalData += string.Empty.PadRight(50, ' ');

                var f62 = origin.RawVal(FIELD.F_62);
                var authorizationCharacteristicsIndicator = string.Empty.PadLeft(1, ' ');
                var marketSpecificDataIdentifier = string.Empty.PadLeft(1, ' ');
                var mtiCondition = "0100".Equals(origin.Mti()) || "0101".Equals(origin.Mti())
                                   || "0200".Equals(origin.Mti()) || "0220".Equals(origin.Mti())
                                   || "0120".Equals(origin.Mti()) || "0400".Equals(origin.Mti())
                                   || "0420".Equals(origin.Mti());
                if (!ObjectUtils.IsNull(f62) && mtiCondition)
                {
                    var f62_1 = new F_62VISABitmapField((byte[])f62).Field(FIELD.F_1);
                    if (!ObjectUtils.IsNull(f62_1))
                    {
                        authorizationCharacteristicsIndicator = f62_1.ToString().Trim().PadLeft(1, ' ');
                        isChange = true;
                    }

                    var f62_4 = new F_62VISABitmapField((byte[])f62).Field(FIELD.F_4);
                    if (!ObjectUtils.IsNull(f62_4))
                    {
                        marketSpecificDataIdentifier = f62_4.ToString().Trim().PadLeft(1, ' ');
                        isChange = true;
                    }
                }

                // Authorization Characteristics Indicator
                additionalData += authorizationCharacteristicsIndicator;

                // Market-Specific Data Identifier
                additionalData += marketSpecificDataIdentifier;

                if (isChange)
                {
                    target.RawVal(FIELD.F_48, additionalData);
                }
            }
            catch (Exception e)
            {
                WriteLogException(typeof(InField_48), string.Format("Error occur(InField_48): {0}", e));
            }
        }
    }
}

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InField_3.cs" company="Cybersoft">
//   Author: Kaven
// </copyright>
// <summary>
//   Defines the InField_3 type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.TM.VISA.Converter.InField
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.MCOLA;

    #endregion

    internal class InField_3 : ConvertUnitBase
    {
        protected internal static void ToTargetProcessingCode(VISA origin, MCOLA target)
        {
            try
            {
                var processingCode = string.Empty;
                if (!"0100".Equals(origin.Mti()) && !"0101".Equals(origin.Mti()) && !"0120".Equals(origin.Mti())
                    && !"0200".Equals(origin.Mti()) && !"0220".Equals(origin.Mti())
                    && !"0400".Equals(origin.Mti()) && !"0420".Equals(origin.Mti()))
                {
                    return;
                }

                var transactionType = string.Empty.PadLeft(2, ' ');
                if (!ObjectUtils.IsNull(origin.RawVal(FIELD.F_3)))
                {
                    transactionType = origin.RawVal(FIELD.F_3).ToString().Substring(0, 2);
                }

                // Transaction Type
                var f95 = origin.RawVal(FIELD.F_95);
                if ("08".Equals(origin.RawVal(FIELD.F_25)))
                {
                    processingCode = "80";

                    if (!ObjectUtils.IsNull(f95))
                    {
                        var f95Position1To12 = f95.ToString().Substring(0, 12).Trim();
                        var f95Value = Convert.ToUInt64(f95Position1To12);
                        if (!ObjectUtils.IsNull(f95Position1To12) && f95Value > 0)
                        {
                            processingCode = "02";
                        }
                    }
                }
                else if ("51".Equals(origin.RawVal(FIELD.F_25)))
                {
                    processingCode = "81";
                }
                else
                {
                    switch (transactionType)
                    {
                        case "00":
                            processingCode = "00";

                            if (!ObjectUtils.IsNull(f95))
                            {
                                var f95Position1To12 = f95.ToString().Substring(0, 12).Trim();
                                var f95Value = Convert.ToUInt64(f95Position1To12);
                                if (!ObjectUtils.IsNull(f95Position1To12) && f95Value > 0)
                                {
                                    processingCode = "02";
                                }
                            }

                            break;

                        case "01":
                        case "11":
                            processingCode = "01";

                            if (!ObjectUtils.IsNull(f95))
                            {
                                var f95Position1To12 = f95.ToString().Substring(0, 12).Trim();
                                var f95Value = Convert.ToUInt64(f95Position1To12);
                                if (!ObjectUtils.IsNull(f95Position1To12) && f95Value > 0)
                                {
                                    processingCode = "14";
                                }
                            }

                            break;

                        case "20":
                            if ("6010".Equals(origin.RawVal(FIELD.F_18)) || "6011".Equals(origin.RawVal(FIELD.F_18)))
                            {
                                processingCode = "14";
                            }
                            else if (!ObjectUtils.IsNull(f95))
                            {
                                var f95Position1To12 = f95.ToString().Substring(0, 12).Trim();
                                var f95Value = Convert.ToUInt64(f95Position1To12);
                                if (!ObjectUtils.IsNull(f95Position1To12) && f95Value > 0)
                                {
                                    processingCode = "22";
                                }
                            }
                            else
                            {
                                processingCode = "20";
                            }

                            break;

                        case "30":
                            processingCode = "31";
                            break;

                        case "70":
                            processingCode = "96";
                            break;

                        case "39":
                            processingCode = "81";
                            break;

                        default:
                            processingCode = "00";
                            break;
                    }
                }

                // Account Type (From), (To)
                if (transactionType.Equals("01")
                    && ("6010".Equals(origin.RawVal(FIELD.F_18)) || "6011".Equals(origin.RawVal(FIELD.F_18))))
                {
                    processingCode += "3000";
                }
                else if (transactionType.Equals("30"))
                {
                    processingCode += "3000";
                }
                else
                {
                    processingCode += "0030";
                }

                target.RawVal(FIELD.F_3, processingCode);
            }
            catch (Exception e)
            {
                WriteLogException(typeof(InField_3), string.Format("Error occur(InField_3): {0}", e));
            }
        }
    }
}

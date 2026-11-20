// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InField_Header.cs" company="Cybersoft">
//   Author: Kaven
// </copyright>
// <summary>
//   Defines the InField_Header type.
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

    #endregion

    internal class InField_Header : ConvertUnitBase
    {
        protected internal static void ToTargetHeader(VISA origin, MCOLA target)
        {
            try
            {
                // ProductIndicator
                switch (origin.Mti().Substring(0, 2))
                {
                    case "08":
                        target.Header().ProductIndicator("00");
                        break;

                    case "03":
                        target.Header().ProductIndicator("08");
                        break;

                    default:
                        if (!ObjectUtils.IsNull(origin.RawVal(FIELD.F_3)))
                        {
                            switch (origin.RawVal(FIELD.F_3).ToString().Substring(0, 2))
                            {
                                case "01":
                                    if ("0611".Equals(origin.RawVal(FIELD.F_18))
                                        || "0610".Equals(origin.RawVal(FIELD.F_18)))
                                    {
                                        target.Header().ProductIndicator("01");
                                    }

                                    break;

                                case "30":
                                    target.Header().ProductIndicator("01");
                                    break;
                            }
                        }

                        break;
                }

                // OriginatorCode
                if ("03".Equals(origin.Mti().Substring(0, 2)))
                {
                    target.Header().OriginatorCode("5");
                }
                else if ("0120".Equals(origin.Mti()) || "0220".Equals(origin.Mti()))
                {
                    target.Header().OriginatorCode("0");

                    var f63 = origin.RawVal(FIELD.F_63);

                    if (!ObjectUtils.IsNull(f63))
                    {
                        var f63_4 = new F_63VISABitmapField((byte[])f63).Field(FIELD.F_4);

                        if (!ObjectUtils.IsNull(f63_4))
                        {
                            target.Header().OriginatorCode("6");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                WriteLogException(typeof(InField_Header), string.Format("Error occur(InField_Header): {0}", e));
            }
        }
    }
}

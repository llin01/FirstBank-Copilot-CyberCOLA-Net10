// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InField_39.cs" company="Cybersoft">
//   Author: Kaven
// </copyright>
// <summary>
//   Defines the InField_39 type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.TM.VISA.Converter.InField
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.MCOLA;
    using Com.Cybersoft4u.TM.VISA.SubFieldCodec.F48;

    #endregion

    internal class InField_39 : ConvertUnitBase
    {
        protected internal static void ToTargerResponeCode(VISA origin, MCOLA target)
        {
            try
            {
                if (ObjectUtils.IsNull(origin.RawVal(FIELD.F_39)))
                {
                    return;
                }

                FieldFiller.FillField(FIELD.F_39, origin, target, true);

                if (!"031".Equals(origin.Mti().Substring(0, 3)))
                {
                    return;
                }

                switch (origin.RawVal(FIELD.F_39).ToString())
                {
                    case "00":
                        target.RawVal(FIELD.F_39, "00");
                        break;

                    case "06":
                        target.RawVal(FIELD.F_39, "27");
                        string f48 = null;

                        if (!ObjectUtils.IsNull(origin.RawVal(FIELD.F_48)))
                        {
                            f48 = F_48FieldUsage1b.Default.Decode((byte[])origin.RawVal(FIELD.F_48));
                        }

                        switch (f48)
                        {
                            case "0568":
                                target.RawVal(FIELD.F_39, "24");
                                break;

                            case "0565":
                                target.RawVal(FIELD.F_39, "25");
                                break;

                            case "0567":
                                target.RawVal(FIELD.F_39, "N3");
                                break;

                            case "0566":
                                switch (origin.RawVal(FIELD.F_91).ToString())
                                {
                                    case "1":
                                        target.RawVal(FIELD.F_39, "N4");
                                        break;

                                    case "2":
                                        target.RawVal(FIELD.F_39, "N5");
                                        break;
                                }

                                break;
                        }

                        break;

                    case "14":
                    case "25":
                        target.RawVal(FIELD.F_39, "N5");
                        break;
                }
            }
            catch (Exception e)
            {
                WriteLogException(typeof(InField_39), string.Format("Error occur(InField_39): {0}", e));
            }
        }
    }
}

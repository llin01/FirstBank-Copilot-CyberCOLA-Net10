// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MtiConverter.cs" company="Cybersoft">
//   Author: Kaven
// </copyright>
// <summary>
//   Defines the MtiConverter type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.TM.VISA.Converter
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.MCOLA;
    using Com.Cybersoft4u.TM.VISA.SubFieldCodec;

    #endregion

    internal class MtiConverter : ConvertUnitBase
    {
        protected internal static void ToTargetMessageTransactionIndicator(VISA origin, MCOLA target)
        {
            try
            {
                target.Mti(origin.Mti());

                switch (origin.Mti())
                {
                    case "0100":
                    case "0200":
                        target.Mti("0200");

                        if ("5542".Equals(origin.RawVal(FIELD.F_18)))
                        {
                            target.Mti("0100");
                        }

                        var f63 = origin.RawVal(FIELD.F_63);
                        if (!ObjectUtils.IsNull(f63))
                        {
                            if (Convert.ToUInt16((string)new F_63VISABitmapField((byte[])f63).Field(FIELD.F_2)) > 0)
                            {
                                if ("00".Equals(origin.RawVal(FIELD.F_25)))
                                {
                                    target.Mti("0100");
                                }
                            }
                        }

                        break;

                    case "0120":
                        target.Mti("0220");
                        break;

                    case "0302":
                        target.Mti("0300");
                        break;

                    case "0312":
                        target.Mti("0310");
                        break;

                    case "0322":
                        target.Mti("0320");
                        break;
                }

                if (!ObjectUtils.IsNull(origin.RejectHeader()))
                {
                    target.Mti("0190");
                }
            }
            catch (Exception e)
            {
                WriteLogException(typeof(MtiConverter), string.Format("Error occur(MtiConverter): {0}", e));
            }
        }

        protected internal static void ToOriginMessageTransactionIndicator(MCOLA target, VISA origin)
        {
            try
            {
                origin.Mti(target.Mti());

                switch (target.Mti())
                {
                    case "0110":
                    case "0210":
                        origin.Mti("0110");
                        break;

                    case "0230":
                        origin.Mti("0130");
                        break;

                    case "0300":
                        origin.Mti("0302");
                        break;

                    case "0310":
                        origin.Mti("0312");
                        break;

                    case "0330":
                        origin.Mti("0332");
                        break;
                }
            }
            catch (Exception e)
            {
                WriteLogException(typeof(MtiConverter), string.Format("Error occur(MtiConverter): {0}", e));
            }
        }
    }
}

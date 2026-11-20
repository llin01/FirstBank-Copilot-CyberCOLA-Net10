// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InField_113.cs" company="Cybersoft">
//   Author: Kaven
// </copyright>
// <summary>
//   Defines the InField_113 type.
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

    internal class InField_113 : ConvertUnitBase
    {
        protected internal static void ToTargetDE113(VISA origin, MCOLA target)
        {
            try
            {
                if (ObjectUtils.IsNull(origin.RawVal(FIELD.F_48)))
                {
                    return;
                }

                var f48 = F_48Field.Default.Decode((byte[])origin.RawVal(FIELD.F_48));

                if (!"#".Equals(f48.Substring(0, 1)))
                {
                    return;
                }

                target.RawVal(FIELD.F_113, f48);
            }
            catch (Exception e)
            {
                WriteLogException(typeof(InField_113), string.Format("Error occur(InField_113): {0}", e));
            }
        }
    }
}

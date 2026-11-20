// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InField_43.cs" company="Cybersoft">
//   Author: Kaven
// </copyright>
// <summary>
//   Defines the InField_43 type.
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

    internal class InField_43 : ConvertUnitBase
    {
        protected internal static void ToCardAcceptorNameLocation(VISA origin, MCOLA target)
        {
            try
            {
                if (ObjectUtils.IsNull(origin.RawVal(FIELD.F_43)))
                {
                    return;
                }

                var f43 = origin.RawVal(FIELD.F_43);

                var cardAcceptorNameLocation = string.Empty;

                // NAME
                cardAcceptorNameLocation += f43.ToString().Substring(0, 22).Trim().PadRight(22, ' ');

                // CITY
                cardAcceptorNameLocation += f43.ToString().Substring(25, 13).Trim().PadRight(13, ' ');

                // STATE
                cardAcceptorNameLocation += string.Empty.PadLeft(3, ' ');

                // CNTRY
                cardAcceptorNameLocation += f43.ToString().Substring(38, 2).Trim().PadRight(2, ' ');

                target.RawVal(FIELD.F_43, cardAcceptorNameLocation);
            }
            catch (Exception e)
            {
                WriteLogException(typeof(InField_43), string.Format("Error occur(InField_43): {0}", e));
            }
        }
    }
}

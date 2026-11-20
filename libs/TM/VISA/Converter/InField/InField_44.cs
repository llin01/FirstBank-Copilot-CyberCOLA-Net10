// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InField_44.cs" company="Cybersoft">
//   Author: Kaven
// </copyright>
// <summary>
//   Defines the InField_44 type.
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

    internal class InField_44 : ConvertUnitBase
    {
        protected internal static void ToAdditionalResponseData(VISA origin, MCOLA target)
        {
            try
            {
                var additionalResponseData = string.Empty;
                var isChange = false;

                // MTI
                additionalResponseData += origin.Mti();

                // Trace ID
                if (!ObjectUtils.IsNull(origin.RawVal(FIELD.F_11)))
                {
                    additionalResponseData += origin.RawVal(FIELD.F_11).ToString();
                    isChange = true;
                }
                else
                {
                    additionalResponseData += string.Empty.PadRight(6, ' ');
                }

                // Reject Code
                var RejectCode = origin.RejectHeader().RejectDataGroup;
                if (!ObjectUtils.IsNull(RejectCode))
                {
                    additionalResponseData += RejectCode;
                    isChange = true;
                }

                // FILLER
                additionalResponseData += string.Empty.PadLeft(11, ' ');

                if (isChange)
                {
                    target.RawVal(FIELD.F_44, additionalResponseData);
                }
            }
            catch (Exception e)
            {
                WriteLogException(typeof(InField_44), string.Format("Error occur(InField_44): {0}", e));
            }
        }
    }
}

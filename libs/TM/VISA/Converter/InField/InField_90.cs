// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InField_90.cs" company="Cybersoft">
//   Author: Kaven
// </copyright>
// <summary>
//   Defines the InField_90 type.
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

    internal class InField_90 : ConvertUnitBase
    {
        protected internal static void ToOriginalDataElement(VISA origin, MCOLA target)
        {
            try
            {
                if (ObjectUtils.IsNull(origin.RawVal(FIELD.F_90)))
                {
                    return;
                }

                var originalDataElement = string.Empty;

                var f90 = origin.RawVal(FIELD.F_90).ToString();
                var isChange = false;

                // Original message type
                if (f90.Length >= 4)
                {
                    if ("0100".Equals(f90.Substring(0, 4)))
                    {
                        originalDataElement += "0200";
                    }
                    else
                    {
                        originalDataElement += f90.Substring(0, 4);
                    }

                    isChange = true;
                }
                else
                {
                    originalDataElement += string.Empty.PadRight(4, ' ');
                }

                // Original retrieval reference number
                if (!ObjectUtils.IsNull(origin.RawVal(FIELD.F_37)))
                {
                    originalDataElement += origin.RawVal(FIELD.F_37).ToString().PadRight(12, '0');
                    isChange = true;
                }
                else if (f90.Length >= 10)
                {
                    originalDataElement += f90.Substring(4, 6).PadRight(12, '0');
                    isChange = true;
                }
                else
                {
                    originalDataElement += string.Empty.PadRight(12, ' ');
                }

                // Original transaction date/time
                if (f90.Length >= 20)
                {
                    originalDataElement += f90.Substring(10, 10).PadRight(12, '0');
                    isChange = true;
                }
                else
                {
                    originalDataElement += string.Empty.PadRight(12, ' ');
                }

                // Original capture date
                originalDataElement += string.Empty.PadRight(4, '0');

                // Filler
                originalDataElement += string.Empty.PadRight(10, ' ');

                if (isChange)
                {
                    target.RawVal(FIELD.F_90, originalDataElement);
                }
            }
            catch (Exception e)
            {
                WriteLogException(typeof(InField_90), string.Format("Error occur(InField_90): {0}", e));
            }
        }
    }
}

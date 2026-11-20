// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InField_70.cs" company="Cybersoft">
//   Author: Kaven
// </copyright>
// <summary>
//   Defines the InField_70 type.
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

    internal class InField_70 : ConvertUnitBase
    {
        protected internal static void ToNetworkManagementInformationCode(VISA origin, MCOLA target)
        {
            try
            {
                if (ObjectUtils.IsNull(origin.RawVal(FIELD.F_70)))
                {
                    return;
                }

                FieldFiller.FillField(FIELD.F_70, origin, target, true);
                var networkManagementInformationCode = target.RawVal(FIELD.F_70).ToString();

                switch (networkManagementInformationCode)
                {
                    case "078":
                        networkManagementInformationCode = "001";
                        break;
                    case "079":
                        networkManagementInformationCode = "002";
                        break;
                    case "301":
                        networkManagementInformationCode = "031";
                        break;
                }

                target.RawVal(FIELD.F_70, networkManagementInformationCode);
            }
            catch (Exception e)
            {
                WriteLogException(typeof(InField_70), string.Format("Error occur(InField_70): {0}", e));
            }
        }
    }
}

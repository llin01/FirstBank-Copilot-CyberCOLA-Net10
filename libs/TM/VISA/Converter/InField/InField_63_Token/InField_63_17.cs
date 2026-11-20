// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InField_63_17.cs" company="Cybersoft">
//   Author: Kaven
// </copyright>
// <summary>
//   Defines the InField_63_17 type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.TM.VISA.Converter.InField.InField_63_Token
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.MCOLA;
    using Com.Cybersoft4u.TM.ISO8583.Nccc;
    using Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens;
    using Com.Cybersoft4u.TM.VISA.SubFieldCodec;

    #endregion

    internal class InField_63_17 : ConvertUnitBase
    {
        protected internal static string ToPosAdditionalDataToken17(VISA origin, MCOLA target, string inputString)
        {
            try
            {
                var token = new TokenGroup();
                token.Decode(inputString);
                token.Tokens["17"] = new Token17();
                var token_17 = (Token17)token.Tokens["17"];
                token_17.DataDecode(string.Empty.PadLeft(24, ' '));
                var f62 = origin.RawVal(FIELD.F_62);
                var isTokenChange = false;

                // TRAN-ID
                if (!ObjectUtils.IsNull(f62))
                {
                    var f62_2 = new F_62VISABitmapField((byte[])f62).Field(FIELD.F_2);
                    if (!ObjectUtils.IsNull(f62_2))
                    {
                        token_17.TranId = f62_2.ToString().Substring(1, 15).Trim().PadLeft(15, ' ');
                        isTokenChange = true;
                    }
                }

                if (isTokenChange)
                {
                    var outputString = token.Encode();
                    return outputString;
                }

                return inputString;
            }
            catch (Exception e)
            {
                WriteLogException(typeof(InField_63_17), string.Format("Error occur(InField_63_17): {0}", e));
                return inputString;
            }
        }
    }
}

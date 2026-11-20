// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InField_63_C6.cs" company="Cybersoft">
//   Author: Kaven
// </copyright>
// <summary>
//   Defines the InField_63_C6 type.
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

    internal class InField_63_C6 : ConvertUnitBase
    {
        protected internal static string ToPosAdditionalDataTokenC6(VISA origin, MCOLA target, string inputString)
        {
            try
            {
                var token = new TokenGroup();
                token.Decode(inputString);
                token.Tokens["C6"] = new TokenC6();
                var token_C6 = (TokenC6)token.Tokens["C6"];
                token_C6.DataDecode(string.Empty.PadLeft(80, ' '));
                var isTokenChange = false;

                var f126 = origin.RawVal(FIELD.F_126);
                if (!ObjectUtils.IsNull(f126))
                {
                    var f126_8 = new F_126VISABitmapField((byte[])f126).Field(FIELD.F_8);
                    var f126_9 = new F_126VISABitmapField((byte[])f126).Field(FIELD.F_9);

                    // XID
                    if (!ObjectUtils.IsNull(f126_8))
                    {
                        token_C6.Xid = ((byte[])f126_8).GetHexString();
                        isTokenChange = true;
                    }

                    // TRANS-STAIN/CAVV
                    if (!ObjectUtils.IsNull(f126_9))
                    {
                        token_C6.TransStainCavv = ((byte[])f126_9).GetHexString();
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
                WriteLogException(typeof(InField_63_C6), string.Format("Error occur(InField_63_C6): {0}", e));
                return inputString;
            }
        }
    }
}

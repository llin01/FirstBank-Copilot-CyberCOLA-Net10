// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OutField_55.cs" company="Cybersoft">
//   Author: Kaven
// </copyright>
// <summary>
//   Defines the OutField_55 type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.TM.VISA.Converter.OutField
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.MCOLA;
    using Com.Cybersoft4u.TM.ISO8583.Nccc;
    using Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens;
    using Com.Cybersoft4u.TM.ISO8583.TLV;

    #endregion

    internal class OutField_55 : ConvertUnitBase
    {
        protected internal static void ToVisaF55(VISA origin, MCOLA target)
        {
            try
            {
                var token = new TokenGroup();
                if (!ObjectUtils.IsNull(target.RawVal(FIELD.F_63)))
                {
                    token.Decode(target.RawVal(FIELD.F_63).ToString());
                }
                else
                {
                    return;
                }

                TokenB5 token63_B5 = null;
                try
                {
                    token63_B5 = (TokenB5)token.Tokens["B5"];
                }
                catch (Exception)
                {
                    WriteLogCodecFail(typeof(OutField_55), string.Format("OutField_55(): Token B5 cannot found."));
                }

                TokenB6 token63_B6 = null;
                try
                {
                    token63_B6 = (TokenB6)token.Tokens["B6"];
                }
                catch (Exception)
                {
                    WriteLogCodecFail(typeof(OutField_55), string.Format("OutField_55(): Token B6 cannot found."));
                }

                TokenB2 token63_B2 = null;
                try
                {
                    token63_B2 = (TokenB2)token.Tokens["B2"];
                }
                catch (Exception)
                {
                    WriteLogCodecFail(typeof(OutField_55), string.Format("OutField_55(): Token B2 cannot found."));
                }

                var tlv = new TLV();
                var dataset = new Dataset { Id = "01" };
                var tag_91 = new Tag();
                var tag_71 = new Tag();
                var tag_9F36 = new Tag();
                var isTlvNull = true;

                if (!ObjectUtils.IsNull(token63_B5))
                {
                    tag_91 = new Tag { Id = "91", Value = (token63_B5.Arpc + token63_B5.IssRespCde).GetHexBytes() };
                    dataset.Tags["91"] = tag_91;
                    isTlvNull = false;
                }

                if (!ObjectUtils.IsNull(token63_B6))
                {
                    tag_71 = new Tag
                    {
                        Id = "71",
                        Value =
                                         new ByteBuffer().WriteByte((byte)(token63_B6.IssScriptData.Length / 2))
                                         .WriteBytes(token63_B6.IssScriptData.GetHexBytes())
                                         .Val()
                    };
                    dataset.Tags["71"] = tag_71;
                    isTlvNull = false;
                }

                if (!ObjectUtils.IsNull(token63_B2))
                {
                    tag_9F36 = new Tag { Id = "9F36", Value = token63_B2.Atc.GetHexBytes() };
                    dataset.Tags["9F36"] = tag_9F36;
                    isTlvNull = false;
                }

                if (!isTlvNull)
                {
                    tlv.Datasets["01"] = dataset;
                    origin.RawVal(FIELD.F_55, tlv.Encode().Val());
                }
            }
            catch (Exception e)
            {
                WriteLogException(typeof(OutField_55), string.Format("Error occur(OutField_55): {0}", e));
            }
        }
    }
}

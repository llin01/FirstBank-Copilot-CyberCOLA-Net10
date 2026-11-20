// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OutField_Filler.cs" company="Cybersoft">
//   Author: Kaven
// </copyright>
// <summary>
//   Defines the OutField_Filler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.TM.VISA.Converter.OutField
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.MCOLA;
    using Com.Cybersoft4u.TM.ISO8583.TLV;
    using Com.Cybersoft4u.TM.VISA.SubFieldCodec.F127;
    using Com.Cybersoft4u.TM.VISA.SubFieldCodec.F48;
    using SubFieldCodec;

    #endregion

    internal class OutField_Filler : ConvertUnitBase
    {
        protected internal static void OutField0110(VISA origin, MCOLA target)
        {
            FieldFiller.FillField(FIELD.F_38, target, origin, false);
            FieldFiller.FillField(FIELD.F_39, target, origin, true);
            FieldFiller.FillField(FIELD.F_44, target, origin, false);
            FieldFiller.FillField(FIELD.F_54, target, origin, false);

            OutField_55.ToVisaF55(origin, target);
            OutField_48.ToVisaF48(origin, target);
        }

        protected internal static void OutField0130(VISA origin, MCOLA target)
        {
            FieldFiller.FillField(FIELD.F_39, target, origin, true);
            FieldFiller.FillField(FIELD.F_44, target, origin, false);

            OutField_48.ToVisaF48(origin, target);
        }

        protected internal static void OutField0410(VISA origin, MCOLA target)
        {
            FieldFiller.FillField(FIELD.F_39, target, origin, true);
            FieldFiller.FillField(FIELD.F_44, target, origin, false);

            OutField_48.ToVisaF48(origin, target);
        }

        protected internal static void OutField0430(VISA origin, MCOLA target)
        {
            FieldFiller.FillField(FIELD.F_39, target, origin, true);
            FieldFiller.FillField(FIELD.F_44, target, origin, false);

            OutField_48.ToVisaF48(origin, target);
        }

        protected internal static void OutField0302(VISA origin, MCOLA target)
        {
            try
            {
                FieldFiller.FillField(FIELD.F_2, target, origin, false);
                FieldFiller.FillField(FIELD.F_7, target, origin, true);
                FieldFiller.FillField(FIELD.F_11, target, origin, true);
                FieldFiller.FillField(FIELD.F_14, target, origin, false);
                FieldFiller.FillField(FIELD.F_19, target, origin, false);
                FieldFiller.FillField(FIELD.F_32, target, origin, false);
                FieldFiller.FillField(FIELD.F_37, target, origin, true);
                FieldFiller.FillField(FIELD.F_73, target, origin, false);
                FieldFiller.FillField(FIELD.F_91, target, origin, true);
                FieldFiller.FillField(FIELD.F_115, target, origin, false);

                //// NCC DE 48
                if (!ObjectUtils.IsNull(target.RawVal(FIELD.F_48)))
                {
                    var de63 = new F_63VISABitmapField();
                    bool modify = false;

                    // VISA Message Reason Codes
                    var mti = origin.Mti().Substring(0, 2);
                    if ("01".Equals(mti) || "02".Equals(mti) || "06".Equals(mti) || "03".Equals(mti))
                    {
                        var VisaMessageReasonCodes = target.RawVal(FIELD.F_48).ToString().Substring(13, 4).Trim();
                        if (!string.IsNullOrEmpty(VisaMessageReasonCodes))
                        {
                            de63.Field(FIELD.F_3, VisaMessageReasonCodes);
                            modify = true;
                        }
                    }

                    // Chip CVR/TVR Bit Error Results
                    if ("03".Equals(mti))
                    {
                        var de63_1 = target.RawVal(FIELD.F_48).ToString().Substring(24, 4).Trim();
                        if (!string.IsNullOrEmpty(de63_1))
                        {
                            de63.Field(FIELD.F_1, de63_1);
                            modify = true;
                        }

                        var de48U2 = target.RawVal(FIELD.F_48).ToString().Substring(28, 30).Trim();
                        if (!string.IsNullOrEmpty(de48U2))
                        {
                            origin.RawVal(FIELD.F_48, F_48Field.Default.Encode("*" + de48U2));
                        }
                    }

                    if (modify)
                    {
                        origin.RawVal(FIELD.F_63, de63.Encode().Val());
                    }

                    // origin.RawVal(FIELD.F_48, F_48Field.Default.Encode("*3713"));
                }

                // VISA DE 101
                var isF127Vp = false;
                var nccc_f101 = target.RawVal(FIELD.F_101);
                if (!ObjectUtils.IsNull(nccc_f101))
                {
                    if (nccc_f101.ToString().Length >= 2 && "VP".Equals(nccc_f101.ToString().Substring(0, 2)))
                    {
                        origin.RawVal(FIELD.F_101, "E2");

                        isF127Vp = true;
                    }
                    else
                    {
                        origin.RawVal(FIELD.F_101, nccc_f101.ToString());
                    }
                }

                // VISA DE 123         
                if (!ObjectUtils.IsNull(target.RawVal(FIELD.F_124)))
                {
                    var tlv123 = new TLV();
                    var dataset_68 = new Dataset { Id = "68" };
                    var nccc_f124 = target.RawVal(FIELD.F_124).ToString();

                    var tag_01 = new Tag
                    {
                        Id = "01",
                        Value = DefaultPacker.EBCDIC.GetBytes(nccc_f124.Substring(43, 19).Trim())
                        // for 20170302 for NCPS changed 
                        //Value = DefaultPacker.EBCDIC.GetBytes(nccc_f124.Substring(92, 19).Trim())
                    };
                    //var tag_02 = new Tag
                    //                 {
                    //                     Id = "02", 
                    //                     Value = DefaultPacker.EBCDIC.GetBytes(nccc_f124.Substring(111, 2))
                    //                 };
                    var tag_03 = new Tag
                    {
                        Id = "03",
                        Value = DefaultPacker.EBCDIC.GetBytes(nccc_f124.Substring(64, 11))
                        // for 20170302 for NCPS changed 
                        //Value = DefaultPacker.EBCDIC.GetBytes(nccc_f124.Substring(113, 11))
                    };
                    //var tag_05 = new Tag
                    //                 {
                    //                     Id = "05", 
                    //                     Value = DefaultPacker.EBCDIC.GetBytes(nccc_f124.Substring(143, 32))
                    //                 };
                    //var tag_0B = new Tag
                    //                 {
                    //                     Id = "0B", 
                    //                     Value = DefaultPacker.EBCDIC.GetBytes(nccc_f124.Substring(183, 32))
                    //                 };

                    dataset_68.Tags["01"] = tag_01;
                    //dataset_68.Tags["02"] = tag_02;
                    dataset_68.Tags["03"] = tag_03;
                    //dataset_68.Tags["05"] = tag_05;
                    //dataset_68.Tags["0B"] = tag_0B;
                    tlv123.Datasets["68"] = dataset_68;

                    origin.RawVal(FIELD.F_123, tlv123.Encode().Val());
                }

                //VISA DE 127
                if (isF127Vp)
                {
                    // VISA DE 127.E1, 127.E2
                    var nccc_f120 = target.RawVal(FIELD.F_120);

                    if (!ObjectUtils.IsNull(nccc_f120) && nccc_f120.ToString().Length >= 50)
                    {
                        var f127_E1 = nccc_f120.ToString().Substring(39, 2);
                        var f127_E2 = nccc_f120.ToString().Substring(41, 9);

                        var fillToF127 = f127_E1 + f127_E2;
                        origin.RawVal(FIELD.F_127, F_127VISAFieldAEMPR.Default.Encode(fillToF127));
                    }
                }
                else if (!ObjectUtils.IsNull(target.RawVal(FIELD.F_124)))
                {
                    var tlv127 = new TLV();
                    var dataset_41 = new Dataset { Id = "41" };
                    var nccc_f124 = target.RawVal(FIELD.F_124).ToString();
                    bool modify = false;

                    //var ReplacementPan = nccc_f124.Substring(580, 19).Trim();
                    // for 20170302 for NCPS changed 
                    var ReplacementPan = nccc_f124.Substring(410, 19).Trim();
                    if (!string.IsNullOrEmpty(ReplacementPan))
                    {
                        var ReplLeng = (ReplacementPan.Length % 2 == 0 ?
                                       (ReplacementPan.Length) : (ReplacementPan.Length + 1));
                        ReplacementPan = ReplacementPan.PadLeft(ReplLeng, '0');

                        var tag_01 = new Tag
                        {
                            Id = "01",
                            //Value = DefaultPacker.EBCDIC.GetBytes(ReplacementPan)
                            Value = ReplacementPan.GetHexBytes()
                        };
                        dataset_41.Tags["01"] = tag_01;
                        modify = true;
                    }

                    //var ReplacementPanExpirationDate = nccc_f124.Substring(599, 4);
                    // for 20170302 for NCPS changed 
                    var ReplacementPanExpirationDate = nccc_f124.Substring(429, 4);
                    if (!string.IsNullOrEmpty(ReplacementPanExpirationDate.Trim()))
                    {
                        var tag_02 = new Tag
                        {
                            Id = "02",
                            //Value = DefaultPacker.EBCDIC.GetBytes(ReplacementPanExpirationDate)
                            Value = ReplacementPanExpirationDate.GetHexBytes()
                        };
                        dataset_41.Tags["02"] = tag_02;
                        modify = true;
                    }

                    if (modify)
                    {
                        tlv127.Datasets["41"] = dataset_41;
                        origin.RawVal(FIELD.F_127, tlv127.Encode().Val());
                    }
                }
            }
            catch (Exception e)
            {
                WriteLogException(typeof(OutField_Filler), string.Format("Error occur(OutField_Filler): {0}", e));
            }
        }

        protected internal static void OutField0800(VISA origin, MCOLA target)
        {
            try
            {
                FieldFiller.FillField(FIELD.F_7, target, origin, true);
                FieldFiller.FillField(FIELD.F_11, target, origin, true);

                // VISA DE 70
                if (!ObjectUtils.IsNull(target.RawVal(FIELD.F_70)))
                {
                    switch (target.RawVal(FIELD.F_70).ToString())
                    {
                        case "001":
                            origin.RawVal(FIELD.F_70, "0078");
                            break;
                        case "002":
                            origin.RawVal(FIELD.F_70, "0079");
                            break;
                        case "301":
                            origin.RawVal(FIELD.F_70, "0301");
                            break;
                        default:
                            origin.RawVal(FIELD.F_70, target.RawVal(FIELD.F_70));
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                WriteLogException(typeof(OutField_Filler), string.Format("Error occur(OutField_Filler): {0}", e));
            }
        }

        protected internal static void OutField0810(VISA origin, MCOLA target)
        {
            FieldFiller.FillField(FIELD.F_39, target, origin, true);
        }

        protected internal static void OutField0610(VISA origin, MCOLA target)
        {
            FieldFiller.FillField(FIELD.F_39, target, origin, true);
        }

        protected internal static void OutField0630(VISA origin, MCOLA target)
        {
            FieldFiller.FillField(FIELD.F_39, target, origin, true);
        }
    }
}

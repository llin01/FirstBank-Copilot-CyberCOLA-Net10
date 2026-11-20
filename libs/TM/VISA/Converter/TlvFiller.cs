// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TlvFiller.cs" company="Cybersoft">
//   Author: Kaven
// </copyright>
// <summary>
//   Defines the TlvFiller type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.TM.VISA.Converter
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.TLV;
    using Com.Cybersoft4u.TM.VISA.SubFieldCodec.F127;
    using System.Collections.Generic;

    #endregion

    internal class TlvFiller : ConvertUnitBase
    {
        protected internal static TLV TryDecodeTlv(FIELD field, VISA origin)
        {
            if (!ObjectUtils.IsNull(origin.RawVal(field)))
            {
                try
                {
                    if (FIELD.F_127.Equals(field))
                    {
                        return F_127VISAFieldUsage2.Default.Decode((byte[])origin.RawVal(field));
                    }

                    //var TLVData = new TLV().Decode((byte[])origin.RawVal(field));
                    //TryListTag(field, TLVData);
                    //return TLVData;
                    return new TLV().Decode((byte[])origin.RawVal(field));
                }
                catch (Exception)
                {
                    WriteLogCodecFail(typeof(TlvFiller), string.Format("TlvFiller(): Fail to decode VISA DE {0} to TLV format.", field));
                    return null;
                }
            }

            WriteLogCodecFail(typeof(TlvFiller), string.Format("TlvFiller(): DE {0} is null.", field));
            return null;
        }

        protected internal static string TryGetEBCDICTag(TLV field, string dataset, string tag)
        {
            if (!ObjectUtils.IsNull(field))
            {
                try
                {
                    return DefaultPacker.EBCDIC.GetString(field.Datasets[dataset].Tags[tag].Value);
                }
                catch (Exception)
                {
                    WriteLogCodecFail(typeof(TlvFiller), string.Format("TlvFiller(): TLV {0}: Dataset {1}: Tag {2} not found", field, dataset, tag));
                    return string.Empty;
                }
            }

            WriteLogCodecFail(typeof(TlvFiller), string.Format("TlvFiller(): VISA DE {0} is null.", field));
            return string.Empty;
        }

        protected internal static string TryGetTag(TLV field, string dataset, string tag)
        {
            if (!ObjectUtils.IsNull(field))
            {
                try
                {
                    return field.Datasets[dataset].Tags[tag].Value.GetHexString();
                }
                catch (Exception)
                {
                    WriteLogCodecFail(typeof(TlvFiller), string.Format("TlvFiller(): TLV {0}: Dataset {1}: Tag {2} not found", field, dataset, tag));
                    return string.Empty;
                }
            }
            WriteLogCodecFail(typeof(TlvFiller), string.Format("TlvFiller(): VISA DE {0} is null.", field));
            return string.Empty;
        }

        protected internal static void TryListTag(FIELD field, TLV fieldTlv)
        {
            if (!ObjectUtils.IsNull(fieldTlv))
            {
                try
                {
                    foreach (KeyValuePair<string, Dataset> dataset in fieldTlv.Datasets)
                    {
                        foreach (KeyValuePair<string, Tag> tag in dataset.Value.Tags)
                        {
                            WriteLogCodecInfo(typeof(TlvFiller), string.Format("TlvFiller(): VISA Field {0} Dataset [{1}] Tag [{2}] Data [{3}]", field, dataset.Key, tag.Key, tag.Value.Value.GetHexString()));
                        }
                    }
                }
                catch (System.Exception)
                {
                    // Ignore
                }
            }
        }
    }
}

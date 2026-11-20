// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FieldFiller.cs" company="Cybersoft">
//   Author: Kaven
// </copyright>
// <summary>
//   Defines the FieldFiller type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.TM.VISA.Converter
{
    #region DEPENDENCY

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.MCOLA;

    #endregion

    internal class FieldFiller : ConvertUnitBase
    {
        protected internal static void FillField(FIELD field, VISA origin, MCOLA target, bool cond)
        {
            if (!ObjectUtils.IsNull(origin.RawVal(field)))
            {
                switch (field)
                {
                    case FIELD.F_41:
                        target.RawVal(field, origin.RawVal(field).ToString().PadRight(16, ' '));
                        break;

                    case FIELD.F_19:
                    case FIELD.F_23:
                    case FIELD.F_49:
                    case FIELD.F_51:
                    case FIELD.F_70:
                        target.RawVal(field, origin.RawVal(field).ToString().Substring(1, 3));
                        break;

                    case FIELD.F_22:
                        target.RawVal(field, origin.RawVal(field).ToString().Substring(0, 3));
                        break;

                    case FIELD.F_52:
                        target.RawVal(field, ((byte[])origin.RawVal(field)).GetHexString());
                        break;

                    default:
                        target.RawVal(field, origin.RawVal(field));
                        break;
                }
            }
            else
            {
                switch (cond)
                {
                    case true:
                        WriteLogCodecFail(typeof(FieldFiller), string.Format("FieldFiller(): Data element {0} not found.", field));
                        break;
                }
            }
        }

        protected internal static void FillField(FIELD field, MCOLA target, VISA origin, bool cond)
        {
            if (!ObjectUtils.IsNull(target.RawVal(field)))
            {
                origin.RawVal(field, target.RawVal(field));
            }
            else
            {
                switch (cond)
                {
                    case true:
                        WriteLogCodecFail(typeof(FieldFiller), string.Format("FieldFiller(): Data element {0} not found.", field));
                        break;
                }
            }
        }
    }
}

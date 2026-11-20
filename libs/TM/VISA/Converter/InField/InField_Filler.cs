// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InField_Filler.cs" company="Cybersoft">
//   Author: Kaven
// </copyright>
// <summary>
//   Defines the InField_Filler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.TM.VISA.Converter.InField
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.MCOLA;
    using Com.Cybersoft4u.TM.ISO8583.Nccc;
    using Com.Cybersoft4u.TM.VISA.Converter.InField.InField_63_Token;

    #endregion

    internal class InField_Filler : ConvertUnitBase
    {
        protected internal static void InField0100(VISA origin, MCOLA target)
        {
            var token = new TokenGroup();

            FieldFiller.FillField(FIELD.F_2, origin, target, false);
            FieldFiller.FillField(FIELD.F_4, origin, target, true);
            FieldFiller.FillField(FIELD.F_6, origin, target, false);
            FieldFiller.FillField(FIELD.F_7, origin, target, true);
            FieldFiller.FillField(FIELD.F_10, origin, target, false);
            FieldFiller.FillField(FIELD.F_11, origin, target, true);
            FieldFiller.FillField(FIELD.F_12, origin, target, false);
            FieldFiller.FillField(FIELD.F_13, origin, target, false);
            FieldFiller.FillField(FIELD.F_14, origin, target, false);
            FieldFiller.FillField(FIELD.F_18, origin, target, true);
            FieldFiller.FillField(FIELD.F_19, origin, target, true);
            FieldFiller.FillField(FIELD.F_22, origin, target, false);
            FieldFiller.FillField(FIELD.F_23, origin, target, false);
            FieldFiller.FillField(FIELD.F_25, origin, target, true);
            FieldFiller.FillField(FIELD.F_28, origin, target, false);
            FieldFiller.FillField(FIELD.F_32, origin, target, true);
            FieldFiller.FillField(FIELD.F_33, origin, target, false);
            FieldFiller.FillField(FIELD.F_35, origin, target, false);
            FieldFiller.FillField(FIELD.F_37, origin, target, true);
            FieldFiller.FillField(FIELD.F_41, origin, target, false);
            FieldFiller.FillField(FIELD.F_44, origin, target, false);
            FieldFiller.FillField(FIELD.F_42, origin, target, true);
            FieldFiller.FillField(FIELD.F_45, origin, target, false);
            FieldFiller.FillField(FIELD.F_49, origin, target, true);
            FieldFiller.FillField(FIELD.F_51, origin, target, false);
            FieldFiller.FillField(FIELD.F_52, origin, target, false);
            FieldFiller.FillField(FIELD.F_54, origin, target, false);
            FieldFiller.FillField(FIELD.F_95, origin, target, false);

            InField_3.ToTargetProcessingCode(origin, target);
            InField_43.ToCardAcceptorNameLocation(origin, target);
            InField_48.ToAdditionalData(origin, target);
            InField_124.ToTargetDE124(origin, target);
            InField_113.ToTargetDE113(origin, target);

            token.Decode(InField_63_04.ToPosAdditionalDataToken04(origin, target, token.Encode()));
            token.Decode(InField_63_B4.ToPosAdditionalDataTokenB4(origin, target, token.Encode()));
            token.Decode(InField_63_C0.ToPosAdditionalDataTokenC0(origin, target, token.Encode()));
            token.Decode(InField_63_B2.ToPosAdditionalDataTokenB2(origin, target, token.Encode()));
            token.Decode(InField_63_B3.ToPosAdditionalDataTokenB3(origin, target, token.Encode()));
            token.Decode(InField_63_C4.ToPosAdditionalDataTokenC4(origin, target, token.Encode()));
            token.Decode(InField_63_CH.ToPosAdditionalDataTokenCH(origin, target, token.Encode()));
            token.Decode(InField_63_U7.ToPosAdditionalDataTokenU7(origin, target, token.Encode()));
            token.Decode(InField_63_17.ToPosAdditionalDataToken17(origin, target, token.Encode()));
            token.Decode(InField_63_C6.ToPosAdditionalDataTokenC6(origin, target, token.Encode()));
            token.Decode(InField_63_B5.ToPosAdditionalDataTokenB5(origin, target, token.Encode()));

            target.RawVal(FIELD.F_63, token.Encode());
        }

        protected internal static void InField0101(VISA origin, MCOLA target)
        {
            var token = new TokenGroup();

            FieldFiller.FillField(FIELD.F_2, origin, target, false);
            FieldFiller.FillField(FIELD.F_4, origin, target, true);
            FieldFiller.FillField(FIELD.F_6, origin, target, false);
            FieldFiller.FillField(FIELD.F_7, origin, target, true);
            FieldFiller.FillField(FIELD.F_10, origin, target, false);
            FieldFiller.FillField(FIELD.F_11, origin, target, true);
            FieldFiller.FillField(FIELD.F_12, origin, target, false);
            FieldFiller.FillField(FIELD.F_13, origin, target, false);
            FieldFiller.FillField(FIELD.F_14, origin, target, false);
            FieldFiller.FillField(FIELD.F_18, origin, target, true);
            FieldFiller.FillField(FIELD.F_19, origin, target, true);
            FieldFiller.FillField(FIELD.F_22, origin, target, false);
            FieldFiller.FillField(FIELD.F_23, origin, target, false);
            FieldFiller.FillField(FIELD.F_25, origin, target, true);
            FieldFiller.FillField(FIELD.F_28, origin, target, false);
            FieldFiller.FillField(FIELD.F_32, origin, target, true);
            FieldFiller.FillField(FIELD.F_33, origin, target, false);
            FieldFiller.FillField(FIELD.F_35, origin, target, false);
            FieldFiller.FillField(FIELD.F_37, origin, target, true);
            FieldFiller.FillField(FIELD.F_41, origin, target, false);
            FieldFiller.FillField(FIELD.F_42, origin, target, true);
            FieldFiller.FillField(FIELD.F_44, origin, target, false);
            FieldFiller.FillField(FIELD.F_45, origin, target, false);
            FieldFiller.FillField(FIELD.F_49, origin, target, true);
            FieldFiller.FillField(FIELD.F_51, origin, target, false);
            FieldFiller.FillField(FIELD.F_52, origin, target, false);
            FieldFiller.FillField(FIELD.F_54, origin, target, false);
            FieldFiller.FillField(FIELD.F_95, origin, target, false);

            InField_3.ToTargetProcessingCode(origin, target);
            InField_43.ToCardAcceptorNameLocation(origin, target);
            InField_48.ToAdditionalData(origin, target);
            InField_124.ToTargetDE124(origin, target);
            InField_113.ToTargetDE113(origin, target);

            token.Decode(InField_63_04.ToPosAdditionalDataToken04(origin, target, token.Encode()));
            token.Decode(InField_63_B4.ToPosAdditionalDataTokenB4(origin, target, token.Encode()));
            token.Decode(InField_63_C0.ToPosAdditionalDataTokenC0(origin, target, token.Encode()));
            token.Decode(InField_63_B2.ToPosAdditionalDataTokenB2(origin, target, token.Encode()));
            token.Decode(InField_63_B3.ToPosAdditionalDataTokenB3(origin, target, token.Encode()));
            token.Decode(InField_63_C4.ToPosAdditionalDataTokenC4(origin, target, token.Encode()));
            token.Decode(InField_63_CH.ToPosAdditionalDataTokenCH(origin, target, token.Encode()));
            token.Decode(InField_63_U7.ToPosAdditionalDataTokenU7(origin, target, token.Encode()));
            token.Decode(InField_63_17.ToPosAdditionalDataToken17(origin, target, token.Encode()));
            token.Decode(InField_63_C6.ToPosAdditionalDataTokenC6(origin, target, token.Encode()));
            token.Decode(InField_63_B5.ToPosAdditionalDataTokenB5(origin, target, token.Encode()));

            target.RawVal(FIELD.F_63, token.Encode());
        }

        protected internal static void InField0120(VISA origin, MCOLA target)
        {
            var token = new TokenGroup();

            FieldFiller.FillField(FIELD.F_2, origin, target, false);
            FieldFiller.FillField(FIELD.F_4, origin, target, true);
            FieldFiller.FillField(FIELD.F_6, origin, target, false);
            FieldFiller.FillField(FIELD.F_7, origin, target, true);
            FieldFiller.FillField(FIELD.F_10, origin, target, false);
            FieldFiller.FillField(FIELD.F_11, origin, target, true);
            FieldFiller.FillField(FIELD.F_12, origin, target, false);
            FieldFiller.FillField(FIELD.F_13, origin, target, false);
            FieldFiller.FillField(FIELD.F_14, origin, target, false);
            FieldFiller.FillField(FIELD.F_18, origin, target, true);
            FieldFiller.FillField(FIELD.F_19, origin, target, true);
            FieldFiller.FillField(FIELD.F_22, origin, target, false);
            FieldFiller.FillField(FIELD.F_23, origin, target, false);
            FieldFiller.FillField(FIELD.F_25, origin, target, true);
            FieldFiller.FillField(FIELD.F_28, origin, target, false);
            FieldFiller.FillField(FIELD.F_32, origin, target, true);
            FieldFiller.FillField(FIELD.F_33, origin, target, false);
            FieldFiller.FillField(FIELD.F_35, origin, target, false);
            FieldFiller.FillField(FIELD.F_37, origin, target, true);
            FieldFiller.FillField(FIELD.F_38, origin, target, false);
            FieldFiller.FillField(FIELD.F_41, origin, target, false);
            FieldFiller.FillField(FIELD.F_42, origin, target, true);
            FieldFiller.FillField(FIELD.F_44, origin, target, false);
            FieldFiller.FillField(FIELD.F_45, origin, target, false);
            FieldFiller.FillField(FIELD.F_49, origin, target, true);
            FieldFiller.FillField(FIELD.F_51, origin, target, false);
            FieldFiller.FillField(FIELD.F_52, origin, target, false);
            FieldFiller.FillField(FIELD.F_54, origin, target, false);
            FieldFiller.FillField(FIELD.F_95, origin, target, false);

            InField_3.ToTargetProcessingCode(origin, target);
            InField_39.ToTargerResponeCode(origin, target);
            InField_43.ToCardAcceptorNameLocation(origin, target);
            InField_48.ToAdditionalData(origin, target);
            InField_90.ToOriginalDataElement(origin, target);
            InField_124.ToTargetDE124(origin, target);
            InField_113.ToTargetDE113(origin, target);

            token.Decode(InField_63_04.ToPosAdditionalDataToken04(origin, target, token.Encode()));
            token.Decode(InField_63_B4.ToPosAdditionalDataTokenB4(origin, target, token.Encode()));
            token.Decode(InField_63_C0.ToPosAdditionalDataTokenC0(origin, target, token.Encode()));
            token.Decode(InField_63_B2.ToPosAdditionalDataTokenB2(origin, target, token.Encode()));
            token.Decode(InField_63_B3.ToPosAdditionalDataTokenB3(origin, target, token.Encode()));
            token.Decode(InField_63_C4.ToPosAdditionalDataTokenC4(origin, target, token.Encode()));
            token.Decode(InField_63_CH.ToPosAdditionalDataTokenCH(origin, target, token.Encode()));
            token.Decode(InField_63_U7.ToPosAdditionalDataTokenU7(origin, target, token.Encode()));
            token.Decode(InField_63_17.ToPosAdditionalDataToken17(origin, target, token.Encode()));
            token.Decode(InField_63_C6.ToPosAdditionalDataTokenC6(origin, target, token.Encode()));

            target.RawVal(FIELD.F_63, token.Encode());
        }

        protected internal static void InField0400(VISA origin, MCOLA target)
        {
            var token = new TokenGroup();

            FieldFiller.FillField(FIELD.F_2, origin, target, false);
            FieldFiller.FillField(FIELD.F_4, origin, target, true);
            FieldFiller.FillField(FIELD.F_6, origin, target, false);
            FieldFiller.FillField(FIELD.F_7, origin, target, true);
            FieldFiller.FillField(FIELD.F_10, origin, target, false);
            FieldFiller.FillField(FIELD.F_11, origin, target, true);
            FieldFiller.FillField(FIELD.F_12, origin, target, false);
            FieldFiller.FillField(FIELD.F_13, origin, target, false);
            FieldFiller.FillField(FIELD.F_14, origin, target, false);
            FieldFiller.FillField(FIELD.F_18, origin, target, true);
            FieldFiller.FillField(FIELD.F_19, origin, target, true);
            FieldFiller.FillField(FIELD.F_22, origin, target, false);
            FieldFiller.FillField(FIELD.F_23, origin, target, false);
            FieldFiller.FillField(FIELD.F_25, origin, target, true);
            FieldFiller.FillField(FIELD.F_28, origin, target, false);
            FieldFiller.FillField(FIELD.F_32, origin, target, true);
            FieldFiller.FillField(FIELD.F_33, origin, target, false);
            FieldFiller.FillField(FIELD.F_35, origin, target, false);
            FieldFiller.FillField(FIELD.F_37, origin, target, true);
            FieldFiller.FillField(FIELD.F_38, origin, target, true);
            FieldFiller.FillField(FIELD.F_41, origin, target, false);
            FieldFiller.FillField(FIELD.F_42, origin, target, true);
            FieldFiller.FillField(FIELD.F_44, origin, target, false);
            FieldFiller.FillField(FIELD.F_45, origin, target, false);
            FieldFiller.FillField(FIELD.F_49, origin, target, true);
            FieldFiller.FillField(FIELD.F_51, origin, target, false);
            FieldFiller.FillField(FIELD.F_52, origin, target, false);
            FieldFiller.FillField(FIELD.F_54, origin, target, false);
            FieldFiller.FillField(FIELD.F_95, origin, target, false);

            InField_3.ToTargetProcessingCode(origin, target);
            InField_43.ToCardAcceptorNameLocation(origin, target);
            InField_48.ToAdditionalData(origin, target);
            InField_90.ToOriginalDataElement(origin, target);
            InField_124.ToTargetDE124(origin, target);
            InField_113.ToTargetDE113(origin, target);

            token.Decode(InField_63_04.ToPosAdditionalDataToken04(origin, target, token.Encode()));
            token.Decode(InField_63_C0.ToPosAdditionalDataTokenC0(origin, target, token.Encode()));
            token.Decode(InField_63_B2.ToPosAdditionalDataTokenB2(origin, target, token.Encode()));
            token.Decode(InField_63_B3.ToPosAdditionalDataTokenB3(origin, target, token.Encode()));
            token.Decode(InField_63_C4.ToPosAdditionalDataTokenC4(origin, target, token.Encode()));
            token.Decode(InField_63_CH.ToPosAdditionalDataTokenCH(origin, target, token.Encode()));
            token.Decode(InField_63_B4.ToPosAdditionalDataTokenB4(origin, target, token.Encode()));
            token.Decode(InField_63_17.ToPosAdditionalDataToken17(origin, target, token.Encode()));
            token.Decode(InField_63_C6.ToPosAdditionalDataTokenC6(origin, target, token.Encode()));
            token.Decode(InField_63_U7.ToPosAdditionalDataTokenU7(origin, target, token.Encode()));

            target.RawVal(FIELD.F_63, token.Encode());
        }

        protected internal static void InField0420(VISA origin, MCOLA target)
        {
            var token = new TokenGroup();

            FieldFiller.FillField(FIELD.F_2, origin, target, false);
            FieldFiller.FillField(FIELD.F_4, origin, target, true);
            FieldFiller.FillField(FIELD.F_6, origin, target, false);
            FieldFiller.FillField(FIELD.F_7, origin, target, true);
            FieldFiller.FillField(FIELD.F_10, origin, target, false);
            FieldFiller.FillField(FIELD.F_11, origin, target, true);
            FieldFiller.FillField(FIELD.F_12, origin, target, false);
            FieldFiller.FillField(FIELD.F_13, origin, target, false);
            FieldFiller.FillField(FIELD.F_14, origin, target, false);
            FieldFiller.FillField(FIELD.F_18, origin, target, true);
            FieldFiller.FillField(FIELD.F_19, origin, target, true);
            FieldFiller.FillField(FIELD.F_22, origin, target, false);
            FieldFiller.FillField(FIELD.F_23, origin, target, false);
            FieldFiller.FillField(FIELD.F_25, origin, target, true);
            FieldFiller.FillField(FIELD.F_28, origin, target, false);
            FieldFiller.FillField(FIELD.F_32, origin, target, true);
            FieldFiller.FillField(FIELD.F_33, origin, target, false);
            FieldFiller.FillField(FIELD.F_35, origin, target, false);
            FieldFiller.FillField(FIELD.F_37, origin, target, true);
            FieldFiller.FillField(FIELD.F_38, origin, target, false);
            FieldFiller.FillField(FIELD.F_41, origin, target, false);
            FieldFiller.FillField(FIELD.F_42, origin, target, true);
            FieldFiller.FillField(FIELD.F_44, origin, target, false);
            FieldFiller.FillField(FIELD.F_45, origin, target, false);
            FieldFiller.FillField(FIELD.F_49, origin, target, true);
            FieldFiller.FillField(FIELD.F_51, origin, target, false);
            FieldFiller.FillField(FIELD.F_52, origin, target, false);
            FieldFiller.FillField(FIELD.F_54, origin, target, false);
            FieldFiller.FillField(FIELD.F_95, origin, target, false);

            InField_3.ToTargetProcessingCode(origin, target);
            InField_39.ToTargerResponeCode(origin, target);
            InField_43.ToCardAcceptorNameLocation(origin, target);
            InField_48.ToAdditionalData(origin, target);
            InField_90.ToOriginalDataElement(origin, target);
            InField_124.ToTargetDE124(origin, target);
            InField_113.ToTargetDE113(origin, target);

            token.Decode(InField_63_04.ToPosAdditionalDataToken04(origin, target, token.Encode()));
            token.Decode(InField_63_C0.ToPosAdditionalDataTokenC0(origin, target, token.Encode()));
            token.Decode(InField_63_B2.ToPosAdditionalDataTokenB2(origin, target, token.Encode()));
            token.Decode(InField_63_B3.ToPosAdditionalDataTokenB3(origin, target, token.Encode()));
            token.Decode(InField_63_C4.ToPosAdditionalDataTokenC4(origin, target, token.Encode()));
            token.Decode(InField_63_CH.ToPosAdditionalDataTokenCH(origin, target, token.Encode()));
            token.Decode(InField_63_B4.ToPosAdditionalDataTokenB4(origin, target, token.Encode()));
            token.Decode(InField_63_17.ToPosAdditionalDataToken17(origin, target, token.Encode()));
            token.Decode(InField_63_C6.ToPosAdditionalDataTokenC6(origin, target, token.Encode()));
            token.Decode(InField_63_U7.ToPosAdditionalDataTokenU7(origin, target, token.Encode()));

            target.RawVal(FIELD.F_63, token.Encode());
        }

        protected internal static void InField0312(VISA origin, MCOLA target)
        {
            FieldFiller.FillField(FIELD.F_2, origin, target, false);
            FieldFiller.FillField(FIELD.F_7, origin, target, true);
            FieldFiller.FillField(FIELD.F_11, origin, target, true);
            FieldFiller.FillField(FIELD.F_14, origin, target, false);
            FieldFiller.FillField(FIELD.F_19, origin, target, false);
            FieldFiller.FillField(FIELD.F_32, origin, target, false);
            FieldFiller.FillField(FIELD.F_37, origin, target, true);
            FieldFiller.FillField(FIELD.F_73, origin, target, false);
            FieldFiller.FillField(FIELD.F_91, origin, target, true);
            FieldFiller.FillField(FIELD.F_92, origin, target, false);
            FieldFiller.FillField(FIELD.F_115, origin, target, false);
            FieldFiller.FillField(FIELD.F_121, origin, target, false);

            InField_39.ToTargerResponeCode(origin, target);
            InField_48.ToAdditionalData(origin, target);
            InField_101.ToFileName(origin, target);
            InField_124.ToTargetDE124(origin, target);
            InField_120.ToTargetDE120(origin, target);
        }

        protected internal static void InField0800(VISA origin, MCOLA target)
        {
            FieldFiller.FillField(FIELD.F_7, origin, target, true);
            FieldFiller.FillField(FIELD.F_11, origin, target, true);
            FieldFiller.FillField(FIELD.F_37, origin, target, false);

            InField_48.ToAdditionalData(origin, target);
            InField_70.ToNetworkManagementInformationCode(origin, target);
        }

        protected internal static void InField0810(VISA origin, MCOLA target)
        {
            FieldFiller.FillField(FIELD.F_7, origin, target, true);
            FieldFiller.FillField(FIELD.F_11, origin, target, true);
            FieldFiller.FillField(FIELD.F_37, origin, target, false);

            InField_39.ToTargerResponeCode(origin, target);
            InField_48.ToAdditionalData(origin, target);
            InField_70.ToNetworkManagementInformationCode(origin, target);
        }

        protected internal static void InField0600(VISA origin, MCOLA target)
        {
            var token = new TokenGroup();

            FieldFiller.FillField(FIELD.F_2, origin, target, true);
            FieldFiller.FillField(FIELD.F_7, origin, target, true);
            FieldFiller.FillField(FIELD.F_11, origin, target, true);
            FieldFiller.FillField(FIELD.F_14, origin, target, false);
            FieldFiller.FillField(FIELD.F_15, origin, target, false);
            FieldFiller.FillField(FIELD.F_37, origin, target, true);

            InField_39.ToTargerResponeCode(origin, target);
            InField_48.ToAdditionalData(origin, target);
            InField_70.ToNetworkManagementInformationCode(origin, target);
            InField_101.ToFileName(origin, target);
            InField_124.ToTargetDE124(origin, target);

            token.Decode(InField_63_17.ToPosAdditionalDataToken17(origin, target, token.Encode()));

            target.RawVal(FIELD.F_63, token.Encode());
        }

        protected internal static void InField0620(VISA origin, MCOLA target)
        {
            var token = new TokenGroup();

            FieldFiller.FillField(FIELD.F_2, origin, target, true);
            FieldFiller.FillField(FIELD.F_7, origin, target, true);
            FieldFiller.FillField(FIELD.F_11, origin, target, true);
            FieldFiller.FillField(FIELD.F_14, origin, target, false);
            FieldFiller.FillField(FIELD.F_15, origin, target, false);
            FieldFiller.FillField(FIELD.F_37, origin, target, true);

            InField_39.ToTargerResponeCode(origin, target);
            InField_48.ToAdditionalData(origin, target);
            InField_70.ToNetworkManagementInformationCode(origin, target);
            InField_124.ToTargetDE124(origin, target);

            token.Decode(InField_63_17.ToPosAdditionalDataToken17(origin, target, token.Encode()));

            target.RawVal(FIELD.F_63, token.Encode());
        }

        protected internal static void InFieldRejectHeader14(VISA origin, MCOLA target)
        {
            FieldFiller.FillField(FIELD.F_2, origin, target, false);
            FieldFiller.FillField(FIELD.F_7, origin, target, true);
            FieldFiller.FillField(FIELD.F_11, origin, target, true);
            FieldFiller.FillField(FIELD.F_35, origin, target, false);
            FieldFiller.FillField(FIELD.F_37, origin, target, true);
            FieldFiller.FillField(FIELD.F_38, origin, target, false);

            InField_39.ToTargerResponeCode(origin, target);
            InField_44.ToAdditionalResponseData(origin, target);
        }
    }
}

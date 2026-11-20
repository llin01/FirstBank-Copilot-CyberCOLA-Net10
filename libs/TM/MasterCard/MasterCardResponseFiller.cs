// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="MasterCardResponseFiller.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the MasterCardResponseFiller type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.MasterCard
{
    using Com.Cybersoft4u.CyberUtils.Utils;
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583;
    using DE48;
    using System.Text;

    #endregion

    public class MasterCardResponseFiller
    {
        public static void DoFill(MasterCard response, MasterCard request)
        {
            switch (response.Mti())
            {
                case "0110":
                    Response0110Process(response, request);
                    break;
                case "0130":
                    Response0130Process(response, request);
                    break;
                case "0410":
                    Response0410Process(response, request);
                    break;
                case "0430":
                    Response0430Process(response, request);
                    break;
                case "0610":
                    Response0610Process(response, request);
                    break;
                case "0630":
                    Response0630Process(response, request);
                    break;
                case "0810":
                    Response0810Process(response, request);
                    break;
            }
        }


        private static void FillResponseField(FIELD field, MasterCard response, MasterCard request)
        {
            response.Val(field, request.Val(field));
        }

        private static void Response0110Process(MasterCard response, MasterCard request)
        {
            FillResponseField(FIELD.F_2, response, request);
            FillResponseField(FIELD.F_3, response, request);
            FillResponseField(FIELD.F_4, response, request);
            FillResponseField(FIELD.F_5, response, request);
            FillResponseField(FIELD.F_6, response, request);
            FillResponseField(FIELD.F_7, response, request);
            FillResponseField(FIELD.F_9, response, request);
            FillResponseField(FIELD.F_10, response, request);
            FillResponseField(FIELD.F_11, response, request);
            FillResponseField(FIELD.F_15, response, request);
            FillResponseField(FIELD.F_16, response, request);
            FillResponseField(FIELD.F_20, response, request);
            FillResponseField(FIELD.F_28, response, request);
            FillResponseField(FIELD.F_32, response, request);
            FillResponseField(FIELD.F_33, response, request);
            FillResponseField(FIELD.F_37, response, request);
            FillResponseField(FIELD.F_41, response, request);
            FillResponseField(FIELD.F_44, response, request);

            //DE 48 SE 42
            if (request.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                MasterCardTransactionConst.AdditionalDataPrivateUse.ElectronicCommerceIndicatorsTagId))
            {
                var de48se42 = request.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                    MasterCardTransactionConst.AdditionalDataPrivateUse.ElectronicCommerceIndicatorsTagId];
                response.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                    MasterCardTransactionConst.AdditionalDataPrivateUse.ElectronicCommerceIndicatorsTagId] = de48se42;
            }
            //DE 48 SE 77
            if (request.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                MasterCardTransactionConst.AdditionalDataPrivateUse.FundingPaymentTransactionTypeIndicator))
            {
                var de48se77 = request.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                    MasterCardTransactionConst.AdditionalDataPrivateUse.FundingPaymentTransactionTypeIndicator];
                response.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                    MasterCardTransactionConst.AdditionalDataPrivateUse.FundingPaymentTransactionTypeIndicator] = de48se77;
            }
            //DE 48 SE 82 & 83
            if (request.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                MasterCardTransactionConst.AdditionalDataPrivateUse.AddressVerificationRequest))
            {
                var de48se82 = request.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                    MasterCardTransactionConst.AdditionalDataPrivateUse.AddressVerificationRequest];
                response.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                    MasterCardTransactionConst.AdditionalDataPrivateUse.AddressVerificationRequest] = de48se82;
                if (MasterCardTransactionConst.MemberDefinedData.AddressVerificationRequestAndAuthorization.AVSAndAuth.Equals(de48se82.Value))
                {
                    if (!request.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                        MasterCardTransactionConst.AdditionalDataPrivateUse.AddressVerificationResponse))
                    {
                        var stringBuilder = new StringBuilder();
                        stringBuilder.Append(MasterCardTransactionConst.AdditionalDataPrivateUse.AddressVerificationResponse);
                        stringBuilder.Append(MasterCardTransactionConst.MemberDefinedData.AddressVerificationResponseAndAuthorization.AVSRespLength);
                        stringBuilder.Append(MasterCardTransactionConst.MemberDefinedData.AddressVerificationResponseAndAuthorization.AVSRespCurrentlyNotSupport);
                        MasterCardDE48SE83 de48se83 = new MasterCardDE48SE83();
                        de48se83.Decode(stringBuilder.ToString());
                        response.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.Add(
                            MasterCardTransactionConst.AdditionalDataPrivateUse.AddressVerificationResponse, de48se83);
                    }
                    else
                    {
                        var de48se83 = request.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                                       MasterCardTransactionConst.AdditionalDataPrivateUse.AddressVerificationResponse];
                        de48se83.Value = MasterCardTransactionConst.MemberDefinedData.AddressVerificationResponseAndAuthorization.AVSRespCurrentlyNotSupport;
                        response.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                        MasterCardTransactionConst.AdditionalDataPrivateUse.AddressVerificationResponse] = de48se83;
                    }
                }
            }

            // DE 48 SE 92
            if (request.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                MasterCardTransactionConst.AdditionalDataPrivateUse.Cvc2TagId))
            {
                var de48se92 = request.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                    MasterCardTransactionConst.AdditionalDataPrivateUse.Cvc2TagId];

                response.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                    MasterCardTransactionConst.AdditionalDataPrivateUse.Cvc2TagId] = de48se92;
            }
            response.Val(FIELD.F_48, response.F48AdditionalDataPrivateUseEncode());

            FillResponseField(FIELD.F_49, response, request);
            FillResponseField(FIELD.F_50, response, request);
            FillResponseField(FIELD.F_51, response, request);

            // DE 54 fill for account type
            var reqDe3 = request.RawVal(FIELD.F_3) as string;
            var respDe54 = response.RawVal(FIELD.F_54) as string;
            if ((!ObjectUtils.IsNull(respDe54)) && (!ObjectUtils.IsNull(reqDe3)))
            {
                var reqDe3AccountType = reqDe3.Substring(2, 2);
                var f54 = string.Empty;
                var f54sb = new StringBuilder();
                for (int i = 0; i < respDe54.Length; i = i + 20)
                {
                    f54sb.Append(reqDe3AccountType);
                    f54sb.Append(respDe54.Substring((i + 2), 18));
                }

                response.Val(FIELD.F_54, f54sb.ToString().TrimEnd());
            }

            FillResponseField(FIELD.F_62, response, request);
            FillResponseField(FIELD.F_63, response, request);
            FillResponseField(FIELD.F_112, response, request);
            FillResponseField(FIELD.F_120, response, request);
            //FillResponseField(FIELD.F_124, response, request);
            FillResponseField(FIELD.F_127, response, request);
        }
        private static void Response0130Process(MasterCard response, MasterCard request)
        {
            FillResponseField(FIELD.F_2, response, request);
            FillResponseField(FIELD.F_3, response, request);
            FillResponseField(FIELD.F_4, response, request);
            FillResponseField(FIELD.F_5, response, request);
            FillResponseField(FIELD.F_6, response, request);
            FillResponseField(FIELD.F_7, response, request);
            FillResponseField(FIELD.F_9, response, request);
            FillResponseField(FIELD.F_10, response, request);
            FillResponseField(FIELD.F_11, response, request);
            FillResponseField(FIELD.F_15, response, request);
            FillResponseField(FIELD.F_16, response, request);
            FillResponseField(FIELD.F_20, response, request);
            FillResponseField(FIELD.F_23, response, request);
            FillResponseField(FIELD.F_28, response, request);
            FillResponseField(FIELD.F_32, response, request);
            FillResponseField(FIELD.F_33, response, request);
            FillResponseField(FIELD.F_37, response, request);
            FillResponseField(FIELD.F_41, response, request);
            FillResponseField(FIELD.F_44, response, request);

            if ("190".Equals(request.ExtendedCore().AdviceReasonCode.AdviceReasonCodeSubfield) ||
                "191".Equals(request.ExtendedCore().AdviceReasonCode.AdviceReasonCodeSubfield))
            {
                MasterCardDE48SE33 de48se33 = new MasterCardDE48SE33();
                de48se33 = null;
                if (response.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                    MasterCardTransactionConst.AdditionalDataPrivateUse.PanMappingFileInformationTagId))
                {
                    de48se33 = (MasterCardDE48SE33)response.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                        MasterCardTransactionConst.AdditionalDataPrivateUse.PanMappingFileInformationTagId];
                }
                FillResponseField(FIELD.F_48, response, request);
                response.F48AdditionalDataPrivateUseDecode(response.RawVal(FIELD.F_48).ToString());
                if (de48se33 != null)
                {
                    response.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                        MasterCardTransactionConst.AdditionalDataPrivateUse.PanMappingFileInformationTagId] = de48se33;
                }
                response.Val(FIELD.F_48, response.F48AdditionalDataPrivateUseEncode());
            }

            FillResponseField(FIELD.F_49, response, request);
            FillResponseField(FIELD.F_50, response, request);
            FillResponseField(FIELD.F_51, response, request);
            FillResponseField(FIELD.F_62, response, request);
            FillResponseField(FIELD.F_63, response, request);
            FillResponseField(FIELD.F_127, response, request);
        }
        private static void Response0410Process(MasterCard response, MasterCard request)
        {
            FillResponseField(FIELD.F_2, response, request);
            FillResponseField(FIELD.F_3, response, request);
            FillResponseField(FIELD.F_4, response, request);
            FillResponseField(FIELD.F_5, response, request);
            FillResponseField(FIELD.F_6, response, request);
            FillResponseField(FIELD.F_7, response, request);
            FillResponseField(FIELD.F_9, response, request);
            FillResponseField(FIELD.F_10, response, request);
            FillResponseField(FIELD.F_11, response, request);
            FillResponseField(FIELD.F_15, response, request);
            FillResponseField(FIELD.F_16, response, request);
            FillResponseField(FIELD.F_20, response, request);
            FillResponseField(FIELD.F_28, response, request);
            FillResponseField(FIELD.F_32, response, request);
            FillResponseField(FIELD.F_33, response, request);
            FillResponseField(FIELD.F_37, response, request);
            FillResponseField(FIELD.F_41, response, request);

            MasterCardDE48SE33 de48se33 = new MasterCardDE48SE33();
            de48se33 = null;
            if (response.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                MasterCardTransactionConst.AdditionalDataPrivateUse.PanMappingFileInformationTagId))
            {
                de48se33 = (MasterCardDE48SE33)response.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                    MasterCardTransactionConst.AdditionalDataPrivateUse.PanMappingFileInformationTagId];
            }
            FillResponseField(FIELD.F_48, response, request);
            response.F48AdditionalDataPrivateUseDecode(response.RawVal(FIELD.F_48).ToString());
            if (de48se33 != null)
            {
                response.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                    MasterCardTransactionConst.AdditionalDataPrivateUse.PanMappingFileInformationTagId] = de48se33;
            }
            response.Val(FIELD.F_48, response.F48AdditionalDataPrivateUseEncode());

            FillResponseField(FIELD.F_49, response, request);
            FillResponseField(FIELD.F_50, response, request);
            FillResponseField(FIELD.F_51, response, request);
            FillResponseField(FIELD.F_62, response, request);
            FillResponseField(FIELD.F_63, response, request);
            FillResponseField(FIELD.F_90, response, request);
            FillResponseField(FIELD.F_95, response, request);
            FillResponseField(FIELD.F_112, response, request);
            FillResponseField(FIELD.F_124, response, request);
            FillResponseField(FIELD.F_127, response, request);
        }
        private static void Response0430Process(MasterCard response, MasterCard request)
        {
            FillResponseField(FIELD.F_2, response, request);
            FillResponseField(FIELD.F_3, response, request);
            FillResponseField(FIELD.F_4, response, request);
            FillResponseField(FIELD.F_5, response, request);
            FillResponseField(FIELD.F_6, response, request);
            FillResponseField(FIELD.F_7, response, request);
            FillResponseField(FIELD.F_9, response, request);
            FillResponseField(FIELD.F_10, response, request);
            FillResponseField(FIELD.F_11, response, request);
            FillResponseField(FIELD.F_15, response, request);
            FillResponseField(FIELD.F_16, response, request);
            FillResponseField(FIELD.F_20, response, request);
            FillResponseField(FIELD.F_28, response, request);
            FillResponseField(FIELD.F_32, response, request);
            FillResponseField(FIELD.F_33, response, request);
            FillResponseField(FIELD.F_37, response, request);
            FillResponseField(FIELD.F_49, response, request);
            FillResponseField(FIELD.F_50, response, request);
            FillResponseField(FIELD.F_51, response, request);
            FillResponseField(FIELD.F_62, response, request);
            FillResponseField(FIELD.F_63, response, request);
            FillResponseField(FIELD.F_90, response, request);
            FillResponseField(FIELD.F_95, response, request);
            FillResponseField(FIELD.F_127, response, request);
        }
        private static void Response0610Process(MasterCard response, MasterCard request)
        {
            FillResponseField(FIELD.F_2, response, request);
            FillResponseField(FIELD.F_7, response, request);
            FillResponseField(FIELD.F_11, response, request);
            FillResponseField(FIELD.F_32, response, request);
            FillResponseField(FIELD.F_33, response, request);
            FillResponseField(FIELD.F_41, response, request);
            FillResponseField(FIELD.F_42, response, request);
            FillResponseField(FIELD.F_43, response, request);
            FillResponseField(FIELD.F_60, response, request);
            FillResponseField(FIELD.F_62, response, request);
            FillResponseField(FIELD.F_63, response, request);
        }
        private static void Response0630Process(MasterCard response, MasterCard request)
        {
            FillResponseField(FIELD.F_7, response, request);
            FillResponseField(FIELD.F_11, response, request);
            FillResponseField(FIELD.F_33, response, request);
            FillResponseField(FIELD.F_62, response, request);
            FillResponseField(FIELD.F_63, response, request);
            FillResponseField(FIELD.F_100, response, request);
        }
        private static void Response0810Process(MasterCard response, MasterCard request)
        {
            FillResponseField(FIELD.F_2, response, request);
            FillResponseField(FIELD.F_7, response, request);
            FillResponseField(FIELD.F_11, response, request);
            FillResponseField(FIELD.F_33, response, request);
            FillResponseField(FIELD.F_48, response, request);
            FillResponseField(FIELD.F_63, response, request);
            FillResponseField(FIELD.F_70, response, request);
        }
    }
}

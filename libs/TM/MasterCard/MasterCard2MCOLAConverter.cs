namespace Com.Cybersoft4u.TM.MasterCard
{
    #region DEPENDENCY

    using System;
    using System.Text;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.Exception;
    using Com.Cybersoft4u.TM.ISO8583.TLV;
    using Com.Cybersoft4u.TM.ISO8583.Nccc;
    using Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens;
    using Com.Cybersoft4u.TM.ISO8583.MCOLA;
    using Com.Cybersoft4u.TM.ISO8583.MCOLA.Transaction;
    using Com.Cybersoft4u.TM.MasterCard.DE48;
    using Com.Cybersoft4u.TM.MasterCard.Transaction;
    using System.Linq;

    #endregion

    public class MasterCard2MCOLAConverter : MessageConverter<MasterCard, MCOLA>
    {
        public static readonly MasterCard2MCOLAConverter Default = new MasterCard2MCOLAConverter();

        public override MCOLA ToTarget(MasterCard origin)
        {
            var target = this.GenerateTarget(origin);

            if (target is SaleRequest)
            {
                this.ToTargetSaleRequest(origin, target);
            }
            else if (target is PreAuthRequest)
            {
                this.ToTargetPreAuthRequest(origin, target);
            }
            else if (target is PreAuthCompleteRequest)
            {
                this.ToTargetPreAuthCompleteRequest(origin, target);
            }
            else if (target is SaleAdviceRequest)
            {
                this.ToTargetSaleAdviceRequest(origin, target);
            }
            else if (target is ISO8583.MCOLA.Transaction.AuthorizationResponseNegAck)
            {
                this.ToTargetAuthorizationResponseNegAck(origin, target);
            }
            else if (target is FileUpdateResponse)
            {
                this.ToTargetFileUpdateResponse(origin, target);
            }
            else if (target is ISO8583.MCOLA.Transaction.ReversalRequest)
            {
                this.ToTargetReversalRequest(origin, target);
            }
            else if (target is ISO8583.MCOLA.Transaction.ReversalAdviceRequest)
            {
                this.ToTargetReversalAdviceRequest(origin, target);
            }
            else if (target is ISO8583.MCOLA.Transaction.AdministrativeRequest)
            {
                this.ToTargetAdministrativeRequest(origin, target);
            }
            else if (target is ISO8583.MCOLA.Transaction.AdministrativeAdviceRequest)
            {
                this.ToTargetAdministrativeAdviceRequest(origin, target);
            }
            else if (target is ISO8583.MCOLA.Transaction.SignOnResponse)
            {
                this.ToTargetGroupSignOnResponse(origin, target);
            }
            else if (target is ISO8583.MCOLA.Transaction.SignOffResponse)
            {
                this.ToTargetGroupSignOffResponse(origin, target);
            }
            else if (target is ISO8583.MCOLA.Transaction.ChangeKeyRequest)
            {
                this.ToTargetChangeKeyRequest(origin, target);
            }
            else if (target is ISO8583.MCOLA.Transaction.ChangeKeyAdviceRequest)
            {
                this.ToTargetChangeKeyAdviceRequest(origin, target);
            }
            else if (target is ISO8583.MCOLA.Transaction.AlternativeNetworkRoutingResponse)
            {
                this.ToTargetAlternativeNetworkRoutingResponse(origin, target);
            }
            else if (target is ISO8583.MCOLA.Transaction.TransactionHistoryAdviceResponse)
            {
                this.ToTargetTransactionHistoryAdviceResponse(origin, target);
            }

            if (!ObjectUtils.IsNull(target))
            {
                target.Prepare();
            }

            return target;
        }

        public override MasterCard ToOrigin(MCOLA target)
        {
            var origin = this.GenerateOrigin(target);

            if (origin is AuthorizationResponse)
            {
                this.FromTargetAuthorizationResponse(target, origin);
            }
            else if (origin is AuthorizationAdviceResponse)
            {
                this.FromTargetAuthorizationAdviceResponse(target, origin);
            }
            else if (origin is TM.MasterCard.Transaction.IssuerFileUpdateRequest)
            {
                this.FromTargetIssuerFileUpdateRequest(target, origin);
            }
            else if (origin is TM.MasterCard.Transaction.ReversalResponse)
            {
                this.FromTargetReversalResponse(target, origin);
            }
            else if (origin is TM.MasterCard.Transaction.AdministrativeResponse)
            {
                this.FromTargetAdministrativeResponse(target, origin);
            }
            else if (origin is TM.MasterCard.Transaction.AdministrativeAdviceResponse)
            {
                this.FromTargetAdministrativeAdviceResponse(target, origin);
            }
            else if (origin is GroupSignOnRequest)
            {
                this.FromTargetGroupSignOnRequest(target, origin);
            }
            else if (origin is GroupSignOffRequest)
            {
                this.FromTargetGroupSignOffRequest(target, origin);
            }
            else if (origin is PekExchangeResponse)
            {
                this.FromTargetPekExchangeResponse(target, origin);
            }
            else if (origin is TM.MasterCard.Transaction.AlternativeNetworkRoutingRequest)
            {
                this.FromAlternativeNetworkRoutingRequest(target, origin);
            }
            else if (origin is TM.MasterCard.Transaction.TransactionHistoryAdviceRequest)
            {
                this.FromTransactionHistoryAdviceRequest(target, origin);
            }

            if (!ObjectUtils.IsNull(origin))
            {
                origin.Prepare();
            }

            return origin;
        }

        protected MCOLA GenerateTarget(MasterCard origin)
        {
            switch (origin.Mti())
            {
                case MasterCardTransactionConst.MTI.AuthorizationRequest:
                    if (!string.IsNullOrEmpty(origin.ExtendedCore().PointOfServiceData.PosTransactionStatus)
                        &&
                        origin.ExtendedCore().PointOfServiceData.PosTransactionStatus.Equals(
                        MasterCardTransactionConst.PointOfServiceData.PosTransactionStatus.PreAuthorizedRequest))
                    {
                        return MCOLATransCreator.PreAuthRequest().Core(origin.Core().Copy());
                    }
                    //else if (origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                    //    MasterCardTransactionConst.AdditionalDataPrivateUse.PosDataExtendedConditionCodesTagId)
                    //    &&
                    //    !string.IsNullOrEmpty(((MasterCardDE48SE61)origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                    //    MasterCardTransactionConst.AdditionalDataPrivateUse.PosDataExtendedConditionCodesTagId])
                    //    .FinalAuthorizationIndicator)
                    //    &&
                    //    ((MasterCardDE48SE61)origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                    //    MasterCardTransactionConst.AdditionalDataPrivateUse.PosDataExtendedConditionCodesTagId])
                    //    .FinalAuthorizationIndicator.Equals(
                    //    MasterCardTransactionConst.AdditionalDataPrivateUse.PosDataExtendedConditionCodes.FinalAuthorization))
                    //{
                    //    return MCOLATransCreator.PreAuthCompleteRequest().Core(origin.Core().Copy());
                    //}
                    else
                    {
                        return MCOLATransCreator.SaleRequest().Core(origin.Core().Copy());
                    }
                case MasterCardTransactionConst.MTI.AuthorizationAdviceRequest:
                    return MCOLATransCreator.SaleAdviceRequest().Core(origin.Core().Copy());
                case MasterCardTransactionConst.MTI.AuthorizationResponseNegAck:
                    return MCOLATransCreator.AuthorizationResponseNegAck().Core(origin.Core().Copy());
                case MasterCardTransactionConst.MTI.IssuerFileUpdateResponse:
                    return MCOLATransCreator.FileUpdateResponse().Core(origin.Core().Copy());
                case MasterCardTransactionConst.MTI.ReversalRequest:
                    return MCOLATransCreator.ReversalRequest().Core(origin.Core().Copy());
                case MasterCardTransactionConst.MTI.ReversalAdviceRequest:
                    return MCOLATransCreator.ReversalAdviceRequest().Core(origin.Core().Copy());
                case MasterCardTransactionConst.MTI.AdministrativeRequest:
                    return MCOLATransCreator.AdministrativeRequest().Core(origin.Core().Copy());
                case MasterCardTransactionConst.MTI.AdministrativeAdviceRequest:
                    return MCOLATransCreator.AdministrativeAdviceRequest().Core(origin.Core().Copy());
                case MasterCardTransactionConst.MTI.NetworkManagementResponse:
                    if (!string.IsNullOrEmpty(origin.Core().NetworkManagementInformationCode))
                    {
                        switch (origin.Core().NetworkManagementInformationCode)
                        {
                            case MasterCardTransactionConst.NetworkManagementInformationCode.GroupSignOn:
                            case MasterCardTransactionConst.NetworkManagementInformationCode.SignOn:
                                return MCOLATransCreator.SignOnResponse().Core(origin.Core().Copy());
                            case MasterCardTransactionConst.NetworkManagementInformationCode.GroupSignOff:
                            case MasterCardTransactionConst.NetworkManagementInformationCode.SignOff:
                                return MCOLATransCreator.SignOffResponse().Core(origin.Core().Copy());
                        }
                    }

                    break;
                case MasterCardTransactionConst.MTI.NetworkManagementRequest:
                    if (!string.IsNullOrEmpty(origin.Core().NetworkManagementInformationCode))
                    {
                        switch (origin.Core().NetworkManagementInformationCode)
                        {
                            case MasterCardTransactionConst.NetworkManagementInformationCode.EncryptionKeyExchangeRequest:
                            case MasterCardTransactionConst.NetworkManagementInformationCode.EncryptionKeyExchangeOnDemandRequest:
                                return MCOLATransCreator.ChangeKeyRequest().Core(origin.Core().Copy());
                        }
                    }

                    break;
                case MasterCardTransactionConst.MTI.NetworkManagementAdviceRequest:
                    if (!string.IsNullOrEmpty(origin.Core().NetworkManagementInformationCode))
                    {
                        switch (origin.Core().NetworkManagementInformationCode)
                        {
                            case MasterCardTransactionConst.NetworkManagementInformationCode.EncryptionKeyExchangeRequest:
                            case MasterCardTransactionConst.NetworkManagementInformationCode.EncryptionKeyExchangeOnDemandRequest:
                                return MCOLATransCreator.ChangeKeyAdviceRequest().Core(origin.Core().Copy());
                        }
                    }

                    break;
                case MasterCardTransactionConst.MTI.AlternativeNetworkRoutingResponse:
                    return MCOLATransCreator.AlternativeNetworkRoutingResponse().Core(origin.Core().Copy());
                case MasterCardTransactionConst.MTI.TransactionHistoryAdviceResponse:
                    return MCOLATransCreator.TransactionHistoryAdviceResponse().Core(origin.Core().Copy());
            }

            throw new TransactionConversionException("No Such Conversion.");
        }

        protected MasterCard GenerateOrigin(MCOLA target)
        {
            switch (target.Mti())
            {
                case MCOLATransactionConst.MTI.NetworkManagementRequest:
                    switch (target.Core().NetworkManagementInformationCode)
                    {
                        case MCOLATransactionConst.NetworkManagementInformationCode.SignOn:
                            //// GroupSignOnRequest or SignOnRequest
                            return MasterCardTransCreator.GroupSignOnRequest().Core(target.Core().Copy());
                        case MCOLATransactionConst.NetworkManagementInformationCode.SignOff:
                            //// GroupSignOffRequest or SignOffRequest
                            return MasterCardTransCreator.GroupSignOffRequest().Core(target.Core().Copy());
                    }

                    break;
                case MCOLATransactionConst.MTI.NetworkManagementResponse:
                    switch (target.Core().NetworkManagementInformationCode)
                    {
                        case MCOLATransactionConst.NetworkManagementInformationCode.ChangeKey:
                            return MasterCardTransCreator.PekExchangeResponse().Core(target.Core().Copy());
                        case MCOLATransactionConst.NetworkManagementInformationCode.NewKey:
                            return MasterCardTransCreator.PekExchangeResponse().Core(target.Core().Copy());
                    }

                    break;
                case MCOLATransactionConst.MTI.SaleResponse:
                case MCOLATransactionConst.MTI.PreAuthResponse:
                    return MasterCardTransCreator.AuthorizationResponse().Core(target.Core().Copy());
                case MCOLATransactionConst.MTI.SaleAdviceResponse:
                    return MasterCardTransCreator.AuthorizationAdviceResponse().Core(target.Core().Copy());
                case MCOLATransactionConst.MTI.FileUpdateRequest:
                    return MasterCardTransCreator.IssuerFileUpdateRequest().Core(target.Core().Copy());
                case MCOLATransactionConst.MTI.AdministrativeResponse:
                    return MasterCardTransCreator.AdministrativeResponse().Core(target.Core().Copy());
                case MCOLATransactionConst.MTI.AdministrativeAdviceResponse:
                    return MasterCardTransCreator.AdministrativeAdviceResponse().Core(target.Core().Copy());
                case MCOLATransactionConst.MTI.ReversalResponse:
                    return MasterCardTransCreator.ReversalResponse().Core(target.Core().Copy());
                case MCOLATransactionConst.MTI.ReversalAdviceResponse:
                    return MasterCardTransCreator.ReversalAdviceResponse().Core(target.Core().Copy());
                case MCOLATransactionConst.MTI.AlternativeNetworkRoutingRequest:
                    return MasterCardTransCreator.AlternativeNetworkRoutingRequest().Core(target.Core().Copy());
                case MCOLATransactionConst.MTI.TransactionHistoryAdviceRequest:
                    return MasterCardTransCreator.TransactionHistoryAdviceRequest().Core(target.Core().Copy());
            }

            throw new TransactionConversionException("No Such Conversion.");
        }

        //// MCOLA 0200
        protected void ToTargetSaleRequest(MasterCard origin, MCOLA target)
        {
            //// MCOLA DE 3
            this.ToTargetProcessingCode(origin, target);
            //// MCOLA DE 6
            this.ToTargetCardholderBillingAmount(origin, target);
            //// MCOLA DE 10
            this.ToTargetCardholderBillingConversionRate(origin, target);
            //// MCOLA DE 22
            this.ToTargetPointOfServiceEntryMode(origin, target);
            //// MCOLA DE 23
            this.ToTargetCardSequenceNumber(origin, target);
            //// MCOLA DE 25
            this.ToTargetPointOfServiceConditionMode(origin, target);
            //// MCOLA DE 28
            this.ToTargetTransactionFeeAmount(origin, target);
            //// MCOLA DE 33
            this.ToTargetForwardingInstitutionIdCode(origin, target);
            //// MCOLA DE 43
            this.ToTargetCardAcceptorNameLocation(origin, target);
            //// MCOLA DE 48
            this.ToTargetAdditionalData(origin, target);
            //// MCOLA DE 51
            this.ToTargetCardholderBillingCurrencyCode(origin, target);

            var tokenIdList = new List<string>();

            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.Token04.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.Token20.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenB0.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenB2.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenB3.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenB4.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenC0.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenC4.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenCE.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenCH.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenCZ.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenF1.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenU7.Id);

            //// MCOLA DE 63
            this.ToTargetPosAdditionalData(origin, target, tokenIdList);
            //// MCOLA DE 120
            this.ToTargetRecordData(origin, target);
            //// MCOLA DE 124
            this.ToTargetMemberDefinedData(origin, target);
            //// MCOLA DE 127
            this.ToTargetPosUserData(origin, target);
        }

        //// MCOLA 0100
        protected void ToTargetPreAuthRequest(MasterCard origin, MCOLA target)
        {
            //// MCOLA DE 3
            this.ToTargetProcessingCode(origin, target);
            //// MCOLA DE 6
            this.ToTargetCardholderBillingAmount(origin, target);
            //// MCOLA DE 10
            this.ToTargetCardholderBillingConversionRate(origin, target);
            //// MCOLA DE 22
            this.ToTargetPointOfServiceEntryMode(origin, target);
            //// MCOLA DE 23
            this.ToTargetCardSequenceNumber(origin, target);
            //// MCOLA DE 25
            this.ToTargetPointOfServiceConditionMode(origin, target);
            //// MCOLA DE 33
            this.ToTargetForwardingInstitutionIdCode(origin, target);
            //// MCOLA DE 43
            this.ToTargetCardAcceptorNameLocation(origin, target);
            //// MCOLA DE 48
            this.ToTargetAdditionalData(origin, target);
            //// MCOLA DE 51
            this.ToTargetCardholderBillingCurrencyCode(origin, target);

            var tokenIdList = new List<string>();

            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.Token04.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.Token20.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenB0.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenB2.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenB3.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenB4.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenC0.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenC4.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenCE.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenCH.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenCZ.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenF1.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenU7.Id);

            //// MCOLA DE 63
            this.ToTargetPosAdditionalData(origin, target, tokenIdList);
            //// MCOLA DE 120
            this.ToTargetRecordData(origin, target);
            //// MCOLA DE 124
            this.ToTargetMemberDefinedData(origin, target);
            //// MCOLA DE 127
            this.ToTargetPosUserData(origin, target);
        }

        //// MCOLA 0220
        protected void ToTargetPreAuthCompleteRequest(MasterCard origin, MCOLA target)
        {
            //// MCOLA DE 3
            this.ToTargetProcessingCode(origin, target);
            //// MCOLA DE 6
            this.ToTargetCardholderBillingAmount(origin, target);
            //// MCOLA DE 10
            this.ToTargetCardholderBillingConversionRate(origin, target);
            //// MCOLA DE 16
            this.ToTargetConversionDate(origin, target);
            //// MCOLA DE 22
            this.ToTargetPointOfServiceEntryMode(origin, target);
            //// MCOLA DE 23
            this.ToTargetCardSequenceNumber(origin, target);
            //// MCOLA DE 25
            this.ToTargetPointOfServiceConditionMode(origin, target);
            //// MCOLA DE 33
            this.ToTargetForwardingInstitutionIdCode(origin, target);
            //// MCOLA DE 43
            this.ToTargetCardAcceptorNameLocation(origin, target);
            //// MCOLA DE 48
            this.ToTargetAdditionalData(origin, target);
            //// MCOLA DE 51
            this.ToTargetCardholderBillingCurrencyCode(origin, target);

            var tokenIdList = new List<string>();

            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.Token20.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenB0.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenB2.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenB3.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenB4.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenC0.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenC4.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenCE.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenCH.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenCZ.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenF1.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenU7.Id);

            //// MCOLA DE 63
            this.ToTargetPosAdditionalData(origin, target, tokenIdList);
            //// MCOLA DE 120
            this.ToTargetRecordData(origin, target);
            //// MCOLA DE 124
            this.ToTargetMemberDefinedData(origin, target);
            //// MCOLA DE 127
            this.ToTargetPosUserData(origin, target);
        }

        //// MCOLA 0220
        protected void ToTargetSaleAdviceRequest(MasterCard origin, MCOLA target)
        {
            //// MCOLA DE 3
            this.ToTargetProcessingCode(origin, target);
            //// MCOLA DE 6
            this.ToTargetCardholderBillingAmount(origin, target);
            //// MCOLA DE 10
            this.ToTargetCardholderBillingConversionRate(origin, target);
            //// MCOLA DE 22
            this.ToTargetPointOfServiceEntryMode(origin, target);
            //// MCOLA DE 23
            this.ToTargetCardSequenceNumber(origin, target);
            //// MCOLA DE 28
            this.ToTargetTransactionFeeAmount(origin, target);
            //// MCOLA DE 33
            this.ToTargetForwardingInstitutionIdCode(origin, target);
            //// MCOLA DE 25
            this.ToTargetPointOfServiceConditionMode(origin, target);
            //// MCOLA DE 43
            this.ToTargetCardAcceptorNameLocation(origin, target);
            //// MCOLA DE 48
            this.ToTargetAdditionalData(origin, target);
            //// MCOLA DE 51
            this.ToTargetCardholderBillingCurrencyCode(origin, target);

            var tokenIdList = new List<string>();

            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.Token20.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenB0.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenB2.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenB3.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenB4.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenC0.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenC4.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenCE.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenCH.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenCZ.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenF1.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenU7.Id);

            //// MCOLA DE 63
            this.ToTargetPosAdditionalData(origin, target, tokenIdList);
            //// MCOLA DE 120
            this.ToTargetRecordData(origin, target);
            //// MCOLA DE 124
            this.ToTargetMemberDefinedData(origin, target);
            //// MCOLA DE 127
            this.ToTargetPosUserData(origin, target);
        }

        //// MCOLA 0190
        protected void ToTargetAuthorizationResponseNegAck(MasterCard origin, MCOLA target)
        {
        }

        //// MCOLA 0310
        protected void ToTargetFileUpdateResponse(MasterCard origin, MCOLA target)
        {
            var tokenIdList = new List<string>();
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenU7.Id);

            //// MCOLA DE 63
            this.ToTargetPosAdditionalData(origin, target, tokenIdList);
            //// MCOLA DE 91
            this.ToTargetIssuerFileUpdateCode(origin, target);
            //// MCOLA DE 101
            this.ToTargetFileName(origin, target);
            //// MCOLA DE 120
            this.ToTargetRecordData(origin, target);
        }

        //// MCOLA 0400
        protected void ToTargetReversalRequest(MasterCard origin, MCOLA target)
        {
            //// MCOLA DE 3
            this.ToTargetProcessingCode(origin, target);
            //// MCOLA DE 6
            this.ToTargetCardholderBillingAmount(origin, target);
            //// MCOLA DE 10
            this.ToTargetCardholderBillingConversionRate(origin, target);
            //// MCOLA DE 22
            this.ToTargetPointOfServiceEntryMode(origin, target);
            //// MCOLA DE 23
            this.ToTargetCardSequenceNumber(origin, target);
            //// MCOLA DE 25
            this.ToTargetPointOfServiceConditionMode(origin, target);
            //// MCOLA DE 28
            this.ToTargetTransactionFeeAmount(origin, target);
            //// MCOLA DE 33
            this.ToTargetForwardingInstitutionIdCode(origin, target);
            //// MCOLA DE 43
            this.ToTargetCardAcceptorNameLocation(origin, target);
            //// MCOLA DE 48
            this.ToTargetAdditionalData(origin, target);
            //// MCOLA DE 51
            this.ToTargetCardholderBillingCurrencyCode(origin, target);

            var tokenIdList = new List<string>();

            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.Token20.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenB0.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenB2.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenB3.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenB4.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenC0.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenC4.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenCE.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenCH.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenCZ.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenF1.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenU7.Id);

            //// MCOLA DE 63
            this.ToTargetPosAdditionalData(origin, target, tokenIdList);
            //// MCOLA DE 90
            this.ToTargetMessageOriginalDataElements(origin, target);
            //// MCOLA DE 120
            this.ToTargetRecordData(origin, target);
            //// MCOLA DE 124
            this.ToTargetMemberDefinedData(origin, target);
            //// MCOLA DE 127
            this.ToTargetPosUserData(origin, target);
        }

        //// MCOLA 0420
        protected void ToTargetReversalAdviceRequest(MasterCard origin, MCOLA target)
        {
            //// MCOLA DE 3
            this.ToTargetProcessingCode(origin, target);
            //// MCOLA DE 6
            this.ToTargetCardholderBillingAmount(origin, target);
            //// MCOLA DE 10
            this.ToTargetCardholderBillingConversionRate(origin, target);
            //// MCOLA DE 22
            this.ToTargetPointOfServiceEntryMode(origin, target);
            //// MCOLA DE 23
            this.ToTargetCardSequenceNumber(origin, target);
            //// MCOLA DE 25
            this.ToTargetPointOfServiceConditionMode(origin, target);
            //// MCOLA DE 28
            this.ToTargetTransactionFeeAmount(origin, target);
            //// MCOLA DE 33
            this.ToTargetForwardingInstitutionIdCode(origin, target);
            //// MCOLA DE 43
            this.ToTargetCardAcceptorNameLocation(origin, target);
            //// MCOLA DE 48
            this.ToTargetAdditionalData(origin, target);
            //// MCOLA DE 51
            this.ToTargetCardholderBillingCurrencyCode(origin, target);

            var tokenIdList = new List<string>();

            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.Token20.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenB0.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenB2.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenB3.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenB4.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenC0.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenCE.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenCH.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenCZ.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenF1.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenU7.Id);

            //// MCOLA DE 63
            this.ToTargetPosAdditionalData(origin, target, tokenIdList);
            //// MCOLA DE 90
            this.ToTargetMessageOriginalDataElements(origin, target);
            //// MCOLA DE 120
            this.ToTargetRecordData(origin, target);
            //// MCOLA DE 124
            this.ToTargetMemberDefinedData(origin, target);
            //// MCOLA DE 127
            this.ToTargetPosUserData(origin, target);
        }

        //// MCOLA 0600
        protected void ToTargetAdministrativeRequest(MasterCard origin, MCOLA target)
        {
            //// MCOLA DE 43
            this.ToTargetCardAcceptorNameLocation(origin, target);

            var tokenIdList = new List<string>();
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenB0.Id);

            //// MCOLA DE 63
            this.ToTargetPosAdditionalData(origin, target, tokenIdList);
        }

        //// MCOLA 0620
        protected void ToTargetAdministrativeAdviceRequest(MasterCard origin, MCOLA target)
        {
            //// MCOLA DE 43
            this.ToTargetCardAcceptorNameLocation(origin, target);

            var tokenIdList = new List<string>();
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenB0.Id);

            //// MCOLA DE 63
            this.ToTargetPosAdditionalData(origin, target, tokenIdList);
            //// MCOLA DE 120
            this.ToTargetRecordData(origin, target);

        }

        //// MCOLA 0800
        protected void ToTargetGroupSignOnResponse(MasterCard origin, MCOLA target)
        {
            //// MCOLA DE 70
            this.ToTargetNetworkManagementInformationCode(origin, target);
            //// MCOLA DE 120
            this.ToTargetRecordData(origin, target);
            //// MCOLA DE 123
            this.ToTargetCryptographicServiceMessage(origin, target);
        }

        //// MCOLA 0800
        protected void ToTargetGroupSignOffResponse(MasterCard origin, MCOLA target)
        {
            //// MCOLA DE 70
            this.ToTargetNetworkManagementInformationCode(origin, target);
            //// MCOLA DE 120
            this.ToTargetRecordData(origin, target);
            //// MCOLA DE 123
            this.ToTargetCryptographicServiceMessage(origin, target);
        }

        //// MCOLA 0800
        protected void ToTargetChangeKeyRequest(MasterCard origin, MCOLA target)
        {
            //// MCOLA DE 53
            this.ToTargetSecurityRelatedControlInformation(origin, target);
            //// MCOLA DE 70
            this.ToTargetNetworkManagementInformationCode(origin, target);
            //// MCOLA DE 120
            this.ToTargetRecordData(origin, target);
            //// MCOLA DE 123
            this.ToTargetCryptographicServiceMessage(origin, target);
        }

        //// MCOLA 0800
        protected void ToTargetChangeKeyAdviceRequest(MasterCard origin, MCOLA target)
        {
            //// MCOLA DE 53
            this.ToTargetSecurityRelatedControlInformation(origin, target);
            //// MCOLA DE 70
            this.ToTargetNetworkManagementInformationCode(origin, target);
            //// MCOLA DE 120
            this.ToTargetRecordData(origin, target);
            //// MCOLA DE 123
            this.ToTargetCryptographicServiceMessage(origin, target);
        }

        //// MCOLA 0110
        protected void ToTargetAlternativeNetworkRoutingResponse(MasterCard origin, MCOLA target)
        {
            //// MCOLA DE 3
            this.ToTargetProcessingCode(origin, target);
            //// MCOLA DE 22
            this.ToTargetPointOfServiceEntryMode(origin, target);
            //// MCOLA DE 48
            this.ToTargetAdditionalData(origin, target);

            var tokenIdList = new List<string>();

            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenB0.Id);
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenU7.Id);

            //// MCOLA DE 63
            this.ToTargetPosAdditionalData(origin, target, tokenIdList);
            //// MCOLA DE 120
            this.ToTargetRecordData(origin, target);
            //// MCOLA DE 127
            this.ToTargetPosUserData(origin, target);
        }

        //// MCOLA 0130
        protected void ToTargetTransactionHistoryAdviceResponse(MasterCard origin, MCOLA target)
        {
            var tokenIdList = new List<string>();
            tokenIdList.Add(MCOLATransactionConst.PosAdditionalData.TokenU7.Id);

            //// MCOLA DE 63
            this.ToTargetPosAdditionalData(origin, target, tokenIdList);
            //// MCOLA DE 127
            this.ToTargetPosUserData(origin, target);
        }

        //// MCOLA 0210
        protected void FromTargetAuthorizationResponse(MCOLA target, MasterCard origin)
        {
            //// MCOLA DE 48
            this.FromTargetAdditionalData(target, origin);
            //// MCOLA DE 63
            this.FromTargetPosAdditionalData(target, origin);
            //// MasterCard DE 48 SE 33
            this.ToOriginDE48SE33(origin, target);
            //// MCOLA DE 124
            this.FromTargetMemberDefinedData(target, origin);

            origin.Purge(FIELD.F_127);
            origin.Core().PurgeOthersValByKey("F_127");
            //origin.Core().SetOthersValByKey("F_127", null); 

        }

        //// MCOLA 0230
        protected void FromTargetAuthorizationAdviceResponse(MCOLA target, MasterCard origin)
        {
            //// MasterCard DE 48 SE 33
            this.ToOriginDE48SE33(origin, target);

            origin.Purge(FIELD.F_127);
            origin.Core().PurgeOthersValByKey("F_127");
        }

        //// MCOLA 0300
        protected void FromTargetIssuerFileUpdateRequest(MCOLA target, MasterCard origin)
        {
            //// MCOLA DE 33
            this.FromTargetForwardingInstitutionIdCode(target, origin);
            //// MCOLA DE 91
            this.FromTargetIssuerFileUpdateCode(target, origin);
            //// MCOLA DE 96
            this.FromTargetMessageSecurityCode(target, origin);
            //// MCOLA DE 120
            this.FromTargetRecordData(target, origin);
        }

        //// MCOLA 0410
        protected void FromTargetReversalResponse(MCOLA target, MasterCard origin)
        {
            //// MasterCard DE 48 SE 33
            this.ToOriginDE48SE33(origin, target);

            origin.Purge(FIELD.F_127);
            origin.Core().PurgeOthersValByKey("F_127");
        }

        //// MCOLA 0430
        protected void FromTargetReversalAdviceResponse(MCOLA target, MasterCard origin)
        {
            origin.Purge(FIELD.F_127);
            origin.Core().PurgeOthersValByKey("F_127");
        }

        //// MCOLA 0610
        protected void FromTargetAdministrativeResponse(MCOLA target, MasterCard origin)
        {
            origin.Purge(FIELD.F_127);
            origin.Core().PurgeOthersValByKey("F_127");
        }

        //// MCOLA 0630
        protected void FromTargetAdministrativeAdviceResponse(MCOLA target, MasterCard origin)
        {
            origin.Purge(FIELD.F_127);
            origin.Core().PurgeOthersValByKey("F_127");
        }

        //// MCOLA 0800
        protected void FromTargetGroupSignOnRequest(MCOLA target, MasterCard origin)
        {
            //// MCOLA DE 33
            this.FromTargetForwardingInstitutionIdCode(target, origin);
            //// MCOLA DE 70
            this.FromTargetNetworkManagementInformationCode(target, origin);
            //// MCOLA DE 94
            this.FromTargetServiceIndicator(target, origin);
            //// MCOLA DE 96
            this.FromTargetMessageSecurityCode(target, origin);
        }

        //// MCOLA 0800
        protected void FromTargetGroupSignOffRequest(MCOLA target, MasterCard origin)
        {
            //// MCOLA DE 33
            this.FromTargetForwardingInstitutionIdCode(target, origin);
            //// MCOLA DE 70
            this.FromTargetNetworkManagementInformationCode(target, origin);
            //// MCOLA DE 94
            this.FromTargetServiceIndicator(target, origin);
            //// MCOLA DE 96
            this.FromTargetMessageSecurityCode(target, origin);
        }

        //// MCOLA 0810
        protected void FromTargetPekExchangeResponse(MCOLA target, MasterCard origin)
        {
        }

        //// MCOLA 0100
        protected void FromAlternativeNetworkRoutingRequest(MCOLA target, MasterCard origin)
        {
            //// MCOLA DE 11
            this.FromTargetSystemTraceAuditNumber(target, origin);
            //// MCOLA DE 22
            this.FromTargetPointOfServiceEntryMode(target, origin);
            //// MCOLA DE 23
            this.FromTargetCardSequenceNumber(target, origin);
            //// MCOLA DE 48
            this.FromTargetAdditionalData(target, origin);
            //// MCOLA DE 63
            this.FromTargetPosAdditionalData(target, origin);
            //// MCOLA DE 70
            this.FromTargetNetworkManagementInformationCode(target, origin);
            //// MCOLA DE 127
            this.FromTargetSecurityLvl(target, origin);

            ////// for ANR if no UCAF, do not fulfill DE 48 
            //if (!(origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
            //    MasterCardTransactionConst.AdditionalDataPrivateUse.ElectronicCommerceIndicatorsTagId)))
            //{
            //    origin.ExtendedCore().AdditionalDataPrivateUse.TransactionCategoryCode = null;
            //}

        }

        //// MCOLA 0120
        protected void FromTransactionHistoryAdviceRequest(MCOLA target, MasterCard origin)
        {
            //// MCOLA DE 11
            this.FromTargetSystemTraceAuditNumber(target, origin);
            //// MCOLA DE 22
            this.FromTargetPointOfServiceEntryMode(target, origin);
            //// MCOLA DE 43
            this.FromTargetCardAcceptorNameLocation(target, origin);
            //// MCOLA DE 48
            this.FromTargetAdditionalData(target, origin);
            //// MCOLA DE 63
            this.FromTargetPosAdditionalData(target, origin);
            //// MCOLA DE 120
            this.FromTargetRecordData(target, origin);
        }

        //// MCOLA DE 3
        protected void ToTargetProcessingCode(MasterCard origin, MCOLA target)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(this.ToTargetProcessingCodeCardholderTransactionTypeCode(origin));
            stringBuilder.Append(this.ToTargetProcessingCodeCardholderFromAccountTypeCode(origin));
            stringBuilder.Append(this.ToTargetProcessingCodeCardholderToAccountTypeCode(origin));

            target.Core().ProcessingCode = stringBuilder.ToString();
        }

        protected string ToTargetProcessingCodeCardholderTransactionTypeCode(MasterCard origin)
        {
            if (origin.Mti().Equals(MasterCardTransactionConst.MTI.AuthorizationRequest) ||
                origin.Mti().Equals(MasterCardTransactionConst.MTI.AuthorizationAdviceRequest) ||
                origin.Mti().Equals(MasterCardTransactionConst.MTI.ReversalRequest) ||
                origin.Mti().Equals(MasterCardTransactionConst.MTI.ReversalAdviceRequest))
            {
                if (origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                    MasterCardTransactionConst.AdditionalDataPrivateUse.MobileProgramIndicatorsTagId))
                {
                    return MCOLATransactionConst.ProcessingCode.CardholderTransactionTypeCode.MailOrder;
                }


                if (!string.IsNullOrEmpty(origin.ExtendedCore().PointOfServiceData.PosTransactionStatus))
                {
                    switch (origin.ExtendedCore().PointOfServiceData.PosTransactionStatus)
                    {
                        case MasterCardTransactionConst.PointOfServiceData.PosTransactionStatus.AtcUpdate:
                        case MasterCardTransactionConst.PointOfServiceData.PosTransactionStatus.AccountStatusInquiryService:
                            return MCOLATransactionConst.ProcessingCode.CardholderTransactionTypeCode.AccountVerification;
                    }
                }

                switch (origin.ExtendedCore().ProcessingCode.CardholderTransactionTypeCode)
                {
                    case MasterCardTransactionConst.ProcessingCode.CardholderTransactionTypeCode.Purchase:
                        return MCOLATransactionConst.ProcessingCode.CardholderTransactionTypeCode.Purchase;
                    case MasterCardTransactionConst.ProcessingCode.CardholderTransactionTypeCode.Withdrawal:
                        return MCOLATransactionConst.ProcessingCode.CardholderTransactionTypeCode.CashAdvance;
                    case MasterCardTransactionConst.ProcessingCode.CardholderTransactionTypeCode.DebitAdjustment:
                        return MCOLATransactionConst.ProcessingCode.CardholderTransactionTypeCode.PurchaseAdjustment;
                    case MasterCardTransactionConst.ProcessingCode.CardholderTransactionTypeCode.PurchaseReturnRefund:
                        if (!string.IsNullOrEmpty(origin.ExtendedCore().AdditionalDataPrivateUse.TransactionCategoryCode)
                            &&
                            origin.ExtendedCore().AdditionalDataPrivateUse.TransactionCategoryCode.Equals(
                            MasterCardTransactionConst.AdditionalDataPrivateUse.TransactionCategoryCode.CashDisbursement))
                        {
                            return MCOLATransactionConst.ProcessingCode.CardholderTransactionTypeCode.CashAdvanceAdjustment;
                        }
                        else
                        {
                            return MCOLATransactionConst.ProcessingCode.CardholderTransactionTypeCode.Refund;
                        }
                    case MasterCardTransactionConst.ProcessingCode.CardholderTransactionTypeCode.CreditAdjustment:
                        return MCOLATransactionConst.ProcessingCode.CardholderTransactionTypeCode.RefundAdjustment;
                    case MasterCardTransactionConst.ProcessingCode.CardholderTransactionTypeCode.BalanceInquiry:
                        return MCOLATransactionConst.ProcessingCode.CardholderTransactionTypeCode.BalanceInquiry;
                    case MasterCardTransactionConst.ProcessingCode.CardholderTransactionTypeCode.PinChange:
                        return MCOLATransactionConst.ProcessingCode.CardholderTransactionTypeCode.PinChange;
                    case MasterCardTransactionConst.ProcessingCode.CardholderTransactionTypeCode.CashDisbursement:
                        return MCOLATransactionConst.ProcessingCode.CardholderTransactionTypeCode.CashAdvance;
                    default:
                        return MCOLATransactionConst.ProcessingCode.CardholderTransactionTypeCode.Purchase;
                }
            }

            return origin.ExtendedCore().ProcessingCode.CardholderTransactionTypeCode;
        }

        protected string ToTargetProcessingCodeCardholderFromAccountTypeCode(MasterCard origin)
        {
            if (origin.Mti().Equals(MasterCardTransactionConst.MTI.AuthorizationRequest) ||
                origin.Mti().Equals(MasterCardTransactionConst.MTI.AuthorizationAdviceRequest) ||
                origin.Mti().Equals(MasterCardTransactionConst.MTI.ReversalRequest) ||
                origin.Mti().Equals(MasterCardTransactionConst.MTI.ReversalAdviceRequest))
            {
                switch (origin.ExtendedCore().ProcessingCode.CardholderTransactionTypeCode)
                {

                    case MasterCardTransactionConst.ProcessingCode.CardholderTransactionTypeCode.Withdrawal:
                        if (!string.IsNullOrEmpty(origin.ExtendedCore().AdditionalDataPrivateUse.TransactionCategoryCode)
                            &&
                            origin.ExtendedCore().AdditionalDataPrivateUse.TransactionCategoryCode.Equals(
                            MasterCardTransactionConst.AdditionalDataPrivateUse.TransactionCategoryCode.CashDisbursement))
                        {
                            return MCOLATransactionConst.ProcessingCode.CardholderFromAccountTypeCode.CreditAccount;
                        }
                        else
                        {
                            return MCOLATransactionConst.ProcessingCode.CardholderFromAccountTypeCode.DefaultAccount;
                        }
                    case MasterCardTransactionConst.ProcessingCode.CardholderTransactionTypeCode.BalanceInquiry:
                        return MCOLATransactionConst.ProcessingCode.CardholderFromAccountTypeCode.CreditAccount;
                    default:
                        return MCOLATransactionConst.ProcessingCode.CardholderFromAccountTypeCode.DefaultAccount;
                }
            }

            return origin.ExtendedCore().ProcessingCode.CardholderFromAccountTypeCode;
        }

        protected string ToTargetProcessingCodeCardholderToAccountTypeCode(MasterCard origin)
        {
            if (origin.Mti().Equals(MasterCardTransactionConst.MTI.AuthorizationRequest) ||
                origin.Mti().Equals(MasterCardTransactionConst.MTI.AuthorizationAdviceRequest) ||
                origin.Mti().Equals(MasterCardTransactionConst.MTI.ReversalRequest) ||
                origin.Mti().Equals(MasterCardTransactionConst.MTI.ReversalAdviceRequest))
            {
                if (!string.IsNullOrEmpty(origin.ExtendedCore().ProcessingCode.CardholderFromAccountTypeCode)
                    &&
                    origin.ExtendedCore().ProcessingCode.CardholderFromAccountTypeCode.Equals
                    (MasterCardTransactionConst.ProcessingCode.CardholderFromAccountTypeCode.CreditCardAccount))
                {
                    return MCOLATransactionConst.ProcessingCode.CardholderToAccountTypeCode.DefaultAccount;
                }

                return MCOLATransactionConst.ProcessingCode.CardholderToAccountTypeCode.CreditAccount;
            }

            return origin.ExtendedCore().ProcessingCode.CardholderToAccountTypeCode;
        }

        //// MCOLA DE 6
        protected void ToTargetCardholderBillingAmount(MasterCard origin, MCOLA target)
        {
            if (!string.IsNullOrEmpty(origin.ExtendedCore().CardholderBillingAmount))
            {
                target.F6Decode(origin.ExtendedCore().CardholderBillingAmount);
            }
        }

        protected void FromTargetCardholderBillingAmount(MCOLA target, MasterCard origin)
        {
            if (!string.IsNullOrEmpty(target.F6Encode()))
            {
                origin.ExtendedCore().CardholderBillingAmount = target.F6Encode();
            }
        }

        //// MCOLA DE 10
        protected void ToTargetCardholderBillingConversionRate(MasterCard origin, MCOLA target)
        {
            if (!string.IsNullOrEmpty(origin.ExtendedCore().CardholderBillingConversionRate))
            {
                target.F10Decode(origin.ExtendedCore().CardholderBillingConversionRate);
            }
        }

        protected void FromTargetCardholderBillingConversionRate(MCOLA target, MasterCard origin)
        {
            if (!string.IsNullOrEmpty(target.F10Encode()))
            {
                origin.ExtendedCore().CardholderBillingConversionRate = target.F10Encode();
            }
        }

        //// MCOLA DE 11
        protected void FromTargetSystemTraceAuditNumber(MCOLA target, MasterCard origin)
        {
            if (origin is TM.MasterCard.Transaction.AlternativeNetworkRoutingRequest ||
                origin is TM.MasterCard.Transaction.TransactionHistoryAdviceRequest)
            {
                origin.F127Decode(target.Core().SystemTraceAuditNumber);
                origin.Core().SystemTraceAuditNumber = null;
            }
        }

        //// MCOLA DE 16
        protected void ToTargetConversionDate(MasterCard origin, MCOLA target)
        {
            if (string.IsNullOrEmpty(origin.ExtendedCore().ConversionDate))
            {
                target.F16Decode(origin.ExtendedCore().ConversionDate);
            }
        }

        protected void FromTargetConversionDate(MCOLA target, MasterCard origin)
        {
            if (string.IsNullOrEmpty(target.F16Encode()))
            {
                origin.ExtendedCore().ConversionDate = target.F16Encode();
            }
        }

        //// MCOLA DE 22
        protected void ToTargetPointOfServiceEntryMode(MasterCard origin, MCOLA target)
        {
            if (!string.IsNullOrEmpty(origin.F22PointOfServiceEntryModeEncode()))
            {
                target.Core().PointOfServiceEntryMode = origin.F22PointOfServiceEntryModeEncode();
            }
        }

        protected void FromTargetPointOfServiceEntryMode(MCOLA target, MasterCard origin)
        {
            if (!string.IsNullOrEmpty(target.Core().PointOfServiceEntryMode))
            {
                origin.F22PointOfServiceEntryModeDecode(target.Core().PointOfServiceEntryMode);
            }
        }

        //// MCOLA DE 23
        protected void ToTargetCardSequenceNumber(MasterCard origin, MCOLA target)
        {
            if (!string.IsNullOrEmpty(origin.ExtendedCore().CardSequenceNumber))
            {
                target.F23Decode(origin.ExtendedCore().CardSequenceNumber);
            }
        }

        protected void FromTargetCardSequenceNumber(MCOLA target, MasterCard origin)
        {
            if (!string.IsNullOrEmpty(target.F23Encode()))
            {
                origin.ExtendedCore().CardSequenceNumber = target.F23Encode();
            }
        }

        //// MCOLA DE 25
        protected void ToTargetPointOfServiceConditionMode(MasterCard origin, MCOLA target)
        {
            switch (origin.ExtendedCore().PointOfServiceData.PosTransactionStatus)
            {
                case MasterCardTransactionConst.PointOfServiceData.PosTransactionStatus.PreAuthorizedRequest:
                    target.Core().PointOfServiceConditionMode = MCOLATransactionConst.PointOfServiceConditionMode.PreAuthPreAuthCmpl;
                    return;
                case MasterCardTransactionConst.PointOfServiceData.PosTransactionStatus.AccountStatusInquiryService:
                    target.Core().PointOfServiceConditionMode = MCOLATransactionConst.PointOfServiceConditionMode.VisaAccountVerification;
                    return;
                default:
                    break;
            }

            if (origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                    MasterCardTransactionConst.AdditionalDataPrivateUse.UniversalCardholderAuthenticationFieldTagId))
            {
                target.Core().PointOfServiceConditionMode = MCOLATransactionConst.PointOfServiceConditionMode.MasterCardEC;
                return;
            }
            else
            {
                if (origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                    MasterCardTransactionConst.AdditionalDataPrivateUse.MobileProgramIndicatorsTagId))
                {
                    {
                        var MobileProgramIndicatorsTag = (MasterCardDE48SE48)origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                            MasterCardTransactionConst.AdditionalDataPrivateUse.MobileProgramIndicatorsTagId];

                        switch (MobileProgramIndicatorsTag.MasterCardMobileRemotePaymentTransactionType.Value)
                        {
                            //case MasterCardTransactionConst.AdditionalDataPrivateUse.MobileProgramIndicators.RemotePurchaseEC:
                            //    target.Core().PointOfServiceConditionMode = MCOLATransactionConst.PointOfServiceConditionMode.MasterCardEC;
                            //    return;
                            //    break;
                            case MasterCardTransactionConst.AdditionalDataPrivateUse.MobileProgramIndicators.RemotePurchaseMOTO:
                                target.Core().PointOfServiceConditionMode = MCOLATransactionConst.PointOfServiceConditionMode.MailOrder;
                                return;
                            default:
                                break;
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(origin.ExtendedCore().PointOfServiceData.PosCardholderPresence)
                &&
                !origin.ExtendedCore().PointOfServiceData.PosCardholderPresence.Equals
                (MasterCardTransactionConst.PointOfServiceData.PosCardholderPresence.Present))
            {
                target.Core().PointOfServiceConditionMode = MCOLATransactionConst.PointOfServiceConditionMode.NotPresent;
                return;
            }

            target.Core().PointOfServiceConditionMode = MCOLATransactionConst.PointOfServiceConditionMode.Present;
        }

        //// MCOLA DE 28
        protected void ToTargetTransactionFeeAmount(MasterCard origin, MCOLA target)
        {
            if (!string.IsNullOrEmpty(origin.F28Encode()))
            {
                target.Core().TransactionFeeAmount = origin.F28Encode();
            }
        }

        protected void FromTargetTransactionFeeAmount(MCOLA target, MasterCard origin)
        {
            if (!string.IsNullOrEmpty(target.Core().TransactionFeeAmount))
            {
                origin.F28Decode(target.Core().TransactionFeeAmount);
            }
        }

        //// MCOLA DE 33
        protected void ToTargetForwardingInstitutionIdCode(MasterCard origin, MCOLA target)
        {
            if (!string.IsNullOrEmpty(origin.ExtendedCore().ForwardingInstitutionIdCode))
            {
                target.F33Decode(origin.ExtendedCore().ForwardingInstitutionIdCode);
            }
        }

        protected void FromTargetForwardingInstitutionIdCode(MCOLA target, MasterCard origin)
        {
            if (!string.IsNullOrEmpty(target.F33Encode()))
            {
                origin.ExtendedCore().ForwardingInstitutionIdCode = target.F33Encode();
            }
        }

        //// MCOLA DE 39
        protected void ToTargetMessageResponseCode(MasterCard origin, MCOLA target)
        {
            if (origin.Mti().Equals(MasterCardTransactionConst.MTI.IssuerFileUpdateResponse))
            {
                switch (origin.Core().ResponseCode)
                {
                    case MasterCardTransactionConst.ResponseCode.IssuerFileUpdate.CompletedSuccessfully:
                        target.Core().ResponseCode = MCOLATransactionConst.ResponseCode.FileUpdate.AcceptAndUpdate;
                        break;
                    case MasterCardTransactionConst.ResponseCode.IssuerFileUpdate.FormatError:
                        target.Core().ResponseCode = MCOLATransactionConst.ResponseCode.FileUpdate.InvalidUpdateCode;
                        break;
                    case MasterCardTransactionConst.ResponseCode.IssuerFileUpdate.UnableLocateRecord:
                        target.Core().ResponseCode = MCOLATransactionConst.ResponseCode.FileUpdate.DeleteNotFound;
                        break;
                    case MasterCardTransactionConst.ResponseCode.IssuerFileUpdate.SystemError:
                        target.Core().ResponseCode = MCOLATransactionConst.ResponseCode.FileUpdate.FileReadError;
                        break;
                    case MasterCardTransactionConst.ResponseCode.IssuerFileUpdate.DuplicateAdd:
                        target.Core().ResponseCode = MCOLATransactionConst.ResponseCode.FileUpdate.FileAddExisted;
                        break;
                    case MasterCardTransactionConst.ResponseCode.IssuerFileUpdate.RecordNotActive:
                        target.Core().ResponseCode = MCOLATransactionConst.ResponseCode.FileUpdate.FileChangeNotExisted;
                        break;
                    case MasterCardTransactionConst.ResponseCode.IssuerFileUpdate.RecordDeleted:
                        target.Core().ResponseCode = MCOLATransactionConst.ResponseCode.FileUpdate.FileChangeNotExisted;
                        break;
                    case MasterCardTransactionConst.ResponseCode.IssuerFileUpdate.FieldEditError:
                        target.Core().ResponseCode = MCOLATransactionConst.ResponseCode.FileUpdate.ConvertFormatError;
                        break;
                    default:
                        break;
                }
            }
        }

        //// MCOLA DE 43
        protected void ToTargetCardAcceptorNameLocation(MasterCard origin, MCOLA target)
        {
            target.Core().BankName = origin.ExtendedCore().CardAcceptorNameLocation.CardAcceptorName;
            target.Core().City = origin.ExtendedCore().CardAcceptorNameLocation.CardAcceptorCity;
            target.Core().State = origin.ExtendedCore().CardAcceptorNameLocation.CardAcceptorStateCountryCode;
            target.Core().Country = string.Empty.PadLeft(2, ' ');
        }

        protected void FromTargetCardAcceptorNameLocation(MCOLA target, MasterCard origin)
        {
            origin.ExtendedCore().CardAcceptorNameLocation.CardAcceptorName = target.Core().BankName;
            origin.ExtendedCore().CardAcceptorNameLocation.CardAcceptorCity = target.Core().City;
            origin.ExtendedCore().CardAcceptorNameLocation.CardAcceptorStateCountryCode = target.Core().State;
        }

        //// MCOLA DE 48
        protected void ToTargetAdditionalData(MasterCard origin, MCOLA target)
        {
            var panSeqNum = string.Empty.PadLeft(3, '0');
            var fiid = "BK00";
            var accountType = "31";
            var errorCode = string.Empty.PadLeft(4, ' ');

            var visaMessageReasonCode = string.Empty.PadLeft(4, '0');
            var responseSourceOrReasonCode = string.Empty.PadLeft(1, ' ');
            var stipOrSwitchReasonCode = string.Empty.PadLeft(4, '0');
            var transactionCategoryCode = origin.ExtendedCore().AdditionalDataPrivateUse.TransactionCategoryCode;
            var accountNumberIndicator = string.Empty.PadLeft(1, ' ');
            var chipCvrTvrBitErrorResults = string.Empty.PadLeft(50, ' ');
            var filler = string.Empty.PadLeft(2, '0');

            if (origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags.ContainsKey("5F34"))
            {
                panSeqNum = HexStringConverter.FromByteArray(
                    origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags["5F34"].Value);
            }
            else if (!string.IsNullOrEmpty(origin.ExtendedCore().CardSequenceNumber))
            {
                panSeqNum = origin.ExtendedCore().CardSequenceNumber;
            }

            if (origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                MasterCardTransactionConst.AdditionalDataPrivateUse.PanMappingFileInformationTagId))
            {
                accountNumberIndicator = ((MasterCardDE48SE33)origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                    MasterCardTransactionConst.AdditionalDataPrivateUse.PanMappingFileInformationTagId]).AccountNumberIndicator.Value;
            }

            if (origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                MasterCardTransactionConst.AdditionalDataPrivateUse.ChipCvrTvrBitErrorResultTagId))
            {
                chipCvrTvrBitErrorResults = ((MasterCardDE48SE79)origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                    MasterCardTransactionConst.AdditionalDataPrivateUse.ChipCvrTvrBitErrorResultTagId]).ValueEncode().PadRight(50, ' ');
            }

            var stringBuilder = new StringBuilder();

            stringBuilder.Append(panSeqNum);
            stringBuilder.Append(fiid);
            stringBuilder.Append(accountType);
            stringBuilder.Append(errorCode);
            stringBuilder.Append(visaMessageReasonCode);
            stringBuilder.Append(responseSourceOrReasonCode);
            stringBuilder.Append(stipOrSwitchReasonCode);
            stringBuilder.Append(transactionCategoryCode);
            stringBuilder.Append(accountNumberIndicator);
            stringBuilder.Append(chipCvrTvrBitErrorResults);
            stringBuilder.Append(filler);

            target.F48Decode(stringBuilder.ToString());
        }

        protected void FromTargetAdditionalData(MCOLA target, MasterCard origin)
        {
            if (!string.IsNullOrEmpty(target.F48Encode())
                &&
                target.F48Encode().Length >= 23)
            {
                var transactionCategoryCode = target.F48Encode().Substring(22, 1);
                origin.ExtendedCore().AdditionalDataPrivateUse.TransactionCategoryCode = transactionCategoryCode;
            }
            else
            {
                origin.ExtendedCore().AdditionalDataPrivateUse.TransactionCategoryCode = MasterCardTransactionConst.AdditionalDataPrivateUse.TransactionCategoryCode.Space;
            }
        }

        //// MCOLA DE 51
        protected void ToTargetCardholderBillingCurrencyCode(MasterCard origin, MCOLA target)
        {
            if (!string.IsNullOrEmpty(origin.ExtendedCore().CardholderBillingCurrencyCode))
            {
                target.F51Decode(origin.ExtendedCore().CardholderBillingCurrencyCode);
            }
        }

        protected void FromTargetCardholderBillingCurrencyCode(MCOLA target, MasterCard origin)
        {
            if (!string.IsNullOrEmpty(target.F51Encode()))
            {
                origin.ExtendedCore().CardholderBillingCurrencyCode = target.F51Encode();
            }
        }

        //// MCOLA DE 53
        protected void ToTargetSecurityRelatedControlInformation(MasterCard origin, MCOLA target)
        {
            var keyType = MCOLATransactionConst.SecurityRelatedControlInformation.KeyType.Custom;
            var keyDirection = MCOLATransactionConst.SecurityRelatedControlInformation.KeyDirection.OutboundKeyOnly;
            var filler = string.Empty.PadRight(12, '0');

            target.F53Decode(keyType + keyDirection + filler);
        }

        //// MCOLA DE 63
        protected void ToTargetPosAdditionalData(MasterCard origin, MCOLA target, List<string> tokenIdList)
        {
            var tokenGroup = new TokenGroup();
            Token token = null;

            foreach (var tokenId in tokenIdList)
            {
                switch (tokenId)
                {
                    case NcccTransactionConst.PosAdditionalData.Token04.Id:
                        token = this.ToTargetPosAdditionalDataToken04(origin);
                        break;
                    case NcccTransactionConst.PosAdditionalData.Token20.Id:
                        token = this.ToTargetPosAdditionalDataToken20(origin);
                        break;
                    case NcccTransactionConst.PosAdditionalData.TokenB0.Id:
                        token = this.ToTargetPosAdditionalDataTokenB0(origin);
                        break;
                    case NcccTransactionConst.PosAdditionalData.TokenB2.Id:
                        token = this.ToTargetPosAdditionalDataTokenB2(origin);
                        break;
                    case NcccTransactionConst.PosAdditionalData.TokenB3.Id:
                        token = this.ToTargetPosAdditionalDataTokenB3(origin);
                        break;
                    case NcccTransactionConst.PosAdditionalData.TokenB4.Id:
                        token = this.ToTargetPosAdditionalDataTokenB4(origin);
                        break;
                    case NcccTransactionConst.PosAdditionalData.TokenC0.Id:
                        token = this.ToTargetPosAdditionalDataTokenC0(origin);
                        break;
                    case NcccTransactionConst.PosAdditionalData.TokenC4.Id:
                        token = this.ToTargetPosAdditionalDataTokenC4(origin);
                        break;
                    case NcccTransactionConst.PosAdditionalData.TokenCE.Id:
                        token = this.ToTargetPosAdditionalDataTokenCE(origin);
                        break;
                    case NcccTransactionConst.PosAdditionalData.TokenCH.Id:
                        token = this.ToTargetPosAdditionalDataTokenCH(origin);
                        break;
                    case NcccTransactionConst.PosAdditionalData.TokenCZ.Id:
                        token = this.ToTargetPosAdditionalDataTokenCZ(origin);
                        break;
                    case NcccTransactionConst.PosAdditionalData.TokenF1.Id:
                        token = this.ToTargetPosAdditionalDataTokenF1(origin);
                        break;
                    case NcccTransactionConst.PosAdditionalData.TokenU7.Id:
                        token = this.ToTargetPosAdditionalDataTokenU7(origin);
                        break;
                }

                if (!ObjectUtils.IsNull(token))
                {
                    tokenGroup.Tokens[token.Id] = token;
                }
            }

            target.F63TokenDecode(tokenGroup.Encode());
        }

        protected TokenB0 ToTargetPosAdditionalDataTokenB0(MasterCard origin)
        {
            var modify = false;
            var tokenB0 = new TokenB0();

            tokenB0.Fiid = "BNET";

            if (!string.IsNullOrEmpty(origin.ExtendedCore().AdviceReasonCode.AdviceReasonCodeSubfield))
            {
                modify = true;
                tokenB0.AdviceRsnCde = origin.ExtendedCore().AdviceReasonCode.AdviceReasonCodeSubfield;
            }

            if (origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                MasterCardTransactionConst.AdditionalDataPrivateUse.OnbehalfServicesTagId))
            {
                modify = true;
                tokenB0.OnBehalf = origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                    MasterCardTransactionConst.AdditionalDataPrivateUse.OnbehalfServicesTagId].ValueEncode().PadRight(30, ' ');
            }

            if (!string.IsNullOrEmpty(origin.ExtendedCore().AdviceReasonCode.AdviceDetailCode))
            {
                modify = true;
                tokenB0.AdvcDetlCde = origin.ExtendedCore().AdviceReasonCode.AdviceDetailCode;
            }

            if (!string.IsNullOrEmpty(origin.ExtendedCore().AuthorizingAgentIdCode))
            {
                modify = true;
                tokenB0.AuthAgentIdCde = origin.ExtendedCore().AuthorizingAgentIdCode;
            }

            if (modify)
            {
                return tokenB0;
            }
            else
            {
                return null;
            }
        }

        protected TokenB2 ToTargetPosAdditionalDataTokenB2(MasterCard origin)
        {
            var modify = false;
            var tokenB2 = new TokenB2();

            var originIccTags = origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags;

            if (origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags.ContainsKey("9F27"))
            {
                modify = true;
                tokenB2.CryptoInfData = origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.GetTagHex("9F27", 2);
            }

            if (origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags.ContainsKey("95"))
            {
                modify = true;
                tokenB2.Tvr = origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.GetTagHex("95", 10);
            }

            if (origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags.ContainsKey("9F26"))
            {
                modify = true;
                tokenB2.Arqc = origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.GetTagHex("9F26", 16);
            }

            if (origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags.ContainsKey("9F02"))
            {
                modify = true;
                tokenB2.AmtAuth = origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.GetTagHex("9F02", 12);
            }

            if (origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags.ContainsKey("9F03"))
            {
                modify = true;
                tokenB2.AmtOther = origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.GetTagHex("9F03", 12);
            }

            if (origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags.ContainsKey("82"))
            {
                modify = true;
                tokenB2.Aip = origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.GetTagHex("82", 4);
            }

            if (origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags.ContainsKey("9F36"))
            {
                modify = true;
                tokenB2.Atc = origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.GetTagHex("9F36", 4);
            }

            if (origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags.ContainsKey("9F1A"))
            {
                var termCntryCde = origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.GetTagHex("9F1A", 3);

                if (termCntryCde.Length >= 3)
                {
                    modify = true;
                    tokenB2.TermCntryCde = termCntryCde.Substring(termCntryCde.Length - 3);
                }
            }

            if (origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags.ContainsKey("5F2A"))
            {
                var termCrncyCde = origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.GetTagHex("5F2A", 3);

                if (termCrncyCde.Length >= 3)
                {
                    modify = true;
                    tokenB2.TermCrncyCde = termCrncyCde.Substring(termCrncyCde.Length - 3);
                }
            }

            if (origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags.ContainsKey("9A"))
            {
                modify = true;
                tokenB2.TranDat = origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.GetTagHex("9A", 6);
            }

            if (origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags.ContainsKey("9C"))
            {
                modify = true;
                tokenB2.TranType = origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.GetTagHex("9C", 2);
            }

            if (origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags.ContainsKey("9F37"))
            {
                modify = true;
                tokenB2.UnpredictNum = origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.GetTagHex("9F37", 8);
            }

            if (origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags.ContainsKey("9F10"))
            {
                modify = true;
                tokenB2.IssApplDataLgth = origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags["9F10"].Value.Length.ToString().PadLeft(4, '0');
                tokenB2.IssApplData = origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.GetTagHex("9F10", 64);
            }

            if (modify)
            {
                return tokenB2;
            }
            else
            {
                return null;
            }
        }

        protected TokenB3 ToTargetPosAdditionalDataTokenB3(MasterCard origin)
        {
            var modify = false;
            var tokenB3 = new TokenB3();

            if (origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags.ContainsKey("9F1E"))
            {
                modify = true;
                //tokenB3.TermSerNum = origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.GetTagHex("9F1E", 8);
                // for low-value handle
                //tokenB3.TermSerNum = EbcdicConverter.GetString(origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.GetTagHex("9F1E", 8));
                var TermSerNum = origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.GetTagHex("9F1E", 8);
                //if ("00000000".Equals(TermSerNum))
                //{
                //    tokenB3.TermSerNum = TermSerNum;
                //}
                //else
                //{
                // 2018/02/22
                // for some acq set terminal id by pack numeric info
                if (TermSerNum.Length <= 8)
                {
                    tokenB3.TermSerNum = TermSerNum.PadRight(8, '0');
                }
                else
                {
                    var TermSerNumWork = EbcdicConverter.GetString(TermSerNum.PadRight(16, '0'));
                    tokenB3.TermSerNum = new string(TermSerNumWork.Where(c => !char.IsControl(c)).ToArray()).PadRight(8, '0');
                    //tokenB3.TermSerNum = EbcdicConverter.GetString(TermSerNum);
                }
                //}
            }

            if (origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags.ContainsKey("9F33"))
            {
                modify = true;
                tokenB3.TermCap = origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.GetTagHex("9F33", 6);
            }

            if (origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags.ContainsKey("9F35"))
            {
                modify = true;
                tokenB3.EmvTermTyp = origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.GetTagHex("9F35", 2);
            }

            if (origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags.ContainsKey("9F34"))
            {
                modify = true;
                tokenB3.CvmRslts = origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.GetTagHex("9F34", 6);
            }

            if (origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags.ContainsKey("84"))
            {
                modify = true;
                tokenB3.DfNameLgth = origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags["84"].Value.Length.ToString().PadLeft(4, '0');
                tokenB3.DfName = origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.GetTagHex("84", 32);
            }

            if (modify)
            {
                return tokenB3;
            }
            else
            {
                return null;
            }
        }

        protected TokenB4 ToTargetPosAdditionalDataTokenB4(MasterCard origin)
        {
            var tokenB4 = new TokenB4();

            if (!string.IsNullOrEmpty(origin.F22PointOfServiceEntryModeEncode()))
            {
                tokenB4.PtSrvEntryMde = origin.F22PointOfServiceEntryModeEncode();
            }

            //if (!string.IsNullOrEmpty(origin.ExtendedCore().PointOfServiceData.PosCardDataTerminalInputCapabilityIndicator))
            //{
            //    tokenB4.TermEntryCap = origin.ExtendedCore().PointOfServiceData.PosCardDataTerminalInputCapabilityIndicator;
            //}
            //else
            //{
            tokenB4.TermEntryCap = "5";
            //}

            if (origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags.Count > 0)
            {
                tokenB4.LastEmvStat = MCOLATransactionConst.PosAdditionalData.TokenB4.LastEmvStat.FallbackOrEmv;
            }
            else
            {
                tokenB4.LastEmvStat = MCOLATransactionConst.PosAdditionalData.TokenB4.LastEmvStat.Others;
            }

            tokenB4.DataSuspect = MCOLATransactionConst.PosAdditionalData.TokenB4.DataSuspect.CamDataCorrect;

            if (origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags.ContainsKey("5F34"))
            {
                tokenB4.ApplPanSeqNum = origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.GetTagHex("5F34", 2);
            }
            else if (!string.IsNullOrEmpty(origin.ExtendedCore().CardSequenceNumber))
            {
                var len = origin.ExtendedCore().CardSequenceNumber.Length;

                if (len >= 2)
                {
                    tokenB4.ApplPanSeqNum = origin.ExtendedCore().CardSequenceNumber.IndexSubstring(len - 2, len);
                }
                else
                {
                    tokenB4.ApplPanSeqNum = origin.ExtendedCore().CardSequenceNumber.PadLeft(2, '0');
                }
            }

            tokenB4.DevInfoCamFlg = string.Empty.PadRight(6, ' ');
            tokenB4.RsnOnLineCde = string.Empty.PadRight(4, ' ');

            if (origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                      MasterCardTransactionConst.AdditionalDataPrivateUse.AdditionalProcessingInformationTagId))
            {
                var de48se74 = (MasterCardDE48SE74)origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                                    MasterCardTransactionConst.AdditionalDataPrivateUse.AdditionalProcessingInformationTagId];
                if (de48se74.infos[0].ProcessingInformation.Equals(
                    MasterCardTransactionConst.AdditionalDataPrivateUse.AdditionalProcessingInformation.ProcessingInformation.ApplicationCryptogramInvalid))
                {
                    tokenB4.ArqcVerify = MCOLATransactionConst.PosAdditionalData.TokenB4.ArqcVerify.Failed;
                }
                else if (de48se74.infos[0].ProcessingInformation.Equals(
                    MasterCardTransactionConst.AdditionalDataPrivateUse.AdditionalProcessingInformation.ProcessingInformation.ValidApplicationCryptogram))
                {
                    tokenB4.ArqcVerify = MCOLATransactionConst.PosAdditionalData.TokenB4.ArqcVerify.Passed;
                }
                else
                {
                    tokenB4.ArqcVerify = MCOLATransactionConst.PosAdditionalData.TokenB4.ArqcVerify.Space;
                }
            }
            // tokenB4.ArqcVerify = MCOLATransactionConst.PosAdditionalData.TokenB4.ArqcVerify.NoPerform;

            tokenB4.UsrFld1 = string.Empty.PadRight(1, ' ');

            return tokenB4;
        }

        protected Token04 ToTargetPosAdditionalDataToken04(MasterCard origin)
        {
            var token04 = new Token04();
            token04.ErrorFlg = MCOLATransactionConst.PosAdditionalData.Token04.ErrorFlg.CardVerificationValid;

            if (origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                    MasterCardTransactionConst.AdditionalDataPrivateUse.CardValidationCodeResultTagId))
            {
                var de48se87 = (MasterCardDE48SE87)origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                        MasterCardTransactionConst.AdditionalDataPrivateUse.CardValidationCodeResultTagId];
                if (de48se87.Value.Equals(MasterCardTransactionConst.AdditionalDataPrivateUse.CardValidationCodeResult.InvalidCvc1)
                    || de48se87.Value.Equals(MasterCardTransactionConst.AdditionalDataPrivateUse.CardValidationCodeResult.NoMatch))
                {
                    token04.CrdVeryFlg = MCOLATransactionConst.PosAdditionalData.Token04.CrdVrfyFlg.CvdInvalid;
                }
                else if (de48se87.Value.Equals(MasterCardTransactionConst.AdditionalDataPrivateUse.CardValidationCodeResult.Match))
                {
                    token04.CrdVeryFlg = MCOLATransactionConst.PosAdditionalData.Token04.CrdVrfyFlg.CvdValid;
                }
                else if (de48se87.Value.Equals(MasterCardTransactionConst.AdditionalDataPrivateUse.CardValidationCodeResult.NotProcessed))
                {
                    token04.CrdVeryFlg = MCOLATransactionConst.PosAdditionalData.Token04.CrdVrfyFlg.SecurityDeviceError;
                }
                else if (de48se87.Value.Equals(MasterCardTransactionConst.AdditionalDataPrivateUse.CardValidationCodeResult.E))
                {
                    token04.CrdVeryFlg = MCOLATransactionConst.PosAdditionalData.Token04.CrdVrfyFlg.CrdVrfyFlgE;
                }
            }


            if (!string.IsNullOrEmpty(origin.ExtendedCore().PointOfServiceEntryMode.PosTerminalPanEntryMode))
            {
                switch (origin.ExtendedCore().PointOfServiceEntryMode.PosTerminalPanEntryMode)
                {
                    case MasterCardTransactionConst.PointOfServiceEntryMode.PosTerminalPanEntryMode.Chip:
                    case MasterCardTransactionConst.PointOfServiceEntryMode.PosTerminalPanEntryMode.Contactless:
                    case MasterCardTransactionConst.PointOfServiceEntryMode.PosTerminalPanEntryMode.MagneticStripe:
                    case MasterCardTransactionConst.PointOfServiceEntryMode.PosTerminalPanEntryMode.ContactlessMagneticStripe:
                        token04.CompleteTk2Data = MCOLATransactionConst.PosAdditionalData.Token04.CompleteTk2Data.Completed;
                        break;
                    default:
                        token04.CompleteTk2Data = MCOLATransactionConst.PosAdditionalData.Token04.CompleteTk2Data.NotCompleted;
                        break;
                }
            }
            else
            {
                token04.CompleteTk2Data = MCOLATransactionConst.PosAdditionalData.Token04.CompleteTk2Data.NotCompleted;
            }

            return token04;
        }

        protected Token20 ToTargetPosAdditionalDataToken20(MasterCard origin)
        {
            var token20 = new Token20();

            if (origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                MasterCardTransactionConst.AdditionalDataPrivateUse.TraceId))
            {
                var de48se63 = (MasterCardDE48SE63)origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                    MasterCardTransactionConst.AdditionalDataPrivateUse.TraceId];
                token20.TraceId = de48se63.Value.Trim().PadRight(15, ' ');
                return token20;
            }
            else
            {
                return null;
            }
        }

        protected TokenC0 ToTargetPosAdditionalDataTokenC0(MasterCard origin)
        {
            var modify = false;
            var tokenC0 = new TokenC0();

            if (origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                MasterCardTransactionConst.AdditionalDataPrivateUse.Cvc2TagId))
            {
                modify = true;
                var de48Se92 = (MasterCardDE48SE92)origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                    MasterCardTransactionConst.AdditionalDataPrivateUse.Cvc2TagId];

                tokenC0.CvdFld = de48Se92.Cvc2.Trim(' ').PadRight(4, ' ');
            }

            switch (origin.Mti())
            {
                case MasterCardTransactionConst.MTI.AuthorizationRequest:
                case MasterCardTransactionConst.MTI.AuthorizationAdviceRequest:
                case MasterCardTransactionConst.MTI.ReversalRequest:
                    modify = true;

                    if (origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                        MasterCardTransactionConst.AdditionalDataPrivateUse.MobileProgramIndicatorsTagId))
                    {
                        tokenC0.MotoOrECFlag = MCOLATransactionConst.PosAdditionalData.TokenC0.MoToEcFlag.MailPhoneOrder;
                    }
                    else if (origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                        MasterCardTransactionConst.AdditionalDataPrivateUse.ElectronicCommerceIndicatorsTagId))
                    {
                        switch (((MasterCardDE48SE42)origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                        MasterCardTransactionConst.AdditionalDataPrivateUse.ElectronicCommerceIndicatorsTagId])
                        .SecurityLevelIndicator)
                        {
                            case MasterCardTransactionConst.AdditionalDataPrivateUse.ElectronicCommerceIndicators.SecurityLevelIndicator.ChannelEncryptCertNotUsed:
                                tokenC0.MotoOrECFlag = MCOLATransactionConst.PosAdditionalData.TokenC0.MoToEcFlag.NonAuthenticated;
                                break;
                            case MasterCardTransactionConst.AdditionalDataPrivateUse.ElectronicCommerceIndicators.SecurityLevelIndicator.NoSecurityProtocolCertNotUsed:
                                tokenC0.MotoOrECFlag = MCOLATransactionConst.PosAdditionalData.TokenC0.MoToEcFlag.NonSecureEc;
                                break;
                            case MasterCardTransactionConst.AdditionalDataPrivateUse.ElectronicCommerceIndicators.SecurityLevelIndicator.DigitalSecureRemotePayment:
                                switch (((MasterCardDE48SE42)origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                                         MasterCardTransactionConst.AdditionalDataPrivateUse.ElectronicCommerceIndicatorsTagId])
                                         .UcafCollectionIndicator)
                                {
                                    case MasterCardTransactionConst.AdditionalDataPrivateUse.ElectronicCommerceIndicators.UCAFCollectionIndicator.UCAFMerchantRiskBasedDecisioning:
                                        tokenC0.MotoOrECFlag = MCOLATransactionConst.PosAdditionalData.TokenC0.MoToEcFlag.NonAuthenticated;
                                        break;
                                    case MasterCardTransactionConst.AdditionalDataPrivateUse.ElectronicCommerceIndicators.UCAFCollectionIndicator.UCAFDataCollectionSupportFullyAAV:
                                        tokenC0.MotoOrECFlag = MCOLATransactionConst.PosAdditionalData.TokenC0.MoToEcFlag.ChannelEncryption;
                                        break;
                                    case MasterCardTransactionConst.AdditionalDataPrivateUse.ElectronicCommerceIndicators.UCAFCollectionIndicator.UCAFPartialShipmentRecurringpayment:
                                        tokenC0.MotoOrECFlag = MCOLATransactionConst.PosAdditionalData.TokenC0.MoToEcFlag.RecurringTransaction;
                                        break;
                                    default:
                                        tokenC0.MotoOrECFlag = MCOLATransactionConst.PosAdditionalData.TokenC0.MoToEcFlag.NotApplicable;
                                        break;
                                }
                                break;
                            default:
                                tokenC0.MotoOrECFlag = MCOLATransactionConst.PosAdditionalData.TokenC0.MoToEcFlag.NotApplicable;
                                break;
                        }
                    }
                    else
                    {
                        tokenC0.MotoOrECFlag = MCOLATransactionConst.PosAdditionalData.TokenC0.MoToEcFlag.NotApplicable;
                    }

                    if (origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                        MasterCardTransactionConst.AdditionalDataPrivateUse.ElectronicCommerceIndicatorsTagId))
                    {
                        var UcafAuthnCollInd = ((MasterCardDE48SE42)origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                            MasterCardTransactionConst.AdditionalDataPrivateUse.ElectronicCommerceIndicatorsTagId]).UcafCollectionIndicator;
                        tokenC0.UcafAuthnCollInd = UcafAuthnCollInd;
                    }

                    break;
                default:
                    break;
            }

            if (modify)
            {
                return tokenC0;
            }
            else
            {
                return null;
            }
        }

        protected TokenCE ToTargetPosAdditionalDataTokenCE(MasterCard origin)
        {
            var tokenCE = new TokenCE();

            if (origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                MasterCardTransactionConst.AdditionalDataPrivateUse.UniversalCardholderAuthenticationFieldTagId))
            {
                var de48se43 = origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                    MasterCardTransactionConst.AdditionalDataPrivateUse.UniversalCardholderAuthenticationFieldTagId];

                tokenCE.AuthIndFlg = MCOLATransactionConst.PosAdditionalData.TokenCE.AuthIndFlg.Ucaf;
                tokenCE.AuthIndData = de48se43.Value.PadRight(200, ' ');
            }
            else
            {
                return null;
            }

            return tokenCE;
        }

        protected TokenC4 ToTargetPosAdditionalDataTokenC4(MasterCard origin)
        {
            var tokenC4 = new TokenC4();

            if (!ObjectUtils.IsNull(origin.ExtendedCore().PointOfServiceData))
            {
                tokenC4.TermAttendInd = origin.ExtendedCore().PointOfServiceData.PosTerminalAttendance;
                tokenC4.TermLocInd = origin.ExtendedCore().PointOfServiceData.PosTerminalLocation;
                tokenC4.ChPresentInd = origin.ExtendedCore().PointOfServiceData.PosCardholderPresence;
                tokenC4.CrdPresentInd = origin.ExtendedCore().PointOfServiceData.PosCardPresence;
                tokenC4.CrdCaptrInd = origin.ExtendedCore().PointOfServiceData.PosCardCaptureCapabilities;
                tokenC4.TxnStatInd = origin.ExtendedCore().PointOfServiceData.PosTransactionStatus;
                tokenC4.TxnSecInd = origin.ExtendedCore().PointOfServiceData.PosTransactionSecurity;
                tokenC4.ChActvtInd = origin.ExtendedCore().PointOfServiceData.CardholderActivatedTerminalLevel;
                tokenC4.TermInputCapInd = origin.ExtendedCore().PointOfServiceData.PosCardDataTerminalInputCapabilityIndicator;
            }
            else
            {
                return null;
            }

            return tokenC4;
        }

        protected TokenCH ToTargetPosAdditionalDataTokenCH(MasterCard origin)
        {
            var modify = false;
            var tokenCH = new TokenCH();

            tokenCH.RecurPmntInd = string.Empty.PadRight(1, ' ');

            if (origin.Mti().Equals(MasterCardTransactionConst.MTI.ReversalRequest) &&
                origin.Core().ResponseCode.Equals(MasterCardTransactionConst.ResponseCode.Reversal.SuspectFraud))
            {
                modify = true;
                tokenCH.RvslRsnInd = MCOLATransactionConst.PosAdditionalData.TokenCH.RvslRsnInd.SuspectFraud;
            }

            if (origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                MasterCardTransactionConst.AdditionalDataPrivateUse.PosDataExtendedConditionCodesTagId))
            {
                modify = true;
                var de48se61 = (MasterCardDE48SE61)origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                    MasterCardTransactionConst.AdditionalDataPrivateUse.PosDataExtendedConditionCodesTagId];
                tokenCH.AuthMsgInd = de48se61.FinalAuthorizationIndicator;
            }

            if (!ObjectUtils.IsNull(origin.ExtendedCore().PointOfServiceData))
            {
                modify = true;
                tokenCH.TermTyp = origin.ExtendedCore().PointOfServiceData.CardholderActivatedTerminalLevel;
            }

            if (modify)
            {
                return tokenCH;
            }
            else
            {
                return null;
            }
        }

        protected TokenCZ ToTargetPosAdditionalDataTokenCZ(MasterCard origin)
        {
            var modify = false;
            var tokenCZ = new TokenCZ();

            if (origin.Mti().Equals(MasterCardTransactionConst.MTI.AuthorizationRequest)
                || origin.Mti().Equals(MasterCardTransactionConst.MTI.AuthorizationAdviceRequest))
            {
                if (origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                    MasterCardTransactionConst.AdditionalDataPrivateUse.ATCInformation))
                {
                    modify = true;
                    var de48se34 = (MasterCardDE48SE34)origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                        MasterCardTransactionConst.AdditionalDataPrivateUse.ATCInformation];

                    int ProcATC;
                    if (int.TryParse(de48se34.ATC, out ProcATC))
                    {
                        var ConvIntStr = Convert.ToString(ProcATC, 16).PadLeft(tokenCZ.ATC.Length, '0').ToUpper();
                        tokenCZ.ATC = ConvIntStr.Substring((ConvIntStr.Length - tokenCZ.ATC.Length), tokenCZ.ATC.Length);
                    }

                    tokenCZ.ATCDiscrepancyIndicator = de48se34.ATCDiscrepancyIndicator;

                    if (int.TryParse(de48se34.ATCDiscrepancyValue, out ProcATC))
                    {
                        tokenCZ.ATCDiscrepancyValue = de48se34.ATCDiscrepancyValue;
                    }
                }
            }

            if (modify)
            {
                return tokenCZ;
            }
            else
            {
                return null;
            }
        }

        protected TokenF1 ToTargetPosAdditionalDataTokenF1(MasterCard origin)
        {
            var modify = false;
            var tokenF1 = new TokenF1();

            if (origin.Mti().Equals(MasterCardTransactionConst.MTI.AuthorizationRequest)
                || origin.Mti().Equals(MasterCardTransactionConst.MTI.AuthorizationAdviceRequest)
                || origin.Mti().Equals(MasterCardTransactionConst.MTI.AdministrativeAdviceRequest)
                || origin.Mti().Equals(MasterCardTransactionConst.MTI.ReversalRequest))
            {
                if (origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                    MasterCardTransactionConst.AdditionalDataPrivateUse.MastercardAssignedTagId))
                {
                    modify = true;
                    var de48Se32 = (MasterCardDE48SE32)origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                        MasterCardTransactionConst.AdditionalDataPrivateUse.MastercardAssignedTagId];

                    tokenF1.AssignedId = de48Se32.MastercardAssignedId.PadRight(15, ' ');
                }
                else
                {
                    tokenF1.AssignedId = string.Empty.PadRight(15, ' ');
                }

                if (origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                    MasterCardTransactionConst.AdditionalDataPrivateUse.ElectronicCommerceIndicatorsTagId))
                {
                    modify = true;

                    tokenF1.CardholderAuthenticationIndicator = ((MasterCardDE48SE42)origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                    MasterCardTransactionConst.AdditionalDataPrivateUse.ElectronicCommerceIndicatorsTagId])
                    .SecurityLevelIndicator.Substring(1, 1);
                    //switch (((MasterCardDE48SE42)origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                    //MasterCardTransactionConst.AdditionalDataPrivateUse.ElectronicCommerceIndicatorsTagId])
                    //.SecurityLevelIndicator)
                    //{
                    //    case MasterCardTransactionConst.AdditionalDataPrivateUse.ElectronicCommerceIndicators.SecurityLevelIndicator.DigitalSecureRemotePayment:
                    //        tokenF1.CardholderAuthenticationIndicator =
                    //            MasterCardTransactionConst.AdditionalDataPrivateUse.ElectronicCommerceIndicators.CardholderAuthentication.DigitalSecureRemotePaymentUCAF;
                    //        break;
                    //    default:
                    //        tokenF1.CardholderAuthenticationIndicator =
                    //            MasterCardTransactionConst.AdditionalDataPrivateUse.ElectronicCommerceIndicators.CardholderAuthentication.NotApplicable;
                    //        break;
                    //}
                }
            }
            else if (origin.Mti().Equals(MasterCardTransactionConst.MTI.ReversalAdviceRequest))
            {
                if (origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                    MasterCardTransactionConst.AdditionalDataPrivateUse.MastercardAssignedTagId))
                {
                    modify = true;
                    var de48Se32 = (MasterCardDE48SE32)origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                        MasterCardTransactionConst.AdditionalDataPrivateUse.MastercardAssignedTagId];

                    tokenF1.AssignedId = de48Se32.MastercardAssignedId.PadRight(15, ' ');
                    tokenF1.CardholderAuthenticationIndicator =
                        MasterCardTransactionConst.AdditionalDataPrivateUse.ElectronicCommerceIndicators.CardholderAuthentication.NotApplicable;
                }
                else
                {
                    tokenF1.AssignedId = string.Empty.PadRight(15, ' ');
                }
            }

            if (modify)
            {
                return tokenF1;
            }
            else
            {
                return null;
            }
        }

        protected TokenU7 ToTargetPosAdditionalDataTokenU7(MasterCard origin)
        {
            var modify = false;
            var tokenU7 = new TokenU7();

            if (origin.IsTokenizationAuthorizationRequest())
            {
                modify = true;

                switch (origin.ExtendedCore().MemberDefinedData.WalletServiceProviderTokenizationRecommendation)
                {
                    case MasterCardTransactionConst.MemberDefinedData.WalletServiceProviderTokenizationRecommendation.Approve:
                        tokenU7.WalletProviderRiskAssessment =
                            MCOLATransactionConst.PosAdditionalData.TokenU7.WalletProviderRiskAssessment.Approve;
                        break;
                    case MasterCardTransactionConst.MemberDefinedData.WalletServiceProviderTokenizationRecommendation.RequireAdditionalAuthentication:
                        tokenU7.WalletProviderRiskAssessment =
                            MCOLATransactionConst.PosAdditionalData.TokenU7.WalletProviderRiskAssessment.ConditionallyApproved;
                        break;
                    case MasterCardTransactionConst.MemberDefinedData.WalletServiceProviderTokenizationRecommendation.Decline:
                        tokenU7.WalletProviderRiskAssessment =
                            MCOLATransactionConst.PosAdditionalData.TokenU7.WalletProviderRiskAssessment.Decline;
                        break;
                    default:
                        break;
                }
            }

            if (origin.IsActivationCodeNotification())
            {
                modify = true;
                tokenU7.Otp = origin.ExtendedCore().MemberDefinedData.ActivationCode.PadRight(16, ' ');
            }

            if (origin.Mti().Equals(MasterCardTransactionConst.MTI.AuthorizationAdviceRequest)
                || origin.Mti().Equals(MasterCardTransactionConst.MTI.AuthorizationRequest)
                || origin.Mti().Equals(MasterCardTransactionConst.MTI.ReversalRequest)
                || origin.Mti().Equals(MasterCardTransactionConst.MTI.ReversalAdviceRequest))
            {
                if (origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                    MasterCardTransactionConst.AdditionalDataPrivateUse.WalletProgramDataTagId))
                {
                    modify = true;
                    tokenU7.WalletProviderId = ((MasterCardDE48SE26)origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                        MasterCardTransactionConst.AdditionalDataPrivateUse.WalletProgramDataTagId]).WalletIdentifier.PadRight(11, ' ');
                }
            }

            if (!string.IsNullOrEmpty(origin.ExtendedCore().NetworkData.FinancialNetworkCode))
            {
                modify = true;
                tokenU7.FinancialNetworkCode = origin.ExtendedCore().NetworkData.FinancialNetworkCode;
            }
            if (!string.IsNullOrEmpty(origin.ExtendedCore().NetworkData.BanknetReferenceNumber))
            {
                modify = true;
                tokenU7.BanknetReferenceNumber = origin.ExtendedCore().NetworkData.BanknetReferenceNumber.PadRight(9, ' ');
            }

            if (origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                    MasterCardTransactionConst.AdditionalDataPrivateUse.OriginalTransactionDataTagId))
            {
                modify = true;
                var de48se31 = (MasterCardDE48SE31)origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                    MasterCardTransactionConst.AdditionalDataPrivateUse.OriginalTransactionDataTagId];

                tokenU7.OriginalDe3TransactionType = de48se31.OriginalProcessingCode.Value;
                tokenU7.OriginalMti = de48se31.OriginalMessageTypeIdentifier.Value;
            }

            if (modify)
            {
                return tokenU7;
            }
            else
            {
                return null;
            }
        }

        protected void FromTargetPosAdditionalData(MCOLA target, MasterCard origin)
        {
            var tokenGroup = (TokenGroup)target.Core().GetOthersValByKey("F_63");

            if (tokenGroup == null)
            {
                return;
            }

            foreach (var t in tokenGroup.Tokens)
            {
                switch (t.Key)
                {
                    case NcccTransactionConst.PosAdditionalData.TokenB2.Id:
                        this.FromTargetPosAdditionalDataTokenB2(origin, (TokenB2)t.Value);
                        break;
                    case NcccTransactionConst.PosAdditionalData.TokenB3.Id:
                        this.FromTargetPosAdditionalDataTokenB3(origin, (TokenB3)t.Value);
                        break;
                    case NcccTransactionConst.PosAdditionalData.TokenB4.Id:
                        this.FromTargetPosAdditionalDataTokenB4(origin, (TokenB4)t.Value);
                        break;
                    case NcccTransactionConst.PosAdditionalData.TokenB5.Id:
                        this.FromTargetPosAdditionalDataTokenB5(origin, (TokenB5)t.Value);
                        break;
                    case NcccTransactionConst.PosAdditionalData.TokenB6.Id:
                        this.FromTargetPosAdditionalDataTokenB6(origin, (TokenB6)t.Value);
                        break;
                    case NcccTransactionConst.PosAdditionalData.Token04.Id:
                        if (tokenGroup.Tokens.ContainsKey(NcccTransactionConst.PosAdditionalData.TokenC0.Id))
                        {
                            this.FromTargetPosAdditionalDataToken04(origin, (Token04)t.Value,
                                (TokenC0)tokenGroup.GetToken(NcccTransactionConst.PosAdditionalData.TokenC0.Id));
                        }
                        break;
                    case NcccTransactionConst.PosAdditionalData.TokenC0.Id:
                        this.FromTargetPosAdditionalDataTokenC0(origin, (TokenC0)t.Value);
                        break;
                    case NcccTransactionConst.PosAdditionalData.TokenCE.Id:
                        this.FromTargetPosAdditionalDataTokenCE(origin, (TokenCE)t.Value);
                        break;
                    case NcccTransactionConst.PosAdditionalData.TokenU7.Id:
                        this.FromTargetPosAdditionalDataTokenU7(origin, (TokenU7)t.Value);
                        break;
                }
            }
        }

        protected void FromTargetPosAdditionalDataTokenB2(MasterCard origin, TokenB2 tokenB2)
        {
            if (tokenB2 == null)
            {
                return;
            }

            if (origin is TM.MasterCard.Transaction.AlternativeNetworkRoutingRequest)
            {

                var de55tag9F27 = new Tag();
                de55tag9F27.Id = "9F27";
                de55tag9F27.Value = HexStringConverter.ToByteArray(tokenB2.CryptoInfData);
                origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags[de55tag9F27.Id] = de55tag9F27;

                var de55tag95 = new Tag();
                de55tag95.Id = "95";
                de55tag95.Value = HexStringConverter.ToByteArray(tokenB2.Tvr);
                origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags[de55tag95.Id] = de55tag95;

                var de55tag9F26 = new Tag();
                de55tag9F26.Id = "9F26";
                de55tag9F26.Value = HexStringConverter.ToByteArray(tokenB2.Arqc);
                origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags[de55tag9F26.Id] = de55tag9F26;

                var de55tag9F02 = new Tag();
                de55tag9F02.Id = "9F02";
                de55tag9F02.Value = HexStringConverter.ToByteArray(tokenB2.AmtAuth);
                origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags[de55tag9F02.Id] = de55tag9F02;

                var de55tag9F03 = new Tag();
                de55tag9F03.Id = "9F03";
                de55tag9F03.Value = HexStringConverter.ToByteArray(tokenB2.AmtOther);
                origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags[de55tag9F03.Id] = de55tag9F03;

                var de55tag82 = new Tag();
                de55tag82.Id = "82";
                de55tag82.Value = HexStringConverter.ToByteArray(tokenB2.Aip);
                origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags[de55tag82.Id] = de55tag82;

                var de55tag9F36 = new Tag();
                de55tag9F36.Id = "9F36";
                de55tag9F36.Value = HexStringConverter.ToByteArray(tokenB2.Atc);
                origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags[de55tag9F36.Id] = de55tag9F36;

                var de55tag9F1A = new Tag();
                de55tag9F1A.Id = "9F1A";
                de55tag9F1A.Value = HexStringConverter.ToByteArray(tokenB2.TermCntryCde.Trim().PadLeft(4, '0'));
                origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags[de55tag9F1A.Id] = de55tag9F1A;

                var de55tag5F2A = new Tag();
                de55tag5F2A.Id = "5F2A";
                de55tag5F2A.Value = HexStringConverter.ToByteArray(tokenB2.TermCrncyCde.Trim().PadLeft(4, '0'));
                origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags[de55tag5F2A.Id] = de55tag5F2A;

                var de55tag9A = new Tag();
                de55tag9A.Id = "9A";
                de55tag9A.Value = HexStringConverter.ToByteArray(tokenB2.TranDat);
                origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags[de55tag9A.Id] = de55tag9A;

                var de55tag9C = new Tag();
                de55tag9C.Id = "9C";
                de55tag9C.Value = HexStringConverter.ToByteArray(tokenB2.TranType);
                origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags[de55tag9C.Id] = de55tag9C;

                var de55tag9F37 = new Tag();
                de55tag9F37.Id = "9F37";
                de55tag9F37.Value = HexStringConverter.ToByteArray(tokenB2.UnpredictNum);
                origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags[de55tag9F37.Id] = de55tag9F37;

                int IssApplLength = 0;
                if (int.TryParse(tokenB2.IssApplDataLgth, out IssApplLength))
                {
                    if (IssApplLength > 0)
                    {
                        if (IssApplLength < 33)
                        {
                            IssApplLength = IssApplLength * 2;
                        }
                        else if (IssApplLength % 2 == 1)
                        {
                            IssApplLength++;
                        }
                        var de55tag9F10 = new Tag();
                        de55tag9F10.Id = "9F10";
                        var IssApplData = tokenB2.IssApplData.Substring(0, IssApplLength);
                        de55tag9F10.Value = HexStringConverter.ToByteArray(IssApplData.PadRight(IssApplLength, '0'));
                        origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags[de55tag9F10.Id] = de55tag9F10;
                    }
                }
            }
        }

        protected void FromTargetPosAdditionalDataTokenB3(MasterCard origin, TokenB3 tokenB3)
        {
            if (tokenB3 == null)
            {
                return;
            }

            if (origin is TM.MasterCard.Transaction.AlternativeNetworkRoutingRequest)
            {

                if (!string.IsNullOrEmpty(tokenB3.TermSerNum.Trim('0')))
                {
                    var de55tag9F1E = new Tag();
                    de55tag9F1E.Id = "9F1E";
                    //de55tag9F1E.Value = HexStringConverter.ToByteArray(tokenB3.TermSerNum);
                    de55tag9F1E.Value = HexStringConverter.ToByteArray(EbcdicConverter.FromString(tokenB3.TermSerNum));
                    origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags[de55tag9F1E.Id] = de55tag9F1E;
                }

                var de55tag9F33 = new Tag();
                de55tag9F33.Id = "9F33";
                de55tag9F33.Value = HexStringConverter.ToByteArray(tokenB3.TermCap);
                origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags[de55tag9F33.Id] = de55tag9F33;

                var de55tag9F35 = new Tag();
                de55tag9F35.Id = "9F35";
                de55tag9F35.Value = HexStringConverter.ToByteArray(tokenB3.EmvTermTyp);
                origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags[de55tag9F35.Id] = de55tag9F35;

                var de55tag9F34 = new Tag();
                de55tag9F34.Id = "9F34";
                de55tag9F34.Value = HexStringConverter.ToByteArray(tokenB3.CvmRslts);
                origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags[de55tag9F34.Id] = de55tag9F34;

                int DfLength = 0;
                if (int.TryParse(tokenB3.DfNameLgth, out DfLength))
                {
                    if (DfLength > 0)
                    {
                        if (DfLength < 17)
                        {
                            DfLength = DfLength * 2;
                        }
                        else if (DfLength % 2 == 1)
                        {
                            DfLength++;
                        }
                        var de55tag84 = new Tag();
                        de55tag84.Id = "84";
                        var DfName = tokenB3.DfName.Substring(0, DfLength);
                        de55tag84.Value = HexStringConverter.ToByteArray(DfName.PadRight(DfLength, '0'));
                        origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags[de55tag84.Id] = de55tag84;
                    }
                }
            }
        }

        protected void FromTargetPosAdditionalDataTokenB4(MasterCard origin, TokenB4 tokenB4)
        {
            if (tokenB4 == null)
            {
                return;
            }

            if (origin is AuthorizationResponse)
            {
                MasterCardDE48SE74 de48se74 = null;

                if (origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                    MasterCardTransactionConst.AdditionalDataPrivateUse.AdditionalProcessingInformationTagId))
                {
                    de48se74 = (MasterCardDE48SE74)origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                        MasterCardTransactionConst.AdditionalDataPrivateUse.AdditionalProcessingInformationTagId];
                }
                else
                {
                    de48se74 = new MasterCardDE48SE74();
                }

                var info = new AdditionalProcessingInformation();
                info.ProcessingIndicator = MasterCardTransactionConst.AdditionalDataPrivateUse.AdditionalProcessingInformation.ProcessingIndicator.IssuerChipValidation;

                switch (tokenB4.ArqcVerify)
                {
                    case MCOLATransactionConst.PosAdditionalData.TokenB4.ArqcVerify.Failed:
                        info.ProcessingInformation = MasterCardTransactionConst.AdditionalDataPrivateUse.AdditionalProcessingInformation.ProcessingInformation.ApplicationCryptogramInvalid;
                        de48se74.infos.Add(info);
                        origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[de48se74.Id] = de48se74;
                        break;
                    case MCOLATransactionConst.PosAdditionalData.TokenB4.ArqcVerify.Passed:
                        info.ProcessingInformation = MasterCardTransactionConst.AdditionalDataPrivateUse.AdditionalProcessingInformation.ProcessingInformation.ValidApplicationCryptogram;
                        de48se74.infos.Add(info);
                        origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[de48se74.Id] = de48se74;
                        break;
                    default:
                        break;
                }
            }

            if (origin is TM.MasterCard.Transaction.AlternativeNetworkRoutingRequest)
            {
                Regex rgx = new Regex(@"^[0-9A-F]+$");
                if (rgx.IsMatch(tokenB4.ApplPanSeqNum))
                {
                    var de55tag5F34 = new Tag();
                    de55tag5F34.Id = "5F34";
                    de55tag5F34.Value = HexStringConverter.ToByteArray(tokenB4.ApplPanSeqNum);
                    origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags[de55tag5F34.Id] = de55tag5F34;
                }
            }
        }

        protected void FromTargetPosAdditionalDataTokenB5(MasterCard origin, TokenB5 tokenB5)
        {
            if (tokenB5 == null)
            {
                return;
            }

            if (origin is AuthorizationResponse)
            {
                var de55tag91 = new Tag();
                de55tag91.Id = "91";
                de55tag91.Value = HexStringConverter.ToByteArray(tokenB5.Arpc + tokenB5.IssRespCde);

                origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags[de55tag91.Id] = de55tag91;
            }
        }

        protected void FromTargetPosAdditionalDataTokenB6(MasterCard origin, TokenB6 tokenB6)
        {
            if (tokenB6 == null)
            {
                return;
            }

            if (origin is AuthorizationResponse)
            {
                var de55tag71 = new Tag();
                de55tag71.Id = "71";
                de55tag71.Value = HexStringConverter.ToByteArray(tokenB6.IssScriptData);

                origin.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags[de55tag71.Id] = de55tag71;
            }
        }

        protected void FromTargetPosAdditionalDataToken04(MasterCard origin, Token04 token04, TokenC0 tokenC0)
        {
            if (token04 == null)
            {
                return;
            }

            if (tokenC0 == null)
            {
                return;
            }

            if (origin is AuthorizationResponse)
            {
                MasterCardDE48SE87 de48se87 = null;

                if (origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                    MasterCardTransactionConst.AdditionalDataPrivateUse.CardValidationCodeResultTagId))
                {
                    de48se87 = (MasterCardDE48SE87)origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                        MasterCardTransactionConst.AdditionalDataPrivateUse.CardValidationCodeResultTagId];
                }
                else
                {
                    de48se87 = new MasterCardDE48SE87();
                }

                int i;
                var isTokenC0CvdFldNumber = int.TryParse(tokenC0.CvdFld, out i);

                if (isTokenC0CvdFldNumber)
                {
                    switch (token04.CrdVeryFlg)
                    {
                        case MCOLATransactionConst.PosAdditionalData.Token04.CrdVrfyFlg.CvdInvalid:
                        case MCOLATransactionConst.PosAdditionalData.Token04.CrdVrfyFlg.CvdInvalidErrFlgC:
                            de48se87.Cvv2Response = MasterCardTransactionConst.AdditionalDataPrivateUse.CardValidationCodeResult.NoMatch;
                            break;
                        case MCOLATransactionConst.PosAdditionalData.Token04.CrdVrfyFlg.SecurityDeviceError:
                            de48se87.Cvv2Response = MasterCardTransactionConst.AdditionalDataPrivateUse.CardValidationCodeResult.NotProcessed;
                            break;
                        case MCOLATransactionConst.PosAdditionalData.Token04.CrdVrfyFlg.CvdValid:
                            de48se87.Cvv2Response = MasterCardTransactionConst.AdditionalDataPrivateUse.CardValidationCodeResult.Match;
                            break;
                        default:
                            return;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(token04.CrdVeryFlg))
                    {
                        if (token04.CrdVeryFlg.Equals(MCOLATransactionConst.PosAdditionalData.Token04.CrdVrfyFlg.CvdInvalidErrFlgC)
                            || token04.CrdVeryFlg.Equals(MCOLATransactionConst.PosAdditionalData.Token04.CrdVrfyFlg.CvdInvalid))
                        {
                            de48se87.Cvv2Response = MasterCardTransactionConst.AdditionalDataPrivateUse.CardValidationCodeResult.InvalidCvc1;
                        }
                        else if (token04.CrdVeryFlg.Equals(MCOLATransactionConst.PosAdditionalData.Token04.CrdVrfyFlg.CrdVrfyFlgE))
                        {
                            de48se87.Cvv2Response = MasterCardTransactionConst.AdditionalDataPrivateUse.CardValidationCodeResult.E;
                        }
                        else if (token04.CrdVeryFlg.Equals(MCOLATransactionConst.PosAdditionalData.Token04.CrdVrfyFlg.CrdVrfyFlgP))
                        {
                            de48se87.Cvv2Response = MasterCardTransactionConst.AdditionalDataPrivateUse.CardValidationCodeResult.NotProcessed;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(de48se87.ValueEncode()))
                {
                    origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[de48se87.Id] = de48se87;
                }
            }
        }

        protected void FromTargetPosAdditionalDataTokenC0(MasterCard origin, TokenC0 tokenC0)
        {
            if (origin is TM.MasterCard.Transaction.AlternativeNetworkRoutingRequest ||
                //origin is TM.MasterCard.Transaction.TransactionHistoryAdviceRequest ||
                origin is TM.MasterCard.Transaction.ReversalResponse)
            {
                MasterCardDE48SE42 de48se42 = null;
                if (origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                    MasterCardTransactionConst.AdditionalDataPrivateUse.ElectronicCommerceIndicatorsTagId))
                {
                    de48se42 = ((MasterCardDE48SE42)origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                       MasterCardTransactionConst.AdditionalDataPrivateUse.ElectronicCommerceIndicatorsTagId]);

                    de48se42.UcafCollectionIndicator = tokenC0.UcafAuthnCollInd;
                }
                else
                {
                    de48se42 = new MasterCardDE48SE42();
                    de48se42.UcafCollectionIndicator = tokenC0.UcafAuthnCollInd;
                }

                origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[de48se42.Id] = de48se42;
            }
        }

        protected void FromTargetPosAdditionalDataTokenCE(MasterCard origin, TokenCE tokenCE)
        {
            if (origin is TM.MasterCard.Transaction.AlternativeNetworkRoutingRequest ||
                //origin is TM.MasterCard.Transaction.TransactionHistoryAdviceRequest ||
                origin is TM.MasterCard.Transaction.ReversalResponse)
            {
                if (!string.IsNullOrEmpty(tokenCE.AuthIndFlg)
                    &&
                    tokenCE.AuthIndFlg.Equals(MCOLATransactionConst.PosAdditionalData.TokenCE.AuthIndFlg.Ucaf))
                {
                    MasterCardDE48SE43 de48se43 = null;

                    if (origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                        MasterCardTransactionConst.AdditionalDataPrivateUse.UniversalCardholderAuthenticationFieldTagId))
                    {
                        de48se43 = (MasterCardDE48SE43)origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                            MasterCardTransactionConst.AdditionalDataPrivateUse.UniversalCardholderAuthenticationFieldTagId];
                    }
                    else
                    {
                        de48se43 = new MasterCardDE48SE43();
                    }

                    de48se43.Value = tokenCE.AuthIndData.Substring(0, 28);
                    origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[de48se43.Id] = de48se43;
                }
            }
        }

        protected void FromTargetPosAdditionalDataTokenU7(MasterCard origin, TokenU7 tokenU7)
        {
            if (origin is TM.MasterCard.Transaction.TransactionHistoryAdviceRequest)
            {
                MasterCardDE48SE31 de48se31 = null;

                if (origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                    MasterCardTransactionConst.AdditionalDataPrivateUse.OriginalTransactionDataTagId))
                {
                    de48se31 = (MasterCardDE48SE31)origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                        MasterCardTransactionConst.AdditionalDataPrivateUse.OriginalTransactionDataTagId];
                }
                else
                {
                    de48se31 = new MasterCardDE48SE31();
                }

                de48se31.OriginalProcessingCode.Value = tokenU7.OriginalDe3TransactionType;
                de48se31.OriginalMessageTypeIdentifier.Value = tokenU7.OriginalMti;
                origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[de48se31.Id] = de48se31;

                origin.ExtendedCore().NetworkData.FinancialNetworkCode = tokenU7.FinancialNetworkCode;
                origin.ExtendedCore().NetworkData.BanknetReferenceNumber = tokenU7.BanknetReferenceNumber.Trim();
            }
        }

        //// MCOLA DE 70
        protected void ToTargetNetworkManagementInformationCode(MasterCard origin, MCOLA target)
        {
            switch (origin.Core().NetworkManagementInformationCode)
            {
                case MasterCardTransactionConst.NetworkManagementInformationCode.GroupSignOn:
                    target.Core().NetworkManagementInformationCode =
                        MCOLATransactionConst.NetworkManagementInformationCode.SignOn;
                    break;
                case MasterCardTransactionConst.NetworkManagementInformationCode.GroupSignOff:
                    target.Core().NetworkManagementInformationCode =
                        MCOLATransactionConst.NetworkManagementInformationCode.SignOff;
                    break;
                case MasterCardTransactionConst.NetworkManagementInformationCode.EchoTest:
                    target.Core().NetworkManagementInformationCode =
                        MCOLATransactionConst.NetworkManagementInformationCode.EchoTest;
                    break;
                case MasterCardTransactionConst.NetworkManagementInformationCode.EncryptionKeyExchangeRequest:
                    target.Core().NetworkManagementInformationCode =
                        MCOLATransactionConst.NetworkManagementInformationCode.NewKey;
                    break;
                case MasterCardTransactionConst.NetworkManagementInformationCode.EncryptionKeyExchangeOnDemandRequest:
                    target.Core().NetworkManagementInformationCode =
                        MCOLATransactionConst.NetworkManagementInformationCode.ChangeKey;
                    break;
                case MasterCardTransactionConst.NetworkManagementInformationCode.ANR:
                    target.Core().NetworkManagementInformationCode =
                        MCOLATransactionConst.NetworkManagementInformationCode.ANR;
                    break;
                default:
                    target.Core().NetworkManagementInformationCode =
                        origin.Core().NetworkManagementInformationCode;
                    break;
            }
        }

        protected void FromTargetNetworkManagementInformationCode(MCOLA target, MasterCard origin)
        {
            switch (target.Core().NetworkManagementInformationCode)
            {
                case MCOLATransactionConst.NetworkManagementInformationCode.SignOn:
                    origin.Core().NetworkManagementInformationCode =
                        MasterCardTransactionConst.NetworkManagementInformationCode.GroupSignOn;
                    break;
                case MCOLATransactionConst.NetworkManagementInformationCode.SignOff:
                    origin.Core().NetworkManagementInformationCode =
                        MasterCardTransactionConst.NetworkManagementInformationCode.GroupSignOff;
                    break;
                case MCOLATransactionConst.NetworkManagementInformationCode.EchoTest:
                    origin.Core().NetworkManagementInformationCode =
                        MasterCardTransactionConst.NetworkManagementInformationCode.EchoTest;
                    break;
                case MCOLATransactionConst.NetworkManagementInformationCode.ChangeKey:
                    origin.Core().NetworkManagementInformationCode =
                        MasterCardTransactionConst.NetworkManagementInformationCode.EncryptionKeyExchangeRequest;
                    break;
                case MCOLATransactionConst.NetworkManagementInformationCode.NewKey:
                    origin.Core().NetworkManagementInformationCode =
                        MasterCardTransactionConst.NetworkManagementInformationCode.EncryptionKeyExchangeOnDemandRequest;
                    break;
                case MCOLATransactionConst.NetworkManagementInformationCode.ANR:
                    origin.Core().NetworkManagementInformationCode =
                        MasterCardTransactionConst.NetworkManagementInformationCode.ANR;
                    break;
                default:
                    origin.Core().NetworkManagementInformationCode =
                        target.Core().NetworkManagementInformationCode;
                    break;
            }
        }

        //// MCOLA DE 90
        protected void ToTargetMessageOriginalDataElements(MasterCard origin, MCOLA target)
        {
            var stringBuilder = new StringBuilder();

            if (!string.IsNullOrEmpty(origin.ExtendedCore().OriginalDataElements.OriginalMessageTypeIdentifier)
                &&
                origin.ExtendedCore().OriginalDataElements.OriginalMessageTypeIdentifier.Equals
                (MasterCardTransactionConst.MTI.AuthorizationRequest))
            {
                stringBuilder.Append(MCOLATransactionConst.MTI.SaleRequest);
            }
            else
            {
                stringBuilder.Append(origin.ExtendedCore().OriginalDataElements.OriginalMessageTypeIdentifier);
            }

            stringBuilder.Append(origin.ExtendedCore().OriginalDataElements.OriginalSystemsTraceAuditNumber.PadRight(6, '0'));
            stringBuilder.Append(origin.ExtendedCore().OriginalDataElements.OriginalTransmissionDateAndTime.PadRight(10, '0'));
            stringBuilder.Append(origin.ExtendedCore().OriginalDataElements.OriginalAcquiringInstitutionIdCode.PadRight(11, '0'));
            stringBuilder.Append(origin.ExtendedCore().OriginalDataElements.OriginalForwardingInstitutionIdCode.PadRight(11, '0'));

            target.F90OriginalDataElementsDecode(stringBuilder.ToString());
        }

        //// MCOLA DE 91
        protected void ToTargetIssuerFileUpdateCode(MasterCard origin, MCOLA target)
        {
            if (!string.IsNullOrEmpty(origin.ExtendedCore().IssuerFileUpdateCode))
            {
                target.F91FileUpdateCodeDecode(origin.ExtendedCore().IssuerFileUpdateCode);
            }
        }

        protected void FromTargetIssuerFileUpdateCode(MCOLA target, MasterCard origin)
        {
            if (!string.IsNullOrEmpty(target.F91FileUpdateCodeEncode()))
            {
                origin.ExtendedCore().IssuerFileUpdateCode = target.F91FileUpdateCodeEncode();
            }
        }

        //// MCOLA DE 94
        protected void FromTargetServiceIndicator(MCOLA target, MasterCard origin)
        {
            if (target.Mti().StartsWith("08"))
            {
                var serviceIndicator = (MasterCardTransactionConst.ServiceIndicator.ReservedForFutureUse.Default
                                    + MasterCardTransactionConst.ServiceIndicator.AcquirerIssuerIndicator.IssuerOnly
                                    + MasterCardTransactionConst.ServiceIndicator.AddressDataIndicator.AvsNotSupport)
                                    .PadRight(7, '0');
                origin.F94ServiceIndicatorDecode(serviceIndicator);
                return;
            }

            if (!string.IsNullOrEmpty(target.F94Encode()))
            {
                origin.F94ServiceIndicatorDecode(target.F94Encode());
            }
        }

        //// MCOLA DE 96
        protected void ToTargetMessageSecurityCode(MasterCard origin, MCOLA target)
        {
            if (!string.IsNullOrEmpty(origin.ExtendedCore().MessageSecurityCode))
            {
                target.F96Decode(origin.ExtendedCore().MessageSecurityCode.PadRight(16, ' '));
            }
        }

        protected void FromTargetMessageSecurityCode(MCOLA target, MasterCard origin)
        {
            if (target.Mti().StartsWith("03") || target.Mti().StartsWith("08"))
            {
                origin.ExtendedCore().MessageSecurityCode = string.Empty.PadRight(8, '0');
                return;
            }

            if (!string.IsNullOrEmpty(target.F96Encode())
                &&
                target.F96Encode().Length >= 8)
            {
                origin.ExtendedCore().MessageSecurityCode = target.F96Encode().IndexSubstring(0, 8);
            }
        }

        //// MCOLA DE 101
        protected void ToTargetFileName(MasterCard origin, MCOLA target)
        {
            if (origin.Mti().Equals(MasterCardTransactionConst.MTI.IssuerFileUpdateResponse))
            {
                target.F101FileNameDecode(MCOLATransactionConst.FileName.MasterCard);
            }
        }

        //// MCOLA DE 120
        protected void ToTargetRecordData(MasterCard origin, MCOLA target)
        {
            if (target is ISO8583.MCOLA.Transaction.SignOnResponse ||
                target is ISO8583.MCOLA.Transaction.SignOffResponse ||
                target is ISO8583.MCOLA.Transaction.ChangeKeyRequest ||
                target is ISO8583.MCOLA.Transaction.ChangeKeyAdviceRequest)
            {
                if (origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                    MasterCardTransactionConst.AdditionalDataPrivateUse.KeyExchangeBlockData))
                {
                    var de48se11 = (MasterCardDE48SE11Double)origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                            MasterCardTransactionConst.AdditionalDataPrivateUse.KeyExchangeBlockData];
                    target.F120Decode(de48se11.KeyCheckValue.IndexSubstring(0, 4).PadRight(6, '0'));
                }
            }
            else if (origin.Mti().StartsWith("01") || origin.Mti().StartsWith("04"))
            {
                this.ToTargetRecordData01xx04xx(origin, target);
            }
            else if (target is ISO8583.MCOLA.Transaction.AdministrativeAdviceRequest)
            {
                if (!string.IsNullOrEmpty(origin.ExtendedCore().AdviceReasonCode.AdviceReasonCodeSubfield)
                    &&
                    origin.ExtendedCore().AdviceReasonCode.AdviceReasonCodeSubfield.Equals(
                    MasterCardTransactionConst.AdviceReasonCode.AdviceReasonCodeSubfield.McDigitalEnableServiceAdviceIssuer))
                {
                    switch (origin.ExtendedCore().AdviceReasonCode.AdviceDetailCode)
                    {
                        case MasterCardTransactionConst.AdviceReasonCode.AdviceDetailCode.ActivationCodeNotification:
                        case MasterCardTransactionConst.AdviceReasonCode.AdviceDetailCode.TokenizationCompleteNotification:
                        case MasterCardTransactionConst.AdviceReasonCode.AdviceDetailCode.TokenizationEventNotification:
                            this.ToTargetRecordData0620(origin, target);
                            break;
                    }
                }
            }
            else if (target is FileUpdateResponse)
            {
                switch (origin.ExtendedCore().FileName)
                {
                    case MasterCardTransactionConst.FileName.StandInAccountFile:
                        this.ToTargetRecordData0310Mcc102(origin, target);
                        break;
                    case MasterCardTransactionConst.FileName.ElectronicWarningBulletinFile:
                        this.ToTargetRecordData0310Mcc103(origin, target);
                        break;
                    case MasterCardTransactionConst.FileName.PanMappingFile:
                        if (origin.F120Encode().Length == 22)
                        {
                            this.ToTargetRecordData0310Mcc106Pan(origin, target);
                        }
                        else if (origin.F120Encode().Length == 47)
                        {
                            this.ToTargetRecordData0310Mcc106Token(origin, target);
                        }

                        break;
                }
            }
        }

        protected void ToTargetRecordData01xx04xx(MasterCard origin, MCOLA target)
        {
            var modify = false;

            var token = string.Empty;
            var tokenExpirationDate = string.Empty;
            var tokenAssuranceLevel = string.Empty;
            var tokenRequestorId = string.Empty;
            var tokenTransactionId = string.Empty;

            if (origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                MasterCardTransactionConst.AdditionalDataPrivateUse.PanMappingFileInformationTagId))
            {
                modify = true;
                var de48se33 = (MasterCardDE48SE33)origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                        MasterCardTransactionConst.AdditionalDataPrivateUse.PanMappingFileInformationTagId];

                //token = de48se33.AccountNumber.Value.PadRight(19,' ');
                if (!string.IsNullOrEmpty(de48se33.AccountNumber.Value))
                {
                    token = de48se33.AccountNumber.Value.PadRight(19, ' ');
                }
                else
                {
                    token = string.Empty.PadRight(19, ' ');
                }

                //tokenExpirationDate = de48se33.ExpirationDate.Value.PadRight(4, ' ');
                if (!string.IsNullOrEmpty(de48se33.ExpirationDate.Value))
                {
                    tokenExpirationDate = de48se33.ExpirationDate.Value.PadRight(4, ' ');
                }
                else
                {
                    tokenExpirationDate = string.Empty.PadRight(4, ' ');
                }

                //tokenAssuranceLevel = (de48se33.TokenAssuranceLevel.Value != null) ? de48se33.TokenAssuranceLevel.Value.PadRight(2, ' ') : string.Empty.PadRight(2, ' ');
                if (!string.IsNullOrEmpty(de48se33.TokenAssuranceLevel.Value))
                {
                    tokenAssuranceLevel = de48se33.TokenAssuranceLevel.Value.PadRight(2, ' ');
                }
                else
                {
                    tokenAssuranceLevel = string.Empty.PadRight(2, ' ');
                }

                //tokenRequestorId = de48se33.TokenRequestorId.Value.PadRight(11, ' ') ?? string.Empty.PadRight(11, ' ');
                if (!string.IsNullOrEmpty(de48se33.TokenRequestorId.Value))
                {
                    tokenRequestorId = de48se33.TokenRequestorId.Value.PadRight(11, ' ');
                }
                else
                {
                    tokenRequestorId = string.Empty.PadRight(11, ' ');
                }
            }
            else
            {
                token = string.Empty.PadRight(19, ' ');
                tokenExpirationDate = string.Empty.PadRight(4, ' ');
                tokenAssuranceLevel = string.Empty.PadRight(2, ' ');
                tokenRequestorId = string.Empty.PadRight(11, ' ');
            }

            if (origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                MasterCardTransactionConst.AdditionalDataPrivateUse.TokenTransactionIdentifierTagId))
            {
                if (origin.Mti().Equals(MasterCardTransactionConst.MTI.AlternativeNetworkRoutingResponse)
                    ||
                    origin.Mti().Equals(MasterCardTransactionConst.MTI.TransactionHistoryAdviceRequest))
                {
                    modify = true;
                    var de48se30 = (MasterCardDE48SE30)origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                        MasterCardTransactionConst.AdditionalDataPrivateUse.TokenTransactionIdentifierTagId];

                    tokenTransactionId = tokenTransactionId + de48se30.Value.PadRight(64, ' ') ?? "".PadRight(64, ' ');
                }
            }

            if (modify)
            {
                tokenTransactionId = tokenTransactionId.PadRight(64, ' ');
                target.F120Decode(token + tokenExpirationDate + tokenAssuranceLevel + tokenRequestorId + tokenTransactionId);
            }
            else
            {
                return;
            }
        }

        protected void ToTargetRecordData0620(MasterCard origin, MCOLA target)
        {
            if (!string.IsNullOrEmpty(origin.F120Encode()))
            {
                target.F120Decode(origin.F120Encode());
            }
        }

        protected void ToTargetRecordData0310Mcc102(MasterCard origin, MCOLA target)
        {
            if (origin.F120Encode().Length != 64)
            {
                return;
            }

            var stringBuilder = new StringBuilder();

            stringBuilder.Append("PATH");
            stringBuilder.Append("MCC");

            switch (origin.ExtendedCore().IssuerFileUpdateCode)
            {
                case MasterCardTransactionConst.IssuerFileUpdateCode.Add:
                case MasterCardTransactionConst.IssuerFileUpdateCode.Update:
                    stringBuilder.Append(MCOLATransactionConst.RecordData.StandInAccountFile.UpdateCode.AddOrUpdate);
                    break;
                case MasterCardTransactionConst.IssuerFileUpdateCode.Delete:
                    stringBuilder.Append(MCOLATransactionConst.RecordData.StandInAccountFile.UpdateCode.Delete);
                    break;
                case MasterCardTransactionConst.IssuerFileUpdateCode.Inquiry:
                    stringBuilder.Append(MCOLATransactionConst.RecordData.StandInAccountFile.UpdateCode.Inquiry);
                    break;
            }

            stringBuilder.Append(MasterCardTransactionConst.FileName.StandInAccountFile.PadRight(15, ' '));
            stringBuilder.Append(string.Empty.PadRight(5, '0'));
            stringBuilder.Append(origin.F120Encode());

            target.F120Decode(stringBuilder.ToString());
        }

        protected void ToTargetRecordData0310Mcc103(MasterCard origin, MCOLA target)
        {
            if (origin.F120Encode().Length < 56)
            {
                return;
            }

            var stringBuilder = new StringBuilder();

            stringBuilder.Append("PATH");
            stringBuilder.Append("MCC");

            switch (origin.ExtendedCore().IssuerFileUpdateCode)
            {
                case MasterCardTransactionConst.IssuerFileUpdateCode.Add:
                case MasterCardTransactionConst.IssuerFileUpdateCode.Update:
                    stringBuilder.Append(MCOLATransactionConst.RecordData.ElectronicWarningBulletinFile.UpdateCode.AddOrUpdate);
                    break;
                case MasterCardTransactionConst.IssuerFileUpdateCode.Delete:
                    stringBuilder.Append(MCOLATransactionConst.RecordData.ElectronicWarningBulletinFile.UpdateCode.Delete);
                    break;
                case MasterCardTransactionConst.IssuerFileUpdateCode.Inquiry:
                    stringBuilder.Append(MCOLATransactionConst.RecordData.ElectronicWarningBulletinFile.UpdateCode.Inquiry);
                    break;
            }

            stringBuilder.Append(MasterCardTransactionConst.FileName.ElectronicWarningBulletinFile.PadRight(15, ' '));
            stringBuilder.Append(string.Empty.PadRight(5, '0'));
            // Field 1~5
            stringBuilder.Append(origin.F120Encode().IndexSubstring(0, 31));

            // Field 7
            var field7 = origin.F120Encode().Substring(56);
            while (field7.Length >= 7)
            {
                var regionPurgeDate = field7.IndexSubstring(0, 7);
                if (!regionPurgeDate.Equals(string.Empty.PadRight(7, ' ')))
                {
                    stringBuilder.Append(regionPurgeDate);
                }

                field7 = field7.Substring(7);
            }

            target.F120Decode(stringBuilder.ToString().PadRight(101, ' '));
        }

        protected void ToTargetRecordData0310Mcc106Pan(MasterCard origin, MCOLA target)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(MCOLATransactionConst.RecordData.PanMappingFilePan.UpdatedMemo);
            stringBuilder.Append(MCOLATransactionConst.RecordData.PanMappingFilePan.ActCode);
            stringBuilder.Append(origin.F120Encode());

            target.F120Decode(stringBuilder.ToString().PadRight(38, ' '));
        }

        protected void ToTargetRecordData0310Mcc106Token(MasterCard origin, MCOLA target)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(MCOLATransactionConst.RecordData.PanMappingFileToken.UpdatedMemo);
            stringBuilder.Append(MCOLATransactionConst.RecordData.PanMappingFileToken.ActCode);
            stringBuilder.Append(origin.F120Encode());

            target.F120Decode(stringBuilder.ToString().PadRight(63, ' '));
        }

        protected void FromTargetRecordData(MCOLA target, MasterCard origin)
        {
            if (target.Mti().StartsWith("03")
                &&
                !string.IsNullOrEmpty(target.F120Encode()))
            {
                switch (target.F120Encode().Length)
                {
                    case MCOLATransactionConst.RecordData.StandInAccountFileLength:
                        if (target.F120Encode().Length >= 23
                            &&
                            target.F120Encode().IndexSubstring(8, 23).Equals(
                                MCOLATransactionConst.RecordData.StandInAccountFile.ActCode))
                        {
                            this.FromTargetRecordData03xxMcc102(target, origin);
                        }
                        break;
                    case MCOLATransactionConst.RecordData.ElectronicWarningBulletinFileLength:
                        if (target.F120Encode().Length >= 23
                            &&
                            target.F120Encode().IndexSubstring(8, 23).Equals(
                                MCOLATransactionConst.RecordData.ElectronicWarningBulletinFile.ActCode))
                        {
                            this.FromTargetRecordData03xxMcc103(target, origin);
                        }
                        break;
                    case MCOLATransactionConst.RecordData.PanMappingFilePanLength:
                        if (target.F120Encode().Length >= 16
                            &&
                            target.F120Encode().IndexSubstring(0, 1).Equals(
                                MCOLATransactionConst.RecordData.PanMappingFilePan.UpdatedMemo)
                            &&
                            target.F120Encode().IndexSubstring(1, 16).Equals(
                                MCOLATransactionConst.RecordData.PanMappingFilePan.ActCode))
                        {
                            this.FromTargetRecordData03xxMcc106Pan(target, origin);
                        }
                        break;
                    case MCOLATransactionConst.RecordData.PanMappingFileTokenLength:
                        if (target.F120Encode().Length >= 16
                            &&
                            target.F120Encode().IndexSubstring(0, 1).Equals(
                                MCOLATransactionConst.RecordData.PanMappingFileToken.UpdatedMemo)
                            &&
                            target.F120Encode().IndexSubstring(1, 16).Equals(
                                MCOLATransactionConst.RecordData.PanMappingFileToken.ActCode))
                        {
                            this.FromTargetRecordData03xxMcc106Token(target, origin);
                        }
                        break;
                    case MCOLATransactionConst.RecordData.PanMappingFileADLength:
                        if (target.F120Encode().Length >= 16
                            &&
                            target.F120Encode().IndexSubstring(0, 1).Equals(
                                MCOLATransactionConst.RecordData.PanMappingFileAD.UpdatedMemo)
                            &&
                            target.F120Encode().IndexSubstring(1, 16).Equals(
                                MCOLATransactionConst.RecordData.PanMappingFileAD.ActCode))
                        {
                            this.FromTargetRecordData03xxMcc106AD(target, origin);
                        }
                        break;
                }
            }
            else
            {
                if ((target.Mti().StartsWith("01") ||
                     target.Mti().StartsWith("04"))
                    &&
                    !string.IsNullOrEmpty(target.F120Encode()))
                {
                    MasterCardDE48SE30 de48se30 = null;

                    if (origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                        MasterCardTransactionConst.AdditionalDataPrivateUse.TokenTransactionIdentifierTagId))
                    {
                        de48se30 = (MasterCardDE48SE30)origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                            MasterCardTransactionConst.AdditionalDataPrivateUse.TokenTransactionIdentifierTagId];
                    }
                    else
                    {
                        de48se30 = new MasterCardDE48SE30();
                    }

                    de48se30.Value = target.F120Encode().Substring(36, 44);
                    origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[de48se30.Id] = de48se30;

                }
            }

        }

        protected void FromTargetRecordData03xxMcc102(MCOLA target, MasterCard origin)
        {
            origin.ExtendedCore().FileName = MasterCardTransactionConst.FileName.StandInAccountFile;
            origin.ExtendedCore().MessageSecurityCode = string.Empty.PadRight(8, '0');

            if (target.F120Encode().Length >= 47)
            {
                switch (target.F120Encode().IndexSubstring(7, 8))
                {
                    case MCOLATransactionConst.RecordData.StandInAccountFile.UpdateCode.AddOrUpdate:
                        //var de120 = target.F120Encode().Substring(28, 19).Trim() 
                        //            + target.F120Encode().Substring(47);
                        //origin.F120Decode(de120);
                        origin.F120Decode(target.F120Encode().Substring(28));
                        break;
                    default:
                        // Only Field 1 CardNo
                        //origin.F120Decode(target.F120Encode().Substring(28, 19).Trim());
                        origin.F120Decode(target.F120Encode().Substring(28, 19));
                        break;
                }
            }
        }

        protected void FromTargetRecordData03xxMcc103(MCOLA target, MasterCard origin)
        {
            origin.ExtendedCore().FileName = MasterCardTransactionConst.FileName.ElectronicWarningBulletinFile;
            origin.ExtendedCore().MessageSecurityCode = string.Empty.PadRight(8, '0');

            if (target.F120Encode().Length >= 47)
            {
                switch (target.F120Encode().IndexSubstring(7, 8))
                {
                    case MCOLATransactionConst.RecordData.ElectronicWarningBulletinFile.UpdateCode.AddOrUpdate:
                        if (target.F120Encode().Length == 59)
                        {
                            // Only Field 1~5
                            //var de120 = target.F120Encode().Substring(28, 19).Trim() 
                            //            + target.F120Encode().Substring(47);
                            //origin.F120Decode(de120);
                            origin.F120Decode(target.F120Encode().Substring(28));
                        }
                        else if (target.F120Encode().Length > 59)
                        {
                            // Insert Field 6
                            var stringBuilder = new StringBuilder();

                            //stringBuilder.Append(target.F120Encode().IndexSubstring(28, 46).Trim());
                            //stringBuilder.Append(target.F120Encode().IndexSubstring(47, 59));
                            stringBuilder.Append(target.F120Encode().IndexSubstring(28, 59));
                            stringBuilder.Append(string.Empty.PadRight(25, ' '));
                            stringBuilder.Append(target.F120Encode().Substring(59).TrimEnd());

                            origin.F120Decode(stringBuilder.ToString());
                        }

                        break;
                    default:
                        // Only Field 1 CardNo
                        //origin.F120Decode(target.F120Encode().Substring(28, 19).Trim());
                        origin.F120Decode(target.F120Encode().Substring(28, 19));
                        break;
                }
            }
        }

        protected void FromTargetRecordData03xxMcc106Pan(MCOLA target, MasterCard origin)
        {
            origin.ExtendedCore().FileName = MasterCardTransactionConst.FileName.PanMappingFilePan;
            //var de120 = target.F120Encode().Substring(16, 3) 
            //            + target.F120Encode().Substring(19, 19).Trim();
            //origin.F120Decode(de120);
            origin.F120Decode(target.F120Encode().Substring(16));
        }

        protected void FromTargetRecordData03xxMcc106Token(MCOLA target, MasterCard origin)
        {
            origin.ExtendedCore().FileName = MasterCardTransactionConst.FileName.PanMappingFileToken;
            //var de120 = target.F120Encode().Substring(16, 1) 
            //            + target.F120Encode().Substring(17, 19).Trim()
            //            + target.F120Encode().Substring(36, 8) 
            //            + target.F120Encode().Substring(44, 19).Trim();
            //origin.F120Decode(de120);
            origin.F120Decode(target.F120Encode().Substring(16));
        }

        protected void FromTargetRecordData03xxMcc106AD(MCOLA target, MasterCard origin)
        {
            origin.ExtendedCore().FileName = MasterCardTransactionConst.FileName.PanMappingFileToken;
            //var de120 = target.F120Encode().Substring(16, 19).Trim() 
            //            + target.F120Encode().Substring(35, 19).Trim()
            //            + target.F120Encode().Substring(54, 4)
            //            + target.F120Encode().Substring(58, 19).Trim() 
            //            + target.F120Encode().Substring(77, 19).Trim();
            //origin.F120Decode(de120);
            origin.F120Decode(target.F120Encode().Substring(16));
        }

        //// MCOLA DE 123
        protected void ToTargetCryptographicServiceMessage(MasterCard origin, MCOLA target)
        {
            if (target is ISO8583.MCOLA.Transaction.SignOnResponse ||
                target is ISO8583.MCOLA.Transaction.SignOffResponse ||
                target is ISO8583.MCOLA.Transaction.ChangeKeyRequest ||
                target is ISO8583.MCOLA.Transaction.ChangeKeyAdviceRequest)
            {
                if (origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                    MasterCardTransactionConst.AdditionalDataPrivateUse.KeyExchangeBlockData))
                {
                    var de48se11 = (MasterCardDE48SE11Double)origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                            MasterCardTransactionConst.AdditionalDataPrivateUse.KeyExchangeBlockData];
                    target.F123CryptographicServiceMessageDecode(
                        string.Format("CSM(MCL/KSM RCV/BC28 ORG/NCCC KD/{0} CTP/12345678901234 )", de48se11.PinEncryptionKey));
                }
            }
        }

        //// MCOLA DE 124
        protected void ToTargetMemberDefinedData(MasterCard origin, MCOLA target)
        {
            if (!string.IsNullOrEmpty(origin.F124MemberDefinedDataEncode()))
            {
                target.F124Decode(origin.F124MemberDefinedDataEncode());
            }
        }

        protected void FromTargetMemberDefinedData(MCOLA target, MasterCard origin)
        {
            if (!ObjectUtils.IsNull(target.RawVal(FIELD.F_124)))
            {
                target.F124Decode(target.RawVal(FIELD.F_124).ToString());

                if (!string.IsNullOrEmpty(target.F124Encode()))
                {
                    origin.F124MemberDefinedDataDecode(target.F124Encode());
                }
            }
        }

        //// MCOLA DE 127
        protected void ToTargetPosUserData(MasterCard origin, MCOLA target)
        {
            if (target is ISO8583.MCOLA.Transaction.AlternativeNetworkRoutingResponse ||
                target is ISO8583.MCOLA.Transaction.TransactionHistoryAdviceResponse)
            {
                target.Core().SystemTraceAuditNumber = origin.F127Encode();
            }
            //else if (target.Mti().StartsWith("01") ||
            //    target.Mti().StartsWith("04"))
            else if (origin.Mti().StartsWith("01") ||
                origin.Mti().StartsWith("04"))
            {
                var stringBuilder = new StringBuilder();
                if (origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                        MasterCardTransactionConst.AdditionalDataPrivateUse.ElectronicCommerceCertificateQualifyingInformation))
                {
                    var de48se41 = (MasterCardDE48SE41)origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                        MasterCardTransactionConst.AdditionalDataPrivateUse.ElectronicCommerceCertificateQualifyingInformation];
                    var CustId = de48se41.CitizenId.Value.PadRight(10, ' ').IndexSubstring(0, 10);

                    //target.F127PosUserDataDecode(CustId.PadLeft(67, ' ')); 
                    stringBuilder.Append(CustId.PadLeft(67, ' '));
                }
                else
                {
                    string CustId = "";
                    stringBuilder.Append(CustId.PadLeft(67, ' '));
                }

                if (origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                        MasterCardTransactionConst.AdditionalDataPrivateUse.ElectronicCommerceIndicatorsTagId))
                {
                    var de48se42s1s2 = ((MasterCardDE48SE42)origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                        MasterCardTransactionConst.AdditionalDataPrivateUse.ElectronicCommerceIndicatorsTagId]).SecurityLevelIndicator;
                    stringBuilder.Append(de48se42s1s2.PadLeft(2, '0'));

                    target.F127PosUserDataDecode(stringBuilder.ToString());
                }
                else
                {
                    if (stringBuilder.ToString().Trim().Length > 0)
                    {
                        target.F127PosUserDataDecode(stringBuilder.ToString());
                    }
                }
            }
        }

        protected void FromTargetSecurityLvl(MCOLA target, MasterCard origin)
        {
            if (!ObjectUtils.IsNull(target.RawVal(FIELD.F_127)))
            {
                if (target.RawVal(FIELD.F_127).ToString().Length > 68)
                {
                    var de48se42s1s2 = target.RawVal(FIELD.F_127).ToString().Substring(67, 2);
                    if (origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.ContainsKey(
                        MasterCardTransactionConst.AdditionalDataPrivateUse.ElectronicCommerceIndicatorsTagId))
                    {
                        var de48se42 = ((MasterCardDE48SE42)origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[
                           MasterCardTransactionConst.AdditionalDataPrivateUse.ElectronicCommerceIndicatorsTagId]);

                        de48se42.SecurityLevelIndicator = de48se42s1s2;
                        origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags[de48se42.Id] = de48se42;
                    }
                }
            }
        }

        //// Special Rule
        // To MasterCard DE 48 SE 33
        protected void ToOriginDE48SE33(MasterCard origin, MCOLA target)
        {
            MasterCardDE48SE33 de48se33 = new MasterCardDE48SE33();
            var space = " ";
            bool isChange = false;

            de48se33.Subfields.Remove("04");
            de48se33.Subfields.Remove("05");
            de48se33.Subfields.Remove("06");
            de48se33.Subfields.Remove("07");
            de48se33.Subfields.Remove("08");

            //// 48.33.1
            if (!string.IsNullOrEmpty(target.F48Encode())
                &&
                target.F48Encode().Length >= 24)
            {
                var accountNumberIndicator = target.F48Encode().Substring(23, 1);
                if (!space.Equals(accountNumberIndicator))
                {
                    de48se33.AccountNumberIndicator.Value = accountNumberIndicator;
                    isChange = true;
                }
            }

            //// 48.33.2
            if (!string.IsNullOrEmpty(target.F120Encode())
                &&
                target.F120Encode().Length >= 19)
            {
                var accountNumber = target.F120Encode().Substring(0, 19).Trim();
                if (!space.Equals(accountNumber))
                {
                    de48se33.AccountNumber.Value = accountNumber;
                    isChange = true;
                }
            }

            //// 48.33.3
            if (!string.IsNullOrEmpty(target.F120Encode())
                &&
                target.F120Encode().Length >= 23)
            {
                var expirationDate = target.F120Encode().Substring(19, 4);
                if (!space.Equals(expirationDate))
                {
                    de48se33.ExpirationDate.Value = expirationDate;
                    isChange = true;
                }
            }

            if (isChange)
            {
                origin.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags.Add(
                    MasterCardTransactionConst.AdditionalDataPrivateUse.PanMappingFileInformationTagId, de48se33);
            }
        }

    }
}

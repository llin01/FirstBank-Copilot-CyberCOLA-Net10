namespace Com.Cybersoft4u.TM.MasterCard.Transaction
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.Category.Transaction;
    using Com.Cybersoft4u.TM.ISO8583.Exception;

    #endregion

    public class NetworkConnectionStatusRequest : MasterCardRequest, EchoTest
    {
        internal NetworkConnectionStatusRequest()
        {
        }

        internal NetworkConnectionStatusRequest(byte[] message)
            : base(message)
        {
        }

        public override Iso8583 Prepare()
        {
            try
            {
                this.PurgeFields();
                this.ConstructProcess();

                if (this.Core().NullSecondBitmap)
                {
                    this.Val(FIELD.F_1, "0000000000000000");
                }

                //this.Val(FIELD.F_2, this.Core().Pan);
                this.Val(FIELD.F_2, "03382");
                this.Val(FIELD.F_7, this.Core().TransmissionDatetime);
                this.Val(FIELD.F_11, this.Core().SystemTraceAuditNumber);
                //this.Val(FIELD.F_33, this.ExtendedCore().ForwardingInstitutionIdCode);
                this.Val(FIELD.F_33, "003371");
                this.Val(FIELD.F_63, this.F63NetworkDataEncode());
                this.Val(FIELD.F_70, this.Core().NetworkManagementInformationCode);
            }
            catch (Exception e)
            {
                // TODO
                // Log.e(t);
                throw new TransactionPrepareException(e);
            }

            return base.Prepare();
        }

        protected override void ConstructProcess()
        {
            base.ConstructProcess();
            this.Mti(MasterCardTransactionConst.MTI.NetworkManagementRequest);

            //var masterCardGroup = string.Empty.PadRight(19, '0');
            var masterCardGroup = "03382";
            var utcDateTime = TimeUtils.CurrentIso8583UtcDatetime();
            var stan = SerialGenerator.Stan();
            //var customerId = string.Empty.PadRight(6, '0');
            var customerId = "003371";
            var networkManagementInformationCode = MasterCardTransactionConst.NetworkManagementInformationCode.EchoTest;

            this.RawVal(FIELD.F_2, masterCardGroup)
                .RawVal(FIELD.F_7, utcDateTime)
                .RawVal(FIELD.F_11, stan)
                .RawVal(FIELD.F_33, customerId)
                //// Network Data
                //// .RawVal(FIELD.F_63, )
                .RawVal(FIELD.F_70, networkManagementInformationCode);
        }
    }
}

// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="SignOnRequest.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the SignOnRequest type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.MCOLA.Transaction
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583.Exception;
    using Com.Cybersoft4u.TM.ISO8583.Category.Transaction;

    #endregion

    public class SignOffResponse : MCOLAResponse, SignOff
    {
        public override Bytes Encode()
        {
            this.SetFieldWhenNull(FIELD.F_39, () => "00")
                .SetFieldWhenNull(FIELD.F_70, () => "002")
                .CheckNonNullFields(new[] { FIELD.F_7, FIELD.F_11, FIELD.F_39, 
                                            ////FIELD.F_48, 
                                            FIELD.F_70 });
            return base.Encode();
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

                this.Val(FIELD.F_2, this.Core().Pan);
                this.Val(FIELD.F_7, this.Core().TransmissionDatetime);
                this.Val(FIELD.F_11, this.Core().SystemTraceAuditNumber);
                this.Val(FIELD.F_39, this.Core().ResponseCode);

                //// Additional Response Data
                //// this.Val(FIELD.F_44, );

                this.Val(FIELD.F_70, this.Core().NetworkManagementInformationCode);
                this.Val(FIELD.F_120, this.F120Encode());
                this.Val(FIELD.F_123, this.F123CryptographicServiceMessageEncode());
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
            this.Mti("0810");
        }
    }
}

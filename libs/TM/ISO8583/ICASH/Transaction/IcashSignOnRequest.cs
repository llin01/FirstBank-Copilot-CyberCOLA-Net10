// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="SignOnRequest.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the SignOnRequest type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.TM.ISO8583.ICASH.Transaction
{
    #region DEPENDENCY

    using System;
    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583.Category.Attributes;

    #endregion

    [TransactionType("0800")]

    public class IcashSignOnRequest : IcashRequest
    {
        public IcashSignOnRequest(byte[] message = null)
            : base(message)
        {
        }

        protected override void ConstructProcess()
        {
            base.ConstructProcess();
            this.Mti("0800");
            var localDateTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            var stan = SerialGenerator.Stan();
            this.RawVal(FIELD.F_7, localDateTime.Substring(4))
                .RawVal(FIELD.F_11, stan)
                .RawVal(FIELD.F_70, IcashTransactionConst.NetworkManagementInformationCode.SignOn);
        }
    }
}

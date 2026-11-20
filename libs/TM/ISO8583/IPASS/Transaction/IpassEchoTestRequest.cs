// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="IpassEchoTestRequest.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the IpassEchoTestRequest type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.TM.ISO8583.IPASS.Transaction
{
    #region DEPENDENCY

    using System;
    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583.Category.Attributes;

    #endregion

    [TransactionType("0800")]
    public class IpassEchoTestRequest : IpassRequest
    {
        public IpassEchoTestRequest(byte[] message = null)
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
                .RawVal(FIELD.F_13, localDateTime.Substring(0, 4))
                .RawVal(FIELD.F_70, IpassTransactionConst.NetworkManagementInformationCode.EchoTest);
        }
    }
}

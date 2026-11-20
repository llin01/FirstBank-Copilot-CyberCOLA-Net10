// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="SvcsEchoTestRequest.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the SvcsEchoTestRequest type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.TM.ISO8583.SVCS.Transaction
{
    #region DEPENDENCY

    using System;
    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583.Category.Attributes;

    #endregion

    [TransactionType("0800")]
    public class SvcsEchoTestRequest : SvcsRequest
    {
        public SvcsEchoTestRequest(byte[] message = null)
            : base(message)
        {
        }

        protected override void ConstructProcess()
        {
            base.ConstructProcess();
            this.Mti("0800");
            var localDateTime = DateTime.Now.ToString("MMddHHmmss");
            var stan = SerialGenerator.Stan();
            this.RawVal(FIELD.F_7, localDateTime)
                .RawVal(FIELD.F_11, stan)
                .RawVal(FIELD.F_70, SvcsTransactionConst.NetworkManagementInformationCode.EchoTest);
        }
    }
}

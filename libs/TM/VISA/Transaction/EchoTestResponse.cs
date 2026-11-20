// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="EchoTestResponse.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the EchoTestResponse type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.VISA.Transaction
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583.Category.Transaction;

    #endregion

    public class EchoTestResponse : VISAResponse, EchoTest
    {
        protected override void ConstructProcess()
        {
            base.ConstructProcess();
            this.Mti("0810");
        }
    }
}

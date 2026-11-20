// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="SignOnResponse.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the SignOnResponse type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.VISA.Transaction
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583.Category.Transaction;

    #endregion

    public class StinSignOnResponse : VISAResponse, SignOn
    {
        protected override void ConstructProcess()
        {
            base.ConstructProcess();
            this.Mti("0810");
        }
    }
}

// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="SignOffResponse.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the SignOffResponse type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.VISA.Transaction
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583.Category.Transaction;

    #endregion

    public class SignOffResponse : VISAResponse, SignOff
    {
        protected override void ConstructProcess()
        {
            base.ConstructProcess();
            this.Mti("0810");
        }
    }
}

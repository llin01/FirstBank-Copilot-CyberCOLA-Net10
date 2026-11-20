// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="FiscRequest.cs">
// 
// </copyright>
// <summary>
//   Defines the FiscRequest type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.FISC.Transaction
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583.Category.Action;

    #endregion

    public class FiscRequest : FISC, Request
    {
        public FiscRequest()
        {
        }

        public FiscRequest(byte[] message)
            : base(message)
        {
        }
    }
}

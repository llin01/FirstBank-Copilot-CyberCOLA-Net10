// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="FiscResponse.cs">
//
// </copyright>
// <summary>
//   Defines the FiscResponse type.
// </summary>
//
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.FISC.Transaction
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583.Category.Action;

    #endregion

    public class FiscResponse : FISC, Response
    {
        public FiscResponse()
        {
        }

        public FiscResponse(byte[] message)
            : base(message)
        {
        }
    }
}

// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="MCOLAResponse.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the MCOLAResponse type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.MCOLA.Transaction
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583.Category.Action;

    #endregion

    public class MCOLAResponse : MCOLA, Response
    {
        public MCOLAResponse()
        {
        }

        public MCOLAResponse(byte[] message)
            : base(message)
        {
        }
    }
}

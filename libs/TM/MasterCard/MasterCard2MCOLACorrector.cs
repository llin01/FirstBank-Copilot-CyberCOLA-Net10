namespace Com.Cybersoft4u.TM.MasterCard
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.MCOLA;

    #endregion

    public class MasterCard2MCOLACorrector : MessageCorrector<MasterCard, MCOLA>
    {
        public static readonly MasterCard2MCOLACorrector Default = new MasterCard2MCOLACorrector();

        public override void CorrectResponse(MCOLA targetReponse, MasterCard originRequest)
        {
            this.CorrectMti(targetReponse, originRequest);
        }

        public void CorrectMti(MCOLA targetReponse, MasterCard originRequest)
        {
            if (originRequest.Mti().Equals(MasterCardTransactionConst.MTI.AuthorizationRequest)
                &&
                targetReponse.Mti().Equals(MCOLATransactionConst.MTI.PreAuthCompleteResponse))
            {
                targetReponse.Mti(MCOLATransactionConst.MTI.PreAuthResponse);
            }
        }
    }
}

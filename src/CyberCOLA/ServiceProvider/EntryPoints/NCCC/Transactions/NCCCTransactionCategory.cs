namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.NCCC.Transactions
{
    /// <summary>
    /// Enum NCCCTransactionCategory
    /// </summary>
    public enum NCCCTransactionCategory
    {
        /// <summary>
        ///    一般授權交易
        /// </summary>
        NormalPurchase,

        /// <summary>
        ///    一般授權交易調整
        /// </summary>
        NormalPurchaseAdjustment,

        /// <summary>
        ///    退貨交易
        /// </summary>
        Refund,

        /// <summary>
        ///    退貨交易調整
        /// </summary>
        RefundAdjustment,

        /// <summary>
        ///    預先授權交易
        /// </summary>
        PreAuthorization,

        /// <summary>
        ///    預先授權完成交易
        /// </summary>
        PreAuthorizationCompletion,

        /// <summary>
        ///    郵購交易
        /// </summary>
        MailPhoneOrder,

        /// <summary>
        ///    網路授權交易
        /// </summary>
        ElectronicCommerce,

        /// <summary>
        ///    預借現金交易
        /// </summary>
        CashAdvance,

        /// <summary>
        ///    ATM 預借現金交易調整
        /// </summary>
        ATMCashAdvanceAdjustment,

        /// <summary>
        ///    ATM 密碼變更交易
        /// </summary>
        PINChange,

        /// <summary>
        ///    分期付款交易
        /// </summary>
        InstallmentTransaction,

        /// <summary>
        ///    紅利積點抵扣交易
        /// </summary>
        BonusPointsOfRedemption,

        /// <summary>
        ///    小額付款交易
        /// </summary>
        SmallPaymentTransaction,

        /// <summary>
        ///    帳戶確認交易
        /// </summary>
        AccountVerification,

        /// <summary>
        ///    補登交易
        /// </summary>
        ForcePosting,

        /// <summary>
        ///    沖消交易
        /// </summary>
        Reversal,

        /// <summary>
        ///    不符合交易
        /// </summary>
        None
    }
}

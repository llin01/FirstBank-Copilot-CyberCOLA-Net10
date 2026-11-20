namespace Com.Cybersoft4u.TM.ISO8583.Category.Attributes
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.CyberUtils.Utils.Category.Attribute;

    #endregion

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class TransactionAttribute : CategoryAttribute
    {
        public static readonly UndefinedTransactionAttribute Undefined = new UndefinedTransactionAttribute();

        public static readonly Type UndefinedType = Undefined.GetType();

        public class UndefinedTransactionAttribute : TransactionAttribute
        {
        }
    }
}

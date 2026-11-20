namespace Com.Cybersoft4u.TM.ISO8583.Category.Attributes
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.CyberUtils.Utils.Category.Attribute;

    #endregion

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class TransactionTypeAttribute : CategoryAttribute
    {
        public TransactionTypeAttribute(
            string messageTypeIndicator = null,
            string processingCode = null,
            string functionCode = null)
        {
            if (messageTypeIndicator != null)
            {
                ObjectUtils.CheckLength(messageTypeIndicator, 4, "TransactionTypeAttribute initial failed, messageTypeIndicator length error. ProcessingCode length must be 4");
                this.MessageTypeIndicator = messageTypeIndicator;
            }

            if (processingCode != null)
            {
                ObjectUtils.CheckLength(processingCode, 6, "TransactionTypeAttribute initial failed, processingCode length error. ProcessingCode length must be 6");
                this.ProcessingCode = processingCode;
            }

            if (functionCode != null)
            {
                ObjectUtils.CheckLength(functionCode, 3, "TransactionTypeAttribute initial failed, functionCode length error. ProcessingCode length must be 3");
                this.FunctionCode = functionCode;
            }
        }

        public string MessageTypeIndicator { get; protected internal set; }

        public string ProcessingCode { get; protected internal set; }

        public string FunctionCode { get; protected internal set; }

        public string Identity()
        {
            return this.MessageTypeIndicator.Default(string.Empty)
                + this.ProcessingCode.Default(string.Empty)
                   + this.FunctionCode.Default(string.Empty);
        }

        public override string ToString()
        {
            return this.MessageTypeIndicator.Default(string.Empty) + this.ProcessingCode.Default(string.Empty)
                   + this.FunctionCode.Default(string.Empty);
        }
    }
}

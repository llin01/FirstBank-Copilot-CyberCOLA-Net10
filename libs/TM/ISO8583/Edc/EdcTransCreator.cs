namespace Com.Cybersoft4u.TM.ISO8583.Edc
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583.Edc.Transaction;

    #endregion

    public class EdcTransCreator
    {
        public static SaleRequest SaleRequest(byte[] message = null)
        {
            return new SaleRequest(message);
        }

        public static SaleResponse SaleResponse(byte[] message = null)
        {
            return new SaleResponse(message);
        }

        public static RefundRequest RefundRequest(byte[] message = null)
        {
            return new RefundRequest(message);
        }

        public static RefundResponse RefundResponse(byte[] message = null)
        {
            return new RefundResponse(message);
        }

        public static TcUploadRequest TcUploadRequest(byte[] message = null)
        {
            return new TcUploadRequest(message);
        }

        public static TcUploadResponse TcUploadResponse(byte[] message = null)
        {
            return new TcUploadResponse(message);
        }

        public static VoidSaleRequest VoidSaleRequest(byte[] message = null)
        {
            return new VoidSaleRequest(message);
        }

        public static VoidSaleResponse VoidSaleResponse(byte[] message = null)
        {
            return new VoidSaleResponse(message);
        }
    }
}

namespace Com.Cybersoft4u.TM.MasterCard.DE48
{
    #region DEPENDENCY

    using System;

    using System.Collections.Generic;

    using System.Text;

    using Com.Cybersoft4u.CyberUtils.Utils;

    #endregion

    public class OnbehalfService
    {
        //// Subfields
        public string OnbehalfServiceIdentifier = null;
        public string OnbehalfResult1 = null;
        public string OnbehalfResult2 = null;

        public const int Length = 4;
    }

    public class MasterCardDE48SE71 : MasterCardDE48SE
    {
        public List<OnbehalfService> services = new List<OnbehalfService>();

        public MasterCardDE48SE71()
            : base(MasterCardTransactionConst.AdditionalDataPrivateUse.OnbehalfServicesTagId)
        {
        }

        public override String ValueEncode()
        {
            var stringBuilder = new StringBuilder();

            foreach (var onbehalfService in this.services)
            {
                stringBuilder.Append(onbehalfService.OnbehalfServiceIdentifier);
                stringBuilder.Append(onbehalfService.OnbehalfResult1);
                //stringBuilder.Append(onbehalfService.OnbehalfResult2);
            }

            return stringBuilder.ToString();
        }

        public override MasterCardDE48SE ValueDecode()
        {
            this.services = new List<OnbehalfService>();
            var val = this.Value.Copy();

            while (val.Length > 0)
            {
                //var v = val.IndexSubstring(0, OnbehalfService.Length);
                var v = val.IndexSubstring(0, (OnbehalfService.Length < val.Length ? OnbehalfService.Length : val.Length));
                var onbehalfService = new OnbehalfService();

                onbehalfService.OnbehalfServiceIdentifier = v.IndexSubstring(0, 2);
                onbehalfService.OnbehalfResult1 = v.IndexSubstring(2, 3);
                //onbehalfService.OnbehalfResult2 = v.IndexSubstring(3, 4);
                onbehalfService.OnbehalfResult2 = " ";

                this.services.Add(onbehalfService);

                val = val.Substring((OnbehalfService.Length < val.Length ? OnbehalfService.Length : val.Length));
            }

            return this;
        }
    }
}

// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenB4.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TokenB4 type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583.MCOLA;

    #endregion

    public class TokenB4 : Token
    {
        public TokenB4()
            : base(NcccTransactionConst.PosAdditionalData.TokenB4.Id)
        {
            this.PtSrvEntryMde = string.Empty.PadRight(3, ' ');
            this.TermEntryCap = "5";
            this.LastEmvStat = string.Empty.PadRight(1, ' ');
            this.DataSuspect = MCOLATransactionConst.PosAdditionalData.TokenB4.DataSuspect.CamDataCorrect;
            this.ApplPanSeqNum = string.Empty.PadRight(2, '0');
            this.DevInfoCamFlg = string.Empty.PadRight(6, ' ');
            this.RsnOnLineCde = string.Empty.PadRight(4, ' ');
            this.ArqcVerify = string.Empty.PadRight(1, ' ');
            this.UsrFld1 = string.Empty.PadRight(1, ' ');
        }

        public string PtSrvEntryMde { get; set; }

        public string TermEntryCap { get; set; }

        public string LastEmvStat { get; set; }

        public string DataSuspect { get; set; }

        public string ApplPanSeqNum { get; set; }

        public string DevInfoCamFlg { get; set; }

        public string RsnOnLineCde { get; set; }

        public string ArqcVerify { get; set; }

        public string UsrFld1 { get; set; }

        public override string DataEncode()
        {
            return this.PtSrvEntryMde + this.TermEntryCap + this.LastEmvStat + this.DataSuspect + this.ApplPanSeqNum
                   + this.DevInfoCamFlg + this.RsnOnLineCde + this.ArqcVerify + this.UsrFld1;
        }

        public override Token DataDecode(string data)
        {
            this.PtSrvEntryMde = data.Substring(0, 3);
            this.TermEntryCap = data.Substring(3, 1);
            this.LastEmvStat = data.Substring(4, 1);
            this.DataSuspect = data.Substring(5, 1);
            this.ApplPanSeqNum = data.Substring(6, 2);
            this.DevInfoCamFlg = data.Substring(8, 6);
            this.RsnOnLineCde = data.Substring(14, 4);
            this.ArqcVerify = data.Substring(18, 1);
            this.UsrFld1 = data.Substring(19, 1);
            return this;
        }
    }
}

// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="VISA2MCOLAConverter.cs">
//   Author: Yeti, Kaven
// </copyright>
// <summary>
//   Defines the VISA2MCOLAConverter type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.VISA
{
    #region DEPENDENCY

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.MCOLA;
    using Com.Cybersoft4u.TM.VISA.Converter;
    using Com.Cybersoft4u.TM.VISA.Converter.InField;
    using Com.Cybersoft4u.TM.VISA.Converter.OutField;

    #endregion

    public class VISA2MCOLAConverter : MessageConverter<VISA, MCOLA>
    {
        public static readonly VISA2MCOLAConverter Default = new VISA2MCOLAConverter();

        public override MCOLA ToTarget(VISA origin)
        {
            var target = new MCOLA();

            MtiConverter.ToTargetMessageTransactionIndicator(origin, target);
            InField_Header.ToTargetHeader(origin, target);

            if (!ObjectUtils.IsNull(origin.RejectHeader()))
            {
                InField_Filler.InFieldRejectHeader14(origin, target);
            }
            else
            {
                switch (origin.Mti())
                {
                    case "0100":
                    case "0200":
                        InField_Filler.InField0100(origin, target);
                        break;
                    case "0101":
                        InField_Filler.InField0101(origin, target);
                        break;
                    case "0120":
                    case "0220":
                        InField_Filler.InField0120(origin, target);
                        break;
                    case "0400":
                        InField_Filler.InField0400(origin, target);
                        break;
                    case "0420":
                        InField_Filler.InField0420(origin, target);
                        break;
                    case "0312":
                        InField_Filler.InField0312(origin, target);
                        break;
                    case "0800":
                        InField_Filler.InField0800(origin, target);
                        break;
                    case "0810":
                        InField_Filler.InField0810(origin, target);
                        break;
                    case "0600":
                        InField_Filler.InField0600(origin, target);
                        break;
                    case "0620":
                        InField_Filler.InField0620(origin, target);
                        break;
                    default:
                        break;
                }
            }

            return target;
        }

        public override VISA ToOrigin(MCOLA target)
        {
            var origin = new VISA();

            MtiConverter.ToOriginMessageTransactionIndicator(target, origin);

            switch (origin.Mti())
            {
                case "0110":
                case "0210":
                    OutField_Filler.OutField0110(origin, target);
                    break;
                case "0130":
                case "0230":
                    OutField_Filler.OutField0130(origin, target);
                    break;
                case "0410":
                    OutField_Filler.OutField0410(origin, target);
                    break;
                case "0430":
                    OutField_Filler.OutField0430(origin, target);
                    break;
                case "0302":
                    OutField_Filler.OutField0302(origin, target);
                    break;
                case "0800":
                    OutField_Filler.OutField0800(origin, target);
                    break;
                case "0810":
                    OutField_Filler.OutField0810(origin, target);
                    break;
                case "0610":
                    OutField_Filler.OutField0610(origin, target);
                    break;
                case "0630":
                    OutField_Filler.OutField0630(origin, target);
                    break;
            }

            return origin;
        }
    }
}

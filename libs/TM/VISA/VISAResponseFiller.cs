// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="VISAResponseFiller.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the VISAResponseFiller type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.VISA
{
    #region DEPENDENCY

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;
    using System.Text;

    #endregion

    public class VISAResponseFiller
    {
        public static void DoFill(VISA response, VISA request)
        {
            FillVisaHeader(response, request);
            switch (response.Mti())
            {
                case "0110":
                case "0210":
                    if (request.Mti() == "0100" || request.Mti() == "0101")
                    {
                        response.Mti("0110");
                    }
                    else
                    {
                        response.Mti("0210");
                    }
                    Response0110Process(response, request);
                    break;
                case "0130":
                case "0230":
                    if (request.Mti() == "0120")
                    {
                        response.Mti("0130");
                    }
                    else
                    {
                        response.Mti("0230");
                    }
                    Response0130Process(response, request);
                    break;
                case "0410":
                    Response0410Process(response, request);
                    break;
                case "0430":
                    Response0430Process(response, request);
                    break;
                case "0332":
                    Response0332Process(response, request);
                    break;
                case "0810":
                    Response0810Process(response, request);
                    break;
                case "0610":
                    Response0610Process(response, request);
                    break;
                case "0630":
                    Response0630Process(response, request);
                    break;
            }
        }

        private static void FillVisaHeader(VISA response, VISA request)
        {
            var respHeader = response.Header();
            var reqHeader = request.Header();
            respHeader.DestinationId = reqHeader.SourceId;
            respHeader.SourceId = reqHeader.DestinationId;
            respHeader.RoundTripControlInformation = reqHeader.RoundTripControlInformation;
            respHeader.BaseIFlags = reqHeader.BaseIFlags;
            respHeader.MessageStatusFlags = reqHeader.MessageStatusFlags;
            respHeader.BatchNumber = reqHeader.BatchNumber;
            respHeader.Reserved = reqHeader.Reserved;
            respHeader.UserInformation = reqHeader.UserInformation;
        }

        private static void FillResponseField(FIELD field, VISA response, VISA request)
        {
            response.Core().Field(field, request.Core().Field(field));
        }

        private static void Response0110Process(VISA response, VISA request)
        {
            FillResponseField(FIELD.F_2, response, request);
            FillResponseField(FIELD.F_3, response, request);
            FillResponseField(FIELD.F_4, response, request);
            // For VISA 2018 BER Certified Error
            //FillResponseField(FIELD.F_6, response, request);
            FillResponseField(FIELD.F_7, response, request);
            // For VISA 2018 BER Certified Error
            //FillResponseField(FIELD.F_10, response, request);
            FillResponseField(FIELD.F_11, response, request);
            FillResponseField(FIELD.F_15, response, request);
            FillResponseField(FIELD.F_19, response, request);
            FillResponseField(FIELD.F_20, response, request);
            FillResponseField(FIELD.F_23, response, request);
            FillResponseField(FIELD.F_25, response, request);
            FillResponseField(FIELD.F_32, response, request);
            FillResponseField(FIELD.F_37, response, request);
            FillResponseField(FIELD.F_41, response, request);
            FillResponseField(FIELD.F_42, response, request);

            // DE 44 fill
            var reqDe44 = request.RawVal(FIELD.F_44) as string;
            var respDe44 = response.RawVal(FIELD.F_44) as string;
            if (!ObjectUtils.IsNull(respDe44))
            {
                if (ObjectUtils.IsNull(reqDe44))
                {
                    reqDe44 = string.Empty;
                }

                var f44 = string.Empty;
                var reqDe44Length = reqDe44.Length;
                reqDe44 = reqDe44.PadRight(24, ' ');
                respDe44 = respDe44.PadRight(24, ' ');

                var f44_1 = reqDe44.Substring(0, 1);
                var f44_2 = reqDe44.Substring(1, 1);
                var f44_3 = reqDe44.Substring(2, 1);
                var f44_4 = reqDe44.Substring(3, 1);
                var f44_5 = reqDe44.Substring(4, 1);
                var f44_6 = reqDe44.Substring(5, 2);
                var f44_7 = reqDe44.Substring(7, 1);
                var f44_8 = reqDe44.Substring(8, 1);
                var f44_9 = reqDe44.Substring(9, 1);
                var f44_10 = reqDe44.Substring(10, 1);
                var f44_11 = reqDe44.Substring(11, 2);
                var f44_12 = reqDe44.Substring(13, 1);
                var f44_13 = reqDe44.Substring(14, 1);
                var f44_14 = reqDe44.Substring(15, 4);
                var f44_15 = reqDe44.Substring(19, 4);
                var f44_16 = reqDe44.Substring(23, 1);

                f44_5 = respDe44.Substring(4, 1);
                f44_8 = respDe44.Substring(8, 1);
                f44_10 = respDe44.Substring(10, 1);
                f44_13 = respDe44.Substring(14, 1);

                f44 = f44_1 + f44_2 + f44_3 + f44_4 + f44_5 + f44_6 + f44_7 + f44_8 + f44_9 + f44_10
                    + f44_11 + f44_12 + f44_13 + f44_14 + f44_15 + f44_16;

                if (f44.TrimEnd().Length >= reqDe44Length)
                {
                    response.Val(FIELD.F_44, f44.TrimEnd());
                }
                else
                {
                    response.Val(FIELD.F_44, f44.Substring(0, reqDe44Length));
                }
            }
            else
            {
                FillResponseField(FIELD.F_44, response, request);
            }

            if (response.RawVal(FIELD.F_48) != null)
            {
                var respDe48 = TM.VISA.SubFieldCodec.F48.F_48Field.Default.Decode((byte[])response.RawVal(FIELD.F_48));
                if (respDe48.Substring(0, 4) != "#TSP")
                {
                    FillResponseField(FIELD.F_48, response, request);
                }
            }
            else
            {
                FillResponseField(FIELD.F_48, response, request);
            }

            FillResponseField(FIELD.F_49, response, request);
            // For VISA 2018 BER Certified Error
            //FillResponseField(FIELD.F_51, response, request);

            // DE 54 fill for account type
            var reqDe3 = request.RawVal(FIELD.F_3) as string;
            var respDe54 = response.RawVal(FIELD.F_54) as string;
            if ((!ObjectUtils.IsNull(respDe54)) && (!ObjectUtils.IsNull(reqDe3)))
            {
                var reqDe3AccountType = reqDe3.Substring(2, 2);
                var f54 = string.Empty;
                var f54sb = new StringBuilder();
                for (int i = 0; i < respDe54.Length; i = i + 20)
                {
                    f54sb.Append(reqDe3AccountType);
                    f54sb.Append(respDe54.Substring((i + 2), 18));
                }

                response.Val(FIELD.F_54, f54sb.ToString().TrimEnd());
            }

            // For VISA 2018 BER Certified Error
            //FillResponseField(FIELD.F_60, response, request);
            FillResponseField(FIELD.F_62, response, request);
            FillResponseField(FIELD.F_63, response, request);
            FillResponseField(FIELD.F_73, response, request);
            FillResponseField(FIELD.F_91, response, request);
            FillResponseField(FIELD.F_102, response, request);
            FillResponseField(FIELD.F_103, response, request);
            FillResponseField(FIELD.F_104, response, request);
            FillResponseField(FIELD.F_115, response, request);
            FillResponseField(FIELD.F_117, response, request);
            FillResponseField(FIELD.F_118, response, request);
            FillResponseField(FIELD.F_121, response, request);
            // FillResponseField(FIELD.F_123, response, request);
            FillResponseField(FIELD.F_126, response, request);
        }

        private static void Response0130Process(VISA response, VISA request)
        {
            FillResponseField(FIELD.F_2, response, request);
            FillResponseField(FIELD.F_3, response, request);
            FillResponseField(FIELD.F_4, response, request);
            FillResponseField(FIELD.F_7, response, request);
            FillResponseField(FIELD.F_11, response, request);
            FillResponseField(FIELD.F_15, response, request);
            FillResponseField(FIELD.F_19, response, request);
            FillResponseField(FIELD.F_23, response, request);
            FillResponseField(FIELD.F_25, response, request);
            FillResponseField(FIELD.F_32, response, request);
            FillResponseField(FIELD.F_37, response, request);
            FillResponseField(FIELD.F_41, response, request);
            FillResponseField(FIELD.F_42, response, request);

            // DE 44 fill
            var reqDe44 = request.RawVal(FIELD.F_44) as string;
            var respDe44 = response.RawVal(FIELD.F_44) as string;
            if (!ObjectUtils.IsNull(respDe44))
            {
                if (ObjectUtils.IsNull(reqDe44))
                {
                    reqDe44 = string.Empty;
                }

                var f44 = string.Empty;
                var reqDe44Length = reqDe44.Length;
                reqDe44 = reqDe44.PadRight(24, ' ');
                respDe44 = respDe44.PadRight(24, ' ');

                var f44_1 = reqDe44.Substring(0, 1);
                var f44_2 = reqDe44.Substring(1, 1);
                var f44_3 = reqDe44.Substring(2, 1);
                var f44_4 = reqDe44.Substring(3, 1);
                var f44_5 = reqDe44.Substring(4, 1);
                var f44_6 = reqDe44.Substring(5, 2);
                var f44_7 = reqDe44.Substring(7, 1);
                var f44_8 = reqDe44.Substring(8, 1);
                var f44_9 = reqDe44.Substring(9, 1);
                var f44_10 = reqDe44.Substring(10, 1);
                var f44_11 = reqDe44.Substring(11, 2);
                var f44_12 = reqDe44.Substring(13, 1);
                var f44_13 = reqDe44.Substring(14, 1);
                var f44_14 = reqDe44.Substring(15, 4);
                var f44_15 = reqDe44.Substring(19, 4);
                var f44_16 = reqDe44.Substring(23, 1);

                f44_5 = respDe44.Substring(4, 1);
                f44_8 = respDe44.Substring(8, 1);
                f44_10 = respDe44.Substring(10, 1);
                f44_13 = respDe44.Substring(14, 1);

                f44 = f44_1 + f44_2 + f44_3 + f44_4 + f44_5 + f44_6 + f44_7 + f44_8 + f44_9 + f44_10
                    + f44_11 + f44_12 + f44_13 + f44_14 + f44_15 + f44_16;

                if (f44.TrimEnd().Length >= reqDe44Length)
                {
                    response.Val(FIELD.F_44, f44.TrimEnd());
                }
                else
                {
                    response.Val(FIELD.F_44, f44.Substring(0, reqDe44Length));
                }
            }
            else
            {
                FillResponseField(FIELD.F_44, response, request);
            }

            if (response.RawVal(FIELD.F_48) != null)
            {
                var respDe48 = TM.VISA.SubFieldCodec.F48.F_48Field.Default.Decode((byte[])response.RawVal(FIELD.F_48));
                if (respDe48.Substring(0, 4) != "#TSP")
                {
                    FillResponseField(FIELD.F_48, response, request);
                }
            }
            else
            {
                FillResponseField(FIELD.F_48, response, request);
            }

            FillResponseField(FIELD.F_49, response, request);
            // For VISA 2018 BER Certified Error
            //FillResponseField(FIELD.F_60, response, request);
            FillResponseField(FIELD.F_62, response, request);
            FillResponseField(FIELD.F_63, response, request);
            FillResponseField(FIELD.F_100, response, request);
            FillResponseField(FIELD.F_115, response, request);
            FillResponseField(FIELD.F_116, response, request);
            FillResponseField(FIELD.F_117, response, request);
            FillResponseField(FIELD.F_118, response, request);
        }

        private static void Response0410Process(VISA response, VISA request)
        {
            FillResponseField(FIELD.F_2, response, request);
            FillResponseField(FIELD.F_3, response, request);
            FillResponseField(FIELD.F_4, response, request);
            // For VISA 2018 BER Certified Error
            //FillResponseField(FIELD.F_6, response, request);
            FillResponseField(FIELD.F_7, response, request);
            // For VISA 2018 BER Certified Error
            //FillResponseField(FIELD.F_10, response, request);
            FillResponseField(FIELD.F_11, response, request);
            FillResponseField(FIELD.F_19, response, request);
            FillResponseField(FIELD.F_23, response, request);
            FillResponseField(FIELD.F_25, response, request);
            FillResponseField(FIELD.F_32, response, request);
            FillResponseField(FIELD.F_37, response, request);
            FillResponseField(FIELD.F_41, response, request);
            FillResponseField(FIELD.F_42, response, request);

            // DE 44 fill
            var reqDe44 = request.RawVal(FIELD.F_44) as string;
            var respDe44 = response.RawVal(FIELD.F_44) as string;
            if (!ObjectUtils.IsNull(respDe44))
            {
                if (ObjectUtils.IsNull(reqDe44))
                {
                    reqDe44 = string.Empty;
                }

                var f44 = string.Empty;
                var reqDe44Length = reqDe44.Length;
                reqDe44 = reqDe44.PadRight(24, ' ');
                respDe44 = respDe44.PadRight(24, ' ');

                var f44_1 = reqDe44.Substring(0, 1);
                var f44_2 = reqDe44.Substring(1, 1);
                var f44_3 = reqDe44.Substring(2, 1);
                var f44_4 = reqDe44.Substring(3, 1);
                var f44_5 = reqDe44.Substring(4, 1);
                var f44_6 = reqDe44.Substring(5, 2);
                var f44_7 = reqDe44.Substring(7, 1);
                var f44_8 = reqDe44.Substring(8, 1);
                var f44_9 = reqDe44.Substring(9, 1);
                var f44_10 = reqDe44.Substring(10, 1);
                var f44_11 = reqDe44.Substring(11, 2);
                var f44_12 = reqDe44.Substring(13, 1);
                var f44_13 = reqDe44.Substring(14, 1);
                var f44_14 = reqDe44.Substring(15, 4);
                var f44_15 = reqDe44.Substring(19, 4);
                var f44_16 = reqDe44.Substring(23, 1);

                f44_5 = respDe44.Substring(4, 1);
                f44_8 = respDe44.Substring(8, 1);
                f44_10 = respDe44.Substring(10, 1);
                f44_13 = respDe44.Substring(14, 1);

                f44 = f44_1 + f44_2 + f44_3 + f44_4 + f44_5 + f44_6 + f44_7 + f44_8 + f44_9 + f44_10
                    + f44_11 + f44_12 + f44_13 + f44_14 + f44_15 + f44_16;

                if (f44.TrimEnd().Length >= reqDe44Length)
                {
                    response.Val(FIELD.F_44, f44.TrimEnd());
                }
                else
                {
                    response.Val(FIELD.F_44, f44.Substring(0, reqDe44Length));
                }
            }
            else
            {
                FillResponseField(FIELD.F_44, response, request);
            }

            if (response.RawVal(FIELD.F_48) != null)
            {
                var respDe48 = TM.VISA.SubFieldCodec.F48.F_48Field.Default.Decode((byte[])response.RawVal(FIELD.F_48));
                if (respDe48.Substring(0, 4) != "#TSP")
                {
                    FillResponseField(FIELD.F_48, response, request);
                }
            }
            else
            {
                FillResponseField(FIELD.F_48, response, request);
            }

            FillResponseField(FIELD.F_49, response, request);
            // For VISA 2018 BER Certified Error
            //FillResponseField(FIELD.F_51, response, request);
            FillResponseField(FIELD.F_54, response, request);
            FillResponseField(FIELD.F_55, response, request);
            // For VISA 2018 BER Certified Error
            //FillResponseField(FIELD.F_60, response, request);
            FillResponseField(FIELD.F_62, response, request);
            FillResponseField(FIELD.F_63, response, request);
            FillResponseField(FIELD.F_90, response, request);
            FillResponseField(FIELD.F_95, response, request);
            FillResponseField(FIELD.F_102, response, request);
            FillResponseField(FIELD.F_103, response, request);
            FillResponseField(FIELD.F_104, response, request);
            FillResponseField(FIELD.F_117, response, request);
            FillResponseField(FIELD.F_118, response, request);
            FillResponseField(FIELD.F_121, response, request);
            // FillResponseField(FIELD.F_123, response, request);
            FillResponseField(FIELD.F_126, response, request);
        }

        private static void Response0430Process(VISA response, VISA request)
        {
            FillResponseField(FIELD.F_2, response, request);
            FillResponseField(FIELD.F_3, response, request);
            FillResponseField(FIELD.F_4, response, request);
            FillResponseField(FIELD.F_7, response, request);
            FillResponseField(FIELD.F_11, response, request);
            FillResponseField(FIELD.F_19, response, request);
            FillResponseField(FIELD.F_23, response, request);
            FillResponseField(FIELD.F_25, response, request);
            FillResponseField(FIELD.F_32, response, request);
            FillResponseField(FIELD.F_37, response, request);
            FillResponseField(FIELD.F_41, response, request);
            FillResponseField(FIELD.F_42, response, request);

            // DE 44 fill
            var reqDe44 = request.RawVal(FIELD.F_44) as string;
            var respDe44 = response.RawVal(FIELD.F_44) as string;
            if (!ObjectUtils.IsNull(respDe44))
            {
                if (ObjectUtils.IsNull(reqDe44))
                {
                    reqDe44 = string.Empty;
                }

                var f44 = string.Empty;
                var reqDe44Length = reqDe44.Length;
                reqDe44 = reqDe44.PadRight(24, ' ');
                respDe44 = respDe44.PadRight(24, ' ');

                var f44_1 = reqDe44.Substring(0, 1);
                var f44_2 = reqDe44.Substring(1, 1);
                var f44_3 = reqDe44.Substring(2, 1);
                var f44_4 = reqDe44.Substring(3, 1);
                var f44_5 = reqDe44.Substring(4, 1);
                var f44_6 = reqDe44.Substring(5, 2);
                var f44_7 = reqDe44.Substring(7, 1);
                var f44_8 = reqDe44.Substring(8, 1);
                var f44_9 = reqDe44.Substring(9, 1);
                var f44_10 = reqDe44.Substring(10, 1);
                var f44_11 = reqDe44.Substring(11, 2);
                var f44_12 = reqDe44.Substring(13, 1);
                var f44_13 = reqDe44.Substring(14, 1);
                var f44_14 = reqDe44.Substring(15, 4);
                var f44_15 = reqDe44.Substring(19, 4);
                var f44_16 = reqDe44.Substring(23, 1);

                f44_5 = respDe44.Substring(4, 1);
                f44_8 = respDe44.Substring(8, 1);
                f44_10 = respDe44.Substring(10, 1);
                f44_13 = respDe44.Substring(14, 1);

                f44 = f44_1 + f44_2 + f44_3 + f44_4 + f44_5 + f44_6 + f44_7 + f44_8 + f44_9 + f44_10
                    + f44_11 + f44_12 + f44_13 + f44_14 + f44_15 + f44_16;

                if (f44.TrimEnd().Length >= reqDe44Length)
                {
                    response.Val(FIELD.F_44, f44.TrimEnd());
                }
                else
                {
                    response.Val(FIELD.F_44, f44.Substring(0, reqDe44Length));
                }
            }
            else
            {
                FillResponseField(FIELD.F_44, response, request);
            }


            if (response.RawVal(FIELD.F_48) != null)
            {
                var respDe48 = TM.VISA.SubFieldCodec.F48.F_48Field.Default.Decode((byte[])response.RawVal(FIELD.F_48));
                if (respDe48.Substring(0, 4) != "#TSP")
                {
                    FillResponseField(FIELD.F_48, response, request);
                }
            }
            else
            {
                FillResponseField(FIELD.F_48, response, request);
            }

            FillResponseField(FIELD.F_54, response, request);
            // For VISA 2018 BER Certified Error
            //FillResponseField(FIELD.F_60, response, request);
            FillResponseField(FIELD.F_62, response, request);
            FillResponseField(FIELD.F_63, response, request);
            FillResponseField(FIELD.F_90, response, request);
            FillResponseField(FIELD.F_95, response, request);
            FillResponseField(FIELD.F_102, response, request);
            FillResponseField(FIELD.F_117, response, request);
            FillResponseField(FIELD.F_118, response, request);
            FillResponseField(FIELD.F_126, response, request);
        }

        private static void Response0332Process(VISA response, VISA request)
        {
            FillResponseField(FIELD.F_2, response, request);
            FillResponseField(FIELD.F_7, response, request);
            FillResponseField(FIELD.F_11, response, request);
            FillResponseField(FIELD.F_32, response, request);
            FillResponseField(FIELD.F_37, response, request);
            FillResponseField(FIELD.F_63, response, request);
        }

        private static void Response0810Process(VISA response, VISA request)
        {
            FillResponseField(FIELD.F_7, response, request);
            FillResponseField(FIELD.F_11, response, request);
            FillResponseField(FIELD.F_37, response, request);
            FillResponseField(FIELD.F_48, response, request);
            FillResponseField(FIELD.F_70, response, request);
        }

        private static void Response0610Process(VISA response, VISA request)
        {
            FillResponseField(FIELD.F_2, response, request);
            FillResponseField(FIELD.F_7, response, request);
            FillResponseField(FIELD.F_11, response, request);
            FillResponseField(FIELD.F_15, response, request);
            FillResponseField(FIELD.F_37, response, request);
            FillResponseField(FIELD.F_39, response, request);
            FillResponseField(FIELD.F_62, response, request);
            FillResponseField(FIELD.F_63, response, request);
            FillResponseField(FIELD.F_70, response, request);
            FillResponseField(FIELD.F_115, response, request);
        }

        private static void Response0630Process(VISA response, VISA request)
        {
            FillResponseField(FIELD.F_2, response, request);
            FillResponseField(FIELD.F_7, response, request);
            FillResponseField(FIELD.F_11, response, request);
            FillResponseField(FIELD.F_15, response, request);
            FillResponseField(FIELD.F_37, response, request);
            FillResponseField(FIELD.F_62, response, request);
            FillResponseField(FIELD.F_63, response, request);
            FillResponseField(FIELD.F_70, response, request);
            FillResponseField(FIELD.F_115, response, request);
        }
    }
}

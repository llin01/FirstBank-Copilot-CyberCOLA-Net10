using System.Collections.Generic;

namespace Com.Cybersoft4u.CommonLib.Resource
{
    /// <summary>
    /// Provides static data dictionaries for system-wide constants including currency decimal places and HSM response code mappings.
    /// </summary>
    public static class SystemStaticData
    {

        internal static Dictionary<string, int> CurrencyDecimalNumber = new Dictionary<string, int>()
        {
            { "108", 0 }, { "974", 0 }, { "152", 0 }, { "262", 0 }, { "324", 0 }, { "392", 0 }, { "174", 0 },
            { "410", 0 }, { "600", 0 }, { "646", 0 }, { "800", 0 }, { "704", 0 }, { "548", 0 }, { "950", 0 },
            { "952", 0 }, { "953", 0 }, { "048", 3 }, { "368", 3 }, { "400", 3 }, { "414", 3 }, { "434", 3 },
            { "512", 3 }, { "788", 3 }, { "352", 0 }
        };
        internal static readonly Dictionary<string, string> HSMResponseCodeMapping = new Dictionary<string, string>()
                                                                                  {
                                                                                      { "00", "Success (No error)"},
                                                                                      { "01", "Fail"},
                                                                                      { "30", "HSM communication error"}
                                                                                  };
        internal static readonly Dictionary<string, string> HSMReturnCodeMapping = new Dictionary<string, string>()
                                                                                 {
                                                                                    { "0000", "Success (No error)"},
                                                                                    { "0303", "SUIP error"       },
                                                                                    { "0304", "HSM error"        }
                                                                                 };
        internal static readonly Dictionary<string, string> HSMReasonCodeMapping = new Dictionary<string, string>()
                                                                                 {
                                                                                    { "0000", "Success (No error)"                   },
                                                                                    { "7000", "Command not licensed"                 },
                                                                                    { "8000", "Get key from key store fail"          },
                                                                                    { "8001", "Set key into key store fail"          },
                                                                                    { "8005", "Key inappropriate length for function"},
                                                                                    { "8020", "Invalid input data (invalid format, invalid characters, or not enough data provided)"},
                                                                                    { "9000", "Invalid return function code from HSM"},
                                                                                    { "9001", "PIN/PVV/CVV/MAC/etc verify fail"      },
                                                                                    { "9020", "Invalid receive data from HSM (invalid format, invalid characters, or not enough data provided)"},
                                                                                    { "2999", "Other error"                          },
                                                                                    { "KR01", "Verification failure"                 },
                                                                                    { "CZ01", "Verification failure"                 },
                                                                                    { "2F15", "Invalid input data"                   },
                                                                                    { "2H15", "Invalid input data"                   },
                                                                                    { "2J15", "Invalid input data"                   },
                                                                                    { "2L15", "Invalid input data"                   },
                                                                                    { "3J15", "Invalid input data"                   },
                                                                                    { "CD15", "Invalid input data"                   },
                                                                                    { "CX15", "Invalid input data"                   },
                                                                                    { "CZ15", "Invalid input data"                   },
                                                                                    { "DH15", "Invalid input data"                   },
                                                                                    { "FB15", "Invalid input data"                   },
                                                                                    { "GD15", "Invalid input data"                   },
                                                                                    { "IB15", "Invalid input data"                   },
                                                                                    { "JF15", "Invalid input data"                   },
                                                                                    { "KR15", "Invalid input data"                   },
                                                                                    { "YB15", "Invalid input data"                   },
                                                                                    { "CD24", "PIN is fewer than 4 or more than 12 digits in length"  },
                                                                                    { "DH24", "PIN is fewer than 4 or more than 12 digits in length"  },
                                                                                    { "JF24", "PIN is fewer than 4 or more than 12 digits in length"  },
                                                                                    { "KR80", "Data length error"                    }
                                                                                 };

    }
}

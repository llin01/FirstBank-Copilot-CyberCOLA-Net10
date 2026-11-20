using System;

namespace Com.Cybersoft4u.CommonLib.SMS;

/// <summary>
/// SMS service transmission parameter model.
/// </summary>
public class SMSParameterModel
{
    /// <summary>
    /// ACS system code: ACS.
    /// </summary>
    public string SystemId { get; set; } = "ACS";

    /// <summary>
    /// ACS system sequence number.
    /// </summary>
    public string SourceId { get; set; } = "";

    /// <summary>
    /// SMS provider/telecom operator code.
    /// </summary>
    public string SmsProvider { get; set; } = "Mitake";

    /// <summary>
    /// Mobile phone number.
    /// </summary>
    public string CellPhone { get; set; } = null!;

    /// <summary>
    /// SMS subject line.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// SMS message body content.
    /// </summary>
    public string MessageBody { get; set; } = null!;

    /// <summary>
    /// Scheduled send time.
    /// </summary>
    public DateTime? BookingTime { get; set; } = null;

    /// <summary>
    /// SMS validity time.
    /// </summary>
    public DateTime? ValidTime { get; set; } = null;

    /// <summary>
    /// Cardholder customer number (ID number/serial).
    /// </summary>
    public string CUST_NBR { get; set; } = "";
}


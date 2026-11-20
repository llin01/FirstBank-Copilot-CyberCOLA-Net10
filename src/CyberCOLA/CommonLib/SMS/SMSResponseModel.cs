using System;

namespace Com.Cybersoft4u.CommonLib.SMS;

/// <summary>
/// SMS service send response data model.
/// </summary>
public class SMSResponseModel
{
    /// <summary>
    /// Source system code.
    /// </summary>
    public string SYS_ID { get; set; } = null!;

    /// <summary>
    /// Source system sequence number.
    /// </summary>
    public string SOURCE_ID { get; set; } = null!;

    /// <summary>
    /// SMS provider/telecom operator sequence number.
    /// </summary>
    public string MSG_ID { get; set; } = null!;

    /// <summary>
    /// SMS recipient mobile phone number.
    /// </summary>
    public string CELL_PHONE { get; set; } = null!;

    /// <summary>
    /// SMS subject line.
    /// </summary>
    public string SMS_NAME { get; set; } = null!;

    /// <summary>
    /// SMS message body content.
    /// </summary>
    public string SMS_BODY { get; set; } = null!;

    /// <summary>
    /// SMS provider code.
    /// </summary>
    public string SMS_PROVIDER { get; set; } = null!;

    /// <summary>
    /// SMS status code.
    /// </summary>
    public string SMS_STATUS { get; set; } = null!;

    /// <summary>
    /// SMS status description.
    /// </summary>
    public string SMS_STATUS_STRING { get; set; } = null!;

    /// <summary>
    /// SMS account balance (points remaining).
    /// </summary>
    public decimal? SMS_CURR_POINT { get; set; } = null;

    /// <summary>
    /// Transaction trace sequence number.
    /// </summary>
    public string TRACE_ID { get; set; } = null!;

    /// <summary>
    /// Reserved field 1.
    /// </summary>
    public string USER_CHAR_1 { get; set; } = null!;

    /// <summary>
    /// Reserved field 2.
    /// </summary>
    public string USER_CHAR_2 { get; set; } = null!;

    /// <summary>
    /// SMS scheduled send time.
    /// </summary>
    public DateTime? BOOKING_DTE { get; set; } = null;

    /// <summary>
    /// SMS validity period.
    /// </summary>
    public DateTime? EXPIR_DTE { get; set; } = null;


    /// <summary>
    /// SMS delivery time.
    /// </summary>
    public DateTime? REACH_DTE { get; set; } = null;

    /// <summary>
    /// Maintenance timestamp.
    /// </summary>
    public DateTime? MNT_DT { get; set; } = null;

    /// <summary>
    /// Maintenance user.
    /// </summary>
    public string MNT_USER { get; set; } = null!;
    /// <summary>
    /// OTP transaction record creation timestamp.
    /// </summary>
    public DateTime? CREATE_DT { get; set; } = null;

    /// <summary>
    /// OTP transaction record creation program name.
    /// </summary>
    public string CREATE_USER { get; set; } = null!;
}


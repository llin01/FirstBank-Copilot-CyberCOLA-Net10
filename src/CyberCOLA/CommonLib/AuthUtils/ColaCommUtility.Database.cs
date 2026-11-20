#nullable enable
using Microsoft.Data.SqlClient;
using System;

namespace Com.Cybersoft4u.CommonLib.AuthUtils
{
    /// <summary>
    /// Core utility class providing common functions for authorization processing, database operations, 
    /// encryption/decryption, SMS messaging, and ISO 8583 message handling.
    /// </summary>
    public static partial class ColaCommUtility
    {
        /// <summary>
        /// Gets an SQL connection object using the provided connection string.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        /// <returns>A new <see cref="SqlConnection"/> instance if successful; otherwise, <c>null</c>.</returns>
        public static SqlConnection? GetSqlConnection(string connectionString)
        {
            try
            {
                return (new SqlConnection(connectionString));
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"SqlConnection Exception:{ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Retrieves the system parameter connection string, decrypting it if necessary.
        /// </summary>
        /// <returns>The decrypted or plain connection string.</returns>
        private static string? GetSystemParameterConnectionString()
        {
            try
            {
                var connectionString = SystemParameterInfo.DatabaseInfo.ConnectionString.Trim();
                //based on customer's needs
                //var connectionSystemId = SystemParameterInfo.DatabaseInfo.ConnectionSystemID.Trim();
                var isEncrypted = Convert.ToBoolean(SystemParameterInfo.DatabaseInfo.IsEncrypted.Trim());
                if (isEncrypted)
                {
                    if (_ConnectionString == null)
                    {
                        _ConnectionString = DecryptAesGcm(connectionString);
                    }
                    connectionString = _ConnectionString != null ? new string(_ConnectionString.ToCharArray()) : null;
                }
                if (connectionString != null) return (connectionString);
                Log.Info("GetSystemParameterConnectionString Decrypt Error");
                return null;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"GetSystemParameterConnectionString Exception:{ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Retrieves the system parameter connection string, decrypting it if necessary.
        /// </summary>
        /// <returns>The decrypted or plain connection string.</returns>
        public static string? GetConnectionString()
        {
            try
            {
                var connectionString = GetSystemParameterConnectionString();
                if (IsDisplayConnectionString)
                {
                    if (!string.IsNullOrEmpty(connectionString))
                    {
                        Log.Debug(connectionString.ToUpper().Contains("USER")
                                  ? $"ConnectionString use GetSystemParameterConnectionString[{connectionString.Substring(0, connectionString.ToUpper().IndexOf("USER", StringComparison.Ordinal))}], isEncrypt[{SystemParameterInfo?.DatabaseInfo.IsEncrypted}]"
                                  : $"ConnectionString use GetSystemParameterConnectionString[{connectionString}], isEncrypt[{SystemParameterInfo?.DatabaseInfo.IsEncrypted}]");
                    }
                    IsDisplayConnectionString = false;
                }

                if (string.IsNullOrEmpty(connectionString))
                {
                    Log.Error($"GetConnectionString Method Get ConnectionString Invalid");
                }
                return (connectionString);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"GetConnectionString Exception:{ex.Message}");
                return null;
            }
        }
    }
}

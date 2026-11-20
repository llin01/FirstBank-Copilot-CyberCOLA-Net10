using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.Resource;
using Microsoft.Data.SqlClient;
using NLog;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Com.Cybersoft4u.CommonLib.DBUtils
{
    public class DbStoreProc
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private readonly string _connectionString;
        private string _storeProcedureName;

        /// <summary>
        /// Constructor DbStoreProcedure Instance
        /// </summary>
        /// <param name="connectionString">DataBase Connection String</param>

        public DbStoreProc(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new Exception("Connection String Invalid");
            _connectionString = connectionString;
        }

        /// <summary>
        /// Constructor DbStoreProc Instance
        /// </summary>
        /// <param name="connectionString">DataBase Connection String</param>
        /// <param name="storeProcedureName">Store Procedure Name</param>
        public DbStoreProc(string connectionString, string storeProcedureName)
        {
            if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(storeProcedureName))
                throw new Exception("Connection String Or Store Procedure Name Invalid");
            ValidateStoreProcedureName(storeProcedureName);
            _connectionString = connectionString;
            _storeProcedureName = storeProcedureName;
            Log.Debug($"Execute Store Procedure Name[{storeProcedureName}]");
        }

        /// <summary>
        /// Get Store Procedure Name
        /// </summary>
        public string StoreProcedureName
        {
            get => _storeProcedureName;
            set
            {
                ValidateStoreProcedureName(value);
                _storeProcedureName = value;
            }
        }

        /// <summary>
        /// Validates that the stored procedure name is a valid SQL identifier
        /// </summary>
        /// <param name="storeProcedureName">The stored procedure name to validate</param>
        private static void ValidateStoreProcedureName(string storeProcedureName)
        {
            if (string.IsNullOrWhiteSpace(storeProcedureName))
                throw new ArgumentException("Store procedure name cannot be null or empty.", nameof(storeProcedureName));

            // Valid SQL identifier pattern: allows schema.procedure or [schema].[procedure] format
            // Pattern allows: alphanumeric, underscore, dot (for schema separator), brackets, and Chinese characters
            var validPattern = @"^(\[?[\w\u4e00-\u9fa5]+\]?\.)?(\[?[\w\u4e00-\u9fa5]+\]?)$";
            if (!Regex.IsMatch(storeProcedureName, validPattern))
                throw new ArgumentException("Store procedure name contains invalid characters.", nameof(storeProcedureName));
        }

        /// <summary>
        /// Execute Store Procedure(Insert/Delete/Update)
        /// </summary>
        /// <param name="paraValues">Parameter Array</param>
        /// <returns>Effect Row Count</returns>
        public int ExecuteNonQuery(params object[] paraValues)
        {

            using (SqlConnection connection = new SqlConnection(_connectionString + ";" + SystemConstantData.CommonData.ColumnEncryptionSetting))
            {

                SqlCommand command = CreateSqlCommand(connection);

                try
                {
                    DeriveParameters(command);
                    AssignParameterValues(command, paraValues);
                    var execStart = AuthTiming.Start();
                    connection.Open();
                    int affectedRowsCount = command.ExecuteNonQuery();
                    AuthTiming.StopDbExec(execStart);
                    return affectedRowsCount;
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, $"ExecuteNonQuery Exception:{ex.Message}");
                    InvalidateParameters();
                    throw;
                }
            }
        }

        /// <summary>
        /// Execute Store Procedure And Return Out Parameter
        /// </summary>
        /// <param name="outputParaCount">OutPut Parameter Count</param>
        /// <param name="paraValues">Parameter Array</param>
        /// <returns>Effect Row Count</returns>
        public string[] ExecProcOutput(int outputParaCount, params object[] paraValues)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString + ";" + SystemConstantData.CommonData.ColumnEncryptionSetting))
            {

                SqlCommand command = CreateSqlCommand(connection);

                try
                {
                    command.Parameters.Clear();
                    DeriveParameters(command);
                    AssignParameterValues(command, paraValues);
                    var execStart = AuthTiming.Start();
                    connection.Open();
                    int affectedRowsCount = command.ExecuteNonQuery();
                    AuthTiming.StopDbExec(execStart);
                    //Log.Info("ExecProcOutput:Effected Row Count: " + affectedRowsCount);
                    if (outputParaCount <= 0) return null;
                    string[] returnStr = new string[outputParaCount];
                    int outParameter = 0;
                    for (int index = 0; index < command.Parameters.Count; index++)
                    {
                        if (Log.IsTraceEnabled)
                            Log.Trace(command.Parameters[index].Value + "--" + command.Parameters[index].Direction);
                        if ((command.Parameters[index].Direction == ParameterDirection.Output) ||
                            (command.Parameters[index].Direction == ParameterDirection.InputOutput))
                        {
                            if (outParameter >= outputParaCount) return null;
                            returnStr[outParameter] = command.Parameters[index].Value.ToString();
                            outParameter++;
                        }
                    }
                    return returnStr;
                }
                catch (SqlException sqlex)
                {
                    Log.Fatal(sqlex, $"ExecProcOutput Exception:{sqlex.Message}");
                    InvalidateParameters();
                    throw;
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, $"ExecProcOutput Exception:{ex.Message}");
                    InvalidateParameters();
                    throw;
                }
            }

        }

        /// <summary>
        /// Execute Store Procedure(Return System.Data.DataTable)
        /// </summary>
        /// <param name="paraValues">Parameter Array</param>
        /// <returns>Result Set(System.Data.DataTable)</returns>
        public DataTable ExecuteDataTable(params object[] paraValues)
        {

            using (SqlConnection connection = new SqlConnection(_connectionString + ";" + SystemConstantData.CommonData.ColumnEncryptionSetting))
            {
                SqlCommand command = CreateSqlCommand(connection);
                try
                {
                    command.Parameters.Clear();
                    DeriveParameters(command);
                    AssignParameterValues(command, paraValues);
                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    DataTable dataTable = new DataTable();
                    var execStart = AuthTiming.Start();
                    adapter.Fill(dataTable);
                    AuthTiming.StopDbExec(execStart);
                    return dataTable;
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, $"ExecuteDataTable Exception:{ex.Message}");
                    InvalidateParameters();
                    throw;
                }
            }
        }

        /// <summary>
        /// Execute Store Procedure System.Data.DataTable。
        /// </summary>
        /// <param name="dataTable">Result Set(System.Data.DataTable)</param>
        /// <param name="paraValues">Parameter Array</param>
        public void ExecuteFillDataTable(DataTable dataTable, params object[] paraValues)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString + ";" + SystemConstantData.CommonData.ColumnEncryptionSetting))
            {
                SqlCommand command = CreateSqlCommand(connection);
                try
                {
                    command.Parameters.Clear();
                    DeriveParameters(command);
                    AssignParameterValues(command, paraValues);
                    var execStart = AuthTiming.Start();
                    connection.Open();
                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    adapter.Fill(dataTable);
                    AuthTiming.StopDbExec(execStart);
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, $"ExecuteFillDataTable Exception:{ex.Message}");
                    InvalidateParameters();
                    throw;
                }
            }
        }

        /// <summary>
        /// Execute Store Procedure(Return Microsoft.Data.SqlClient.SqlDataReader)
        /// At Microsoft.Data.SqlClient.SqlDataReader Closed,Database Connection Auto Closed
        /// </summary>
        /// <param name="paraValues">Parameter Array</param>
        /// <returns>Result Set(Return Microsoft.Data.SqlClient.SqlDataReader)</returns>
        public SqlDataReader ExecuteDataReader(params object[] paraValues)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString + ";" + SystemConstantData.CommonData.ColumnEncryptionSetting))
            {
                SqlCommand command = CreateSqlCommand(connection);
                try
                {
                    command.Parameters.Clear();
                    DeriveParameters(command);
                    AssignParameterValues(command, paraValues);
                    connection.Open();
                    return command.ExecuteReader(CommandBehavior.CloseConnection);
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, $"ExecuteDataReader Exception:{ex.Message}");
                    InvalidateParameters();
                    throw;
                }
            }
        }

        /// <summary>
        ///  Execute Store Procedure And Return Result Set First Row,First Column
        /// </summary>
        /// <param name="paraValues">Parameter Array</param>
        /// <returns>Return Result Set First Row,First Column Or Null</returns>
        public object ExecuteScalar(params object[] paraValues)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString + ";" + SystemConstantData.CommonData.ColumnEncryptionSetting))
            {
                SqlCommand command = CreateSqlCommand(connection);
                try
                {
                    command.Parameters.Clear();
                    DeriveParameters(command);
                    AssignParameterValues(command, paraValues);
                    var execStart = AuthTiming.Start();
                    connection.Open();
                    var scalar = command.ExecuteScalar();
                    AuthTiming.StopDbExec(execStart);
                    return scalar;
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, $"ExecuteScalar Exception:{ex.Message}");
                    InvalidateParameters();
                    throw;
                }
            }
        }

        /// <summary>
        /// From Microsoft.Data.SqlClient.SqlCommand 中指定的存儲過程中檢索參數資訊並填充指定的
        /// Microsoft.Data.SqlClient.SqlCommand 對象的 Microsoft.Data.SqlClient.SqlCommand.Parameters 集合。
        /// </summary>
        /// <param name="sqlCommand">將從其中導出參數資訊的存儲過程的 Microsoft.Data.SqlClient.SqlCommand 對象。</param>
        internal void DeriveParameters(SqlCommand sqlCommand)
        {
            StoredProcedureParameterCache.Apply(ParameterCacheKey, sqlCommand, DeriveFromDatabase);
        }

        private string ParameterCacheKey => StoredProcedureParameterCache.Key(_connectionString, _storeProcedureName);

        // Called on any execution error: the procedure's signature may have changed, so the next call derives again.
        private void InvalidateParameters() => StoredProcedureParameterCache.Invalidate(ParameterCacheKey);

        private void DeriveFromDatabase(SqlCommand sqlCommand)
        {
            try
            {
                // One round trip per procedure and connection string; AuthTiming shows when it still happens.
                var deriveStart = AuthTiming.Start();
                sqlCommand.Connection.Open();
                SqlCommandBuilder.DeriveParameters(sqlCommand);
                sqlCommand.Connection.Close();
                AuthTiming.StopDbDerive(deriveStart);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"DeriveParameters Exception:{ex.Message}");
                sqlCommand.Connection?.Close();
                throw;
            }
        }

        // 用指定的參數值列表為存儲過程參數賦值。
        private void AssignParameterValues(SqlCommand sqlCommand, params object[] paraValues)
        {
            if (Log.IsTraceEnabled)
                Log.Trace($"sqlCommand.Parameters.Count[{sqlCommand.Parameters.Count - 1}],paraValues.Length[{paraValues.Length}]");
            if (paraValues != null)
            {
                if ((sqlCommand.Parameters.Count - 1) != paraValues.Length)
                {
                    throw new ArgumentNullException($"The number of parameters does not match number of values for stored procedure.{(sqlCommand.Parameters.Count - 1)}");
                }
                for (int i = 0; i < paraValues.Length; i++)
                {
                    //if (Log.IsDebugEnabled)
                    //    Log.Debug(sqlCommand.Parameters[i + 1].ParameterName + "=" + (paraValues[i]));

                    sqlCommand.Parameters[i + 1].Value = (paraValues[i] == null) ? DBNull.Value : ParameterSanitizer.Sanitize(paraValues[i]);
                }
            }
        }

        // Create Sql Command For Store Procedure Execute
        [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "StoreProcedureName is validated by ValidateStoreProcedureName method to prevent SQL injection")]
        private SqlCommand CreateSqlCommand(SqlConnection connection)
        {
            SqlCommand command = new SqlCommand(_storeProcedureName, connection);
            command.CommandType = CommandType.StoredProcedure;
            return command;
        }
    }
}

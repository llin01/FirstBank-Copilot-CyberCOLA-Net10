using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.Resource;
using Microsoft.Data.SqlClient;
using NLog;
using System.Data;

namespace Com.Cybersoft4u.CommonLib.DBUtils
{
    public class DbFunction
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private readonly string _connectionString;
        private string _sqlSelectText;

        /// <summary>
        /// Constructor DbFunction Instance
        /// </summary>
        /// <param name="connectionString">DataBase Connection String</param>

        public DbFunction(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Constructor DbFunction Instance
        /// </summary>
        /// <param name="connectionString">DataBase Connection String</param>
        /// <param name="sqlSelectText">Store Procedure Name</param>
        public DbFunction(string connectionString, string sqlSelectText)
        {
            _connectionString = connectionString;
            _sqlSelectText = sqlSelectText;
        }

        /// <summary>
        /// Get SQL Select Text String
        /// </summary>
        public string SqlSelectText
        {
            get { return _sqlSelectText; }
            set { _sqlSelectText = value; }
        }

        /// <summary>
        /// Execute Table-Value Function(Return System.Data.DataTable)
        /// </summary>
        /// <param name="paraValues">Parameter Array</param>
        /// <returns>Result Set(System.Data.DataTable)</returns>
        public DataTable ExecuteDataTable(params object[] paraValues)
        {

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                SqlCommand command = CreateSqlCommand(connection);
                try
                {
                    //DeriveParameters(command);
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
                    Log.Fatal(ex, "ExecuteDataTable Exception:");

                    throw;
                }
            }
        }

        /// <summary>
        ///  Execute Store Procedure And Return Result Set First Row,First Column
        /// </summary>
        /// <param name="paraValues">Parameter Array</param>
        /// <return>Result Set First Row,First Column Or Null</return>
        public object ExecuteScalar(params object[] paraValues)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString + ";" + SystemConstantData.CommonData.ColumnEncryptionSetting))
            {
                SqlCommand command = CreateSqlCommand(connection);
                try
                {
                    //DeriveParameters(command);
                    AssignParameterValues(command, paraValues);
                    var execStart = AuthTiming.Start();
                    connection.Open();
                    var scalar = command.ExecuteScalar();
                    AuthTiming.StopDbExec(execStart);
                    return scalar;
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, "ExecuteScalar Exception:");

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
            try
            {
                sqlCommand.Connection.Open();
                SqlCommandBuilder.DeriveParameters(sqlCommand);
                sqlCommand.Connection.Close();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "DeriveParameters Exception:");
                if (sqlCommand.Connection != null)
                {
                    sqlCommand.Connection.Close();
                }
                throw;
            }
        }

        // 用指定的參數值列表為存儲過程參數賦值。
        private void AssignParameterValues(SqlCommand sqlCommand, params object[] paraValues)
        {
            if (Log.IsTraceEnabled)
                Log.Trace($"sqlCommand.Parameters.Count[{sqlCommand.Parameters.Count - 1}],paraValues.Length[{paraValues.Length}]");

            SqlParameter param = new SqlParameter();
            param.ParameterName = "@messageType";
            param.SqlDbType = SqlDbType.VarChar;
            param.Size = 4;
            param.Value = "1234";

            // 3. add new parameter to command object
            sqlCommand.Parameters.Add(param);

            //for (int i = 0; i < paraValues.Length; i++)
            //{
            //    Log.Info(sqlCommand.Parameters[i].ParameterName + "=" + (paraValues[i]));
            //    //sqlCommand.Parameters[i].Value = (paraValues[i] == null) ? DBNull.Value : paraValues[i];
            //    //sqlCommand.Parameters.Add((paraValues[i] == null) ? DBNull.Value : paraValues[i]);
            //    SqlParameter param = new SqlParameter();
            //    param.ParameterName = "@City";
            //    param.Value = ((paraValues[i] == null) ? DBNull.Value : paraValues[i]);

            //    // 3. add new parameter to command object
            //    sqlCommand.Parameters.Add(param);
            //}
        }

        // Create SQL Command For Store Procedure Execute
        private SqlCommand CreateSqlCommand(SqlConnection connection)
        {
            SqlCommand command = new SqlCommand(_sqlSelectText, connection);
            command.CommandType = CommandType.Text;
            return command;
        }
    }
}

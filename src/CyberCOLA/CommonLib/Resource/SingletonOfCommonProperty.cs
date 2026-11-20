using Com.Cybersoft4u.CommonLib.AuthUtils;
using NLog;
using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;

namespace Com.Cybersoft4u.CommonLib.Resource
{
    public class SingletonOfCommonProperty
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static SingletonOfCommonProperty _instance = new SingletonOfCommonProperty();
        private static readonly object InstanceLock = new object();
        private static readonly object LockFlag = new object();

        private readonly string _connectionString;
        private ColaPasystfInfoStruct _colaPasystfInfoStruct;


        private SingletonOfCommonProperty()
        {
            try
            {
                _connectionString = ColaCommUtility.GetConnectionString();
                _colaPasystfInfoStruct = new ColaPasystfInfoStruct();
                if (GetColaPasystfInfoFromDataBase() != 0)
                {
                    Log.Fatal("ColaPasystfInfoStruct Get Data Error");
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"ColaPasystfInfoStruct Get Data Error:{ex.Message}");
            }

        }

        public static SingletonOfCommonProperty Instance
        {
            get
            {
                lock (InstanceLock)
                {
                    return _instance ??= new SingletonOfCommonProperty();
                }
            }
        }

        /// <summary>
        /// Gets the cola pasystf information structure.
        /// </summary>
        /// <returns>ColaPasystfInfoStruct.</returns>
        public ColaPasystfInfoStruct GetColaPasystfInfoStruct()
        {
            return (_colaPasystfInfoStruct);
        }

        private int GetColaPasystfInfoFromDataBase()
        {
            int returnCode = -1;
            try
            {
                using (var connection = new SqlConnection(_connectionString + ";" + SystemConstantData.CommonData.ColumnEncryptionSetting))
                {
                    var cmd = new SqlCommand("dbo.Cola_SelectPasystf", connection);
                    cmd.CommandType = CommandType.StoredProcedure;

                    var parm00 = new SqlParameter("@input_key", SqlDbType.VarChar);
                    parm00.Value = SystemConstantData.CommonData.ColaBu;
                    parm00.Direction = ParameterDirection.Input;
                    cmd.Parameters.Add(parm00);

                    var parm01 = new SqlParameter("@return_code", SqlDbType.Int);
                    parm01.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm01);

                    var parm02 = new SqlParameter("@bu", SqlDbType.VarChar);
                    parm02.Size = 3;
                    parm02.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm02);

                    var parm03 = new SqlParameter("@syst_work_sw", SqlDbType.VarChar);
                    parm03.Size = 14;
                    parm03.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm03);

                    var parm04 = new SqlParameter("@holiday", SqlDbType.VarChar);
                    parm04.Size = 270;
                    parm04.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm04);

                    var parm05 = new SqlParameter("@today_process_dt", SqlDbType.VarChar);
                    parm05.Size = 20;
                    parm05.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm05);

                    var parm06 = new SqlParameter("@prev_process_dt", SqlDbType.VarChar);
                    parm06.Size = 20;
                    parm06.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm06);

                    var parm07 = new SqlParameter("@next_process_dt", SqlDbType.VarChar);
                    parm07.Size = 20;
                    parm07.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm07);

                    var parm08 = new SqlParameter("@lst_batch_dt", SqlDbType.VarChar);
                    parm08.Size = 20;
                    parm08.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm08);

                    var parm09 = new SqlParameter("@nccc_system_dt", SqlDbType.VarChar);
                    parm09.Size = 20;
                    parm09.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm09);

                    var parm10 = new SqlParameter("@nccc_system_upd_dt", SqlDbType.VarChar);
                    parm10.Size = 20;
                    parm10.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm10);

                    var parm11 = new SqlParameter("@nccc_iwk_check_digits", SqlDbType.VarChar);
                    parm11.Size = 6;
                    parm11.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm11);

                    var parm12 = new SqlParameter("@nccc_iwk_change_dt", SqlDbType.VarChar);
                    parm12.Size = 20;
                    parm12.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm12);

                    var parm13 = new SqlParameter("@fisc_system_dt", SqlDbType.VarChar);
                    parm13.Size = 20;
                    parm13.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm13);

                    var parm14 = new SqlParameter("@fisc_system_upd_dt", SqlDbType.VarChar);
                    parm14.Size = 20;
                    parm14.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm14);

                    var parm15 = new SqlParameter("@fisc_iwk_check_digits", SqlDbType.VarChar);
                    parm15.Size = 6;
                    parm15.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm15);

                    var parm16 = new SqlParameter("@fisc_iwk_change_dt", SqlDbType.VarChar);
                    parm16.Size = 20;
                    parm16.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm16);

                    var parm17 = new SqlParameter("@twin_system_dt", SqlDbType.VarChar);
                    parm17.Size = 20;
                    parm17.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm17);

                    var parm18 = new SqlParameter("@twin_system_upd_dt", SqlDbType.VarChar);
                    parm18.Size = 20;
                    parm18.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm18);

                    var parm19 = new SqlParameter("@twin_iwk_check_digits", SqlDbType.VarChar);
                    parm19.Size = 6;
                    parm19.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm19);

                    var parm20 = new SqlParameter("@twin_iwk_change_dt", SqlDbType.VarChar);
                    parm20.Size = 20;
                    parm20.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm20);

                    var parm21 = new SqlParameter("@post_result", SqlDbType.VarChar);
                    parm21.Size = 2;
                    parm21.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm21);

                    var parm22 = new SqlParameter("@mnt_dt", SqlDbType.VarChar);
                    parm22.Size = 20;
                    parm22.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm22);

                    var parm23 = new SqlParameter("@mnt_user", SqlDbType.VarChar);
                    parm23.Size = 20;
                    parm23.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm23);

                    connection.Open();
                    cmd.ExecuteNonQuery();
                    connection.Close();
                    returnCode = (int)cmd.Parameters["@return_code"].Value;
                    _colaPasystfInfoStruct.Bu = cmd.Parameters["@bu"].Value.ToString();
                    _colaPasystfInfoStruct.SystWorkSw = cmd.Parameters["@syst_work_sw"].Value.ToString();
                    _colaPasystfInfoStruct.Holiday = cmd.Parameters["@holiday"].Value.ToString();
                    _colaPasystfInfoStruct.TodayProcessDt = cmd.Parameters["@today_process_dt"].Value.ToString();
                    _colaPasystfInfoStruct.PrevProcessDt = cmd.Parameters["@prev_process_dt"].Value.ToString();
                    _colaPasystfInfoStruct.NextProcessDt = cmd.Parameters["@next_process_dt"].Value.ToString();
                    _colaPasystfInfoStruct.LstBatchDt = cmd.Parameters["@lst_batch_dt"].Value.ToString();
                    _colaPasystfInfoStruct.NCCCSystemDt = cmd.Parameters["@nccc_system_dt"].Value.ToString();
                    _colaPasystfInfoStruct.NCCCSystemUpdDt = cmd.Parameters["@nccc_system_upd_dt"].Value.ToString();
                    _colaPasystfInfoStruct.NCCCIwkCheckDigits = cmd.Parameters["@nccc_iwk_check_digits"].Value.ToString();
                    _colaPasystfInfoStruct.NCCCIwkChangeDt = cmd.Parameters["@nccc_iwk_change_dt"].Value.ToString();
                    _colaPasystfInfoStruct.FISCSystemDt = cmd.Parameters["@fisc_system_dt"].Value.ToString();
                    _colaPasystfInfoStruct.FISCSystemUpdDt = cmd.Parameters["@fisc_system_upd_dt"].Value.ToString();
                    _colaPasystfInfoStruct.FISCIwkCheckDigits = cmd.Parameters["@fisc_iwk_check_digits"].Value.ToString();
                    _colaPasystfInfoStruct.FISCIwkChangeDt = cmd.Parameters["@fisc_iwk_change_dt"].Value.ToString();
                    _colaPasystfInfoStruct.TWINSystemDt = cmd.Parameters["@twin_system_dt"].Value.ToString();
                    _colaPasystfInfoStruct.TWINSystemUpdDt = cmd.Parameters["@twin_system_upd_dt"].Value.ToString();
                    _colaPasystfInfoStruct.TWINIwkCheckDigits = cmd.Parameters["@twin_iwk_check_digits"].Value.ToString();
                    _colaPasystfInfoStruct.TWINIwkChangeDt = cmd.Parameters["@twin_iwk_change_dt"].Value.ToString();
                    _colaPasystfInfoStruct.PostResult = cmd.Parameters["@post_result"].Value.ToString();
                    _colaPasystfInfoStruct.MntDt = cmd.Parameters["@mnt_dt"].Value.ToString();
                    _colaPasystfInfoStruct.MntUser = cmd.Parameters["@mnt_user"].Value.ToString();
                }
                return returnCode;
            }
            catch (Exception ex)
            {
                Log.Info("Select COLA_PASYSTF Error[{0}]", ex.Message);
                return returnCode;
            }
        }

        public void SetColaPasystfInfoSystemDate(string systemDate)
        {
            lock (LockFlag)
            {
                Log.Info("Set Cola PasystfInfo SystemDate at : {0:yyyy-MM-dd HH:mm:ss},Value[{1}]", DateTime.Now,
                         systemDate);
                _colaPasystfInfoStruct.NCCCSystemDt = systemDate;
            }
        }

        public void SetColaPasystfInfoSystemDate(string systemDate, int cardInfo)
        {
            switch (cardInfo)
            {
                case SystemConstantData.CommonData.PasystfNCCCInfo:
                    lock (LockFlag)
                    {
                        Log.Info("Set Cola PasystfInfo NCCCSystemDate at : {0:yyyy-MM-dd HH:mm:ss}, Value[{1}]", DateTime.Now, systemDate);
                        _colaPasystfInfoStruct.NCCCSystemDt = systemDate;
                    }
                    break;
                case SystemConstantData.CommonData.PasystfFISCInfo:
                    lock (LockFlag)
                    {
                        Log.Info("Set Cola PasystfInfo FISCSystemDate at : {0:yyyy-MM-dd HH:mm:ss}, Value[{1}]", DateTime.Now, systemDate);
                        _colaPasystfInfoStruct.FISCSystemDt = systemDate;
                    }
                    break;
                case SystemConstantData.CommonData.PasystfTWINInfo:
                    lock (LockFlag)
                    {
                        Log.Info("Set Cola PasystfInfo TWINSystemDate at : {0:yyyy-MM-dd HH:mm:ss}, Value[{1}]", DateTime.Now, systemDate);
                        _colaPasystfInfoStruct.TWINSystemDt = systemDate;
                    }
                    break;
                default:
                    Log.Error($"Set SetColaPasystfInfoSystemDate, Input Error[{cardInfo}] at : {DateTime.Now:yyyy-MM-dd HH:mm:ss}, Value[{systemDate}]");
                    break;

            }
        }

        public void SetColaSignOn(int cardInfo)
        {
            switch (cardInfo)
            {
                case SystemConstantData.CommonData.PasystfNCCCInfo:
                    lock (LockFlag)
                    {
                        Log.Info("Set NCCC SignOn at : {0:yyyy-MM-dd HH:mm:ss}", DateTime.Now);
                    }
                    break;
                case SystemConstantData.CommonData.PasystfFISCInfo:
                    lock (LockFlag)
                    {
                        Log.Info("Set FISC SignOn at : {0:yyyy-MM-dd HH:mm:ss}", DateTime.Now);
                    }
                    break;
                case SystemConstantData.CommonData.PasystfTWINInfo:
                    lock (LockFlag)
                    {
                        Log.Info("Set TWIN SignOn at : {0:yyyy-MM-dd HH:mm:ss}", DateTime.Now);
                    }
                    break;
                case SystemConstantData.CommonData.PasystfSVCSInfo:
                    lock (LockFlag)
                    {
                        Log.Info("Set SVCS SignOn at : {0:yyyy-MM-dd HH:mm:ss}", DateTime.Now);
                    }
                    break;
                case SystemConstantData.CommonData.PasystfICASHInfo:
                    lock (LockFlag)
                    {
                        Log.Info("Set ICASH SignOn at : {0:yyyy-MM-dd HH:mm:ss}", DateTime.Now);
                    }
                    break;
                case SystemConstantData.CommonData.PasystfIPASSInfo:
                    lock (LockFlag)
                    {
                        Log.Info("Set IPASS SignOn at : {0:yyyy-MM-dd HH:mm:ss}", DateTime.Now);
                    }
                    break;
                default:
                    Log.Error($"Set SetColaSignOn, Input Error[{cardInfo}] at : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    break;

            }
        }
        public void SetColaSignOff(int cardInfo)
        {
            switch (cardInfo)
            {
                case SystemConstantData.CommonData.PasystfNCCCInfo:
                    lock (LockFlag)
                    {
                        Log.Info("Set NCCC SignOff at : {0:yyyy-MM-dd HH:mm:ss}", DateTime.Now);
                    }
                    break;
                case SystemConstantData.CommonData.PasystfFISCInfo:
                    lock (LockFlag)
                    {
                        Log.Info("Set FISC SignOff at : {0:yyyy-MM-dd HH:mm:ss}", DateTime.Now);
                    }
                    break;
                case SystemConstantData.CommonData.PasystfTWINInfo:
                    lock (LockFlag)
                    {
                        Log.Info("Set TWIN SignOff at : {0:yyyy-MM-dd HH:mm:ss}", DateTime.Now);
                    }
                    break;
                case SystemConstantData.CommonData.PasystfSVCSInfo:
                    lock (LockFlag)
                    {
                        Log.Info("Set SVCS SignOff at : {0:yyyy-MM-dd HH:mm:ss}", DateTime.Now);
                    }
                    break;
                case SystemConstantData.CommonData.PasystfICASHInfo:
                    lock (LockFlag)
                    {
                        Log.Info("Set ICASH SignOff at : {0:yyyy-MM-dd HH:mm:ss}", DateTime.Now);
                    }
                    break;
                case SystemConstantData.CommonData.PasystfIPASSInfo:
                    lock (LockFlag)
                    {
                        Log.Info("Set IPASS SignOff at : {0:yyyy-MM-dd HH:mm:ss}", DateTime.Now);
                    }
                    break;
                default:
                    Log.Error($"Set SetColaSignOff, Input Error[{cardInfo}] at : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    break;

            }
        }
        public void SetColaPasystfInfoIwkCheckDigits(string iwkCheckDigits)
        {
            lock (LockFlag)
            {
                Log.Info("Set Cola PasystfInfo NCCCIwkCheckDigits at : {0:yyyy-MM-dd HH:mm:ss}, Value[{1}]", DateTime.Now, iwkCheckDigits);
                _colaPasystfInfoStruct.NCCCIwkCheckDigits = iwkCheckDigits;
            }
        }

        public void SetColaPasystfInfoIwkCheckDigits(string iwkCheckDigits, int cardInfo)
        {
            switch (cardInfo)
            {
                case SystemConstantData.CommonData.PasystfNCCCIwkUpdate:
                    lock (LockFlag)
                    {
                        Log.Info("Set Cola PasystfInfo NCCCIwkCheckDigits at : {0:yyyy-MM-dd HH:mm:ss}, Value[{1}]", DateTime.Now, iwkCheckDigits);
                        _colaPasystfInfoStruct.NCCCIwkCheckDigits = iwkCheckDigits;
                    }
                    break;
                case SystemConstantData.CommonData.PasystfFISCIwkUpdate:
                    lock (LockFlag)
                    {
                        Log.Info("Set Cola PasystfInfo FISCIwkCheckDigits at : {0:yyyy-MM-dd HH:mm:ss}, Value[{1}]", DateTime.Now, iwkCheckDigits);
                        _colaPasystfInfoStruct.FISCIwkCheckDigits = iwkCheckDigits;
                    }
                    break;
                case SystemConstantData.CommonData.PasystfTWINIwkUpdate:
                    lock (LockFlag)
                    {
                        Log.Info("Set Cola PasystfInfo TWINIwkCheckDigits at : {0:yyyy-MM-dd HH:mm:ss}, Value[{1}]", DateTime.Now, iwkCheckDigits);
                        _colaPasystfInfoStruct.TWINIwkCheckDigits = iwkCheckDigits;
                    }
                    break;
                default:
                    Log.Error($"Set SetColaPasystfInfoIwkCheckDigits, Input Error[{cardInfo}] at : {DateTime.Now:yyyy-MM-dd HH:mm:ss}, Value[{iwkCheckDigits}]");
                    break;

            }
        }
        public int SyncColaPasysInfoData()
        {
            int returnCode = -1;
            lock (LockFlag)
            {
                using (var connection = new SqlConnection(_connectionString + ";" + SystemConstantData.CommonData.ColumnEncryptionSetting))
                {
                    var cmd = new SqlCommand("dbo.Cola_SelectPasystf", connection);
                    cmd.CommandType = CommandType.StoredProcedure;

                    var parm00 = new SqlParameter("@input_key", SqlDbType.VarChar);
                    parm00.Value = SystemConstantData.CommonData.ColaBu;
                    parm00.Direction = ParameterDirection.Input;
                    cmd.Parameters.Add(parm00);

                    var parm01 = new SqlParameter("@return_code", SqlDbType.Int);
                    parm01.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm01);

                    var parm02 = new SqlParameter("@bu", SqlDbType.VarChar);
                    parm02.Size = 3;
                    parm02.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm02);

                    var parm03 = new SqlParameter("@syst_work_sw", SqlDbType.VarChar);
                    parm03.Size = 14;
                    parm03.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm03);

                    var parm04 = new SqlParameter("@holiday", SqlDbType.VarChar);
                    parm04.Size = 270;
                    parm04.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm04);

                    var parm05 = new SqlParameter("@today_process_dt", SqlDbType.VarChar);
                    parm05.Size = 20;
                    parm05.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm05);

                    var parm06 = new SqlParameter("@prev_process_dt", SqlDbType.VarChar);
                    parm06.Size = 20;
                    parm06.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm06);

                    var parm07 = new SqlParameter("@next_process_dt", SqlDbType.VarChar);
                    parm07.Size = 20;
                    parm07.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm07);

                    var parm08 = new SqlParameter("@lst_batch_dt", SqlDbType.VarChar);
                    parm08.Size = 20;
                    parm08.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm08);

                    var parm09 = new SqlParameter("@nccc_system_dt", SqlDbType.VarChar);
                    parm09.Size = 20;
                    parm09.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm09);

                    var parm10 = new SqlParameter("@nccc_system_upd_dt", SqlDbType.VarChar);
                    parm10.Size = 20;
                    parm10.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm10);

                    var parm11 = new SqlParameter("@nccc_iwk_check_digits", SqlDbType.VarChar);
                    parm11.Size = 6;
                    parm11.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm11);

                    var parm12 = new SqlParameter("@nccc_iwk_change_dt", SqlDbType.VarChar);
                    parm12.Size = 20;
                    parm12.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm12);

                    var parm13 = new SqlParameter("@fisc_system_dt", SqlDbType.VarChar);
                    parm13.Size = 20;
                    parm13.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm13);

                    var parm14 = new SqlParameter("@fisc_system_upd_dt", SqlDbType.VarChar);
                    parm14.Size = 20;
                    parm14.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm14);

                    var parm15 = new SqlParameter("@fisc_iwk_check_digits", SqlDbType.VarChar);
                    parm15.Size = 6;
                    parm15.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm15);

                    var parm16 = new SqlParameter("@fisc_iwk_change_dt", SqlDbType.VarChar);
                    parm16.Size = 20;
                    parm16.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm16);

                    var parm17 = new SqlParameter("@twin_system_dt", SqlDbType.VarChar);
                    parm17.Size = 20;
                    parm17.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm17);

                    var parm18 = new SqlParameter("@twin_system_upd_dt", SqlDbType.VarChar);
                    parm18.Size = 20;
                    parm18.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm18);

                    var parm19 = new SqlParameter("@twin_iwk_check_digits", SqlDbType.VarChar);
                    parm19.Size = 6;
                    parm19.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm19);

                    var parm20 = new SqlParameter("@twin_iwk_change_dt", SqlDbType.VarChar);
                    parm20.Size = 20;
                    parm20.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm20);

                    var parm21 = new SqlParameter("@post_result", SqlDbType.VarChar);
                    parm21.Size = 2;
                    parm21.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm21);

                    var parm22 = new SqlParameter("@mnt_dt", SqlDbType.VarChar);
                    parm22.Size = 20;
                    parm22.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm22);

                    var parm23 = new SqlParameter("@mnt_user", SqlDbType.VarChar);
                    parm23.Size = 20;
                    parm23.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm23);
                    try
                    {
                        connection.Open();
                        cmd.ExecuteNonQuery();
                        connection.Close();
                        returnCode = (int)cmd.Parameters["@return_code"].Value;
                        if (returnCode == 0)
                        {
                            _colaPasystfInfoStruct.NCCCSystemDt = cmd.Parameters["@nccc_system_dt"].Value.ToString();
                            _colaPasystfInfoStruct.NCCCSystemUpdDt = cmd.Parameters["@nccc_system_upd_dt"].Value.ToString();
                            _colaPasystfInfoStruct.NCCCIwkCheckDigits = cmd.Parameters["@nccc_iwk_check_digits"].Value.ToString();
                            _colaPasystfInfoStruct.NCCCIwkChangeDt = cmd.Parameters["@nccc_iwk_change_dt"].Value.ToString();

                            _colaPasystfInfoStruct.FISCSystemDt = cmd.Parameters["@fisc_system_dt"].Value.ToString();
                            _colaPasystfInfoStruct.FISCSystemUpdDt = cmd.Parameters["@fisc_system_upd_dt"].Value.ToString();
                            _colaPasystfInfoStruct.FISCIwkCheckDigits = cmd.Parameters["@fisc_iwk_check_digits"].Value.ToString();
                            _colaPasystfInfoStruct.FISCIwkChangeDt = cmd.Parameters["@fisc_iwk_change_dt"].Value.ToString();

                            _colaPasystfInfoStruct.TWINSystemDt = cmd.Parameters["@twin_system_dt"].Value.ToString();
                            _colaPasystfInfoStruct.TWINSystemUpdDt = cmd.Parameters["@twin_system_upd_dt"].Value.ToString();
                            _colaPasystfInfoStruct.TWINIwkCheckDigits = cmd.Parameters["@twin_iwk_check_digits"].Value.ToString();
                            _colaPasystfInfoStruct.TWINIwkChangeDt = cmd.Parameters["@twin_iwk_change_dt"].Value.ToString();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Info($"SyncColaPasysInfoData Exception : {ex.Message}");
                        return -9;
                    }
                }
            }
            return (returnCode);
        }

        public string DisplayColaPasysInfo()
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                foreach (var finfo in _colaPasystfInfoStruct.GetType().GetFields())
                {
                    sb.Append($"Name[{finfo.Name}],Value[{finfo.GetValue(_colaPasystfInfoStruct)}]{Environment.NewLine}");
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                Log.Info($"GetColaPasysInfo Exception : {ex.Message}");
                return string.Empty;
            }
        }

    }

}

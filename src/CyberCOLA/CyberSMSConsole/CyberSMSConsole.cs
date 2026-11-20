using Com.Cybersoft4u.CommonLib.DBUtils;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CommonLib.SMS;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Com.Cybersoft4u.Service.SMS.CyberSMSConsole
{
    public class CyberSMSConsole
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog
        private static readonly SingletonOfSMSServerInfo SingletonOfSMSServerInfo = SingletonOfSMSServerInfo.Instance;
        private static int _maxProcessCount = 100;
        private static int _maxParallelTaskCount = 5;
        private static string _startKey = string.Empty;
        private static bool _haveChangedStartKey = false;
        //Below variable dependent on Database define
        private static readonly string DatabaseKeyName = "TRAN_LOG_KEY";
        private static readonly string DataBaseDefineTranLogKeyListTranLogKeyName = "TranLogKey";
        private static readonly string DataBaseDefineTranLogKeyListRtnCodeName = "RtnCode";

        static void Main(string[] args)
        {
            var startValue = SingletonOfSMSServerInfo.GetSMSServerInfo().AppDataInfo.StartKey.Trim().ToUpper();
            _startKey = startValue.Equals("NOW") ? DateTime.Now.ToString("yyyyMMddHHmmss") : startValue;
            _maxProcessCount = Convert.ToInt16(SingletonOfSMSServerInfo.GetSMSServerInfo().AppDataInfo.MaxProcessCount.Trim());
            _maxParallelTaskCount = Convert.ToInt16(SingletonOfSMSServerInfo.GetSMSServerInfo().AppDataInfo.MaxParallelTaskCount.Trim());
            Log.Info($"Service start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            try
            {
                var sleepInterval = SingletonOfSMSServerInfo.GetSMSServerInfo().AppDataInfo.ThreadSleepTimeByMiliSec;
                _haveChangedStartKey = SingletonOfSMSServerInfo.GetSMSServerInfo().AppDataInfo.HaveChangedStartKey;
                Console.WriteLine("Press ESC to stop Process");
                do
                {
                    while (!Console.KeyAvailable)
                    {
                        var returnCode = CreateSMSData();
                        if (returnCode < 0)
                        {
                            Console.WriteLine("Application Error, Press ESC to stop Process");
                            break;
                        }
                        if (returnCode == 0)
                        {
                            Thread.Sleep(sleepInterval);
                        }
                    }
                } while (Console.ReadKey(true).Key != ConsoleKey.Escape);
                Log.Info($"Service stop  at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Start CyberSMSConsole Exception:{ex.Message}");
            }
            finally
            {
                Log.Info($"Start CyberSMSConsole completed at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }


            // Handler Of getDataTimer
            int CreateSMSData()
            {
                try
                {
                    Log.Info($"Start Process at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                    var getStoreProcedureName = @"SMS_GetProcessAuthLogByStartKey";
                    Log.Debug($"Start Key[{_startKey}] Process at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                    var getDataTableFromDatabase = StoreProcUtility.SMSGetProcessAuthLogByStartKey(getStoreProcedureName, _startKey, _maxProcessCount);
                    if (getDataTableFromDatabase == null)
                    {
                        Log.Info($"Get Database Data at : [{DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}] Null Data");
                        return -1;
                    }
                    if (getDataTableFromDatabase.Rows.Count == 0)
                    {
                        Log.Info($"Get Database Data at : [{DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}] No Data");
                        return 0;
                    }
                    Log.Info($"Get Database Data at : [{DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}] Have [{getDataTableFromDatabase.Rows.Count}] Row, Last Key[{getDataTableFromDatabase.Rows[getDataTableFromDatabase.Rows.Count - 1].Field<string>(DatabaseKeyName)}]");
                    if (_haveChangedStartKey)
                    {
                        _startKey = getDataTableFromDatabase.Rows[getDataTableFromDatabase.Rows.Count - 1].Field<string>(DatabaseKeyName);
                    }
                    var dataTableCountArray = new int[_maxParallelTaskCount];
                    List<DataTable> dataTableList = new List<DataTable>();
                    for (var index = 0; index < getDataTableFromDatabase.Rows.Count; index++)
                    {
                        var remainder = index % _maxParallelTaskCount;
                        dataTableCountArray[remainder]++;
                    }

                    var startIndex = 0;
                    foreach (int arrayIndex in dataTableCountArray)
                    {
                        if (arrayIndex == 0) continue;
                        var endIndex = startIndex + arrayIndex;
                        DataTable copyTable = getDataTableFromDatabase.Clone();
                        for (var rowIndex = startIndex; rowIndex < endIndex; rowIndex++)
                        {
                            copyTable.ImportRow(getDataTableFromDatabase.Rows[rowIndex]);
                        }
                        dataTableList.Add(copyTable);
                        startIndex = endIndex;
                    }
                    dataTableList.ForEach(x => Log.Info($"{x.Rows.Count}"));
                    dataTableList.ForEach(x => Log.Info($"Key[{x.Rows[0].Field<string>(DatabaseKeyName)}]"));
                    //if (Log.IsDebugEnabled)
                    //{
                    //    foreach (DataTable dataTable in dataTableList)
                    //    {
                    //        foreach (DataRow dataRow in dataTable.Rows)
                    //        {
                    //            Log.Debug($"Key Value[{dataRow[DatabaseKeyName]}]");
                    //            foreach (DataColumn column in dataTable.Columns)
                    //            {
                    //                Log.Debug(
                    //                    $"ColumnName[{column.ColumnName}], ColumnData[{dataRow[column].ToString()}]");
                    //            }
                    //        }
                    //    }
                    //}
                    ConcurrentBag<string> updateKeyList = new ConcurrentBag<string>();

                    Parallel.ForEach(dataTableList.ToArray(), dataTable =>
                    {
                        var dataTableKeyList = SMSCommUtility.SMSMessageSendToSMSServerReturnKeyList(SingletonOfSMSServerInfo.GetSMSServerInfo(), dataTable);
                        dataTableKeyList.ForEach(key => updateKeyList.Add(key));
                    });
                    if (updateKeyList.Count == 0)
                    {
                        Log.Info("updateKeyList Count is Zero");
                        return 0;
                    }

                    //Task<int>[] tasks = new Task<int>[dataTableList.Count];
                    //for (var index = 0; index < dataTableList.Count; index++)
                    //{
                    //    var index1 = index;
                    //    tasks[index] = Task<int>.Factory.StartNew(() => MQUtils.MQMessageSendToPRISM(SingletonOfSMSServerInfo.GetSMSServerInfo().MQServers.MQServer[0], dataTableList[index1]));
                    //}
                    //Task.WaitAll(tasks);
                    //for (var index = 0; index < dataTableList.Count; index++)
                    //{
                    //    Log.Info($"Task[{index}]- Result[{tasks[index].Result}]");
                    //}
                    //Update DataTable Set PROCESS_FLAG to Y
                    DataTable updateDataTable = new DataTable();
                    updateDataTable.Clear();
                    updateDataTable.Columns.Add(DataBaseDefineTranLogKeyListTranLogKeyName, typeof(string));
                    updateDataTable.Columns.Add(DataBaseDefineTranLogKeyListRtnCodeName, typeof(string));
                    //foreach (DataRow dataRow in getDataTableFromDatabase.Rows)
                    //{
                    //    DataRow newRow = updateDataTable.NewRow();
                    //    newRow[DataBaseDefineTranLogKeyListTranLogKeyName] = dataRow[DatabaseKeyName];
                    //    updateDataTable.Rows.Add(newRow);
                    //}
                    //returnKeyList.OrderBy(key => key).ToList().ForEach(key => updateDataTable.Rows.Add(key));
                    var orderUpdateKeyList = updateKeyList.ToList().OrderBy(key => key).ToList();
                    foreach (var keyString in orderUpdateKeyList)
                    {
                        var stringArrays = keyString.Split('-');
                        if (stringArrays.Length == 2)
                        {
                            updateDataTable.Rows.Add(stringArrays[0], stringArrays[1]);
                        }
                    }
                    if (updateDataTable.Rows.Count == 0) return 0;
                    var updateStoreProcedureName = @"SMS_UpdateProcessAuthLog";
                    var returnCode = StoreProcUtility.SMSUpdateProcessAuthLog(updateDataTable, updateStoreProcedureName, DataBaseDefineTranLogKeyListTranLogKeyName, DataBaseDefineTranLogKeyListRtnCodeName);
                    Log.Info($"Run SMS_UpdateProcessAuthLog Return Code [{returnCode}]");
                    Log.Info($"Process Completed at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                    return updateDataTable.Rows.Count;
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, $"CreateSMSData Exception:{ex.Message}");
                    return -1;
                }
            }
        }
    }
}

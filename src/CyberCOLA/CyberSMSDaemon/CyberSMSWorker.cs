using Com.Cybersoft4u.CommonLib.DBUtils;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CommonLib.SMS;
using Com.Cybersoft4u.Utils;
using Com.Cybersoft4u.Utils.NLog;
using System.Collections.Concurrent;
using System.Data;
using System.Timers;

namespace Com.Cybersoft4u.Service.SMS.CyberSMSDaemon
{
    /// <summary>
    /// Background daemon service that processes and delivers SMS messages.
    /// Runs as a daemon process to continuously monitor and process SMS notification requests from the transaction log.
    /// </summary>
    public class CyberSMSWorker : BackgroundService
    {
        private readonly ILogger<CyberSMSWorker> _logger;
        private readonly Logger _appLogger;
        private const string ServiceName = "CyberSMSDaemon";
        private static readonly SingletonOfSMSServerInfo SingletonOfSMSServerInfo = SingletonOfSMSServerInfo.Instance;
        private static int _maxProcessCount = 100;
        private static int _maxParallelTaskCount = 5;
        private static string _startKey = string.Empty;
        private static bool _haveChangedStartKey = false;
        //Below variable dependent on Database define
        private static readonly string DatabaseKeyName = "TRAN_LOG_KEY";
        private static readonly string DataBaseDefineTranLogKeyListTranLogKeyName = "TranLogKey";
        private static readonly string DataBaseDefineTranLogKeyListRtnCodeName = "RtnCode";
        private readonly System.Timers.Timer getDataTimer = new System.Timers.Timer();


        /// <summary>
        /// Initializes a new instance of the <see cref="CyberSMSWorker"/> class.
        /// Configures SMS daemon processing parameters including start key, process limits, and parallelism settings.
        /// </summary>
        /// <param name="logger">The logger instance provided by dependency injection for structured logging.</param>
        public CyberSMSWorker(ILogger<CyberSMSWorker> logger)
        {
            LoggerAdapter.ReBuild(new NLoggerProvider(), "NLog.config");
            _appLogger = LoggerAdapter.GetLogger("LoggerHandler[CyberSMSDaemon]");
            _logger = logger;
            var startValue = SingletonOfSMSServerInfo.GetSMSServerInfo().AppDataInfo.StartKey.Trim().ToUpper();
            _startKey = startValue.Equals("NOW") ? DateTime.Now.ToString("yyyyMMddHHmmss") : startValue;
            _maxProcessCount = Convert.ToInt16(SingletonOfSMSServerInfo.GetSMSServerInfo().AppDataInfo.MaxProcessCount.Trim());
            _maxParallelTaskCount = Convert.ToInt16(SingletonOfSMSServerInfo.GetSMSServerInfo().AppDataInfo.MaxParallelTaskCount.Trim());
        }

        /// <summary>
        /// Executes the SMS daemon worker startup sequence.
        /// Initializes the timer for periodic SMS data retrieval and processing in daemon mode.
        /// </summary>
        /// <param name="stoppingToken">Cancellation token to signal when the service should stop.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //while (!stoppingToken.IsCancellationRequested)
            //{
            //    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            //    await Task.Delay(1000, stoppingToken);
            //}
            string displayMessage = $"Service Name({ServiceName}) execute at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}";
            _appLogger.Info(displayMessage);
            try
            {
                _haveChangedStartKey = SingletonOfSMSServerInfo.GetSMSServerInfo().AppDataInfo.HaveChangedStartKey;
                getDataTimer.Elapsed += new ElapsedEventHandler(GetDataElapsed);
                getDataTimer.Interval = SingletonOfSMSServerInfo.GetSMSServerInfo().AppDataInfo.ThreadSleepTimeByMiliSec;
                getDataTimer.Start();
            }
            catch (Exception ex)
            {
                _appLogger.Fatal($"ExecuteAsync Service Name({ServiceName}) Exception:{ex.Message}", ex);
                throw new Exception("Application Error");
            }
            finally
            {
                _appLogger.Info($"ExecuteAsync Completed At : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }
            displayMessage = $"Service Name({ServiceName}) execute completed at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}";
            _appLogger.Info(displayMessage);
            //while (!stoppingToken.IsCancellationRequested)
            //{
            //    await Task.Delay(1000, stoppingToken);
            //}
            await Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            string displayMessage = $"Service Name({ServiceName}) stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}";
            _appLogger.Info(displayMessage);
            try
            {
                getDataTimer.Stop();
                displayMessage = $"StopAsync Service Name({ServiceName}) start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}";
                _appLogger.Info(displayMessage);
            }
            catch (Exception ex)
            {
                _appLogger.Fatal($"StopAsync CyberSMSDaemon Exception:{ex.Message}", ex);
            }
            displayMessage = $"Service Name({ServiceName}) stop completed at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}";
            _appLogger.Info(displayMessage);
            //return Task.CompletedTask;
            await base.StopAsync(stoppingToken);
        }

        //public override Task StartAsync(CancellationToken cancellationToken)
        //{
        //    string displayMessage = $"Service Name({ServiceName}) start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}";
        //    _appLogger.Info(displayMessage);
        //    return Task.CompletedTask;
        //}

        //public override Task StopAsync(CancellationToken stoppingToken)
        //{
        //    _logger.LogInformation($"Worker StopAsync at: {DateTime.Now.ToString()}");
        //    return Task.CompletedTask;
        //}
        // Handler Of getDataTimer

        void GetDataElapsed(object? sender, EventArgs e)
        {
            try
            {
                var returnCode = CreateSMSData();
                if (returnCode < 0)
                {
                    var displayMessage = $"Application Error At : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}";
                    _appLogger.Info(displayMessage);
                }
            }
            catch (Exception ex)
            {
                _appLogger.Fatal($"GetDataElapsed Exception:{ex.Message}", ex);
            }
        }
        //Create SMS Message For SMS Server
        int CreateSMSData()
        {
            try
            {

                _appLogger.Info("Start Process At : " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                var getStoreProcedureName = @"SMS_GetProcessAuthLogByStartKey";
                _appLogger.Info($"Start Key[{_startKey}] Process At : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                var getDataTableFromDatabase = StoreProcUtility.SMSGetProcessAuthLogByStartKey(getStoreProcedureName, _startKey, _maxProcessCount);
                if (getDataTableFromDatabase == null)
                {
                    _appLogger.Info($"Get Database Data At : [{DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}] Null Data");
                    return -1;
                }
                if (getDataTableFromDatabase.Rows.Count == 0)
                {
                    _appLogger.Info($"Get Database Data At : [{DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}] No Data");
                    return 0;
                }
                _appLogger.Info($"Get Database Data At : [{DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}] Have [{getDataTableFromDatabase.Rows.Count}] Row, Last Key[{getDataTableFromDatabase.Rows[getDataTableFromDatabase.Rows.Count - 1].Field<string>(DatabaseKeyName)}]");
                if (_haveChangedStartKey)
                {
                    _startKey = getDataTableFromDatabase.Rows[getDataTableFromDatabase.Rows.Count - 1].Field<string>(DatabaseKeyName) ?? string.Empty;
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
                dataTableList.ForEach(x => _appLogger.Info($"{x.Rows.Count}"));
                dataTableList.ForEach(x => _appLogger.Info($"Key[{x.Rows[0].Field<string>(DatabaseKeyName)}]"));
                //if (Logger.IsDebugEnabled)
                //{
                //    foreach (DataTable dataTable in dataTableList)
                //    {
                //        foreach (DataRow dataRow in dataTable.Rows)
                //        {
                //            Logger.Debug($"Key Value[{dataRow[DatabaseKeyName]}]");
                //            foreach (DataColumn column in dataTable.Columns)
                //            {
                //                Logger.Debug($"ColumnName[{column.ColumnName}], ColumnData[{dataRow[column].ToString()}]");
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
                    _appLogger.Info("updateKeyList Count is Zero");
                    return 0;
                }

                DataTable updateDataTable = new DataTable();
                updateDataTable.Clear();
                updateDataTable.Columns.Add(DataBaseDefineTranLogKeyListTranLogKeyName, typeof(string));
                updateDataTable.Columns.Add(DataBaseDefineTranLogKeyListRtnCodeName, typeof(string));
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
                _appLogger.Info($"Run SMSUpdateProcessAuthLog Return Code [{returnCode}]");
                _appLogger.Info($"Process Completed At : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                return updateDataTable.Rows.Count;
            }
            catch (Exception ex)
            {
                _appLogger.Fatal($"CreateSMSData Exception:{ex.Message}");
                return -1;
            }
        }
    }
}

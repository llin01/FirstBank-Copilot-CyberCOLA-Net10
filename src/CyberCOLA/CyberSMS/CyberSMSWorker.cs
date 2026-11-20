using Com.Cybersoft4u.CommonLib.DBUtils;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CommonLib.SMS;
using Com.Cybersoft4u.Utils;
using Com.Cybersoft4u.Utils.NLog;
using System.Collections.Concurrent;
using System.Data;
using System.Timers;
using Logger = Com.Cybersoft4u.Utils.Logger;

namespace Com.Cybersoft4u.Service.SMS.CyberSMS
{
    /// <summary>
    /// Background worker service that manages SMS message processing and delivery.
    /// Periodically retrieves transaction log data, generates SMS messages, and handles SMS delivery through configured channels.
    /// Uses a timer-based polling mechanism to process pending SMS records from the database.
    /// </summary>
    public class CyberSMSWorker : BackgroundService
    {
        private readonly ILogger<CyberSMSWorker> _logger;
        private readonly Logger _appLogger;
        private const string ServiceName = "CyberSMS";
        private static readonly SingletonOfSMSServerInfo SingletonOfSMSServerInfo = SingletonOfSMSServerInfo.Instance;
        private static int _maxProcessCount = 100;
        private static int _maxParallelTaskCount = 5;
        private static string _startKey = string.Empty;
        private static bool _haveChangedStartKey = false;

        private static readonly string DatabaseKeyName = "TRAN_LOG_KEY";
        private static readonly string DataBaseDefineTranLogKeyListTranLogKeyName = "TranLogKey";
        private static readonly string DataBaseDefineTranLogKeyListRtnCodeName = "RtnCode";
        private readonly System.Timers.Timer getDataTimer = new System.Timers.Timer();

        /// <summary>
        /// Initializes a new instance of the <see cref="CyberSMSWorker"/> class.
        /// Loads SMS server configuration and initializes processing parameters including start key, max process count, and parallelism settings.
        /// </summary>
        /// <param name="logger">The logger instance provided by dependency injection for structured logging.</param>
        public CyberSMSWorker(ILogger<CyberSMSWorker> logger)
        {
            _logger = logger;

            // Initialize NLog uniformly
            LoggerAdapter.ReBuild(new NLoggerProvider(), "NLog.config");
            _appLogger = LoggerAdapter.GetLogger($"LoggerHandler[{ServiceName}]");

            var smsInfo = SingletonOfSMSServerInfo.GetSMSServerInfo();
            var startValue = smsInfo.AppDataInfo.StartKey.Trim().ToUpper();
            _startKey = startValue.Equals("NOW") ? DateTime.Now.ToString("yyyyMMddHHmmss") : startValue;
            _maxProcessCount = Convert.ToInt16(smsInfo.AppDataInfo.MaxProcessCount.Trim());
            _maxParallelTaskCount = Convert.ToInt16(smsInfo.AppDataInfo.MaxParallelTaskCount.Trim());
        }

        /// <summary>
        /// Executes the SMS worker service startup sequence.
        /// Starts a timer to periodically poll and process SMS records from the database.
        /// Runs continuously until cancellation is requested.
        /// </summary>
        /// <param name="stoppingToken">Cancellation token to signal when the service should stop.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _appLogger.Info($"Unified SMS Service Name({ServiceName}) execute at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");

            try
            {
                // Start SMS data processing timer
                _haveChangedStartKey = SingletonOfSMSServerInfo.GetSMSServerInfo().AppDataInfo.HaveChangedStartKey;
                getDataTimer.Elapsed += new ElapsedEventHandler(GetDataElapsed);
                getDataTimer.Interval = SingletonOfSMSServerInfo.GetSMSServerInfo().AppDataInfo.ThreadSleepTimeByMiliSec;
                getDataTimer.Start();

                _appLogger.Info($"{ServiceName} Data Timer started with interval: {getDataTimer.Interval}ms at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }
            catch (Exception ex)
            {
                _appLogger.Fatal($"ExecuteAsync Service Name({ServiceName}) Exception: {ex.Message}", ex);
            }

            _appLogger.Info($"Service Name({ServiceName}) start completed at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");

            // Keep running until cancellation signal is received
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        /// <summary>
        /// Gracefully stops the SMS worker service.
        /// Stops the data processing timer and completes any pending operations.
        /// </summary>
        /// <param name="stoppingToken">Cancellation token to signal when the stop operation should complete.</param>
        /// <returns>A task that represents the asynchronous stop operation.</returns>
        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _appLogger.Info($"Service Name({ServiceName}) stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");

            try
            {
                // Stop SMS data processing timer
                _appLogger.Info($"{ServiceName} Data Timer stopping at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                getDataTimer.Stop();
            }
            catch (Exception ex)
            {
                _appLogger.Fatal($"StopAsync Service Name({ServiceName}) Exception: {ex.Message}", ex);
            }

            _appLogger.Info($"Service Name({ServiceName}) stop completed at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            await base.StopAsync(stoppingToken);
        }

        private void GetDataElapsed(object? sender, EventArgs e)
        {
            try
            {
                var returnCode = CreateSMSData();
                if (returnCode < 0)
                {
                    _appLogger.Error($"Application Error in CreateSMSData at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                }
            }
            catch (Exception ex)
            {
                _appLogger.Fatal($"GetDataElapsed Exception: {ex.Message}", ex);
            }
        }

        private int CreateSMSData()
        {
            try
            {
                _appLogger.Info($"Start Process Auth Log at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                var getStoreProcedureName = @"SMS_GetProcessAuthLogByStartKey";

                var getDataTableFromDatabase = StoreProcUtility.SMSGetProcessAuthLogByStartKey(getStoreProcedureName, _startKey, _maxProcessCount);

                if (getDataTableFromDatabase == null)
                {
                    _appLogger.Info($"Get Database Data: Null at {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                    return -1;
                }

                if (getDataTableFromDatabase.Rows.Count == 0)
                {
                    return 0;
                }

                _appLogger.Info($"Fetched {getDataTableFromDatabase.Rows.Count} rows. Last Key: {getDataTableFromDatabase.Rows[getDataTableFromDatabase.Rows.Count - 1].Field<string>(DatabaseKeyName)}");

                if (_haveChangedStartKey)
                {
                    _startKey = getDataTableFromDatabase.Rows[getDataTableFromDatabase.Rows.Count - 1].Field<string>(DatabaseKeyName) ?? string.Empty;
                }

                // Partition data for parallel processing
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

                // Prepare results for DB update
                DataTable updateDataTable = new DataTable();
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

                _appLogger.Info($"SMSUpdateProcessAuthLog Return Code [{returnCode}] at {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                return updateDataTable.Rows.Count;
            }
            catch (Exception ex)
            {
                _appLogger.Fatal($"CreateSMSData Exception: {ex.Message}", ex);
                return -1;
            }
        }
    }
}

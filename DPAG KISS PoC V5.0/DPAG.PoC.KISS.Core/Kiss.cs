using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.Collections.Specialized;
using System.Threading;
using System.Data;
using System.Data.SqlClient;

namespace DPAG.PoC.KISS.Core
{
    /// <summary>
    /// 
    /// </summary>
    public enum KissTransactionMode
    {
        DTC,
        MSMQ
    }

    /// <summary>
    /// 
    /// </summary>
    public enum KissTimeout
    {
        NEIGHBORHOOD_SQL_GET,
        DISTRIBUTION_SQL_RETRIEVE,
        DISTRIBUTION_SQL_RETRIEVE_ALL,
        DISTRIBUTION_SQL_PROCESS,
        SD_TTRQ,
        SD_TTBR,
        SD_RECEIVE_TIMEOUT,
        SD_SQL_STORAGE,
        SD_DTC_STORAGE,
        PO_TTRQ,
        PO_TTBR,
        PO_RECEIVE_TIMEOUT,
        PO_SQL_STORAGE,
        PO_SQL_RECOVERY,
        PO_DTC_STORAGE,
        PO_DTC_RECOVERY,
        PO_CONTROL_TTRQ,
        PO_CONTROL_TTBR,
        PO_CONTROL_RECEIVE_TIMEOUT,
        BUNDLE_TTRQ,
        BUNDLE_TTBR,
        BUNDLE_RECEIVE_TIMEOUT,
        BUNDLE_SSSB,
        BUNDLE_SQL_STORAGE,
        BUNDLE_SQL_RECOVERY,
        BUNDLE_DTC_DISTRIBUTION,
        BUNDLE_DTC_STORAGE,
        BUNDLE_DTC_RECOVERY,
        BUNDLE_CONTROL_TTRQ,
        BUNDLE_CONTROL_TTBR,
        BUNDLE_CONTROL_RECEIVE_TIMEOUT,
        KISS_SSSB_PC_REGISTRATION,
        KISS_SSSB_PING,
        KISS_SSSB_STAGELIST,
        KISS_SQL_PC_REGISTRATION,
        KISS_SQL_PING,
        KISS_SQL_STAGELIST,
        KISS_SQL_GARBAGE_COLLECTION
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class Kiss
    {
        private const int SLEEP_KISS_JOBS = 1 * 1000;
        private const char SEPARATOR_SQL_INSTANCE = '\\';
        private const char SEPARATOR_PING_SCHEDULE = ',';
        private const string NAME_KISS_CONNECTION = "Kiss";
        private const string KISS_COMMON_CONFIG_PATH = "kiss/common";
        private const string KISS_DIRECTORIES_CONFIG_PATH = "kiss/directories.{0}";
        private const string KISS_JOBS_CONFIG_PATH = "kiss/jobs.{0}";
        private const string KISS_TIMEOUTS_CONFIG_PATH = "kiss/timeouts.{0}";
        private const string CONFIG_PROFILE = "Profile";
        private const string CONFIG_PING_SCHEDULE = "Ping Schedule";
        private const string CONFIG_STANDING_DATA_SCHEDULE = "Standing Data Schedule";
        private const string PROCEDURE_PING = "[kiss].[Ping]";
        private const string PROCEDURE_PC_REGISTRATION = "[kiss].[RegisterPC]";

        private static NameValueCollection _kissCommon,
                                           _kissDirectories,
                                           _kissJobs,
                                           _kissTimeouts;
        private static Kiss _jobs;

        private bool _executeJobs;
        private int _msmqCapacity;
        private string _sql, 
                       _clientId;
        private Thread _backgroundWorker;

        static Kiss()
        {
            _kissCommon = (NameValueCollection)ConfigurationManager.GetSection(KISS_COMMON_CONFIG_PATH);

            _kissDirectories = (NameValueCollection)ConfigurationManager.
                GetSection(string.Format(KISS_DIRECTORIES_CONFIG_PATH, _kissCommon[CONFIG_PROFILE]));

            _kissJobs = (NameValueCollection)ConfigurationManager.
                GetSection(string.Format(KISS_JOBS_CONFIG_PATH, _kissCommon[CONFIG_PROFILE]));
            
            _kissTimeouts = (NameValueCollection)ConfigurationManager.
                GetSection(string.Format(KISS_TIMEOUTS_CONFIG_PATH, _kissCommon[CONFIG_PROFILE]));
        }

        public static string GetConnection(string sql)
        {
            if (string.IsNullOrEmpty(sql))
                sql = string.Empty;
            else if (sql[0] != SEPARATOR_SQL_INSTANCE)
                sql = SEPARATOR_SQL_INSTANCE + sql;

            return string.Format(ConfigurationManager.ConnectionStrings[NAME_KISS_CONNECTION].ConnectionString, sql);
        }

        public static string GetDirectory(string name)
        {
            return _kissDirectories[name];
        }

        public static int GetTimeout(KissTimeout timeoutType)
        {
            return int.Parse(_kissTimeouts[timeoutType.ToString()]);
        }

        public static void StartJobs(string sql, string clientId, int msmqCapacity)
        {
            if (_jobs == null)
            {
                _jobs = new Kiss(sql, clientId, msmqCapacity);
                _jobs.StartBackgroundWorker();
            }
        }

        public static void StopJobs()
        {
            if (_jobs != null)
            {
                _jobs.StopBackgroundWorker();
                _jobs = null;
            }
        }

        public Kiss(string sql, string clientId, int msmqCapacity)
        {
            _sql = sql;
            _clientId = clientId;
            _msmqCapacity = msmqCapacity;
        }

        public void Ping()
        {
            var dialogLifetime = Kiss.GetTimeout(KissTimeout.KISS_SSSB_PING);
            var sqltoPing = Kiss.GetTimeout(KissTimeout.KISS_SQL_PING);
            
            using (var kiss = new SqlConnection(Kiss.GetConnection(_sql)))
            using (var ping = new SqlCommand(PROCEDURE_PING, kiss) { CommandTimeout = sqltoPing, 
                                                                     CommandType = CommandType.StoredProcedure })
            {
                ping.Parameters.AddWithValue("@dialog_lifetime", dialogLifetime);

                kiss.Open();
                ping.ExecuteNonQuery();
                kiss.Close();
            }
        }

        public void RegisterPC(bool deregister)
        {
            var dialogLifetime = Kiss.GetTimeout(KissTimeout.KISS_SSSB_PC_REGISTRATION);
            var sqltoRegistration = Kiss.GetTimeout(KissTimeout.KISS_SQL_PC_REGISTRATION);
            
            using (var kiss = new SqlConnection(Kiss.GetConnection(_sql)))
            using (var pcRegistration = 
                       new SqlCommand(PROCEDURE_PC_REGISTRATION, kiss) { CommandTimeout = sqltoRegistration, 
                                                                         CommandType = CommandType.StoredProcedure })
            {
                pcRegistration.Parameters.AddWithValue("@deregister", deregister ? 1 : 0);
                pcRegistration.Parameters.AddWithValue("@dialog_lifetime", dialogLifetime);

                kiss.Open();
                pcRegistration.ExecuteNonQuery();
                kiss.Close();
            }
        }

        private void StartBackgroundWorker()
        {
            if (_backgroundWorker == null)
            {
                _executeJobs = true;
                _backgroundWorker = new Thread(BackgroundWorker) { IsBackground = true };
                _backgroundWorker.Start();
            }
        }

        private void StopBackgroundWorker()
        {
            if (_backgroundWorker != null)
            {
                _executeJobs = false;
                _backgroundWorker.Join();
                _backgroundWorker = null;
            }
        }

        private void BackgroundWorker()
        {
            var rnd = new Random();
            var pingDelayMin = int.Parse(_kissJobs[CONFIG_PING_SCHEDULE].Split(SEPARATOR_PING_SCHEDULE)[0]);
            var pingDelayMax = int.Parse(_kissJobs[CONFIG_PING_SCHEDULE].Split(SEPARATOR_PING_SCHEDULE)[1]);
            var lastPing = DateTime.Today.AddDays(-1);
            var pingCounter = 0;
            var pingCounterMax = pingDelayMax > 0 ? rnd.Next(pingDelayMin, pingDelayMax) : pingDelayMax;
            var standingDataCounter = 0;
            var standingDataCounterMax = int.Parse(_kissJobs[CONFIG_STANDING_DATA_SCHEDULE]);

            do
            {
                Thread.Sleep(SLEEP_KISS_JOBS);

                try
                {
                    if (pingCounterMax > 0 && ++pingCounter == pingCounterMax)
                    {
                        pingCounter = 0;
                        pingCounterMax = rnd.Next(pingDelayMin, pingDelayMax);

                        if (lastPing != DateTime.Today)
                        {
                            Ping();
                            lastPing = DateTime.Today;
                        }
                    }

                    if (standingDataCounterMax > 0 && ++standingDataCounter == standingDataCounterMax)
                    {
                        standingDataCounter = 0;
                        new StandingDataManager(_sql, _msmqCapacity).ProcessInstructions(KissTransactionMode.MSMQ);
                    }
                }
                catch
                {
                }
            } while (_executeJobs);
        }
    }
}

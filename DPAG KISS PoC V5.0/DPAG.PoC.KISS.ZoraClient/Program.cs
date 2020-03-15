using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Configuration;
using System.Threading;
using DPAG.PoC.KISS.Core;

namespace DPAG.PoC.KISS.ZoraClient
{
    /// <summary>
    /// 
    /// </summary>
    internal static class Program
    {
        private const char SEPARATOR_DISTR_DELAY = ',';
        private const string CONFIG_SQL_INSTANCE = "SQL Instance";
        private const string CONFIG_MSMQ_CAPACITY = "MSMQ Capacity";
        private const string CONFIG_PO_CONCURRENCY = "Persistent Object Concurrency";
        private const string CONFIG_PO_SIZE = "Persistent Object Size";
        private const string CONFIG_BUNDLE_SIZE = "Bundle Size";
        private const string CONFIG_DISTRIBUTION_COUNT = "Distribution Count";
        private const string CONFIG_DISTRIBUTION_DELAY = "Distribution Delay";
        private const string CONFIG_LISTENING_MODE = "Listening Mode";

        private static void Main(string[] args)
        {
            var sql = args.Length > 0 ? args[0] : "SQLEXPRESS";
            var runZoraClient = true;
            string[] lastAllBundleIds = new string[0], 
                     lastAllPoIds = new string[0],
                     lastAllSdIds = new string[0];
            var msmqCapacity = int.Parse(ConfigurationManager.AppSettings[CONFIG_MSMQ_CAPACITY]);
            var sdListener = new StandingDataListener(sql, msmqCapacity);
            var poListener = new PersistentObjectListener(sql, msmqCapacity);
            var bundleListener = new BundleListener(sql, msmqCapacity);
            var poControlListener = new PersistentObjectControlListener(sql, msmqCapacity);
            var bundleControlListener = new BundleControlListener(sql, msmqCapacity);

            do
            {
                Console.Clear();
                
                Console.WriteLine("DPAG KISS PoC Zora Client '{0} / {1} / {2}'", 
                                  sql, new Neighborhood(sql).GetLocalClient(), Process.GetCurrentProcess().Id);
                
                Console.WriteLine();

                Console.WriteLine("a. Register PC");
                Console.WriteLine("b. Deregister PC");
                Console.WriteLine("c. Install MSMQ");
                Console.WriteLine("d. Uninstall MSMQ");
                Console.WriteLine("e. Start Background Work");
                Console.WriteLine("f. Stop Background Work");
                Console.WriteLine("g. Distribute Bundles");
                Console.WriteLine("h. Distribute Persistent Objects");
                Console.WriteLine("i. Recover Bundles");
                Console.WriteLine("j. Recover Persistent Objects");
                Console.WriteLine("k. Remove Persistent Objects");
                Console.WriteLine("l. Transmit Bundles");
                Console.WriteLine("w. Clear Report Cache");
                Console.WriteLine("x. Report Bundles");
                Console.WriteLine("y. Report Persistent Objects");
                Console.WriteLine("z. Report Standing Data");
                
                Console.WriteLine();
                Console.WriteLine("0. Exit Application");
                Console.WriteLine();

                try
                {
                    switch (Console.ReadKey(true).KeyChar)
                    {
                        case 'a':
                            RegisterPC(sql);
                            break;

                        case 'b':
                            DeregisterPC(sql);
                            break;

                        case 'c':
                            InstallMSMQ(sql);
                            break;

                        case 'd':
                            DeinstallMSMQ(sql);
                            break;

                        case 'e':
                            StartBackgroundWork(sql, 
                                                sdListener, poListener, bundleListener, 
                                                poControlListener, bundleControlListener);
                            break;

                        case 'f':
                            StopBackgroundWork(true,
                                               sdListener, poListener, bundleListener,
                                               poControlListener, bundleControlListener);
                            break;

                        case 'g':
                            DistributeBundles(sql);
                            break;

                        case 'h':
                            DistributePersistentObjects(sql);
                            break;

                        case 'i':
                            RecoverBundles(sql);
                            break;

                        case 'j':
                            RecoverPersistentObjects(sql);
                            break;

                        case 'k':
                            RemovePersistentObjects(sql);
                            break;

                        case 'l':
                            TransmitBundles(sql);
                            break;

                        case 'w':
                            ClearReportCache(ref lastAllBundleIds, ref lastAllPoIds, ref lastAllSdIds);
                            break;

                        case 'x':
                            ReportBundles(sql, ref lastAllBundleIds);
                            break;

                        case 'y':
                            ReportPersistentObjects(sql, ref lastAllPoIds);
                            break;

                        case 'z':
                            ReportStandingData(sql, ref lastAllSdIds);
                            break;

                        case '0':
                            runZoraClient = false;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("\n\nCaught exception: {0}.\n\n", ex);
                    Console.WriteLine("Press any key to continue.");
                    
                    Console.ReadKey();
                }
            } while (runZoraClient);

            try
            {
                StopBackgroundWork(false, 
                                   sdListener, poListener, bundleListener, 
                                   poControlListener, bundleControlListener);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Caught exception while stopping KISS jobs and MSMQ listeners: {0}.\n\n", ex);
                Console.WriteLine("Press any key to continue.");

                Console.ReadKey();
            }
        }

        private static void RegisterPC(string sql)
        {
            var msmqCapacity = int.Parse(ConfigurationManager.AppSettings[CONFIG_MSMQ_CAPACITY]);
            var clientId = new Neighborhood(sql).GetLocalClient();
            var kiss = new Kiss(sql, clientId, msmqCapacity);
            var bundleManager = new BundleManager(sql, clientId, msmqCapacity);
            var poManager = new PersistentObjectManager(sql, msmqCapacity);
            var sdManager = new StandingDataManager(sql, msmqCapacity);

            Console.WriteLine("Registering PC...");

            kiss.RegisterPC(false);

            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
        }

        private static void DeregisterPC(string sql)
        {
            var msmqCapacity = int.Parse(ConfigurationManager.AppSettings[CONFIG_MSMQ_CAPACITY]);
            var clientId = new Neighborhood(sql).GetLocalClient();
            var kiss = new Kiss(sql, clientId, msmqCapacity);
            var bundleManager = new BundleManager(sql, clientId, msmqCapacity);
            var poManager = new PersistentObjectManager(sql, msmqCapacity);
            var sdManager = new StandingDataManager(sql, msmqCapacity);

            Console.WriteLine("Deregistering PC...");

            kiss.RegisterPC(true);

            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
        }

        private static void InstallMSMQ(string sql)
        {
            var msmqCapacity = int.Parse(ConfigurationManager.AppSettings[CONFIG_MSMQ_CAPACITY]);
            var clientId = new Neighborhood(sql).GetLocalClient();
            var bundleManager = new BundleManager(sql, clientId, msmqCapacity);
            var poManager = new PersistentObjectManager(sql, msmqCapacity);
            var sdManager = new StandingDataManager(sql, msmqCapacity);

            Console.WriteLine("Installing MSMQ...");

            bundleManager.InstallMSMQ();
            poManager.InstallMSMQ();
            sdManager.InstallMSMQ();

            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
        }

        private static void DeinstallMSMQ(string sql)
        {
            var msmqCapacity = int.Parse(ConfigurationManager.AppSettings[CONFIG_MSMQ_CAPACITY]);
            var clientId = new Neighborhood(sql).GetLocalClient();
            var bundleManager = new BundleManager(sql, clientId, msmqCapacity);
            var poManager = new PersistentObjectManager(sql, msmqCapacity);
            var sdManager = new StandingDataManager(sql, msmqCapacity);

            Console.WriteLine("Deinstalling MSMQ...");

            bundleManager.DeinstallMSMQ();
            poManager.DeinstallMSMQ();
            sdManager.DeinstallMSMQ();

            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
        }

        private static void StartBackgroundWork(string sql, 
                                                StandingDataListener sdListener,
                                                PersistentObjectListener poListener,
                                                BundleListener bundleListener,
                                                PersistentObjectControlListener poControlListener,
                                                BundleControlListener bundleControlListener)
        {
            var msmqCapacity = int.Parse(ConfigurationManager.AppSettings[CONFIG_MSMQ_CAPACITY]);
            var clientId = new Neighborhood(sql).GetLocalClient();
            
            Console.WriteLine("Starting Background Work...");

            StartListener(sdListener, poListener, bundleListener, poControlListener, bundleControlListener);
            Kiss.StartJobs(sql, clientId, msmqCapacity);

            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
        }

        private static void StopBackgroundWork(bool interactive,
                                               StandingDataListener sdListener,
                                               PersistentObjectListener poListener,
                                               BundleListener bundleListener,
                                               PersistentObjectControlListener poControlListener,
                                               BundleControlListener bundleControlListener)
        {
            if (interactive)
                Console.WriteLine("Stopping Background Work...");

            Kiss.StopJobs();
            StopListener(sdListener, poListener, bundleListener, poControlListener, bundleControlListener);

            if (interactive)
            {
                Console.WriteLine("Press any key to continue.");
                Console.ReadKey();
            }
        }

        private static void DistributeBundles(string sql)
        {
            Bundle bundle;
            var bundleSize = int.Parse(ConfigurationManager.AppSettings[CONFIG_BUNDLE_SIZE]);
            var msmqCapacity = int.Parse(ConfigurationManager.AppSettings[CONFIG_MSMQ_CAPACITY]);
            var distrCount = int.Parse(ConfigurationManager.AppSettings[CONFIG_DISTRIBUTION_COUNT]);
            var distrDelay = ConfigurationManager.AppSettings[CONFIG_DISTRIBUTION_DELAY].Split(SEPARATOR_DISTR_DELAY);
            var clientId = new Neighborhood(sql).GetLocalClient();
            var rnd = new Random();
            var bundleData = new byte[bundleSize];

            Console.WriteLine("Distributing {0} Bundle(s)...", distrCount);
            
            for (var i = 0; i < distrCount; i++)
            {
                for (var j = 0; j < bundleData.Length; j++)
                    bundleData[j] = (byte)rnd.Next(256);

                bundleData = bundleData.Length > 0 ? bundleData : null;
                bundle = new Bundle(sql, clientId, msmqCapacity, bundleData);
                bundle.Distribute(KissTransactionMode.DTC);

                if (distrCount > 1 && int.Parse(distrDelay[1]) > 0)
                    Thread.Sleep(rnd.Next(int.Parse(distrDelay[0]), int.Parse(distrDelay[1]) + 1));
            }

            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
        }

        private static void DistributePersistentObjects(string sql)
        {
            string newId;
            PersistentObject persistentObject;
            var poConcurrency = int.Parse(ConfigurationManager.AppSettings[CONFIG_PO_CONCURRENCY]);
            var poSize = int.Parse(ConfigurationManager.AppSettings[CONFIG_PO_SIZE]);
            var msmqCapacity = int.Parse(ConfigurationManager.AppSettings[CONFIG_MSMQ_CAPACITY]);
            var distrCount = int.Parse(ConfigurationManager.AppSettings[CONFIG_DISTRIBUTION_COUNT]);
            var distrDelay = ConfigurationManager.AppSettings[CONFIG_DISTRIBUTION_DELAY].Split(SEPARATOR_DISTR_DELAY);
            var clientId = new Neighborhood(sql).GetLocalClient();
            var rnd = new Random();
            var poData = new byte[poSize];

            Console.WriteLine("Distributing {0} Persistent Object(s)...", distrCount);
            
            for (var i = 0; i < distrCount; i++)
            {
                for (var j = 0; j < poData.Length; j++)
                    poData[j] = (byte)rnd.Next(256);

                if (poConcurrency > 0)
                {
                    newId = rnd.Next(1, poConcurrency + 1) == 1 ?
                        "42" : string.Format("{0} {1}", clientId, rnd.Next().ToString());
                }
                else
                    newId = string.Format("{0} {1}", clientId, rnd.Next().ToString());

                poData = poData.Length > 0 ? poData : null;
                persistentObject = new PersistentObject(sql, newId, msmqCapacity, poData);
                persistentObject.Distribute(KissTransactionMode.MSMQ);

                if (distrCount > 1 && int.Parse(distrDelay[1]) > 0)
                    Thread.Sleep(rnd.Next(int.Parse(distrDelay[0]), int.Parse(distrDelay[1]) + 1));
            }
            
            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
        }

        private static void RecoverBundles(string sql)
        {
            var msmqCapacity = int.Parse(ConfigurationManager.AppSettings[CONFIG_MSMQ_CAPACITY]);
            var clientId = new Neighborhood(sql).GetLocalClient();
            var bundleManager = new BundleManager(sql, clientId, msmqCapacity);

            Console.WriteLine("Recovering Bundles...");

            bundleManager.RecoverBundles();

            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
        }

        private static void RecoverPersistentObjects(string sql)
        {
            var msmqCapacity = int.Parse(ConfigurationManager.AppSettings[CONFIG_MSMQ_CAPACITY]);
            var poManager = new PersistentObjectManager(sql, msmqCapacity);

            Console.WriteLine("Recovering Persistent Objects...");

            poManager.RecoverPersistentObjects();

            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
        }

        private static void RemovePersistentObjects(string sql)
        {
            PersistentObject persistentObject;
            var msmqCapacity = int.Parse(ConfigurationManager.AppSettings[CONFIG_MSMQ_CAPACITY]);
            var allIds = PersistentObject.RetrieveAll(sql);

            Console.WriteLine("Removing {0} Persistent Object(s)...", allIds.Length);

            foreach (var id in allIds)
            {
                persistentObject = new PersistentObject(sql, id, msmqCapacity);
                persistentObject.Remove(KissTransactionMode.MSMQ);
            }

            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
        }

        private static void TransmitBundles(string sql)
        {
            var msmqCapacity = int.Parse(ConfigurationManager.AppSettings[CONFIG_MSMQ_CAPACITY]);
            var clientId = new Neighborhood(sql).GetLocalClient();
            var bundleManager = new BundleManager(sql, clientId, msmqCapacity);

            Console.WriteLine("Collecting and transmitting Bundles...");

            bundleManager.TransmitBundles();

            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
        }

        private static void ClearReportCache(ref string[] lastAllBundleIds,
                                             ref string[] lastAllPoIds,
                                             ref string[] lastAllSdIds)
        {
            Console.WriteLine("Clearing Report Cache...");

            lastAllBundleIds = new string[0];
            lastAllPoIds = new string[0];
            lastAllSdIds = new string[0];

            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
        }

        private static void ReportBundles(string sql, ref string[] lastAllIds)
        {
            Bundle bundle;
            var msmqCapacity = int.Parse(ConfigurationManager.AppSettings[CONFIG_MSMQ_CAPACITY]);
            var allIds = Bundle.RetrieveAll(sql);

            Console.WriteLine("Retrieving Bundles...\n");
            
            foreach (var id in allIds)
            {
                if (Array.BinarySearch(lastAllIds, id) >= 0)
                    continue;

                bundle = new Bundle(sql, long.Parse(id.Split(',')[0]), id.Split(',')[1], msmqCapacity);
                bundle.Retrieve();

                Console.WriteLine("Retrieved BUNDLE from SQL ({0} bytes)", bundle.Length);
                Console.WriteLine(bundle);
                Console.WriteLine();
            }

            lastAllIds = allIds;
            Array.Sort(lastAllIds);

            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
        }

        private static void ReportPersistentObjects(string sql, ref string[] lastAllIds)
        {
            PersistentObject persistentObject;
            var msmqCapacity = int.Parse(ConfigurationManager.AppSettings[CONFIG_MSMQ_CAPACITY]);
            var allIds = PersistentObject.RetrieveAll(sql);

            Console.WriteLine("Retrieving Persistent Objects...\n");

            foreach (var id in allIds)
            {
                if (Array.BinarySearch(lastAllIds, id) >= 0)
                    continue;

                persistentObject = new PersistentObject(sql, id, msmqCapacity);
                persistentObject.Retrieve();

                Console.WriteLine("Retrieved PO from SQL ({0} bytes)", persistentObject.Length);
                Console.WriteLine(persistentObject);
                Console.WriteLine();
            }

            lastAllIds = allIds;
            Array.Sort(lastAllIds);

            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
        }

        private static void ReportStandingData(string sql, ref string[] lastAllIds)
        {
            StandingData standingData;
            var msmqCapacity = int.Parse(ConfigurationManager.AppSettings[CONFIG_MSMQ_CAPACITY]);
            var allIds = StandingData.RetrieveAll(sql);

            Console.WriteLine("Retrieving Standing Data...\n");

            foreach (var id in allIds)
            {
                if (Array.BinarySearch(lastAllIds, id) >= 0)
                    continue;

                standingData = new StandingData(sql, long.Parse(id), msmqCapacity);
                standingData.Retrieve();

                Console.WriteLine("Retrieved SD from SQL ({0} bytes)", standingData.Length);
                Console.WriteLine(standingData);
                Console.WriteLine();
            }

            lastAllIds = allIds;
            Array.Sort(lastAllIds);

            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
        }

        private static void StartListener(StandingDataListener sdListener, 
                                          PersistentObjectListener poListener,
                                          BundleListener bundleListener,
                                          PersistentObjectControlListener poControlListener,
                                          BundleControlListener bundleControlListener)
        {
            var listeningMode = ConfigurationManager.AppSettings[CONFIG_LISTENING_MODE];

            if (listeningMode.Contains("SD-DTC"))
                sdListener.Start(KissTransactionMode.DTC);

            if (listeningMode.Contains("PO-DTC"))
            {
                poListener.Start(KissTransactionMode.DTC);
                poControlListener.Start(KissTransactionMode.MSMQ);
            }

            if (listeningMode.Contains("BUNDLE-DTC"))
            {
                bundleListener.Start(KissTransactionMode.DTC);
                bundleControlListener.Start(KissTransactionMode.MSMQ);
            }

            if (listeningMode.Contains("SD-MSMQ"))
                sdListener.Start(KissTransactionMode.MSMQ);

            if (listeningMode.Contains("PO-MSMQ"))
            {
                poListener.Start(KissTransactionMode.MSMQ);
                poControlListener.Start(KissTransactionMode.MSMQ);
            }

            if (listeningMode.Contains("BUNDLE-MSMQ"))
            {
                bundleListener.Start(KissTransactionMode.MSMQ);
                bundleControlListener.Start(KissTransactionMode.MSMQ);
            }
        }

        private static void StopListener(StandingDataListener sdListener, 
                                         PersistentObjectListener poListener,
                                         BundleListener bundleListener,
                                         PersistentObjectControlListener poControlListener,
                                         BundleControlListener bundleControlListener)
        {
            var listeningMode = ConfigurationManager.AppSettings[CONFIG_LISTENING_MODE];

            if (listeningMode.Contains("SD"))
            {
                sdListener.Stop();
                sdListener.WaitUntilStopped();
            }

            if (listeningMode.Contains("PO"))
            {
                poControlListener.Stop();
                poControlListener.WaitUntilStopped();

                poListener.Stop();
                poListener.WaitUntilStopped();
            }

            if (listeningMode.Contains("BUNDLE"))
            {
                bundleControlListener.Stop();
                bundleControlListener.WaitUntilStopped();

                bundleListener.Stop();
                bundleListener.WaitUntilStopped();
            }
        }
    }
}

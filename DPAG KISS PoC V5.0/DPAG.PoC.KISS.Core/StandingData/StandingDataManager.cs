using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml.XPath;

namespace DPAG.PoC.KISS.Core
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class StandingDataManager
    {
        private const string FORMAT_SD_POSTFIX = "sd";
        private const string CONFIG_DIRECTORY_STANDING_DATA = "Standing Data";

        private int _msmqCapacity;
        private string _sql;

        public StandingDataManager(string sql, int msmqCapacity)
        {
            _sql = sql;
            _msmqCapacity = msmqCapacity;
        }

        public void InstallMSMQ()
        {
            new Neighborhood(_sql, FORMAT_SD_POSTFIX).CreateLocalQueue(true);
        }

        public void DeinstallMSMQ()
        {
            new Neighborhood(_sql, FORMAT_SD_POSTFIX).DeleteLocalQueue();
        }

        public void ProcessInstructions(KissTransactionMode mode)
        {
            string path,
                   branchId;
            byte[] sdData;
            XPathNavigator instruction;
            StandingData standingData;
            var filter = new List<string>();
            var neighborhood = new Neighborhood(_sql);
            var neighborhoodClients = neighborhood.GetAllClients().ToArray();

            foreach (var id in StandingData.RetrieveInstructionAll(_sql))
            {
                using (var sr = new StringReader(StandingData.RetrieveInstruction(_sql, long.Parse(id))))
                {
                    instruction = new XPathDocument(sr).CreateNavigator();

                    path = instruction.SelectSingleNode("//StandingData/@Path").Value;
                    branchId = instruction.SelectSingleNode("//StandingData/@BranchId").Value;

                    Array.Sort(neighborhoodClients);

                    foreach (XPathNavigator clientId in instruction.Select("//Client/@Id"))
                    {
                        if (Array.BinarySearch(neighborhoodClients, clientId.Value) >= 0)
                            filter.Add(clientId.Value);
                    }

                    if (branchId == neighborhood.GetLocalBranch() && filter.Count > 0)
                    {
                        sdData = DownloadStandingData(path);

                        standingData = new StandingData(_sql, long.Parse(id), _msmqCapacity, sdData);
                        standingData.Distribute(mode, filter.ToArray());
                    }
                }
            }
        }

        private byte[] DownloadStandingData(string path)
        {
            byte[] sdData;

            path = Path.Combine(Kiss.GetDirectory(CONFIG_DIRECTORY_STANDING_DATA), path);
            sdData = File.ReadAllBytes(path);

            return sdData;
        }
    }
}

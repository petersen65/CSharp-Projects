using System;
using System.Linq;
using System.Configuration;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.MediaServices.Client;

namespace Media.GettingStarted
{
    internal static class Program
    {
        private static readonly string _supportFiles = Path.GetFullPath(@"..\..\supportFiles\");

        private static readonly string _singleInputFilePath = Path.Combine(_supportFiles, @"interview1.wmv");
        private static readonly string _outputFilesFolder = Path.Combine(_supportFiles, @"outputfiles\");

        private static readonly string _accountName = ConfigurationManager.AppSettings["accountName"];
        private static readonly string _accountKey = ConfigurationManager.AppSettings["accountKey"];

        private static CloudMediaContext _mediaContext;

        private static void Main(string[] args)
        {
            _mediaContext = new CloudMediaContext(_accountName, _accountKey);
            var asset = CreateAssetAndUploadSingleFile(AssetCreationOptions.None, _singleInputFilePath);

            CreateEncodingJob(asset, _singleInputFilePath, _outputFilesFolder);
        }

        private static IAsset CreateEmptyAsset(string assetName, AssetCreationOptions assetCreationOptions)
        {
            var asset = _mediaContext.Assets.Create(assetName, assetCreationOptions);

            Console.WriteLine("Asset name: " + asset.Name);
            Console.WriteLine("Time created: " + asset.Created.Date.ToString());

            return asset;
        }

        private static IAsset CreateAssetAndUploadSingleFile(AssetCreationOptions assetCreationOptions, string singleFilePath)
        {
            var fileName = Path.GetFileName(singleFilePath);
            var assetName = "UploadSingleFile_" + DateTime.UtcNow.ToString();
            var asset = CreateEmptyAsset(assetName, assetCreationOptions);
            var assetFile = asset.AssetFiles.Create(fileName);

            Console.WriteLine("Created assetFile {0}", assetFile.Name);

            var accessPolicy = _mediaContext.AccessPolicies.
                Create(assetName, TimeSpan.FromDays(3), AccessPermissions.Write | AccessPermissions.List);

            var locator = _mediaContext.Locators.CreateLocator(LocatorType.Sas, asset, accessPolicy);

            Console.WriteLine("Upload {0}", assetFile.Name);
            assetFile.Upload(singleFilePath);
            Console.WriteLine("Done uploading of {0} using Upload()", assetFile.Name);

            locator.Delete();
            accessPolicy.Delete();

            return asset;
        }

        private static IJob CreateEncodingJob(IAsset asset, string inputMediaFilePath, string outputFolder)
        {
            var job = _mediaContext.Jobs.Create("My encoding job");
            
            // Get a media processor reference, and pass to it the name of the processor to use for the specific task.
            var processor = GetLatestMediaProcessorByName("Azure Media Encoder");

            // Create a task with the encoding details, using a string preset.
            var task = job.Tasks.AddNew("My encoding task", processor, "H264 Broadband 720p", TaskOptions.ProtectedConfiguration);

            // Specify the input asset to be encoded.
            task.InputAssets.Add(asset);
            
            // Add an output asset to contain the results of the job. 
            // This output is specified as AssetCreationOptions.None, which means the output asset is not encrypted. 
            task.OutputAssets.AddNew("Output asset", AssetCreationOptions.None);
            
            // Use the following event handler to check job progress.  
            job.StateChanged += StateChanged;

            // Launch the job.
            job.Submit();

            // Optionally log job details. 
            // This displays basic job details to the console and saves them to a JobDetails-{JobId}.txt file in your output folder.
            LogJobDetails(job.Id);

            // Check job execution and wait for job to finish. 
            var progressJobTask = job.GetExecutionProgressTask(CancellationToken.None);
            progressJobTask.Wait();

            // Optional code.  
            // Code after this point is not required for an encoding job, but shows how to access the assets that 
            // are the output of a job, either by creating URLs to the asset on the server, or by downloading. 

            // Get an updated job reference.
            job = GetJob(job.Id);

            // If job state is Error the event handling method for job progress should log errors.  
            // Here we check for error state and exit if needed.
            if (job.State == JobState.Error)
            {
                Console.WriteLine("\nExiting method due to job error.");
                return job;
            }

            // Get a reference to the output asset from the job.
            var outputAsset = job.OutputMediaAssets[0];

            // Declare an access policy for permissions on the asset. 
            // You can call an async or sync create method. 
            var policy = _mediaContext.AccessPolicies.Create("My 30 days readonly policy", TimeSpan.FromDays(30), AccessPermissions.Read);

            // Create a SAS locator to enable direct access to the asset in blob storage. 
            // You can call a sync or async create method.  
            // You can set the optional startTime param as 5 minutes earlier than Now to compensate for differences in time between the client and server clocks. 
            var locator = _mediaContext.Locators.CreateLocator(LocatorType.Sas, outputAsset, policy, DateTime.UtcNow.AddMinutes(-5));

            // Build a list of SAS URLs to each file in the asset. 
            var sasUrlList = GetAssetSasUrlList(outputAsset, locator);

            // Write the URL list to a local file. You can use the saved SAS URLs to browse directly to the files in the asset.
            if (sasUrlList != null)
            {
                var outFilePath = Path.GetFullPath(Path.Combine(outputFolder, @"FileSasUrlList.txt"));
                var fileList = new StringBuilder();

                foreach (string url in sasUrlList)
                {
                    fileList.AppendLine(url);
                    fileList.AppendLine();
                }

                WriteToFile(outFilePath, fileList.ToString());

                // Download the output to the local machine.
                DownloadAssetToLocal(job.Id, outputFolder);
            }

            return job;
        }

        private static IMediaProcessor GetLatestMediaProcessorByName(string mediaProcessorName)
        {
            // The possible strings that can be passed into the method for the mediaProcessor parameter:
            //   Azure Media Encoder
            //   Windows Azure Media Packager
            //   Windows Azure Media Encryptor
            //   Storage Decryption

            var processor = _mediaContext.MediaProcessors.
                Where(p => p.Name == mediaProcessorName).ToList().OrderBy(p => new Version(p.Version)).LastOrDefault();

            if (processor == null)
                throw new ArgumentException(string.Format("Unknown media processor", mediaProcessorName));

            return processor;
        }

        private static void StateChanged(object sender, JobStateChangedEventArgs e)
        {
            Console.WriteLine("Job state changed event:");
            Console.WriteLine("  Previous state: " + e.PreviousState);
            Console.WriteLine("  Current state: " + e.CurrentState);

            switch (e.CurrentState)
            {
                case JobState.Finished:
                    Console.WriteLine();
                    Console.WriteLine("********************");
                    Console.WriteLine("Job is finished.");
                    Console.WriteLine("Please wait while local tasks or downloads complete...");
                    Console.WriteLine("********************");
                    Console.WriteLine();
                    Console.WriteLine();
                    break;

                case JobState.Canceling:
                case JobState.Queued:
                case JobState.Scheduled:
                case JobState.Processing:
                    Console.WriteLine("Please wait...\n");
                    break;

                case JobState.Canceled:
                case JobState.Error:
                    // Cast sender as a job.
                    var job = (IJob)sender;
                    // Display or log error details as needed.
                    LogJobStop(job.Id);
                    break;

                default:
                    break;
            }
        }

        private static void LogJobStop(string jobId)
        {
            var builder = new StringBuilder();
            var job = GetJob(jobId);

            builder.AppendLine("\nThe job stopped due to cancellation or an error.");
            builder.AppendLine("***************************");
            builder.AppendLine("Job ID: " + job.Id);
            builder.AppendLine("Job Name: " + job.Name);
            builder.AppendLine("Job State: " + job.State.ToString());
            builder.AppendLine("Job started (server UTC time): " + job.StartTime.ToString());
            builder.AppendLine("Media Services account name: " + _accountName);

            // Log job errors if they exist.  
            if (job.State == JobState.Error)
            {
                builder.Append("Error Details: \n");
                
                foreach (ITask task in job.Tasks)
                {
                    foreach (ErrorDetail detail in task.ErrorDetails)
                    {
                        builder.AppendLine("  Task Id: " + task.Id);
                        builder.AppendLine("    Error Code: " + detail.Code);
                        builder.AppendLine("    Error Message: " + detail.Message + "\n");
                    }
                }
            }

            builder.AppendLine("***************************\n");
            
            // Write the output to a local file and to the console. The template for an error output file is:  JobStop-{JobId}.txt
            var  outputFile = _outputFilesFolder + @"\JobStop-" + JobIdAsFileName(job.Id) + ".txt";
            WriteToFile(outputFile, builder.ToString());
            
            Console.Write(builder.ToString());
        }

        private static void LogJobDetails(string jobId)
        {
            var builder = new StringBuilder();
            var job = GetJob(jobId);

            builder.AppendLine("\nJob ID: " + job.Id);
            builder.AppendLine("Job Name: " + job.Name);
            builder.AppendLine("Job submitted (client UTC time): " + DateTime.UtcNow.ToString());
            builder.AppendLine("Media Services account name: " + _accountName);

            // Write the output to a local file and to the console. The template 
            // for an error output file is:  JobDetails-{JobId}.txt
            var outputFile = _outputFilesFolder + @"\JobDetails-" + JobIdAsFileName(job.Id) + ".txt";
            WriteToFile(outputFile, builder.ToString());
            
            Console.Write(builder.ToString());
        }

        private static string JobIdAsFileName(string jobID)
        {
            return jobID.Replace(":", "_");
        }

        private static void WriteToFile(string outFilePath, string fileContent)
        {
            var sr = File.CreateText(outFilePath);
            
            sr.Write(fileContent);
            sr.Close();
        }

        private static List<String> GetAssetSasUrlList(IAsset asset, ILocator locator)
        {
            // Declare a list to contain all the SAS URLs.
            List<String> fileSasUrlList = new List<String>();

            // If the asset has files, build a list of URLs to each file in the asset and return. 
            foreach (IAssetFile file in asset.AssetFiles)
            {
                var sasUrl = BuildFileSasUrl(file, locator);
                fileSasUrlList.Add(sasUrl);
            }

            // Return the list of SAS URLs.
            return fileSasUrlList;
        }

        private static string BuildFileSasUrl(IAssetFile file, ILocator locator)
        {
            // Create and return a SAS URL to a single file in an asset. 

            // Take the locator path, add the file name, and build a full SAS URL to access this file. This is the only code required to build the full URL.
            var uriBuilder = new UriBuilder(locator.Path);
            uriBuilder.Path += "/" + file.Name;

            // Print the locator.Path to the asset, and the full SAS URL to the file
            Console.WriteLine("Locator path: ");
            Console.WriteLine(locator.Path);
            Console.WriteLine();
            Console.WriteLine("Full URL to file: ");
            Console.WriteLine(uriBuilder.Uri.AbsoluteUri);
            Console.WriteLine();

            //Return the SAS URL.
            return uriBuilder.Uri.AbsoluteUri;
        }

        private static IAsset DownloadAssetToLocal(string jobId, string outputFolder)
        {
            // This method illustrates how to download a single asset. 
            // However, you can iterate through the OutputAssets collection, and download all assets if there are many. 

            // Get a reference to the job. 
            var job = GetJob(jobId);
            
            // Get a reference to the first output asset. If there were multiple 
            // output media assets you could iterate and handle each one.
            var outputAsset = job.OutputMediaAssets[0];

            var accessPolicy = _mediaContext.AccessPolicies.Create("File Download Policy", TimeSpan.FromDays(30), AccessPermissions.Read);
            var locator = _mediaContext.Locators.CreateSasLocator(outputAsset, accessPolicy);
            
            BlobTransferClient blobTransfer = new BlobTransferClient
            {
                NumberOfConcurrentTransfers = 10,
                ParallelTransferThreadCount = 10
            };

            var downloadTasks = new List<Task>();

            foreach (var outputFile in outputAsset.AssetFiles)
            {
                // Use the following event handler to check download progress.
                outputFile.DownloadProgressChanged += DownloadProgress;

                var localDownloadPath = Path.Combine(outputFolder, outputFile.Name);
                Console.WriteLine("File download path:  " + localDownloadPath);
                downloadTasks.Add(outputFile.DownloadAsync(Path.GetFullPath(localDownloadPath), blobTransfer, locator, CancellationToken.None));
                
                outputFile.DownloadProgressChanged -= DownloadProgress;
            }

            Task.WaitAll(downloadTasks.ToArray());
            return outputAsset;
        }

        private static void DownloadProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            Console.WriteLine(string.Format("{0} % download progress. ", e.Progress));
        }

        private static IJob GetJob(string jobId)
        {
            // Use a Linq select query to get an updated reference by Id. 
            var jobInstance =
                from j in _mediaContext.Jobs
                where j.Id == jobId
                select j;

            // Return the job reference as an Ijob. 
            var job = jobInstance.FirstOrDefault();
            return job;
        }

        private static IAsset GetAsset(string assetId)
        {
            // Use a LINQ Select query to get an asset.
            var assetInstance =
                from a in _mediaContext.Assets
                where a.Id == assetId
                select a;
            
            // Reference the asset as an IAsset.
            var asset = assetInstance.FirstOrDefault();
            return asset;
        }
    }
}

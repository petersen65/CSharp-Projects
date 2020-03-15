using System;
using System.Linq;
using System.Configuration;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;
using Microsoft.WindowsAzure.MediaServices.Client;
using Microsoft.WindowsAzure.MediaServices.Client.ContentKeyAuthorization;
using Microsoft.WindowsAzure.MediaServices.Client.DynamicEncryption;
using System.Diagnostics;
using System.Net;

namespace Media.HowToSeries
{
    internal static class Program
    {
        private static readonly string _supportFiles = Path.GetFullPath(@"..\..\..\Media.GettingStarted\supportFiles\");
        private static readonly string _singleInputFilePath = Path.Combine(_supportFiles, "interview1.wmv");
        private static readonly string _integrativeMomFilePath = Path.Combine(_supportFiles, "IntegrativeMom.mp4");
        private static readonly string _mediaPackagerMP4ToSmooth = File.ReadAllText(Path.Combine(_supportFiles, "MediaPackager_MP4ToSmooth.xml"));
        private static readonly string _mediaEncoderThumbnails = File.ReadAllText(Path.Combine(_supportFiles, "MediaEncoder_Thumbnails.xml"));
        private static readonly string _mediaIndexerIntegrativeMom = File.ReadAllText(Path.Combine(_supportFiles, "MediaIndexer_IntegrativeMom.xml"));
        private static readonly string _outputFilesFolder = Path.Combine(_supportFiles, @"outputfiles\");

        private static readonly string _accountName = ConfigurationManager.AppSettings["accountName"];
        private static readonly string _accountKey = ConfigurationManager.AppSettings["accountKey"];

        private static MediaServicesCredentials _cachedCredentials;
        private static CloudMediaContext _context;

        private static void Main(string[] args)
        {
            IAsset singleFileAsset = null, 
                   mp4Asset = null;
            var runClient = true;

            do
            {
                FormatConsole("Azure Media Services", "Console");
                Console.Clear();

                Console.WriteLine("Azure Media Services - {0}", _accountName);
                Console.WriteLine();

                Console.WriteLine("a. Create an encrypted and unencrypted asset");
                Console.WriteLine("b. Create thumbnails for encrypted asset");
                Console.WriteLine("c. Create encrypted H.264 encoded asset in a MP4 container file");
                Console.WriteLine("d. Create an encrypted asset and perform speech to text indexing");
                Console.WriteLine("e. Create static smooth streaming package and originate it through an on demand endpoint");
                Console.WriteLine("f. Create encrypted multi bit rate H.264 asset and dynamically package it for origination with dynamic encryption");
                Console.WriteLine("g. Create live streaming ingress and program channel");

                Console.WriteLine();
                Console.WriteLine("0. Exit Application");
                Console.WriteLine();

                try 
	            {
                    // Create and cache the Media Services credentials in a static class variable
                    _cachedCredentials = new MediaServicesCredentials(_accountName, _accountKey);

                    // Use the cached credentials to create CloudMediaContex
                    _context = new CloudMediaContext(_cachedCredentials);

                    switch (Console.ReadKey(true).KeyChar)
                    {
                        case 'a':
                            // Create an encrypted and unencrypted asset
                            singleFileAsset = 
                                CreateAssetAndUploadSingleFile("UploadSingleFile_" + DateTime.UtcNow.ToString(), _singleInputFilePath);

                            Console.WriteLine();
                            DecryptStorageEncryptedAsset(singleFileAsset);
                            break;

                        case 'b':
                            // Create thumbnails for encrypted asset
                            CreateThumbnailsForVideo(singleFileAsset);
                            break;

                        case 'c':
                            // Create encrypted H.264 encoded asset in a MP4 container file
                             mp4Asset = EncodeAssetToH264Broadband720p(singleFileAsset).OutputMediaAssets[0];
                            break;

                        case 'd':
                            // Create an encrypted asset and perform speech to text indexing
                            var integrativeMomAsset = 
                                CreateAssetAndUploadSingleFile("UploadIntegrativeMom_" + DateTime.UtcNow.ToString(), _integrativeMomFilePath);

                            Console.WriteLine();
                            IndexAssetToCaptions(integrativeMomAsset, _outputFilesFolder);
                            break;

                        case 'e':
                            // Create static smooth streaming package and originate it through an on demand endpoint
                            var smoothAsset = PackageMP4AssetToSmoothStreaming(mp4Asset).OutputMediaAssets[0];

                            Console.WriteLine();
                            PublishAssetToOnDemandOriginEndpoint(smoothAsset, TimeSpan.FromDays(30));

                            Console.WriteLine("Smooth Streaming URL:");
                            Console.WriteLine(smoothAsset.GetSmoothStreamingUri().ToString());
                            break;

                        case 'f':
                            // Create encrypted multi bit rate H.264 asset and dynamically package it for origination with dynamic encryption
                            var multiBitrateMp4Asset = EncodeAssetToMultibitrateMP4(singleFileAsset).OutputMediaAssets[0];
                            var envEncKey = CreateEnvelopeEncryptionContentKey(multiBitrateMp4Asset);
                            var updatedEnvEncKey = AddOpenAuthorizationPolicyToContentKey(envEncKey);
                            
                            CreateAssetDeliveryPolicy(multiBitrateMp4Asset, updatedEnvEncKey);
                            PublishAssetToOnDemandOriginEndpoint(multiBitrateMp4Asset, TimeSpan.FromDays(30));

                            Console.WriteLine();
                            DownloadMP4FilesToLocalFilesystem(multiBitrateMp4Asset, _outputFilesFolder);
                            
                            Console.WriteLine();
                            Console.WriteLine("Smooth Streaming URL:");
                            Console.WriteLine(multiBitrateMp4Asset.GetSmoothStreamingUri().ToString());
                            Console.WriteLine("MPEG DASH URL:");
                            Console.WriteLine(multiBitrateMp4Asset.GetMpegDashUri().ToString());
                            Console.WriteLine("HLS URL:");
                            Console.WriteLine(multiBitrateMp4Asset.GetHlsUri().ToString());
                            break;

                        case 'g':
                            // Create live streaming ingress and program channel
                            var channel = CreateAndStartChannel("default-sample");

                            // Once you previewed your stream and verified that it is flowing into your Channel, 
                            // you can create an event by creating an Asset, Program, and Streaming Locator. 
                            // If you want to persist your stream and make it available to your audience 
                            // you need to create a Program and Streaming Endpoint.
                            var program = CreateAndStartProgram(channel, "default");

                            Console.WriteLine();
                            PublishAssetToOnDemandOriginEndpoint(program.Asset, program.ArchiveWindowLength);
                            CreateAndStartStreamingEndpoint("default-sample");
                            break;

                        case '0':
                            runClient = false;
                            break;

                        default:
                            Console.WriteLine("Key ignored!");
                            break;
                    }

                    if (runClient)
                    {
                        Console.WriteLine();
                        Console.WriteLine("Operation completed!");
                    }
	            }
	            catch (Exception ex)
	            {
                    Console.WriteLine("\n\nCaught exception: {0}.\n\n", ex);
                }

                if (runClient)
                    Console.ReadKey(true);
            } while (runClient);
        }

        #region Common Helpers
        private static void FormatConsole(string title, string subTitle)
        {
            Console.SetWindowSize(120, 50);
            Console.Title = string.Format("{0} - {1}", title, subTitle);
        }
        #endregion

        #region Azure Media Services Helpers
        private static IAsset CreateAssetAndUploadSingleFile(string assetName, string mezzanineFilePath)
        {
            var asset = _context.Assets.Create(assetName, AssetCreationOptions.StorageEncrypted);
            Console.WriteLine("Asset name: " + asset.Name);
            Console.WriteLine("Time created: " + asset.Created.Date.ToString());

            var assetFile = asset.AssetFiles.Create(Path.GetFileName(mezzanineFilePath));
            Console.WriteLine("Created assetFile {0}", assetFile.Name);

            Console.WriteLine("Upload {0}", assetFile.Name);
            assetFile.Upload(mezzanineFilePath);
            Console.WriteLine("Done uploading of {0} using Upload()", assetFile.Name);

            return asset;
        }

        private static IJob DecryptStorageEncryptedAsset(IAsset inputAsset)
        {
            var processor = _context.MediaProcessors.GetLatestMediaProcessorByName(MediaProcessorNames.StorageDecryption);
            var job = _context.Jobs.Create("Decryption job for Storage Encrypted Asset");

            var task = job.Tasks.AddNew("Decryption Task", processor, string.Empty, TaskOptions.None);
            task.InputAssets.Add(inputAsset);
            task.OutputAssets.AddNew("Decrypted Asset for " + inputAsset.Name, AssetCreationOptions.None);

            job.StateChanged += JobStateChanged;
            job.Submit();
            LogJobDetails(job.Id);
            job.GetExecutionProgressTask(CancellationToken.None).Wait();

            return job;
        }

        private static IJob CreateThumbnailsForVideo(IAsset inputAsset)
        {
            var processor = GetLatestMediaProcessorByName("Azure Media Encoder");
            var job = _context.Jobs.Create("Encoding job for Thumbnails");

            var task = job.Tasks.AddNew("Encoding Task", processor, _mediaEncoderThumbnails, TaskOptions.ProtectedConfiguration);
            task.InputAssets.Add(inputAsset);
            task.OutputAssets.AddNew("Output Asset for " + inputAsset.Name, AssetCreationOptions.StorageEncrypted);

            job.StateChanged += JobStateChanged;
            job.Submit();
            LogJobDetails(job.Id);
            job.GetExecutionProgressTask(CancellationToken.None).Wait();

            return job;
        }

        private static IJob EncodeAssetToH264Broadband720p(IAsset inputAsset)
        {
            var processor = GetLatestMediaProcessorByName("Azure Media Encoder");
            var job = _context.Jobs.Create("Encoding job for H264 Broadband 720p");

            var task = job.Tasks.AddNew("Encoding Task", processor, "H264 Broadband 720p", TaskOptions.ProtectedConfiguration);
            task.InputAssets.Add(inputAsset);
            task.OutputAssets.AddNew("Output Asset for " + inputAsset.Name, AssetCreationOptions.StorageEncrypted);

            job.StateChanged += JobStateChanged;
            job.Submit();
            LogJobDetails(job.Id);
            job.GetExecutionProgressTask(CancellationToken.None).Wait();

            return job;
        }

        private static IJob IndexAssetToCaptions(IAsset inputAsset, string outputFilesFolder)
        {
            var processor = _context.MediaProcessors.GetLatestMediaProcessorByName("Azure Media Indexer");
            var job = _context.Jobs.Create("Indexing job");

            var task = job.Tasks.AddNew("Indexing Task", processor, _mediaIndexerIntegrativeMom, TaskOptions.None);
            task.InputAssets.Add(inputAsset);
            task.OutputAssets.AddNew("Indexed Output Asset for" + inputAsset.Name, AssetCreationOptions.None);

            job.StateChanged += JobStateChanged;
            job.Submit();
            LogJobDetails(job.Id);
            job.GetExecutionProgressTask(CancellationToken.None).Wait();

            // Files description
            // 
            // Audio Indexing Blob (AIB) file is a binary file that can be searched in Microsoft SQL server using full text search.  
            // The AIB file is more powerful than the simple caption files, because it contains alternatives for each word, allowing 
            // a much richer search experience.
            //
            // Synchronized Accessible Media Interchange (SAMI) is a file format designed to deliver synchronized text such as 
            // captions, subtitles, or audio descriptions with digital media content.
            //
            // The Timed Text Markup Language (TTML) is a content type that represents timed text media for the purpose of interchange 
            // among authoring systems. Timed text is textual information that is intrinsically or extrinsically associated with timing information.
            // It is intended to be used for the purpose of transcoding or exchanging timed text information among legacy distribution 
            // content formats presently in use for subtitling and captioning functions.

            job.OutputMediaAssets[0].
                DownloadToFolder(outputFilesFolder,
                                 (af, dp) =>
                                 {
                                     Console.WriteLine("Downloading '{0}' - Progress: {1:0.##}%", af.Name, dp.Progress);
                                 });

            return job;
        }

        private static IJob PackageMP4AssetToSmoothStreaming(IAsset inputAsset)
        {
            var processor = GetLatestMediaProcessorByName("Windows Azure Media Packager");
            var job = _context.Jobs.Create("Packaging job for Smooth Streaming");

            var task = job.Tasks.AddNew("Packaging Task", processor, _mediaPackagerMP4ToSmooth, TaskOptions.ProtectedConfiguration);
            task.InputAssets.Add(inputAsset);
            task.OutputAssets.AddNew("Output Asset for " + inputAsset.Name, AssetCreationOptions.None);

            job.StateChanged += JobStateChanged;
            job.Submit();
            LogJobDetails(job.Id);
            job.GetExecutionProgressTask(CancellationToken.None).Wait();

            return job;
        }

        private static void PublishAssetToOnDemandOriginEndpoint(IAsset asset, TimeSpan duration)
        {
            _context.Locators.Create(LocatorType.OnDemandOrigin, asset, AccessPermissions.Read, duration);
        }

        private static IJob EncodeAssetToMultibitrateMP4(IAsset inputAsset)
        {
            var processor = _context.MediaProcessors.GetLatestMediaProcessorByName(MediaProcessorNames.AzureMediaEncoder);
            var job = _context.Jobs.Create("Encoding job for Multi Bitrate MP4");

            var task = job.Tasks.AddNew("Encoding Task", processor, "H264 Adaptive Bitrate MP4 Set 720p", TaskOptions.ProtectedConfiguration);
            task.InputAssets.Add(inputAsset);
            task.OutputAssets.AddNew("Output Asset for" + inputAsset.Name, AssetCreationOptions.StorageEncrypted);

            job.StateChanged += JobStateChanged;
            job.Submit();
            LogJobDetails(job.Id);
            job.GetExecutionProgressTask(CancellationToken.None).Wait();

            return job;
        }

        private static IContentKey CreateEnvelopeEncryptionContentKey(IAsset asset)
        {
            var keyId = Guid.NewGuid();
            var contentKey = GetCryptoStrongSequence(16);
            var key = _context.ContentKeys.Create(keyId, contentKey, "ContentKey", ContentKeyType.EnvelopeEncryption);

            asset.ContentKeys.Add(key);
            return key;
        }

        private static IContentKey AddOpenAuthorizationPolicyToContentKey(IContentKey contentKey)
        {
            var policy = _context.ContentKeyAuthorizationPolicies.CreateAsync("Open Authorization Policy").Result;

            var restrictions = new List<ContentKeyAuthorizationPolicyRestriction> 
                { 
                    new ContentKeyAuthorizationPolicyRestriction
                    {
                        Name = "HLS Open Authorization Policy Restriction",
                        KeyRestrictionType = (int)ContentKeyRestrictionType.Open,
                        Requirements = null
                    } 
                };

            var policyOption = _context.ContentKeyAuthorizationPolicyOptions.
                Create("Authorization Policy Option", ContentKeyDeliveryType.BaselineHttp, restrictions, string.Empty);

            policy.Options.Add(policyOption);

            contentKey.AuthorizationPolicyId = policy.Id;
            var updatedKey = contentKey.UpdateAsync().Result;

            return updatedKey;
        }

        private static void CreateAssetDeliveryPolicy(IAsset asset, IContentKey key)
        {
            var keyAcquisitionUri = key.GetKeyDeliveryUrl(ContentKeyDeliveryType.BaselineHttp);
            var envelopeEncryptionInitializationVector = Convert.ToBase64String(GetCryptoStrongSequence(16));

            // The following policy configuration specifies: 
            //   key url that will have KID=<Guid> appended to the envelope and
            //   the Initialization Vector (IV) to use for the envelope encryption
            var assetDeliveryPolicyConfiguration =
                new Dictionary<AssetDeliveryPolicyConfigurationKey, string> 
                {
                    { AssetDeliveryPolicyConfigurationKey.EnvelopeKeyAcquisitionUrl, keyAcquisitionUri.ToString() },
                    { AssetDeliveryPolicyConfigurationKey.EnvelopeEncryptionIVAsBase64, envelopeEncryptionInitializationVector }
                };

            var assetDeliveryPolicy = _context.AssetDeliveryPolicies.
                Create("Asset Delivery Policy",
                       AssetDeliveryPolicyType.DynamicEnvelopeEncryption,
                       AssetDeliveryProtocol.SmoothStreaming | AssetDeliveryProtocol.HLS,
                       assetDeliveryPolicyConfiguration);

            asset.DeliveryPolicies.Add(assetDeliveryPolicy);
        }

        private static void DownloadMP4FilesToLocalFilesystem(IAsset asset, string outputFilesFolder)
        {
            var policy = _context.AccessPolicies.Create("30 Days ReadOnly", TimeSpan.FromDays(30), AccessPermissions.Read);
            var locator = _context.Locators.CreateLocator(LocatorType.Sas, asset, policy);

            var sasUris = (from af in asset.AssetFiles.ToList()
                           where af.Name.EndsWith("mp4", StringComparison.OrdinalIgnoreCase)
                           select af.GetSasUri(locator)).ToList();

            Console.WriteLine("You can progressively download the following MP4 files:");
            sasUris.ForEach(uri => Console.WriteLine(uri));
            Console.WriteLine();

            asset.DownloadToFolder(outputFilesFolder,
                                   (af, dp) =>
                                   {
                                       Console.WriteLine("Downloading '{0}' - Progress: {1:0.##}%", af.Name, dp.Progress);
                                   });
        }

        private static IChannel CreateAndStartChannel(string channelName)
        {
            // Channels are the fundamental entity in Azure Media Services that allows you to ingest a live stream. 
            // Each Channel will have an ingest URL and a preview URL, it can also have one or more Programs associated with it. 
            // It usually takes around 2 minutes to start a Channel but could take as much as 20 minutes. 
            // You don’t have to start the Channel right away, usually people set up the Channel in advance 
            // but wait to start it until their live event is about to start.

            var channel = _context.Channels.Create(
                new ChannelCreationOptions
                {
                    Name = channelName,
                    Input = CreateChannelInput(),
                    Preview = CreateChannelPreview(),
                    Output = CreateChannelOutput()
                });

            channel.Start();

            Console.WriteLine("Starting Channel " + channelName);
            Console.WriteLine("Channel's ingest URL " + channel.Input.Endpoints.FirstOrDefault().Url.ToString());
            Console.WriteLine("Channel's preview URL " + channel.Preview.Endpoints.FirstOrDefault().Url.ToString());

            return channel;
        }

        private static ChannelInput CreateChannelInput()
        {
            return new ChannelInput
            {
                StreamingProtocol = StreamingProtocol.RTMP,
                AccessControl = new ChannelAccessControl
                {
                    IPAllowList = new List<IPRange>
                    {
                        new IPRange
                        {
                            Name = "Public Internet",
                            Address = IPAddress.Parse("0.0.0.0"),
                            SubnetPrefixLength = 0
                        }
                    }
                }
            };
        }

        private static ChannelPreview CreateChannelPreview()
        {
            return new ChannelPreview
            {
                AccessControl = new ChannelAccessControl
                {
                    IPAllowList = new List<IPRange>
                    {
                        new IPRange
                        {
                            Name = "Public Internet",
                            Address = IPAddress.Parse("0.0.0.0"),
                            SubnetPrefixLength = 0
                        }
                    }
                }
            };
        }

        private static ChannelOutput CreateChannelOutput()
        {
            return new ChannelOutput
            {
                Hls = new ChannelOutputHls { FragmentsPerSegment = 1 }
            };
        }

        private static IProgram CreateAndStartProgram(IChannel channel, string programName)
        {
            // A Program enables you to control the publishing and storage of a live stream. 
            // You can run up to three Programs concurrently, this allows you to publish and 
            // archive different parts of the stream as needed.
            // You can specify the number of hours you want to retain the recorded content for the 
            // Program by setting the ArchiveWindowLength property. 
            // This value can be set from a minimum of 5 minutes to a maximum of 25 hours. 
            // This also dictates the maximum amount of time viewers can seek back in time from the current live position. 
            // Programs can run over the specified amount of time, but content that falls 
            // behind the window length is continuously discarded.
            
            var asset = _context.Assets.Create("Output Asset for " + programName, AssetCreationOptions.None);
            var program = channel.Programs.Create(programName, TimeSpan.FromHours(3), asset.Id);
            
            program.Start();
            Console.WriteLine("Starting program {0}", programName);
            
            return program;
        }

        private static ILocator CreateLocatorForAsset(IAsset asset, TimeSpan archiveWindowLength)
        {
            // The Streaming Locator makes the Asset you associated with your Program available 
            // for streaming through your Streaming Endpoint.

            var locator = _context.Locators.CreateLocator
                (
                    LocatorType.OnDemandOrigin,
                    asset,
                    _context.AccessPolicies.Create("Live Stream Policy", archiveWindowLength, AccessPermissions.Read)
                );

            return locator;
        }

        private static IStreamingEndpoint CreateAndStartStreamingEndpoint(string streamingEndpointName)
        {
            // A single Media Services account can have multiple Streaming Endpoints. 
            // You may want to have multiple Streaming Endpoints if you want to have different 
            // configurations for each (for example, security settings, cross site access policies, scale units, etc.) 
            // or if you want to separate your Video on Demand (VOD) and Live streaming.

            var options = new StreamingEndpointCreationOptions
            {
                Name = streamingEndpointName,
                ScaleUnits = 1,
                AccessControl = new StreamingEndpointAccessControl
                {
                    IPAllowList = new List<IPRange>
                    {
                        new IPRange
                        {
                            Name = "Public Internet",
                            Address = IPAddress.Parse("0.0.0.0"),
                            SubnetPrefixLength = 0
                        }
                    }
                },
                CacheControl = new StreamingEndpointCacheControl
                {
                    MaxAge = TimeSpan.FromSeconds(1000)
                }
            };

            var streamingEndpoint = _context.StreamingEndpoints.Create(options);
            streamingEndpoint.Start();

            return streamingEndpoint;
        }
        #endregion

        #region Azure Media Services Utilities
        private static IMediaProcessor GetLatestMediaProcessorByName(string mediaProcessorName)
        {
            var processor = (from mp in _context.MediaProcessors
                             where mp.Name == mediaProcessorName
                             orderby mp.Version descending
                             select mp).FirstOrDefault();

            if (processor == null)
                throw new ArgumentException(string.Format("Unknown media processor", mediaProcessorName));

            return processor;
        }

        private static byte[] GetCryptoStrongSequence(int size)
        {
            var randomBytes = new byte[size];
            
            using (var rng = new RNGCryptoServiceProvider())
                rng.GetBytes(randomBytes);

            return randomBytes;
        }

        private static void JobStateChanged(object sender, JobStateChangedEventArgs e)
        {
            Console.WriteLine();
            Console.WriteLine("Job state changed event:");
            Console.WriteLine("  Previous state: " + e.PreviousState);
            Console.WriteLine("  Current state: " + e.CurrentState);

            switch (e.CurrentState)
            {
                case JobState.Finished:
                    Console.WriteLine("Job is finished.");
                    break;

                case JobState.Canceling:
                case JobState.Queued:
                case JobState.Scheduled:
                case JobState.Processing:
                    Console.WriteLine("Please wait...");
                    break;

                case JobState.Canceled:
                case JobState.Error:
                    LogJobError((sender as IJob).Id);
                    break;

                default:
                    break;
            }
        }

        private static IJob GetJob(string jobId)
        {
            return (from j in _context.Jobs
                    where j.Id == jobId
                    select j).FirstOrDefault();
        }

        private static IAsset GetAsset(string assetId)
        {
            return (from a in _context.Assets
                    where a.Id == assetId
                    select a).FirstOrDefault();
        }

        private static void LogJobError(string jobId)
        {
            var job = GetJob(jobId);
            var builder = new StringBuilder();

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
            Console.Write(builder.ToString());
        }

        private static void LogJobDetails(string jobId)
        {
            var job = GetJob(jobId);
            var builder = new StringBuilder();

            builder.AppendLine("Job ID: " + job.Id);
            builder.AppendLine("Job Name: " + job.Name);
            builder.AppendLine("Job submitted (client UTC time): " + DateTime.UtcNow.ToString());
            builder.AppendLine("Media Services account name: " + _accountName);

            Console.Write(builder.ToString());
        }
        #endregion
    }
}

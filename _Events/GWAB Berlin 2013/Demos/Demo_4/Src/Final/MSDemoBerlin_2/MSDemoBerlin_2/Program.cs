#region Using Directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;


using Microsoft.WindowsAzure.MediaServices.Client;
using System.Threading;


#endregion

namespace MSDemoBerlin_2
{
    class Program
    {
        #region Declarations

        // Your Media Service Account Name
        private static string accName = "Your Media Service Account";

        // Your Media Service Access Key  
        private static string accKey = "Your Media Service Access Key";

        // Path for Video Upload
        private static string _singleInputFilePath = Path.GetFullPath(@"C:\Assets\azure.wmv");

        // CloudMediaContext
        private static CloudMediaContext mContext = null;

        // Path for Config File
        private static string configFilePath = Path.GetFullPath(@"C:\Assets");

        #endregion

        static void Main(string[] args)
        {
            mContext = new CloudMediaContext(accName, accKey);

            IAsset asset = CreateAssetAndUploadSingleFile(_singleInputFilePath);
            
            CreateEncodingJob(asset);
            
            Console.ReadLine();
        }

        #region CreateEncodingJob

        #endregion

        static void CreateEncodingJob(IAsset asset)
        {
            IJob job = mContext.Jobs.Create("Encoding into MP4");

            IMediaProcessor processor = mContext.MediaProcessors.Where(p => p.Name == "Windows Azure Media Encoder").
                ToList().OrderBy(p => new Version(p.Version)).LastOrDefault();

            ITask task = job.Tasks.AddNew("Encoding into MP4",
                                            processor,
                                            "H264 Broadband 720p",
                                             Microsoft.WindowsAzure.MediaServices.Client.TaskOptions.None);

            task.InputAssets.Add(asset);

            task.OutputAssets.AddNew("MSDemo in H264", AssetCreationOptions.None);

            string configuration = File.ReadAllText(Path.GetFullPath(configFilePath + @"\MP4 to Smooth Streams.xml"));

            IMediaProcessor processor2 = mContext.MediaProcessors.Where(p => p.Name == "Windows Azure Media Packager").
                ToList().OrderBy(p => new Version(p.Version)).LastOrDefault();

            ITask streamingtask = job.Tasks.AddNew("Packaging into Smooth Streaming",
                                                   processor2,
                                                   configuration,
                                                   Microsoft.WindowsAzure.MediaServices.Client.TaskOptions.None);

            streamingtask.InputAssets.Add(task.OutputAssets[0]);
            streamingtask.OutputAssets.AddNew("MSDemo in Smooth Streaming", AssetCreationOptions.None);

            job.Submit();

            CheckJobProgress(job.Id);


            // Get an updated job reference, after waiting for the job on the thread in the CheckJobProgress method.
            job = GetJob(job.Id);

            // Get a reference to the output asset from the job.
            IAsset outputAsset = job.OutputMediaAssets[1];

            GetStreamingOriginLocator(outputAsset);
        }

        public static ILocator GetStreamingOriginLocator(IAsset asset)
        {

            // Get a reference to the streaming manifest file from the  
            // collection of files in the asset. 
            var theManifest = from f in asset.AssetFiles
                              where f.Name.EndsWith(".ism")
                              select f;

            // Cast the reference to a true IAssetFile type. 
            IAssetFile manifestFile = theManifest.First();

            // Create a 30-day readonly access policy. 
            IAccessPolicy policy = mContext.AccessPolicies.Create("Streaming policy",
                TimeSpan.FromDays(30),
                AccessPermissions.Read);

            // Create a locator to the streaming content on an origin. 
            ILocator originLocator = mContext.Locators.CreateLocator(LocatorType.OnDemandOrigin, asset,
                policy,
                DateTime.UtcNow.AddMinutes(-5));

            // Display some useful values based on the locator.
            // Display the base path to the streaming asset on the origin server.
            Console.WriteLine("Streaming asset base path on origin: ");
            Console.WriteLine(originLocator.Path);
            Console.WriteLine();

            // Create a full URL to the manifest file. Use this for playback
            // in smooth streaming and HLS media clients. 
            string urlForClientStreaming = originLocator.Path + manifestFile.Name + "/manifest";

            Console.WriteLine("URL to manifest for client streaming: ");
            Console.WriteLine(urlForClientStreaming);
            Console.WriteLine();

            // Return the locator. 
            return originLocator;
        }


        private static void CheckJobProgress(string jobId)
        {
            // Flag to indicate when job state is finished. 
            bool jobCompleted = false;
            // Expected polling interval in milliseconds.  Adjust this 
            // interval as needed based on estimated job completion times.
            const int JobProgressInterval = 10000;

            while (!jobCompleted)
            {
                // Get an updated reference to the job in case 
                // reference gets 'stale' while thread waits.
                IJob theJob = GetJob(jobId);

                // Check job and report state. 
                switch (theJob.State)
                {
                    case JobState.Finished:
                        jobCompleted = true;
                        Console.WriteLine("");
                        Console.WriteLine("********************");
                        Console.WriteLine("Job state is: " + theJob.State + ".");
                        Console.WriteLine("Please wait while local tasks complete...");
                        Console.WriteLine();
                        break;
                    case JobState.Queued:
                    case JobState.Scheduled:
                    case JobState.Processing:
                        Console.WriteLine("Job state is: " + theJob.State + ".");
                        Console.WriteLine("Please wait...");
                        Console.WriteLine();
                        break;
                    case JobState.Error:
                        // Log error as needed.
                        break;
                    default:
                        Console.WriteLine(theJob.State.ToString());
                        break;
                }

                // Wait for the specified job interval before checking state again.
                Thread.Sleep(JobProgressInterval);
            }
        }

        static IJob GetJob(string jobId)
        {
            // Use a Linq select query to get an updated reference by Id. 
            var job =
                from j in mContext.Jobs
                where j.Id == jobId
                select j;

            // Return the job reference as an Ijob. 
            IJob theJob = job.SingleOrDefault();

            // Confirm whether job exists, and return. 
            if (theJob != null)
                return theJob;
            else
                Console.WriteLine("Job does not exist.");
            return null;
        }
                  

        #region CreateAssetAndUploadSingleFile

        static public IAsset CreateAssetAndUploadSingleFile(string singleFilePath)
        {
            //1. Create asset

            var assetName = "BerlinDemo_2";
            var asset = mContext.Assets.Create(assetName, AssetCreationOptions.None);

            //2. Create file object(s) associated with asset

            var fileName = Path.GetFileName(singleFilePath);
            var assetFile = asset.AssetFiles.Create(fileName);
            Console.WriteLine("Created assetFile {0}", assetFile.Name);

            //3. Create assess policy and locator so you can upload files from local filesystem in next step

            var policy = mContext.AccessPolicies.Create("Write", TimeSpan.FromMinutes(5), AccessPermissions.Write);
            var locator = mContext.Locators.CreateSasLocator(asset, policy);

            //4. Upload files from local disk to corresponding asset files objects

            Console.WriteLine("Upload {0}", assetFile.Name);
            assetFile.Upload(singleFilePath);

            //5. Select which file will a primary file within an asset

            assetFile.IsPrimary = true;
            Console.WriteLine("Done uploading of {0} using Upload()", assetFile.Name);

            locator.Delete();

            return asset;
        }

        #endregion

    }
}

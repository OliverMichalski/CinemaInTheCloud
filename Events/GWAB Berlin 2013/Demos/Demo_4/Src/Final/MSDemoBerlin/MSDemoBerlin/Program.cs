#region Using Directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.MediaServices.Client;


#endregion

namespace MSDemoBerlin
{
    class Program
    {
        #region Declarations

        // Your Media Service Account Name
        private static string accName = "Your Media Service Account";

        // Your Media Service Access Key  
        private static string accKey = "Your Media Service Access Key";

        // Path for Video Üpload
        private static string _singleInputFilePath = Path.GetFullPath(@"C:\Assets\BigBuckBunny.mp4");

        // CloudMediaContext
        private static CloudMediaContext mContext = null;

        #endregion

        static void Main(string[] args)
        {
            mContext = new CloudMediaContext(accName, accKey);

            IAsset asset = CreateAssetAndUploadSingleFile(_singleInputFilePath);

            Console.ReadLine();
        }

        static public IAsset CreateAssetAndUploadSingleFile(string singleFilePath)
        {
            //1. Create asset

            var assetName = "BerlinDemo_1";
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

    }
}

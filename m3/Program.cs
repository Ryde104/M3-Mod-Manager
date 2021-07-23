using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace m3
{
    class Program
    {
        const string CONNECTION_STRING = "";
        static async Task Main(string[] args)
        {
            string strMethod                                            = string.Empty;
            string strValue                                             = string.Empty;
            string strRun                                               = string.Empty;

            if(args.Length > 1)
            {
                strMethod                                               = Clean(args[0]);
                strValue                                                = Clean(args[1]);

                switch(strMethod)
                {
                    case "install":
                    case "i":
                        {
                            await IntallAsync(strValue);
                            break;
                        }
                    case "init":
                    case "create":
                        {
                            await InitAsync(strValue);
                            break;
                        }
                    case "push":
                        {
                            await PushAsync(strValue);
                            break;
                        }
                    case "pull":
                        {
                            await PullAsync(strValue);
                            break;
                        }
                }

            }
            else
            {
                WriteHelp();
            }
        }

        private static async Task InitAsync(string strValue)
        {
            Console.WriteLine("Init  " + strValue);

            strValue                                                    = Clean(strValue);

            try
            {
                CloudStorageAccount saAccount                           = CloudStorageAccount.Parse(CONNECTION_STRING);
                CloudBlobClient bClient                                 = saAccount.CreateCloudBlobClient();

                CloudBlobContainer container                            = bClient.GetContainerReference(strValue);

                bool result                                             = await container.CreateIfNotExistsAsync();

                if (result != true)
                    throw new Exception("Name already exists please select a different name.");

                Directory.CreateDirectory(strValue);

                Console.WriteLine("Success! Please add your mods and do a m3 push " + strValue + "!");
                Console.WriteLine("Happy moding!");

                string strDir                                           = Directory.GetCurrentDirectory() + @"\" + strValue;
                Process.Start("explorer.exe", strDir);

            }
            catch (Exception exError)
            {
                Console.WriteLine("Errors!");
                Console.WriteLine(exError.Message);
            }
        }

        private static async Task PushAsync(string strValue)
        {
            Console.WriteLine("Push  " + strValue);

            strValue                                                    = Clean(strValue);

            try
            {
                CloudStorageAccount saAccount                           = CloudStorageAccount.Parse(CONNECTION_STRING);
                CloudBlobClient bClient                                 = saAccount.CreateCloudBlobClient();
                var bcContainer                                         = bClient.GetContainerReference(strValue);
                List<string> astrServerFiles                            = new List<string>();
                BlobContinuationToken blobContinuationToken             = null;
                var results                                             = await bcContainer.ListBlobsSegmentedAsync(null, blobContinuationToken);
                blobContinuationToken                                   = results.ContinuationToken;
                foreach (IListBlobItem item in results.Results)
                {
                    string strFileName                                  = Path.GetFileName(item.Uri.ToString());
                    Console.WriteLine("Checking Server: " + strFileName);
                    astrServerFiles.Add(strFileName);
                }

                string[] astrLocalFiles                                 = Directory.GetFiles(Directory.GetCurrentDirectory() + @"\" + strValue);

                foreach(string strLocalFile in astrLocalFiles)
                {
                    if (!astrServerFiles.Contains(Path.GetFileName(strLocalFile))) //Need to upload
                    {
                        Console.WriteLine("Adding: " + Path.GetFileName(strLocalFile));
                        var newBlob                                     = bcContainer.GetBlockBlobReference(Path.GetFileName(strLocalFile));
                        await newBlob.UploadFromFileAsync(strLocalFile);
                    }
                }

                //Delete what is not suposed to be there
                foreach(string strServerFile in astrServerFiles)
                {
                    if (!Contains(astrLocalFiles, strServerFile))
                    {
                        Console.WriteLine("Removing: " + strServerFile);
                        var newBlob                                     = bcContainer.GetBlockBlobReference(strServerFile);
                        await newBlob.DeleteAsync();
                    }
                }
            }
            catch (Exception exError)
            {
                Console.WriteLine("Errors!");
                Console.WriteLine(exError.Message);
            }
        }

        private static async Task IntallAsync(string strValue)
        {
            Console.WriteLine("Intalling " + strValue);
            strValue = Clean(strValue);
            try
            {
                string strBase                                          = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

                if (!Directory.Exists(strBase + @"\.minecraft"))
                {
                    Console.WriteLine("minecraft is not installed, please install");
                    return;
                }

                //Check mod folder
                if (!Directory.Exists(strBase + @"\.minecraft\mods"))
                    Directory.CreateDirectory(strBase + @"\.minecraft\mods");

                string[] astrLocalFiles                                 = Directory.GetFiles(strBase + @"\.minecraft\mods");
                List<string> astrServerFiles                            = new List<string>();

                CloudStorageAccount saAccount                           = CloudStorageAccount.Parse(CONNECTION_STRING);
                CloudBlobClient bClient                                 = saAccount.CreateCloudBlobClient();
                var bcContainer                                         = bClient.GetContainerReference(strValue);

                BlobContinuationToken blobContinuationToken             = null;
                var results                                             = await bcContainer.ListBlobsSegmentedAsync(null, blobContinuationToken);
                blobContinuationToken                                   = results.ContinuationToken;
                foreach (IListBlobItem item in results.Results)
                {
                    string strFileName                                  = Path.GetFileName(item.Uri.ToString());
                    astrServerFiles.Add(strFileName);
                    Console.WriteLine("Checking: " + strFileName);

                    if (!Contains(astrLocalFiles, strFileName))
                    {
                        Console.WriteLine("Adding: " + strFileName);
                        await bcContainer.GetBlockBlobReference(strFileName).DownloadToFileAsync(strBase + @"\.minecraft\mods\" + strFileName, FileMode.Create);
                    }
                }

                //Delete what is not suposed to be there
                foreach(string strFile in astrLocalFiles)
                {
                    if (!astrServerFiles.Contains(Path.GetFileName(strFile)))
                    {
                        Console.WriteLine("Removing: " + strFile);
                        File.Delete(strFile);
                    }
                }
            }
            catch (Exception exError)
            {
                Console.WriteLine("Errors!");
                Console.WriteLine(exError.Message);
            }
        }

        private static async Task PullAsync(string strValue)
        {
            Console.WriteLine("Pulling " + strValue);
            strValue = Clean(strValue);
            try
            {
                //Check mod folder
                if (!Directory.Exists(Directory.GetCurrentDirectory() + @"\" + strValue))
                    Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\" + strValue);

                string[] astrLocalFiles = Directory.GetFiles(Directory.GetCurrentDirectory() + @"\" + strValue);
                List<string> astrServerFiles = new List<string>();

                CloudStorageAccount saAccount = CloudStorageAccount.Parse(CONNECTION_STRING);
                CloudBlobClient bClient = saAccount.CreateCloudBlobClient();
                var bcContainer = bClient.GetContainerReference(strValue);

                BlobContinuationToken blobContinuationToken = null;
                var results = await bcContainer.ListBlobsSegmentedAsync(null, blobContinuationToken);
                blobContinuationToken = results.ContinuationToken;
                foreach (IListBlobItem item in results.Results)
                {
                    string strFileName = Path.GetFileName(item.Uri.ToString());
                    astrServerFiles.Add(strFileName);
                    Console.WriteLine("Checking: " + strFileName);

                    if (!Contains(astrLocalFiles, strFileName))
                    {
                        Console.WriteLine("Adding: " + strFileName);
                        await bcContainer.GetBlockBlobReference(strFileName).DownloadToFileAsync(Directory.GetCurrentDirectory() + @"\" + strValue + @"\" + strFileName, FileMode.Create);
                    }
                }

                //Delete what is not suposed to be there
                foreach (string strFile in astrLocalFiles)
                {
                    if (!astrServerFiles.Contains(Path.GetFileName(strFile)))
                    {
                        Console.WriteLine("Removing: " + strFile);
                        File.Delete(strFile);
                    }
                }

                Process.Start("explorer.exe", Directory.GetCurrentDirectory() + @"\" + strValue);

            }
            catch (Exception exError)
            {
                Console.WriteLine("Errors!");
                Console.WriteLine(exError.Message);
            }
        }

        public static bool Contains(string[] astr, string strCompare)
        {
            foreach(string str in astr)
            {
                if (Path.GetFileName(str) == strCompare)
                    return true;
            }

            return false;
        }

        public static void WriteHelp()
        {
            Console.WriteLine("m3 Help!");
            Console.WriteLine("-------------------------");
            Console.WriteLine("");
            Console.WriteLine("m3 install <repo>");
            Console.WriteLine("m3 pull <repo>");
            Console.WriteLine("m3 init <repo>");
            Console.WriteLine("m3 push <repo>");
        }

        public static string Clean(string str)
        {
            string strReturn                                            = string.Empty;

            try
            {
                strReturn                                               = str.Trim().ToLower().Replace(" ", "");
            }
            catch (Exception) { };
            return strReturn;
        }
    }
}

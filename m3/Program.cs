using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
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

        private static async Task PullAsync(string strValue)
        {
            Console.WriteLine("pulling " + strValue);

            try
            {
                string strBase                                          = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

                if (!Directory.Exists(strBase + @"\.minecraft"))
                {
                    Console.WriteLine("minecraft is not installed, please install");
                    return;
                }

                //Check mod folder
                if (!Directory.Exists(strBase + @"\.minecraft\mod"))
                    Directory.CreateDirectory(strBase + @"\.minecraft\mod");

                string[] astrLocalFiles                                     = Directory.GetFiles(strBase + @"\.minecraft\mod");

                CloudStorageAccount saAccount                               = CloudStorageAccount.Parse(CONNECTION_STRING);
                CloudBlobClient bClient                                     = saAccount.CreateCloudBlobClient();
                var bcContainer                                             = bClient.GetContainerReference(strValue);

                BlobContinuationToken blobContinuationToken                 = null;
                var results                                                 = await bcContainer.ListBlobsSegmentedAsync(null, blobContinuationToken);
                blobContinuationToken                                       = results.ContinuationToken;
                foreach (IListBlobItem item in results.Results)
                {
                    string strFileName = Path.GetFileName(item.Uri.ToString());
                    Console.WriteLine("Checking: " + strFileName);

                    if (!Contains(astrLocalFiles, strFileName))
                    {
                        await bcContainer.GetBlockBlobReference(strFileName).DownloadToFileAsync(strBase + @"\.minecraft\mod\" + strFileName, FileMode.Create);
                    }
                }
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
            Console.WriteLine("m3 pull <repo>");
            Console.WriteLine("m3 init <repo>");
            Console.WriteLine("m3 push <repo>");
        }

        public static string Clean(string str)
        {
            string strReturn = string.Empty;

            try
            {
                strReturn                                               = str.Trim().ToLower();
            }
            catch (Exception) { };
            return strReturn;
        }
    }
}

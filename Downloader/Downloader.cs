using Syroot.Windows.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace MultithreadingDownloader
{
    public class Downloader
    {
        private readonly string downloadPath = new KnownFolder(KnownFolderType.Downloads).Path;

        private BindingList<Download> downloads;

        public static Downloader Instance { get; private set; }

        public BindingList<Download> Downloads => downloads;

        public event Action<Action> ListUpdate;

        static Downloader()
        {
            if (Instance == null)
                Instance = new Downloader();
        }

        private Downloader()
        {
            downloads = new BindingList<Download>();
            ServicePointManager.DefaultConnectionLimit = Properties.Settings.Default.parallelDownloadsCount;
        }

        public static void GetDownloadMinMaxBytes(out long min, ref long max, int currentIndex, long length)
        {
            long partSize = length / Constants.DownloadPartsCount;

            if (max == 0)
                min = currentIndex * partSize;
            else
                min = max + 1;

            if (currentIndex < Constants.DownloadPartsCount - 1)
                max = (currentIndex + 1) * partSize;
            else
                max = length;
        }

        public async Task Download(string url, string fileName)
        {
            string[] files = Directory.GetFiles(downloadPath);
            if (files.Any(f => f.Equals($"{downloadPath}\\{fileName}", StringComparison.OrdinalIgnoreCase)))
            {
                throw new IOException("File already exists");
            }
            
            await StartDownload(url, fileName);
            await FinishDownload(fileName);
            ListUpdate?.Invoke(null);
        }

        public async Task StartDownload(string url, string fileName)
        {
            long length = Utils.GetFileLength(url);

            Task[] partDownloads = new Task[Constants.DownloadPartsCount];
            //long max = 0;
            for (int i = 0; i < Constants.DownloadPartsCount; i++)
            {
                int z = i;
                partDownloads[z] = Task.Factory.StartNew(() => 
                {
                    long max = 0;
                    GetDownloadMinMaxBytes(out long min, ref max, z, length);

                    int currentPartNumber = z;
                    long minPart = min;
                    long maxPart = max;

                    DownloadFilePart(url, fileName, minPart, maxPart, currentPartNumber);
                }, TaskCreationOptions.LongRunning);
            }

            await Task.WhenAll(partDownloads);
        }

        private async Task FinishDownload(string fileName)
        {
            var thisDownloads = downloads.Where(d => d.FileName.Contains(fileName));
            var url = thisDownloads.ToList()[0].Url;
            var partIndexes = thisDownloads.ToList().Select(d => downloads.IndexOf(d));

            foreach (var index in partIndexes)
                ListUpdate?.Invoke(() => downloads.RemoveAt(index));

            var download = new Download(fileName, url);
            download.Status = DownloadStatuses.Linking;
            ListUpdate?.Invoke(() => downloads.Add(download));
            ListUpdate?.Invoke(null);

            await Utils.PartialFileFlatsJoin(fileName, downloadPath, download);
        }

        private void DownloadFilePart(string url, string fileName, long bytesMin, long bytesMax, int partNumber)
        {
            var outputFileName = String.Format(Constants.PartialFilesNameFormat, partNumber, fileName);

            long length = bytesMax - bytesMin;
            var request = WebRequest.CreateHttp(url);
            try
            {
                request.Method = WebRequestMethods.Http.Get;
                WebProxy proxy = new WebProxy("proxy", 80);
                proxy.BypassProxyOnLocal = true;
                request.Proxy = proxy;
                request.AddRange(bytesMin, bytesMax);
                using (var response = request.GetResponse())
                using (var remoteStream = response.GetResponseStream())
                    ReadStream(remoteStream, url, outputFileName, length);

            }
            finally
            {
                request.Abort();
            }
        }

        private void ReadStream(Stream remoteStream, string url, string fileName, long length)
        {
            var download = new Download(fileName, url);
            download.Status = DownloadStatuses.InQueue;
            ListUpdate?.Invoke(() => downloads.Add(download));
            download.Size = length;
            
            download.Status = DownloadStatuses.InProgress;

            using (var content = File.Create($"{downloadPath}/{fileName}"))
            {
                var buffer = new byte[4096];
                int currentFlushStage = 1;
                int read;
                int oldPercent = 0;

                while ((read = remoteStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    content.Write(buffer, 0, read);
                    int percent = UpdateDownloadProgress(download, content.Position, length, ref oldPercent);
                    Utils.FlushBuffersIfNeed(content, percent, ref currentFlushStage);
                }

                download.Status = DownloadStatuses.Completed;
                ListUpdate?.Invoke(null);
            }
        }

        private int UpdateDownloadProgress(Download download, long position, long length, ref int oldPercent)
        {
            int percent = (int)(position * 100 / length);
            download.Progress = percent;
            if (oldPercent != percent)
            {
                ListUpdate?.Invoke(null);
                oldPercent = percent;
            }

            return percent;
        }
    }
}

using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Threading.Tasks;

namespace MultithreadingDownloader
{
    internal static class Utils
    {
        [DllImport("kernel32", SetLastError = true)]
        private static extern bool FlushFileBuffers(IntPtr handle);

        public static string ParseFileName(string url)
        {
            if (!String.IsNullOrEmpty(url) && Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
                    return Path.GetFileName(uri.LocalPath);

            return String.Empty;
        }

        public static async Task PartialFileFlatsJoin(string fileName, string folder, Download download)
        {
            using (var commonFileStream = File.Create($"{folder}/{fileName}"))
            {
                for (int i = 0; i < Constants.DownloadPartsCount; i++)
                {
                    string partialFileName = String.Format(Constants.PartialFilesNameFormat, i, fileName);
                    string filePath = $"{folder}/{partialFileName}";
                    using (var partFileStream = new FileStream(filePath, FileMode.Open))
                    {
                        byte[] buffer = new byte[4096];
                        int read;
                        while ((read = await partFileStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                        {
                            await commonFileStream.WriteAsync(buffer, 0, read);
                        }
                    }
                    FlushFileBuffers(commonFileStream.Handle);
                }
            }

            DeleteTempParts(fileName, folder);
            download.Progress = 100;
            download.Status = DownloadStatuses.Completed;
        }

        private static void DeleteTempParts(string fileName, string folder)
        {
            for (int i = 0; i < Constants.DownloadPartsCount; i++)
            {
                string partFileName = String.Format(Constants.PartialFilesNameFormat, i, fileName);
                string filePath = $"{folder}/{partFileName}";
                if (File.Exists(filePath))
                    File.Delete(String.Format(filePath));
            }
        }

        public static long GetFileLength(string url)
        {
            var getHeadersRequest = WebRequest.CreateHttp(url);
            getHeadersRequest.Method = "HEAD";
            getHeadersRequest.Credentials = CredentialCache.DefaultCredentials;

            long length;
            using (var responseH = getHeadersRequest.GetResponse())
            {
                if (responseH.ContentLength < 0)
                    throw new ServerException("Server does not provide content length");

                length = responseH.ContentLength;
            }

            return length;
        }

        public static void FlushBuffersIfNeed(FileStream content, int percent, ref int currentFlushStage)
        {
            if (percent > 0 && percent % 10 == 0 && percent / 10 == currentFlushStage)
            {
                FlushFileBuffers(content.Handle);
                currentFlushStage++;
            }
        }
    }
}

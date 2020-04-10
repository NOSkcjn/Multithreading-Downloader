using System;
using System.IO;

namespace MultithreadingDownloader
{
    public class Download
    {
        private static int currentID = 1;
        private long size;

        public int ID { get; set; }

        public string FileName { get; set; }

        public DownloadStatuses Status { get; set; }

        public long Size
        {
            get
            {
                return size;
            }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(Size), "Error file size value");

                size = value;
            }
        }

        public string SizeText
        {
            get
            {
                double gigabytes = Convert.ToDouble(Size) / 1073741824;
                if (gigabytes >= 1)
                    return String.Format("{0:#.##} Gb", gigabytes);
                double megabytes = Convert.ToDouble(Size) / 1048576;
                if (megabytes >= 1)
                    return String.Format("{0:#.##} Mb", megabytes);
                double kilobytes = Convert.ToDouble(Size) / 1024;
                if (kilobytes >= 1)
                    return String.Format("{0:#.##} Kb", kilobytes);

                return String.Format("{0:#.##} b", Size);
            }
        }

        public int Progress { get; set; }

        public string Url { get; set; }

        public FileStream Content { get; set; }

        public Download()
        {
            ID = currentID++;
        }

        public Download(string fileName, string url) : this()
        {
            FileName = fileName;
            Url = url;
        }
    }
}

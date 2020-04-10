using Syroot.Windows.IO;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
//using System.Net;
using System.Runtime.Remoting;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace MultithreadingDownloader
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeDownloads();
        }

        private void InitializeDownloads()
        {
            Downloads.ItemsSource = Downloader.Instance.Downloads;
            Downloader.Instance.ListUpdate += OnUpdate;
            //Downloader.Instance.Subscribe(Downloads.ItemsSource, OnUpdate);
        }

        private void UrlTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateFileNameOnChangeUrl();
        }

        private void UpdateFileNameOnChangeUrl()
        {
            string fileName = Utils.ParseFileName(UrlTextBox.Text);
            FileNameTextBox.Text = fileName;
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            await Download();
        }

        private async Task Download()
        {
            if (!UrlTextBox.Text.Any())
            {
                MessageBox.Show("Input url", "Error");
                return;
            }

            if (!FileNameTextBox.Text.Any())
            {
                MessageBox.Show("Input file name", "Error");
                return;
            }

            try
            {
                //await Download(UrlTextBox.Text, FileNameTextBox.Text);
                await Downloader.Instance.Download(UrlTextBox.Text, FileNameTextBox.Text);
            }
            catch (IOException e)
            {
                MessageBox.Show(e.Message, "Error");
            }
            catch (ServerException e)
            {
                MessageBox.Show(e.Message, "Server error");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ApplicationExit();
        }

        private void ApplicationExit()
        {
            Environment.Exit(0);
        }

        private void OnUpdate(Action action)
        {
            Action a = () => Downloads.Items.Refresh();
            if (action == null)
                Dispatcher.Invoke(DispatcherPriority.Background, a);
            else
            {
                Dispatcher.Invoke(DispatcherPriority.Background, action);
                Dispatcher.Invoke(DispatcherPriority.Background, a);
            }
        }
    }
}

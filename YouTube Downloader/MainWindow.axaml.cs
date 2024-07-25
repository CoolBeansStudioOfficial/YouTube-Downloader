using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Diagnostics;
using Tmds.DBus.Protocol;
using YoutubeExplode;
using YoutubeExplode.Converter;

namespace YouTube_Downloader
{
    public partial class MainWindow : Window
    {
        YoutubeClient youtube = new YoutubeClient();

        public MainWindow()
        {
            InitializeComponent();
        }
       
        public async void DownloadButton(object sender, RoutedEventArgs args)
        {
            try
            {
                await youtube.Videos.DownloadAsync(URLBox.Text, "test.mp4");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }
}
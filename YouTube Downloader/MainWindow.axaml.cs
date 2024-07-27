using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Metadata;
using Avalonia.Platform.Storage;
using AvaloniaDialogs.Views;
using System;
using System.Diagnostics;
using System.Net;
using Tmds.DBus.Protocol;
using YoutubeExplode;
using YoutubeExplode.Common;
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

        public async void SubmitButton(object sender, RoutedEventArgs args)
        {
            try
            {
                var video = await youtube.Videos.GetAsync(URLBox.Text);
                PreviewTitle.Text = video.Title;
                PreviewThumbnail.Source = await ImageHelper.LoadFromWeb(new Uri(video.Thumbnails.GetWithHighestResolution().Url));
                DownloadArea.IsVisible = true;
                
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);

                SingleActionDialog dialog = new()
                {
                    Message = "Video not found. Did you enter the URL incorrectly?",
                    ButtonText = "OK"
                };
                await dialog.ShowAsync();
            }
            
        }

        public async void DownloadButton(object sender, RoutedEventArgs args)
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
            {
                Title = "Choose where to save the download",
                //You can add either custom or from the built-in file types. See "Defining custom file types" on how to create a custom one.
                SuggestedFileName = "download",
                DefaultExtension = "mp4",
                ShowOverwritePrompt = true
            });



            try
            {
                await youtube.Videos.DownloadAsync(URLBox.Text, file.TryGetLocalPath());

                SingleActionDialog dialog = new()
                {
                    Message = "Successfully downloaded video to " + file.TryGetLocalPath(),
                    ButtonText = "OK"
                };
                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);

                SingleActionDialog dialog = new()
                {
                    Message = "Download failed. Are you connected to the internet?",
                    ButtonText = "OK"
                };
                await dialog.ShowAsync();
            }
        }
    }
}
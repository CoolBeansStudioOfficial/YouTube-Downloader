using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using AvaloniaDialogs.Views;
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

        public async void URLUpdate(object sender, RoutedEventArgs args)
        {

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
                    Message = "Download failed. Did you enter the URL incorrectly?",
                    ButtonText = "OK"
                };
                await dialog.ShowAsync();
            }
        }
    }
}
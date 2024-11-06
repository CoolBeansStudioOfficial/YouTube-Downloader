using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Metadata;
using Avalonia.Platform.Storage;
using AvaloniaDialogs.Views;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using Tmds.DBus.Protocol;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Converter;
using YoutubeExplode.Videos.Streams;

namespace YouTube_Downloader
{
    public partial class MainWindow : Window
    {
        readonly YoutubeClient youtube = new();
        private YoutubeExplode.Videos.Video video;
        private YoutubeExplode.Videos.Streams.StreamManifest manifest;

        public MainWindow()
        {
            InitializeComponent();
        }

        public async void Submit(object sender, RoutedEventArgs args)
        {
            try
            {
                //get video metadata
                video = await youtube.Videos.GetAsync(URLBox.Text);
                PreviewTitle.Text = video.Title;
                PreviewThumbnail.Source = await ImageHelper.LoadFromWeb(new Uri(video.Thumbnails.GetWithHighestResolution().Url));
                DownloadArea.IsVisible = true;

                //get all available resolutions
                manifest = await youtube.Videos.Streams.GetManifestAsync(URLBox.Text);
                foreach (var stream in manifest.GetVideoOnlyStreams())
                {
                    QualitySelect.Items.Add(stream.VideoResolution + " " + stream.Bitrate);
                }
                //select highest resolution by default
                QualitySelect.SelectedIndex = 0;

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

        public async void DownloadVideo(object sender, RoutedEventArgs args)
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
            {
                Title = "Choose where to save the download",
                //You can add either custom or from the built-in file types. See "Defining custom file types" on how to create a custom one.
                SuggestedFileName = video.Title,
                DefaultExtension = "mp4",
                ShowOverwritePrompt = true
            });



            try
            {
                //get best audio alongside user chosen resolution and download video
                var streamInfos = new IStreamInfo[] { manifest.GetAudioOnlyStreams().GetWithHighestBitrate(), manifest.GetVideoOnlyStreams().ElementAt(QualitySelect.SelectedIndex) };
                await youtube.Videos.DownloadAsync(streamInfos, new ConversionRequestBuilder(file.TryGetLocalPath()).Build());

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
                    Message = "Download failed. Is your client outdated?",
                    ButtonText = "OK"
                };
                await dialog.ShowAsync();
            }
        }
    }
}
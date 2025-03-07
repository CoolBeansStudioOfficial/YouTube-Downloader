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
                //hide download area until loading is done
                DownloadArea.IsVisible = false;

                //get video metadata and thumbnail
                video = await youtube.Videos.GetAsync(URLBox.Text);
                PreviewTitle.Text = video.Title;
                PreviewThumbnail.Source = await ImageHelper.LoadFromWeb(new Uri(video.Thumbnails.GetWithHighestResolution().Url));

                //clear old resolution list
                while (QualitySelect.Items.Count > 0)
                {
                    QualitySelect.Items.Remove(QualitySelect.Items.ElementAt(0));
                }

                //get all available resolutions
                manifest = await youtube.Videos.Streams.GetManifestAsync(URLBox.Text);
                foreach (var stream in manifest.GetVideoOnlyStreams())
                {
                    //QualitySelect.Items.Add(stream.VideoResolution + " " + stream.VideoQuality + stream.Bitrate);
                    QualitySelect.Items.Add(stream.VideoQuality + " " + stream.Bitrate);
                }

                //select highest quality by default
                foreach (var item in QualitySelect.Items)
                {
                    if (item.ToString() == (manifest.GetVideoOnlyStreams().GetWithHighestVideoQuality().VideoQuality + " " + manifest.GetVideoOnlyStreams().GetWithHighestVideoQuality().Bitrate))
                    {
                        QualitySelect.SelectedItem = item;
                    }
                }
                
                //reveal download area after loading is finished
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

        public void CheckAudio(object sender, RoutedEventArgs args)
        {
            //hide video quality selection if only audio is being downloaded
            QualitySelect.IsVisible = !AudioCheckbox.IsChecked.Value;
        }

        public async void DownloadVideo(object sender, RoutedEventArgs args)
        {
            try
            {
                //create different save options if audio only was selected
                FilePickerSaveOptions saveOptions;
                if (AudioCheckbox.IsChecked.Value)
                {
                    saveOptions = new FilePickerSaveOptions()
                    {
                        Title = "Choose where to save the download",
                        SuggestedFileName = video.Title,
                        DefaultExtension = "wav",
                        ShowOverwritePrompt = true
                    };
                }
                else
                {
                    saveOptions = new FilePickerSaveOptions()
                    {
                        Title = "Choose where to save the download",
                        SuggestedFileName = video.Title,
                        DefaultExtension = "mp4",
                        ShowOverwritePrompt = true
                    };
                }


                //prompt user to select save location
                var file = await StorageProvider.SaveFilePickerAsync(saveOptions);

                //get best audio alongside user chosen resolution and download video
                if (file is not null)
                {
                    //check if audio only was selected and if so skip muxxing video
                    if (AudioCheckbox.IsChecked.Value)
                    {
                        var streamInfos = new IStreamInfo[] { manifest.GetAudioOnlyStreams().GetWithHighestBitrate() };
                        await youtube.Videos.DownloadAsync(streamInfos, new ConversionRequestBuilder(file.TryGetLocalPath()).Build());
                    }
                    else
                    {
                        var streamInfos = new IStreamInfo[] { manifest.GetAudioOnlyStreams().GetWithHighestBitrate(), manifest.GetVideoOnlyStreams().ElementAt(QualitySelect.SelectedIndex) };
                        await youtube.Videos.DownloadAsync(streamInfos, new ConversionRequestBuilder(file.TryGetLocalPath()).Build());
                    }
                }
                else
                {
                    throw new Exception("Download cancelled");
                }


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

        public async void DownloadThumbnail(object sender, RoutedEventArgs args)
        {
            try
            {
                //prompt user to select save location
                var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
                {
                    Title = "Choose where to save the download",
                    SuggestedFileName = video.Title,
                    DefaultExtension = "png",
                    ShowOverwritePrompt = true
                });

                ImageHelper.SaveFromWeb(new Uri(video.Thumbnails.GetWithHighestResolution().Url), file.TryGetLocalPath());

                SingleActionDialog dialog = new()
                {
                    Message = "Successfully downloaded thumbnail to " + file.TryGetLocalPath(),
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
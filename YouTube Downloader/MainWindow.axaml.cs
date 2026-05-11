using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using AvaloniaDialogs.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Converter;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace YouTube_Downloader
{
    public partial class MainWindow : Window
    {
        readonly YoutubeClient youtube = new();

        Video video;
        StreamManifest manifest;

        public MainWindow()
        {
            InitializeComponent();
        }

        public async void Submit(object sender, RoutedEventArgs args)
        {
            try
            {
                //update loading progress
                LoadingBar.Value = 0;

                //show loading bar
                LoadingBar.IsVisible = true;

                //hide download area until loading is done
                DownloadArea.IsVisible = false;

                //update loading progress
                LoadingBar.Value = 25;

                //get video metadata
                video = await youtube.Videos.GetAsync(URLBox.Text.Substring(32, 11));

                //update loading progress
                LoadingBar.Value = 50;

                manifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);

                //set title
                PreviewTitle.Text = video.Title;

                //update loading progress
                LoadingBar.Value = 75;

                //set thumbnail
                PreviewThumbnail.Source = await ImageHelper.LoadFromWeb(new Uri(video.Thumbnails[0].Url));

                //clear old resolution list
                QualitySelect.Items.Clear();

                foreach (var stream in manifest.GetVideoOnlyStreams())
                {
                    string label = $"{stream.VideoQuality} {stream.Bitrate}";

                    //select highest quality by default
                    if (stream == manifest.GetVideoOnlyStreams().GetWithHighestVideoQuality())
                    {
                        var item = QualitySelect.Items.Add($"{label} (Highest Quality)");
                        QualitySelect.SelectedIndex = item;
                    }
                    else
                    {
                        QualitySelect.Items.Add(label);
                    }
                }

                //reveal download area after loading is finished
                DownloadArea.IsVisible = true;

                //hide loading bar
                LoadingBar.IsVisible = false;

            }
            catch (Exception ex)
            {
                //hide loading bar
                LoadingBar.IsVisible = false;

                Debug.WriteLine(ex);

                SingleActionDialog dialog = new()
                {
                    Message = "Video not found. The URL may not have been entered incorrectly",
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
                IStorageFile? file;
                if (AudioCheckbox.IsChecked.Value) file = await FilePrompt(FileType.Audio, video.Title);
                else file = await FilePrompt(FileType.Video, video.Title);

                //get user chosen resolution and download video
                if (file is not null)
                {
                    var streamInfos = new IStreamInfo[] { manifest.GetAudioOnlyStreams().GetWithHighestBitrate(), manifest.GetVideoOnlyStreams().ElementAt(QualitySelect.SelectedIndex) };
                    await youtube.Videos.DownloadAsync(streamInfos, new ConversionRequestBuilder(file.TryGetLocalPath()).Build());
                }
                else throw new Exception("Download cancelled");

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
                    Message = "Download failed. That can happen if you don't have an internet connection or this app is outdated.",
                    ButtonText = "OK"
                };
                await dialog.ShowAsync();
            }
        }

        public async void DownloadThumbnail(object sender, RoutedEventArgs args)
        {
            //prompt user to select save location
            var file = await FilePrompt(FileType.Image, video.Title);

            if (file is not null)
            {
                try
                {
                    ImageHelper.SaveFromWeb(new Uri(video.Thumbnails.GetWithHighestResolution().Url), file.TryGetLocalPath());

                    SingleActionDialog dialog = new()
                    {
                        Message = "Successfully downloaded thumbnail to " + file.TryGetLocalPath(),
                        ButtonText = "OK"
                    };
                    await dialog.ShowAsync();
                }
                catch
                {
                    SingleActionDialog dialog = new()
                    {
                        Message = "Download failed. That can happen if you don't have an internet connection or this app is outdated.",
                        ButtonText = "OK"
                    };
                    await dialog.ShowAsync();
                }
            }
            else
            {
                SingleActionDialog dialog = new()
                {
                    Message = "Download cancelled",
                    ButtonText = "OK"
                };
                await dialog.ShowAsync();
            }
        }

        public enum FileType
        {
            Video,
            Audio,
            Image
        }

        public async Task<IStorageFile?> FilePrompt(FileType type, string filename)
        {
            //remove invalid characters from filename
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                filename = filename.Replace(c, '_');
            }

            FilePickerSaveOptions options;

            if (type == FileType.Video)
            {
                
                options = new FilePickerSaveOptions()
                {
                    Title = "Choose where to save the download",
                    SuggestedFileName = filename,
                    DefaultExtension = "mp4",
                    ShowOverwritePrompt = true
                };
            }
            else if (type == FileType.Audio)
            {
                options = new FilePickerSaveOptions()
                {
                    Title = "Choose where to save the download",
                    SuggestedFileName = filename,
                    DefaultExtension = "wav",
                    ShowOverwritePrompt = true
                };
            }
            else
            {
                options = new FilePickerSaveOptions()
                {
                    Title = "Choose where to save the download",
                    SuggestedFileName = filename,
                    DefaultExtension = ".jpg",
                    ShowOverwritePrompt = true
                };
            }

            Debug.WriteLine(options.DefaultExtension);

            return await StorageProvider.SaveFilePickerAsync(options);
        }
    }
}
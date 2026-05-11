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
using VideoLibrary;

namespace YouTube_Downloader
{
    public partial class MainWindow : Window
    {
        YouTube youtube = YouTube.Default;

        IEnumerable<YouTubeVideo> videos;
        string videoId;
        List<int> availableResolutions = new();

        public MainWindow()
        {
            InitializeComponent();
        }

        public async void Submit(object sender, RoutedEventArgs args)
        {
            try
            {
                string url = URLBox.Text;

                //hide download area until loading is done
                DownloadArea.IsVisible = false;

                //show loading bar
                LoadingBar.IsVisible = true;

                //update loading progress
                LoadingBar.Value = 33;

                //get video metadata
                videos = await youtube.GetAllVideosAsync(url);

                //get video id
                videoId = url.Substring(32, 11);
                Debug.WriteLine(videoId);
                Debug.WriteLine($"https://img.youtube.com/vi/{videoId}/maxresdefault");

                //set title
                PreviewTitle.Text = videos.First().Title;

                //update loading progress
                LoadingBar.Value = 66;

                //set thumbnail
                PreviewThumbnail.Source = await ImageHelper.LoadFromWeb(new Uri($"https://img.youtube.com/vi/{videoId}/maxresdefault.jpg"));


                //clear old resolution list
                while (QualitySelect.Items.Count > 0)
                {
                    QualitySelect.Items.Remove(QualitySelect.Items.ElementAt(0));
                }
                availableResolutions.Clear();

                int highestRes = 0;
                foreach (var video in videos)
                {
                    //QualitySelect.Items.Add(stream.VideoResolution + " " + stream.VideoQuality + stream.Bitrate);
                    var item = QualitySelect.Items.Add(video.Resolution);
                    availableResolutions.Add(video.Resolution);

                    //select highest quality by default
                    if (video.Resolution > highestRes)
                    {
                        QualitySelect.SelectedIndex = item;
                        highestRes = video.Resolution;
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
                IStorageFile file;
                if (AudioCheckbox.IsChecked.Value)
                {
                    file = await FilePrompt(FileType.Audio, videos.First().FullName);                
                }
                else
                {
                    file = await FilePrompt(FileType.Video, videos.First().FullName);
                }

                Debug.WriteLine("file path is: " + file.TryGetLocalPath());

                //get user chosen resolution and download video
                if (file is not null)
                {
                    YouTubeVideo selectedResolution = videos.Where(i => i.Resolution == availableResolutions[QualitySelect.SelectedIndex]).First();
                    var bytes = await selectedResolution.GetBytesAsync();
                    
                    File.WriteAllBytes(file.TryGetLocalPath(), bytes);
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
            try
            {
                //prompt user to select save location
                var file = await FilePrompt(FileType.Image, videos.First().FullName);

                ImageHelper.SaveFromWeb(new Uri($"https://img.youtube.com/vi/{videoId}/maxresdefault.jpg"), file.TryGetLocalPath());

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

        public enum FileType
        {
            Video,
            Audio,
            Image
        }

        public async Task<IStorageFile?> FilePrompt(FileType type, string filename)
        {
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
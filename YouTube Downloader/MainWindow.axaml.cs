using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Diagnostics;
using Tmds.DBus.Protocol;

namespace YouTube_Downloader
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
       
        public void DownloadButton(object sender, RoutedEventArgs args)
        {
            Debug.WriteLine("Button clicked!");
        }
    }
}
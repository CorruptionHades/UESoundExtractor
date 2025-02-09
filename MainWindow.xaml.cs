using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Win32;
using UESoundExtractor.utils;

namespace UESoundExtractor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Settings.LoadSettings();
            
            PaksFolderTextBox.Text = Settings.settings.PaksFolder;
            AesKeyTextBox.Text = Settings.settings.AesKey;
            OutputFolderTextBox.Text = Settings.settings.OutputFolder;
            
            this.Closing += MainWindow_Closing;
        }
        
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Settings.settings.PaksFolder = PaksFolderTextBox.Text;
            Settings.settings.AesKey = AesKeyTextBox.Text;
            Settings.settings.OutputFolder = OutputFolderTextBox.Text;
            Settings.SaveSettings();
        }

        private void BrowsePaksFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Paks Folder",
                Filter = "All Files (*.*)|*.*",
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Select Folder"
            };

            if (dialog.ShowDialog() == true)
            {
                PaksFolderTextBox.Text = Path.GetDirectoryName(dialog.FileName);
                Settings.settings.PaksFolder = PaksFolderTextBox.Text;
            }
        }

        private void BrowseOutputFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Output Folder",
                Filter = "All Files (*.*)|*.*",
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Select Folder"
            };

            if (dialog.ShowDialog() == true)
            {
                OutputFolderTextBox.Text = Path.GetDirectoryName(dialog.FileName);
                Settings.settings.OutputFolder = OutputFolderTextBox.Text;
            }
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            Settings.settings.PaksFolder = PaksFolderTextBox.Text;
            Settings.settings.AesKey = AesKeyTextBox.Text;
            Settings.settings.OutputFolder = OutputFolderTextBox.Text;
            Settings.SaveSettings();
            
            MainPage loadingPage = new();
            this.Content = loadingPage;
        }
    }

    public class AppSettings
    {
        public string PaksFolder { get; set; }
        public string AesKey { get; set; }
        public string OutputFolder { get; set; }
        
        public HashSet<String> EventFolders { get; set; } = new();
    }
}
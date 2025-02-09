using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using UESoundExporter;
using UESoundExtractor.utils;

namespace UESoundExtractor
{
    public partial class MainPage : Page, INotifyPropertyChanged 
    {
        private ObservableCollection<FolderItem> _folderItems = new ObservableCollection<FolderItem>();
        private ObservableCollection<PackageItem> _packageItems = new ObservableCollection<PackageItem>();
        private PackageItem _selectedPackage;
        private HashSet<string> _loadedPaths = new HashSet<string>();

        public PackageItem SelectedPackage
        {
            get => _selectedPackage;
            set
            {
                _selectedPackage = value;
                PackageDetailsPanel.Visibility = value != null ? Visibility.Visible : Visibility.Collapsed;
                PackageDetailsPanel.DataContext = this;
                OnPropertyChanged("SelectedPackage");   
            }
        }

        public MainPage()
        {
            InitializeComponent();
            LoadDataAsync();
        }

        private async void LoadDataAsync()
        {
            // Initialize the service
            await Task.Run(() => Service.init());

            // Get all audio files
            var audioFiles = await Task.Run(() => Service.getAllAudio());
            
            // Build the folder structure for regular audio files
            BuildFolderStructure(audioFiles);

            // Load event folders
            await LoadEventFolders();

            // Hide loading and show main content
            LoadingProgressBar.Visibility = Visibility.Collapsed;
            MainContent.Visibility = Visibility.Visible;

            // Bind the folder structure
            FoldersTreeView.ItemsSource = _folderItems;
            RefreshEventFoldersList();
        }

        private async Task LoadEventFolders()
        {
            foreach (var eventFolderPath in Settings.settings.EventFolders)
            {
                var eventFiles = await Task.Run(() => Service.GetFilesInEventFolder(eventFolderPath));
                AddEventFolderToStructure(eventFolderPath, eventFiles);
            }
        }

        private void AddEventFolderToStructure(string eventFolderPath, List<string> eventFiles)
        {
            if (_loadedPaths.Contains(eventFolderPath)) return;
            
            var eventFolder = new FolderItem
            {
                Name = System.IO.Path.GetFileName(eventFolderPath),
                Path = eventFolderPath,
                FileCount = eventFiles.Count,
                IsEventFolder = true
            };

            foreach (var filePath in eventFiles)
            {
                _packageItems.Add(new PackageItem
                {
                    Name = System.IO.Path.GetFileName(filePath),
                    Path = filePath,
                    FolderPath = eventFolderPath
                });
            }

            _folderItems.Add(eventFolder);
            _loadedPaths.Add(eventFolderPath);
        }

        private void BuildFolderStructure(List<string> audioFiles)
        {
            foreach (var filePath in audioFiles)
            {
                var parts = filePath.Split('/');
                var currentNode = _folderItems;
                var currentPath = "";

                for (int i = 0; i < parts.Length - 1; i++)
                {
                    var part = parts[i];
                    currentPath += (currentPath == "" ? "" : "/") + part;
                    
                    if (_loadedPaths.Contains(currentPath)) continue;
                    
                    var existingNode = currentNode.FirstOrDefault(x => x.Name == part);

                    if (existingNode == null)
                    {
                        var newNode = new FolderItem { 
                            Name = part, 
                            Path = currentPath,
                            FileCount = 0
                        };
                        currentNode.Add(newNode);
                        currentNode = newNode.Children;
                    }
                    else
                    {
                        currentNode = existingNode.Children;
                    }
                }

                _packageItems.Add(new PackageItem
                {
                    Name = parts.Last(),
                    Path = filePath,
                    FolderPath = currentPath
                });

                UpdateFolderFileCount(currentPath);
            }

            CombineSingleChildFolders(_folderItems);
        }

        private void UpdateFolderFileCount(string folderPath)
        {
            var folder = FindFolder(_folderItems, folderPath);
            if (folder != null)
            {
                folder.FileCount = _packageItems.Count(p => p.FolderPath == folderPath);
            }
        }

        private FolderItem FindFolder(ObservableCollection<FolderItem> items, string path)
        {
            foreach (var item in items)
            {
                if (item.Path == path) return item;
                var found = FindFolder(item.Children, path);
                if (found != null) return found;
            }
            return null;
        }

        private void CombineSingleChildFolders(ObservableCollection<FolderItem> nodes)
        {
            foreach (var node in nodes.ToList())
            {
                if (node.Children.Count == 1 && node.Children[0].Children.Count > 0)
                {
                    var child = node.Children[0];
                    node.Name += "/" + child.Name;
                    node.Path = child.Path;
                    node.FileCount = child.FileCount;
                    node.Children = child.Children;
                    CombineSingleChildFolders(nodes);
                }
                else
                {
                    CombineSingleChildFolders(node.Children);
                }
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(FoldersTreeView == null) return;
            
            string searchText = FolderSearchBox.Text.ToLower();
            FilterFolderView(searchText);
        }

        private void PackageSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = PackageSearchBox.Text.ToLower();
            FilterPackageView(searchText);
        }

        private void FilterFolderView(string searchText)
        {
            
            if(FoldersTreeView == null) return;
            
            if (string.IsNullOrEmpty(searchText))
            {
                FoldersTreeView.ItemsSource = _folderItems;
            }
            else
            {
                var filteredFolders = FilterFolderTree(_folderItems, searchText);
                FoldersTreeView.ItemsSource = filteredFolders;
            }
        }

        private void FilterPackageView(string searchText)
        {
            if(FoldersTreeView == null || PackagesListView == null) return;
            
            if (string.IsNullOrEmpty(searchText))
            {
                PackagesListView.ItemsSource = _packageItems;
            }
            else
            {
                var filteredPackages = _packageItems.Where(p => 
                    p.Name.ToLower().Contains(searchText) || 
                    p.Path.ToLower().Contains(searchText));
                PackagesListView.ItemsSource = new ObservableCollection<PackageItem>(filteredPackages);
            }
        }

        private ObservableCollection<FolderItem> FilterFolderTree(ObservableCollection<FolderItem> nodes, string searchText)
        {
            var filteredNodes = new ObservableCollection<FolderItem>();

            foreach (var node in nodes)
            {
                if (node.Name.ToLower().Contains(searchText))
                {
                    var newNode = new FolderItem { 
                        Name = node.Name, 
                        Path = node.Path,
                        FileCount = node.FileCount
                    };
                    foreach (var child in node.Children)
                    {
                        newNode.Children.Add(child);
                    }
                    filteredNodes.Add(newNode);
                }
                else
                {
                    var filteredChildren = FilterFolderTree(node.Children, searchText);
                    if (filteredChildren.Count > 0)
                    {
                        var newNode = new FolderItem { 
                            Name = node.Name, 
                            Path = node.Path,
                            FileCount = node.FileCount
                        };
                        foreach (var child in filteredChildren)
                        {
                            newNode.Children.Add(child);
                        }
                        filteredNodes.Add(newNode);
                    }
                }
            }

            return filteredNodes;
        }

        private void FoldersTreeView_Selected(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is TreeViewItem treeViewItem && 
                treeViewItem.DataContext is FolderItem folderItem)
            {
                var packagesInFolder = _packageItems.Where(p => p.FolderPath == folderItem.Path);
                PackagesListView.ItemsSource = new ObservableCollection<PackageItem>(packagesInFolder);
            }
        }

        private void PackagesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PackagesListView.SelectedItem is PackageItem selectedPackage)
            {
                if (SelectedPackage != null) {
                    SelectedPackage.nameMap.Clear();
                    
                    Dispatcher.Invoke(() =>
                    {
                        PDPNameMap.Text = "";
                    });
                }
                
                SelectedPackage = selectedPackage;
                
                // laod in the name map in background
                Task.Run(() =>
                {
                    FetchPkgInfo(selectedPackage);
                });
            }
        }

        public void FetchPkgInfo(PackageItem selectedPackage) {

            if (selectedPackage.Path.EndsWith(".uasset")) {
                var nameMap = Service.LoadNameMap(selectedPackage.Path);
                foreach (var (key, value) in nameMap)
                {
                    SelectedPackage.nameMap.Add(key, value);
                }
                    
                string combinedWithNewline = string.Join("\n", nameMap.Select(x => $"{x.Key} -> {x.Value}"));
                    
                Console.WriteLine("Loaded name map for " + selectedPackage.Path);
                Console.WriteLine(combinedWithNewline);
                    
                Dispatcher.Invoke(() =>
                {
                    PDPNameMap.Text = combinedWithNewline;
                    PDPExtractBtn.Visibility = Visibility.Collapsed;
                    OnPropertyChanged("SelectedPackage");
                });
            }
            else if (selectedPackage.Path.EndsWith(".wem")) {
                // add an extract button
                Dispatcher.Invoke(() =>
                {
                    PDPNameMap.Text = "This is a .wem audio file. Use the extract button to convert it to .wav.";
                    PDPExtractBtn.Visibility = Visibility.Visible;
                    OnPropertyChanged("SelectedPackage");
                });
            }
            else if (selectedPackage.Path.EndsWith(".bnk")) {

                HashSet<String> srcIds = BnkExtractor.GetPossibleMediaIDS(selectedPackage.Path);
                Console.WriteLine("Found " + srcIds.Count + " media ids.");
                
                String newLineCombined = string.Join("\n", srcIds);
                
                // add an extract button
                Dispatcher.Invoke(() =>
                {
                    PDPNameMap.Text = "This is a .bnk file which contains references to audio files. \n" + newLineCombined;
                    PDPExtractBtn.Visibility = Visibility.Collapsed;
                    OnPropertyChanged("SelectedPackage");
                });
            }
        }
        
        private void ExtractButton_Click(object sender, RoutedEventArgs e) {
            Console.WriteLine("todo");
            Task.Run(() =>
            {
                var pkgPath = SelectedPackage.Path;
                var nameMap = SelectedPackage.nameMap;

                if (pkgPath.EndsWith(".uasset")) {
                    Console.WriteLine("Extarcting uasset");
                    WemExtractor.ExtractNameMap(pkgPath, nameMap);
                }
                else if (pkgPath.EndsWith(".wem")) {
                    Console.WriteLine("Extarcting wem");
                    WemExtractor.Extract(pkgPath, pkgPath.Split("/").Last());
                }
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null) {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        
        // Update the code-behind file `LoadingPage.xaml.cs`
        private async void AddEventFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddEventFolderDialog();
            if (dialog.ShowDialog() == true)
            {
                var newPath = dialog.FolderPath;
                
                // Verify the path exists in the provider
                var eventFiles = await Task.Run(() => Service.GetFilesInEventFolder(newPath));
                
                if (eventFiles.Any())
                {
                    Settings.settings.EventFolders.Add(newPath);
                    Settings.SaveSettings();
                    
                    // Add to folder structure
                    AddEventFolderToStructure(newPath, eventFiles);
                    RefreshEventFoldersList();
                }
                else
                {
                    MessageBox.Show("No audio files found in the specified path.", "Invalid Path", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void DeleteEventFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (EventFoldersListBox.SelectedItem is string selectedFolder)
            {
                // Remove from settings
                Settings.settings.EventFolders.Remove(selectedFolder);
                Settings.SaveSettings();
                
                // Remove from folder structure
                var folderToRemove = _folderItems.FirstOrDefault(f => f.Path == selectedFolder);
                if (folderToRemove != null)
                {
                    _folderItems.Remove(folderToRemove);
                    _loadedPaths.Remove(selectedFolder);
                    
                    // Remove associated packages
                    var packagesToRemove = _packageItems.Where(p => p.FolderPath == selectedFolder).ToList();
                    foreach (var package in packagesToRemove)
                    {
                        _packageItems.Remove(package);
                    }
                }
                
                RefreshEventFoldersList();
            }
        }

        private void RefreshEventFoldersList()
        {
            Task.Run(() =>
            {
                var eventFolders = Service.GetEventFolders();
                Dispatcher.Invoke(() =>
                {
                    foreach (var folder in eventFolders)
                    {
                        _folderItems.Add(new FolderItem
                        {
                            Name = folder,
                            Path = folder,
                            FileCount = 0
                        });
                    }
                    Settings.SaveSettings();
                    EventFoldersListBox.ItemsSource = null;
                    EventFoldersListBox.ItemsSource = Settings.settings.EventFolders;
                });
            });
            
            EventFoldersListBox.ItemsSource = null;
            EventFoldersListBox.ItemsSource = Settings.settings.EventFolders;
        }

        private void ExportFolderButtonClick(object sender, RoutedEventArgs e) {
            // open explorer
            System.Diagnostics.Process.Start("explorer.exe", Settings.settings.OutputFolder);
        }

        private void DirectExtract_Click(object sender, RoutedEventArgs e) {
            string path = DirectExtractTB.Text;
            string customName = DirectExtractCustomName.Text;
            DirectExtractProgress.Text = "Extracting...";
            if (path.EndsWith(".bnk")) {
            }
            else {
                DirectExtractProgress.Text = "Assuming .wem file...";
                Task.Run(() =>
                {
                    int pathLength = path.Split("/").Length;
                    
                    // if no path add Shootergame/Content/WwiseAudio/Media
                    // 406925269
                    if (pathLength == 1) {
                        path = "Shootergame/Content/WwiseAudio/Media/" + path;
                    }
                    
                    // if no extension add .wem
                    if (!path.EndsWith(".wem")) {
                        path += ".wem";
                    }
                    
                    string wavPath = WemExtractor.Extract(path, customName.Length == 0 ? path.Split("/").Last() : customName);
                    Dispatcher.Invoke(() =>
                    {
                        DirectExtractProgress.Text = "Extracted to " + wavPath;
                    });
                });
            }
        }
    }

    public class FolderItem
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public int FileCount { get; set; }
        public bool IsEventFolder { get; set; }
        public ObservableCollection<FolderItem> Children { get; set; } = new();
    }

    public class PackageItem
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string FolderPath { get; set; }
        
        public string DisplayName => System.IO.Path.GetFileNameWithoutExtension(Name);
        
        public Dictionary<string, string> nameMap = new();
    }
}
using System.Windows;

namespace UESoundExtractor;

// Add the code-behind for the dialog `AddEventFolderDialog.xaml.cs`
public partial class AddEventFolderDialog : Window
{
    public string FolderPath => FolderPathTextBox.Text;

    public AddEventFolderDialog()
    {
        InitializeComponent();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
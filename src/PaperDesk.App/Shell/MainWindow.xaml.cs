using System.Windows;
using Microsoft.Win32;

namespace PaperDesk.App.Shell;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext;

    private async void BrowseFolderClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Multiselect = false,
            Title = "Select a folder to watch"
        };

        if (dialog.ShowDialog() == true)
        {
            ViewModel.WatchedFolderInput = dialog.FolderName;
            await ViewModel.AddWatchedFolderAsync(dialog.FolderName, CancellationToken.None);
        }
    }

    private async void AddFolderClick(object sender, RoutedEventArgs e)
        => await ViewModel.AddWatchedFolderAsync(ViewModel.WatchedFolderInput, CancellationToken.None);

    private async void RemoveFolderClick(object sender, RoutedEventArgs e)
        => await ViewModel.RemoveSelectedFolderAsync(CancellationToken.None);

    private async void StartWatchingClick(object sender, RoutedEventArgs e)
        => await ViewModel.StartWatchingAsync(CancellationToken.None);

    private async void StopWatchingClick(object sender, RoutedEventArgs e)
        => await ViewModel.StopWatchingAsync(CancellationToken.None);

    protected override async void OnClosed(EventArgs e)
    {
        await ViewModel.StopWatchingAsync(CancellationToken.None);
        ViewModel.Dispose();
        base.OnClosed(e);
    }
}

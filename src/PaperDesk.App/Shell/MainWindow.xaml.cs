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

    private async void SearchClick(object sender, RoutedEventArgs e)
        => await ViewModel.SearchAsync(CancellationToken.None);

    private async void RefreshDuplicatesClick(object sender, RoutedEventArgs e)
        => await ViewModel.RefreshDuplicatesAsync(CancellationToken.None);

    private async void MarkCanonicalClick(object sender, RoutedEventArgs e)
        => await ViewModel.MarkSelectedDuplicateAsCanonicalAsync(CancellationToken.None);

    private async void MoveDuplicatesClick(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Move all non-canonical files in this group to the Review Duplicates folder?",
            "Confirm Duplicate Action",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        await ViewModel.MoveNonCanonicalDuplicatesAsync(CancellationToken.None);
    }

    private async void RefreshReviewQueueClick(object sender, RoutedEventArgs e)
        => await ViewModel.RefreshReviewQueueAsync(CancellationToken.None);

    private async void ApproveSelectedClick(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Apply the selected rename/move suggestion now?",
            "Confirm Apply",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        await ViewModel.ApplySelectedSuggestionAsync(CancellationToken.None);
    }

    private async void SkipSelectedClick(object sender, RoutedEventArgs e)
        => await ViewModel.SkipSelectedSuggestionAsync(CancellationToken.None);

    protected override async void OnClosed(EventArgs e)
    {
        await ViewModel.StopWatchingAsync(CancellationToken.None);
        ViewModel.Dispose();
        base.OnClosed(e);
    }
}

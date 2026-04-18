using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using PaperDesk.Application.Abstractions;
using PaperDesk.Application.Services;
using PaperDesk.Domain.Entities;
using PaperDesk.Domain.Enums;
using PaperDesk.Domain.ValueObjects;

namespace PaperDesk.App.Shell;

public sealed class MainWindowViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IFolderWatcherService folderWatcherService;
    private readonly IWatchedFolderValidator watchedFolderValidator;
    private readonly IActivityLog activityLog;
    private readonly IFileProcessingQueue fileProcessingQueue;
    private readonly DocumentIngestionCoordinator ingestionCoordinator;
    private readonly DispatcherTimer refreshTimer;
    private readonly SemaphoreSlim tickGate = new(1, 1);
    private bool isDisposed;
    private bool isWatching;
    private string watchedFolderInput = string.Empty;
    private string statusMessage = "Ready";
    private WatchedFolder? selectedWatchedFolder;

    public MainWindowViewModel(
        IFolderWatcherService folderWatcherService,
        IWatchedFolderValidator watchedFolderValidator,
        IActivityLog activityLog,
        IFileProcessingQueue fileProcessingQueue,
        DocumentIngestionCoordinator ingestionCoordinator)
    {
        this.folderWatcherService = folderWatcherService;
        this.watchedFolderValidator = watchedFolderValidator;
        this.activityLog = activityLog;
        this.fileProcessingQueue = fileProcessingQueue;
        this.ingestionCoordinator = ingestionCoordinator;

        NavigationItems =
        [
            "Dashboard",
            "Watched Folders",
            "Review Queue",
            "Search",
            "Duplicates",
            "Activity Log",
            "Settings",
        ];

        refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(400)
        };
        refreshTimer.Tick += OnRefreshTick;
        refreshTimer.Start();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Title => "PaperDesk";

    public string PhaseStatus
        => isWatching
            ? "Watching folders and processing queue"
            : "Phase 1 pipeline foundation ready";

    public IReadOnlyCollection<string> NavigationItems { get; }

    public ObservableCollection<WatchedFolder> WatchedFolders { get; } = [];

    public ObservableCollection<RenameMovePreview> PendingPreviews { get; } = [];

    public ObservableCollection<ActivityEventItemViewModel> ActivityEvents { get; } = [];

    public string WatchedFolderInput
    {
        get => watchedFolderInput;
        set => SetProperty(ref watchedFolderInput, value);
    }

    public string StatusMessage
    {
        get => statusMessage;
        private set => SetProperty(ref statusMessage, value);
    }

    public bool IsWatching
    {
        get => isWatching;
        private set
        {
            if (SetProperty(ref isWatching, value))
            {
                OnPropertyChanged(nameof(PhaseStatus));
            }
        }
    }

    public WatchedFolder? SelectedWatchedFolder
    {
        get => selectedWatchedFolder;
        set => SetProperty(ref selectedWatchedFolder, value);
    }

    public int QueueCount => fileProcessingQueue.Snapshot().Count;

    public async Task<bool> AddWatchedFolderAsync(string folderPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            StatusMessage = "Choose or enter a folder path.";
            return false;
        }

        var normalizedPath = Path.GetFullPath(folderPath.Trim());
        if (WatchedFolders.Any(folder => string.Equals(Path.GetFullPath(folder.Path), normalizedPath, StringComparison.OrdinalIgnoreCase)))
        {
            StatusMessage = "Folder is already in the watch list.";
            return false;
        }

        var candidate = new WatchedFolder
        {
            Path = normalizedPath,
            IncludedExtensions = [".pdf", ".jpg", ".jpeg", ".png", ".tif", ".tiff"]
        };
        var validation = watchedFolderValidator.Validate(candidate);
        if (!validation.IsValid)
        {
            StatusMessage = validation.Error ?? "Folder is invalid.";
            return false;
        }

        WatchedFolders.Add(candidate);
        WatchedFolderInput = string.Empty;
        StatusMessage = "Folder added.";

        if (IsWatching)
        {
            await folderWatcherService.StartAsync(WatchedFolders.ToArray(), cancellationToken);
            StatusMessage = "Watcher reloaded with the new folder.";
        }

        return true;
    }

    public async Task RemoveSelectedFolderAsync(CancellationToken cancellationToken)
    {
        if (SelectedWatchedFolder is null)
        {
            StatusMessage = "Select a folder to remove.";
            return;
        }

        var removedPath = SelectedWatchedFolder.Path;
        WatchedFolders.Remove(SelectedWatchedFolder);
        SelectedWatchedFolder = null;
        StatusMessage = $"Removed {removedPath}.";

        if (IsWatching)
        {
            if (WatchedFolders.Count == 0)
            {
                await StopWatchingAsync(cancellationToken);
                return;
            }

            await folderWatcherService.StartAsync(WatchedFolders.ToArray(), cancellationToken);
            StatusMessage = "Watcher reloaded after folder removal.";
        }
    }

    public async Task StartWatchingAsync(CancellationToken cancellationToken)
    {
        if (WatchedFolders.Count == 0)
        {
            StatusMessage = "Add at least one folder before starting.";
            return;
        }

        await folderWatcherService.StartAsync(WatchedFolders.ToArray(), cancellationToken);
        IsWatching = true;
        StatusMessage = "Watching started.";
    }

    public async Task StopWatchingAsync(CancellationToken cancellationToken)
    {
        await folderWatcherService.StopAsync(cancellationToken);
        IsWatching = false;
        StatusMessage = "Watching stopped.";
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        refreshTimer.Stop();
        refreshTimer.Tick -= OnRefreshTick;
        tickGate.Dispose();
        isDisposed = true;
    }

    private async void OnRefreshTick(object? sender, EventArgs e)
    {
        if (!await tickGate.WaitAsync(0))
        {
            return;
        }

        try
        {
            for (var index = 0; index < 4; index++)
            {
                var preview = await ingestionCoordinator.ProcessNextAsync(CancellationToken.None);
                if (preview is null)
                {
                    break;
                }

                if (!PendingPreviews.Any(existing => string.Equals(existing.ProposedPath, preview.ProposedPath, StringComparison.OrdinalIgnoreCase)))
                {
                    PendingPreviews.Insert(0, preview);
                    if (PendingPreviews.Count > 200)
                    {
                        PendingPreviews.RemoveAt(PendingPreviews.Count - 1);
                    }
                }
            }

            RefreshActivityView();
            OnPropertyChanged(nameof(QueueCount));
        }
        finally
        {
            tickGate.Release();
        }
    }

    private void RefreshActivityView()
    {
        var snapshot = activityLog.Snapshot()
            .OrderByDescending(item => item.OccurredUtc ?? DateTimeOffset.MinValue)
            .Take(200)
            .Select(item => new ActivityEventItemViewModel(
                item.OccurredUtc ?? DateTimeOffset.UtcNow,
                item.ActivityType,
                item.Message,
                item.Path))
            .ToArray();

        ActivityEvents.Clear();
        foreach (var item in snapshot)
        {
            ActivityEvents.Add(item);
        }
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed record ActivityEventItemViewModel(
    DateTimeOffset OccurredUtc,
    ActivityType ActivityType,
    string Message,
    string? Path);

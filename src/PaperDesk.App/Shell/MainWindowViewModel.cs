using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using PaperDesk.Application.Abstractions;
using PaperDesk.Application.Queries;
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
    private readonly IDocumentIndexService documentIndexService;
    private readonly IDuplicateDetectionService duplicateDetectionService;
    private readonly IDocumentRepository documentRepository;
    private readonly IRenameSuggestionRepository renameSuggestionRepository;
    private readonly DocumentIngestionCoordinator ingestionCoordinator;
    private readonly DispatcherTimer refreshTimer;
    private readonly SemaphoreSlim tickGate = new(1, 1);
    private bool isDisposed;
    private bool isWatching;
    private string watchedFolderInput = string.Empty;
    private string searchQuery = string.Empty;
    private string searchSourceFolder = string.Empty;
    private string searchFromDate = string.Empty;
    private string searchToDate = string.Empty;
    private string selectedSearchDocumentType = "All";
    private string selectedSearchStatus = "All";
    private string statusMessage = "Ready";
    private WatchedFolder? selectedWatchedFolder;
    private ReviewSuggestionItemViewModel? selectedReviewSuggestion;
    private DuplicateGroupItemViewModel? selectedDuplicateGroup;
    private DuplicateDocumentItemViewModel? selectedDuplicateDocument;
    private readonly Dictionary<Guid, List<DuplicateDocumentItemViewModel>> duplicateGroupDocuments = [];

    public MainWindowViewModel(
        IFolderWatcherService folderWatcherService,
        IWatchedFolderValidator watchedFolderValidator,
        IActivityLog activityLog,
        IFileProcessingQueue fileProcessingQueue,
        IDocumentIndexService documentIndexService,
        IDuplicateDetectionService duplicateDetectionService,
        IDocumentRepository documentRepository,
        IRenameSuggestionRepository renameSuggestionRepository,
        DocumentIngestionCoordinator ingestionCoordinator)
    {
        this.folderWatcherService = folderWatcherService;
        this.watchedFolderValidator = watchedFolderValidator;
        this.activityLog = activityLog;
        this.fileProcessingQueue = fileProcessingQueue;
        this.documentIndexService = documentIndexService;
        this.duplicateDetectionService = duplicateDetectionService;
        this.documentRepository = documentRepository;
        this.renameSuggestionRepository = renameSuggestionRepository;
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
            Interval = TimeSpan.FromMilliseconds(500)
        };
        refreshTimer.Tick += OnRefreshTick;
        refreshTimer.Start();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Title => "PaperDesk";

    public string PhaseStatus
        => isWatching
            ? "Watching folders and processing queue"
            : "Workflow ready";

    public IReadOnlyCollection<string> NavigationItems { get; }

    public ObservableCollection<WatchedFolder> WatchedFolders { get; } = [];

    public ObservableCollection<RenameMovePreview> PendingPreviews { get; } = [];

    public ObservableCollection<ReviewSuggestionItemViewModel> ReviewSuggestions { get; } = [];

    public ObservableCollection<ActivityEventItemViewModel> ActivityEvents { get; } = [];

    public ObservableCollection<SearchResultItemViewModel> SearchResults { get; } = [];
    public ObservableCollection<DuplicateGroupItemViewModel> DuplicateGroups { get; } = [];
    public ObservableCollection<DuplicateDocumentItemViewModel> DuplicateDocuments { get; } = [];

    public IReadOnlyCollection<string> SearchDocumentTypes { get; } =
    [
        "All",
        ..Enum.GetNames<DocumentType>()
    ];

    public IReadOnlyCollection<string> SearchStatuses { get; } =
    [
        "All",
        ..Enum.GetNames<ProcessingStatus>()
    ];

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

    public string SearchQuery
    {
        get => searchQuery;
        set => SetProperty(ref searchQuery, value);
    }

    public string SearchSourceFolder
    {
        get => searchSourceFolder;
        set => SetProperty(ref searchSourceFolder, value);
    }

    public string SearchFromDate
    {
        get => searchFromDate;
        set => SetProperty(ref searchFromDate, value);
    }

    public string SearchToDate
    {
        get => searchToDate;
        set => SetProperty(ref searchToDate, value);
    }

    public string SelectedSearchDocumentType
    {
        get => selectedSearchDocumentType;
        set => SetProperty(ref selectedSearchDocumentType, value);
    }

    public string SelectedSearchStatus
    {
        get => selectedSearchStatus;
        set => SetProperty(ref selectedSearchStatus, value);
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

    public ReviewSuggestionItemViewModel? SelectedReviewSuggestion
    {
        get => selectedReviewSuggestion;
        set => SetProperty(ref selectedReviewSuggestion, value);
    }

    public DuplicateGroupItemViewModel? SelectedDuplicateGroup
    {
        get => selectedDuplicateGroup;
        set
        {
            if (!SetProperty(ref selectedDuplicateGroup, value))
            {
                return;
            }

            RefreshSelectedDuplicateDocuments();
        }
    }

    public DuplicateDocumentItemViewModel? SelectedDuplicateDocument
    {
        get => selectedDuplicateDocument;
        set => SetProperty(ref selectedDuplicateDocument, value);
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
            IncludedExtensions = [".pdf", ".jpg", ".jpeg", ".png", ".tif", ".tiff", ".txt"]
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

    public async Task SearchAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            SearchResults.Clear();
            StatusMessage = "Enter a keyword to search indexed OCR text.";
            return;
        }

        var fromDateUtc = TryParseDateUtc(SearchFromDate, toEndOfDay: false);
        var toDateUtc = TryParseDateUtc(SearchToDate, toEndOfDay: true);
        var request = new DocumentSearchRequest(
            SearchQuery,
            ParseDocumentTypeFilter(SelectedSearchDocumentType),
            ParseStatusFilter(SelectedSearchStatus),
            string.IsNullOrWhiteSpace(SearchSourceFolder) ? null : SearchSourceFolder.Trim(),
            fromDateUtc,
            toDateUtc);

        var results = await documentIndexService.SearchAsync(request, cancellationToken);
        SearchResults.Clear();
        foreach (var result in results)
        {
            SearchResults.Add(new SearchResultItemViewModel(
                result.Id,
                result.OriginalPath,
                result.DocumentType.ToString(),
                result.OcrConfidence.ToString(),
                result.LastProcessedUtc ?? result.DiscoveredUtc,
                BuildPreview(result.ExtractedText)));
        }

        StatusMessage = $"Search returned {SearchResults.Count} result(s).";
    }

    public async Task RefreshDuplicatesAsync(CancellationToken cancellationToken)
    {
        var groups = await duplicateDetectionService.FindDuplicatesAsync(cancellationToken);
        var records = await documentRepository.ListAllAsync(cancellationToken);
        var documentMap = records.ToDictionary(record => record.Id);

        DuplicateGroups.Clear();
        DuplicateDocuments.Clear();
        duplicateGroupDocuments.Clear();

        foreach (var group in groups.OrderByDescending(item => item.IsExactMatch).ThenBy(item => item.DocumentIds.Count))
        {
            var items = group.DocumentIds
                .Where(documentMap.ContainsKey)
                .Select(id =>
                {
                    var record = documentMap[id];
                    var path = record.CurrentPath ?? record.OriginalPath;
                    return new DuplicateDocumentItemViewModel(
                        record.Id,
                        Path.GetFileName(path),
                        path,
                        record.DocumentType.ToString(),
                        record.Sha256Hash ?? string.Empty,
                        record.Status.ToString());
                })
                .ToList();

            if (items.Count < 2)
            {
                continue;
            }

            var canonicalId = group.CanonicalDocumentId ?? items[0].DocumentId;
            var viewModel = new DuplicateGroupItemViewModel(
                group.Id,
                group.IsExactMatch,
                group.MatchReason,
                items.Count,
                canonicalId);
            DuplicateGroups.Add(viewModel);
            duplicateGroupDocuments[group.Id] = items;
        }

        SelectedDuplicateGroup = DuplicateGroups.FirstOrDefault();
        StatusMessage = $"Duplicate scan found {DuplicateGroups.Count} group(s).";
    }

    public Task MarkSelectedDuplicateAsCanonicalAsync(CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (SelectedDuplicateGroup is null || SelectedDuplicateDocument is null)
        {
            StatusMessage = "Select a duplicate group and a canonical file.";
            return Task.CompletedTask;
        }

        SelectedDuplicateGroup.CanonicalDocumentId = SelectedDuplicateDocument.DocumentId;
        StatusMessage = "Canonical file selected.";
        return Task.CompletedTask;
    }

    public async Task MoveNonCanonicalDuplicatesAsync(CancellationToken cancellationToken)
    {
        if (SelectedDuplicateGroup is null)
        {
            StatusMessage = "Select a duplicate group first.";
            return;
        }

        if (!duplicateGroupDocuments.TryGetValue(SelectedDuplicateGroup.GroupId, out var items) || items.Count < 2)
        {
            StatusMessage = "Selected duplicate group is empty.";
            return;
        }

        var canonicalId = SelectedDuplicateGroup.CanonicalDocumentId;
        var movedCount = 0;
        var reviewFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "PaperDesk",
            "Review Duplicates",
            SelectedDuplicateGroup.GroupId.ToString("N"));
        Directory.CreateDirectory(reviewFolder);

        foreach (var duplicate in items.Where(item => item.DocumentId != canonicalId))
        {
            var sourcePath = Path.GetFullPath(duplicate.Path);
            if (!File.Exists(sourcePath))
            {
                await activityLog.WriteAsync(new ActivityEvent(
                    ActivityType.Failure,
                    $"Duplicate source file not found: {sourcePath}",
                    sourcePath,
                    duplicate.DocumentId), cancellationToken);
                continue;
            }

            var destinationPath = EnsureUniquePath(reviewFolder, Path.GetFileName(sourcePath), sourcePath);
            File.Move(sourcePath, destinationPath);

            var record = await documentRepository.GetByIdAsync(duplicate.DocumentId, cancellationToken);
            if (record is not null)
            {
                record.CurrentPath = destinationPath;
                record.Status = ProcessingStatus.NeedsReview;
                record.LastProcessedUtc = DateTimeOffset.UtcNow;
                await documentRepository.UpdateAsync(record, cancellationToken);
            }

            await activityLog.WriteAsync(new ActivityEvent(
                ActivityType.DuplicateDetected,
                $"Moved duplicate to review folder: {destinationPath}",
                destinationPath,
                duplicate.DocumentId), cancellationToken);
            movedCount++;
        }

        await RefreshDuplicatesAsync(cancellationToken);
        StatusMessage = movedCount == 0
            ? "No duplicate files were moved."
            : $"Moved {movedCount} duplicate file(s) to review folder.";
    }

    public async Task RefreshReviewQueueAsync(CancellationToken cancellationToken)
    {
        var suggestions = await renameSuggestionRepository.ListForReviewQueueAsync(cancellationToken);
        ReviewSuggestions.Clear();
        foreach (var suggestion in suggestions)
        {
            ReviewSuggestions.Add(new ReviewSuggestionItemViewModel(
                suggestion.Id,
                suggestion.DocumentId,
                suggestion.ProposedFileName,
                suggestion.ProposedDestinationDirectory ?? string.Empty,
                suggestion.Confidence,
                suggestion.Reason));
        }
    }

    public async Task<bool> ApplySelectedSuggestionAsync(CancellationToken cancellationToken)
    {
        if (SelectedReviewSuggestion is null)
        {
            StatusMessage = "Select a suggestion to apply.";
            return false;
        }

        var suggestion = SelectedReviewSuggestion;
        if (string.IsNullOrWhiteSpace(suggestion.ProposedFileName))
        {
            StatusMessage = "Proposed filename cannot be empty.";
            return false;
        }

        var document = await documentRepository.GetByIdAsync(suggestion.DocumentId, cancellationToken);
        if (document is null)
        {
            StatusMessage = "Document was not found.";
            return false;
        }

        var sourcePath = Path.GetFullPath(document.CurrentPath ?? document.OriginalPath);
        if (!File.Exists(sourcePath))
        {
            StatusMessage = $"Source file missing: {sourcePath}";
            return false;
        }

        var destinationDirectory = string.IsNullOrWhiteSpace(suggestion.ProposedDestinationDirectory)
            ? Path.GetDirectoryName(sourcePath) ?? Environment.CurrentDirectory
            : Path.GetFullPath(suggestion.ProposedDestinationDirectory);
        Directory.CreateDirectory(destinationDirectory);

        var safeName = SanitizeFileName(suggestion.ProposedFileName);
        var extension = Path.GetExtension(sourcePath);
        if (string.IsNullOrWhiteSpace(Path.GetExtension(safeName)))
        {
            safeName = $"{safeName}{extension}";
        }

        var destinationPath = EnsureUniquePath(destinationDirectory, safeName, sourcePath);
        if (!string.Equals(sourcePath, destinationPath, StringComparison.OrdinalIgnoreCase))
        {
            File.Move(sourcePath, destinationPath);
        }

        document.CurrentPath = destinationPath;
        document.Status = ProcessingStatus.Completed;
        document.LastProcessedUtc = DateTimeOffset.UtcNow;
        await documentRepository.UpdateAsync(document, cancellationToken);

        var persisted = new RenameSuggestion
        {
            Id = suggestion.Id,
            DocumentId = suggestion.DocumentId,
            ProposedFileName = Path.GetFileName(destinationPath),
            ProposedDestinationDirectory = Path.GetDirectoryName(destinationPath),
            Confidence = suggestion.Confidence,
            Reason = suggestion.Reason,
            IsApproved = true,
            IsSkipped = false
        };
        await renameSuggestionRepository.UpdateAsync(persisted, cancellationToken);

        await activityLog.WriteAsync(new ActivityEvent(
            ActivityType.ActionApproved,
            $"Approved rename suggestion for {Path.GetFileName(sourcePath)}",
            sourcePath,
            suggestion.DocumentId), cancellationToken);
        await activityLog.WriteAsync(new ActivityEvent(
            ActivityType.ActionApplied,
            $"Applied move/rename to {destinationPath}",
            destinationPath,
            suggestion.DocumentId), cancellationToken);

        ReviewSuggestions.Remove(suggestion);
        StatusMessage = "Suggestion applied.";
        return true;
    }

    public async Task SkipSelectedSuggestionAsync(CancellationToken cancellationToken)
    {
        if (SelectedReviewSuggestion is null)
        {
            StatusMessage = "Select a suggestion to skip.";
            return;
        }

        var suggestion = SelectedReviewSuggestion;
        var persisted = new RenameSuggestion
        {
            Id = suggestion.Id,
            DocumentId = suggestion.DocumentId,
            ProposedFileName = suggestion.ProposedFileName,
            ProposedDestinationDirectory = suggestion.ProposedDestinationDirectory,
            Confidence = suggestion.Confidence,
            Reason = suggestion.Reason,
            IsApproved = false,
            IsSkipped = true
        };

        await renameSuggestionRepository.UpdateAsync(persisted, cancellationToken);

        var document = await documentRepository.GetByIdAsync(suggestion.DocumentId, cancellationToken);
        if (document is not null)
        {
            document.Status = ProcessingStatus.Skipped;
            document.LastProcessedUtc = DateTimeOffset.UtcNow;
            await documentRepository.UpdateAsync(document, cancellationToken);
        }

        await activityLog.WriteAsync(new ActivityEvent(
            ActivityType.FileSkipped,
            $"Skipped suggestion for document {suggestion.DocumentId}",
            null,
            suggestion.DocumentId), cancellationToken);

        ReviewSuggestions.Remove(suggestion);
        StatusMessage = "Suggestion skipped.";
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
            for (var index = 0; index < 3; index++)
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

            await RefreshReviewQueueAsync(CancellationToken.None);
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

    private static string BuildPreview(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var singleLine = string.Join(' ', text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return singleLine.Length <= 160 ? singleLine : $"{singleLine[..157]}...";
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(character => invalidChars.Contains(character) ? '-' : character).ToArray());
        return string.Join(' ', sanitized.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Trim();
    }

    private static string EnsureUniquePath(string directory, string fileName, string sourcePath)
    {
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        var counter = 0;
        while (true)
        {
            var candidate = counter == 0
                ? Path.Combine(directory, fileName)
                : Path.Combine(directory, $"{baseName} ({counter + 1}){extension}");

            if (!File.Exists(candidate) || string.Equals(Path.GetFullPath(candidate), Path.GetFullPath(sourcePath), StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }

            counter++;
        }
    }

    private void RefreshSelectedDuplicateDocuments()
    {
        DuplicateDocuments.Clear();
        SelectedDuplicateDocument = null;

        if (SelectedDuplicateGroup is null)
        {
            return;
        }

        if (!duplicateGroupDocuments.TryGetValue(SelectedDuplicateGroup.GroupId, out var items))
        {
            return;
        }

        foreach (var item in items)
        {
            DuplicateDocuments.Add(item);
        }

        SelectedDuplicateDocument = DuplicateDocuments
            .FirstOrDefault(item => item.DocumentId == SelectedDuplicateGroup.CanonicalDocumentId)
            ?? DuplicateDocuments.FirstOrDefault();
    }

    private static DateTimeOffset? TryParseDateUtc(string value, bool toEndOfDay)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!DateTime.TryParse(value, out var date))
        {
            return null;
        }

        var local = toEndOfDay
            ? date.Date.AddDays(1).AddTicks(-1)
            : date.Date;
        return new DateTimeOffset(local.ToUniversalTime());
    }

    private static DocumentType? ParseDocumentTypeFilter(string value)
    {
        if (string.Equals(value, "All", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return Enum.TryParse<DocumentType>(value, true, out var documentType)
            ? documentType
            : null;
    }

    private static ProcessingStatus? ParseStatusFilter(string value)
    {
        if (string.Equals(value, "All", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return Enum.TryParse<ProcessingStatus>(value, true, out var status)
            ? status
            : null;
    }
}

public sealed class ReviewSuggestionItemViewModel : INotifyPropertyChanged
{
    private string proposedFileName;
    private string proposedDestinationDirectory;

    public ReviewSuggestionItemViewModel(
        Guid id,
        Guid documentId,
        string proposedFileName,
        string proposedDestinationDirectory,
        ConfidenceLevel confidence,
        string reason)
    {
        Id = id;
        DocumentId = documentId;
        this.proposedFileName = proposedFileName;
        this.proposedDestinationDirectory = proposedDestinationDirectory;
        Confidence = confidence;
        Reason = reason;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid Id { get; }
    public Guid DocumentId { get; }
    public ConfidenceLevel Confidence { get; }
    public string Reason { get; }

    public string ProposedFileName
    {
        get => proposedFileName;
        set
        {
            if (proposedFileName == value)
            {
                return;
            }

            proposedFileName = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProposedFileName)));
        }
    }

    public string ProposedDestinationDirectory
    {
        get => proposedDestinationDirectory;
        set
        {
            if (proposedDestinationDirectory == value)
            {
                return;
            }

            proposedDestinationDirectory = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProposedDestinationDirectory)));
        }
    }
}

public sealed record ActivityEventItemViewModel(
    DateTimeOffset OccurredUtc,
    ActivityType ActivityType,
    string Message,
    string? Path);

public sealed record SearchResultItemViewModel(
    Guid Id,
    string OriginalPath,
    string DocumentType,
    string OcrConfidence,
    DateTimeOffset ProcessedUtc,
    string Preview);

public sealed class DuplicateGroupItemViewModel : INotifyPropertyChanged
{
    private Guid canonicalDocumentId;

    public DuplicateGroupItemViewModel(
        Guid groupId,
        bool isExactMatch,
        string reason,
        int count,
        Guid canonicalDocumentId)
    {
        GroupId = groupId;
        IsExactMatch = isExactMatch;
        Reason = reason;
        Count = count;
        this.canonicalDocumentId = canonicalDocumentId;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid GroupId { get; }
    public bool IsExactMatch { get; }
    public string Reason { get; }
    public int Count { get; }

    public string MatchType => IsExactMatch ? "Exact" : "Potential";

    public Guid CanonicalDocumentId
    {
        get => canonicalDocumentId;
        set
        {
            if (canonicalDocumentId == value)
            {
                return;
            }

            canonicalDocumentId = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanonicalDocumentId)));
        }
    }
}

public sealed record DuplicateDocumentItemViewModel(
    Guid DocumentId,
    string FileName,
    string Path,
    string DocumentType,
    string Sha256Hash,
    string Status);

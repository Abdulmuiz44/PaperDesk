namespace PaperDesk.App.Shell;

public sealed class MainWindowViewModel
{
    public string Title => "PaperDesk";

    // Keep in sync with MainWindow tab headers. Search and Duplicates are placeholders for now.
    public IReadOnlyCollection<string> NavigationItems { get; } =
    [
        "Dashboard",
        "Watched Folders",
        "Review Queue",
        "Search",
        "Duplicates",
        "Activity Log",
        "Settings",
    ];
}

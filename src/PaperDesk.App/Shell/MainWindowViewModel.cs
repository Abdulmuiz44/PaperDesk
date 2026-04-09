namespace PaperDesk.App.Shell;

public sealed class MainWindowViewModel
{
    public string Title => "PaperDesk";

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

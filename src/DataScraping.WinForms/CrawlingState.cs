namespace DataScraping.WinForms;

public class CrawlingState
{
    public CrawlingState()
    {
        CurrentStatus = Status.NotStart;
    }
    public Status CurrentStatus { get; private set; }
    public List<string> VisitedUrls { get; private set; } = new List<string>();
    public List<string> Emails { get; private set; } = new List<string>();
    public List<string> DownloadLinks { get; private set; } = new List<string>();


    public void Reset()
    {
        CurrentStatus = Status.NotStart;
        VisitedUrls.Clear();
        Emails.Clear();
        DownloadLinks.Clear();
    }

    public void UpdateStatus(Status newStatus)
    {
        CurrentStatus = newStatus;
    }

    public enum Status
    {
        NotStart,
        InProgress,
        Completed,
        Error
    }
}

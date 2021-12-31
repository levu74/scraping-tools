using Abot2.Crawler;
using Abot2.Poco;
using DataScraping.WinForms.Properties;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text.RegularExpressions;

namespace DataScraping.WinForms
{
    public partial class Form1 : Form
    {
        private const string StartProcessText = "Start";
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        Regex emailRegex = new Regex(Settings.Default.EmailPattern, RegexOptions.Compiled);
        Regex downloadLink = new Regex(Settings.Default.DownloadLink, RegexOptions.Compiled);

        private readonly CrawlingState crawlingState = new CrawlingState();
        public Form1()
        {
            InitializeComponent();
        }

        private void Process_Click(object sender, EventArgs e)
        {

            if (this.btnProcess.Text == StartProcessText)
            {
                crawlingState.Reset();
                cancellationTokenSource = new CancellationTokenSource();
                btnProcess.Text = "Stop";
                txtEmail.Cursor = Cursors.WaitCursor;
                txtLink.Cursor = Cursors.WaitCursor;
                var websiteUrl = txtWebUrl.Text;
                txtEmail.Clear();
                StartCrawler(websiteUrl, cancellationTokenSource).ObserveOn(SynchronizationContext.Current!).Subscribe(result =>
                {
                    UpdateTextbox(crawlingState.Emails, txtEmail, true);
                    List<string> links = crawlingState.DownloadLinks.Prepend("Download Links").Append("\r\nVisit Links").Concat(crawlingState.VisitedUrls).ToList();
                    UpdateTextbox(links, txtLink, true);
                    txtEmail.Cursor = Cursors.Default;
                    txtLink.Cursor = Cursors.Default;
                    btnProcess.Text = StartProcessText;
                    MessageBox.Show(this, "Done");
                });
            }
            else
            {
                cancellationTokenSource.Cancel();
                btnProcess.Text = StartProcessText;
                UpdateTextbox(crawlingState.Emails, txtEmail, true);
                UpdateTextbox(crawlingState.DownloadLinks, txtLink, true);
                txtEmail.Cursor = Cursors.Default;
                txtLink.Cursor = Cursors.Default;
            }


        }

        private List<string> ExtractEmail(string html, string url = "")
        {
            var list = new List<string>();
            foreach (Match match in emailRegex.Matches(html))
            {
                list.Add($"{match.Value}|{url}");
            }

            return list;
        }

        private List<string> ExtractDownloadLink(string html, string url = "")
        {
            var list = new List<string>();
            foreach (Group match in downloadLink.Matches(html).OfType<Match>().Select(m => m.Groups["link"]))
            {
                list.Add($"{match.Value}|{url}");
            }

            return list;
        }

        public IObservable<CrawlResult> StartCrawler(string url, CancellationTokenSource source)
        {
            var config = new CrawlConfiguration
            {
                MaxPagesToCrawl = Settings.Default.Pages, //Only crawl 10 pages
                MinCrawlDelayPerDomainMilliSeconds = Settings.Default.Delay, //Wait this many millisecs between requests
                MaxLinksPerPage = Settings.Default.MaxLinksPerPage,
                CrawlTimeoutSeconds = Settings.Default.TimeoutSeconds,
                IsExternalPageLinksCrawlingEnabled = Settings.Default.ExternalCrawlingEnabled,
                IsExternalPageCrawlingEnabled = Settings.Default.ExternalCrawlingEnabled,


            };
            var crawler = new PoliteWebCrawler(config);
            crawler.ShouldCrawlPageDecisionMaker = ShouldCrawlPageDecisionMaker;
            crawler.PageCrawlCompleted += PageCrawlCompleted;//Several events available...

            return crawler.CrawlAsync(new Uri(url), source).ToObservable();
        }

        private CrawlDecision ShouldCrawlPageDecisionMaker(PageToCrawl page, CrawlContext context)
        {
            CrawlDecision decision = new CrawlDecision();
            var notAllowWords = Settings.Default.NotContainsUrlWords.Split(';');
            var currentUrl = page.Uri.AbsoluteUri.ToLower();
            if (notAllowWords.Any(word => currentUrl.IndexOf(word) >= 0))
            {
                decision.Allow = false;
            }
            else
            {
                decision.Allow = true;
            }

            return decision;

        }

        private void PageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
        {
            string visitUrl = e.CrawledPage.Uri.ToString();
            var emailList = ExtractEmail(e.CrawledPage.Content.Text, visitUrl);
            var downloadLinkList = ExtractDownloadLink(e.CrawledPage.Content.Text, visitUrl).Distinct().Select(x =>
            {
                if (x.StartsWith("/"))
                {
                    return x + "\t" + e.CrawledPage.Uri;
                }
                return x;
            }).ToList();
            crawlingState.Emails.AddRange(emailList);
            crawlingState.DownloadLinks.AddRange(downloadLinkList);
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() =>
                {

                    crawlingState.VisitedUrls.Add(visitUrl);
                    toolStripStatusLabel1.Text = visitUrl;
                    var textbox = txtEmail;
                    UpdateTextbox(emailList, txtEmail);
                    UpdateTextbox(downloadLinkList, txtLink);
                }));
            }
        }

        private static void UpdateTextbox(List<string> list, TextBox textbox, bool clearIfEmpty = false)
        {
            if (clearIfEmpty)
            {
                textbox.Clear();
            }

            if (list.Distinct().Any())
            {
                textbox.Text += Environment.NewLine;
                textbox.Text += list.Distinct().Aggregate((m, n) => m + Environment.NewLine + n);
            }
            else
            {
                if (clearIfEmpty)
                    textbox.Clear();
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            UserSettingsDialog dialog = new UserSettingsDialog();
            dialog.StartPosition = FormStartPosition.CenterParent;
            dialog.ShowDialog(this);

        }
    }
}
using Abot2.Crawler;
using Abot2.Poco;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reactive.Threading;
using System.Reactive.Linq;
using System.Threading;
using System.Configuration;
using EmailScraping.Properties;

namespace EmailScraping
{
    public partial class Form1 : Form
    {
        private const string StartProcessText = "Start";
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        Regex emailRegex = new Regex(Settings.Default.EmailPattern, RegexOptions.Compiled);
        Regex downloadLink = new Regex(Settings.Default.DownloadLink, RegexOptions.Compiled);
        private List<string> emails = new List<string>();
        private List<string> links = new List<string>();
        public Form1()
        {
            InitializeComponent();
        }

        private void Process_Click(object sender, EventArgs e)
        {
            
            if (this.btnProcess.Text == StartProcessText)
            {
                cancellationTokenSource = new CancellationTokenSource();
                btnProcess.Text = "Stop";
                txtEmail.Cursor = Cursors.WaitCursor;
                txtLink.Cursor = Cursors.WaitCursor;
                var websiteUrl = txtWebUrl.Text;
                txtEmail.Clear();
                emails = new List<string>();
                links = new List<string>();
                StartCrawler(websiteUrl, cancellationTokenSource).ObserveOn(SynchronizationContext.Current).Subscribe(result =>
                {
                    UpdateTextbox(emails, txtEmail, true);
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
                UpdateTextbox(emails, txtEmail, true);
                UpdateTextbox(links, txtLink, true);
                txtEmail.Cursor = Cursors.Default;
                txtLink.Cursor = Cursors.Default;
            }
            
            
        }

        private List<string> ExtractEmail(string html)
        {
            var list = new List<string>();
            foreach (Match match in emailRegex.Matches(html))
            {
                list.Add(match.Value);
            }

            return list;
        }

        private List<string> ExtractDownloadLink(string html)
        {
            var list = new List<string>();
            foreach (Group match in downloadLink.Matches(html).OfType<Match>().Select(m => m.Groups["link"]))
            {
                list.Add(match.Value);
            }

            return list;
        }

        public IObservable<CrawlResult> StartCrawler(string url, CancellationTokenSource source)
        {
            var config = new CrawlConfiguration
            {
                MaxPagesToCrawl = Settings.Default.Pages, //Only crawl 10 pages
                MinCrawlDelayPerDomainMilliSeconds = Settings.Default.Delay //Wait this many millisecs between requests
            };
            var crawler = new PoliteWebCrawler(config);
            
            crawler.PageCrawlCompleted += PageCrawlCompleted;//Several events available...

             return crawler.CrawlAsync(new Uri(url), source).ToObservable();
        }

        private void PageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
        {
            var emailList = ExtractEmail(e.CrawledPage.Content.Text);
            var downloadLinkList = ExtractDownloadLink(e.CrawledPage.Content.Text).Distinct().Select(x =>
            {
                if (x.StartsWith("/"))
                {
                    return x + "\t" + e.CrawledPage.Uri;
                }
                return x;
            }).ToList();
            emails.AddRange(emailList);
            links.AddRange(downloadLinkList);
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() =>
                {
                    toolStripStatusLabel1.Text = e.CrawledPage.Uri.ToString();
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

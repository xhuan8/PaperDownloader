using mshtml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows.Controls;

namespace PaperDownloader
{
    public class ViewModelMainWindow : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string url = "http://openaccess.thecvf.com/CVPR2019.py";
        private string filter = ".pdf";
        private string exclude = "-supp.pdf";
        private string folder = "E:\\学习资料\\cvpr2019";
        private int count = 0;

        private bool isDownloading;
        private bool downloadWorking;
        private WebBrowser browser;
        List<string> texts;

        private ManualResetEvent waitForDownload = new ManualResetEvent(false);
        private ManualResetEvent waitForDownload2 = new ManualResetEvent(false);

        public string Url
        {
            get { return url; }
            set
            {
                url = value;
                NotifyPropertyChanged();
            }
        }

        public string Filter
        {
            get { return filter; }
            set
            {
                filter = value;
                NotifyPropertyChanged();
            }
        }

        public string Exclude
        {
            get { return exclude; }
            set
            {
                exclude = value;
                NotifyPropertyChanged();
            }
        }

        public string Folder
        {
            get { return folder; }
            set
            {
                folder = value;
                NotifyPropertyChanged();
            }
        }

        public int Count
        {
            get { return count; }
            set
            {
                count = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsDownloading
        {
            get { return isDownloading; }
            set
            {
                isDownloading = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged("IsNotDownloading");
            }
        }

        public bool IsNotDownloading
        {
            get { return !isDownloading; }
        }

        private void NotifyPropertyChanged([CallerMemberName]string propertyName = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        internal void Download(WebBrowser browser)
        {
            IsDownloading = true;
            downloadWorking = true;
            this.browser = browser;

            browser.LoadCompleted += Browser_LoadCompleted;
            browser.Navigate(url);
        }

        private void Browser_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            try
            {
                HTMLDocumentClass document = (HTMLDocumentClass)browser.Document;
                IHTMLDocument2 doc2 = document;
                IHTMLElementCollection links = doc2.links;
                texts = new List<string>();
                foreach (IHTMLElement element in links)
                {
                    IHTMLAnchorElement anchorElement = element as IHTMLAnchorElement;
                    if (anchorElement.href.EndsWith(filter) && 
                        (string.IsNullOrEmpty(exclude) || !anchorElement.href.Contains(exclude)))
                        texts.Add(anchorElement.href);
                }

                Count = texts.Count;
                Thread thread = new Thread(StartThread);
                thread.IsBackground = true;
                thread.Start();
            }
            catch (Exception)
            {

            }
            finally
            {
            }
        }

        private void StartThread()
        {
            try
            {
                waitForDownload.Reset();
                waitForDownload2.Reset();
                Thread thread = new Thread(WorkerThread);
                thread.Start(waitForDownload);
                thread = new Thread(WorkerThread);
                thread.Start(waitForDownload2);
                waitForDownload.WaitOne();
                waitForDownload2.WaitOne();
            }
            finally
            {
                IsDownloading = false;
            }
        }

        private void WorkerThread(object waitHandler)
        {
            WebClient client = new WebClient();

            while (true)
            {
                try
                {
                    if (!downloadWorking)
                        break;

                    string url = string.Empty;
                    lock (texts)
                    {
                        if (texts.Count == 0)
                            break;

                        url = texts[0];
                        texts.RemoveAt(0);
                        Count = texts.Count;
                    }

                    string filename = Path.Combine(folder, GetFileName(url));
                    client.DownloadFile(url, filename);
                }
                catch (Exception ex)
                {

                }
            }

            (waitHandler as ManualResetEvent).Set();
        }

        private string GetFileName(string url)
        {
            return Path.GetFileName(url);
        }

        internal void Cancel()
        {
            downloadWorking = false;
            waitForDownload.Set();
        }
    }
}

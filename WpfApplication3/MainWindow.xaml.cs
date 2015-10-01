using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace WpfApplication3
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        ObservableCollection<Match> matches = new ObservableCollection<Match>();

        string GET_IPLAYER_FOLDER = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "get_iplayer");
        string OUTPUT_FOLDER = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "iPlayer Recordings");

        Brush original_background;
        Brush original_foreground;

        public MainWindow()
        {
            InitializeComponent();
            txtSearch.Focus();
            original_background = txtCmdLine.Background;
            original_foreground = txtCmdLine.Foreground;
            dataGrid1.DataContext = matches;

            // load notes
            if (File.Exists(notespath))
            {
                var xml = XDocument.Load(notespath);

                foreach (var el in xml.Root.Element("Notes").Elements())
                {
                    AllNotes.Add(el.Name.LocalName, el.Value);
                }
            }
        }


        private void btnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(OUTPUT_FOLDER);
           // UpdateHistory();

        }
      


        private void Log(string txt)
        {
            Dispatcher.Invoke((Action)delegate
            {
                textBox1.AppendText(txt);
                textBox1.ScrollToEnd();
            });
        }






        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {

            StringBuilder args = new StringBuilder();
            if (rdoTV.IsChecked == true)
                args.Append("--type=tv");
            else if (rdoRadio.IsChecked == true)
                args.Append("--type=radio");

            args.Append(" --listformat=\"<index>|<name>|<episode>|<web>|<pid>\"");
            args.Append(" " + txtSearch.Text);

            get_iplayer(args.ToString());
        }

        private void btnInfo_Click(object sender, RoutedEventArgs e)
        {
            var match = (sender as Button).DataContext as Match;

            System.Diagnostics.Process.Start(match.Web);
        }


        private void btnGet_Click(object sender, RoutedEventArgs e)
        {
            var match = (sender as Button).DataContext as Match;

            if (match.Downloaded)
            {
                // play
                System.Diagnostics.Process.Start(match.DownloadedFilename);
            }
            else
            {
                // get

                StringBuilder args = new StringBuilder();
                args.Append("--get " + match.Index);

                args.Append(" --file-prefix=\"<pid> <name> - <episode>\"");

                if (chkBestQuality.IsChecked == true)
                    args.Append(" --mode=best");

                get_iplayer(args.ToString());
            }
        }

        private void get_iplayer(string args)
        {
            textBox1.Clear();

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = Path.Combine(GET_IPLAYER_FOLDER, "perl.exe");
            psi.WorkingDirectory = GET_IPLAYER_FOLDER;
            psi.Arguments = string.Format(@"get_iplayer.pl {0}", args);
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardInput = true;
            psi.RedirectStandardError = true;
            psi.CreateNoWindow = true;

            if (!File.Exists(psi.FileName))
            {
                Log("Error: File not found '" + psi.FileName + "'");
                return;
            }

            Process p = new Process() { StartInfo = psi };
            p.EnableRaisingEvents = true;
            p.Exited += p_Exited;
            p.OutputDataReceived += p_OutputDataReceived;
            p.ErrorDataReceived += p_OutputDataReceived;

            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            
            txtCmdLine.Text = ">get_iplayer " + args;
            EnableDisableControls(false);
            foreach (var match in matches.Where(z=>!z.Downloaded))
                match.EnableGet = false;
        }

        void EnableDisableControls(bool bEnable)
        {
            if (!bEnable)
            {
                txtCmdLine.Background = new SolidColorBrush(Color.FromArgb(255, 245, 73, 151));
                txtCmdLine.Foreground = new SolidColorBrush(Colors.White);
            }
            else
            {
                txtCmdLine.Background = original_background;
                txtCmdLine.Foreground = original_foreground;
            }

            btnSearch.IsEnabled = bEnable;
            txtSearch.IsEnabled = bEnable;
            rdoTV.IsEnabled = bEnable;
            rdoRadio.IsEnabled = bEnable;
            chkBestQuality.IsEnabled = bEnable;
            btnShowDownloads.IsEnabled = bEnable;
        }

        void p_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
            {
                Log(Environment.NewLine);
                return;
            }

            Log(e.Data + Environment.NewLine);
        }

        static Dictionary<string, string> AllNotes = new Dictionary<string,string>();

        public class Match : INotifyPropertyChanged
        {

            public string Name { get; set; }
            public bool Downloaded { get; set; }
            public string Web { get; set; }
            public string DownloadedFilename { get; set; }
            public string pid { get; set; }
            public string Index { get; set; }
            public string ButtonText { get { return Downloaded ? "Play" : "Get"; } }
            public SolidColorBrush ButtonColor { get { return Downloaded ? new SolidColorBrush(Colors.LightGreen) : new SolidColorBrush(Colors.HotPink); } }

            public string Notes { get { return AllNotes.ContainsKey(this.pid) ? AllNotes[this.pid] : "";  } set { AllNotes[this.pid] = value; } }

            // all this (below) just to get the UI to change
            // http://msdn.microsoft.com/en-us/library/ms743695.aspx
            private bool _enableget;
            public bool EnableGet { get { return _enableget; } set { _enableget = value; OnPropertyChanged("EnableGet"); } }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string name)
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null)
                    handler(this, new PropertyChangedEventArgs(name));
            }
        }
                
        public class DownloadedFile
        {
            public string pid { get; set; }
            public string Filename { get; set; }
        }

        void p_Exited(object sender, EventArgs e)
        {


            // get pids of files in output folder
            var downloaded_pids = new List<DownloadedFile>();
            foreach (var file in new DirectoryInfo(OUTPUT_FOLDER).GetFiles())
            {
                if (file.Name.Contains('_'))
                {
                    downloaded_pids.Add(new DownloadedFile()
                    {
                        pid = file.Name.Substring(0, file.Name.IndexOf('_')),
                        Filename = file.FullName
                    });
                }
            }

            Dispatcher.Invoke((Action)delegate
            {
                // remove blank lines from end of textBox1.Text
                textBox1.Text = textBox1.Text.TrimEnd(Environment.NewLine.ToCharArray());



                if (txtCmdLine.Text.Contains("--type="))
                {
                    // parse search results
                    matches.Clear();
                    foreach (var line in textBox1.Text.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (line.Length > 0 && char.IsDigit(line[0]) && line.Contains(':'))
                        {
                            string[] parts = line.Split('|');
                            var downloaded = downloaded_pids.FirstOrDefault(z => z.pid == parts[4]);
                            // e.g.
                            // -[0]-|-- [1] --|--------------- [2] ----------------|--------------------- [3] ------------------ |-- [4] --
                            // 13348|Zane Lowe|Zane Lowe. Kanye West. The Interview|http://www.bbc.co.uk/programmes/b03cnqpl.html|b03cnqpl
                            matches.Add(new Match()
                            {
                                Index = parts[0],
                                Name = parts[1] + " - " + parts[2],
                                Web = parts[3],
                                pid = parts[4],
                                Downloaded = downloaded != null,
                                DownloadedFilename = downloaded == null ? null : downloaded.Filename,
                                EnableGet = true
                            });
                        }
                    }
                }
                else if (txtCmdLine.Text.Contains("--get"))
                {
                    // refresh the list after downloading
                    btnSearch_Click(null, null);
                }

                
               
                EnableDisableControls(true);
            });
        }

        string notespath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "gih_notes.xml");

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // save notes
            var notes = new XElement("Notes");
            foreach (var note in AllNotes)
                notes.Add(new XElement(note.Key, note.Value));
            
            var root = new XElement("get-iplayer-helper");
            root.Add(notes);

            XDocument xml = new XDocument();
            xml.Add(root);
            xml.Save(notespath);
        }

       

        private void btnShowDownloads_Click(object sender, RoutedEventArgs e)
        {
            matches.Clear();
            foreach (FileInfo fi in new DirectoryInfo(OUTPUT_FOLDER).GetFiles().OrderByDescending(z=>z.CreationTime))
            {
                if (!fi.Name.Contains('_')) continue;
                int idx = fi.Name.IndexOf('_');
                string pid = fi.Name.Substring(0, idx);

                matches.Add(new Match()
                {
                    Index = "", // TODO?
                    Name = fi.Name.Substring(idx + 1).Replace('_', ' '),
                    Web = string.Format("http://www.bbc.co.uk/programmes/{0}.html", pid), // this url may change in future!!!
                    pid = pid,
                    Downloaded = true,
                    DownloadedFilename = fi.FullName,
                    EnableGet = true
                });
            }
        }


        //public class HistoryEntry
        //{
        //    public string pid { get; set; }
        //    public string name { get; set; }
        //    public string episode { get; set; }
        //    public string type { get; set; }
        //    public string timeadded { get; set; }
        //    public string mode { get; set; }
        //    public string filename { get; set; }
        //    public string versions { get; set; }
        //    public string duration { get; set; }
        //    public string desc { get; set; }
        //    public string channel { get; set; }
        //    public string categories { get; set; }
        //    public string thumbnail { get; set; }
        //    public string guidance { get; set; }
        //    public string web { get; set; }
        //    public string episodenum { get; set; }
        //    public string seriesnum { get; set; }
        //}
        //List<HistoryEntry> history;
        //public void UpdateHistory()
        //{
        //    history = new List<HistoryEntry>();
        //    string historyfile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".get_iplayer", "download_history");
        //    using (StreamReader sr = new StreamReader(historyfile))
        //    {
        //        while (!sr.EndOfStream)
        //        {
        //            string[] parts = sr.ReadLine().Split('|');
        //            history.Add(new HistoryEntry()
        //            {
        //                pid = parts[0],
        //                name = parts[1],
        //                episode = parts[2],
        //                type = parts[3],
        //                timeadded = parts[4],
        //                mode = parts[5],
        //                filename = parts[6],
        //                versions = parts[7],
        //                duration = parts[8],
        //                desc = parts[9],
        //                channel = parts[10],
        //                thumbnail = parts[11],
        //                guidance = parts[12],
        //                web = parts[13],
        //                episodenum = parts[14],
        //                seriesnum = parts[15]
        //            });
        //        }
        //    }
        //}



    }
}
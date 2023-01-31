using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;

namespace LogViewer2
{
    /// <summary>
    /// 
    /// </summary>
    public partial class ControlLog : System.Windows.Controls.UserControl
    {
        #region Delegates
        public delegate void SearchCompleteEvent(ControlLog cl, string fileName, TimeSpan duration, long matches, int numSearchTerms, bool cancelled);
        public delegate void CompleteEvent(ControlLog cl, string fileName, TimeSpan duration, bool cancelled);
        public delegate void BoolEvent(string fileName, bool val);
        public delegate void MessageEvent(string fileName, string message);
        public delegate void ProgressUpdateEvent(int percent);
        public delegate void ExportInitiatedEvent(ControlLog cl, bool all);
        public delegate void MultiSearchInitiatedEvent(ControlLog cl, List<SearchCriteria> searches);
        #endregion

        #region Events
        public event SearchCompleteEvent SearchComplete;
        public event CompleteEvent LoadComplete;
        public event CompleteEvent ExportComplete;
        public event ProgressUpdateEvent ProgressUpdate;
        public event MessageEvent LoadError;
        public event ExportInitiatedEvent ExportInitiated;
        public event MultiSearchInitiatedEvent MultiSearchInitiated;
        #endregion

        #region Member Variables
        //private Color highlightColour { get; set; }  = Color.Lime;
        // private Color contextColour { get; set; }  = Color.LightGray;
        private Configuration config;
        public Searches Searches { get; set; }
        public Global.ViewMode ViewMode { get; set; } = Global.ViewMode.Standard;
        public List<LogLine> Lines { get; private set; } = new List<LogLine>();
        public LogLine LongestLine { get; private set; } = new LogLine();
        public int LineCount { get; private set; } = 0;
        private FileStream fileStream;
        private Mutex readMutex = new Mutex();
        public string FileName { get; private set; }
        public List<ushort> FilterIds { get; private set; } = new List<ushort>();
        public string Guid { get; private set; }
        #endregion

        #region Constructor
        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        public ControlLog(Configuration config)
        {
            InitializeComponent();

            this.AllowDrop = true;
            this.config = config;
            this.Guid = System.Guid.NewGuid().ToString();
            this.Searches = new Searches();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="ct"></param>
        public void Load(string filePath, CancellationToken ct)
        {
            this.Dispose();
            this.FileName = Path.GetFileName(filePath);

            Task.Run(() => {

                DateTime start = DateTime.Now;
                bool cancelled = false;
                bool error = false;
                try
                {
                    byte[] tempBuffer = new byte[1024 * 1024];

                    this.fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    FileInfo fileInfo = new FileInfo(filePath);

                    // Calcs and finally point the position to the end of the line
                    long position = 0;
                    // Holds the offset to the start of the next line
                    long lineStartOffset = 0;
                    // Checks if we have read less than requested e.g. buffer not filled/end of file
                    bool lastSection = false;
                    // Counter for process reporting
                    int counter = 0;
                    // Holds a counter to start checking for the next indexOf('\r')
                    int startIndex = 0;
                    // Once all of the \r (lines) have been emnumerated, there might still be data left in the
                    // buffer, so this holds the number of bytes that need to be added onto the next line
                    int bufferRemainder = 0;
                    // Holds how many bytes were read from the last file stream read
                    int numBytesRead = 0;
                    // Holds the temporary string generated from the file stream buffer
                    string tempStr = string.Empty;
                    // Length of the current line
                    int charCount;
                    // Return value from IndexOf function
                    int indexOf;

                    while (position < this.fileStream.Length)
                    {
                        numBytesRead = this.fileStream.Read(tempBuffer, 0, 1024 * 1024);
                        if (numBytesRead < 1048576)

                        {
                            lastSection = true;
                        }

                        tempStr = Encoding.ASCII.GetString(tempBuffer).Substring(0, numBytesRead);
                        startIndex = 0;

                        // Does the buffer contain at least one "\n", so now enumerate all instances of "\n"
                        if (tempStr.IndexOf('\n') != -1)
                        {
                            while ((indexOf = tempStr.IndexOf('\n', startIndex)) != -1 && startIndex < numBytesRead)
                            {
                                if (indexOf != -1)
                                {
                                    charCount = 0;

                                    // Check if the line contains a CR as well, if it does then we remove the last char as the char count
                                    if (indexOf != 0 && (int)tempBuffer[Math.Max(0, indexOf - 1)] == 13)
                                    {
                                        charCount = bufferRemainder + (indexOf - startIndex - 1);
                                        position += (long)charCount + 2L;
                                    }
                                    else
                                    {
                                        charCount = bufferRemainder + (indexOf - startIndex);
                                        position += (long)charCount + 1L;
                                    }

                                    AddLine(lineStartOffset, charCount);

                                    // The remaining number in the buffer gets set to 0 e.g. after 
                                    //the first iteration as it would add onto the first line
                                    bufferRemainder = 0;

                                    // Set the offset to the end of the last line that has just been added
                                    lineStartOffset = position;
                                    startIndex = indexOf + 1;
                                }
                            }

                            // We had some '\r' in the last buffer read, now they are processing, so just add the rest as the last line
                            if (lastSection == true)
                            {
                                AddLine(lineStartOffset, bufferRemainder + (numBytesRead - startIndex));
                                return;
                            }

                            bufferRemainder += numBytesRead - startIndex;
                        }
                        else
                        {
                            // The entire content of the buffer doesn't contain \r so just add the rest of content as the last line
                            if (lastSection == true)
                            {
                                AddLine(lineStartOffset, bufferRemainder + (numBytesRead - startIndex));
                                return;
                            }

                            bufferRemainder += numBytesRead;
                        }

                        if (counter++ % 50 == 0)
                        {
                            OnProgressUpdate((int)((double)position / (double)fileInfo.Length * 100));

                            if (ct.IsCancellationRequested)
                            {
                                cancelled = true;
                                return;
                            }
                        }
                    } // WHILE
                }
                catch (IOException ex)
                {
                    OnLoadError(ex.Message);
                    error = true;
                }
                finally
                {
                    if (error == false)
                    {
                        DateTime end = DateTime.Now;

                        OnProgressUpdate(100);
                        OnLoadComplete(end - start, cancelled);
                    }
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            this.Searches = new Searches();
            this.Lines.Clear();
            this.LongestLine = new LogLine();
            this.LineCount = 0;
            this.FileName = String.Empty;
            this.FilterIds = new List<ushort>();
            this.FilterIds.Clear();
            this.ItemsSource = null;

            if (this.fileStream != null)
            {
                this.fileStream.Dispose();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="searchText"></param>
        /// <param name="searchType"></param>
        public void SearchMulti(List<SearchCriteria> scs, CancellationToken ct, int numContextLines)
        {
            Task.Run(() => {

                DateTime start = DateTime.Now;
                bool cancelled = false;
                long matches = 0;
                try
                {
                    long counter = 0;
                    string line = string.Empty;
                    bool located = false;

                    foreach (LogLine ll in this.Lines)
                    {
                        // Reset the match flag
                        ll.SearchMatches.Clear();
                        ClearContextLine(ll.LineNumber, numContextLines);

                        foreach (SearchCriteria sc in scs)
                        {
                            line = this.GetLine(ll.LineNumber);

                            located = false;
                            switch (sc.Type)
                            {
                                case Global.SearchType.SubStringCaseInsensitive:
                                    if (line.IndexOf(sc.Pattern, 0, StringComparison.OrdinalIgnoreCase) > -1)
                                    {
                                        located = true;
                                    }
                                    break;

                                case Global.SearchType.SubStringCaseSensitive:
                                    if (line.IndexOf(sc.Pattern, 0, StringComparison.Ordinal) > -1)
                                    {
                                        located = true;
                                    }
                                    break;

                                case Global.SearchType.RegexCaseInsensitive:
                                    if (Regex.Match(line, sc.Pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled) != Match.Empty)
                                    {
                                        located = true;
                                    }
                                    break;

                                case Global.SearchType.RegexCaseSensitive:
                                    if (Regex.Match(line, sc.Pattern, RegexOptions.Compiled) != Match.Empty)
                                    {
                                        located = true;
                                    }
                                    break;

                                default:
                                    break;
                            }

                            if (located == true)
                            {
                                matches++;
                                ll.SearchMatches.Add(sc.Id);

                                if (numContextLines > 0)
                                {
                                    this.SetContextLines(ll.LineNumber, numContextLines);
                                }
                            }
                        }

                        if (counter++ % 50 == 0)
                        {
                            OnProgressUpdate((int)((double)counter / (double)this.Lines.Count * 100));

                            if (ct.IsCancellationRequested)
                            {
                                cancelled = true;
                                return;
                            }
                        }
                    }
                }
                finally
                {
                    DateTime end = DateTime.Now;

                    OnProgressUpdate(100);
                    OnSearchComplete(end - start, matches, scs.Count, cancelled);
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="searchText"></param>
        /// <param name="searchType"></param>
        public void Search(SearchCriteria sc, bool cumulative, CancellationToken ct, int numContextLines)
        {
            Task.Run(() => {

                DateTime start = DateTime.Now;
                bool cancelled = false;
                long matches = 0;
                try
                {
                    long counter = 0;
                    string line = string.Empty;
                    bool located = false;

                    foreach (LogLine ll in this.Lines)
                    {
                        if (cumulative == false)
                        {
                            // Reset the match flag
                            ll.SearchMatches.Clear();
                            //ll.IsContextLine = false;

                            ClearContextLine(ll.LineNumber, numContextLines);
                        }
                        else
                        {
                            if (ll.SearchMatches.Count > 0)
                            {
                                continue;
                            }
                        }

                        line = this.GetLine(ll.LineNumber);

                        located = false;
                        switch (sc.Type)
                        {
                            case Global.SearchType.SubStringCaseInsensitive:
                                if (line.IndexOf(sc.Pattern, 0, StringComparison.OrdinalIgnoreCase) > -1)
                                {
                                    located = true;
                                }
                                break;

                            case Global.SearchType.SubStringCaseSensitive:
                                if (line.IndexOf(sc.Pattern, 0, StringComparison.Ordinal) > -1)
                                {
                                    located = true;
                                }
                                break;

                            case Global.SearchType.RegexCaseInsensitive:
                                if (Regex.Match(line, sc.Pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled) != Match.Empty)
                                {
                                    located = true;
                                }
                                break;

                            case Global.SearchType.RegexCaseSensitive:
                                if (Regex.Match(line, sc.Pattern, RegexOptions.Compiled) != Match.Empty)
                                {
                                    located = true;
                                }
                                break;

                            default:
                                break;
                        }

                        if (located == false)
                        {
                            ll.SearchMatches.Remove(sc.Id);
                        }
                        else
                        {
                            matches++;
                            ll.SearchMatches.Add(sc.Id);

                            if (numContextLines > 0)
                            {
                                this.SetContextLines(ll.LineNumber, numContextLines);
                            }
                        }

                        if (counter++ % 50 == 0)
                        {
                            OnProgressUpdate((int)((double)counter / (double)this.Lines.Count * 100));

                            if (ct.IsCancellationRequested)
                            {
                                cancelled = true;
                                return;
                            }
                        }
                    }
                }
                finally
                {
                    DateTime end = DateTime.Now;

                    OnProgressUpdate(100);
                    OnSearchComplete(end - start, matches, 1, cancelled);
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        public void Export(string filePath, CancellationToken ct)
        {
            this.ExportToFile((IEnumerable<LogLine>)this.ItemsSource, filePath, ct);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        public void ExportSelected(string filePath, CancellationToken ct)
        {
            this.ExportToFile(listLines.SelectedItems.Cast<LogLine>(), filePath, ct);
        }

        /// <summary>
        /// 
        /// </summary>
        public void ShowMultiSearch()
        {
            WindowMultiSearch wms = new WindowMultiSearch(this.Searches);
            // Hack to set owner from a user control?!
            HwndSource source = HwndSource.FromVisual(this) as HwndSource;
            if (source != null)
            {
                WindowInteropHelper helper = new WindowInteropHelper(wms);
                helper.Owner = source.Handle;
            }
            if (wms.ShowDialog() == false)
            {
                return;
            }

            // Clear any existing filter ID's as we will only show the multi-string search
            this.FilterIds.Clear();
            this.Searches.Reset();
            foreach (SearchCriteria sc in wms.NewSearches)
            {
                // Add the ID so that any matches show up straight away
                this.FilterIds.Add(sc.Id);
                this.Searches.Add(sc);
            }

            OnMultiSearchInitiated(wms.NewSearches);
        }

        /// <summary>
        /// 
        /// </summary>
        public void UpdateColumnWidths()
        {
            if (double.IsNaN(colData.Width))
            {
                colData.Width = colData.ActualWidth;
                colData.Width = double.NaN;
            }
           
            if (double.IsNaN(colLineNum.Width))
            {
                colLineNum.Width = colLineNum.ActualWidth + 50;
                colLineNum.Width = double.NaN;
            }            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lineNumber"></param>
        /// <returns></returns>
        public string GetLine(int lineNumber)
        {
            if (lineNumber >= this.Lines.Count)
            {
                return string.Empty;
            }

            byte[] buffer = new byte[this.Lines[lineNumber].CharCount + 1];
            try
            {
                this.readMutex.WaitOne();
                this.fileStream.Seek(this.Lines[lineNumber].Offset, SeekOrigin.Begin);
                this.fileStream.Read(buffer, 0, this.Lines[lineNumber].CharCount);
                this.readMutex.ReleaseMutex();
            }
            catch (Exception) { }

            return Regex.Replace(Encoding.ASCII.GetString(buffer), "[\0-\b\n\v\f\x000E-\x001F\x007F-ÿ]", "", RegexOptions.Compiled);
        }
        #endregion

        #region Properties
        /// <summary>
        /// 
        /// </summary>
        public System.Collections.IEnumerable ItemsSource
        {
            get
            {
                return listLines.ItemsSource;
            }
            set
            {
                listLines.ItemsSource = value;
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lineNumber"></param>
        /// <param name="numLines"></param>
        private void SetContextLines(long lineNumber, int numLines)
        {
            long temp = numLines;
            if (lineNumber < this.Lines.Count)
            {
                if (numLines + lineNumber > this.Lines.Count - 1)
                {
                    temp = this.Lines.Count - lineNumber - 1;
                }
                for (int index = 1; index <= temp; index++)
                {
                    this.Lines[(int)lineNumber + index].IsContextLine = true;
                }
            }

            if (lineNumber > 0)
            {
                if (lineNumber - numLines < 0)
                {
                    temp = lineNumber;
                }
                for (int index = 1; index <= temp; index++)
                {
                    this.Lines[(int)lineNumber - index].IsContextLine = true;
                }
            }
        }

        /// <summary>
        /// Clear the line that is the next after the farthest context
        /// line, so the flag is reset and we won't overwrite
        /// </summary>
        /// <param name="lineNumber"></param>
        /// <param name="numLines"></param>
        private void ClearContextLine(long lineNumber, int numLines)
        {
            if ((int)lineNumber + numLines + 1 < this.Lines.Count - 1)
            {
                this.Lines[(int)lineNumber + numLines + 1].IsContextLine = false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="ct"></param>
        private void ExportToFile(IEnumerable<LogLine> lines, string filePath, CancellationToken ct)
        {
            Task.Run(() =>
            {
                DateTime start = DateTime.Now;
                bool cancelled = false;
                try
                {
                    using (FileStream fs = new FileStream(filePath, FileMode.Create))
                    {
                        string lineStr = string.Empty;
                        byte[] lineBytes;
                        byte[] endLine = new byte[2] { 13, 10 };

                        long counter = 0;
                        foreach (LogLine ll in lines)
                        {
                            lineStr = this.GetLine(ll.LineNumber);
                            lineBytes = Encoding.ASCII.GetBytes(lineStr);
                            fs.Write(lineBytes, 0, lineBytes.Length);
                            // Add \r\n
                            fs.Write(endLine, 0, 2);

                            if (counter++ % 50 == 0)
                            {
                                OnProgressUpdate((int)((double)counter / (double)Lines.Count * 100));

                                if (ct.IsCancellationRequested)
                                {
                                    cancelled = true;
                                    return;
                                }
                            }
                        }

                    }
                }
                finally
                {
                    DateTime end = DateTime.Now;

                    OnProgressUpdate(100);
                    OnExportComplete(end - start, cancelled);
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="charCount"></param>
        private void AddLine(long offset, int charCount)
        {
            LogLine ll = new LogLine
            {
                Control = this,
                Offset = offset,
                CharCount = charCount,
                LineNumber = this.LineCount
            };

            this.Lines.Add(ll);

            if (charCount > this.LongestLine.CharCount)
            {
                this.LongestLine.CharCount = charCount;
                this.LongestLine.LineNumber = ll.LineNumber;
            }

            this.LineCount++;
        }
        #endregion

        #region Event Methods
        /// <summary>
        /// 
        /// </summary>
        private void OnExportInitiated(bool all)
        {
            ExportInitiated?.Invoke(this, all);
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnMultiSearchInitiated(List<SearchCriteria> searches)
        {
            MultiSearchInitiated?.Invoke(this, searches);
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnLoadError(string message)
        {
            LoadError?.Invoke(this.FileName, message);
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnProgressUpdate(int progress)
        {
            ProgressUpdate?.Invoke(progress);
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnLoadComplete(TimeSpan duration, bool cancelled)
        {
            LoadComplete?.Invoke(this, this.FileName, duration, cancelled);
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnExportComplete(TimeSpan duration, bool cancelled)
        {
            ExportComplete?.Invoke(this, this.FileName, duration, cancelled);
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnSearchComplete(TimeSpan duration, long matches, int numTerms, bool cancelled)
        {
            SearchComplete?.Invoke(this, this.FileName, duration, matches, numTerms, cancelled);
        }
        #endregion

        #region Filter Methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool ShowFilter(object item)
        {
            var ll = item as LogLine;
            return ll != null && (ll.SearchMatches.Intersect(this.FilterIds).Any() == true || (ll.IsContextLine == true));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool HideFilter(object item)
        {
            var ll = item as LogLine;
            return ll != null && (ll.SearchMatches.Intersect(this.FilterIds).Any() == false);
        }
        #endregion

        #region Context Menu Event Handlers
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CtxMenuFilteringShowMatched_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(this.ItemsSource);
            view.Filter = ShowFilter;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CtxMenuFilteringHideMatched_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(this.ItemsSource);
            view.Filter = HideFilter;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CtxMenuFilteringClear_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(this.ItemsSource);
            view.Filter = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CtxMenuSearchViewTerms_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            WindowSearchTerms wst = new WindowSearchTerms(this.Searches);
            // Hack to set owner from a user control?!
            HwndSource source = HwndSource.FromVisual(this) as HwndSource;
            if (source != null)
            {
                WindowInteropHelper helper = new WindowInteropHelper(wst);
                helper.Owner = source.Handle;
            }
            if (wst.ShowDialog() == false)
            {
                return;
            }

            this.Searches = wst.Searches;
            this.FilterIds.Clear();
            foreach (SearchCriteria sc in this.Searches.Items)
            {
                if (sc.Enabled == false)
                {
                    continue;
                }

                this.FilterIds.Add(sc.Id);
            }

            listLines.ItemsSource = null;
            listLines.ItemsSource = this.Lines;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CtxMenuExportAll_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            OnExportInitiated(true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CtxMenuExportSelected_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            OnExportInitiated(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CtxMenuCopy_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            //LogFile lf = logs[tabControl.SelectedTab.Tag.ToString()];

            foreach (LogLine ll in listLines.SelectedItems)
            {
                sb.AppendLine(this.GetLine(ll.LineNumber));
            }

            Clipboard.SetText(sb.ToString());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CtxMenu_ContextMenuOpening(object sender, System.Windows.Controls.ContextMenuEventArgs e)
        {
            bool enableLineOps = true;
            if (this.LineCount == 0)
            {
                enableLineOps = false;
            }

            ctxMenuLinesFirst.IsEnabled = enableLineOps;
            ctxMenuLinesLast.IsEnabled = enableLineOps;
            ctxMenuLinesGoTo.IsEnabled = enableLineOps;

            if (listLines.SelectedItems.Count > this.config.MultiSelectLimit)
            {
                ctxMenuCopy.IsEnabled = false;
                ctxMenuExportSelected.IsEnabled = false;
                return;
            }

            ctxMenuCopy.IsEnabled = true;
            ctxMenuExportSelected.IsEnabled = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CtxMenuLinesGoTo_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            WindowGoToLine wgtl = new WindowGoToLine(this.LineCount);
            // Hack to set owner from a user control?!
            HwndSource source = HwndSource.FromVisual(this) as HwndSource;
            if (source != null)
            {
                WindowInteropHelper helper = new WindowInteropHelper(wgtl);
                helper.Owner = source.Handle;
            }

            if (wgtl.ShowDialog() == false)
            {
                return;
            }

            this.listLines.ScrollIntoView(this.listLines.Items[wgtl.LineNo]);
            this.listLines.SelectedItem = this.listLines.Items[wgtl.LineNo];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CtxMenuLinesFirst_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.listLines.ScrollIntoView(this.listLines.Items[0]);
            this.listLines.SelectedItem = this.listLines.Items[0];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CtxMenuLinesLast_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.listLines.ScrollIntoView(this.listLines.Items[this.LineCount - 1]);
            this.listLines.SelectedItem = this.listLines.Items[this.LineCount - 1];
        }
        #endregion

        #region Listview Event Handlers
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListLines_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (listLines.SelectedItems.Count != 1)
            {
                return;
            }

            LogLine ll = (LogLine)listLines.SelectedItem;

            WindowLine wl = new WindowLine(this.GetLine(ll.LineNumber));
            // Hack to set owner from a user control?!
            HwndSource source = HwndSource.FromVisual(this) as HwndSource;
            if (source != null)
            {
                WindowInteropHelper helper = new WindowInteropHelper(wl);
                helper.Owner = source.Handle;
            }

            wl.ShowDialog();
        }
        #endregion
    }
}

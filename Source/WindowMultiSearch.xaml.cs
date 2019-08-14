using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace LogViewer2
{
    /// <summary>
    /// Interaction logic for WindowMultiSearch.xaml
    /// </summary>
    public partial class WindowMultiSearch : Window
    {
        #region Member Variables
        public List<string> Patterns { get; private set; } = new List<string>();
        private Searches existingSearches;
        public List<SearchCriteria> NewSearches { get; private set; } = new List<SearchCriteria>();
        #endregion

        #region Constructor
        /// <summary>
        /// 
        /// </summary>
        public WindowMultiSearch(Searches searches)
        {
            InitializeComponent();

            cmbSearchType.SelectedIndex = 0;
            this.existingSearches = searches;
        }
        #endregion

        #region Button Event Handlers
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (cmbSearchType.SelectedIndex == -1)
            {
                MessageBox.Show("Type is not selected", Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                cmbSearchType.Focus();
                return;
            }

            if (this.Patterns.Count == 0)
            {
                MessageBox.Show("No search patterns loaded", Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                listSearchTerms.Focus();
                return;
            }

            // Disable all existing SearchCriteria
            foreach (SearchCriteria sc in this.existingSearches.Items)
            {
                sc.Enabled = false;
            }

            bool exists = false;
            foreach (string p in this.Patterns)
            {
                SearchCriteria sc = new SearchCriteria();
                sc.Type = (Global.SearchType)cmbSearchType.SelectedIndex;
                sc.Pattern = p;
                sc.Enabled = true;
                sc.Id = this.existingSearches.Add(sc);
                //if (sc.Id == 0)
                //{
                //    sc.Enabled = true;
                //    exists = true;
                //    continue;
                //}

                NewSearches.Add(sc);
            }

            if (exists == true)
            {
                MessageBox.Show("At least one pattern already exists and has not been added", Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }

            DialogResult = true;
            Close();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "All Files|*.*";
            openFileDialog.FileName = "*.*";
            openFileDialog.Title = "Select file";

            if (openFileDialog.ShowDialog(this) == false)
            {
                return;
            }

            string line = string.Empty;
            using (StreamReader sr = new StreamReader(openFileDialog.FileName))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    this.Patterns.Add(line);
                }
            }

            listSearchTerms.ItemsSource = null;
            listSearchTerms.ItemsSource = this.Patterns;
        }
        #endregion
    }
}

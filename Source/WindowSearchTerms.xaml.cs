using System.Windows;

namespace LogViewer2
{
    /// <summary>
    /// Interaction logic for WindowSearchTerms.xaml
    /// </summary>
    public partial class WindowSearchTerms : Window
    {
        #region Member Variables/Properties
        public Searches Searches { get; private set; }
        #endregion

        #region Constructor
        /// <summary>
        /// 
        /// </summary>
        public WindowSearchTerms(Searches searches)
        {
            InitializeComponent();

            listSearches.ItemsSource = searches.Items;
            this.Searches = searches;
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
            DialogResult = true;
            Close();
        }
        #endregion
    }
}

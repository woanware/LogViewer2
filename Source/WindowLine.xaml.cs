using System.Windows;

namespace LogViewer2
{
    /// <summary>
    /// Interaction logic for WindowSearchTerms.xaml
    /// </summary>
    public partial class WindowLine : Window
    {
        #region Constructor
        /// <summary>
        /// 
        /// </summary>
        public WindowLine(string line)
        {
            InitializeComponent();

            textLine.Text = line;
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

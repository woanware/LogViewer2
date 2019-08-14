using System.Windows;

namespace LogViewer2
{
    /// <summary>
    /// Interaction logic for WindowConfiguration.xaml
    /// </summary>
    public partial class WindowConfiguration : Window
    {
        #region Properties
        public Configuration Config { get; private set; }
        #endregion

        #region Constructor
        /// <summary>
        /// 
        /// </summary>
        public WindowConfiguration(Configuration config)
        {
            InitializeComponent();

            this.Config = config;
            if (this.Config.NumContextLines > 0)
            {
                chkShowContext.IsChecked = true;
                cmbContextLines.SelectedIndex = this.Config.NumContextLines - 1;
                ChkShowContext_Checked(this, null);
            }
            else
            {
                chkShowContext.IsChecked = false;
                cmbContextLines.SelectedIndex = 0;
                ChkShowContext_Unchecked(this, null);
            }            
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
            if (chkShowContext.IsChecked == true)
            {
                Config.NumContextLines = cmbContextLines.SelectedIndex + 1;
            }
            else
            {
                Config.NumContextLines = 0;
            }

            DialogResult = true;
            Close();
        }
        #endregion

        #region Checkbox Event Handlers
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChkShowContext_Checked(object sender, RoutedEventArgs e)
        {
            cmbContextLines.IsEnabled = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChkShowContext_Unchecked(object sender, RoutedEventArgs e)
        {
            cmbContextLines.IsEnabled = false;
        }
        #endregion
    }
}

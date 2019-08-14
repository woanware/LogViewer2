using System.Windows;

namespace LogViewer2
{
    /// <summary>
    /// Interaction logic for WindowGoToLine.xaml
    /// </summary>
    public partial class WindowGoToLine : Window
    {
        #region Properties
        public int LineNo { get; private set; }
        private int maxLines = 0;
        #endregion

        #region Constructor
        /// <summary>
        /// 
        /// </summary>
        public WindowGoToLine(int maxLines)
        {
            InitializeComponent();

            textLine.Focus();
            this.maxLines = maxLines;
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
            if (textLine.Text.Trim().Length == 0)
            {
                MessageBox.Show("Line number must be entered", Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                textLine.Focus();
                return;
            }

            var isNumeric = int.TryParse(textLine.Text, out int lineNo);

            if (isNumeric == false)
            {
                MessageBox.Show("Line number value is invalid", Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                textLine.Focus();
                return;
            }

            if (lineNo > this.maxLines)
            {
                MessageBox.Show("Line number value is greater than the number of lines available", Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                textLine.Focus();
                return;
            }

            this.LineNo = lineNo;
            DialogResult = true;
            Close();
        }
        #endregion
    }
}

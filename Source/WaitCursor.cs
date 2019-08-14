using System;
using System.Windows.Input;

namespace LogViewer2
{
    /// <summary>
    /// 
    /// </summary>
    public class WaitCursor : IDisposable
    {
        #region Member Variables
        private Cursor previousCursor;
        #endregion

        #region Constructor
        /// <summary>
        /// 
        /// </summary>
        public WaitCursor()
        {
            this.previousCursor = Mouse.OverrideCursor;
            Mouse.OverrideCursor = Cursors.Wait;
        }
        #endregion

        #region IDisposable Members
        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            Mouse.OverrideCursor = previousCursor;
        }

        #endregion
    }
}

using System.Collections.Generic;
using System.Linq;

namespace LogViewer2
{
    /// <summary>
    /// 
    /// </summary>
    public class LogLine
    {
        #region Member Variables/Properties
        public ControlLog Control { get; set; }
        public int LineNumber { get; set; } = 0;
        public int CharCount { get; set; } = 0;
        public long Offset { get; set; } = 0;
        public List<ushort> SearchMatches { get; set; } = new List<ushort>();
        public bool IsContextLine { get; set; } = false;
        #endregion

        #region Properties
        /// <summary>
        /// 
        /// </summary>
        public string Data
        {
            get
            {
                return this.Control.GetLine(this.LineNumber);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string BackgroundColor
        {
            get
            {
                if (this.SearchMatches.Intersect(Control.FilterIds).Any() == true)
                {
                    return "LimeGreen";//System.Windows.Media.Brush. .FromRgb(66, 245, 105);
                }
                else if (this.IsContextLine == true)
                {
                    // return System.Windows.Media.Color.FromRgb(142, 145, 143);
                    return "Gray";
                }

                return "White";//System.Windows.Media.Color.FromRgb(255, 255, 255);
            }
        }
        #endregion
    }
}

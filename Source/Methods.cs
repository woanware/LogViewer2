namespace LogViewer2
{
    class Methods
    {
        public static string GetApplicationDirectory()
        {
            return System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
        }
    }
}

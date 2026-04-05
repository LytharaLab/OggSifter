namespace OggSifter
{
    internal static class Program
    {
        public static Version VERSION = new Version(1, 0, 0);
        
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            
            using var warningForm = new WarningForm();
            if (warningForm.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            
            Application.Run(new MainForm());
        }
    }
}

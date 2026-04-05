namespace OggSifter
{
    internal static class Program
    {
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

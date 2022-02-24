
using System;
using System.Windows.Forms;

namespace SnapTestDocuments
{
    static class Program
    {
        
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var appSettings = System.Configuration.ConfigurationManager.AppSettings;
            string result = appSettings["ActiveSnapControl"] ?? "ExtSnapControl";


            Application.Run(new SnapControlForm());
        }
    }
}

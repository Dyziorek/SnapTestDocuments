
using System;
using System.Windows.Forms;
using Nuance.SpeechAnywhere;

namespace SnapTestDocuments
{
    static class Program
    {
        private const string _partnerGuid = "455780e4-21b3-4755-9ddd-d73cf636bed9";
        private const string _organizationToken = "2bfb2af1-f235-416b-8f66-52a3638417cb";

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

            if (result == "DictSnapControl")
            {
                Session.SharedSession.Open("ddus", _organizationToken, _partnerGuid, "SnapTestDocuments");
            }

            Application.Run(new SnapControlForm());
        }
    }
}

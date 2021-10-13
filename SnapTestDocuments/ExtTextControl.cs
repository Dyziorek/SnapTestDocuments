using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SnapTestDocuments
{

    class ExtTextControl : TextBox
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            switch (m.Msg)
            {
                case 12:
                    log.Info(string.Format("Entered whole text: {0}", Marshal.PtrToStringAuto(m.LParam)));
                    break;
                case 13:
                    log.Info(string.Format("Read whole text: {0}", Marshal.PtrToStringAuto(m.LParam)));
                    break;
                case 0xc2:
                    log.Info(string.Format("Replace selected text: {0}, Undo{1}", Marshal.PtrToStringAuto(m.LParam), (int)m.WParam));
                    break;
                case 0xb0:
                    log.Info(string.Format("Get Selection: from:{0}, to:{1} - ret:{2:X}", Marshal.ReadInt16(m.LParam), Marshal.ReadInt16(m.WParam), (int)m.Result));
                    break;
                case 0xb1:
                    log.Info(string.Format("Set Selection : from:{0}, to:{1} - ret:{2}", (int)(m.LParam), (int)(m.WParam), (int)m.Result));
                    break;
                default:
                    log.Info("Message: " + m.ToString());
                    break;
            }

        }
    }
}
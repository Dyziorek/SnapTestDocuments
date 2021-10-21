using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SnapTestDocuments
{
    class ExtTxTextControl : TXTextControl.TextControl
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

        protected override void WndProc(ref Message m)
        {
            //log.Info("Message: " + m.ToString() + " with result ");

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
                    log.Info(string.Format("Replace selected text: '{0}'", Marshal.PtrToStringAuto(m.LParam)));
                    break;
                case 0xb0:
                    log.Info(string.Format("Get Selection: from:{0}, to:{1} - ret:{2:X}", Marshal.ReadInt16(m.WParam), Marshal.ReadInt16(m.LParam), (int)m.Result));
                    break;
                case 0xb1:
                    log.Info(string.Format("Set Selection : from:{0}, to:{1} - ret:{2}", (int)(m.WParam), (int)(m.LParam), (int)m.Result));
                    break;
                case 0xb2:
                    var rECTObj = ClientRectangle;
                    rECTObj = (System.Drawing.Rectangle)Marshal.PtrToStructure(m.LParam, typeof(System.Drawing.Rectangle));
                    log.Debug(string.Format("Get Control Rect {0},{1},{2},{3}", rECTObj.Left, rECTObj.Top, rECTObj.Right, rECTObj.Bottom));
                    break;
                case 0xd7:
                    log.Debug(string.Format("CharFromPos loc x,y:{0} return: x.y:{2}.{1} ", (int)m.LParam, ((int)m.Result & 0xFFFF0000) >> 16, (int)m.Result & 0x0000FFFF));
                    break;
                default:
                    log.Info("Message: " + m.ToString());
                    break;
            }

        }
    }
}

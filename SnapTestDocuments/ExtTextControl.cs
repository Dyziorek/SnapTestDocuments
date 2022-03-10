using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SnapTestDocuments
{

    class ExtTextControl : TextBox
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger("ExtTextControl");

        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
            public RECT(int l, int r, int t, int b)
            {
                Left = l;
                Right = r;
                Top = t;
                Bottom = b;
            }
        }

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
                    var ptrData = (RECT)Marshal.PtrToStructure(m.LParam, typeof(RECT));
                    log.Info(string.Format("Get Control Rect {0},{1},{2},{3}", ptrData.Left, ptrData.Top, ptrData.Right, ptrData.Bottom));
                    break;
                case 0xd7:
                    log.Info(string.Format("CharFromPos loc x,y:{0}, {1} return: x.y:{2},{3} ",  (int)m.LParam & 0x0000FFFF, ((int)m.LParam & 0xFFFF0000) >> 16, (int)m.Result & 0x0000FFFF , ((int)m.Result & 0xFFFF0000) >> 16));
                    break;
                case 0xd6: // POSFROMCHAR
                    if((int)m.Result == -1)
                    {
                        log.Info(string.Format("PosFromChar CharPos :{0} return: -1 ", (int)m.WParam & 0x0000FFFF));
                    }
                    else
                        log.Info(string.Format("PosFromChar CharPos :{0}  return: values({1:X}) x.y:{2},{3} ", (int)m.WParam & 0x0000FFFF, (int)m.Result, (int)m.Result & 0x0000FFFF, ((int)m.Result & 0xFFFF0000) >> 16));
                    break;
                default:
                    log.InfoFormat("Message:  ID: 0x{0:x}, Lparam: {1}, WParam {2}, ret {3}", m.Msg, m.LParam, m.WParam, m.Result);
                    break;
            }
            
        }
    }
}
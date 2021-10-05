using DevExpress.XtraRichEdit.API.Native;
using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Windows.Forms;

namespace SnapTestDocuemnts
{
    public class ExtSnapControl : DevExpress.Snap.SnapControl
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

        public Tuple<int, int> lastselectionPair = new Tuple<int, int>(0,0);
        public Tuple<int, int> currentselectionPair = new Tuple<int, int>(0, 0);

        protected override CreateParams CreateParams
        {
            get
            {
                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
                CreateParams cp = base.CreateParams;

                cp.ClassName = "EDIT";

                return cp;
            }
        }

        private Tuple<int, int> SetSelect(int wparam, int lparam)
        {
            int minPos = Math.Min(wparam, lparam);
            int maxPos = Math.Max(wparam, lparam);
            if (maxPos == minPos)
            {
                maxPos += 1;
                minPos += 1;
            }
            if (minPos >= Document.Length)
            {
                minPos = Document.Length - 1;
                maxPos = Document.Length - 1;
            }
            Document.Selection = Document.CreateRange(Document.CreatePosition(minPos), maxPos - minPos);
            return new Tuple<int, int>(minPos, maxPos);
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case 194:
                    //log.Info(string.Format("Replace selected text: {0}, Undo{1}", Marshal.PtrToStringAuto(m.LParam), (int)m.WParam));
                    string messageText = Marshal.PtrToStringAuto(m.LParam);
                    var caretPos = Document.Selection;
                    if (caretPos.Start.ToInt() != lastselectionPair.Item1)
                    {
                        caretPos = Document.CreateRange(Document.CreatePosition(lastselectionPair.Item1), lastselectionPair.Item2 - lastselectionPair.Item1);
                    }
                    SubDocument docFragment = caretPos.BeginUpdateDocument();
                    try
                    {
                        BeginUpdate();
                        if (true || caretPos.Length > 0)
                        {
                            docFragment.Replace(caretPos, messageText);
                        }

                    }
                    finally
                    {
                        caretPos.EndUpdateDocument(docFragment);
                        EndUpdate();
                    }
                    break;
                case 176: //EM_GETSEL
                    base.WndProc(ref m);
                    var docSelection = Document.Selection;
                    int startPos = docSelection.Start.ToInt();
                    int endPos = docSelection.End.ToInt();
                    if (m.WParam != IntPtr.Zero)
                    {
                        Marshal.WriteInt16(m.WParam, Convert.ToInt16(startPos - 1));
                    }
                    if (m.LParam != IntPtr.Zero)
                    {
                        Marshal.WriteInt16(m.LParam, Convert.ToInt16(endPos));
                    }
                    if (startPos > 65535)
                    {
                        startPos = 65535;
                    }
                    if (endPos > 65535)
                    {
                        endPos = 65535;
                    }
                    Int32 retVal = Convert.ToInt16(startPos) + (Convert.ToInt16(endPos) << 16);
                    m.Result = (IntPtr)retVal;
                    break;
                case 177: //EM_SETSEL 
                    var lastCaretPos = Document.Selection;
                    lastselectionPair = new Tuple<int, int>(lastCaretPos.Start.ToInt(), lastCaretPos.Start.ToInt() + lastCaretPos.Length);
                    currentselectionPair = SetSelect((int)m.WParam, (int)m.LParam);
                    //log.Info(string.Format("Updated Selection:  old: {0} new: {1}", lastselectionPair, currentselectionPair));
                    m.Result = (IntPtr)1;
                    break;
                case 135: //WM_GETDLGCODE
                    m.Result = (IntPtr)0x89;
                    break;
                case 0xb8: //EM_GETMODIFY
                    m.Result = (IntPtr)0x1;
                    break;
                case 12:
                    if (m.LParam != IntPtr.Zero)
                    {
                        Text = Marshal.PtrToStringAuto(m.LParam);
                        m.Result = (IntPtr)1;
                    }
                    break;
                case 13:
                    string textBuff = Text;
                    textBuff = textBuff.Replace("\r", String.Empty);
                    if (m.WParam.ToInt32() > 0)
                    {
                        var textBuffPtr = System.Text.Encoding.Unicode.GetBytes(textBuff);
                        if (textBuff.Length >= m.WParam.ToInt32())
                        {
                            Marshal.Copy(textBuffPtr, 0, m.LParam, m.WParam.ToInt32() - 1);
                            Marshal.WriteByte(m.LParam, m.WParam.ToInt32(), 0);
                            m.Result = (IntPtr)(m.WParam.ToInt32() - 1);
                        }
                        else
                        {
                            Marshal.Copy(textBuffPtr, 0, m.LParam, textBuffPtr.Length);
                            Marshal.WriteInt16(m.LParam, textBuffPtr.Length, 0);
                            m.Result = (IntPtr)(textBuff.Length);
                        }
                    }
                    else
                    {
                        m.Result = IntPtr.Zero;
                    }
                    break;
                case 14:
                    string textBuffLen = Text;
                    textBuffLen = textBuffLen.Replace("\r", String.Empty);
                    m.Result = (IntPtr)textBuffLen.Length;
                    break;
                case 0xb2:
                    if (m.LParam != IntPtr.Zero)
                    {
                        var rectObj = ClientRectangle;
                        Marshal.StructureToPtr(rectObj, m.LParam, false);
                        m.Result = (IntPtr)1;
                    }
                    break;
                case 0xd6:
                    if ((int)m.WParam >= 0)
                    {
                        var rectObj = GetBoundsFromPosition(Document.CreatePosition((int)m.WParam));
                        //log.Info(string.Format("Position  C:{0},X:{1},Y:{2}", (int)m.WParam, rectObj.X, rectObj.Y));
                        m.Result = (IntPtr)(Convert.ToInt16(rectObj.X) + (Convert.ToInt16(rectObj.Y) << 16));
                    }
                    break;
                default:
                    base.WndProc(ref m);
                    break;
            }

            //switch (m.Msg)
            //{
            //    case 12:
            //        log.Info(string.Format("Before Entered whole text: {0}", Marshal.PtrToStringAnsi(m.LParam)));
            //        break;
            //    case 13:
            //        log.Info(string.Format("Before Read whole text: {0}", Marshal.PtrToStringAnsi(m.LParam)));
            //        break;
            //    case 0xc2:
            //        log.Info(string.Format("Before Replace selected text: {0}, Undo{1}", Marshal.PtrToStringAuto(m.LParam), (int)m.WParam));
            //        break;
            //    case 0xb0:
            //        log.Info(string.Format("Before Get Selection: from:{0}, to:{1} - ret:{2:X}", Marshal.ReadInt16(m.LParam), Marshal.ReadInt16(m.WParam), (int)m.Result));
            //        break;
            //    case 0xb1:
            //        log.Info(string.Format("Before Set Selection : from:{0}, to:{1} - ret:{2}", (int)(m.LParam), (int)(m.WParam), (int)m.Result));
            //        break;
            //    default:
            //        log.Info("Before Message: " + m.ToString());
            //        break;
            //}

            //if (m.Msg != 14)
            // base.WndProc(ref m);


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
                case 0xb2:
                    RECT rect = new RECT(0, 0, 0, 0);
                    rect = (RECT)Marshal.PtrToStructure(m.LParam, typeof(RECT));
                    log.Info(string.Format("Control Rect {0},{1},{2},{3}", rect.Left, rect.Top, rect.Right, rect.Bottom));
                    break;
                case 0xd6:
                    break;
                default:
                    log.Info("Message: " + m.ToString());
                    break;
            }

        }
    }
}

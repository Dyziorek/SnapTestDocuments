using DevExpress.XtraRichEdit.API.Native;
using DevExpress.XtraRichEdit.SpellChecker;
using DevExpress.XtraSpellChecker.Native;
using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Windows.Forms;

namespace SnapTestDocuments
{
    public class ExtSnapControl : DevExpress.Snap.SnapControl
    {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);
        private ISnapReportContext _currentContext;


        public Tuple<int, int> lastselectionPair = new Tuple<int, int>(0, 0);
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

        public ExtSnapControl()
        {
            ControlToSpellCheckTextControllerMapper.Instance.Register(typeof(ExtSnapControl), typeof(RichEditSpellCheckController));
        }

        public ISnapReportContext SetContext
        {
            set
            {
                _currentContext = value;
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
            if (Math.Min(maxPos, minPos) >= 0)
            {
                Document.Selection = Document.CreateRange(Document.CreatePosition(minPos), maxPos - minPos);
            }
            return new Tuple<int, int>(minPos, maxPos);
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case 194:  // EM_REPLACESEL,  replace text in place of current selection given in pair lastselectionPair, also if no selection simply dictates new text.
                    if (log.IsDebugEnabled)
                    {
                        log.Debug(string.Format("Replace selected text: {0}, Undo: {1}", Marshal.PtrToStringAuto(m.LParam), (int)m.WParam));
                    }

                    string messageText = Marshal.PtrToStringAuto(m.LParam);
                    if (_currentContext != null && _currentContext.GetManager<IDragonAccessManager>() != null)
                    {
                        _currentContext.GetManager<IDragonAccessManager>().ReplaceText(messageText);
                    }
                    else
                    {
                        var caretPos = Document.Selection;
                        if (caretPos.Start.ToInt() != currentselectionPair.Item1)
                        {
                            caretPos = Document.CreateRange(Document.CreatePosition(lastselectionPair.Item1), lastselectionPair.Item2);
                        }
                        SubDocument docFragment = caretPos.BeginUpdateDocument();
                        try
                        {
                            BeginUpdate();
                            docFragment.Replace(caretPos, messageText);
                        }
                        finally
                        {
                            caretPos.EndUpdateDocument(docFragment);
                            EndUpdate();
                        }
                    }
                    break;
                case 176: //EM_GETSEL - returns current selection merked in document
                    Tuple<int, int> lastCaretPos = null;
                    // Temporary. The order is frozen in Report Sign Out Entry when Interpretation section is added to Snap Test Specific repot template. 
                    if (_currentContext != null && _currentContext.GetManager<IDragonAccessManager>() != null)
                    {
                        lastCaretPos = _currentContext.GetManager<IDragonAccessManager>().GetSel();
                    }
                    else if (Document.Selection != null)
                    {
                        var docSelection = Document.Selection;
                        int startPos = docSelection.Start.ToInt();
                        int endPos = docSelection.End.ToInt();
                        lastCaretPos = new Tuple<int, int>(startPos, endPos - startPos);
                    }
                    if (m.WParam != IntPtr.Zero)
                    {
                        Marshal.WriteInt16(m.WParam, Convert.ToInt16(lastCaretPos.Item1));
                    }
                    if (m.LParam != IntPtr.Zero)
                    {
                        Marshal.WriteInt16(m.LParam, Convert.ToInt16(lastCaretPos.Item1 + lastCaretPos.Item2));
                    }
                    if (lastCaretPos.Item1 > 65535)
                    {
                        lastCaretPos = new Tuple<int, int>(65535, lastCaretPos.Item2);
                    }
                    if (lastCaretPos.Item1 + lastCaretPos.Item2 > 65535)
                    {
                        lastCaretPos = new Tuple<int, int>(lastCaretPos.Item1, 65535 - lastCaretPos.Item1);
                    }
                    Int32 retVal = Convert.ToInt16(lastCaretPos.Item1) + (Convert.ToInt16(lastCaretPos.Item2) << 16);
                    m.Result = (IntPtr)retVal;
                    break;
                case 177: //EM_SETSEL - sets Selection marker by positions provided in m.Wparam i m.LParam - store previous selection, 
                          // because Dragon sets cursor after previous selected range just before replacing text
                    Tuple<int, int> lastSelectPos = null;
                    if (_currentContext != null && _currentContext.GetManager<IDragonAccessManager>() != null)
                    {
                        lastSelectPos = _currentContext.GetManager<IDragonAccessManager>().GetSel();
                    }
                    else
                    {
                        var docSelection = Document.Selection;
                        int startPos = docSelection.Start.ToInt();
                        int endPos = docSelection.End.ToInt();
                        lastSelectPos = new Tuple<int, int>(startPos, endPos);
                    }
                    lastselectionPair = new Tuple<int, int>(lastSelectPos.Item1, Math.Abs(lastSelectPos.Item2 - lastSelectPos.Item1));
                    if (_currentContext != null && _currentContext.GetManager<IDragonAccessManager>() != null)
                    {
                        _currentContext.GetManager<IDragonAccessManager>().SetSel((int)m.WParam, (int)m.LParam);
                    }
                    else
                    {
                        SetSelect((int)m.WParam, (int)m.LParam);
                    }
                    currentselectionPair = new Tuple<int, int>((int)m.WParam, Math.Abs((int)m.LParam - (int)m.WParam));

                    if (lastselectionPair == currentselectionPair)
                    {
                        m.Result = (IntPtr)0;
                    }
                    else
                    {
                        m.Result = (IntPtr)1;
                    }

                    log.DebugFormat("Updated Selection:  old: {0} new: {1} - result:{2}", lastselectionPair, currentselectionPair, m.Result);
                    break;
                case 135: //WM_GETDLGCODE
                    m.Result = (IntPtr)0x89;
                    break;
                case 0xb8: //EM_GETMODIFY
                    m.Result = (IntPtr)0x1;
                    break;
                case 13: // WM_GETTEXT - 
                    string textBuff = String.Empty;
                    if (_currentContext != null && _currentContext.GetManager<IDragonAccessManager>() != null)
                    {
                        textBuff = _currentContext.GetManager<IDragonAccessManager>().GetText();
                    }
                    else
                    {
                        textBuff = Text;
                    }
                    if (textBuff != null)
                    {
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
                    }
                    else
                    {
                        m.Result = IntPtr.Zero;
                    }
                    break;
                case 14:  // WM_GETEXTLEN - returne all text in control - this by default 
                    if (_currentContext != null && _currentContext.GetManager<IDragonAccessManager>() != null)
                    {
                        m.Result = (IntPtr)_currentContext.GetManager<IDragonAccessManager>().GetTextLen();
                    }
                    else
                    {
                        string textBuffLen = Text;
                        textBuffLen = textBuffLen.Replace("\r", String.Empty);
                        m.Result = (IntPtr)textBuffLen.Length;
                    }
                    break;
                case 0xb2:  // WM_GETRECT
                    if (m.LParam != IntPtr.Zero)
                    {
                        var rectObj = ClientRectangle;
                        Marshal.StructureToPtr(rectObj, m.LParam, false);
                        m.Result = (IntPtr)1;
                    }
                    break;
                case 0xd6: //EM_POSFROMCHAR
                    if ((int)m.WParam >= 0)
                    {
                        System.Drawing.Rectangle rectObj = new System.Drawing.Rectangle();
                        if (_currentContext != null && _currentContext.GetManager<IDragonAccessManager>() != null)
                        {
                            rectObj = _currentContext.GetManager<IDragonAccessManager>().PosFromChar((int)m.WParam);
                        }
                        else
                        {
                            rectObj = GetBoundsFromPosition(Document.CreatePosition((int)m.WParam));
                        }
                        if (log.IsDebugEnabled)
                        {
                            log.Debug(string.Format("Position  C:{0},X:{1},Y:{2}", (int)m.WParam, rectObj.X, rectObj.Y));
                        }
                        m.Result = (IntPtr)(Convert.ToInt16(rectObj.X) + (Convert.ToInt16(rectObj.Y) << 16));
                        log.Debug("Pos From Char Message processed: " + m.ToString());
                    }
                    break;
                case 0xd7: //EM_CHARFROMPOS
                    if ((int)m.LParam != 0)
                    {
                        int CharParam = (int)m.LParam;
                        int CharPos = 0;
                        System.Drawing.PointF charPos = new System.Drawing.PointF((float)(CharParam & 0xFFFF0000), (float)(CharParam & 0x0000FFFF));
                        if (_currentContext != null && _currentContext.GetManager<IDragonAccessManager>() != null)
                        {
                            CharPos = _currentContext.GetManager<IDragonAccessManager>().CharFromPos(charPos);
                        }
                        else
                        {
                            CharPos = GetPositionFromPoint(charPos).ToInt();
                        }
                        if (log.IsDebugEnabled)
                        {
                            log.Debug(string.Format("Char Pos from Point  C:{0},X:{1},Y:{2}", CharPos, charPos.X, charPos.Y));
                        }
                        m.Result = (IntPtr)(Convert.ToInt16(CharPos) + (Convert.ToInt16(0) << 16));

                    }
                    break;
                default:
                    base.WndProc(ref m);
                    break;
            }

            if (log.IsDebugEnabled)
            {
                switch (m.Msg)
                {
                    case 12:
                        log.Debug(string.Format("Entered whole text:'{0}'", Marshal.PtrToStringAuto(m.LParam)));
                        break;
                    case 13:
                        log.Debug(string.Format("Read whole text:'{0}'", Marshal.PtrToStringAuto(m.LParam)));
                        break;
                    case 14:
                        log.Debug(string.Format("Read text Length:'{0}'", (int)(m.Result)));
                        break;
                    case 0xc2:
                        log.Debug(string.Format("Replace selected text: {0}, Undo:{1}", Marshal.PtrToStringAuto(m.LParam), (int)m.WParam));
                        break;
                    case 0xb0:
                        log.Debug(string.Format("Get Selection: from:{0}, to:{1} - ret:{2:X}", Marshal.ReadInt16(m.WParam), Marshal.ReadInt16(m.LParam), (int)m.Result));
                        break;
                    case 0xb1:
                        log.Debug(string.Format("Set Selection : from:{0}, to:{1} - ret:{2}", (int)(m.LParam), (int)(m.WParam), (int)m.Result));
                        break;
                    case 0xb2:
                        var rECTObj = ClientRectangle;
                        rECTObj = (System.Drawing.Rectangle)Marshal.PtrToStructure(m.LParam, typeof(System.Drawing.Rectangle));
                        log.Debug(string.Format("Control Rect {0},{1},{2},{3}", rECTObj.Left, rECTObj.Top, rECTObj.Right, rECTObj.Bottom));
                        break;
                    default:
                        log.Debug("Base Message processed: " + m.ToString());
                        break;
                }
            }
        }
    }
}

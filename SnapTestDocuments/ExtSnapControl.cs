﻿using DevExpress.XtraRichEdit.API.Native;
using DevExpress.XtraRichEdit.SpellChecker;
using DevExpress.XtraSpellChecker.Native;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text;

namespace SnapTestDocuments
{

    public class ExtSnapControl : DevExpress.Snap.SnapControl
    {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger("ExtSnapControl");
        private ISnapReportContext _currentContext;
        private Dictionary<int, int> mapEditSnapPos = new Dictionary<int, int>();
        private Dictionary<int, int> mapSnapEditPos = new Dictionary<int, int>();
        private Tuple<int, int> requestSelectPair = new Tuple<int, int>(0, 0);  // start, end
        private Tuple<int, int> lastselectionPair = new Tuple<int, int>(0, 0);  // start, end
        private string cachedText;
        private string lastcachedText;
        private int lastTextLength = -1;
        IntPtr hOldFont = IntPtr.Zero;
        IntPtr oldRectPtr = IntPtr.Zero;
        System.Drawing.Rectangle rectData = System.Drawing.Rectangle.Empty;
        [System.Runtime.InteropServices.DllImport("GDI32.dll")]
        public static extern bool DeleteObject(IntPtr objectHandle);
        [StructLayout (LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
            public RECT(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }
        }


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
            this.ContentChanged -= ExtSnapControl_ContentChanged;
            this.ContentChanged += ExtSnapControl_ContentChanged;
            this.SelectionChanged -= ExtSnapControl_SelectionChanged;
            this.SelectionChanged += ExtSnapControl_SelectionChanged;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (hOldFont != IntPtr.Zero)
            {
                DeleteObject(hOldFont);
            }
        }

        public ISnapReportContext SetContext
        {
            set
            {
                _currentContext = value;
            }
        }

        private void ReplaceText(string messageText)
        {
            var caretPos = Document.Selection;
            bool extendSelection = false;
            if (lastselectionPair != requestSelectPair && requestSelectPair.Item1 == requestSelectPair.Item2)
            {
                caretPos = Document.CreateRange(lastselectionPair.Item1, Math.Abs(lastselectionPair.Item2 - lastselectionPair.Item1));
            }
            string textToReplace = cachedText.Substring(caretPos.Start.ToInt(), caretPos.End.ToInt() - caretPos.Start.ToInt());
            log.InfoFormat("Replace selected text: '{0}', with  '{1}' on text '{2}'", textToReplace, messageText, cachedText);
            if (!String.IsNullOrEmpty(textToReplace) && !String.IsNullOrEmpty(messageText))
            {
                if (char.IsWhiteSpace(textToReplace[0]) != char.IsWhiteSpace(messageText[0]))
                {
                    extendSelection = true;
                }
            }
            if (extendSelection)
            {
                caretPos = Document.CreateRange(Document.CreatePosition(caretPos.Start.ToInt()), caretPos.Length);
            }
            SubDocument docFragment = caretPos.BeginUpdateDocument();
            try
            {
                docFragment.BeginUpdate();
                messageText = CalculateCachedTextChanges(caretPos, messageText);
                docFragment.Replace(caretPos, messageText);
            }
            finally
            {
                docFragment.EndUpdate();
                caretPos.EndUpdateDocument(docFragment);
                ExtSnapControl_ContentChanged(this, EventArgs.Empty);// cachedText = Text.Replace("\r", string.Empty);
                Document.CaretPosition = caretPos.End;  //ANDATA
                lastselectionPair = new Tuple<int, int>(caretPos.End.ToInt(), caretPos.End.ToInt());
            }
        }


        private Tuple<int, int> SetSelect(int wparam, int lparam)
        {
            int minPos = Math.Min(wparam, lparam);
            int maxPos = Math.Max(wparam, lparam);
            log.InfoFormat("Request SetSelect from:{0} to: {1}", wparam, lparam);
            

            if (minPos > cachedText.Length)
            {
                minPos = Document.Length;
                maxPos = Document.Length;
                log.InfoFormat("Changed Request SetSelect from:{0} to: {1}", minPos, maxPos);
            }
            
            if (minPos == -1)
            {
                Document.Selection = Document.CreateRange(Document.Range.Start, Document.Range.Length);
            }
            else
            {
                if (!mapEditSnapPos.TryGetValue(minPos, out minPos))
                {
                    log.InfoFormat("SetSelect: Unable get mapEditSnapPos correct position req: {0}", Math.Min(wparam, lparam));
                    minPos = Math.Min(wparam, lparam);
                }
                if (!mapEditSnapPos.TryGetValue(maxPos, out maxPos))
                {
                    log.InfoFormat("SetSelect: Unable get mapEditSnapPos correct position req: {0}", Math.Max(wparam, lparam));
                    maxPos = Math.Max(wparam, lparam);
                }
                log.InfoFormat("Mapped setsel orig:{0},{1}, maps{2},{3}", wparam, lparam, minPos, maxPos);
                if (Math.Abs(maxPos - minPos) == 0)
                {
                    Document.CaretPosition = Document.CreatePosition(minPos);
                }
                else
                {
                    Document.Selection = Document.CreateRange(Document.CreatePosition(Math.Min(maxPos, minPos)), Math.Abs(maxPos - minPos));
                }
            }
            return new Tuple<int, int>(minPos, maxPos);
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case 194:  // EM_REPLACESEL,  replace text in place of current selection given in pair lastselectionPair, also if no selection simply dictates new text.
                    if (log.IsInfoEnabled)
                    {
                        log.Info(string.Format("Replace selected text: '{0}', Undo: {1}", Marshal.PtrToStringAuto(m.LParam), (int)m.WParam));
                    }
                    
                    string messageText = Marshal.PtrToStringAuto(m.LParam);
                    if (_currentContext != null && _currentContext.GetManager<IDragonAccessManager>() != null)
                    {
                        _currentContext.GetManager<IDragonAccessManager>().ReplaceText(messageText);
                    }
                    else
                    {
                        ReplaceText(messageText);
                    }
                    break;
                case 176: //EM_GETSEL - returns current selection merked in document
                    Tuple<int, int> lastCaretPos = null;

                    if (_currentContext != null && _currentContext.GetManager<IDragonAccessManager>() != null)
                    {
                        lastCaretPos = _currentContext.GetManager<IDragonAccessManager>().GetSel();
                    }
                    else
                    {
                        lastCaretPos = lastselectionPair;
                    }
                    int firstPos = lastCaretPos.Item1;
                    int secondPos = lastCaretPos.Item2;
                    if (!mapSnapEditPos.TryGetValue(lastCaretPos.Item1, out firstPos))
                    {
                        log.InfoFormat("SetSelect: Unable get mapSnapEditPos correct position req: {0}", lastCaretPos.Item1);
                        firstPos = lastCaretPos.Item1;
                    }
                    //int firstPos = mapSnapEditPos[lastCaretPos.Item1];
                    if (!mapSnapEditPos.TryGetValue(lastCaretPos.Item2, out secondPos))
                    {
                        log.InfoFormat("SetSelect: Unable get mapSnapEditPos correct position req: {0}", lastCaretPos.Item2);
                        secondPos = lastCaretPos.Item2;
                    }
                    // mapSnapEditPos[lastCaretPos.Item2];
                    if (m.WParam != IntPtr.Zero)
                    {
                        Marshal.WriteInt32(m.WParam, Convert.ToInt32(firstPos)); //ANDATA, Important change
                    }
                    if (m.LParam != IntPtr.Zero)
                    {
                        Marshal.WriteInt32(m.LParam, Convert.ToInt32(secondPos)); //ANDATA, Important change
                    }
                    if (lastCaretPos.Item1 > 65535)
                    {
                        lastCaretPos = new Tuple<int, int>(65535, lastCaretPos.Item2);
                    }
                    if (lastCaretPos.Item1 + lastCaretPos.Item2 > 65535)
                    {
                        lastCaretPos = new Tuple<int, int>(lastCaretPos.Item1, 65535 - lastCaretPos.Item1);
                    }
                    Int32 retVal = Convert.ToInt16(firstPos) + (Convert.ToInt16(secondPos) << 16);
                    m.Result = (IntPtr)retVal;
                    log.InfoFormat("Get Selection: from:{0}, to:{1} - ret:{2:X8}", Marshal.ReadInt32(m.WParam), Marshal.ReadInt32(m.LParam), (int)m.Result);
                    break;
                case 177: //EM_SETSEL - sets Selection marker by positions provided in m.Wparam i m.LParam - store previous selection, 
                          // because Dragon sets cursor after previous selected range just before replacing text
                    if (_currentContext != null && _currentContext.GetManager<IDragonAccessManager>() != null)
                    {
                        requestSelectPair = _currentContext.GetManager<IDragonAccessManager>().SetSel((int)m.WParam, (int)m.LParam);
                    }
                    else
                    {
                        requestSelectPair = SetSelect((int)m.WParam, (int)m.LParam); //ANDATA
                    }
                    m.Result = (IntPtr)1;
                    log.InfoFormat("Updated Selection:  old: {0} new: {1} - result:{2}", requestSelectPair, new Tuple<int, int>((int)m.WParam, (int)m.LParam), m.Result);
                    break;
                case 135: //WM_GETDLGCODE
                    m.Result = (IntPtr)0x89;
                    break;
                case 0xb8: //EM_GETMODIFY
                    m.Result = (IntPtr)0x1;
                    break;
                case 0x31:  // WM_GETFONT
                    IntPtr hNewfont = Font.ToHfont();
                    m.Result = hNewfont;
                    DeleteObject(hOldFont);
                    hOldFont = hNewfont;
                    break;
                case 0x2111: // WM_USER + 111 (edit specific info)
                    m.Result = IntPtr.Zero;
                    break;
                case 0xb2: // WM_GETRECT
                    {
                        rectData = ClientRectangle;
                        RECT winRect = new RECT(rectData.X + 4, rectData.Y, rectData.Width - 4, rectData.Height);
                        IntPtr pnt = Marshal.AllocHGlobal(Marshal.SizeOf(winRect));
                        Marshal.StructureToPtr(winRect, pnt, false);
                        m.Result = pnt;
                        if (oldRectPtr != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(oldRectPtr);
                        }
                        oldRectPtr = pnt;
                        log.InfoFormat("GetRect: x:{0}, y:{1}, w:{2} ,h:{3}", winRect.Left, winRect.Top, winRect.Bottom, winRect.Right);
                    }
                    break;
                case 13: // WM_GETTEXT - 
                    string textBuff;
                    if (_currentContext != null && _currentContext.GetManager<IDragonAccessManager>() != null)
                    {
                        textBuff = _currentContext.GetManager<IDragonAccessManager>().GetText();
                    }
                    else
                    {
                        textBuff = cachedText;
                    }
                    if (textBuff != null)
                    {
                        //textBuff = textBuff.Replace("\r", String.Empty);
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
                                Marshal.WriteInt32(m.LParam, textBuffPtr.Length, 0);
                                m.Result = (IntPtr)(textBuff.Length);
                            }
                        }
                        else
                        {
                            m.Result = IntPtr.Zero;
                        }
                        if (!string.Equals(lastcachedText,cachedText))
                        {
                            log.InfoFormat("GetText reported:  text: {0} - result:{1}", textBuff, (int)m.Result);
                            lastcachedText = cachedText;
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
                        if (cachedText == null)
                        {
                            cachedText = Text;
                            MapTextPositions();
                        }
                        m.Result = (IntPtr)cachedText.Length;
                    }
                    if (lastTextLength != cachedText.Length)
                    {
                        log.InfoFormat("GetTextLengh: {0}", cachedText.Length);
                        lastTextLength = cachedText.Length;
                    }
                    break;
                case 0xd6: //EM_POSFROMCHAR
                    m.Result = (IntPtr)(-1);
                    if ((int)m.WParam >= 0)
                    {
                        System.Drawing.Rectangle rectObj = new System.Drawing.Rectangle();
                        if (_currentContext != null && _currentContext.GetManager<IDragonAccessManager>() != null)
                        {
                            rectObj = _currentContext.GetManager<IDragonAccessManager>().PosFromChar((int)m.WParam);
                            m.Result = (IntPtr)(Convert.ToInt16(rectObj.X) + (Convert.ToInt16(rectObj.Y) << 16));
                        }
                        else
                        {
                            if (Document.Range.Length > (int)m.WParam)
                            {
                                int position = (int)m.WParam;
                                if (!mapSnapEditPos.TryGetValue(position, out position))
                                {
                                    log.InfoFormat("EM_POSFROMCHAR: Unable get mapSnapEditPos correct position req: {0}", (int)m.WParam);
                                    position = (int)m.WParam;
                                }
                                var docCharPos = Document.CreatePosition(position);
                                rectObj = GetBoundsFromPosition(docCharPos);
                                rectObj = DevExpress.Office.Utils.Units.DocumentsToPixels(rectObj, DpiX, DpiY);
                                
                                m.Result = (IntPtr)(Convert.ToInt16(rectObj.X) + (Convert.ToInt16(rectObj.Y) << 16));
                            }
                            else
                            {
                                m.Result = (IntPtr)(-1);
                            }
                        }
                        if (log.IsInfoEnabled)
                        {
                            log.InfoFormat("Position  C:{0},X:{1},Y:{2} ret:{3:X8}", (int)m.WParam, rectObj.X, rectObj.Y, (int)m.Result);
                        }
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
                        if (log.IsInfoEnabled)
                        {
                            log.InfoFormat("Char Pos from Point  C:{0},X:{1},Y:{2}, result{3}", CharPos, charPos.X, charPos.Y, (Convert.ToInt16(CharPos) + (Convert.ToInt16(0) << 16)));
                        }
                        m.Result = (IntPtr)(Convert.ToInt16(CharPos) + (Convert.ToInt16(0) << 16));

                    }
                    break;
                case 0xc9:  // EM_LINEFROMCHAR
                    m.Result = IntPtr.Zero;
                    if (_currentContext != null && _currentContext.GetManager<IDragonAccessManager>() != null)
                    {
                        textBuff = _currentContext.GetManager<IDragonAccessManager>().GetText();
                    }
                    else
                    {
                        textBuff = cachedText;
                    }
                    if (textBuff != null)
                    {
                        int position = (int)m.WParam;
                        if (!mapSnapEditPos.TryGetValue(position, out position))
                        {
                            log.InfoFormat("EM_LINEFROMCHAR: Unable get mapSnapEditPos correct position req: {0}", (int)m.WParam);
                            position = (int)m.WParam;
                        }
                        String[] lineparts = textBuff.Split('\n');
                        if (textBuff.Length < position)
                        {
                            m.Result = (IntPtr)(lineparts.Length - 1);
                        }
                        else if (lineparts.Length > 0)
                        {
                            int stringTotal = 0;
                            int lineCounter = lineparts.Count(sum =>
                            {
                                stringTotal += sum.Length + 1;
                                if (stringTotal < position)
                                {
                                    return true;
                                }
                                return false;
                            });

                            m.Result = (IntPtr)(lineCounter - 1);
                        }
                    }
                    break;
                case 0xc1:  // EM_LINELENGTH
                    m.Result = IntPtr.Zero;
                    if (_currentContext != null && _currentContext.GetManager<IDragonAccessManager>() != null)
                    {
                        textBuff = _currentContext.GetManager<IDragonAccessManager>().GetText();
                    }
                    else
                    {
                        textBuff = cachedText;
                    }
                    if (textBuff != null)
                    {
                        String[] lineparts = textBuff.Split('\n');
                        if (textBuff.Length < (int)m.WParam)
                        {
                            m.Result = (IntPtr)lineparts.LastOrDefault().Length;
                        }
                        else if (lineparts.Length > 0)
                        {
                            int charPos = (int)m.WParam;
                            int stringTotal = 0;
                            int lineCounter = lineparts.Count(sum =>
                            {
                                stringTotal += sum.Length + 1;
                                if (stringTotal < charPos)
                                {
                                    return true;
                                }
                                return false;
                            });

                            m.Result = (IntPtr)(lineparts[lineCounter].Length);
                        }
                    }
                    break;
                default:
                    //log.Info("Base Message processed: " + m.ToString());
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
                        log.Debug(string.Format("Replace selected text: '{0}', Undo:{1}", Marshal.PtrToStringAuto(m.LParam), (int)m.WParam));
                        break;
                    case 0xb0:
                        //log.Debug(string.Format("Get Selection: from:{0}, to:{1} - ret:{2:X}", Marshal.ReadInt16(m.WParam), Marshal.ReadInt16(m.LParam), (int)m.Result));
                        break;
                    case 0xb1:
                        log.Debug(string.Format("Set Selection : from:{0}, to:{1} - ret:{2}", (int)(m.LParam), (int)(m.WParam), (int)m.Result));
                        break;
                    default:
                        log.Debug("Base Message processed: " + m.ToString());
                        break;
                }
            }
        }

        private string CalculateCachedTextChanges(DocumentRange caretPos, string messageText)
        {
            string oldCachedText = cachedText;

            string begin = cachedText.Substring(0, caretPos.Start.ToInt());
            string end = cachedText.Substring(caretPos.Start.ToInt() + caretPos.Length);

            log.InfoFormat("Cached Text '{0}', begin with {1}, ends with {2}", cachedText, begin, end);

            log.InfoFormat("Update cached text on replacing: old cached '{0}', text to replace '{1}'  at {2} , result '{3}'", oldCachedText, messageText, caretPos.Start.ToInt(), begin + messageText + end);
            if (begin.Length > 0 && begin.Last() == ' ' && messageText.Length > 0 && messageText.First() == ' ')
            {
                log.InfoFormat("Correction '{0}'", messageText.Substring(1));
                return messageText.Substring(1);
            }
            
            log.InfoFormat("New text '{0}'", !String.IsNullOrEmpty(messageText) ? messageText.Substring(1) : "Empty String");
            
            return messageText;
        }

        private void ExtSnapControl_SelectionChanged(object sender, EventArgs e)
        {            
            log.Info("ExtSnapControl_SelectionChanged:");
            
            var docSelection = Document.Selection;
            int startPos = docSelection.Start.ToInt();
            int endPos = docSelection.End.ToInt();


            if ((startPos - endPos) >= cachedText.Length)
            {
                startPos = 0;
                endPos = cachedText.Length;
            }

  
            log.InfoFormat("ExtSnapControl Current Selection begin:{0}, end:{1}", startPos, endPos);
            lastselectionPair = new Tuple<int, int>(startPos, endPos);
            
        }

        private void ExtSnapControl_ContentChanged(object sender, EventArgs e)
        {
            //var textOpts = new DevExpress.XtraRichEdit.Export.PlainTextDocumentExporterOptions();
            //textOpts.ExportFinalParagraphMark = DevExpress.XtraRichEdit.Export.PlainText.ExportFinalParagraphMark.Always;
            cachedText = Text;
            MapTextPositions();
            log.InfoFormat("SnapControl_ContentChanged - retrieved text with option: '{0}'", cachedText);
        }

        private void MapTextPositions()
        {
            var dictEditPosData = new Dictionary<int, int>();
            var dictSnapPosData = new Dictionary<int, int>();
            String[] lineparts = cachedText.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            if (lineparts.Length > 0)
            {
                int sumTextEdit = 0;
                int sumTextSnap = 0;
                foreach (string lineText in lineparts)
                {

                    for (int charIdx = 0; charIdx < lineText.Length; charIdx++)
                    {
                        dictEditPosData[charIdx + sumTextEdit] = charIdx + sumTextSnap;
                        dictSnapPosData[charIdx + sumTextSnap] = charIdx + sumTextEdit;
                    }
                    dictEditPosData[lineText.Length + sumTextEdit] = lineText.Length + sumTextSnap;
                    dictEditPosData[lineText.Length + sumTextEdit + 1] = lineText.Length + sumTextSnap;
                    dictSnapPosData[lineText.Length + sumTextSnap] = lineText.Length + sumTextEdit;
                    sumTextEdit += lineText.Length + 2;
                    sumTextSnap += lineText.Length + 1;
                }
            }
            mapEditSnapPos = dictEditPosData;
            mapSnapEditPos = dictSnapPosData;

            //log.InfoFormat("Mapping Edit -> Snap {0}", mapEditSnapPos.AsEnumerable().Aggregate( new StringBuilder(), (x, y) =>
            //{
            //    x.Append(y).Append(" ");
            //    return x;
            //}).ToString());
            //log.InfoFormat("Mapping Snap -> Edit {0}", mapSnapEditPos.AsEnumerable().Aggregate(new StringBuilder(), (x, y) =>
            //{
            //    x.Append(y).Append(" ");
            //    return x;
            //}).ToString());
        }
    }
}

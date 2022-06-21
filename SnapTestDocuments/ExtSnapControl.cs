using DevExpress.XtraRichEdit.API.Native;
using DevExpress.XtraRichEdit.SpellChecker;
using DevExpress.XtraSpellChecker.Native;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text;
using DevExpress.XtraRichEdit.Commands;
using System.Drawing;

namespace SnapTestDocuments
{

    //public class QString
    //{
    //    public String Text { get; set; }
    //    public QString(string member)
    //    {
    //        Text = member;
    //    }

    //    public override string ToString()
    //    {

    //    }
    //}

    public class ExtSnapControl : DevExpress.Snap.SnapControl
    {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);
        private ITextEditWinFormsUIContext _currentContext;
        DragonDictationHelper dictationHelper = new DragonDictationHelper();
        private Tuple<int, int> requestSelectPair = new Tuple<int, int>(-1, -1);  // start, end in snap positions
        private Tuple<int, int> lastSelectionPair = new Tuple<int, int>(-1, -1);  // start, end in snap positions
        private string cachedText;
        private string lastcachedText;
        private bool fieldTextChanged = false;
        private Tuple<bool, Field> adjustField = new Tuple<bool, Field>(false, null);
        private int lastTextLength = -1;
        private static int charSize = Marshal.SizeOf(typeof(tagChar));
        IntPtr hOldFont = IntPtr.Zero;
        IntPtr oldRectPtr = IntPtr.Zero;
        System.Drawing.Rectangle rectData = System.Drawing.Rectangle.Empty;
        [System.Runtime.InteropServices.DllImport("GDI32.dll")]
        public static extern bool DeleteObject(IntPtr objectHandle);
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct tagChar
        {
            char charVal;
        }
        [StructLayout(LayoutKind.Sequential)]
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

        public ITextEditWinFormsUIContext SetContext
        {
            set
            {
                _currentContext = value;
            }
        }

        private void ReplaceText(string messageText)
        {
            var caretPos = Document.Selection;
            bool fieldProcessed = false;
            if (true == _currentContext.GetManager<ISelectionChangedTrackingManager>()?.LastSelectionInfo.IsCaretPosInField)
            {
                var fieldCheck = DragonDictationHelper.GetFieldFromPosInRange(Document.CaretPosition.ToInt(), Document, Document.Range);
                var fieldUpdated = dictationHelper.HandleWorkFieldSelection(_currentContext, fieldCheck, messageText);
                if (fieldUpdated != null)
                {
                    fieldTextChanged = true;
                    fieldProcessed = true;
                    adjustField = new Tuple<bool, Field>(false, fieldUpdated);
                }
                caretPos = Document.Selection;
            }
            if (!fieldProcessed)
            {

                if (lastSelectionPair != requestSelectPair && requestSelectPair.Item1 == requestSelectPair.Item2)
                {
                    caretPos = Document.CreateRange(requestSelectPair.Item1, Math.Abs(requestSelectPair.Item2 - requestSelectPair.Item1));
                }
                string textToReplace = cachedText;
                if (caretPos.End.ToInt() <= cachedText.Length)
                {
                    textToReplace = cachedText.Substring(caretPos.Start.ToInt(), caretPos.End.ToInt() - caretPos.Start.ToInt());
                }
                log.InfoFormat("Replace selected text: '{0}', with  '{1}' on text '{2}'", textToReplace, messageText, cachedText);
                if (!string.IsNullOrEmpty(textToReplace) && !string.IsNullOrEmpty(messageText))
                {
                    if (char.IsWhiteSpace(textToReplace[0]) != char.IsWhiteSpace(messageText[0]))
                    {
                        caretPos = Document.CreateRange(Document.CreatePosition(caretPos.Start.ToInt()), caretPos.Length);
                    }
                }
                SubDocument docFragment = caretPos.BeginUpdateDocument();
                try
                {
                    docFragment.BeginUpdate();
                    docFragment.Replace(caretPos, messageText);
                }
                finally
                {
                    docFragment.EndUpdate();
                    caretPos.EndUpdateDocument(docFragment);
                    ExtSnapControl_ContentChanged(this, EventArgs.Empty);
                    Document.CaretPosition = caretPos.End;
                    lastSelectionPair = new Tuple<int, int>(caretPos.End.ToInt(), caretPos.End.ToInt());
                    log.InfoFormat("Replaced text: {1} last selection is:{0}", lastSelectionPair, messageText);
                }
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
                minPos = dictationHelper.EditToSnap(minPos);
                maxPos = dictationHelper.EditToSnap(maxPos);
                log.InfoFormat("Mapped setsel orig:{0},{1}, maps{2},{3}", wparam, lparam, minPos, maxPos);
                if (maxPos == minPos)
                {
                    bool adjustedField = false;
                    if (adjustField.Item1 && adjustField.Item2 != null)
                    {
                        var cursorPos = dictationHelper.SnapToEdit(minPos);
                        var fieldEndPos = dictationHelper.SnapToEdit(adjustField.Item2.Range.End.ToInt());
                        if (cursorPos - fieldEndPos == 0 || cursorPos - fieldEndPos == 1)
                        {
                            this.Document.CaretPosition = Document.CreatePosition(adjustField.Item2.ResultRange.End.ToInt());
                            adjustedField = true;
                            adjustField = new Tuple<bool, Field>(false, null);
                        }
                    }
                    if (!adjustedField && !requestSelectPair.Equals(new Tuple<int, int>(minPos, maxPos)))
                    {
                        Document.BeginUpdate();
                        var docCharPos = Document.CreatePosition(minPos);
                        var rectObj = GetLayoutPhysicalBoundsFromPosition(docCharPos);
                        var viewBounds = ViewBounds;
                        viewBounds.Inflate((int)Math.Floor(-viewBounds.Width * 0.05f), (int)Math.Floor(-viewBounds.Height * 0.05f));
                        rectObj.Offset(viewBounds.X, viewBounds.Y);
                        Document.CaretPosition = docCharPos;
                        log.InfoFormat("Client Rect : {0} View Rect: {1}", ClientRectangle, ViewBounds);
                        if (rectObj == Rectangle.Empty || !viewBounds.Contains(rectObj.Left, rectObj.Top) || !viewBounds.Contains(rectObj.Right, rectObj.Bottom))
                        {
                            ScrollToCaret(0.3f);
                        }
                        Document.EndUpdate();

                        //Document.CaretPosition = Document.CreatePosition(minPos);
                    }
                }
                else
                {
                    if (!requestSelectPair.Equals(new Tuple<int, int>(minPos, maxPos)))
                    {
                        Document.BeginUpdate();
                        var docCharPos = Document.CreatePosition(minPos);
                        var rectObj = GetLayoutPhysicalBoundsFromPosition(docCharPos);
                        var viewBounds = ViewBounds;
                        viewBounds.Inflate((int)Math.Floor(-viewBounds.Width * 0.05f), (int)Math.Floor(-viewBounds.Height * 0.05f));
                        rectObj.Offset(viewBounds.X, viewBounds.Y);
                        Document.CaretPosition = docCharPos;
                        log.InfoFormat("Client Rect : {0} View Rect: {1}", ClientRectangle, ViewBounds);
                        if (rectObj == Rectangle.Empty || !viewBounds.Contains(rectObj.Left, rectObj.Top) || !viewBounds.Contains(rectObj.Right, rectObj.Bottom))
                        {
                            Document.CaretPosition = docCharPos;
                            ScrollToCaret(0.3f);
                        }
                        Document.Selection = Document.CreateRange(minPos, maxPos - minPos);
                        Document.EndUpdate();
                    }
                }
            }
            log.DebugFormat("SetSel returned minPos:{0}, maxPos:{1}", minPos, maxPos);
           

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
                    if (_currentContext?.GetManager<IDragonAccessManager>() != null)
                    {
                        _currentContext.GetManager<IDragonAccessManager>().ReplaceText(messageText);
                    }
                    else
                    {
                        ReplaceText(messageText);
                    }
                    break;
                case 176: // EM_GETSEL - returns current selection merked in document
                    {
                        Tuple<int, int> lastCaretPos = null;

                        if (_currentContext?.GetManager<IDragonAccessManager>() != null)
                        {
                            lastCaretPos = _currentContext.GetManager<IDragonAccessManager>().GetSel();
                        }
                        else
                        {
                            lastCaretPos = new Tuple<int, int>(dictationHelper.SnapToEdit(lastSelectionPair.Item1), dictationHelper.SnapToEdit(lastSelectionPair.Item2));
                        }

                        if (m.WParam != IntPtr.Zero)
                        {
                            Marshal.WriteInt32(m.WParam, Convert.ToInt32(lastCaretPos.Item1));
                        }
                        if (m.LParam != IntPtr.Zero)
                        {
                            Marshal.WriteInt32(m.LParam, Convert.ToInt32(lastCaretPos.Item2));
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
                        log.InfoFormat("Get Selection: from:{0}, to:{1} - ret:{2:X8}", Marshal.ReadInt32(m.WParam), Marshal.ReadInt32(m.LParam), (int)m.Result);
                    }
                    break;
                case 177: // EM_SETSEL - sets Selection marker by positions provided in m.Wparam i m.LParam - store previous selection, 
                          // because Dragon sets cursor after previous selected range just before replacing text
                    {
                        if (IntPtr.Size == 8)
                        {
                            if ((long)m.LParam > int.MaxValue || (long)m.LParam < int.MinValue)
                            {
                                m.LParam = IntPtr.Zero;
                            }
                            if ((long)m.WParam > int.MaxValue || (long)m.WParam < int.MinValue)
                            {
                                m.WParam = IntPtr.Zero;
                            }
                        }
                        if (_currentContext?.GetManager<IDragonAccessManager>() != null)
                        {
                            requestSelectPair = _currentContext.GetManager<IDragonAccessManager>().SetSel((int)m.WParam, (int)m.LParam);
                        }
                        else
                        {
                            requestSelectPair = SetSelect((int)m.WParam, (int)m.LParam);
                        }

                       

                        m.Result = (IntPtr)1;
                        log.InfoFormat("Updated Selection:  old: {1} new: {0} - result:{2}", requestSelectPair, new Tuple<int, int>((int)m.WParam, (int)m.LParam), m.Result);
                    }
                    break;
                case 135: // WM_GETDLGCODE
                    m.Result = (IntPtr)0x89;
                    break;
                case 0xb8: // EM_GETMODIFY
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
                    if (_currentContext?.GetManager<IDragonAccessManager>() != null)
                    {
                        textBuff = _currentContext.GetManager<IDragonAccessManager>().GetText();
                    }
                    else
                    {
                        textBuff = cachedText;
                    }
                    if (textBuff != null)
                    {
                        if (m.WParam.ToInt32() > 0)
                        {
                            if (textBuff.Length >= m.WParam.ToInt32())
                            {
                                Marshal.Copy(textBuff.ToCharArray(), 0, m.LParam, m.WParam.ToInt32() - 1);
                                Marshal.WriteByte(m.LParam, charSize * m.WParam.ToInt32(), 0);
                                m.Result = (IntPtr)(m.WParam.ToInt32() - 1);
                            }
                            else
                            {
                                Marshal.Copy(textBuff.ToCharArray(), 0, m.LParam, textBuff.Length);
                                Marshal.WriteInt16(m.LParam, textBuff.Length * charSize, 0);
                                m.Result = (IntPtr)(textBuff.Length);
                            }
                        }
                        else
                        {
                            m.Result = IntPtr.Zero;
                        }
                        if (!string.Equals(lastcachedText, cachedText))
                        {
                            log.InfoFormat("GetText reported:  text: {0} - result:{1} and buffer size {2}", textBuff, (int)m.Result, (int)m.WParam);
                            lastcachedText = cachedText;
                        }
                    }
                    else
                    {
                        m.Result = IntPtr.Zero;
                    }
                    break;
                case 14:  // WM_GETEXTLEN - returne all text in control - this by default 
                    if (_currentContext?.GetManager<IDragonAccessManager>() != null)
                    {
                        m.Result = (IntPtr)_currentContext.GetManager<IDragonAccessManager>().GetTextLen();
                    }
                    else
                    {
                        if (cachedText == null)
                        {
                            cachedText = Text;
                            dictationHelper.MapTextPositions(cachedText);
                        }
                        m.Result = (IntPtr)cachedText.Length;
                    }
                    if (cachedText != null && lastTextLength != cachedText.Length)
                    {
                        log.InfoFormat("GetTextLengh: {0}", cachedText.Length);
                        lastTextLength = cachedText.Length;
                    }
                    break;
                case 0xd6: // EM_POSFROMCHAR
                    m.Result = (IntPtr)(-1);
                    if ((int)m.WParam >= 0)
                    {
                        System.Drawing.Rectangle rectObj = new System.Drawing.Rectangle();

                        if (_currentContext?.GetManager<IDragonAccessManager>() != null)
                        {
                            rectObj = _currentContext.GetManager<IDragonAccessManager>().PosFromChar((int)m.WParam);
                            if (log.IsInfoEnabled)
                            {
                                log.InfoFormat("Position  C:{0},mapped:{4},X:{1},Y:{2} ret:{3:X8}", (int)m.WParam, rectObj.X, rectObj.Y, (int)m.Result, dictationHelper.EditToSnap((int)m.WParam));
                            }
                            if (rectObj.IsEmpty)
                            {
                                m.Result = (IntPtr)(-1);
                            }
                            else
                            {
                                m.Result = (IntPtr)(Convert.ToInt16(rectObj.X) + (Convert.ToInt16(rectObj.Y) << 16));
                            }
                        }
                        else
                        {
                            int charPosition = dictationHelper.EditToSnap((int)m.WParam);
                            if (Document.Range.Length > charPosition)
                            {
                                var docCharPos = Document.CreatePosition(charPosition);
                                rectObj = GetBoundsFromPosition(docCharPos);
                                rectObj = DevExpress.Office.Utils.Units.DocumentsToPixels(rectObj, DpiX, DpiY);

                                m.Result = (IntPtr)(Convert.ToInt16(rectObj.X) + (Convert.ToInt16(rectObj.Y) << 16));
                            }
                            else
                            {
                                m.Result = (IntPtr)(-1);
                            }
                            if (log.IsInfoEnabled)
                            {
                                log.InfoFormat("Position  C:{0},mapped:{4},X:{1},Y:{2} ret:{3:X8}", (int)m.WParam, rectObj.X, rectObj.Y, (int)m.Result, charPosition);
                            }

                        }
                    }
                    break;
                case 0xd7: // EM_CHARFROMPOS
                    if ((int)m.LParam != 0)
                    {
                        int CharParam = (int)m.LParam;
                        int CharPos = 0;
                        System.Drawing.PointF charPos = new System.Drawing.PointF((float)(CharParam & 0xFFFF0000), (float)(CharParam & 0x0000FFFF));
                        if (_currentContext?.GetManager<IDragonAccessManager>() != null)
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
                    int position = (int)m.WParam;
                    if (_currentContext?.GetManager<IDragonAccessManager>() != null)
                    {
                        textBuff = _currentContext.GetManager<IDragonAccessManager>().GetText();
                    }
                    else
                    {
                        textBuff = cachedText;
                    }
                    if (textBuff != null)
                    {
                        m.Result = (IntPtr)DragonDictationHelper.getLineFromText(textBuff, position);
                        if (log.IsInfoEnabled)
                        {
                            log.InfoFormat("Line Pos from Point  C:{0},Line:{1}", position, (int)m.Result);
                        }
                    }
                    break;
                case 0xbb: // EM_LINEINDEX
                    {
                        m.Result = (IntPtr)(-1);
                        int lineIndex = (int)m.WParam;
                        if (_currentContext?.GetManager<IDragonAccessManager>() != null)
                        {
                            textBuff = _currentContext.GetManager<IDragonAccessManager>().GetText();
                        }
                        else
                        {
                            textBuff = cachedText;
                        }
                        if (textBuff != null)
                        {
                            m.Result = (IntPtr)DragonDictationHelper.getCharLineIndex(textBuff, lineIndex);
                            if (log.IsInfoEnabled)
                            {
                                log.InfoFormat("Line Pos from Point  C:{0},Line:{1}", lineIndex, (int)m.Result);
                            }
                        }
                    }
                    break;
                case 0xc1:  // EM_LINELENGTH
                    m.Result = IntPtr.Zero;
                    if (_currentContext?.GetManager<IDragonAccessManager>() != null)
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
                    log.Debug("Base Message processed: " + m.ToString());
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
                        log.Debug(string.Format("Get Selection: from:{0}, to:{1} - ret:{2:X}", Marshal.ReadInt16(m.WParam), Marshal.ReadInt16(m.LParam), (int)m.Result));
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

        private void ExtSnapControl_SelectionChanged(object sender, EventArgs e)
        {
            log.Info("ExtSnapControl_SelectionChanged:");
            if (Document != null)
            {
                var docSelection = Document.Selection;
                int startPos = docSelection.Start.ToInt();
                int endPos = docSelection.End.ToInt();

                if (cachedText != null)
                {
                    if ((endPos - startPos) >= cachedText.Length)
                    {
                        startPos = 0;
                        endPos = cachedText.Length;
                    }
                }

                log.InfoFormat("ExtSnapControl Current Selection begin:{0}, end:{1}", startPos, endPos);
                lastSelectionPair = new Tuple<int, int>(startPos, endPos);
            }
        }

        private void ExtSnapControl_ContentChanged(object sender, EventArgs e)
        {
            //var textOpts = new DevExpress.XtraRichEdit.Export.PlainTextDocumentExporterOptions();
            //textOpts.ExportFinalParagraphMark = DevExpress.XtraRichEdit.Export.PlainText.ExportFinalParagraphMark.Always;
            if (_currentContext?.GetManager<IDragonAccessManager>() == null)
            {
                cachedText = Text;
                if (fieldTextChanged)
                {
                    adjustField = new Tuple<bool, Field>(true, adjustField.Item2);
                }
                else
                {
                    adjustField = new Tuple<bool, Field>(false, null);
                }
                if (Document.Fields?.Count > 0)
                {
                    var fieldRange = Document.Fields.ToList();
                    fieldRange.Sort((field1, field2) => field1.ResultRange.Start.ToInt().CompareTo(field2.ResultRange.Start.ToInt()));
                    var allEntities = new LinkedList<Field>(fieldRange);

                    var stringParts = new List<Tuple<string, int>>();
                    int initialRange = Document.Range.Start.ToInt();
                    var fieldData = allEntities.First;
                    while(fieldData.Next != null)
                    {
                        stringParts.Add(new Tuple<string, int>(Document.GetText(Document.CreateRange(initialRange, fieldData.Value.Range.Start.ToInt() - initialRange)), fieldData.Value.CodeRange.Length + 2));

                        if (fieldData.Next.Value.Range.Start.ToInt() < fieldData.Value.ResultRange.End.ToInt())
                        {
                            // text in field to nested field
                            stringParts.Add(new Tuple<string, int>(Document.GetText(Document.CreateRange(fieldData.Value.ResultRange.Start.ToInt(),
                                fieldData.Next.Value.ResultRange.Start.ToInt() - fieldData.Value.ResultRange.Start.ToInt())), 1));
                            initialRange = fieldData.Next.Value.Range.Start.ToInt();
                        }
                        else
                        {
                            // whole text in field
                            stringParts.Add(new Tuple<string, int>(Document.GetText(fieldData.Value.ResultRange), 1));
                            initialRange = fieldData.Value.Range.End.ToInt();
                        }
                        fieldData = fieldData.Next;
                    }
                    if (fieldData.Value.Range.Start.ToInt() > initialRange)
                    {
                        stringParts.Add(new Tuple<string, int>(Document.GetText(Document.CreateRange(initialRange, fieldData.Value.Range.Start.ToInt() - initialRange)), fieldData.Value.CodeRange.Length + 2));
                        stringParts.Add(new Tuple<string, int>(Document.GetText(fieldData.Value.ResultRange), 1));
                        initialRange = fieldData.Value.Range.End.ToInt();
                    }
                    stringParts.Add(new Tuple<string, int>((Document.GetText(Document.CreateRange(initialRange, Document.Range.End.ToInt() - initialRange))), 0));
                    dictationHelper.MapTextPositions(stringParts);

                    if (Document.Fields?.Count > 0)
                    {
                        var stringPartsExt = new List<Tuple<string, int>>();
                        int initialRangeExt = Document.Range.Start.ToInt();
                        foreach (var fieldDataExt in Document.Fields)
                        {
                            if (fieldDataExt.Range.Start.ToInt() > initialRangeExt)
                            {
                                stringPartsExt.Add(new Tuple<string, int>(Document.GetText(Document.CreateRange(initialRangeExt, fieldDataExt.Range.Start.ToInt() - initialRangeExt)), fieldDataExt.CodeRange.Length + 2));
                                stringPartsExt.Add(new Tuple<string, int>(Document.GetText(fieldDataExt.ResultRange), 1));
                                initialRangeExt = fieldDataExt.Range.End.ToInt();
                            }
                        }
                        stringPartsExt.Add(new Tuple<string, int>((Document.GetText(Document.CreateRange(initialRangeExt, Document.Range.End.ToInt() - initialRangeExt))), 0));

                        dictationHelper.MapTextPositions(stringPartsExt);

                    }

                }
                    else
                {
                    dictationHelper.MapTextPositions(cachedText);
                }
                log.InfoFormat("SnapControl_ContentChanged - retrieved text with option: '{0}'", cachedText);



                
            }
        }


    }
}

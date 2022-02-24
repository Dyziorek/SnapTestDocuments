using DevExpress.XtraRichEdit.API.Native;
using System;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace SnapTestDocuments
{
    public class DragonAccessManagerCmn : IDragonAccessManager
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger("DragonAccessManagerCmn");
        private DocumentEntityBase currentSelectedInterp = null;
        private ITextFieldInfo currentSectionField = null;
        int currentSectionOffset = 0;
        private Tuple<int, int> lastselectionPair = new Tuple<int, int>(0, 0); //start, end
        private Tuple<int, int> requestSelectPair = new Tuple<int, int>(0, 0);
        private string cacheStringText = string.Empty;

        protected ISnapCtrlContext snapCtrlCtx;
        protected DevExpress.Snap.SnapControl SnapCtrl { get { return snapCtrlCtx.SnapControl; } }

        public DragonAccessManagerCmn(ISnapCtrlContext snapCtrlCtx)
        {
            this.snapCtrlCtx = snapCtrlCtx;
        }
        private Tuple<int, Rectangle> lastPosChar = new Tuple<int, Rectangle>(-1, Rectangle.Empty);

        public void Clear()
        {
            currentSelectedInterp = null;
            snapCtrlCtx.SnapControl.ContentChanged -= SnapControl_ContentChanged;
            snapCtrlCtx.SnapControl.SelectionChanged -= SnapControl_SelectionChanged;
        }

        public Tuple<int, int> GetSel()
        {
            if (this.currentSelectedInterp != null)
            {
                log.InfoFormat("DragAccMgrCmn  GetSel returns p:{0}, l:{1}", lastselectionPair.Item1, lastselectionPair.Item2);
                return lastselectionPair;
            }
            log.InfoFormat("DragAccMgrCmn GetSel returns zero p:{0}, l:{1}", 0, 0);
            return new Tuple<int, int>(0, 0);

        }

        public string GetText()
        {
            log.InfoFormat("DragAccMgrCmn GetText:'{0}'", cacheStringText);
            return cacheStringText;
        }

        public int GetTextLen()
        {
            log.Debug("GetTextLen:");
            string textResult = GetText();
            if (!string.IsNullOrEmpty(textResult))
            {
                log.InfoFormat("DragAccMgrCmn TextLen:{0}", textResult.Length);
                return textResult.Length;
            }

            return 0;
        }

        public void Init()
        {
            currentSelectedInterp = null;
            this.snapCtrlCtx.SnapControl.ContentChanged -= SnapControl_ContentChanged;
            this.snapCtrlCtx.SnapControl.ContentChanged += SnapControl_ContentChanged;
            this.snapCtrlCtx.SnapControl.SelectionChanged -= SnapControl_SelectionChanged;
            this.snapCtrlCtx.SnapControl.SelectionChanged += SnapControl_SelectionChanged;
        }

        private void SnapControl_SelectionChanged(object sender, EventArgs e)
        {
            log.Info("SnapControl_SelectionChanged:");
            if (this.currentSelectedInterp != null)
            {
                if (currentSectionField != null && SnapFieldTools.IsValidField(currentSectionField))
                {
                    var docSelection = this.SnapCtrl.Document.Selection;
                    int startPos = docSelection.Start.ToInt();
                    int endPos = docSelection.End.ToInt();


                    int currentSectionLength = currentSectionField.ResultRange.Length;

                    int minPos = Math.Min(startPos - currentSectionOffset, endPos - currentSectionOffset);
                    int maxPos = Math.Max(startPos - currentSectionOffset, endPos - currentSectionOffset);
                    if ((maxPos - minPos) >= currentSectionLength)
                    {
                        minPos = 0;
                        maxPos = currentSectionLength;
                    }

                    log.InfoFormat("DragAccMgrCmn Current Selection begin:{0}, end:{1}", minPos, maxPos);
                    lastselectionPair = new Tuple<int, int>(minPos, maxPos);
                }
                else
                {
                    this.currentSelectedInterp = null;
                }
            }
        }

        private void SnapControl_ContentChanged(object sender, EventArgs e)
        {
            log.Debug("SnapControl_ContentChanged:");
            if (this.currentSelectedInterp != null)
            {
                if (currentSectionField != null && SnapFieldTools.IsValidField(currentSectionField.Field))
                {
                    var textOpts = new DevExpress.XtraRichEdit.Export.PlainTextDocumentExporterOptions();
                    textOpts.ExportFinalParagraphMark = DevExpress.XtraRichEdit.Export.PlainText.ExportFinalParagraphMark.Always;
                    cacheStringText = this.SnapCtrl.Document.GetText(currentSectionField.Field.ToSnap().ResultRange, textOpts);
                    log.InfoFormat("SnapControl_ContentChanged - retrieved text with option: '{0}'", cacheStringText);
                    cacheStringText = cacheStringText.Replace("\r", string.Empty);
                }
                else
                {
                    currentSelectedInterp = null;
                    cacheStringText = string.Empty;
                }
            }
            else
                cacheStringText = string.Empty;

            if (lastPosChar.Item1 != -1)
            {
                lastPosChar = new Tuple<int, Rectangle>(-1, Rectangle.Empty);
            }
            log.InfoFormat("SnapControl_ContentChanged - updated text: '{0}'", cacheStringText);
        }

        public bool HasSections()
        {
            log.Debug("HasSections:");
            return this.currentSelectedInterp != null;
        }
        public void UpdateSelectedItem(DocumentEntityBase selectedItem)
        {
            log.DebugFormat("UpdateSelectedItem: {0}", selectedItem != null);
            if (selectedItem?.Type == DocumentEntityTypes.InterpretationSection)
            {
                Field sectionField = snapCtrlCtx.GetManager<IInterpSectionsManager>().GetSectionField(selectedItem);
                if (sectionField != null)
                {
                    currentSectionOffset = sectionField.ResultRange.Start.ToInt();
                    cacheStringText = SnapCtrl.Document.GetText(sectionField.ResultRange);
                    this.currentSectionField = sectionField.GetTextFieldInfo(SnapCtrl.Document); 
                    this.currentSelectedInterp = selectedItem;
                }
                else
                {
                    this.currentSelectedInterp = null;
                    this.lastselectionPair = new Tuple<int, int>(0, 0);
                    this.currentSectionField = null;
                    currentSectionOffset = 0;
                }
            }
            else
            {
                this.currentSelectedInterp = null;
                this.lastselectionPair = new Tuple<int, int>(0, 0);
                this.currentSectionField = null;
                currentSectionOffset = 0;
            }
        }
        public void ReplaceText(string text)
        {
            lock (this)
            {
                log.DebugFormat("ReplaceText with '{0}'", text);

                {
                    if (this.currentSelectedInterp != null && this.currentSectionField != null && SnapFieldTools.IsValidField(currentSectionField))
                    {
                        var selection = this.SnapCtrl.Document.Selection;
                        if (lastselectionPair != requestSelectPair && requestSelectPair.Item1 == requestSelectPair.Item2 && requestSelectPair.Item1 + currentSectionOffset + 1 == selection.Start.ToInt())
                        {
                            // Dragon resets selected word to single cursor after selected word just before replacing this with new word
                            // so we need to restore selection before replacing
                            if (log.IsDebugEnabled)
                            {
                                log.DebugFormat("Use cached selection: current sel: {0} , cache sel:{1}, snap selection pos {3}", requestSelectPair, lastselectionPair, selection.Start.ToInt());
                            }
                            selection = this.SnapCtrl.Document.CreateRange(lastselectionPair.Item1 + currentSectionOffset, Math.Abs(lastselectionPair.Item2 - lastselectionPair.Item1));
                        }

                        if (selection.Start.ToInt() > currentSectionField.Field.ToSnap().ResultRange.End.ToInt())
                        {
                            selection = this.SnapCtrl.Document.CreateRange(currentSectionField.Field.ToSnap().ResultRange.End.ToInt(), 0);
                        }
                        else if (selection.End.ToInt() < currentSectionField.Field.ToSnap().ResultRange.Start.ToInt())
                        {
                            selection = this.SnapCtrl.Document.CreateRange(currentSectionField.Field.ToSnap().ResultRange.Start.ToInt(), 0);
                        }
                        else
                        {
                            log.InfoFormat("DragAccMgrCmn Replace Text selected: {0} size:{1}", selection.Start.ToInt() - currentSectionOffset, selection.Length);
                        }
                        if (SnapRangePermissionsTools.IsDocumentRangeEditableRange(SnapCtrl.Document, selection))
                        {
                            if (selection.End == currentSectionField.Field.ToSnap().ResultRange.End)
                            {
                                if (cacheStringText.Length > 0 && !Char.IsWhiteSpace(cacheStringText[cacheStringText.Length - 1]) && text.Length > 0 ? !Char.IsWhiteSpace(text[0]) : false)
                                {
                                    log.InfoFormat("DragAccMgrCmn Adding space existing text: '{0}', addedText '{1}', lastCharWhiteSpace:{2}, first:{3}", cacheStringText, text, !Char.IsWhiteSpace(cacheStringText[cacheStringText.Length - 1]), !Char.IsWhiteSpace(text[0]));
                                    text = " " + text;
                                }
                            }
                            SubDocument docFragment = selection.BeginUpdateDocument();
                            try
                            {
                                docFragment.BeginUpdate();
                                text = CalculateCachedTextChanges(selection, text);
                                log.InfoFormat("DragAccMgrCmn Replace final Text:'{0}'", text);
                                docFragment.Replace(selection, text);
                                //currentSectionField.Field.ToSnap().Update();
                            }
                            finally
                            {
                                docFragment.EndUpdate();
                                selection.EndUpdateDocument(docFragment);
                                

                                if (selection.End <= currentSectionField.Field.ToSnap().ResultRange.End)
                                {
                                    this.SnapCtrl.Document.CaretPosition = selection.End;
                                    lastselectionPair = new Tuple<int, int>(selection.End.ToInt() - currentSectionOffset, selection.End.ToInt() - currentSectionOffset);
                                }
                                else
                                {
                                    this.SnapCtrl.Document.CaretPosition = currentSectionField.Field.ToSnap().ResultRange.End;
                                    lastselectionPair = new Tuple<int, int>(currentSectionField.Field.ToSnap().ResultRange.End.ToInt() - currentSectionOffset, currentSectionField.Field.ToSnap().ResultRange.End.ToInt() - currentSectionOffset);
                                }

                                cacheStringText = this.SnapCtrl.Document.GetText(currentSectionField.Field.ToSnap().ResultRange);
                                log.InfoFormat("DragAccMgrCmn Whole Section Text after replace:'{0}'", cacheStringText);
                            }

                        }
                    }
                    else if (currentSelectedInterp == null)
                    {
                        log.Info("Replace Text: no section selected!");
                    }
                }
            }
        }

        public void Reset()
        {
            this.currentSelectedInterp = null;
        }

        public Tuple<int, int> SetSel(int start, int end)
        {

            log.DebugFormat("SetSel: {0}, {1}", start, end);
            if (this.currentSelectedInterp != null)
            {
                if (currentSectionField != null && SnapFieldTools.IsValidField(currentSectionField))
                {
                    int currentSectionLength = currentSectionField.ResultRange.Length;

                    var selection = this.SnapCtrl.Document.Selection;
                    this.lastselectionPair = new Tuple<int, int>(selection.Start.ToInt() - currentSectionOffset, selection.End.ToInt() - currentSectionOffset);
                    int minPos = Math.Min(currentSectionOffset + start, currentSectionOffset + end);
                    int maxPos = Math.Max(currentSectionOffset + start, currentSectionOffset + end);
                    if ((maxPos - minPos) > currentSectionLength)
                    {
                        minPos = currentSectionOffset;
                        maxPos = currentSectionOffset + currentSectionLength - 1;
                    }
                    if ((maxPos - minPos) == 0)
                    {
                        this.SnapCtrl.Document.CaretPosition = this.SnapCtrl.Document.CreatePosition(minPos + 1);
                    }
                    else
                    {
                        this.SnapCtrl.Document.Selection = this.SnapCtrl.Document.CreateRange(minPos, maxPos - minPos);
                    }
                    log.InfoFormat("DragAccMgrCmn  Set Sel at:{0} with {1}", minPos - currentSectionOffset, maxPos - minPos);
                    requestSelectPair =new Tuple<int, int>(minPos - currentSectionOffset, maxPos - currentSectionOffset);
                    return requestSelectPair;
                }
                else
                {
                    this.currentSelectedInterp = null;
                }
            }
            log.Info("Ignored Set Sel Comamnd");
            return new Tuple<int, int>(0, 0);

        }

        public Rectangle PosFromChar(int charPos)
        {
            log.DebugFormat("PosFromChar: for charpos {0}", charPos);
            if (this.currentSelectedInterp != null)
            {
                if (currentSectionField != null && SnapFieldTools.IsValidField(currentSectionField))
                {
                    int currentSectionOffset = currentSectionField.Field.ToSnap().ResultRange.Start.ToInt();
                    int currentSectionLength = currentSectionField.ResultRange.Length;

                    int minPos = Math.Min(currentSectionOffset + charPos, currentSectionOffset + currentSectionLength);

                    if (minPos == lastPosChar.Item1)
                    {
                        return lastPosChar.Item2;
                    }
                    lastPosChar = new Tuple<int, Rectangle>(minPos, snapCtrlCtx.SnapControl.GetBoundsFromPosition(snapCtrlCtx.SnapDocument.CreatePosition(minPos)));
                    return lastPosChar.Item2;
                }
                else
                {
                    this.currentSelectedInterp = null;
                    log.InfoFormat("DragAccMgrCmn Ignored Set Sel Comamnd reason: no section field");
                }
            }
            else
            {
                log.InfoFormat("DragAccMgrCmn Ignored Set Sel Comamnd reason: unselected section");
            }
            return new Rectangle();
        }

        public int CharFromPos(PointF charPos)
        {
            log.DebugFormat("CharFromPos: from {0}", charPos);
            if (this.currentSelectedInterp != null)
            {
                if (currentSectionField != null && SnapFieldTools.IsValidField(currentSectionField))
                {
                    int currentSectionOffset = currentSectionField.Field.ToSnap().ResultRange.Start.ToInt();
                    int currentSectionLength = currentSectionField.ResultRange.Length;
                    int minPos = Math.Min(snapCtrlCtx.SnapControl.GetPositionFromPoint(charPos).ToInt(), currentSectionOffset + currentSectionLength);
                    return Math.Max(0, minPos - currentSectionOffset);
                }
            }
            return -1;
        }

        private string CalculateCachedTextChanges(DocumentRange caretPos, string messageText)
        {
            string oldCachedText = cacheStringText;

            int changeTextPos = caretPos.Start.ToInt() - currentSectionOffset;

            if (changeTextPos > 0 && changeTextPos < cacheStringText.Length)
            {
                string begin = cacheStringText.Substring(0, changeTextPos);
                string end = cacheStringText.Substring(changeTextPos + caretPos.Length);

                log.InfoFormat("Cached Text '{0}', begin with '{1}', ends with '{2}'", cacheStringText, begin, end);

                log.InfoFormat("Update cached text on replacing: old cached '{0}', text to replace '{1}'  at {2} , result '{3}'", oldCachedText, messageText, caretPos.Start.ToInt(), begin + messageText + end);
                if (begin.Length > 0 && begin.Last() == ' ' && messageText.Length > 0 && messageText.First() == ' ')
                {
                    log.InfoFormat("Correction '{0}'", messageText.Substring(1));
                    return messageText.Substring(1);
                }
                log.InfoFormat("New text '{0}'", messageText.Length > 0 ? messageText.Substring(1) : messageText);
            }
            else
            {
                log.InfoFormat("Unable to correct text '{0}' based on cache", messageText);
                log.InfoFormat("Cache text '{0}', text size:{1}, postion to verify:{2}", cacheStringText, cacheStringText.Length, changeTextPos);
            }
            return messageText;
        }

        public void TurnOn()
        {
            TurnOff();
            this.snapCtrlCtx.SnapControl.ContentChanged += SnapControl_ContentChanged;
            this.snapCtrlCtx.SnapControl.SelectionChanged += SnapControl_SelectionChanged;
        }

        public void TurnOff()
        {
            this.snapCtrlCtx.SnapControl.ContentChanged -= SnapControl_ContentChanged;
            this.snapCtrlCtx.SnapControl.SelectionChanged -= SnapControl_SelectionChanged;
        }
    }
}


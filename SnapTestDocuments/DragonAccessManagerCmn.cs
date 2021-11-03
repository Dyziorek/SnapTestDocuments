using DevExpress.XtraRichEdit.API.Native;
using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Threading;

namespace SnapTestDocuments
{
    public class DragonAccessManagerCmn : IDragonAccessManager
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger("ExtSnapControl");
        private DocumentEntityBase currentSelectedInterp = null;
        private Field currentSectionField = null;
        int currentSectionOffset = 0;
        private Tuple<int, int> lastselectionPair = new Tuple<int, int>(0, 0); //start, length
        private Tuple<int, Rectangle> lastPosChar = new Tuple<int, Rectangle>(-1, Rectangle.Empty);
        private string cacheStringText = string.Empty;


        protected ISnapCtrlContext snapCtrlCtx;
        protected DevExpress.Snap.SnapControl SnapCtrl { get { return snapCtrlCtx.SnapControl; } }

        public DragonAccessManagerCmn(ISnapCtrlContext snapCtrlCtx)
        {
            this.snapCtrlCtx = snapCtrlCtx;
        }

        public void Clear()
        {
            currentSelectedInterp = null;
            snapCtrlCtx.SnapControl.ContentChanged -= SnapControl_ContentChanged;
            snapCtrlCtx.SnapControl.SelectionChanged -= SnapControl_SelectionChanged;
        }

        public Tuple<int, int> GetSel()
        {
            lock (this)
            {
                if (this.currentSelectedInterp != null)
                {
                    if (lastselectionPair.Item1 - lastselectionPair.Item2 == 0)
                    {
                        var docSelection = this.SnapCtrl.Document.CaretPosition.ToInt();

                        int curPos = docSelection - currentSectionOffset;
                        if (curPos > 0 && curPos <= currentSectionField.ResultRange.Length)
                        {
                            lastselectionPair = new Tuple<int, int>(curPos, curPos);
                        }
                    }
                    log.InfoFormat("[{0}] GetSel returns p:{1}, l:{2}", RuntimeHelpers.GetHashCode(this), lastselectionPair.Item1, lastselectionPair.Item2);
                    return lastselectionPair;
                }
                log.InfoFormat("[{0}]GetSel returns zero p:{1}, l:{2}", RuntimeHelpers.GetHashCode(this), 0, 0);
                return new Tuple<int, int>(0, 0);
            }
        }

        public string GetText()
        {
            lock (this)
            {
                log.InfoFormat("[{0}]GetText:'{1}'", RuntimeHelpers.GetHashCode(this), cacheStringText);
                return cacheStringText;
            }
        }

        public int GetTextLen()
        {
            lock (this)
            {
                log.Debug("GetTextLen:");
                string textResult = GetText();
                if (!string.IsNullOrEmpty(textResult))
                {
                    log.InfoFormat("[{0}]TextLen:{1}", RuntimeHelpers.GetHashCode(this), textResult.Length);
                    return textResult.Length;
                }

                return 0;
            }
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
                if (currentSectionField != null)
                {
                    var docSelection = this.SnapCtrl.Document.Selection;
                    int startPos = docSelection.Start.ToInt();
                    int endPos = docSelection.End.ToInt();


                    int currentSectionLength = currentSectionField.ResultRange.Length;

                    int minPos = Math.Min(startPos - currentSectionOffset, endPos - currentSectionOffset);
                    int maxPos = Math.Max(startPos - currentSectionOffset, endPos - currentSectionOffset);
                    //if (maxPos == minPos)
                    //{
                    //    maxPos += 1;
                    //    minPos += 1;
                    //}
                    if ((maxPos - minPos) >= currentSectionLength)
                    {
                        minPos = 0;
                        maxPos = currentSectionLength - 1;
                    }

                    log.InfoFormat("[{0}]Current Selection begin:{1}, end:{2}", RuntimeHelpers.GetHashCode(this), minPos, maxPos);
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
                if (currentSectionField != null)
                {
                    cacheStringText = this.SnapCtrl.Document.GetText(currentSectionField.ResultRange);
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
        }

        //public bool InsertText(string text)
        //{
        //    if (!string.IsNullOrEmpty(text))
        //    {
        //        if (SnapRangePermissionsTools.IsDocumentPositionEditableRange(SnapCtrl.Document, SnapCtrl.Document.CaretPosition))
        //        {
        //            this.SnapCtrl.Document.BeginUpdate();
        //            if (this.SnapCtrl.Document.Selection.Length > 0 && SnapRangePermissionsTools.IsDocumentRangeEditableRange(SnapCtrl.Document, this.SnapCtrl.Document.Selection))
        //            {
        //                this.SnapCtrl.Document.Delete(this.SnapCtrl.Document.Selection);
        //            }
        //            this.SnapCtrl.Document.EndUpdate();
        //            SnapCtrl.BeginUpdate();
        //            SnapCtrl.Document.InsertText(SnapCtrl.Document.CaretPosition, text);
        //            SnapCtrl.EndUpdate();
        //            return true;
        //        }
        //    }
        //    return false;
        //}

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
                this.currentSelectedInterp = selectedItem;
                Field sectionField = snapCtrlCtx.GetManager<IInterpSectionsManager>().GetSectionField(currentSelectedInterp);
                if (sectionField != null)
                {
                    currentSectionOffset = sectionField.ResultRange.Start.ToInt();
                    cacheStringText = SnapCtrl.Document.GetText(sectionField.ResultRange);
                    this.currentSectionField = sectionField;
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
                if (!string.IsNullOrEmpty(text))
                {
                    if (this.currentSelectedInterp != null)
                    {
                        var selection = this.SnapCtrl.Document.Selection;
                        if (selection.Start.ToInt() - currentSectionOffset != lastselectionPair.Item1)
                        {
                            log.InfoFormat("[{0}]Replace Text selected (cached): s:{1} e:{2}", RuntimeHelpers.GetHashCode(this), lastselectionPair.Item1, lastselectionPair.Item2);
                            log.InfoFormat("[{0}]Current selection was: {1} size:{2}", RuntimeHelpers.GetHashCode(this), selection.Start.ToInt() - currentSectionOffset, selection.Length);
                            selection = this.SnapCtrl.Document.CreateRange(currentSectionField.ResultRange.Start.ToInt() + lastselectionPair.Item1, lastselectionPair.Item2 - lastselectionPair.Item1);
                        }
                        else
                        {
                            log.InfoFormat("[{0}]Replace Text selected: {1} size:{2}", RuntimeHelpers.GetHashCode(this), selection.Start.ToInt() - currentSectionOffset, selection.Length);
                        }
                        if (SnapRangePermissionsTools.IsDocumentRangeEditableRange(SnapCtrl.Document, selection))
                        {
                            log.InfoFormat("[{0}]Replace Text:'{1}'", RuntimeHelpers.GetHashCode(this), text);
                            log.InfoFormat("[{0}]Replacing selected text in place:'{1}'", RuntimeHelpers.GetHashCode(this), SnapCtrl.Document.GetText(selection));

                            if (currentSectionField != null)
                            {
                                if (selection.Start.ToInt() - currentSectionOffset - 1 == cacheStringText.Length)
                                {
                                    if (cacheStringText.Length > 0 && !Char.IsWhiteSpace(cacheStringText[cacheStringText.Length - 1]) && !Char.IsWhiteSpace(text[0]))
                                    {
                                        log.InfoFormat("[{0}]Adding space existing text: '{1}', addedText '{2}', lastCharWhiteSpace:{3}, first:{4}", RuntimeHelpers.GetHashCode(this), cacheStringText, text, !Char.IsWhiteSpace(cacheStringText[cacheStringText.Length - 1]), !Char.IsWhiteSpace(text[0]));
                                        text = " " + text;
                                    }
                                }
                                SubDocument docFragment = selection.BeginUpdateDocument();
                                try
                                {
                                    this.SnapCtrl.BeginUpdate();
                                    docFragment.Replace(selection, text);
                                    this.SnapCtrl.Document.CaretPosition = selection.End;
                                }
                                finally
                                {
                                    selection.EndUpdateDocument(docFragment);
                                    this.SnapCtrl.EndUpdate();
                                    cacheStringText = this.SnapCtrl.Document.GetText(currentSectionField.ResultRange);
                                    log.InfoFormat("[{0}]Whole Section Text after replace:'{1}'", RuntimeHelpers.GetHashCode(this), cacheStringText);
                                }
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
            lock (this)
            {
                log.DebugFormat("SetSel: {0}, {1}", start, end);
                if (this.currentSelectedInterp != null)
                {
                    if (currentSectionField != null)
                    {

                        int currentSectionLength = currentSectionField.ResultRange.Length;

                        var selection = this.SnapCtrl.Document.Selection;
                        this.lastselectionPair = new Tuple<int, int>(selection.Start.ToInt() - currentSectionOffset, selection.End.ToInt() - currentSectionOffset);
                        int minPos = Math.Min(currentSectionOffset + start, currentSectionOffset + end);
                        int maxPos = Math.Max(currentSectionOffset + start, currentSectionOffset + end);
                        if (maxPos == minPos)
                        {
                            maxPos += 1;
                            minPos += 1;
                        }
                        if ((maxPos - minPos) > currentSectionLength)
                        {
                            minPos = currentSectionOffset;
                            maxPos = currentSectionOffset + currentSectionLength - 1;
                        }
                        this.SnapCtrl.Document.Selection = this.SnapCtrl.Document.CreateRange(minPos, maxPos - minPos);
                        log.InfoFormat("[{0}]Set Sel at:{0} with {1}", minPos - currentSectionOffset, maxPos - minPos);
                        return new Tuple<int, int>(minPos - currentSectionOffset, maxPos - currentSectionOffset);
                    }
                    else
                    {
                        this.currentSelectedInterp = null;
                    }
                }
                log.Info("Ignored Set Sel Comamnd");
                return new Tuple<int, int>(0, 0);
            }
        }

        public Rectangle PosFromChar(int charPos)
        {
            log.DebugFormat("PosFromChar: for charpos {0}", charPos);
            if (this.currentSelectedInterp != null)
            {
                if (currentSectionField != null)
                {
                    int currentSectionOffset = currentSectionField.ResultRange.Start.ToInt();
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
                    log.InfoFormat("[{0}]Ignored Set Sel Comamnd reason: no section field {1}", RuntimeHelpers.GetHashCode(this), currentSectionField == null ? "true" : "false");
                }
            }
            else
            {
                log.InfoFormat("[{0}]Ignored Set Sel Comamnd reason: unselected section {1}", RuntimeHelpers.GetHashCode(this), currentSelectedInterp == null ? "true" : "false");
            }
            return new Rectangle();
        }

        public int CharFromPos(PointF charPos)
        {
            log.DebugFormat("CharFromPos: from {0}", charPos);
            if (this.currentSelectedInterp != null)
            {
                if (currentSectionField != null)
                {
                    int currentSectionOffset = currentSectionField.ResultRange.Start.ToInt();
                    int currentSectionLength = currentSectionField.ResultRange.Length;
                    int minPos = Math.Min(snapCtrlCtx.SnapControl.GetPositionFromPoint(charPos).ToInt(), currentSectionOffset + currentSectionLength);
                    return Math.Max(0, minPos - currentSectionOffset);
                }
            }
            return -1;
        }
    }
}


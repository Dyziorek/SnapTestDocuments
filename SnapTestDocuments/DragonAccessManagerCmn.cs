using DevExpress.XtraRichEdit.API.Native;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            if (this.currentSelectedInterp != null)
            {
                return lastselectionPair;
            }
            log.InfoFormat("GetSel returns zero p:{0}, l:{1}", 0, 0);
            return new Tuple<int, int>(0, 0);
        }

        public string GetText()
        {
            log.InfoFormat("GetText:'{0}'", cacheStringText);
            return cacheStringText;
        }

        public int GetTextLen()
        {
            log.Debug("GetTextLen:");
            string textResult = GetText();
            if (!string.IsNullOrEmpty(textResult))
            {
                log.InfoFormat("TextLen:{0}", textResult.Length);
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
            log.Debug("SnapControl_SelectionChanged:");
            if (this.currentSelectedInterp != null)
            {
                Field sectionField = snapCtrlCtx.GetManager<IInterpSectionsManager>().GetSectionField(currentSelectedInterp);
                if (sectionField != null)
                {
                    var docSelection = this.SnapCtrl.Document.Selection;
                    int startPos = docSelection.Start.ToInt();
                    int endPos = docSelection.End.ToInt();


                    int currentSectionOffset = sectionField.ResultRange.Start.ToInt();
                    int currentSectionLength = sectionField.ResultRange.Length;

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

                    log.InfoFormat("Current Selection p:{0}, l:{1}", minPos, maxPos - minPos);
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
            log.DebugFormat("ReplaceText with '{0}'", text);
            if (!string.IsNullOrEmpty(text))
            {
                if (this.currentSelectedInterp != null)
                {

                    var selection = this.SnapCtrl.Document.Selection;
                    if (selection.Start.ToInt() != lastselectionPair.Item1)
                    {
                        log.InfoFormat("Replace Text selected (cached): {0} size:{1}", lastselectionPair.Item1, lastselectionPair.Item2);
                        log.InfoFormat("Current selection was: {0} size:{1}", selection.Start.ToInt(), selection.Length);
                        selection = this.SnapCtrl.Document.CreateRange(lastselectionPair.Item1, lastselectionPair.Item2);
                    }
                    else
                    {
                        log.InfoFormat("Replace Text selected: {0} size:{1}", selection.Start.ToInt(), selection.Length);
                    }
                    if (SnapRangePermissionsTools.IsDocumentRangeEditableRange(SnapCtrl.Document, selection))
                    {
                        log.InfoFormat("Replace Text:'{0}'", text);
                        log.InfoFormat("Replacing selected text in place:'{0}'", SnapCtrl.Document.GetText(selection));
                        
                        if (currentSectionField != null)
                        {
                            int currentSectionOffset = currentSectionField.ResultRange.Start.ToInt() - 1;
                            if (selection.Start.ToInt() - currentSectionOffset == cacheStringText.Length)
                            {
                                if (cacheStringText.Length > 0 && !Char.IsWhiteSpace(cacheStringText[cacheStringText.Length - 1]) && !Char.IsWhiteSpace(text[0]))
                                {
                                    log.InfoFormat("Adding space existing text: '{0}', addedText '{1}', lastCharWhiteSpace:{2}, first:{3}", cacheStringText, text, !Char.IsWhiteSpace(cacheStringText[cacheStringText.Length - 1]), !Char.IsWhiteSpace(text[0]));
                                    text = " " + text;
                                }
                            }
                            SubDocument docFragment = selection.BeginUpdateDocument();
                            try
                            {
                                this.SnapCtrl.BeginUpdate();
                                docFragment.Replace(selection, text);
                            }
                            finally
                            {
                                selection.EndUpdateDocument(docFragment);
                                this.SnapCtrl.EndUpdate();
                                cacheStringText = this.SnapCtrl.Document.GetText(currentSectionField.ResultRange);
                                log.InfoFormat("Whole Section Text after replace:'{0}'", cacheStringText);
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

        public void Reset()
        {
            this.currentSelectedInterp = null;
        }

        public Tuple<int, int> SetSel(int start, int end)
        {
            log.DebugFormat("SetSel: {0}, {1}", start, end);
            if (this.currentSelectedInterp != null)
            {
                if (currentSectionField != null)
                {

                    int currentSectionLength = currentSectionField.ResultRange.Length;

                    var selection = this.SnapCtrl.Document.Selection;
                    this.lastselectionPair = new Tuple<int, int>(selection.Start.ToInt(), selection.Length);
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
                    log.InfoFormat("Set Sel at:{0} with {1}", minPos - currentSectionOffset, maxPos - minPos);
                    return new Tuple<int, int>(minPos, maxPos);
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
                    log.InfoFormat("Ignored Set Sel Comamnd reason: no section field {0}", currentSectionField == null ? "true" : "false");
                }
            }
            else
            {
                log.InfoFormat("Ignored Set Sel Comamnd reason: unselected section {0}", currentSelectedInterp == null ? "true" : "false");
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


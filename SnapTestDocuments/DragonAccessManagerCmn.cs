using DevExpress.Snap;
using DevExpress.XtraRichEdit.API.Native;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace SnapTestDocuments
{
    public class DragonAccessManagerCmn : IDragonAccessManager
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger("DragonAccessManagerCmn");
        private DragonDictationHelper dictationHelper = new DragonDictationHelper();
        private DocumentEntityBase currentSelectedInterp = null;
        private ITextFieldInfo currentSectionField = null;
        int currentSectionOffset = 0;
        private Tuple<int, int> lastSelectionPair = new Tuple<int, int>(-1, -1); //start, end
        private Tuple<int, int> requestSelectPair = new Tuple<int, int>(-1, -1);
        private Tuple<int, Rectangle> lastCharRect = new Tuple<int, Rectangle>(-1, Rectangle.Empty);
        private string cacheStringText = null;
        private string lastCachedText = null;
        private int lastCachedTextLength = 0;
        private DocumentEntityBase currentSectionElement;
        private bool DirtySectionTextMapping = true;

        protected ISnapCtrlContext snapCtrlCtx;
        protected SnapControl SnapCtrl { get { return snapCtrlCtx.SnapControl; } }

        public DragonAccessManagerCmn(ISnapCtrlContext snapCtrlCtx)
        {
            this.snapCtrlCtx = snapCtrlCtx;
        }

        public void Clear()
        {
            currentSelectedInterp = null;
            snapCtrlCtx.SnapControl.ContentChanged -= DragonAccessManagerCmn_ContentChanged;
            snapCtrlCtx.SnapControl.SelectionChanged -= DragonAccessManagerCmn_SelectionChanged;
        }

        public Tuple<int, int> GetSel()
        {
            if (currentSelectedInterp != null)
            {
                if (DirtySectionTextMapping)
                {
                    UpdateSectionTextMapping();
                }
                if (log.IsDebugEnabled)
                {
                    log.DebugFormat("GetSel local:{0} mapped:{1}", lastSelectionPair, new Tuple<int, int>(dictationHelper.SnapToEdit(lastSelectionPair.Item1), dictationHelper.SnapToEdit(lastSelectionPair.Item2)));
                }
                return new Tuple<int, int>(dictationHelper.SnapToEdit(lastSelectionPair.Item1), dictationHelper.SnapToEdit(lastSelectionPair.Item2));
            }
            log.DebugFormat("GetSel returns zero p:{0}, l:{1}", 0, 0);
            return new Tuple<int, int>(0, 0);

        }

        public string GetText()
        {
            if (String.Compare(cacheStringText, lastCachedText) != 0)
            {
                log.DebugFormat("GetText:'{0}'", cacheStringText);
            }
            lastCachedText = cacheStringText;
            return cacheStringText;
        }

        public int GetTextLen()
        {
            string textResult = GetText();
            if (!string.IsNullOrEmpty(textResult))
            {
                if(textResult.Length != lastCachedTextLength)
                {
                    log.InfoFormat("TextLen:{0}", textResult.Length);
                }
                lastCachedTextLength = textResult.Length;
                return textResult.Length;
            }

            return 0;
        }

        public void Init()
        {
            currentSelectedInterp = null;
            snapCtrlCtx.SnapControl.ContentChanged -= DragonAccessManagerCmn_ContentChanged;
            snapCtrlCtx.SnapControl.ContentChanged += DragonAccessManagerCmn_ContentChanged;
            snapCtrlCtx.SnapControl.SelectionChanged -= DragonAccessManagerCmn_SelectionChanged;
            snapCtrlCtx.SnapControl.SelectionChanged += DragonAccessManagerCmn_SelectionChanged;
        }

        private void DragonAccessManagerCmn_SelectionChanged(object sender, EventArgs e)
        {
            log.Info("SnapControl_SelectionChanged:");
            if (currentSelectedInterp != null)
            {
                if (currentSectionField != null && SnapFieldTools.IsValidField(currentSectionField))
                {
                    var docSelection = SnapCtrl.Document.Selection;
                    int startPos = docSelection.Start.ToInt();
                    int endPos = docSelection.End.ToInt();


                    int currentSectionLength = currentSectionField.Field.ToSnap().ResultRange.End.ToInt() - currentSectionOffset;

                    int minPos = Math.Min(startPos - currentSectionOffset, endPos - currentSectionOffset);
                    int maxPos = Math.Max(startPos - currentSectionOffset, endPos - currentSectionOffset);
                    if ((maxPos - minPos) >= currentSectionLength)
                    {
                        minPos = 0;
                        maxPos = currentSectionLength;
                    }

                    log.InfoFormat("Current Selection begin:{0}, end:{1}", minPos, maxPos);
                    lastSelectionPair = new Tuple<int, int>(minPos, maxPos);
                }
                else
                {
                    currentSelectedInterp = null;
                }
            }
        }

        private void DragonAccessManagerCmn_ContentChanged(object sender, EventArgs e)
        {
            log.Debug("SnapControl_ContentChanged:");
            if (currentSelectedInterp != null)
            {
                if (currentSectionField != null && SnapFieldTools.IsValidField(currentSectionField.Field))
                {
                    cacheStringText = SnapCtrl.Document.GetText(currentSectionField.Field.ToSnap().ResultRange);
                    DirtySectionTextMapping = true;
                }
                else
                {
                    currentSelectedInterp = null;
                    cacheStringText = string.Empty;
                }
            }
            else
            {
                cacheStringText = string.Empty;
            }

            log.InfoFormat("DragAccMgrCmn: SnapControl_ContentChanged - updated text: '{0}'", cacheStringText);
        }


        private void UpdateSectionTextMapping()
        {
            log.Debug("Analyze text section started");
            bool lastParMark = false;
            var paragraphs = SnapCtrl.Document.Paragraphs.Get(currentSectionField.Field.ToSnap().ResultRange);
            var paragraphPositions = new List<int>();
            foreach (var parItem in paragraphs)
            {
                paragraphPositions.Add(parItem.Range.End.ToInt());
                if (parItem.Range.End.ToInt() == currentSectionField.Field.ToSnap().ResultRange.End.ToInt())
                {
                    lastParMark = true;
                }
            }
            if (lastParMark)
            {
                cacheStringText += "\r\n";
            }
            dictationHelper.AnalyzeTextSection(snapCtrlCtx,
                SnapCtrl, currentSectionField.Field.ToSnap().ResultRange, 
                cacheStringText, paragraphPositions.Select(sectionParagraph => sectionParagraph - currentSectionOffset), 
                new Tuple<int, int>(lastSelectionPair.Item1+currentSectionOffset, lastSelectionPair.Item2+currentSectionOffset));
            DirtySectionTextMapping = false;
            log.Debug("Analyze text section completed");
        }

        public bool HasSections()
        {
            log.Debug("HasSections:");
            return currentSelectedInterp != null;
        }
        public void UpdateSelectedItem(DocumentEntityBase selectedItem)
        {
            log.DebugFormat("UpdateSelectedItem: {0}", selectedItem != null);
            var selectedEntityObject = selectedItem;
            if (selectedItem?.Type == DocumentEntityTypes.CannedMessage || selectedItem?.Type == DocumentEntityTypes.EditField || selectedItem?.Type == DocumentEntityTypes.UDxSection)
            {
                log.DebugFormat("UpdateSelectedItem: {0}", selectedItem.Type);
                selectedEntityObject = selectedItem.GetTopParent();
                currentSectionElement = selectedItem;
            }

            if (selectedEntityObject?.Type == DocumentEntityTypes.InterpretationSection)
            {
                Field sectionField = snapCtrlCtx.GetManager<IInterpSectionsManager>().GetSectionField(selectedEntityObject);
                if (sectionField != null)
                {
                    var lastSelectedInterp = currentSelectedInterp;
                    currentSectionOffset = sectionField.ResultRange.Start.ToInt();
                    currentSectionField = sectionField.GetTextFieldInfo(SnapCtrl.Document);
                    currentSelectedInterp = selectedEntityObject;
                    lastCharRect = new Tuple<int, Rectangle>(-1, Rectangle.Empty);
                    DragonAccessManagerCmn_SelectionChanged(this, EventArgs.Empty);
                    if (lastSelectedInterp != selectedEntityObject)
                    {
                        DragonAccessManagerCmn_ContentChanged(this, EventArgs.Empty);
                    }
                    requestSelectPair = lastSelectionPair;
                }
                else
                {
                    currentSelectedInterp = null;
                    lastSelectionPair = new Tuple<int, int>(0, 0);
                    currentSectionField = null;
                    currentSectionOffset = 0;
                    cacheStringText = "";
                }
            }
            else
            {
                currentSelectedInterp = null;
                lastSelectionPair = new Tuple<int, int>(0, 0);
                currentSectionField = null;
                currentSectionOffset = 0;
                currentSectionElement = null;
                cacheStringText = "";
            }
        }
        public void ReplaceText(string text)
        {
            lock (this)
            {
                log.DebugFormat("ReplaceText with '{0}'", text);
                if (this.currentSelectedInterp != null && this.currentSectionField != null && SnapFieldTools.IsValidField(currentSectionField.Field))
                {
                    var selection = this.SnapCtrl.Document.Selection;
                    bool fieldProcessed = false;
                    if (currentSectionElement?.Type == DocumentEntityTypes.EditField)
                    {
                        var fieldCheck = DragonDictationHelper.GetFieldFromPosInRange(snapCtrlCtx.Document.CaretPosition.ToInt(), snapCtrlCtx.Document, currentSectionField.Field.ResultRange);
                        var fieldUpdated = dictationHelper.HandleWorkFieldSelection(snapCtrlCtx, fieldCheck, text);
                        if (fieldUpdated != null)
                        {
                            fieldProcessed = true;
                            DragonAccessManagerCmn_ContentChanged(this, EventArgs.Empty);
                            int updatedCaretPosition = this.SnapCtrl.Document.Selection.End.ToInt() - currentSectionOffset;
                            lastSelectionPair = new Tuple<int, int>(updatedCaretPosition, updatedCaretPosition);
                        }
                    }
                    if (!fieldProcessed)
                    {
                        if (lastSelectionPair != requestSelectPair && requestSelectPair.Item1 == requestSelectPair.Item2 && requestSelectPair.Item1 + currentSectionOffset + 1 == selection.Start.ToInt())
                        {
                            // Dragon resets selected word to single cursor after selected word just before replacing this with new word
                            // so we need to restore selection before replacing
                            if (log.IsDebugEnabled)
                            {
                                log.DebugFormat("Use cached selection: current sel: {0} , cache sel:{1}, snap selection pos {3}", requestSelectPair, lastSelectionPair, selection.Start.ToInt());
                            }
                            selection = SnapCtrl.Document.CreateRange(lastSelectionPair.Item1 + currentSectionOffset, Math.Abs(lastSelectionPair.Item2 - lastSelectionPair.Item1));
                        }

                        if (selection.Start.ToInt() > currentSectionField.Field.ToSnap().ResultRange.End.ToInt())
                        {
                            log.InfoFormat("DragonAcc - selection is too far start at:{0}, range ends at{1}", selection.Start.ToInt(), currentSectionField.Field.ToSnap().ResultRange.End.ToInt());
                            selection = SnapCtrl.Document.CreateRange(currentSectionField.Field.ToSnap().ResultRange.End.ToInt(), 0);
                        }
                        else if (selection.End.ToInt() < currentSectionField.Field.ToSnap().ResultRange.Start.ToInt())
                        {
                            log.InfoFormat("DragonAcc - selection is too close start at:{0}, range starts at{1}", selection.Start.ToInt(), currentSectionField.Field.ToSnap().ResultRange.Start.ToInt());
                            selection = SnapCtrl.Document.CreateRange(currentSectionField.Field.ToSnap().ResultRange.Start.ToInt(), 0);
                        }
                        else
                        {
                            log.InfoFormat("Replace Text selected: {0} size:{1}", selection.Start.ToInt() - currentSectionOffset, selection.Length);
                        }
                        if (SnapRangePermissionsTools.IsDocumentRangeEditableRange(SnapCtrl.Document, selection))
                        {
                            var nearField = dictationHelper.GetNearestFieldFromPosition(snapCtrlCtx, selection.Start.ToInt());
                            if (nearField.Item1 != null)
                            {
                                if (Math.Abs(selection.Start.ToInt() - nearField.Item1.ResultRange.End.ToInt()) <= 1)
                                {
                                    var rangeToReplace = nearField.Item1.ResultRange;
                                    if (log.IsDebugEnabled || log.IsInfoEnabled)
                                    {
                                        log.InfoFormat("Tracked field change text is {0}", SnapCtrl.Document.GetText(nearField.Item1.ResultRange));
                                    }
                                    var subDocumentUpdate = rangeToReplace.BeginUpdateDocument();
                                    try
                                    {
                                        subDocumentUpdate.Replace(rangeToReplace, subDocumentUpdate.GetText(rangeToReplace) + text);
                                    }
                                    finally
                                    {
                                        rangeToReplace.EndUpdateDocument(subDocumentUpdate);
                                    }
                                    SnapCtrl.Document.Selection = SnapCtrl.Document.CreateRange(rangeToReplace.End, 0);
                                    fieldProcessed = true;
                                }
                            }
                            if (!fieldProcessed)
                            {
                                SubDocument docFragment = selection.BeginUpdateDocument();
                                try
                                {
                                    docFragment.BeginUpdate();
                                    log.InfoFormat("Replace final Text:'{0}'", text);
                                    docFragment.Replace(selection, text);
                                }
                                finally
                                {
                                    docFragment.EndUpdate();
                                    selection.EndUpdateDocument(docFragment);
                                    int updatedCaretPosition = selection.End.ToInt() - currentSectionOffset;
                                    log.InfoFormat("ReplaceText current caret position:{0}", updatedCaretPosition);
                                    DragonAccessManagerCmn_ContentChanged(this, EventArgs.Empty);
                                    SnapCtrl.Document.CaretPosition = selection.End;
                                    lastSelectionPair = new Tuple<int, int>(updatedCaretPosition, updatedCaretPosition);
                                    UpdateSectionTextMapping();
                                    log.InfoFormat("Whole Section Text after replace:'{0}'", cacheStringText);
                                }
                            }
                        }
                    }
                }
                else
                {
                    log.Info("Replace Text: no section selected!");
                }

            }
        }

        public void Reset()
        {
            currentSelectedInterp = null;
        }

        public Tuple<int, int> SetSel(int start, int end)
        {

            log.InfoFormat("SetSel params: begin:{0}, end:{1}", start, end);
            if (currentSelectedInterp != null)
            {
                if (currentSectionField != null && SnapFieldTools.IsValidField(currentSectionField.Field))
                {
                    int currentSectionLength = currentSectionField.Field.ToSnap().ResultRange.End.ToInt() - currentSectionOffset;

                    var selection = SnapCtrl.Document.Selection;
                    log.InfoFormat("SetSel: Current Selection is at - begin:{0}, end:{1}", selection.Start.ToInt() - currentSectionOffset, selection.End.ToInt() - currentSectionOffset);
                    if (DirtySectionTextMapping)
                    {
                        UpdateSectionTextMapping();
                    }
                    int startPos = dictationHelper.EditToSnap(start);
                    int endPos = dictationHelper.EditToSnap(end);
                    log.DebugFormat("Mapped setsel orig:{0},{1}, maps:{2},{3}", start, end, startPos, endPos);
                    int minPos = Math.Min(currentSectionOffset + startPos, currentSectionOffset + endPos);
                    int maxPos = Math.Max(currentSectionOffset + startPos, currentSectionOffset + endPos);
                    if (maxPos == minPos)
                    {
                        if (!requestSelectPair.Equals(new Tuple<int, int>(minPos - currentSectionOffset, maxPos - currentSectionOffset)))
                        {
                            SnapCtrl.Document.BeginUpdate();
                            var docCharPos = SnapCtrl.Document.CreatePosition(minPos);
                            var rectObj = SnapCtrl.GetBoundsFromPosition(docCharPos);
                            SnapCtrl.Document.CaretPosition = docCharPos;
                            rectObj = DevExpress.Office.Utils.Units.DocumentsToPixels(rectObj, SnapCtrl.DpiX, SnapCtrl.DpiY);
                            var clientRect = SnapCtrl.ClientRectangle;
                            clientRect.Inflate(0, Convert.ToInt32(-clientRect.Height * 0.05));
                            if (rectObj == Rectangle.Empty || !clientRect.Contains(rectObj.X, rectObj.Y))
                            {
                                SnapCtrl.ScrollToCaret();
                            }
                            SnapCtrl.Document.EndUpdate();
                        }
                    }
                    else
                    {
                        if ((maxPos - minPos) > currentSectionLength)
                        {
                            minPos = currentSectionOffset;
                            maxPos = currentSectionOffset + currentSectionLength - 1;
                        }

                        if (!requestSelectPair.Equals(new Tuple<int, int>(minPos, maxPos)))
                        {
                            SnapCtrl.Document.BeginUpdate();
                            var docCharPos = SnapCtrl.Document.CreatePosition(minPos);
                            var rectObj = SnapCtrl.GetBoundsFromPosition(docCharPos);
                            rectObj = DevExpress.Office.Utils.Units.DocumentsToPixels(rectObj, SnapCtrl.DpiX, SnapCtrl.DpiY);
                            var clientRect = SnapCtrl.ClientRectangle;
                            clientRect.Inflate(0, Convert.ToInt32(-clientRect.Height * 0.05));
                            if (rectObj == Rectangle.Empty || !clientRect.Contains(rectObj.X, rectObj.Y))
                            {
                                SnapCtrl.Document.CaretPosition = docCharPos;
                                SnapCtrl.ScrollToCaret();
                            }
                        SnapCtrl.Document.Selection = SnapCtrl.Document.CreateRange(minPos, maxPos - minPos);
                            SnapCtrl.Document.EndUpdate();
                        }
                    }
                    log.InfoFormat("SetSel set at begin:{0}, length:{1}", minPos - currentSectionOffset, maxPos - minPos);
                    requestSelectPair = new Tuple<int, int>(minPos - currentSectionOffset, maxPos - currentSectionOffset);
                    return requestSelectPair;
                }
                else
                {
                    currentSelectedInterp = null;
                }
            }
            log.Info("Ignored Set Sel Comamnd");
            return new Tuple<int, int>(0, 0);

        }

        public Rectangle PosFromChar(int charPos)
        {
            log.DebugFormat("PosFromChar: for charpos {0}", charPos);
            if (currentSelectedInterp != null)
            {
                if (currentSectionField != null && SnapFieldTools.IsValidField(currentSectionField))
                {
                    int currentSectionOffset = currentSectionField.Field.ToSnap().ResultRange.Start.ToInt();
                    int currentSectionLength = currentSectionField.Field.ToSnap().ResultRange.End.ToInt() - currentSectionOffset;
                    if (charPos <= cacheStringText.Length)
                    {
                        if (DirtySectionTextMapping)
                        {
                            UpdateSectionTextMapping();
                        }
                        int snapPos = dictationHelper.EditToSnap(charPos);
                    if (snapPos <= currentSectionLength)
                    {
                        if (lastCharRect.Item1 == snapPos + currentSectionOffset)
                        {
                            return lastCharRect.Item2;
                        }

                        var rectPos = DevExpress.Office.Utils.Units.DocumentsToPixels(snapCtrlCtx.SnapControl.GetBoundsFromPosition(snapCtrlCtx.SnapDocument.CreatePosition(currentSectionOffset + snapPos)), snapCtrlCtx.SnapControl.DpiX, snapCtrlCtx.SnapControl.DpiY);
                        lastCharRect = new Tuple<int, Rectangle>(snapPos + currentSectionOffset, rectPos);
                            log.InfoFormat("Position  Char Pos:{0}, Mapped to Snap:{3}, X:{1}, Y:{2}", charPos, rectPos.X, rectPos.Y, snapPos);
                        return rectPos;
                    }
                    }
                    log.InfoFormat("Wrong PosFromChar reason: char outside, posChar:{0}, textLen: {1}", charPos,  currentSectionLength);
                }
                else
                {
                    currentSelectedInterp = null;
                    log.Debug("Ignored PosFromChar reason: no section field");
                }
            }
            else
            {
                log.DebugFormat("Ignored PosFromChar reason: unselected section");
            }
            return new Rectangle();
        }

        public int CharFromPos(PointF charPos)
        {
            log.DebugFormat("CharFromPos: from {0}", charPos);
            if (currentSelectedInterp != null)
            {
                if (currentSectionField != null && SnapFieldTools.IsValidField(currentSectionField))
                {
                    int currentSectionOffset = currentSectionField.Field.ToSnap().ResultRange.Start.ToInt();
                    int currentSectionLength = currentSectionField.Field.ToSnap().ResultRange.End.ToInt() - currentSectionOffset;
                    int minPos = Math.Min(snapCtrlCtx.SnapControl.GetPositionFromPoint(charPos).ToInt(), currentSectionOffset + currentSectionLength);
                    return Math.Max(0, minPos - currentSectionOffset);
                }
            }
            return -1;
        }

        public void TurnOn()
        {
            TurnOff();
            snapCtrlCtx.SnapControl.ContentChanged += DragonAccessManagerCmn_ContentChanged;
            snapCtrlCtx.SnapControl.SelectionChanged += DragonAccessManagerCmn_SelectionChanged;
        }

        public void TurnOff()
        {
            snapCtrlCtx.SnapControl.ContentChanged -= DragonAccessManagerCmn_ContentChanged;
            snapCtrlCtx.SnapControl.SelectionChanged -= DragonAccessManagerCmn_SelectionChanged;
        }

        public int SnapFromEdit(int editPos)
        {
            if (DirtySectionTextMapping)
            {
                UpdateSectionTextMapping();
            }
            return dictationHelper.EditToSnap(editPos);
        }

        public int EditFromSnap(int snapPos)
        {
            if (DirtySectionTextMapping)
            {
                UpdateSectionTextMapping();
            }
            return dictationHelper.SnapToEdit(snapPos);
        }
    }
}


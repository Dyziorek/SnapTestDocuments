using DevExpress.Snap;
using DevExpress.XtraRichEdit.API.Native;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace SnapTestDocuments
{
    public class DragonAccessManagerCmn : IDragonAccessManager
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger("DragonAccessManagerCmn");
        private DragonDictationHelper dictationHelper = new DragonDictationHelper();
        private DocumentEntityBase currentSelectedInterp = null;
        private ITextFieldInfo currentSectionField = null;
        int currentSectionOffset = 0;
        private bool fieldTextChanged = false;
        private Tuple<bool, Field> adjustField = new Tuple<bool, Field>(false, null);
        private Tuple<int, int> lastSelectionPair = new Tuple<int, int>(-1, -1); //start, end
        private Tuple<int, int> requestSelectPair = new Tuple<int, int>(-1, -1);
        private Tuple<int, Rectangle> lastCharRect = new Tuple<int, Rectangle>(-1, Rectangle.Empty);
        private string cacheStringText = null;
        private DocumentEntityBase currentSectionElement;

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
                if (log.IsInfoEnabled)
                {
                    log.InfoFormat("DragAccMgrCmn GetSel local:{0} mapped:{1}", lastSelectionPair, new Tuple<int, int>(dictationHelper.SnapToEdit(lastSelectionPair.Item1), dictationHelper.SnapToEdit(lastSelectionPair.Item2)));
                }
                return new Tuple<int, int>(dictationHelper.SnapToEdit(lastSelectionPair.Item1), dictationHelper.SnapToEdit(lastSelectionPair.Item2));
            }
            log.InfoFormat("DragAccMgrCmn GetSel returns zero p:{0}, l:{1}", 0, 0);
            return new Tuple<int, int>(0, 0);

        }

        public string GetText()
        {
            log.DebugFormat("DragAccMgrCmn GetText:'{0}'", cacheStringText);
            return cacheStringText;
        }

        public int GetTextLen()
        {
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
                if (currentSectionField != null && SnapFieldTools.IsValidField(currentSectionField.Field))
                {
                    var docSelection = SnapCtrl.Document.Selection;
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
            bool textMapped = false;
            if (currentSelectedInterp != null)
            {
                if (currentSectionField != null && SnapFieldTools.IsValidField(currentSectionField.Field))
                {
                    if (fieldTextChanged)
                    {
                        adjustField = new Tuple<bool, Field>(true, adjustField.Item2);
                    }
                    else
                    {
                        adjustField = new Tuple<bool, Field>(false, null);
                    }
                    bool lastParMark = false;
                    cacheStringText = SnapCtrl.Document.GetText(currentSectionField.Field.ToSnap().ResultRange);
                    var paragraphs = SnapCtrl.Document.Paragraphs.Get(currentSectionField.Field.ToSnap().ResultRange);
                    foreach (var parItem in paragraphs)
                    {
                        if (parItem.Range.End.ToInt() == currentSectionField.Field.ToSnap().ResultRange.End.ToInt())
                        {
                            lastParMark = true;
                        }
                    }
                    if (lastParMark)
                    {
                        cacheStringText += "\r\n";
                    }
                    var sectionRange = currentSectionField.Field.ToSnap().ResultRange;
                    var allEntities = SnapFieldTools.GetFieldsInRange(SnapCtrl.Document, sectionRange);

                    if (allEntities.Count() > 0)
                    {
                        var stringParts = new List<Tuple<string, int>>();
                        int initialRange = sectionRange.Start.ToInt();
                        foreach (var fieldData in allEntities)
                        {
                            if (fieldData.Range.Start.ToInt() >= initialRange)
                            {
                                stringParts.Add(new Tuple<string, int>(SnapCtrl.Document.GetText(SnapCtrl.Document.CreateRange(initialRange, fieldData.Range.Start.ToInt() - initialRange)), fieldData.CodeRange.Length + 2));
                                stringParts.Add(new Tuple<string, int>(SnapCtrl.Document.GetText(fieldData.ResultRange), 1));
                                initialRange = fieldData.Range.End.ToInt();
                            }
                        }
                        stringParts.Add(new Tuple<string, int>((SnapCtrl.Document.GetText(SnapCtrl.Document.CreateRange(initialRange, sectionRange.End.ToInt() - initialRange))), 0));
                        dictationHelper.MapTextPositions(stringParts);
                        textMapped = true;
                        ShowMappingTexts();
                    }
                }
                else
                {
                    currentSelectedInterp = null;
                    cacheStringText = string.Empty;
                }
            }
            else
                cacheStringText = string.Empty;

            if (!textMapped)
            {
                dictationHelper.MapTextPositions(cacheStringText);
            }
            log.InfoFormat("DragAccMgrCmn: SnapControl_ContentChanged - updated text: '{0}'", cacheStringText);

        }

        private void ShowMappingTexts()
        {
            FindTexts("Satisfactory");
            FindTexts("EPITHELIAL");
            FindTexts("Low-grade");
            FindTexts("below");
        }

        private void FindTexts(string textPart)
        {
            var searchResult = SnapCtrl.Document.StartSearch(textPart, SearchOptions.None, SearchDirection.Forward);
            if (searchResult.FindNext())
            {
                if (searchResult.CurrentResult != null)
                {
                    int minPos = searchResult.CurrentResult.Start.ToInt() - currentSectionOffset;
                    int textMinPos = cacheStringText.IndexOf(textPart);
                    snapCtrlCtx.MainForm.textBox1.Text += string.Format("\r\n'{2}' text:{0}, snap:{1}", textMinPos, minPos, textPart);
                }
            }
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
            if (selectedItem?.Type == DocumentEntityTypes.CannedMessage)
            {
                log.Debug("UpdateSelectedItem: Canned message");
                selectedEntityObject = selectedItem.Parent;
                currentSectionElement = selectedItem;
            }
            if (selectedItem?.Type == DocumentEntityTypes.EditField)
            {
                log.Debug("UpdateSelectedItem: EditField");
                selectedEntityObject = selectedItem.GetTopParent();
                currentSectionElement = selectedItem;
            }

            if (selectedEntityObject?.Type == DocumentEntityTypes.InterpretationSection)
            {
                Field sectionField = snapCtrlCtx.GetManager<IInterpSectionsManager>().GetSectionField(selectedEntityObject);
                if (sectionField != null)
                {
                    currentSectionOffset = sectionField.ResultRange.Start.ToInt();
                    currentSectionField = sectionField.GetTextFieldInfo(SnapCtrl.Document);
                    currentSelectedInterp = selectedEntityObject;
                    lastCharRect = new Tuple<int, Rectangle>(-1, Rectangle.Empty);
                    DragonAccessManagerCmn_ContentChanged(this, EventArgs.Empty);
                    DragonAccessManagerCmn_SelectionChanged(this, EventArgs.Empty);
                    requestSelectPair = lastSelectionPair;
                }
                else
                {
                    currentSelectedInterp = null;
                    lastSelectionPair = new Tuple<int, int>(0, 0);
                    currentSectionField = null;
                    currentSectionOffset = 0;
                }
            }
            else
            {
                currentSelectedInterp = null;
                lastSelectionPair = new Tuple<int, int>(0, 0);
                currentSectionField = null;
                currentSectionOffset = 0;
                currentSectionElement = null;
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
                        if (selection.Length == 0)
                        {
                            var fieldCheck = DragonDictationHelper.GetFieldFromPosInRange(snapCtrlCtx.Document.CaretPosition.ToInt(), snapCtrlCtx.Document, currentSectionField.Field.ResultRange);
                            var fieldUpdated = dictationHelper.HandleWorkFieldSelection(snapCtrlCtx, fieldCheck, text);
                            if (fieldUpdated != null)
                            {
                                fieldTextChanged = true;
                                fieldProcessed = true;
                                adjustField = new Tuple<bool, Field>(false, fieldUpdated);
                            }
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
                            log.InfoFormat("DragAccMgrCmn Replace Text selected: {0} size:{1}", selection.Start.ToInt() - currentSectionOffset, selection.Length);
                        }
                        if (SnapRangePermissionsTools.IsDocumentRangeEditableRange(SnapCtrl.Document, selection))
                        {
                            SubDocument docFragment = selection.BeginUpdateDocument();
                            try
                            {
                                docFragment.BeginUpdate();
                                log.InfoFormat("DragAccMgrCmn Replace final Text:'{0}'", text);
                                docFragment.Replace(selection, text);
                            }
                            finally
                            {
                                docFragment.EndUpdate();
                                selection.EndUpdateDocument(docFragment);
                                int updatedCaretPosition = selection.End.ToInt() - currentSectionOffset;
                                log.InfoFormat("DragAccMgrCmn ReplaceText current caret position:{0}", updatedCaretPosition);
                                DragonAccessManagerCmn_ContentChanged(this, EventArgs.Empty);
                                SnapCtrl.Document.CaretPosition = selection.End;
                                lastSelectionPair = new Tuple<int, int>(updatedCaretPosition, updatedCaretPosition);
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
            currentSelectedInterp = null;
        }

        public Tuple<int, int> SetSel(int start, int end)
        {

            log.DebugFormat("SetSel: {0}, {1}", start, end);
            if (currentSelectedInterp != null)
            {
                if (currentSectionField != null && SnapFieldTools.IsValidField(currentSectionField.Field))
                {
                    int currentSectionLength = currentSectionField.ResultRange.Length;

                    var selection = SnapCtrl.Document.Selection;
                    log.InfoFormat("DragAccMgrCmn Set Selection begin:{0}, end:{1}", selection.Start.ToInt() - currentSectionOffset, selection.End.ToInt() - currentSectionOffset);
                    int startPos = dictationHelper.EditToSnap(start);
                    int endPos = dictationHelper.EditToSnap(end);
                    log.InfoFormat("Mapped setsel orig:{0},{1}, maps:{2},{3}", start, end, startPos, endPos);
                    int minPos = Math.Min(currentSectionOffset + startPos, currentSectionOffset + endPos);
                    int maxPos = Math.Max(currentSectionOffset + startPos, currentSectionOffset + endPos);
                    if (maxPos == minPos)
                    {
                        bool adjustedField = false;
                        if (adjustField.Item1 && adjustField.Item2 != null)
                        {
                            var cursorPos = dictationHelper.SnapToEdit(minPos - currentSectionOffset);
                            var fieldEndPos = dictationHelper.SnapToEdit(adjustField.Item2.Range.End.ToInt() - currentSectionOffset);
                            if (cursorPos - fieldEndPos == 1 || cursorPos - fieldEndPos == 0)
                            {
                                SnapCtrl.Document.CaretPosition = SnapCtrl.Document.CreatePosition(adjustField.Item2.ResultRange.End.ToInt());
                                adjustedField = true;
                                adjustField = new Tuple<bool, Field>(false, null);
                            }
                        }
                        if (!requestSelectPair.Equals(new Tuple<int, int>(minPos - currentSectionOffset, maxPos - currentSectionOffset)) && !adjustedField)
                        {
                            SnapCtrl.Document.CaretPosition = SnapCtrl.Document.CreatePosition(minPos);
                        }
                    }
                    else
                    {
                        if ((maxPos - minPos) > currentSectionLength)
                        {
                            minPos = currentSectionOffset;
                            maxPos = currentSectionOffset + currentSectionLength - 1;
                        }
                        SnapCtrl.Document.Selection = SnapCtrl.Document.CreateRange(minPos, maxPos - minPos);
                    }
                    log.InfoFormat("DragAccMgrCmn  Set Sel at:{0} with {1}", minPos - currentSectionOffset, maxPos - minPos);
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
                if (currentSectionField != null && SnapFieldTools.IsValidField(currentSectionField.Field))
                {
                    int currentSectionOffset = currentSectionField.Field.ToSnap().ResultRange.Start.ToInt();
                    int currentSectionLength = currentSectionField.ResultRange.Length;
                    int snapPos = dictationHelper.EditToSnap(charPos);
                    if (snapPos <= currentSectionLength)
                    {
                        if (lastCharRect.Item1 == snapPos + currentSectionOffset)
                        {
                            return lastCharRect.Item2;
                        }

                        var rectPos = DevExpress.Office.Utils.Units.DocumentsToPixels(snapCtrlCtx.SnapControl.GetBoundsFromPosition(snapCtrlCtx.SnapDocument.CreatePosition(currentSectionOffset + snapPos)), snapCtrlCtx.SnapControl.DpiX, snapCtrlCtx.SnapControl.DpiY);
                        lastCharRect = new Tuple<int, Rectangle>(snapPos + currentSectionOffset, rectPos);
                        log.InfoFormat("Position  C:{0},Maps:{3},X:{1},Y:{2}", charPos, rectPos.X, rectPos.Y, snapPos);
                        return rectPos;
                    }
                    log.InfoFormat("DragAccMgrCmn Zerored PosFromChar reason: char outside, posChar:{0}, mapPos:{1} textLen: {2}", charPos, snapPos, currentSectionLength);
                }
                else
                {
                    currentSelectedInterp = null;
                    log.InfoFormat("DragAccMgrCmn Ignored Set Sel Comamnd reason: no section field");
                }
            }
            else
            {
                log.InfoFormat("DragAccMgrCmn Ignored PosFromChar reason: unselected section");
            }
            return new Rectangle();
        }

        public int CharFromPos(PointF charPos)
        {
            log.DebugFormat("CharFromPos: from {0}", charPos);
            if (currentSelectedInterp != null)
            {
                if (currentSectionField != null && SnapFieldTools.IsValidField(currentSectionField.Field))
                {
                    int currentSectionOffset = currentSectionField.Field.ToSnap().ResultRange.Start.ToInt();
                    int currentSectionLength = currentSectionField.ResultRange.Length;
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
    }
}


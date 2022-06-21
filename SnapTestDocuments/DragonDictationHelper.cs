using DevExpress.Snap;
using DevExpress.Snap.Core.API;
using DevExpress.XtraRichEdit.API.Native;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace SnapTestDocuments
{

    public static class DragonDicationTools
    {
        public static bool IsNotNullSelection(this Tuple<int, int> firstOperand)
        {
            return !firstOperand.Equals(new Tuple<int, int>(-1, -1));
        }

        public static int CompareFieldRange(this Field fld, int caretPos)
        {
            if (caretPos < fld.Range.Start.ToInt())
            {
                return -1;
            }
            else if (caretPos > fld.Range.End.ToInt())
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

    }

    class DragonDictationHelper
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger("DragonDictationHelper");
        /// <summary>
        /// Index Edit position to Snap Position - keys are larger tah values
        /// </summary>
        private Dictionary<int, int> mapEditSnapPos = new Dictionary<int, int>();
        /// <summary>
        /// Index Snap position to Edit Position - keys are smaller than values
        /// </summary>
        private Dictionary<int, int> mapSnapEditPos = new Dictionary<int, int>();
        private ITextEditWinFormsUIContext _snapCtrlContext;


        protected IEmptyMergeFieldCharacterPropertiesManager EmptyMergeFieldCharacterPropertiesMgr { get { return _snapCtrlContext.GetManager<IEmptyMergeFieldCharacterPropertiesManager>(); } }
        protected ICustomFieldEditManager CustomFieldEditMgr { get { return _snapCtrlContext.GetManager<ICustomFieldEditManager>(); } }
        protected IPermissionManager PermissionManager { get { return _snapCtrlContext.GetManager<IPermissionManager>(); } }

        public DragonDictationHelper()
        {

        }

        public Field HandleWorkFieldSelection(ITextEditWinFormsUIContext controlContext, Field snapField, string replaceText)
        {
            _snapCtrlContext = controlContext;

            var FieldCheck = snapField;

            if (true == CustomFieldEditMgr?.IsEmptyEntityField(FieldCheck, DocumentEntityTypes.EditField))
            {
                var subDocument = _snapCtrlContext.Document;
                if (PermissionManager.IsDocumentFieldEditable(FieldCheck))
                {
                    DocumentRange rangeToReplace = FieldCheck.ResultRange;

                    if (EmptyMergeFieldCharacterPropertiesMgr != null)
                        EmptyMergeFieldCharacterPropertiesMgr.AddEmptyMergeFieldsCharacterProperties(FieldCheck, subDocument, subDocument.CreateRange(rangeToReplace.Start, 1));

                    var subDocumentUpdate = rangeToReplace.BeginUpdateDocument();
                    try
                    {
                        subDocumentUpdate.Replace(rangeToReplace, replaceText);
                    }
                    finally
                    {
                        rangeToReplace.EndUpdateDocument(subDocumentUpdate);
                    }
                    

                    //selection needs to be removed
                    _snapCtrlContext.Document.Selection = subDocument.CreateRange(FieldCheck.ResultRange.Start, 0);

                    //caret position does NOT need to be set
                    //this.snapCtrl.Document.CaretPosition = fieldsCol[0].ResultRange.Start;

                    //if (EmptyMergeFieldCharacterPropertiesMgr != null)
                    //    FontTools.ReplaceFontPropertiesForField(_snapCtrlContext.Document, FieldCheck, _snapCtrlContext.Document.CaretPosition, EmptyMergeFieldCharacterPropertiesMgr, _snapCtrlContext.IsSetup);

                    //Not neeeded - Key should be normally handled by the Snap Control
                    //snapSubDocument.InsertText(fieldsCol[0].ResultRange.Start, chr.ToString());
                    return FieldCheck;
                }
            }
            return null;
        }

        private int SnapMaxPos;
        private int EditMaxPos;
        /// <summary>
        /// Mapping Snap to Edit postions and vice versa
        /// This code is to simulate TextEdit like behavior in SnapControl
        /// The problem is caused by SnapControl counting new line characters as single position, but TextEdit counts as two.
        /// So the solution for this issue is remapping positions for example we have text <code>'Test text\r\nNew Line'</code>
        /// In Edit control text has positions:
        /// <code>
        /// Sample text:    T   e   s   t       t   e   x   t   \r  \n  N   e   w       l   i   n   e
        /// Edit Control:   0   1   2   3   4   5   6   7   8   9   10  11  12  13  14  15  16  17  18
        /// Snap Control:   0   1   2   3   4   5   6   7   8   9   9   10  11  12  13  14  15  16  17
        /// </code>
        /// <para>Note that on Enter there is number gap wich makes Edit Control postion out of sync with Snap Control and more lines makes gap worsen.</para>
        /// So mapping essentially tells Dragon current position in Edit control terms but navigates internally in Snap control positions.
        /// <para>When Dragon asks for positions application gets Snap control position then finds edit position based on snap postion
        /// In this case for snap position of 9 edit is the same, but for position 15 edit postion is 16 and so on.</para>
        /// <para>When Dragon request changes it tells in Edit position, so we need to obtain correct snap position for example
        /// for postion 9 returns 9 but for position 15 returns 14.</para>
        /// </summary>
        /// <param name="textSection">Text content on which Dragon dication is working</param>
        public Tuple<Dictionary<int, int>, Dictionary<int, int>> MapTextPositions(string textSection, bool append = false)
        {
            var dictEditPosData = new Dictionary<int, int>();
            var dictSnapPosData = new Dictionary<int, int>();
            string[] lineparts = textSection.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            if (lineparts.Length > 0)
            {
                int sumTextEdit = 0;
                int sumTextSnap = 0;

                for (int indexLine = 0; indexLine < lineparts.Length; indexLine++)
                {
                    string lineText = lineparts[indexLine];
                    for (int charIdx = 0; charIdx < lineText.Length; charIdx++)
                    {
                        dictEditPosData[charIdx + sumTextEdit] = charIdx + sumTextSnap;
                        dictSnapPosData[charIdx + sumTextSnap] = charIdx + sumTextEdit;
                    }
                    if (!append || indexLine != lineparts.Length - 1)
                    {
                        dictEditPosData[lineText.Length + sumTextEdit] = lineText.Length + sumTextSnap;
                        dictEditPosData[lineText.Length + sumTextEdit + 1] = lineText.Length + sumTextSnap;
                        dictSnapPosData[lineText.Length + sumTextSnap] = lineText.Length + sumTextEdit;
                    }
                    sumTextEdit += lineText.Length + 2;
                    sumTextSnap += lineText.Length + 1;
                }
            }
            if (!append)
            {
                mapEditSnapPos = dictEditPosData;
                mapSnapEditPos = dictSnapPosData;
                if (mapEditSnapPos.Count > 0)
                {
                    EditMaxPos = mapEditSnapPos.Max(check => check.Key) + 1;
                }
                if (mapSnapEditPos.Count > 0)
                {
                    SnapMaxPos = mapSnapEditPos.Max(check => check.Key) + 1;
                }
            }
            if (log.IsDebugEnabled)
            {
                int diffValue = 0;
                log.DebugFormat("Mapping Edit -> Snap {0}", mapEditSnapPos.AsEnumerable().Aggregate(new StringBuilder(), (x, y) =>
                {
                    if (y.Key - y.Value > diffValue)
                    {
                        x.Append("Diff:").Append(++diffValue).Append(" at:").Append(y).Append(" ");
                    }
                    return x;
                }).ToString());
                diffValue = 0;
                log.DebugFormat("Mapping Snap -> Edit {0}", mapSnapEditPos.AsEnumerable().Aggregate(new StringBuilder(), (x, y) =>
                {
                    if (y.Value - y.Key > diffValue)
                    {
                        x.Append("Diff:").Append(++diffValue).Append(" at:").Append(y).Append(" ");
                    }
                    return x;
                }).ToString());
            }

            return new Tuple<Dictionary<int, int>, Dictionary<int, int>>(dictEditPosData, dictSnapPosData);
        }

        public void MapTextPositions(List<Tuple<string, int>> textSections)
        {
            int snapTextOffset = 0;
            int editTextOffset = 0;
            var dictEditToSnapPosData = new Dictionary<int, int>();
            var dictSnapToEditPosData = new Dictionary<int, int>();
            foreach (var tuples in textSections)
            {
                var mapping = MapTextPositions(tuples.Item1, true);
                foreach (var mapEdits in mapping.Item1)
                {
                    dictEditToSnapPosData.Add(mapEdits.Key + editTextOffset, mapEdits.Value + snapTextOffset);
                }
                foreach (var mapEdits in mapping.Item2)
                {
                    dictSnapToEditPosData.Add(mapEdits.Key + snapTextOffset, mapEdits.Value + editTextOffset);
                }
                if (tuples.Item2 > 0 && !String.IsNullOrEmpty(tuples.Item1) && dictSnapToEditPosData.Count > 0)                                               // TODO : review this code because make errors???
                {
                    int editValue = dictSnapToEditPosData.Max(cmp => cmp.Value) + 1;
                    int snapValue = dictSnapToEditPosData.Max(cmp => cmp.Key) + 1;
                    for (int index = 0; index < tuples.Item2; index++)
                    {
                        dictSnapToEditPosData.Add(snapValue + index, editValue);
                    }
                }
                else if (tuples.Item2 > 0)
                {
                    int editValue = 1;
                    int snapValue = 1;
                    if (dictSnapToEditPosData.Count > 0)
                    {
                        editValue = dictSnapToEditPosData.Max(cmp => cmp.Value) + 1;
                        snapValue = dictSnapToEditPosData.Max(cmp => cmp.Key) + 1;
                    }
                    for (int index = 0; index < tuples.Item2; index++)
                    {
                        dictSnapToEditPosData.Add(snapValue + index, editValue);
                    }
                }
                if (mapping.Item1.Count > 0)
                {
                    editTextOffset = editTextOffset + mapping.Item1.Max(cmp => cmp.Key) + 1;
                    snapTextOffset = snapTextOffset + mapping.Item1.Max(cmp => cmp.Value) + tuples.Item2 + 1;
                }
                else if (tuples.Item2 > 0)
                {
                    snapTextOffset = snapTextOffset + tuples.Item2 + 1;
                    editTextOffset = editTextOffset + 1;
                }
            }
            mapEditSnapPos = dictEditToSnapPosData;
            mapSnapEditPos = dictSnapToEditPosData;
            if (mapEditSnapPos.Count > 0)
            {
                EditMaxPos = mapEditSnapPos.Max(check => check.Key) + 1;
            }
            if (mapSnapEditPos.Count > 0)
            {
                SnapMaxPos = mapSnapEditPos.Max(check => check.Key) + 1;
            }
        }

        public int EditToSnap(int editPos, [System.Runtime.CompilerServices.CallerMemberName] string CallMethod = "", [System.Runtime.CompilerServices.CallerLineNumber] int LineNumber = 0)
        {
            int snapPos;
            if (!mapEditSnapPos.TryGetValue(editPos, out snapPos))
            {
                if (editPos >= EditMaxPos)
                {
                    if (editPos == EditMaxPos)
                    {
                        log.InfoFormat("EditToSnap: Edit Pos just past max character count : {0} from {1}, at:{2}", editPos, CallMethod, LineNumber);
                        snapPos = SnapMaxPos;

                    }
                    else
                    {
                        log.InfoFormat("EditToSnap: Edit Pos too large : {0} and max is {1} from {2}, at:{3}", editPos, EditMaxPos, CallMethod, LineNumber);
                        snapPos = SnapMaxPos;
                    }
                }
                else
                {
                    log.InfoFormat("EditToSnap: Unable get Snap position from Edit : {0} called from {1}, at:{2}", editPos, CallMethod, LineNumber);
                    snapPos = editPos;
                }
            }
            return snapPos;
        }

        public int SnapToEdit(int snapPos, [System.Runtime.CompilerServices.CallerMemberName] string CallMethod = "", [System.Runtime.CompilerServices.CallerLineNumber] int LineNumber = 0)
        {
            int editPos;
            if (!mapSnapEditPos.TryGetValue(snapPos, out editPos))
            {
                if (snapPos >= SnapMaxPos)
                {
                    if (snapPos == SnapMaxPos)
                    {
                        log.InfoFormat("SnapToEdit: Just past snap text range: {0} called from: {1}, at:{2}", snapPos, CallMethod, LineNumber);
                        editPos = EditMaxPos;
                    }
                    else
                    {
                        log.InfoFormat("SnapToEdit: Snap post too large than text range: {0} called from: {1}, at:{2}", snapPos, CallMethod, LineNumber);
                        editPos = EditMaxPos;
                    }
                }
                else
                {
                    log.InfoFormat("SnapToEdit: Unable get Edit position from Snap: {0} called from: {1}, at:{2}", snapPos, CallMethod, LineNumber);
                    editPos = snapPos;
                }
            }
            return editPos;
        }

        public static Field GetFieldFromPosInRange(int cursorPosition, SnapSubDocument document, DocumentRange documentRange)
        {
            var allEntities = SnapFieldTools.GetFieldsInRange(document, documentRange);
            foreach (var snapField in allEntities)
            {
                var result = snapField.CompareFieldRange(cursorPosition);
                switch (result)
                {
                    case 0:
                    {
                        return snapField;   // found field in range
                    }
                    case -1:
                    {
                        return null;        // fields after cursor can be ignored
                    }
                    default:
                        break;
                }
            }
            return null;
        }

        public static int getLineFromText(string textCharacters, int position)
        {
            String[] lineparts = textCharacters.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            if (textCharacters.Length < position)
            {
                return lineparts.Length - 1;
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
                return lineCounter;
            }
            return 0;
        }

        public static bool IsRangeVisible(SnapControl control, DocumentRange range)
        {
            return IsPositionVisible(control, range.Start);
        }
        private static bool IsPositionVisible(SnapControl snapControl, DocumentPosition position)
        {
            Rectangle bounds = snapControl.GetLayoutPhysicalBoundsFromPosition(position);
            if (bounds == Rectangle.Empty)
                return false;
            Rectangle viewBounds = ((DevExpress.XtraRichEdit.IRichEditControl)snapControl).ViewBounds;
            bounds.Offset(viewBounds.X, viewBounds.Y);
            return viewBounds.Contains(bounds.Left, bounds.Top) && viewBounds.Contains(bounds.Right, bounds.Bottom);
        }

        public static void ScrollRangeToVisible(SnapControl snapControl, Tuple<int, int> range)
        {
            Rectangle bounds = snapControl.GetLayoutPhysicalBoundsFromPosition(snapControl.Document.CreatePosition(range.Item1));
            if (bounds == Rectangle.Empty)
            {
                log.Debug("Invalid Document Range");
                return;
            }
            Rectangle viewBounds = ((DevExpress.XtraRichEdit.IRichEditControl)snapControl).ViewBounds;
            int horizontalDiff = viewBounds.X - bounds.X;
            snapControl.VerticalScrollValue = -horizontalDiff;
        }

        public static int getCharLineIndex(string textCharacters, int lineIndex)
        {
            String[] lineparts = textCharacters.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            if (textCharacters.Length <= lineIndex)
            {
                return -1;
            }
            else if (lineparts.Length > 0)
            {
                int stringTotal = 0;
                var lineTextCounter = lineparts.Take(lineIndex);
                foreach (var textPart in lineTextCounter)
                {
                    stringTotal += textPart.Length + 1;
                };
                return stringTotal;
            }
            return -1;
        }


    }
}

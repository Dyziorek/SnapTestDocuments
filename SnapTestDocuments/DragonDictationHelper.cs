using DevExpress.Snap;
using DevExpress.XtraRichEdit.API.Native;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace SnapTestDocuments
{
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

        private int SnapMaxPos;
        private int EditMaxPos;
        /// <summary>
        /// Mapping Snap to Edit postions and vice versa
        /// This code is to simulate TextEdit like behavior in SnapControl
        /// The problem is caused by SnapControl counting new line characters as single position, but TextEdit counts as two.
        /// So the solution for this issue is remapping positions for example we have text 'Test text\r\nNew Line'
        /// In Edit control text has positions:
        /// Sample text:    T   e   s   t       t   e   x   t   \r  \n  N   e   w       l   i   n   e
        /// Edit Control:   0   1   2   3   4   5   6   7   8   9   10  11  12  13  14  15  16  17  18
        /// Snap Control:   0   1   2   3   4   5   6   7   8   9   9   10  11  12  13  14  15  16  17
        /// 
        /// Note that on Enter there is number gap wich makes Edit Control postion out of sync with Snap Control and more lines makes gap worsen
        /// So mapping essetially tells Dragon that current postion in Edit control terms but navigates in Snap control positions.
        /// When Dragon asks for positions application gets Snap control position then finds edit position based on snap postion
        /// In this case for snap position of 9 edit is the same, but for position 15 edit postion is 16 and so on.
        /// When Dragon request changes it tells in Edit position, so we need to obtain correct snap position for example
        /// for postion 9 returns 9 but for position 15 returns 14.
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
                EditMaxPos = mapEditSnapPos.Max(check => check.Key) + 1;
                SnapMaxPos = mapSnapEditPos.Max(check => check.Key) + 1;
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
            var dictEditPosData = new Dictionary<int, int>();
            var dictSnapPosData = new Dictionary<int, int>();
            foreach (var tuples in textSections)
            {
                var mapping = MapTextPositions(tuples.Item1, true);
                foreach(var mapEdits in mapping.Item1)
                {
                    dictEditPosData.Add(mapEdits.Key + editTextOffset, mapEdits.Value + snapTextOffset);
                }
                foreach (var mapEdits in mapping.Item2)
                {
                    dictSnapPosData.Add(mapEdits.Key + snapTextOffset, mapEdits.Value + editTextOffset);
                }
                if (tuples.Item2 > 0 && !string.IsNullOrEmpty(tuples.Item1))                                               // TODO : review this code because make errors???
                {
                    int editValue = dictSnapPosData.Max(cmp => cmp.Value) + 1;
                    int snapValue = dictSnapPosData.Max(cmp => cmp.Key) + 1;
                    for(int index = 0; index < tuples.Item2; index++)
                    {
                        dictSnapPosData.Add(snapValue + index, editValue);
                    }
                }

                if (mapping.Item1.Count > 0)
                {
                    editTextOffset = editTextOffset + mapping.Item1.Max(cmp => cmp.Key) + 1;
                    snapTextOffset = snapTextOffset + mapping.Item1.Max(cmp => cmp.Value) + tuples.Item2 + 1;
                }
                else
                {
                    editTextOffset++;
                    snapTextOffset += tuples.Item2 + 1;
                }
            }
            mapEditSnapPos = dictEditPosData;
            mapSnapEditPos = dictSnapPosData;
            EditMaxPos = mapEditSnapPos.Max(check => check.Key) + 1;
            SnapMaxPos = mapSnapEditPos.Max(check => check.Key) + 1;
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
                        log.InfoFormat("EditToSnap: Edit Pos too large : {0} and max is {1} from {2}, at:{3}", editPos, EditMaxPos,  CallMethod, LineNumber);
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

        public int SnapToEdit(int snapPos, [System.Runtime.CompilerServices.CallerMemberName] string CallMethod = "",  [System.Runtime.CompilerServices.CallerLineNumber] int LineNumber = 0)
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

        public bool HandleWorkFieldSelection(SnapControl snapControl, Field fieldEdited, string replaceText)
        {
            SnapControl _snapCtrlContext = snapControl;
            if (fieldEdited.CodeRange.Length == 56)
            {
                Field field = _snapCtrlContext.Document.Fields.First();
                
                var rangeToReplace = field.ResultRange;

                var subDocumentUpdate = rangeToReplace.BeginUpdateDocument();
                try
                {
                    subDocumentUpdate.BeginUpdate();
                    subDocumentUpdate.Replace(rangeToReplace, replaceText);
                }
                finally
                {
                    subDocumentUpdate.EndUpdate();
                    rangeToReplace.EndUpdateDocument(subDocumentUpdate);
                }
                //selection needs to be removed
                //_snapCtrlContext.Document.Selection = subDocument.CreateRange(field.ResultRange.Start, 0);

                //caret position does NOT need to be set
                //_snapCtrlContext.Document.CaretPosition = _snapCtrlContext.Document.CreatePosition(field.ResultRange.End.Position - 1);


                //Not neeeded - Key should be normally handled by the Snap Control
                //snapSubDocument.InsertText(fieldsCol[0].ResultRange.Start, chr.ToString());
                return true;

            }
            return false;
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


        public static int getCharLineIndex(string textCharacters, int lineIndex)
        {
            String[] lineparts = textCharacters.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            if (textCharacters.Length <= lineIndex)
            {
                return - 1;
            }
            else if (lineparts.Length > 0)
            {
                int stringTotal = 0;
                var lineTextCounter = lineparts.Take(lineIndex); 
                foreach(var textPart in lineTextCounter)
                {
                    stringTotal += textPart.Length + 1;
                };
                return stringTotal;
            }
            return -1;
        }

        public static bool IsPositionVisible(SnapControl snapControl, DocumentPosition position)
        {
            Rectangle bounds = snapControl.GetLayoutPhysicalBoundsFromPosition(position);
            if (bounds == Rectangle.Empty)
            {
                log.Info("IsPositionVisible: Invalid Document Range");
                return false;
            }
            Rectangle viewBounds = ((DevExpress.XtraRichEdit.IRichEditControl)snapControl).ViewBounds;
            bounds.Offset(viewBounds.X, viewBounds.Y);
            log.InfoFormat("IsPositionVisible: result {0}", viewBounds.Contains(bounds.Left, bounds.Top) && viewBounds.Contains(bounds.Right, bounds.Bottom));
            return viewBounds.Contains(bounds.Left, bounds.Top) && viewBounds.Contains(bounds.Right, bounds.Bottom);
        }

        public static void ScrollRangeToVisible(SnapControl snapControl, DocumentRange range)
        {
            Rectangle bounds = snapControl.GetLayoutPhysicalBoundsFromPosition(range.Start);
            if (bounds == Rectangle.Empty)
            {
                log.Info("ScrollRangeToVisible: Invalid Document Range");
                return;
            }
            Rectangle viewBounds = ((DevExpress.XtraRichEdit.IRichEditControl)snapControl).ViewBounds;
            int horizontalDiff = viewBounds.X - bounds.X;
            log.InfoFormat("Scroll Document to distance {0}", horizontalDiff);
            snapControl.VerticalScrollValue += horizontalDiff;
        }
    }
}

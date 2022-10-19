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
    public class FieldLinkedList : LinkedList<Field>
    {
        public FieldLinkedList(ICollection<Field> items) : base(items)
        { }

        public override string ToString()
        {
            var counterEnum = GetEnumerator();
            StringBuilder fullPack = new StringBuilder();
            fullPack.Append("(").AppendLine();
            while (counterEnum.MoveNext())
            {
                if (counterEnum.Current != null)
                {
                    if (counterEnum.Current.Parent != null)
                    {
                        fullPack.AppendLine(counterEnum.Current != null ? "(" + counterEnum.Current.Range.Start.ToString() + ", " + counterEnum.Current.Range.End.ToString() + ")" + "{" + counterEnum.Current.ResultRange.Start.ToString() + ", " + counterEnum.Current.ResultRange.End.ToString() + "}" + " ---> " + "(" + counterEnum.Current.Parent.Range.Start.ToString() + ", " + counterEnum.Current.Parent.Range.End.ToString() + ")" + "{" + counterEnum.Current.Parent.ResultRange.Start.ToString() + ", " + counterEnum.Current.Parent.ResultRange.End.ToString() + "}" : "[data null]");
                    }
                    else
                    {
                        fullPack.AppendLine(counterEnum.Current != null ? "(" + counterEnum.Current.Range.Start.ToString() + ", " + counterEnum.Current.Range.End.ToString() + ")" + "{" + counterEnum.Current.ResultRange.Start.ToString() + ", " + counterEnum.Current.ResultRange.End.ToString() + "}" : "[data null]");
                    }
                }
            }
            fullPack.Append(")").AppendLine();
            return fullPack.ToString();

        }


    }


    public enum FieldLocationType
    {
        BeforeField,
        InsideField,
        AfterField
    }

    public static class DragonDictationTools
    {
        public static int SnapLengthText(this StringBuilder textSection)
        {
            var countLines = textSection.ToString().Count(varCheck =>
            {
                if (varCheck == '\r')
                {
                    return true;
                }
                return false;
            });

            return textSection.Length - countLines;
        }

        public static List<Tuple<string, int>> GetStringParts(this List<FieldTreeNode> cacheFields)
        {
            List<Tuple<string, int>> stringElements = new List<Tuple<string, int>>();
            foreach (FieldTreeNode fieldLook in cacheFields)
            {
                stringElements.AddRange(fieldLook.GetCachedStringData());
            }

            return stringElements;
        }

        public static FieldTreeNode ToNodeField(this List<FieldTreeNode> cacheFields, Field fieldNode)
        {
            foreach (FieldTreeNode fieldLook in cacheFields)
            {
                if (fieldNode.Equals(fieldLook.Data))
                {
                    return fieldLook;
                }
                var childElems = fieldLook.Children.Where(elemCheck => elemCheck.Data.Equals(fieldNode));
                if (childElems.Count() == 1)
                {
                    return childElems.First();
                }
            }
            return null;
        }

        public static FieldTreeNode GetNextNodeField(this List<FieldTreeNode> localFields, FieldTreeNode checkingField)
        {
            int minimalDistance = int.MaxValue;
            FieldTreeNode closestField = null;
            int selectionPosition = checkingField.Data.ResultRange.End.ToInt();
            foreach (FieldTreeNode fieldLook in localFields)
            {
                int fieldCheckPosition = fieldLook.Data.ResultRange.End.ToInt();
                var distance = Math.Abs(selectionPosition - fieldCheckPosition);
                if (distance < minimalDistance && (selectionPosition - fieldCheckPosition) < 0)
                {
                    closestField = fieldLook;
                    minimalDistance = distance;
                    var childField = fieldLook.closestNextNodeField(selectionPosition, ref minimalDistance);
                    if (childField != null)
                    {
                        closestField = childField;
                    }
                }
            }

            return closestField;
        }

        public static FieldTreeNode GetPreviousNodeField(this List<FieldTreeNode> localFields, FieldTreeNode checkingField)
        {
            if (localFields.First().Data.Equals(checkingField))
            {
                return null;
            }

            int minimalDistance = int.MaxValue;
            FieldTreeNode closestField = null;
            int selectionPosition = checkingField.Data.ResultRange.End.ToInt();
            foreach (FieldTreeNode fieldLook in localFields)
            {
                int fieldCheckPosition = fieldLook.Data.ResultRange.End.ToInt();
                var distance = Math.Abs(selectionPosition - fieldCheckPosition);
                if (distance < minimalDistance && !fieldLook.Data.Equals(checkingField))
                {
                    closestField = fieldLook;
                    minimalDistance = distance;
                    var childField = fieldLook.closestNodeField(selectionPosition, ref minimalDistance, true);
                    if (childField != null)
                    {
                        closestField = childField;
                    }
                }
                else if (fieldLook.Data.Equals(checkingField))
                {
                    return closestField;
                }
            }

            return closestField;
        }

        public static FieldTreeNode GetNearestNodeField(this List<FieldTreeNode> localFields, int selectionPosition, bool fromEnd)
        {
            int minimalDistance = int.MaxValue;
            FieldTreeNode closestField = null;
            foreach (FieldTreeNode fieldLook in localFields)
            {
                int fieldCheckPosition = fieldLook.Data.ResultRange.End.ToInt();
                if (!fromEnd)
                {
                    fieldCheckPosition = fieldLook.Data.ResultRange.Start.ToInt();
                }
                var distance = Math.Abs(selectionPosition - fieldCheckPosition);
                if (distance < minimalDistance)
                {
                    closestField = fieldLook;
                    minimalDistance = distance;
                    var childField = fieldLook.closestNodeField(selectionPosition, ref minimalDistance, fromEnd);
                    if (childField != null)
                    {
                        closestField = childField;
                    }
                }
                else
                {
                    return closestField;
                }
            }

            return closestField;
        }

        public static FieldTreeNode GetNeighborField(this List<FieldTreeNode> fields, Field locationField)
        {
            int minimalDistance = int.MaxValue;
            int referenceLocation = locationField.ResultRange.End.ToInt();
            FieldTreeNode closestField = null;
            foreach (FieldTreeNode fieldLook in fields)
            {
                int fieldCheckPosition = fieldLook.Data.ResultRange.End.ToInt();
                var distance = Math.Abs(referenceLocation - fieldCheckPosition);
                if (distance < minimalDistance && !fieldLook.Data.Equals(locationField))
                {
                    closestField = fieldLook;
                    minimalDistance = distance;
                    var childField = fieldLook.ClosestNeighborNodeField(referenceLocation, ref minimalDistance, locationField);
                    if (childField != null)
                    {
                        closestField = childField;
                    }
                }
            }

            return closestField;
        }

        public static KeyValuePair<String, String> IsCacheValid(this List<FieldTreeNode> fields, string controlText, IEnumerable<int> sectionParagraphs)
        {

            int fieldOffsets = 0;
            var textSections = fields.GetStringParts();

            StringBuilder textBuilder = new StringBuilder();
            for (int index = 0; index < textSections.Count; index++)
            {
                var sectionOffseted = sectionParagraphs.Select(sectionOffsetPart => sectionOffsetPart -= fieldOffsets).ToList();
                if (textSections[index].Item2 > 0)
                {
                    fieldOffsets += textSections[index].Item2;
                }
                var strTextPart = textSections[index].Item1;
                textBuilder.Append(strTextPart);
                if (sectionOffseted.Contains(textBuilder.SnapLengthText() + 1))
                {
                    strTextPart += "\r\n";
                    textBuilder.AppendLine();
                    textSections[index] = new Tuple<string, int>(strTextPart, textSections[index].Item2);
                }
            }

            //var comparisonEqual = controlText?.CompareTo(textBuilder.ToString());
            //return comparisonEqual.HasValue ? comparisonEqual == 0 : false;
            return new KeyValuePair<string, string>(controlText, textBuilder.ToString());
        }

        public static int NodeCount(this List<FieldTreeNode> fieldTreeNodes)
        {
            return fieldTreeNodes.Sum(elem => elem.AllChildren.Count + 1);
        }

        public static void ArrangeFields(this List<FieldTreeNode> baseFields, FieldLinkedList fields)
        {
            var fieldElem = fields.First;
            baseFields.Clear();
            baseFields.Add(new FieldTreeNode(fieldElem.Value));
            while (fieldElem.Next != null)
            {
                fieldElem = fieldElem.Next;
                if (fieldElem.Value.Parent != null)
                {
                    bool found = false;
                    foreach (FieldTreeNode node in baseFields)
                    {
                        if (node.Data.Range.Start == fieldElem.Value.Parent.Range.Start && node.Data.Range.End == fieldElem.Value.Parent.Range.End)
                        {
                            node.AddChild(fieldElem);
                            found = true;
                        }
                        else
                        {
                            var childNode = node.AllChildren.FirstOrDefault(check => check.Data.Range.Start == fieldElem.Value.Parent.Range.Start && check.Data.Range.End == fieldElem.Value.Parent.Range.End);
                            if (childNode != null)
                            {
                                childNode.AddChild(fieldElem);
                                found = true;
                            }
                        }
                    }
                    if (!found)
                    {
                        baseFields.Add(new FieldTreeNode(fieldElem.Value));
                    }
                }
                else
                {
                    baseFields.Add(new FieldTreeNode(fieldElem.Value));
                }
            }
        }

    }

    class DragonDictationHelper
    {
        #region FIELDS
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
        private string textEditImage;
        private List<FieldTreeNode> fieldNodes = new List<FieldTreeNode>();

        private int SnapMaxPos;
        private int EditMaxPos;

        protected ISelectionChangedTrackingManager SelectionTrackingMgr { get { return _snapCtrlContext.GetManager<ISelectionChangedTrackingManager>(); } }
        protected IEmptyMergeFieldCharacterPropertiesManager EmptyMergeFieldCharacterPropertiesMgr { get { return _snapCtrlContext.GetManager<IEmptyMergeFieldCharacterPropertiesManager>(); } }
        protected ICustomFieldEditManager CustomFieldEditMgr { get { return _snapCtrlContext.GetManager<ICustomFieldEditManager>(); } }
        protected IPermissionManager PermissionManager { get { return _snapCtrlContext.GetManager<IPermissionManager>(); } }



        #endregion FIELDS
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
                    var rangeToReplace = FieldCheck.ResultRange;

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

                    //if (EmptyMergeFieldCharacterPropertiesMgr != null)
                    //    FontTools.ReplaceFontPropertiesForField(_snapCtrlContext.Document, FieldCheck, _snapCtrlContext.Document.CaretPosition, EmptyMergeFieldCharacterPropertiesMgr, _snapCtrlContext.IsSetup);

                    return FieldCheck;
                }
            }
            return null;
        }


        public void AnalyzeTextSection(ITextEditWinFormsUIContext controlContext, SnapControl control, DocumentRange sectionPart, string sectionText, IEnumerable<int> paragraphsCollection, Tuple<int, int> lastSelect)
        {
            var fieldRange = SnapFieldTools.GetFieldsInRange(control.Document, sectionPart).ToList();
            var paragraphPositions = paragraphsCollection.ToList();
            fieldRange.Sort((field1, field2) => field1.Range.Start.ToInt().CompareTo(field2.Range.Start.ToInt()));

            if (fieldRange.Count() != fieldNodes.NodeCount())
            {
                ResetMappings();
            }
            if (fieldRange.Count() > 0)
            {
                var cacheInfo = fieldNodes.IsCacheValid(sectionText, paragraphPositions);
                if (fieldRange.Count() != fieldNodes.NodeCount())
                {
                    var stringParts = BuildFieldCache(fieldNodes, fieldRange, control, sectionPart);

                    var cachedStringParts = fieldNodes.GetStringParts();

                    if (log.IsDebugEnabled)
                    {
                        log.DebugFormat("Generated string count {0} cached count {1} Equals {2}", stringParts.Count, cachedStringParts.Count, Enumerable.SequenceEqual(cachedStringParts, stringParts));
                    }
                    MapTextPositions(stringParts, paragraphPositions, sectionText, control, sectionPart.Start.ToInt());
                }
                else if (String.Compare(cacheInfo.Key, cacheInfo.Value) != 0)
                {
                    var fieldNode = GetNearestNodeFieldFromPosition(controlContext, lastSelect.Item1);
                    switch (fieldNode.Item2)
                    {
                        case FieldLocationType.BeforeField:
                            Field previousField = fieldNodes.GetPreviousNodeField(fieldNode.Item1).Data;
                            int initialRange = sectionPart.Start.ToInt();
                            if (previousField != null)
                            {
                                initialRange = previousField.Range.End.ToInt();
                            }
                            Field workingField = fieldNode.Item1.Data;
                            var fieldPrefix = new Tuple<string, int>(control.Document.GetText(control.Document.CreateRange(initialRange, workingField.Range.Start.ToInt() - initialRange)), workingField.CodeRange.Length + 2);
                            fieldNodes.Last().UpdateField(fieldPrefix, null, null);
                            break;
                        case FieldLocationType.InsideField:
                            var fieldText = new Tuple<string, int>(control.Document.GetText(fieldNode.Item1.Data.ResultRange), 1);
                            fieldNodes.Last().UpdateField(null, fieldText, null);
                            break;
                        case FieldLocationType.AfterField:
                        default:
                            FieldTreeNode nextField = fieldNodes.GetNextNodeField(fieldNode.Item1);
                            int endRange = sectionPart.End.ToInt();
                            if (nextField != null)
                            {
                                endRange = fieldNode.Item1.Data.Range.End.ToInt();
                                var fieldprefix = new Tuple<string, int>(control.Document.GetText(control.Document.CreateRange(endRange, nextField.Data.Range.Start.ToInt() - endRange)), nextField.Data.CodeRange.Length + 2);
                                nextField.UpdateField(fieldprefix, null, null);
                            }
                            else
                            {
                                var fieldSuffix = new Tuple<string, int>((control.Document.GetText(control.Document.CreateRange(fieldNode.Item1.Data.Range.End.ToInt(), endRange - fieldNode.Item1.Data.Range.End.ToInt()))), 0);
                                fieldNodes.Last().UpdateField(null, null, fieldSuffix);
                            }
                            break;
                    }
                    var updateData = fieldNodes.GetStringParts();

#if DEBUG
                    var cacheCompare = fieldNodes.IsCacheValid(sectionText, paragraphPositions);
                    if (string.Compare(cacheInfo.Key, cacheInfo.Value) != 0)
                    {
                        log.DebugFormat("Invalid cache result {0}", string.Compare(cacheInfo.Key, cacheInfo.Value));
                        string[] originalTexts = cacheInfo.Key.Split( new String[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                        string[] generatedTexts = cacheInfo.Value.Split(new String[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                        for(int textIdx = 0; textIdx < originalTexts.Length; textIdx++)
                        {
                            if (generatedTexts.Length > textIdx)
                            {
                                if (!originalTexts[textIdx].Equals(generatedTexts[textIdx]))
                                {
                                    log.DebugFormat("Number: {0}, Original Text '{1}', Cache Text '{2}'", textIdx, originalTexts[textIdx], generatedTexts[textIdx]);
                                }
                            }
                            else
                            {
                                log.DebugFormat("Number: {0}, Original Text '{1}', No Cached", textIdx, originalTexts[textIdx]);
                            }
                        }
                        var stringParts = BuildFieldCache(fieldNodes, fieldRange, control, sectionPart);
                    }
#endif
                    MapTextPositions(fieldNodes.GetStringParts(), paragraphPositions, sectionText, control, sectionPart.Start.ToInt());
                }
            }
            else
            {
                MapTextPositions(sectionText);
            }
        }

        public List<Tuple<string, int>> BuildFieldCache(List<FieldTreeNode> nodesCache, List<Field> fieldsToCache, SnapControl control, DocumentRange sectionPart)
        {
            fieldNodes.ArrangeFields(new FieldLinkedList(fieldsToCache));
            var stringParts = new List<Tuple<string, int>>();
            int initialRange = sectionPart.Start.ToInt();
            foreach (var fieldElement in fieldNodes)
            {
                if (fieldElement.IsLeaf)
                {
                    // no nested fields
                    var fieldData = fieldElement.Data;
                    if (fieldData.ResultRange.Start.ToInt() >= initialRange)
                    {
                        // text before (or between fields) 
                        var fieldprefix = new Tuple<string, int>(control.Document.GetText(control.Document.CreateRange(initialRange, fieldData.Range.Start.ToInt() - initialRange)), fieldData.CodeRange.Length + 2);
                        stringParts.Add(fieldprefix);

                        // text inside field
                        var fieldText = new Tuple<string, int>(control.Document.GetText(fieldData.ResultRange), 1);
                        stringParts.Add(fieldText);
                        initialRange = fieldData.Range.End.ToInt();
                        fieldElement.UpdateField(fieldprefix, fieldText, null);
                    }
                }
                else
                {
                    // field has nested fields inside
                    var fieldData = fieldElement.Data;
                    if (fieldData.ResultRange.Start.ToInt() >= initialRange)
                    {
                        // text before field with nested children
                        var fieldprefix = new Tuple<string, int>(control.Document.GetText(control.Document.CreateRange(initialRange, fieldData.Range.Start.ToInt() - initialRange)), fieldData.CodeRange.Length + 2);
                        stringParts.Add(fieldprefix);
                        initialRange = fieldData.Range.Start.ToInt() + 1;
                        AnalyzeNestedFields(control, fieldElement, ref initialRange, stringParts);
                        initialRange = fieldData.Range.End.ToInt();
                    }
                }

            }
            var fieldSuffix = new Tuple<string, int>((control.Document.GetText(control.Document.CreateRange(initialRange, sectionPart.End.ToInt() - initialRange))), 0);
            fieldNodes.Last().UpdateField(null, null, fieldSuffix);
            stringParts.Add(fieldSuffix);

            log.InfoFormat("String parts count:{0}", stringParts.Count);
            if (log.IsInfoEnabled)
            {
                for (int index = 0; index < stringParts.Count; index++)
                {
                    log.InfoFormat("Part {0} Elements '{1}' , {2}", index, stringParts[index].Item1, stringParts[index].Item2);
                }
            }

            return stringParts;
        }

        private void AnalyzeNestedFields(SnapControl control, FieldTreeNode fieldElement, ref int initialRange, List<Tuple<string, int>> stringParts)
        {

            foreach (var childItem in fieldElement.Children)
            {
                if (!childItem.IsLeaf)
                {
                    AnalyzeNestedFields(control, childItem, ref initialRange, stringParts);
                }
                else
                {
                    var currentChild = childItem.Data;
                    Tuple<string, int> fieldprefix;
                    // check if current field has resultRange text before first nested field
                    if (fieldElement.FirstChild == childItem && fieldElement.Data.ResultRange.Start < fieldElement.FirstChild.Data.ResultRange.Start)
                    {
                        fieldprefix = new Tuple<string, int>(control.Document.GetText(control.Document.CreateRange(fieldElement.Data.ResultRange.Start.ToInt(),
                            fieldElement.FirstChild.Data.Range.Start.ToInt() - fieldElement.Data.ResultRange.Start.ToInt())), currentChild.CodeRange.Length + 2);
                        stringParts.Add(fieldprefix);
                        initialRange = fieldElement.FirstChild.Data.Range.Start.ToInt();
                    }
                    else
                    {
                        fieldprefix = new Tuple<string, int>(control.Document.GetText(control.Document.CreateRange(initialRange, currentChild.Range.Start.ToInt() - initialRange)), currentChild.CodeRange.Length + 2);
                        stringParts.Add(fieldprefix);
                    }
                    var fieldText = new Tuple<string, int>(control.Document.GetText(currentChild.ResultRange), 1);
                    stringParts.Add(fieldText);
                    initialRange = currentChild.Range.End.ToInt();
                    childItem.UpdateField(fieldprefix, fieldText, null);
                }
            }

            if (fieldElement.Data.ResultRange.End > fieldElement.LastChild.Data.ResultRange.End)
            {
                var fieldTextSuffix = new Tuple<string, int>(control.Document.GetText(control.Document.CreateRange(fieldElement.LastChild.Data.Range.End.ToInt(),
                            fieldElement.Data.ResultRange.End.ToInt() - fieldElement.LastChild.Data.Range.End.ToInt())), 1);
                stringParts.Add(fieldTextSuffix);
                fieldElement.UpdateField(null, null, fieldTextSuffix);
                initialRange = fieldElement.Data.Range.End.ToInt();
            }
        }


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
        public Tuple<Dictionary<int, int>, Dictionary<int, int>> MapTextPositions(string textSection, bool append = false, IEnumerable<int> sectionParagraphs = null)
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
                        if (sectionParagraphs != null && sectionParagraphs.Contains(lineText.Length + sumTextEdit + 1))
                        {
                            dictEditPosData[lineText.Length + sumTextEdit] = lineText.Length + sumTextSnap;
                            dictEditPosData[lineText.Length + sumTextEdit + 1] = lineText.Length + sumTextSnap;
                        }
                        else
                        {
                            dictEditPosData[lineText.Length + sumTextEdit] = lineText.Length + sumTextSnap;
                            dictEditPosData[lineText.Length + sumTextEdit + 1] = lineText.Length + sumTextSnap;
                        }
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

        public void ResetMappings()
        {
            mapEditSnapPos = new Dictionary<int, int>();
            mapSnapEditPos = new Dictionary<int, int>();
            fieldNodes = new List<FieldTreeNode>();
        }

        public bool MapTextPositions(List<Tuple<string, int>> textSections, IEnumerable<int> sectionParagraphs, string texControl, SnapControl control, int sectionOffset)
        {
            int snapTextOffset = 0;
            int editTextOffset = 0;
            int fieldOffsets = 0;
            var dictEditToSnapPosData = new Dictionary<int, int>();
            var dictSnapToEditPosData = new Dictionary<int, int>();
            StringBuilder textBuilder = new StringBuilder();
            for (int index = 0; index < textSections.Count; index++)
            {
                var sectionOffseted = sectionParagraphs.Select(sectionOffsetPart => sectionOffsetPart -= fieldOffsets).ToList();
                if (textSections[index].Item2 > 0)
                {
                    fieldOffsets += textSections[index].Item2;
                }
                var strTextPart = textSections[index].Item1;
                textBuilder.Append(strTextPart);
                if (sectionOffseted.Contains(textBuilder.SnapLengthText() + 1))
                {
                    strTextPart += "\r\n";
                    textBuilder.AppendLine();
                    textSections[index] = new Tuple<string, int>(strTextPart, textSections[index].Item2);
                }

            }

            StringBuilder validatorBuilder = new StringBuilder();
            foreach (var sectionTextPart in textSections)
            {
                var textPartCheck = sectionTextPart.Item1;
                validatorBuilder.Append(textPartCheck);
                var mapping = MapTextPositions(textPartCheck, true, null);
                foreach (var mapEdits in mapping.Item1)
                {
                    dictEditToSnapPosData[mapEdits.Key + editTextOffset] = mapEdits.Value + snapTextOffset;
                }
                foreach (var mapEdits in mapping.Item2)
                {
                    dictSnapToEditPosData[mapEdits.Key + snapTextOffset] = mapEdits.Value + editTextOffset;
                }
                if (sectionTextPart.Item2 > 0 && !String.IsNullOrEmpty(sectionTextPart.Item1) && dictSnapToEditPosData.Count > 0)                                               // TODO : review this code because make errors???
                {
                    int editValue = dictSnapToEditPosData.Max(cmp => cmp.Value) + 1;
                    int snapValue = dictSnapToEditPosData.Max(cmp => cmp.Key) + 1;
                    for (int index = 0; index < sectionTextPart.Item2; index++)
                    {
                        dictSnapToEditPosData[snapValue + index] = editValue;
                    }
                }
                else if (sectionTextPart.Item2 > 0)
                {
                    int editValue = 1;
                    int snapValue = 1;
                    if (dictSnapToEditPosData.Count > 0)
                    {
                        editValue = dictSnapToEditPosData.Max(cmp => cmp.Value) + 1;
                        snapValue = dictSnapToEditPosData.Max(cmp => cmp.Key) + 1;
                    }
                    for (int index = 0; index < sectionTextPart.Item2; index++)
                    {
                        dictSnapToEditPosData[snapValue + index] = editValue;
                    }
                }
                if (mapping.Item1.Count > 0)
                {
                    editTextOffset = editTextOffset + mapping.Item1.Max(cmp => cmp.Key) + 1;
                    snapTextOffset = snapTextOffset + mapping.Item1.Max(cmp => cmp.Value) + sectionTextPart.Item2 + 1;
                }
                else if (sectionTextPart.Item2 > 0)
                {
                    if (dictEditToSnapPosData.Count > 0)
                    {
                        snapTextOffset = snapTextOffset + sectionTextPart.Item2 + 1;
                        editTextOffset = editTextOffset + 1;
                    }
                    else
                    {
                        snapTextOffset = snapTextOffset + sectionTextPart.Item2;
                    }
                }

                if (validatorBuilder.Length != editTextOffset)
                {
                    log.InfoFormat("Mapping data has errors  textLen:{0}, editMax:{1}", validatorBuilder.Length, editTextOffset);
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

            if (true == sectionParagraphs?.Contains(SnapMaxPos - 1))
            {
                dictEditToSnapPosData[EditMaxPos] = SnapMaxPos;
                dictEditToSnapPosData[EditMaxPos + 1] = SnapMaxPos;

                dictSnapToEditPosData[SnapMaxPos] = EditMaxPos;
                SnapMaxPos++;
                EditMaxPos++;
                textSections[textSections.Count - 1] = new Tuple<string, int>(textSections.Last().Item1 + "\r\n", textSections.Last().Item2);
            }


            textEditImage = textBuilder.ToString();
            var textImageSize = textEditImage.Length;

            if (log.IsInfoEnabled)
            {
                int compOP = texControl.CompareTo(textEditImage);
                log.InfoFormat("Comparsion sectionText:'{0}'  textParts:'{1}' - result {2}", texControl, textEditImage, compOP);
                log.InfoFormat("length sectionTextParts:{0}  texFromRange:{1}, maxEditPos {2}", textImageSize, texControl.Length, EditMaxPos);
            }

            if (log.IsDebugEnabled)
            {
                // this is very slow debug diagnostics code, use this only for error investigation
                if (textEditImage.Length < EditMaxPos + 1)
                {
                    textEditImage = textEditImage.PadRight(EditMaxPos + 1, '`');
                }

                if (texControl.Length < EditMaxPos + 1)
                {
                    texControl = texControl.PadRight(EditMaxPos + 1, '`');
                }

                if (mapSnapEditPos.Max(check => check.Value) + 1 > EditMaxPos)
                {
                    log.InfoFormat("Found wrong EditPos {0}", mapSnapEditPos.Max(check => check.Value) + 1);
                    var keyList = new List<int>(mapSnapEditPos.Keys);

                    foreach (var keyEelem in keyList)
                    {
                        if (mapSnapEditPos[keyEelem] > EditMaxPos)
                        {
                            mapSnapEditPos[keyEelem] = EditMaxPos;
                        }
                    }
                }

                var keyValItems = mapEditSnapPos.ToArray();
                StringBuilder mapTextBuilderES = new StringBuilder();
                mapTextBuilderES.AppendLine();
                for (int keyIndex = 0; keyIndex < keyValItems.Length; keyIndex++)
                {
                    var charText = textEditImage[keyValItems[keyIndex].Key];
                    var chaVal = Char.IsWhiteSpace(charText) ? String.Format("0x{0:X2}", ((byte)charText)) : String.Format("{0}", charText);
                    var charTextSect = texControl[keyValItems[keyIndex].Key];
                    var chaValSect = Char.IsWhiteSpace(charTextSect) ? String.Format("0x{0:X2}", ((byte)charTextSect)) : String.Format("{0}", charTextSect);
                    mapTextBuilderES.Append("ID:").Append(keyIndex).Append(",E_id:").Append(keyValItems[keyIndex].Key).Append(",E_val:").Append(chaVal);
                    mapTextBuilderES.Append("   ,T_id:").Append(keyValItems[keyIndex].Key).Append(",T_val:").Append(chaValSect);
                    if (log.IsDebugEnabled)
                    {
                        mapTextBuilderES.Append("   ,S_id:").Append(keyValItems[keyIndex].Value).Append(",S_val:").Append(control.Document.GetText(control.Document.CreateRange(keyValItems[keyIndex].Value + sectionOffset, 1))).AppendLine();
                    }
                    else
                    {
                        mapTextBuilderES.AppendLine();
                    }
                }
                log.Info("MapEditSnap: " + mapTextBuilderES.ToString());
                keyValItems = mapSnapEditPos.ToArray();
                StringBuilder mapTextBuilderSE = new StringBuilder();
                mapTextBuilderSE.AppendLine();
                for (int keyIndex = 0; keyIndex < keyValItems.Length; keyIndex++)
                {
                    var charText = textEditImage[keyValItems[keyIndex].Value];
                    var chaVal = Char.IsWhiteSpace(charText) ? String.Format("0x{0:X2}", ((byte)charText)) : String.Format("{0}", charText);
                    var charTextSect = texControl[keyValItems[keyIndex].Value];
                    var chaValSect = Char.IsWhiteSpace(charTextSect) ? String.Format("0x{0:X2}", ((byte)charTextSect)) : String.Format("{0}", charTextSect);
                    mapTextBuilderSE.Append("ID:").Append(keyIndex);
                    if (log.IsDebugEnabled)
                    {
                        mapTextBuilderSE.Append(",S_id:").Append(keyValItems[keyIndex].Key).Append(",S_val:").Append(control.Document.GetText(control.Document.CreateRange(keyValItems[keyIndex].Key + sectionOffset, 1)));
                        mapTextBuilderSE.Append("   ,E_id:").Append(keyValItems[keyIndex].Value).Append(",E_val:").Append(chaVal);
                    }
                    else
                    {
                        mapTextBuilderSE.Append(",E_id:").Append(keyValItems[keyIndex].Value).Append(",E_val:").Append(chaVal);
                    }
                    mapTextBuilderSE.Append("   ,T_id:").Append(keyValItems[keyIndex].Value).Append(",T_val:").Append(chaValSect).AppendLine();
                }
                log.Info("MapSnapEdit: " + mapTextBuilderSE.ToString());
            }

            return textImageSize == EditMaxPos;
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
                        log.DebugFormat("EditToSnap: Edit Pos just past max character count : {0} from {1}, at:{2}", editPos, CallMethod, LineNumber);
                        snapPos = SnapMaxPos;
                    }
                    else
                    {
                        log.DebugFormat("EditToSnap: Edit Pos too large : {0} and max is {1} from {2}, at:{3}", editPos, EditMaxPos, CallMethod, LineNumber);
                        snapPos = SnapMaxPos;
                    }
                }
                else
                {
                    log.DebugFormat("EditToSnap: Unable get Snap position from Edit : {0} called from {1}, at:{2}", editPos, CallMethod, LineNumber);
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
                        log.DebugFormat("SnapToEdit: Just past snap text range: {0} called from: {1}, at:{2}", snapPos, CallMethod, LineNumber);
                        editPos = EditMaxPos;
                    }
                    else
                    {
                        log.DebugFormat("SnapToEdit: Snap post too large than text range: {0} called from: {1}, at:{2}", snapPos, CallMethod, LineNumber);
                        editPos = EditMaxPos;
                    }
                }
                else
                {
                    log.DebugFormat("SnapToEdit: Unable get Edit position from Snap: {0} called from: {1}, at:{2}", snapPos, CallMethod, LineNumber);
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

        public static void ScrollRangeToVisible(SnapControl snapControl, DocumentRange range)
        {
            Rectangle bounds = snapControl.GetLayoutPhysicalBoundsFromPosition(range.Start);
            if (bounds == Rectangle.Empty)
            {
                log.Debug("Invalid Document Range");
                return;
            }
            Rectangle viewBounds = ((DevExpress.XtraRichEdit.IRichEditControl)snapControl).ViewBounds;
            int horizontalDiff = viewBounds.X - bounds.X;
            snapControl.VerticalScrollValue = -horizontalDiff;
        }

        public Tuple<FieldTreeNode, FieldLocationType> GetNearestNodeFieldFromPosition(ITextEditWinFormsUIContext controlContext, int snapPosition, bool fromEnd = true)
        {
            _snapCtrlContext = controlContext;
            var closestField = this.fieldNodes.GetNearestNodeField(snapPosition, fromEnd);
            if (PermissionManager.IsDocumentFieldEditable(closestField.Data))
            {
                var foundField = closestField.Data.CompareFieldRange(snapPosition);
                switch (foundField)
                {
                    case -1:
                        return new Tuple<FieldTreeNode, FieldLocationType>(closestField, FieldLocationType.BeforeField);
                    case 1:
                        return new Tuple<FieldTreeNode, FieldLocationType>(closestField, FieldLocationType.AfterField);
                    case 0:
                    default:
                        return new Tuple<FieldTreeNode, FieldLocationType>(closestField, FieldLocationType.InsideField);

                }
            }
            return null;
        }

        public static int GetCharLineIndex(string textCharacters, int lineIndex)
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

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


    public static class DragonDictationTools
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

        public static void ArrangeFields(this List<FieldTreeNode> baseFields, FieldLinkedList fields, SnapControl control)
        {
            var fieldElem = fields.First;

            string fieldCode = VerifyField(fieldElem.Value, control);
            baseFields.Add(new FieldTreeNode(fieldElem.Value, fieldCode, control.Document.GetText(fieldElem.Value.ResultRange )));
            while (fieldElem.Next != null)
            {
                fieldElem = fieldElem.Next;
                if (fieldElem.Value.Parent != null)
                {
                    bool found = false;
                    foreach(FieldTreeNode node in baseFields)
                    {
                        if (node.Data.Range.Start == fieldElem.Value.Parent.Range.Start && node.Data.Range.End == fieldElem.Value.Parent.Range.End)
                        {
                            node.AddChild(fieldElem, control);
                            found = true;
                        }
                        else
                        {
                            var childNode = node.AllChildren.FirstOrDefault(check => check.Data.Range.Start == fieldElem.Value.Parent.Range.Start && check.Data.Range.End == fieldElem.Value.Parent.Range.End);
                            if (childNode != null)
                            {
                                childNode.AddChild(fieldElem, control);
                                found = true;
                            }
                         }
                    }

                    
                    if (!found)
                    {
                        string fieldCodeChild = VerifyField(fieldElem.Value, control);
                        if (fieldCodeChild != null)
                        {
                            baseFields.Add(new FieldTreeNode(fieldElem.Value, fieldCodeChild, control.Document.GetText(fieldElem.Value.ResultRange)));
                        }
                        else
                        {
                            baseFields.Add(new FieldTreeNode(fieldElem.Value, "GetTextCall:" + control.Document.GetText(fieldElem.Value.CodeRange), control.Document.GetText(fieldElem.Value.ResultRange)));
                        }
                    }
                }
                else
                {
                    string fieldCodeChild = VerifyField(fieldElem.Value, control);
                    if (fieldCodeChild != null)
                    {
                        baseFields.Add(new FieldTreeNode(fieldElem.Value, fieldCodeChild, control.Document.GetText(fieldElem.Value.ResultRange)));
                    }
                    else
                    {
                        baseFields.Add(new FieldTreeNode(fieldElem.Value, "GetTextCall:" + control.Document.GetText(fieldElem.Value.CodeRange), control.Document.GetText(fieldElem.Value.ResultRange)));
                    }

                }
            }
        }

        public static string VerifyField(Field value, SnapControl control)
        {
            var fld = control.Document.ParseField(value);
            if (fld is DevExpress.Snap.API.Native.NativeSnapList lst)
            {
                return "SnapList:" + lst.Name;
            }
            if (fld is DevExpress.Snap.API.Native.NativeSnapListFilters flt)
            {
                return "SnapLisFilter:";
            }
            if (fld is DevExpress.Snap.API.Native.NativeSnapRowIndex row)
            {
                return "SnapRowIndx:" + row.FormatString;
            }
            if (fld is DevExpress.Snap.API.Native.NativeSnapSection sct)
            {
                return "SnapSection:";
            }
            if (fld is DevExpress.Snap.API.Native.NativeSnapText txt)
            {
                return "FieldText:" + txt.DataFieldName;
            }
            if (fld is DevExpress.Snap.API.Native.NativeSnapSingleListItemEntity se)
            {
                return "SingleList:" + se.DataFieldName;
            }
            if (fld is DevExpress.Snap.API.Native.NativeSnapEntityBase oth)
            {
                return "FieldBase:" + oth.ToString();
            }
            return "Unknown field";
        }

        //public static int WholeCount(this List<FieldTreeNode> fieldTrees)
        //{
        //    int wholeSum = 1;
        //    var counter = fieldTrees.GetEnumerator();
        //    while (counter.MoveNext())
        //    {
        //        wholeSum += counter.Current.TreeCount;
        //    }
        //    return wholeSum;
        //}
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
        private string textEditImage;


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

        public bool AnalyzeTextSectionOld(SnapControl control, DocumentRange sectionPart, IEnumerable<int> paragraphsCollection)
        {
             var sectionRange = sectionPart;
            var fieldRange = SnapFieldTools.GetFieldsInRange(control.Document, sectionRange).ToList();
            var paragraphPositions = paragraphsCollection.ToList();

            var ll = paragraphPositions.Select(Call => new Tuple<int, int>(Call, Call));
            fieldRange.Sort((field1, field2) => field1.ResultRange.Start.ToInt().CompareTo(field2.ResultRange.Start.ToInt()));
            List<FieldTreeNode> fieldNodes = new List<FieldTreeNode>();
            


            var allEntities = new FieldLinkedList(fieldRange);
            if (allEntities.Count() > 0)
            {
                // document contains edit fields
                fieldNodes.ArrangeFields(allEntities, control);
                var stringParts = new List<Tuple<string, int>>();
                int initialRange = sectionRange.Start.ToInt();
                var fieldData = allEntities.First;
                while (fieldData.Next != null)
                {
                    // text before field
                    stringParts.Add(new Tuple<string, int>(control.Document.GetText(control.Document.CreateRange(initialRange, fieldData.Value.Range.Start.ToInt() - initialRange)), fieldData.Value.CodeRange.Length + 2));
                    if (paragraphPositions.Exists(condition => condition == fieldData.Value.Range.Start.ToInt()))
                    {
                        var lastElement = stringParts.Last();
                        stringParts[stringParts.Count - 1] = new Tuple<string, int>(lastElement.Item1 + "\r\n", lastElement.Item2);
                    }
                    if (fieldData.Next.Value.Range.Start.ToInt() < fieldData.Value.ResultRange.End.ToInt())
                    {
                        // text in field to nested field
                        stringParts.Add(new Tuple<string, int>(control.Document.GetText(control.Document.CreateRange(fieldData.Value.ResultRange.Start.ToInt(),
                            fieldData.Next.Value.Range.Start.ToInt() - fieldData.Value.ResultRange.Start.ToInt())), 0));
                        initialRange = fieldData.Next.Value.Range.Start.ToInt();
                    }
                    else
                    {
                        // text in field
                        stringParts.Add(new Tuple<string, int>(control.Document.GetText(fieldData.Value.ResultRange), 1));
                        initialRange = fieldData.Value.Range.End.ToInt();
                    }
                    fieldData = fieldData.Next;
                }
                if (fieldData.Value.Range.Start.ToInt() >= initialRange)
                {
                    // last field - text before
                    stringParts.Add(new Tuple<string, int>(control.Document.GetText(control.Document.CreateRange(initialRange, fieldData.Value.Range.Start.ToInt() - initialRange)), fieldData.Value.CodeRange.Length + 2));
                    if (paragraphPositions.Exists(condition => condition == fieldData.Value.Range.Start.ToInt()))
                    {
                        var lastElement = stringParts.Last();
                        stringParts[stringParts.Count - 1] = new Tuple<string, int>(lastElement.Item1 + "\r\n", lastElement.Item2);
                    }
                    stringParts.Add(new Tuple<string, int>(control.Document.GetText(fieldData.Value.ResultRange), 1));
                    initialRange = fieldData.Value.Range.End.ToInt();
                }
                var fieldinRange = allEntities.Where(checker => checker.ResultRange.Start.ToInt() <= initialRange && checker.ResultRange.End.ToInt() >= initialRange);
                if (fieldinRange.Count() == 1)
                {
                    var lastPartRange = control.Document.CreateRange(initialRange, fieldinRange.First().ResultRange.End.ToInt() - initialRange);
                    string partialTexts = control.Document.GetText(lastPartRange);
                    stringParts.Add(new Tuple<string, int>(partialTexts, 1));
                    lastPartRange = control.Document.CreateRange(fieldinRange.First().Range.End.ToInt(), sectionRange.End.ToInt() - fieldinRange.First().Range.End.ToInt());
                    partialTexts = control.Document.GetText(lastPartRange);
                    stringParts.Add(new Tuple<string, int>(partialTexts, 0));
                }
                else
                {
                    var lastPartRange = control.Document.CreateRange(initialRange, sectionRange.End.ToInt() - initialRange);
                    stringParts.Add(new Tuple<string, int>(control.Document.GetText(lastPartRange), 0));
                }
                log.InfoFormat("String parts count:{0}", stringParts.Count);
                if (log.IsInfoEnabled)
                {
                    for (int index = 0; index < stringParts.Count; index++)
                    {
                        log.InfoFormat("Part {0} Elements '{1}' , {2}", index, stringParts[index].Item1, stringParts[index].Item2);
                    }
                }
                MapTextPositions(stringParts, paragraphPositions, "",  control, sectionPart.Start.ToInt());
                return true;
            }
            return false;
        }


        public bool AnalyzeTextSection(SnapControl control, DocumentRange sectionPart, string sectionText,  IEnumerable<int> paragraphsCollection)
        {
            var sectionRange = sectionPart;
            var fieldRange = SnapFieldTools.GetFieldsInRange(control.Document, sectionRange).ToList();
            var paragraphPositions = paragraphsCollection.ToList();
            fieldRange.Sort((field1, field2) => field1.Range.Start.ToInt().CompareTo(field2.Range.Start.ToInt()));
            List<FieldTreeNode> fieldNodes = new List<FieldTreeNode>();

            if (fieldRange.Count() > 0)
            {
                // document contains edit fields
                fieldNodes.ArrangeFields(new FieldLinkedList(fieldRange), control);
                var stringParts = new List<Tuple<string, int>>();
                int initialRange = sectionRange.Start.ToInt();
                foreach (var fieldElement in fieldNodes)
                {
                    if (fieldElement.IsLeaf)
                    {
                        // no nested fields
                        var fieldData = fieldElement.Data;
                        if (fieldData.ResultRange.Start.ToInt() >= initialRange)
                        {
                            // text before (or between fields) 
                            stringParts.Add(new Tuple<string, int>(control.Document.GetText(control.Document.CreateRange(initialRange, fieldData.Range.Start.ToInt() - initialRange)), fieldData.CodeRange.Length + 2));
                            // text inside field
                            stringParts.Add(new Tuple<string, int>(control.Document.GetText(fieldData.ResultRange), 1));
                            initialRange = fieldData.Range.End.ToInt();
                        }
                    }
                    else
                    {
                        // field has nested fields inside
                        var fieldData = fieldElement.Data;
                        if (fieldData.ResultRange.Start.ToInt() >= initialRange)
                        {
                            // text before field with nested children
                            stringParts.Add(new Tuple<string, int>(control.Document.GetText(control.Document.CreateRange(initialRange, fieldData.Range.Start.ToInt() - initialRange)), fieldData.CodeRange.Length + 2));
                            initialRange = fieldData.Range.Start.ToInt() + 1;
                            AnalyzeNestedFields(control, fieldElement, ref initialRange, stringParts);
                            initialRange = fieldData.Range.End.ToInt();
                        }
                    }

                }
                stringParts.Add(new Tuple<string, int>((control.Document.GetText(control.Document.CreateRange(initialRange, sectionPart.End.ToInt() - initialRange))), 0));
                log.InfoFormat("String parts count:{0}", stringParts.Count);
                if (log.IsInfoEnabled)
                {
                    for (int index = 0; index < stringParts.Count; index++)
                    {
                        log.InfoFormat("Part {0} Elements '{1}' , {2}", index, stringParts[index].Item1, stringParts[index].Item2);
                    }
                }

                MapTextPositions(stringParts, paragraphPositions, sectionText, control, sectionPart.Start.ToInt());

                return true;
            }
            return false;
        }

        private void AnalyzeNestedFields(SnapControl control, FieldTreeNode fieldElement, ref int initialRange, List<Tuple<string, int>> stringParts)
        {
            // first check if current field has resultRange text before first nested field
            if (fieldElement.Data.ResultRange.Start < fieldElement.FirstChild.Data.ResultRange.Start)
            {

                    stringParts.Add(new Tuple<string, int>(control.Document.GetText(control.Document.CreateRange(fieldElement.Data.ResultRange.Start.ToInt(),
                                fieldElement.FirstChild.Data.Range.Start.ToInt() - fieldElement.Data.ResultRange.Start.ToInt())), 0));
                initialRange = fieldElement.FirstChild.Data.Range.Start.ToInt();
            }

            foreach (var childItem in fieldElement.Children)
            {
                if (!childItem.IsLeaf)
                {
                    AnalyzeNestedFields(control, childItem, ref initialRange, stringParts);
                }
                else
                {
                    var currentChild = childItem.Data;
                    stringParts.Add(new Tuple<string, int>(control.Document.GetText(control.Document.CreateRange(initialRange, currentChild.Range.Start.ToInt() - initialRange)), currentChild.CodeRange.Length + 2));
                    stringParts.Add(new Tuple<string, int>(control.Document.GetText(currentChild.ResultRange), 1));
                    initialRange = currentChild.Range.End.ToInt();
                }
            }

            if (fieldElement.Data.ResultRange.End > fieldElement.LastChild.Data.ResultRange.End)
            {
                stringParts.Add(new Tuple<string, int>(control.Document.GetText(control.Document.CreateRange(fieldElement.LastChild.Data.Range.End.ToInt(),
                            fieldElement.Data.ResultRange.End.ToInt() - fieldElement.LastChild.Data.Range.End.ToInt())), 1));
                initialRange = fieldElement.Data.Range.End.ToInt();
            }
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
        public Tuple<Dictionary<int, int>, Dictionary<int, int>> MapTextPositions(string textSection, bool append = false, IEnumerable<int> sectionParagraphs = null )
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
                            dictEditPosData[lineText.Length + sumTextEdit] = lineText.Length  + sumTextSnap;
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

        public void MapTextPositions(List<Tuple<string, int>> textSections, IEnumerable<int> sectionParagraphs, string texControl, SnapControl control, int sectionOffset)
        {
            int snapTextOffset = 0;
            int editTextOffset = 0;
            int fieldOffsets = 0;
            var dictEditToSnapPosData = new Dictionary<int, int>();
            var dictSnapToEditPosData = new Dictionary<int, int>();
            for(int index = 0; index < textSections.Count; index++)
            {
                var sectionOffseted = sectionParagraphs.Select(sectionOffsetPart => sectionOffsetPart -= fieldOffsets).ToList();
                if (textSections[index].Item2 > 0)
                {
                    fieldOffsets += textSections[index].Item2;
                }
                var strTextPart = textSections[index].Item1;
                if (sectionOffseted.Contains(strTextPart.Length + 1))
                {
                    strTextPart += "\r\n";
                    textSections[index] = new Tuple<string, int>(strTextPart, textSections[index].Item2);
                }
            }


            foreach (var sectionTextPart in textSections)
            {
                var mapping = MapTextPositions(sectionTextPart.Item1, true, null);
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
                    snapTextOffset = snapTextOffset + sectionTextPart.Item2 + 1;
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

            if (true == sectionParagraphs?.Contains(SnapMaxPos - 1))
            {
                dictEditToSnapPosData[EditMaxPos] = SnapMaxPos;
                dictEditToSnapPosData[EditMaxPos + 1] = SnapMaxPos;

                dictSnapToEditPosData[SnapMaxPos] = EditMaxPos;
                SnapMaxPos++;
                EditMaxPos++;
                textSections[textSections.Count-1] = new Tuple<string, int>(textSections.Last().Item1 + "\r\n", textSections.Last().Item2);
            }


            StringBuilder textBuilder = new StringBuilder();

            textSections.ForEach(textPart => textBuilder.Append(textPart.Item1));
            textEditImage = textBuilder.ToString();
            
            int compOP = texControl.CompareTo(textEditImage);

            {
                log.InfoFormat("Comparsion sectionText:'{0}'  textParts:'{1}' - result {2}", texControl, textEditImage, compOP);
                log.InfoFormat("length sectionTextParts:{0}  texFromRange:{1}, maxEditPos {2}", textEditImage.Length, texControl.Length, EditMaxPos );
                
            }    

            if (textEditImage.Length < EditMaxPos + 1)
            {
                textEditImage = textEditImage.PadRight(EditMaxPos+1, '`');
            }

            if (texControl.Length < EditMaxPos + 1)
            {
                texControl = texControl.PadRight(EditMaxPos + 1, '`');
            }

            if (log.IsInfoEnabled && textSections.Count < 20)
            {
                if (mapSnapEditPos.Max(check => check.Value) + 1 > EditMaxPos)
                {
                    log.InfoFormat("Found wrong EditPos {0}", mapSnapEditPos.Max(check => check.Value) + 1);
                    var keyList = new List<int>(mapSnapEditPos.Keys);

                    foreach(var keyEelem in keyList)
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
                for ( int keyIndex = 0;  keyIndex < keyValItems.Length; keyIndex++)
                {
                    var charText = textEditImage[keyValItems[keyIndex].Key];
                    var chaVal = Char.IsWhiteSpace(charText) ? String.Format("0x{0:X2}", ((byte)charText)) : String.Format("{0}", charText);
                    var charTextSect = texControl[keyValItems[keyIndex].Key];
                    var chaValSect = Char.IsWhiteSpace(charTextSect) ? String.Format("0x{0:X2}", ((byte)charTextSect)) : String.Format("{0}", charTextSect);
                    mapTextBuilderES.Append("ID:").Append(keyIndex).Append(",E_id:").Append(keyValItems[keyIndex].Key).Append(",E_val:").Append(chaVal);
                    mapTextBuilderES.Append("   ,T_id:").Append(keyValItems[keyIndex].Key).Append(",T_val:").Append(chaValSect);
                    mapTextBuilderES.Append("   ,S_id:").Append(keyValItems[keyIndex].Value).Append(",S_val:").Append(control.Document.GetText(control.Document.CreateRange(keyValItems[keyIndex].Value + sectionOffset, 1))).AppendLine();
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
                    mapTextBuilderSE.Append("ID:").Append(keyIndex).Append(",S_id:").Append(keyValItems[keyIndex].Key).Append(",S_val:").Append(control.Document.GetText(control.Document.CreateRange(keyValItems[keyIndex].Key + sectionOffset, 1)));
                    mapTextBuilderSE.Append("   ,E_id:").Append(keyValItems[keyIndex].Value).Append(",E_val:").Append(chaVal);
                    mapTextBuilderSE.Append("   ,T_id:").Append(keyValItems[keyIndex].Value).Append(",T_val:").Append(chaValSect).AppendLine();
                }
                log.Info("MapSnapEdit: " + mapTextBuilderSE.ToString());
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

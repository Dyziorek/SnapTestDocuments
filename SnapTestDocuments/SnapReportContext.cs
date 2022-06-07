using DevExpress.Snap;
using DevExpress.Snap.Core.API;
using DevExpress.XtraRichEdit.API.Native;
//using Nuance.SoD.TextControl;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TextEditor.Core.API;
using DevExpress.XtraRichEdit.Services;

namespace TextEditor.Core.API
{
    public class IField
    {
        private Field nativeField;
        public IField(Field native)
        {
            nativeField = native;
        }
        public Field FieldNative
        {
            get
            {
                return nativeField;
            }
        }

        public DocumentRange ResultRange => nativeField.ResultRange;
    }
}

namespace SnapTestDocuments
{
    public interface IDocumentPosition
    {
        int Position { get; }
    }

    public interface IDocumentRange
    {
        IDocumentPosition Start { get; }
        IDocumentPosition End { get; }
        int Length { get; }
    }

    public enum DocumentEntityTypes
    {
        EditField,
        CannedMessage,
        InterpretationSection,
        EmptySection
    }

    public class DocumentEntityBase
    {
        public string name;
        public DocumentEntityTypes Type;
        public DocumentEntityBase Parent;

        public DocumentEntityBase GetTopParent()
        {
            return null;
        }
    }

    

    public interface ITextFieldInfo
    {
        TextEditor.Core.API.IField Field { get; }
        IDocumentRange ResultRange { get; }

        ISubDocument Document { get; }
    }

    public interface IFieldCollection : IReadOnlyCollection<IField>
    {

    }

    public class UserListService : IUserListService
    {
        public IList<string> GetUsers()
        {
            List<string> l = new List<string>();
            l.Add("Regular User");
            return l;
        }
    }

    public interface ISubDocument : System.IEquatable<ISubDocument>
    {
        IFieldCollection Fields { get; }
    }

    public interface ITextEditManager
    {
        void Init();
        /// <summary>
        /// Clear data when work with snap control finished.
        /// </summary>
        void Clear();
        /// <summary>
        /// Reset manager state when new document loaded into snap control.
        /// </summary>
        void Reset();
    }

    public interface ISnapCtrlContext : ISnapReportContext
    {
        DevExpress.Snap.SnapControl SnapControl { get; }
        DevExpress.Snap.Core.API.SnapDocument SnapDocument { get; }

        SnapControlForm MainForm { get; }
    }

    public interface ISnapReportContext : ITextEditWinFormsUIContext
    {
    }


    public interface ITextEditWinFormsUIContext
    {
        DevExpress.Snap.Core.API.SnapDocument Document { get; }
        T GetManager<T>() where T : ITextEditManager;
    }

    public interface IPermissionManager : ITextEditManager
    {
        bool IsDocumentFieldEditable(Field editField);
    }


    public interface IDragonAccessManager : ITextEditManager
    {
        string GetText();
        int GetTextLen();
        Tuple<int, int> SetSel(int start, int end);
        Tuple<int, int> GetSel();
        void ReplaceText(string text);

        Rectangle PosFromChar(int charPos);
        int CharFromPos(PointF charPos);



        void UpdateSelectedItem(DocumentEntityBase selectedItem);
        bool HasSections();
    }

    public interface IInterpSectionsManager : ITextEditManager
    {


        DevExpress.XtraRichEdit.API.Native.Field GetSectionField(DocumentEntityBase sectionEntity);


    }

    public interface ICustomFieldEditManager : ITextEditManager
    {
        bool IsEmptyEntityField(Field fieldCheck, DocumentEntityTypes editField);
    }

    public interface IEmptyMergeFieldCharacterPropertiesManager : ITextEditManager
    {
        void AddEmptyMergeFieldsCharacterProperties(Field field, SubDocument subDocument, DocumentRange characterPropertiesSourceDocRng);

    }

    public interface SelectionInfo
    {
        bool IsCaretPosInField { get;  }
    }

    public interface ISelectionChangedTrackingManager : ITextEditManager
    {
        SelectionInfo LastSelectionInfo { get; }
    }

    public class SelectionInfoImpl : SelectionInfo
    {
        ISnapCtrlContext workTracker;
        public SelectionInfoImpl (ISnapCtrlContext worker)
        {
            workTracker = worker;
        }
        public bool IsCaretPosInField => workTracker.Document.Fields.Any(checkCursor => checkCursor.Range.Start.ToInt() > workTracker.Document.CaretPosition.ToInt() &&
       checkCursor.Range.End.ToInt() < workTracker.Document.CaretPosition.ToInt());
    }

    public class SelectionChangedTrackingManagerImpl : ISelectionChangedTrackingManager
    {
        ISnapCtrlContext workTracker;
        SelectionInfoImpl checkCursor;
        public SelectionChangedTrackingManagerImpl(ISnapCtrlContext works)
        {
            workTracker = works;
            checkCursor = new SelectionInfoImpl(workTracker);
        }

        public SelectionInfo LastSelectionInfo => checkCursor;

        public void Clear()
        {
            
        }

        public void Init()
        {
            
        }

        public void Reset()
        {
            
        }
    }

    public class CustomFieldEditManagerImpl : ICustomFieldEditManager
    {
        public bool IsEmptyEntityField(Field fieldCheck, DocumentEntityTypes editField)
        {
            return true;
        }

        public void Clear()
        {

        }

        public void Init()
        {

        }

        public void Reset()
        {

        }
    }

    public class EmptyMergeFieldCharacterPropertiesManagerImpl : IEmptyMergeFieldCharacterPropertiesManager
    {
        public void Clear()
        {

        }

        public void Init()
        {

        }

        public void Reset()
        {

        }

        void AddEmptyMergeFieldsCharacterProperties(Field field, SubDocument subDocument, DocumentRange characterPropertiesSourceDocRng)
        {

        }

        void IEmptyMergeFieldCharacterPropertiesManager.AddEmptyMergeFieldsCharacterProperties(Field field, SubDocument subDocument, DocumentRange characterPropertiesSourceDocRng)
        {
            
        }
    }

    public class SectionManagerImpl : IInterpSectionsManager
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger("ExtSnapControl");
        private Field sectionField;
        private SnapControl documentControl;

        public SectionManagerImpl(ISnapCtrlContext ctx)
        {
            documentControl = ctx.SnapControl;
        }

        public void Clear()
        {

        }

        public Field GetSectionField(DocumentEntityBase sectionEntity)
        {
            if (sectionEntity?.Type == DocumentEntityTypes.InterpretationSection)
            {
                foreach (Field checkingField in documentControl.Document.Fields)
                {
                    var fieldCodeText = documentControl.Document.GetText(checkingField.CodeRange);
                    if (fieldCodeText.Contains("FUNCTION") && fieldCodeText.Contains(sectionEntity.name))
                    {
                        return checkingField;
                    }
                }
            }
            return null;
        }



        public void Init()
        {
            foreach (Field checkingField in documentControl.Document.Fields)
            {
                var fieldCodeText = documentControl.Document.GetText(checkingField.CodeRange);
                if (fieldCodeText.Contains("INTERP"))
                {
                    sectionField = checkingField;
                    break;
                }
            }
        }

        public void Reset()
        {
            sectionField = null;
        }
    }

    public class SnapManagerContainer<T> where T : class
    {
        private Dictionary<Type, T> items = new Dictionary<Type, T>();
        public P Get<P>() where P : T
        {
            return items.TryGetValue(typeof(P), out T ret) ? (P)ret : default(P);
        }

        public void Set<P>(P newEntry) where P : T
        {
            if (!items.ContainsKey(typeof(P)))
            {
                items.Add(typeof(P), newEntry);
            }
        }
    }

    public class SnapContextImpl : ISnapCtrlContext
    {
        SnapManagerContainer<ITextEditManager> manager = new SnapTestDocuments.SnapManagerContainer<ITextEditManager>();
        DragonAccessManagerCmn dragonWork = null;
        public DevExpress.Snap.SnapControl WorkControl { get; set; }
        public SnapControlForm FormControl { get; set; }
        SectionManagerImpl sectioner = null;
        SelectionChangedTrackingManagerImpl tracker;
        EmptyMergeFieldCharacterPropertiesManagerImpl mergeFields = null;

        public DevExpress.Snap.SnapControl SnapControl => WorkControl;

        public DevExpress.Snap.Core.API.SnapDocument Document => WorkControl.Document;

        public SnapControlForm MainForm => FormControl;

        public DevExpress.Snap.Core.API.SnapDocument SnapDocument => WorkControl.Document;

        public virtual T GetManager<T>() where T : ITextEditManager
        {
            var managedItem = manager.Get<T>();
            if (managedItem != null)
            {
                return managedItem;
            }

            if (typeof(T) == typeof(IDragonAccessManager))
            {
                if (dragonWork == null)
                {
                    dragonWork = new DragonAccessManagerCmn(this);
                    dragonWork.Init();
                    manager.Set((T)(ITextEditManager)dragonWork);
                    return (T)(ITextEditManager)dragonWork;
                }
            }

            if (typeof(T) == typeof(IInterpSectionsManager))
            {
                if (sectioner == null)
                {
                    sectioner = new SectionManagerImpl(this);
                    manager.Set((T)(ITextEditManager)sectioner);
                    return (T)(ITextEditManager)sectioner;
                }
            }

            if (typeof(T) == typeof(IPermissionManager))
            {
                var pers = new SnapRangePermissionsTools();
                manager.Set((T)(IPermissionManager)(pers));
                return (T)(IPermissionManager)pers;
            }

            if ( typeof(T) == typeof(IEmptyMergeFieldCharacterPropertiesManager))
            {
                if (mergeFields == null)
                {
                    mergeFields = new EmptyMergeFieldCharacterPropertiesManagerImpl();
                    manager.Set<T>((T)(IEmptyMergeFieldCharacterPropertiesManager)(mergeFields));
                    return (T)(IEmptyMergeFieldCharacterPropertiesManager)mergeFields;
                }
            }

            if (typeof(T) == typeof(ISelectionChangedTrackingManager))
            {
                if (tracker == null)
                {
                    tracker = new SelectionChangedTrackingManagerImpl(this);
                    manager.Set<T>((T)(ISelectionChangedTrackingManager)(tracker));
                    return (T)(ISelectionChangedTrackingManager)tracker;
                }
            }


            return (T)(ITextEditManager)null;
        }

        public DragonAccessManagerCmn getDragon()
        {
            return dragonWork;
        }

    }

    public class DocPos : IDocumentPosition
    {
        DocumentRange workItem;
        bool begin;
        public DocPos(DocumentRange fieldRange, bool begin)
        {
            this.begin = begin;
            workItem = fieldRange;
        }

        public int Position => begin ? workItem.Start.ToInt() : workItem.End.ToInt();
    }

    public class DocFields : IFieldCollection
    {
        FieldCollection workFields;
        public DocFields(FieldCollection fields)
        {
            workFields = fields;
        }

        public int Count => workFields.Count;

        public IEnumerator<IField> GetEnumerator()
        {
            return workFields.Select(x => new IField(x)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IField this[int index] => new IField(workFields[index]);
    }

    public class DocContent : ISubDocument
    {
        Document doc;
        public DocContent(Document workDoc)
        {
            doc = workDoc;
        }

        public IFieldCollection Fields => new DocFields(doc.Fields);

        public bool Equals(ISubDocument other)
        {
            return this == other;
        }
    }

    public class DocRange : IDocumentRange
    {
        Field snapField;
        public DocRange(Field native)
        {
            snapField = native;
        }
        public IDocumentPosition Start => new DocPos(snapField.ResultRange, true);


        public IDocumentPosition End => new DocPos(snapField.ResultRange, false);

        public int Length => snapField.ResultRange.Length;
    }

    public class SnapFieldInfo : ITextFieldInfo
    {
        Field nativeField;
        Document workDocument;
        public SnapFieldInfo(Field field, Document document)
        {
            nativeField = field;
            workDocument = document;
        }
        IField ITextFieldInfo.Field => new IField(nativeField);

        IDocumentRange ITextFieldInfo.ResultRange => new DocRange(nativeField);

        ISubDocument ITextFieldInfo.Document => new DocContent(workDocument);
    }

    public static class SnapFieldTools
    {
        public static bool IsValidField(this IField field)
        {
            var check = (field.FieldNative as DevExpress.XtraRichEdit.API.Native.Implementation.NativeField)?.IsValid;
            if (check.HasValue)
            {
                if (check.Value == false)
                {
                    if ((field.FieldNative as DevExpress.XtraRichEdit.API.Native.Implementation.NativeField)?.ResultRange.Length > 0)
                    {
                        return false;
                    }
                    return true;
                }
                return check.Value;
            }
            return false;
        }

        public static bool IsValidField(this ITextFieldInfo fieldInfo)
        {
            var check = (fieldInfo.Field.FieldNative as DevExpress.XtraRichEdit.API.Native.Implementation.NativeField)?.IsValid;
            if (check.HasValue)
            {
                if (check.Value == false)
                {

                    var fieldChecked = fieldInfo.Document.Fields.Where(checkField => checkField == fieldInfo.Field);
                    if (fieldChecked.Count() > 0)
                    {
                        // exists but empty
                        return true;
                    }
                }
                return check.Value;
            }
            // not found or invalid
            return false;
        }

        public static IEnumerable<Field> GetFieldsInRange(SnapSubDocument document, DocumentRange range)
        {
            List<Field> result = new List<Field>();
            if ((document != null))
            {
                result = document.Fields.Where(p => (p != null) && SnapDocumentRangeTools.IsTargetDocumentRangeInBaseDocumentRange(range, p.Range)).ToList<Field>();
            }
            return result;
        }

        public static DevExpress.XtraRichEdit.API.Native.Field ToSnap(this IField field)
        {
            if (field == null) return null;
            return (field).FieldNative;
        }

        public static ITextFieldInfo GetTextFieldInfo(this Field field, SnapDocument subDocument) => new SnapFieldInfo(field, subDocument);



        public static IEnumerable<SnapEntity> GetEntitiesInRange(SnapSubDocument document, DocumentRange range)
        {
            HashSet<SnapEntity> result = new HashSet<SnapEntity>();
            if ((document != null) && (range != null) && (range.Length > 0))
            {
                List<Field> fields = document.Fields.Where(p => (p != null) && SnapDocumentRangeTools.IsTargetDocumentRangeInBaseDocumentRange(range, p.Range)).ToList<Field>();
                result.UnionWith(fields.Select(p => document.ParseField(p)));

            }
            return result;
        }

        public static IEnumerable<Tuple<int, Field, DocumentRange, string>> GetElementsInRange(SnapSubDocument document, DocumentRange range)
        {
            List<Tuple<int, Field, DocumentRange, string>> result = new List<Tuple<int, Field, DocumentRange, string>>();
            if ((document != null) && (range != null) && (range.Length > 0))
            {
                List<Field> fields = document.Fields.Where(p => (p != null) && SnapDocumentRangeTools.IsTargetDocumentRangeInBaseDocumentRange(range, p.Range)).ToList<Field>();
                result.AddRange(fields.Select(fld => new Tuple<int, Field, DocumentRange, string>(fld.Range.Start.ToInt(), fld, fld.Range, document.GetText(fld.CodeRange))));

                List<Bookmark> ts = document.Bookmarks.Where(bkm => (bkm != null) && SnapDocumentRangeTools.IsTargetDocumentRangeInBaseDocumentRange(range, bkm.Range)).ToList<Bookmark>();

                result.AddRange(ts.Select(b => new Tuple<int, Field, DocumentRange, string>(b.Range.Start.ToInt(), null, b.Range, b.Name)));
            }
            return result;
        }

    }



    public class SnapDocumentRangeTools
    {
        public static bool IsTargetDocumentRangeInBaseDocumentRange(DocumentRange baseRange, DocumentRange targetRange)
        {
            if (baseRange == null || targetRange == null) return false;

            int baseRangeStart = baseRange.Start.ToInt();
            int baseRangeEnd = baseRangeStart + baseRange.Length;

            int targeRangeStart = targetRange.Start.ToInt();
            int targeRangeEnd = targeRangeStart + targetRange.Length;

            return baseRangeStart <= targeRangeStart && baseRangeEnd >= targeRangeEnd;
        }

        public static bool IsTargetDocumentPositionInBaseDocumentRange(DocumentRange baseRange, DocumentPosition documentPosition)
        {
            if (baseRange == null || documentPosition == null) return false;

            int baseRangeStart = baseRange.Start.ToInt();
            int baseRangeEnd = baseRangeStart + baseRange.Length;

            return baseRangeStart <= documentPosition.ToInt() && baseRangeEnd >= documentPosition.ToInt();
        }
    }

    public class SnapRangePermissionsTools : IPermissionManager
    {
        public void Clear()
        {

        }

        public void Init()
        {

        }

        public void Reset()
        {
        }

        public static bool IsDocumentRangeEditableRange(SubDocument subDocument, DocumentRange docRange)
        {
            if (subDocument == null || docRange == null) return false;
            bool isEditable = false;

            RangePermissionCollection rangesCol = subDocument.BeginUpdateRangePermissions();
            foreach (RangePermission rangePerm in rangesCol)
            {
                if (rangePerm.UserName == "Regular User" && SnapDocumentRangeTools.IsTargetDocumentRangeInBaseDocumentRange(rangePerm.Range, docRange))
                {
                    isEditable = true;
                    break;
                }
            }
            subDocument.CancelUpdateRangePermissions(rangesCol);


            return isEditable;
        }

        public bool IsDocumentFieldEditable(Field field)
        {
            return true;
        }
    }
}

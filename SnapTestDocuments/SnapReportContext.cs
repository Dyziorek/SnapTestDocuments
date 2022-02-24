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
        InterpretationSection,
        EmptySection
    }

    public class DocumentEntityBase
    {
        public string name;
        public DocumentEntityTypes Type;
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
    }

    public interface ISnapReportContext
    {
        T GetManager<T>() where T : ITextEditManager;
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
            log.Debug("Field Section lookup");
            if (sectionEntity?.Type == DocumentEntityTypes.InterpretationSection)
            {
                foreach (Field checkingField in documentControl.Document.Fields)
                {
                    var fieldCodeText = documentControl.Document.GetText(checkingField.CodeRange);
                    if (fieldCodeText.Contains("FUNCTION") && fieldCodeText.Contains(sectionEntity.name))
                    {
                        log.Debug("Field Section lookup found:" + sectionEntity.name);
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

    public class SnapMangerContainer<T> where T : class
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
        SnapMangerContainer<ITextEditManager> manager = new SnapTestDocuments.SnapMangerContainer<ITextEditManager>();
        DragonAccessManagerCmn dragonWork = null;
        public DevExpress.Snap.SnapControl WorkControl { get; set; }
        SectionManagerImpl sectioner = null;


        public DevExpress.Snap.SnapControl SnapControl => WorkControl;

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


        public static DevExpress.XtraRichEdit.API.Native.Field ToSnap(this IField field)
        {
            if (field == null) return null;
            return (field).FieldNative;
        }

        public static ITextFieldInfo GetTextFieldInfo(this Field field, SnapDocument subDocument) => new SnapFieldInfo(field, subDocument);

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

    public class SnapRangePermissionsTools
    {

        public static bool IsDocumentRangeEditableRange(SubDocument subDocument, DocumentRange docRange)
        {
            if (subDocument == null || docRange == null) return false;

            RangePermissionCollection rangesCol = subDocument.BeginUpdateRangePermissions();
            foreach (RangePermission rangePerm in rangesCol)
            {
                if (rangePerm.UserName == "Regular User" && SnapDocumentRangeTools.IsTargetDocumentRangeInBaseDocumentRange(rangePerm.Range, docRange))
                {
                    break;
                }
            }
            subDocument.CancelUpdateRangePermissions(rangesCol);


            return true;
        }
    }
}

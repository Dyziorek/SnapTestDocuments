using DevExpress.Snap;
using DevExpress.XtraRichEdit.API.Native;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace SnapTestDocuments
{

    public enum DocumentEntityTypes
    {
        InterpretationSection
    }

    public class DocumentEntityBase
    {
        public string name;
        public DocumentEntityTypes Type;
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
            foreach(Field checkingField in documentControl.Document.Fields)
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
            if(!items.ContainsKey(typeof(P)))
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
                    manager.Set((T)(ITextEditManager)dragonWork);
                    return (T)(ITextEditManager)dragonWork;
                }
            }

            if (typeof(T) == typeof(IInterpSectionsManager))
            {
                if(sectioner == null)
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
    }
}

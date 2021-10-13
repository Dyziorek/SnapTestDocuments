using DevExpress.Snap;
using DevExpress.XtraRichEdit.API.Native;
using System;
using System.Drawing;

namespace SnapTestDocuments
{
    public enum DocumentEntityBase
    {
        IntepretationSection
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
            if (sectionEntity == DocumentEntityBase.IntepretationSection)
            {
                foreach (Field checkingField in documentControl.Document.Fields)
                {
                    var fieldCodeText = documentControl.Document.GetText(checkingField.CodeRange);
                    if (fieldCodeText.Contains("INTERP"))
                    {
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



    public class SnapContextImpl : ISnapReportContext
    {
        DragonAccessManagerImpl dragonWork = null;
        public DevExpress.Snap.SnapControl WorkControl { get; set; }


        public DevExpress.Snap.SnapControl SnapControl => WorkControl;

        T ISnapReportContext.GetManager<T>()
        {
            if (dragonWork != null)
            {
                return (T)(ITextEditManager)dragonWork;
            }
            else
            {
                dragonWork = new DragonAccessManagerImpl(this);
            }

            return (T)(ITextEditManager)null;
        }

        public DragonAccessManagerImpl getDragon()
        {
            return dragonWork;
        }
    }

}

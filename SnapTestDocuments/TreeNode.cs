using DevExpress.Snap;
using DevExpress.XtraRichEdit.API.Native;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnapTestDocuments
{
    public class FieldTreeNode : IEnumerable<FieldTreeNode>
    {

        public Field Data { get; set; }
        public string FieldCode;

        public string FieldTextValue;
        public FieldTreeNode Parent { get; set; }
        private readonly LinkedList<FieldTreeNode> children;
        public ICollection<FieldTreeNode> Children => children;

        public Boolean IsRoot
        {
            get { return Parent == null; }
        }

        public bool IsLeaf
        {
            get { return Children.Count == 0; }
        }

        public int Level
        {
            get
            {
                if (this.IsRoot)
                    return 0;
                return Parent.Level + 1;
            }
        }

        public FieldTreeNode FirstChild
        {
            get
            {
                return children.Count > 0 ? children.First.Value : null;
            }
        }

        public FieldTreeNode LastChild
        {
            get
            {
                return children.Count > 0 ? children.Last.Value : null;
            }
        }

        public ICollection<FieldTreeNode> AllChildren { 
            get
            {
                LinkedList<FieldTreeNode> fieldTreeNodes = new LinkedList<FieldTreeNode>();
                foreach(var child in Children)
                {
                    fieldTreeNodes.AddLast(child);
                    foreach (var subChild in child.AllChildren)
                    {
                        fieldTreeNodes.AddLast(subChild);
                    }
                }
                return fieldTreeNodes;
            } 
        }

        public FieldTreeNode(Field data, string fieldCode, string textField)
        {
            this.Data = data;
            this.FieldCode = fieldCode;
            this.FieldTextValue = textField;
            this.children = new LinkedList<FieldTreeNode>();

            //this.ElementsIndex = new LinkedList<FieldTreeNode>();
            //this.ElementsIndex.Add(this);
        }

        public FieldTreeNode AddChild(Field child, string fieldCode, string textField)
        {
            FieldTreeNode childNode = new FieldTreeNode(child, fieldCode, textField) { Parent = this };
            this.Children.Add(childNode);

            //this.RegisterChildForSearch(childNode);

            return childNode;
        }

        public void AddChild(LinkedListNode<Field> indexer, SnapControl control)
        {
            string fieldCodeChild = DragonDictationTools.VerifyField(indexer.Value, control);
            AddChild(indexer.Value, fieldCodeChild, control.Document.GetText(indexer.Value.ResultRange));
        }

        public override string ToString()
        {
            if (Children.Count > 0)
            {
                StringBuilder fullPack = new StringBuilder();
                fullPack.Append("(").AppendLine();
                fullPack.Append(Data != null ? "(" + Data.Range.Start.ToString() + ", " + Data.Range.End.ToString() + ")" + "{" + Data.ResultRange.Start.ToString() + ", " + Data.ResultRange.End.ToString() + "}" : "[data null]");
                fullPack.Append(",FC:").Append(FieldCode).Append(",FV:").Append(FieldTextValue);
                foreach(var child in Children)
                {
                    fullPack.AppendLine();
                    fullPack.Append(child.ToString());
                    fullPack.AppendLine();
                }
                fullPack.Append(")").AppendLine();
                return fullPack.ToString();
            }
            else
            {

                StringBuilder fullPack = new StringBuilder();
                fullPack.Append(Data != null ? "(" + Data.Range.Start.ToString() + ", " + Data.Range.End.ToString() + ")" + "{" + Data.ResultRange.Start.ToString() + ", " + Data.ResultRange.End.ToString() + "}" : "[data null]");
                fullPack.Append(",FC:").Append(FieldCode).Append(",FV:").Append(FieldTextValue);
                return fullPack.ToString();
            }
        }


        #region searching

        //private ICollection<FieldTreeNode> ElementsIndex { get; set; }

        //private void RegisterChildForSearch(FieldTreeNode node)
        //{
        //    ElementsIndex.Add(node);
        //    if (Parent != null)
        //        Parent.RegisterChildForSearch(node);
        //}

        //public FieldTreeNode FindTreeNode(Func<FieldTreeNode, bool> predicate)
        //{
        //    return this.ElementsIndex.FirstOrDefault(predicate);
        //}

        //public int TreeCount { get { return ElementsIndex.Count; } }
        #endregion


        #region iterating

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<FieldTreeNode> GetEnumerator()
        {
            yield return this;
            foreach (var directChild in this.Children)
            {
                foreach (var anyChild in directChild)
                    yield return anyChild;
            }
        }

        #endregion
    }
}

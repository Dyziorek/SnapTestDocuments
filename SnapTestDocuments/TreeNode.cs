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

        public FieldTreeNode(Field data)
        {
            this.Data = data;
            this.children = new LinkedList<FieldTreeNode>();

            //this.ElementsIndex = new LinkedList<FieldTreeNode>();
            //this.ElementsIndex.Add(this);
        }

        public FieldTreeNode AddChild(Field child)
        {
            FieldTreeNode childNode = new FieldTreeNode(child) { Parent = this };
            this.Children.Add(childNode);

            //this.RegisterChildForSearch(childNode);

            return childNode;
        }

        public void AddChildren(LinkedListNode<Field> indexer)
        {
            var childNode = AddChild(indexer.Value);
            if (indexer.Next != null)
            {
                if (childNode.Data.ResultRange.End.ToInt() > indexer.Next.Value.ResultRange.End.ToInt())
                {
                    childNode.AddChildren(indexer.Next);
                }
                //else if (Data.ResultRange.End.ToInt() > indexer.Next.Value.ResultRange.End.ToInt())
                //{
                //    AddChildren(indexer.Next);
                //}
            }
        }

        public override string ToString()
        {
            if (Children.Count > 0)
            {
                StringBuilder fullPack = new StringBuilder();
                fullPack.Append("(").AppendLine();
                fullPack.Append(Data != null ? "(" + Data.Range.Start.ToString() + ", " + Data.Range.End.ToString() + ")" + "{" + Data.ResultRange.Start.ToString() + ", " + Data.ResultRange.End.ToString() + "}" : "[data null]");
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
                return Data != null ? "(" + Data.Range.Start.ToString() + ", " + Data.Range.End.ToString() + ")" + "{" + Data.ResultRange.Start.ToString() + ", " + Data.ResultRange.End.ToString() + "}" : "[data null]";
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

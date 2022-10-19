using DevExpress.Snap;
using DevExpress.XtraRichEdit.API.Native;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SnapTestDocuments
{
    public class FieldTreeNode : IEnumerable<FieldTreeNode>
    {

        public Field Data { get; set; }

        private Tuple<String, int> FieldPrefix;
        private Tuple<String, int> FieldText;
        private Tuple<String, int> FieldSuffix;

        private Tuple<int, int> CoreRange;
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

        public bool IsFieldDirty
        {
            get
            {
                return !Data.Range.Length.Equals(CoreRange.Item2 - CoreRange.Item2);
            }
        }

        public void UpdateField(Tuple<String, int> fieldPrefix, Tuple<String, int> fieldText, Tuple<String, int> fieldSuffix)
        {
            CoreRange = new Tuple<int, int>(Data.Range.Start.ToInt(), Data.Range.End.ToInt());
            FieldPrefix = fieldPrefix ?? FieldPrefix;
            FieldText = fieldText ?? FieldText;
            FieldSuffix = fieldSuffix ?? FieldSuffix;
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

        public FieldTreeNode(Field data)
        {
            this.Data = data;
            this.children = new LinkedList<FieldTreeNode>();

            CoreRange = new Tuple<int, int>(data.Range.Start.ToInt(), data.Range.End.ToInt());
        }

        public FieldTreeNode AddChild(Field child)
        {
            FieldTreeNode childNode = new FieldTreeNode(child) { Parent = this };
            this.Children.Add(childNode);

            return childNode;
        }

        public void AddChild(LinkedListNode<Field> indexer)
        {
            AddChild(indexer.Value);
        }

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

        internal IEnumerable<Tuple<string, int>> GetCachedStringData()
        {
            if (FieldPrefix != null) 
                yield return FieldPrefix;
            if (FieldText != null)
                yield return FieldText;
            if (FieldSuffix != null)
                yield return FieldSuffix;
        }


        internal FieldTreeNode ClosestNeighborNodeField(int referenceLocation, ref int minimalDistance, Field locationField)
        {
            if (IsLeaf)
                return null;

            FieldTreeNode closestField = null;
            foreach (var childEntry in Children)
            {
                int fieldCheckPosition = childEntry.Data.ResultRange.End.ToInt();
                var distance = Math.Abs(referenceLocation - fieldCheckPosition);
                if (distance <= minimalDistance)
                {
                    closestField = childEntry;
                    minimalDistance = distance;
                }
                var closestChildField = childEntry.ClosestNeighborNodeField(referenceLocation, ref minimalDistance, locationField);
                if (closestChildField != null)
                {
                    closestField = closestChildField;
                }
            }
            return closestField;
        }

        public FieldTreeNode closestNextNodeField(int selectionPosition, ref int minimalDistance)
        {
            if (IsLeaf)
                return null;

            FieldTreeNode closestField = null;
            foreach (var childEntry in Children)
            {
                int fieldCheckPosition = childEntry.Data.ResultRange.End.ToInt();
                
                var distance = Math.Abs(selectionPosition - fieldCheckPosition);
                if (distance <= minimalDistance && (selectionPosition - fieldCheckPosition) < 0)
                {
                    closestField = childEntry;
                    minimalDistance = distance;
                }
                var closestChildField = childEntry.closestNextNodeField(selectionPosition, ref minimalDistance);
                if (closestChildField != null)
                {
                    closestField = closestChildField;
                }
            }
            return closestField;
        }


        public FieldTreeNode closestNodeField(int selectionPosition, ref int minimalDistance, bool fromEnd)
        {
            if (IsLeaf)
                return null;

            FieldTreeNode closestField = null;
            foreach (var childEntry in Children)
            {
                int fieldCheckPosition = childEntry.Data.ResultRange.End.ToInt();
                if (!fromEnd)
                {
                    fieldCheckPosition = childEntry.Data.ResultRange.Start.ToInt();
                }
                var distance = Math.Abs(selectionPosition - fieldCheckPosition);
                if (distance <= minimalDistance)
                {
                    closestField = childEntry;
                    minimalDistance = distance;
                }
                var closestChildField = childEntry.closestNodeField(selectionPosition, ref minimalDistance, fromEnd);
                if (closestChildField != null)
                {
                    closestField = closestChildField;
                }
            }
            return closestField;
        }

        #endregion
    }
}

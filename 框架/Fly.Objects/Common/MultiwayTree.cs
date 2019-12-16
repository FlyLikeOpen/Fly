using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fly.Objects.Common
{
    public class TreeNode<T>
    {
        public TreeNode(T value) : this(value, null, null)
        {
        }
        public TreeNode(T value, TreeNode<T> parentNode) : this(value, parentNode, null)
        {
        }
        public TreeNode(T value, List<TreeNode<T>> childList) : this(value, null, childList)
        {
        }
        public TreeNode(T value, TreeNode<T> parentNode, List<TreeNode<T>> childList)
        {
            this.ChildList = new List<TreeNode<T>>();
            this.Value = value;
            this.ParentNode = parentNode;
            this.Level = parentNode == null ? 0 : parentNode.Level + 1;
            this.SetChildList(childList);
        }
        public int Level { get; private set; }
        public T Value { get; private set; }
        public TreeNode<T> ParentNode { get; private set; }
        public List<TreeNode<T>> ChildList { get; private set; }
        public void AddChildNode(TreeNode<T> node)
        {
            ChildList.Add(node);
        }
        public void SetChildList(List<TreeNode<T>> childList)
        {
            if (childList == null || !childList.Any())
                return;
            foreach (var child in childList)
            {
                child.ParentNode = this;
                child.Level = Level + 1;
            }
            this.ChildList = childList;
        }
        public bool IsLeaf()
        {
            return ChildList == null || !ChildList.Any();
        }
    }
    public class MultiwayTree<T>
    {
        public TreeNode<T> Head { get; private set; }
        public MultiwayTree()
        {
            this.Head = null;
        }
        public MultiwayTree(T value)
        {
            this.Head = new TreeNode<T>(value);
        }
        public MultiwayTree(TreeNode<T> treeNode)
        {
            this.Head = treeNode;
        }
        public MultiwayTree(T value, List<TreeNode<T>> childList)
        {
            this.Head = new TreeNode<T>(value, childList);
        }
        public bool IsEmpty()
        {
            if (Head == null)
                return false;
            else
                return true;
        }
        public TreeNode<T> Search(T value)
        {
            return this.Search(this.Head, r => r.Equals(value));
        }
        public TreeNode<T> Search(Predicate<T> predicate)
        {
            return this.Search(this.Head, predicate);
        }
        private TreeNode<T> Search(TreeNode<T> root, Predicate<T> predicate)
        {
            if (root == null)
                return null;
            if (predicate(root.Value))
            {
                return root;
            }
            if (!root.IsLeaf())
            {
                foreach (var child in root.ChildList)
                {
                    Search(child, predicate);
                }
            }
            return null;
        }
        public void LevelOrder(Action<TreeNode<T>> handleNode)
        {
            LevelOrder(this.Head, handleNode,r=>r);
        }
        public void LevelOrder(Action<TreeNode<T>> handleNode, Func<IEnumerable<TreeNode<T>>, IEnumerable<TreeNode<T>>> handleChildList)
        {
            LevelOrder(this.Head, handleNode, list=> handleChildList(list));
        }
        public void LevelOrder(TreeNode<T> root, Action<TreeNode<T>> handleNode, Func<IEnumerable<TreeNode<T>>, IEnumerable<TreeNode<T>>> handleChildList)
        {
            if (root == null)
                return;
            var sb = new StringBuilder();
            Queue<TreeNode<T>> queue = new Queue<TreeNode<T>>();
            queue.Enqueue(root);
            while (queue.Any())
            {
                var tem = queue.Dequeue();
                handleNode(tem);
                if (!tem.IsLeaf())
                {
                    foreach (var child in handleChildList(tem.ChildList))
                    {
                        queue.Enqueue(child);
                    }
                }
            }
        }
    }
}

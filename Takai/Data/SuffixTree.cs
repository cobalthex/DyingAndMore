using System;
using System.Collections.Generic;

namespace Takai.Data
{
    public class SuffixTree<T> where T : System.Collections.IEnumerable
    {
        class Node
        {
            public T value;
            public Dictionary<T, Node> children;

            public Node(T value = default)
            {
                this.value = value;
            }
        }

        Node root;

        public SuffixTree() { }
        public SuffixTree(T rootValue)
        {
            root = new Node(rootValue);
        }
    }
}

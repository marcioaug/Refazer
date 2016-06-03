﻿using System.Collections.Generic;
using System.Linq;
using TreeElement.Spg.Node;

namespace TreeElement.Spg.Walker
{
    public class BFSWalker<T>
    {
        /// <summary>
        /// Breadth First Search traversal
        /// </summary>
        /// <param name="u">Node to be traversed</param>
        /// <returns></returns>
        public static List<ITreeNode<T>> BreadFirstSearch(ITreeNode<T> u)
        {
            var result = new List<ITreeNode<T>>();
            var dist = new Dictionary<ITreeNode<T>, int> { [u] = 0 };
            var q = new Queue<ITreeNode<T>>();
            q.Enqueue(u);

            while (q.Any())
            {
                ITreeNode<T> v = q.Dequeue();

                foreach (var c in v.Children)
                {
                    if (!dist.ContainsKey(c))
                    {
                        dist[v] = dist[u] + 1;
                        result.Add(c);
                        q.Enqueue(c);
                    }
                }
            }

            return result;
        }
    }
}
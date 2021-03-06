﻿using System.Linq;
using Microsoft.CodeAnalysis;
using RefazerFunctions.Spg.Witness;
using TreeEdit.Spg.Match;
using TreeElement.Spg.Node;

namespace RefazerFunctions.Bean
{
    public class K
    {
        /// <summary>
        /// Input tree
        /// </summary>
        private readonly TreeNode<SyntaxNodeOrToken> _input;
        /// <summary>
        /// Node in the input tree
        /// </summary>
        private readonly TreeNode<SyntaxNodeOrToken> _node;

        /// <summary>
        /// Construct a new XPath
        /// </summary>
        /// <param name="input">Input tree</param>
        /// <param name="node">Node in the input tree</param>
        public K(TreeNode<SyntaxNodeOrToken> input, TreeNode<SyntaxNodeOrToken> node)
        {
            _input = input;
            _node = node;
        }

        public int GetK(Pattern patternExample)
        {
            var pattern = patternExample.Tree;
            var currentTree = _input;
            var matches = MatchManager.Matches(currentTree, pattern);
            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                var compare = Semantics.FindChild(match, patternExample.XPath);
                if (compare != null && Match.IsEqual(compare.Value, _node.Value))
                {
                    return i + 1;
                }
            }
            return -int.MaxValue;
        }

        public int GetKParent(Pattern patternExample)
        {
            var pattern = patternExample.Tree;
            var parent = _input.Value.Parent.Parent;
            var currentTree = ConverterHelper.ConvertCSharpToTreeNode(parent);
            var matches = MatchManager.Matches(currentTree, pattern, _input);
            matches = matches.OrderByDescending(o => o.Start).ToList();
            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                var compare = Semantics.FindChild(match, patternExample.XPath);
                if (compare != null && Match.IsEqual(compare.Value, _node.Value))
                {
                   return i + 1;
                }
            }
            return -int.MaxValue;
        }

        public override string ToString()
        {
            return $"K({_input.Label} - {_input}, {_node.Label} - {_node})";
        }

        public override bool Equals(object obj)
        {
            if (obj is K) return true;
            return false;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
    }
}

﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Spg.TreeEdit.Node
{
    public class ConverterHelper
    {

        /// <summary>
        /// Convert a syntax tree to a TreeNode
        /// </summary>
        /// <param name="st">Syntax tree root</param>
        /// <returns>TreeNode</returns>
        public static ITreeNode<SyntaxNodeOrToken> ConvertCSharpToTreeNode(SyntaxNodeOrToken st)
        {
            var list = st.AsNode().ChildNodes();
            if (!list.Any())
            {
                return new TreeNode<SyntaxNodeOrToken>(st);
            }

            List<ITreeNode<SyntaxNodeOrToken>> children = new List<ITreeNode<SyntaxNodeOrToken>>();
            foreach (SyntaxNodeOrToken sot in st.AsNode().ChildNodes())
            {
                ITreeNode<SyntaxNodeOrToken> node = ConvertCSharpToTreeNode(sot);
                children.Add(node);
            }

            ITreeNode<SyntaxNodeOrToken> tree = new TreeNode<SyntaxNodeOrToken>(st, children);
            return tree;
        }

        public static SyntaxNodeOrToken ConvertTreeNodeToCSsharp(ITreeNode<SyntaxNodeOrToken> treeNode)
        {
            return null;
        }

        public static ITreeNode<SyntaxNodeOrToken> MakeACopy(ITreeNode<SyntaxNodeOrToken> st)
        {
            var list = st.Children;
            if (!list.Any())
            {
                return new TreeNode<SyntaxNodeOrToken>(st.Value);
            }

            List<ITreeNode<SyntaxNodeOrToken>> children = new List<ITreeNode<SyntaxNodeOrToken>>();
            foreach (ITreeNode<SyntaxNodeOrToken> sot in st.Children)
            {
                ITreeNode<SyntaxNodeOrToken> node = MakeACopy(sot);
                children.Add(node);
            }

            ITreeNode<SyntaxNodeOrToken> tree = new TreeNode<SyntaxNodeOrToken>(st.Value, children);
            return tree;
        }
    }
}
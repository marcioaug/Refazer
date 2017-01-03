﻿using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ProseFunctions.Substrings;
using TreeElement.Spg.Node;

namespace ProseFunctions.Substrings
{
    public class ConverterHelper
    {

        /// <summary>
        /// Convert a syntax tree to a TreeNode
        /// </summary>
        /// <param name="st">Syntax tree root</param>
        /// <returns>TreeNode</returns>
        public static TreeNode<SyntaxNodeOrToken> ConvertCSharpToTreeNode(SyntaxNodeOrToken st)
        {
            if (!Valid(st)) return null;

            var list = GetChildren(st); //st.AsNode().ChildNodes();
            if (!list.Any())
            {
                var treeNode = new TreeNode<SyntaxNodeOrToken>(st, new TLabel(st.Kind()));
                treeNode.Start = st.SpanStart;
                return treeNode;
            }

            List<TreeNode<SyntaxNodeOrToken>> children = new List<TreeNode<SyntaxNodeOrToken>>();
            foreach (SyntaxNodeOrToken sot in list)
            {
                TreeNode<SyntaxNodeOrToken> node = ConvertCSharpToTreeNode(sot);
                node.Start = sot.SpanStart;
                children.Add(node);
            }

            TreeNode<SyntaxNodeOrToken> tree = new TreeNode<SyntaxNodeOrToken>(st, new TLabel(st.Kind()), children);
            tree.Start = st.SpanStart;
            return tree;
        }

        public static bool Valid(SyntaxNodeOrToken st)
        {
            return st.IsNode /*|| IsAcessModifier(st) ||  IsModifier(st)*/ /*|| st.IsKind(SyntaxKind.IdentifierToken)*/;
        }

        private static List<SyntaxNodeOrToken> GetChildren(SyntaxNodeOrToken st)
        {
            var list = new List<SyntaxNodeOrToken>();
            foreach (var v in st.ChildNodesAndTokens())
            {
                if(Valid(v))
                { 
                    list.Add(v);
                }
            }
            return list;
        }

        public static bool IsAcessModifier(SyntaxNodeOrToken st)
        {
            if (st.AsNode() != null) return false;
            switch (st.Kind())
            {
                case SyntaxKind.PublicKeyword:
                    return true;
                case SyntaxKind.ProtectedKeyword:
                    return true;
                case SyntaxKind.InternalKeyword:
                    return true;
                case SyntaxKind.PrivateKeyword:
                    return true;
            }
            return false;
        }

        public static bool IsModifier(SyntaxNodeOrToken st)
        {
            if (st.AsNode() != null) return false;
            switch (st.Kind())
            {
                case SyntaxKind.AbstractKeyword:
                    return true;
                case SyntaxKind.AsyncKeyword:
                    return true;
                case SyntaxKind.ConstKeyword:
                    return true;
                case SyntaxKind.EventKeyword:
                    return true;
                case SyntaxKind.ExternKeyword:
                    return true;
                case SyntaxKind.NewKeyword:
                    return true;
                case SyntaxKind.OverrideKeyword:
                    return true;
                case SyntaxKind.PartialKeyword:
                    return true;
                case SyntaxKind.ReadOnlyKeyword:
                    return true;
                case SyntaxKind.SealedKeyword:
                    return true;
                case SyntaxKind.StaticKeyword:
                    return true;
                case SyntaxKind.UnsafeKeyword:
                    return true;
                case SyntaxKind.VirtualKeyword:
                    return true;
                case SyntaxKind.VolatileKeyword:
                    return true;
            }
            return false;
        }

        public static TreeNode<Token> ConvertITreeNodeToToken(TreeNode<SyntaxNodeOrToken> st)
        {
            var token = new Token(st.Value.Kind(), st);
            if (!st.Children.Any())
            {
                var dtoken = new DynToken(st.Value.Kind(), st);
                var dtreeNode = new TreeNode<Token>(dtoken, new TLabel(dtoken.Kind));
                return dtreeNode;
            }
            var children = new List<TreeNode<Token>>();
            foreach (var sot in st.Children)
            {
                var node = ConvertITreeNodeToToken(sot);
                children.Add(node);
            }
            var tree = new TreeNode<Token>(token, new TLabel(token.Kind), children);
            return tree;
        }

        public static SyntaxNodeOrToken ConvertTreeNodeToCSsharp(TreeNode<SyntaxNodeOrToken> treeNode)
        {
            return null;
        }

        public static TreeNode<T> MakeACopy<T>(TreeNode<T> st)
        {
            var list = st.Children;
            if (!list.Any())
            {
                return new TreeNode<T>(st.Value, st.Label);
            }

            List<TreeNode<T>> children = new List<TreeNode<T>>();
            foreach (TreeNode<T> sot in st.Children)
            {
                TreeNode<T> node = MakeACopy(sot);
                children.Add(node);
            }

            TreeNode<T> tree = new TreeNode<T>(st.Value, st.Label, children);
            return tree;
        }

        public static TreeNode<T> TreeAtHeight<T>(TreeNode<T> st, Dictionary<TreeNode<T>, int> dist, int height)
        {
            var list = st.Children;
            if (!list.Any() || dist[st] >= height)
            {
                return new TreeNode<T>(st.Value, st.Label);
            }

            List<TreeNode<T>> children = new List<TreeNode<T>>();
            foreach (TreeNode<T> sot in st.Children)
            {
                TreeNode<T> node = TreeAtHeight(sot, dist, height);
                children.Add(node);
            }

            TreeNode<T> tree = new TreeNode<T>(st.Value, st.Label, children);
            return tree;
        }

        /// <summary>
        /// Convert a syntax tree to a TreeNode
        /// </summary>
        /// <param name="st">Syntax tree root</param>
        /// <returns>TreeNode</returns>
        public static string ConvertTreeNodeToString<T>(TreeNode<T> st)
        {           
            var list = st.Children;
            if (!list.Any())
            {
                var content = st.Value.ToString().Trim();
                //if (st.IsLabel(new TLabel(SyntaxKind.StringLiteralExpression)))
                //{
                //    var tNode = "{" + st.Label + "}";
                //    return tNode;
                //}
                if (st.IsLabel(new TLabel(SyntaxKind.StringLiteralExpression)))
                {
                    content = Regex.Replace(content, "[^0-9a-zA-Z\"]+", " ");
                }
                var treeNode = "{"+st.Label+"("+content+")}";             
                return treeNode;
            }
            var tree = "{"+ st.Label;
            foreach (var sot in st.Children)
            {
                var node = ConvertTreeNodeToString(sot);
                tree += node;
            }

            tree += "}";
            return tree;
        }
    }
}

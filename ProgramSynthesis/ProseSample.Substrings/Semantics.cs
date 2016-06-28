﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProseSample.Substrings.List;
using ProseSample.Substrings.Spg.Semantic;
using TreeEdit.Spg.TreeEdit.Update;
using TreeElement;
using TreeElement.Spg.Node;

namespace ProseSample.Substrings
{
    public static class Semantics
    {
        //Before transformation mapping
        private static readonly Dictionary<Node, List<SyntaxNodeOrToken>> BeforeAfterMapping = new Dictionary<Node, List<SyntaxNodeOrToken>>();

        //After transformation mapping
        private static readonly Dictionary<Node, List<SyntaxNodeOrToken>> MappingRegions = new Dictionary<Node, List<SyntaxNodeOrToken>>();

        /// <summary>
        /// Matches the element on the tree with specified kind and child nodes.
        /// </summary>
        /// <param name="kind">Syntax kind</param>
        /// <param name="children">Children nodes</param>
        /// <returns>The element on the tree with specified kind and child nodes</returns>
        public static Pattern C(SyntaxKind kind, IEnumerable<Pattern> children)
        {
            return SemanticMatch.C(kind, children);
        }

        /// <summary>
        /// Splits node in elements of kind type.
        /// </summary>
        /// <param name="node">Source node</param>
        /// <param name="kind">Syntax kind</param>
        /// <returns>Elements of kind type</returns>
        public static List<ITreeNode<SyntaxNodeOrToken>> SplitToNodes(ITreeNode<SyntaxNodeOrToken> node, SyntaxKind kind)
        {
            TLabel label = new TLabel(kind);
            var descendantNodes = node.DescendantNodesAndSelf();

            var kinds = from k in descendantNodes
                where k.IsLabel(label)
                select k;
            return kinds.ToList();
        }

        /// <summary>
        /// Return the tree
        /// </summary>
        /// <param name="variable">Result of a match result</param>
        /// <returns></returns>
        public static Pattern Tree(Pattern variable)
        {
            return variable;
        }

        /// <summary>
        /// Searches a node with with kind and occurrence
        /// </summary>
        /// <param name="kind">Kind</param>
        /// <returns>Search result</returns>
        public static Pattern Variable(SyntaxKind kind)
        {
            return SemanticMatch.Variable(kind);
        }

        public static Pattern Parent(Pattern variable, int k)
        {
            //var currentTree = GetCurrentTree(node);
            //var child = TreeUpdate.FindNode(currentTree, variable.Match.Item1.Value).Children.ElementAt(k - 1);
            //var result = new MatchResult(Tuple.Create(child, new Bindings(new List<SyntaxNodeOrToken> { child.Value })));
            //return result;
            return null;
        }

        /// <summary>
        /// Literal
        /// </summary>
        /// <param name="tree">Value</param>
        /// <returns>Literal</returns>
        public static Pattern Literal(SyntaxNodeOrToken tree)
        {
            return SemanticMatch.Literal(tree);
        }

        /// <summary>
        /// Insert the newNode node as in the k position of the node in the matching result 
        /// </summary>
        /// <param name="node">Input data</param>
        /// <param name="k">Position in witch the node will be inserted.</param>
        /// <param name="newNode">Node that will be insert</param>
        /// <returns>New node with the newNode node inserted as the k child</returns>
        public static Node Insert(Node node, Node newNode, int k)
        {
            return EditOperation.Insert(node, newNode, k);
        }

        /// <summary>
        /// Move edit operation
        /// </summary>
        /// <param name="k">Child index</param>
        /// <param name="from">Moved node</param>
        /// <returns></returns>
        public static Node Move(Node node, Pattern from, int k)
        {
            return EditOperation.Move(node, from, k);
        }

        /// <summary>
        /// Update edit operation
        /// </summary>
        /// <param name="node">Input node</param>
        /// <param name="to">New value</param>
        /// <returns></returns>
        public static Node Update(Node node, Node to)
        {
            return EditOperation.Update(node, to);
        }

        /// <summary>
        /// Delete edit operation
        /// </summary>
        /// <param name="node">Input node</param>
        /// <returns>Result of the edit opration</returns>
        public static Node Delete(Node node, string from)
        {
            return EditOperation.Delete(node);
        }

        /// <summary>
        /// Script semantic function
        /// </summary>
        /// <param name="node">Node</param>
        /// <param name="patch">Edit operations</param>
        /// <returns>Transformed node.</returns>
        public static Node Script(Node node, Patch patch)
        {
            var current = node.Value;
            var afterFlorest = current.Children.Select(ReconstructTree).ToList();
            MappingRegions[node] = afterFlorest;

            var beforeFlorest = patch.Edits.Select(o => o.ToList()).ToList();

            //node.Value.AddChild(ConverterHelper.ConvertCSharpToTreeNode(SyntaxFactory.EmptyStatement()), 0);

            return node;
        }

        /// <summary>
        /// Return a new node
        /// </summary>
        /// <param name="kind">Returned node SyntaxKind</param>
        /// <param name="childrenNodes">Children nodes</param>
        /// <returns>A new node with kind and child</returns>
        public static Node Node(SyntaxKind kind, IEnumerable<Node> childrenNodes)
        {
            var childrenList = (List<Node>) childrenNodes;

            if (!childrenList.Any()) return null;

            var parent = childrenNodes.First().Value.Parent;

            for (int i = 0; i < childrenList.Count(); i++)
            {
                var child = childrenList.ElementAt(i).Value;
                parent.AddChild(child, i);
            }

            var node = new Node(parent);
            return node;
        }

        /// <summary>
        /// Create a constant node
        /// </summary>
        /// <param name="cst">Constant</param>
        /// <returns>A new constant node.</returns>
        public static Node Const(SyntaxNodeOrToken cst)
        {
            var parent = new TreeNode<SyntaxNodeOrToken>(cst.Parent, new TLabel(cst.Parent.Kind()));
            var itreeNode = new TreeNode<SyntaxNodeOrToken>(cst, new TLabel(cst.Kind()));
            itreeNode.Parent = parent;
            var node = new Node(itreeNode);
            return node;
        }

        /// <summary>
        /// Reference semantic function
        /// </summary>
        /// <param name="node">Node</param>
        /// <param name="result">Result of the pattern</param>
        /// <returns>Result of the pattern</returns>
        public static Node Ref(Node node, Pattern result)
        {
            var nodes = EditOperation.Matches(node.Value, result);
            if (nodes.Count > 1) throw new Exception("More than one element is not allowed.");
            return new Node(nodes.First(), node.Value);
        }

        public static IEnumerable<Pattern> CList(Pattern child1, IEnumerable<Pattern> cList)
        {
            return GList<Pattern>.List(child1, cList);
        }

        public static IEnumerable<Pattern> SC(Pattern child)
        {
            return GList<Pattern>.Single(child);
        }

        public static IEnumerable<Pattern> PList(Pattern child1, IEnumerable<Pattern> cList)
        {
            return GList<Pattern>.List(child1, cList);
        }

        public static IEnumerable<Pattern> SP(Pattern child)
        {
            return GList<Pattern>.Single(child);
        }

        public static IEnumerable<Node> NList(Node child1, IEnumerable<Node> cList)
        {
            return GList<Node>.List(child1, cList);
        }

        public static IEnumerable<Node> SN(Node child)
        {
            return GList<Node>.Single(child);
        }

        public static Patch EList(IEnumerable<Node> child1, Patch cList)
        {
            var editList =  GList<IEnumerable<Node>>.List(child1, cList.Edits).ToList();
            var patch = new Patch(editList);
            return patch;
        }

        public static Patch SE(IEnumerable<Node> child)
        {
            var editList =  GList<IEnumerable<Node>>.Single(child).ToList();
            var list = editList.ToList();
            var patch = new Patch(editList);
            return patch;
        }

        public static IEnumerable<SyntaxNodeOrToken> SplitNodes(SyntaxNodeOrToken n)
        {
            SyntaxKind targetKind = SyntaxKind.MethodDeclaration;
            SyntaxNode node = n.AsNode();
            var nodes = from snode in node.DescendantNodes()
                        where snode.IsKind(targetKind)
                        select snode;

            return nodes.Select(snot => (SyntaxNodeOrToken)snot).ToList();
        }

        public static bool FTrue()
        {
            return true;
        }

        public static SyntaxNodeOrToken Transformation(SyntaxNodeOrToken node, IEnumerable<Node> loop)
        {
            List<SyntaxNodeOrToken> afterNodeList;
            List<SyntaxNodeOrToken> beforeNodeList;
            FillBeforeAfterList(out afterNodeList, loop, out beforeNodeList);
            //traversal index of the nodes before the transformation
            var traversalIndices = PostOrderTraversalIndices(node, beforeNodeList);
            //Annotate edited nodes
            node = AnnotateNodeEditedNodes(node, traversalIndices);
            //Update annotated nodes
            node = UpdateAnnotatedNodes(node, traversalIndices, afterNodeList);
            var stringNode = node.ToFullString();
            return node;
        }

        private static SyntaxNodeOrToken UpdateAnnotatedNodes(SyntaxNodeOrToken node, List<int> traversalIndices, List<SyntaxNodeOrToken> afterNodeList)
        {
            for (int i = 0; i < traversalIndices.Count; i++)
            {
                var index = traversalIndices[i];
                var snode = node.AsNode().GetAnnotatedNodes($"ANN{index}");
                if (snode.Any())
                {
                    var rewriter = new UpdateTreeRewriter(snode.First(), afterNodeList.ElementAt(i).AsNode());
                    node = rewriter.Visit(node.AsNode());
                }
            }
            return node;
        }

        private static SyntaxNodeOrToken AnnotateNodeEditedNodes(SyntaxNodeOrToken node, List<int> traversalIndices)
        {
            foreach (var index in traversalIndices)
            {           
                var treeNode = ConverterHelper.ConvertCSharpToTreeNode(node);
                var traversalNodes = treeNode.DescendantNodesAndSelf();
                var ann = new AddAnnotationRewriter(traversalNodes.ElementAt(index).Value.AsNode(),
                    new List<SyntaxAnnotation> {new SyntaxAnnotation($"ANN{index}")});
                node = ann.Visit(node.AsNode());
            }
            return node;
        }

        private static void FillBeforeAfterList(out List<SyntaxNodeOrToken> afterNodeList, IEnumerable<Node> loop, out List<SyntaxNodeOrToken> beforeNodeList)
        {
            afterNodeList = new List<SyntaxNodeOrToken>();
            beforeNodeList = new List<SyntaxNodeOrToken>();
            var list = loop.ToList();
            foreach (var snode in list)
            {
                afterNodeList.AddRange(MappingRegions[snode]);
                beforeNodeList.AddRange(BeforeAfterMapping[snode]);
            }
        }

        private static List<int> PostOrderTraversalIndices(SyntaxNodeOrToken node, List<SyntaxNodeOrToken> beforeNodeList)
        {
            var treeNode = ConverterHelper.ConvertCSharpToTreeNode(node);
            var traversalNodes = treeNode.DescendantNodesAndSelf();
            var traversalIndices = new List<int>();

            for (int i = 0; i < traversalNodes.Count; i++)
            {
                var snode = traversalNodes[i];
                foreach (var v in beforeNodeList)
                {
                    if (snode.Value.Equals(v))
                    {
                        traversalIndices.Add(i);
                    }
                }
            }
            return traversalIndices;
        }

        public static bool NodeMatch(Node sx, Pattern template)
        {
            return IsValue(sx.Value, template.Tree);
        }

        public static Pattern Match(Node target, Pattern kmatch, int k)
        {
            return null;
        }

        public static IEnumerable<Node> Template(Node node, Pattern pattern)
        {
            var currentTree = node.Value;//ConverterHelper.ConvertCSharpToTreeNode(node);
            var res = new List<Node>();
            if (pattern.Tree.Value.Kind == SyntaxKind.EmptyStatement)
            {
                var list = FlorestByKind(pattern, currentTree);

                if (list.Any()) res = CreateRegions(list);
            }

            res = SingleLocations(res);
            return res;
        }

        public static IEnumerable<Node> Traversal(Node node, string type)
        {
            var traversal = new TreeTraversal<SyntaxNodeOrToken>();
            var itreenode = node.Value;
            var nodes = traversal.PostOrderTraversal(itreenode);
            var result = nodes.Select(o => new Node(o, node.Value)).ToList();
            return result;
        }

        private static List<Node> SingleLocations(List<Node> res)
        {
            var list = new List<Node>();
            var candidates = new List<Tuple<Node, Node>> ();
            bool [] intersect = new bool[res.Count]; 

            for (int i = 0; i < res.Count; i++)
            {
                var v = res[i].Value.Value;
                for (int j =  i + 1; j < res.Count; j++)
                {
                    var c = res[j].Value.Value;
                    if (v.Span.Contains(c.Span) || c.Span.Contains(v.Span))
                    {
                        intersect[i] = true;
                        intersect[j] = true;
                        candidates.Add(Tuple.Create(res[i], res[j]));
                        break;
                    }
                }

                if (!intersect[i])
                {
                    list.Add(res[i]);
                }
            }

            list.AddRange(candidates.Select(v => v.Item1.Value.Value.Span.Contains(v.Item2.Value.Value.Span) ? v.Item2 : v.Item1));
            return list.OrderBy(o => o.Value.Value.SpanStart).ToList();
        }

        private static List<Node> CreateRegions(List<List<SyntaxNodeOrToken>> list)
        {
            var regions = new List<Node>();
            for (int j = 0; j < list.First().Count; j++)
            {
                ITreeNode<SyntaxNodeOrToken> iTree = new TreeNode<SyntaxNodeOrToken>(SyntaxFactory.EmptyStatement(), new TLabel(SyntaxKind.EmptyStatement));
                for (int i = 0; i < list.Count; i++)
                {
                    var child = list[i][j];
                    var newchild = ConverterHelper.ConvertCSharpToTreeNode(child);
                    iTree.AddChild(newchild, i);
                }

                var beforeFlorest = iTree.Children.Select(o => o.Value).ToList();
                var emptyStatement = SyntaxFactory.EmptyStatement();
                var newtree = new TreeNode<SyntaxNodeOrToken>(emptyStatement, new TLabel(SyntaxKind.EmptyStatement));
                newtree.AddChild(iTree.Children.First(), 0);
                var newNode = new Node(newtree);
                BeforeAfterMapping[newNode] = beforeFlorest;

                regions.Add(newNode);
            }
            return regions;
        }

        private static List<List<SyntaxNodeOrToken>> FlorestByKind(Pattern match, ITreeNode<SyntaxNodeOrToken> currentTree)
        {
            var list = new List<List<SyntaxNodeOrToken>>();
            foreach (var child in match.Tree.Children)
            {
                var nodeList = SplitToNodes(currentTree, child.Value.Kind);
                var result = (from node in nodeList where IsValue(node, child) select node.Value).ToList();

                if (result.Any())
                {
                    list.Add(result);
                }
            }
            return list;
        }

        public static bool IsValue(ITreeNode<SyntaxNodeOrToken> snode, ITreeNode<Token> pattern)
        {
            if (!snode.Value.IsKind(pattern.Value.Kind)) return false; //root pattern
            foreach (var child in pattern.Children)
            {
                var valid = snode.Children.Any(tchild => IsValue(tchild, child));
                if (!valid) return false;
            }
            return true;
        }

        /// <summary>
        /// Syntax node factory. This method will be removed in future
        /// </summary>
        /// <param name="kind">SyntaxKind of the node that will be created.</param>
        /// <param name="children">Children nodes.</param>
        /// <param name="node">Node</param>
        /// <returns>A SyntaxNode with specific king and children</returns>
        private static SyntaxNodeOrToken GetSyntaxElement(SyntaxKind kind, List<SyntaxNodeOrToken> children, SyntaxNodeOrToken node = default(SyntaxNodeOrToken))
        {
            switch (kind)
            {
            case SyntaxKind.NameColon:
                {
                    var identifier = (IdentifierNameSyntax)children[0];
                    var nameColon = SyntaxFactory.NameColon(identifier);
                    return nameColon;
                }
            case SyntaxKind.LogicalAndExpression:
                {
                    var leftExpression = (ExpressionSyntax)children[0];
                    var rightExpresssion = (ExpressionSyntax)children[1];
                    var logicalAndExpression = SyntaxFactory.BinaryExpression(SyntaxKind.LogicalAndExpression,
                        leftExpression, rightExpresssion);
                    return logicalAndExpression;
                }
            case SyntaxKind.ExpressionStatement:
                {
                    ExpressionSyntax expression = (ExpressionSyntax)children.First();
                    ExpressionStatementSyntax expressionStatement = SyntaxFactory.ExpressionStatement(expression);
                    return expressionStatement;
                }
            case SyntaxKind.Block:
                {
                    var statetements = children.Select(child => (StatementSyntax)child).ToList();

                    var block = SyntaxFactory.Block(statetements);
                    return block;
                }
            case SyntaxKind.InvocationExpression:
                {
                    var expressionSyntax = (ExpressionSyntax)children[0];
                    ArgumentListSyntax argumentList = (ArgumentListSyntax)children[1];
                    var invocation = SyntaxFactory.InvocationExpression(expressionSyntax, argumentList);
                    return invocation;
                }
            case SyntaxKind.SimpleMemberAccessExpression:
                {
                    var expressionSyntax = (ExpressionSyntax)children[0];
                    var syntaxName = (SimpleNameSyntax)children[1];
                    var simpleMemberExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, expressionSyntax, syntaxName);
                    return simpleMemberExpression;
                }
            case SyntaxKind.ParameterList:
                {
                    var parameter = (ParameterSyntax)children[0];
                    var spal = SyntaxFactory.SeparatedList(new[] { parameter });
                    var parameterList = SyntaxFactory.ParameterList(spal);
                    return parameterList;
                }
            case SyntaxKind.Parameter:
                {
                    return children.First().Parent;
                }
            case SyntaxKind.ArgumentList:
                {
                    var listArguments = children.Select(child => (ArgumentSyntax)child).ToList();

                    var spal = SyntaxFactory.SeparatedList(listArguments);
                    var argumentList = SyntaxFactory.ArgumentList(spal);
                    return argumentList;
                }
            case SyntaxKind.Argument:
                if (children.Count() == 1)
                {
                    ExpressionSyntax s = (ExpressionSyntax)children.First();
                    var argument = SyntaxFactory.Argument(s);
                    return argument;
                }
                else
                {
                    var ncolon = (NameColonSyntax)children[0];
                    var expression = (ExpressionSyntax)children[1];
                    var argument = SyntaxFactory.Argument(ncolon, default(SyntaxToken), expression);
                    return argument;
                }
            case SyntaxKind.ParenthesizedLambdaExpression:
                {
                    var parameterList = (ParameterListSyntax)children[0];
                    var csharpbody = (CSharpSyntaxNode)children[1];
                    var parenthizedLambdaExpression = SyntaxFactory.ParenthesizedLambdaExpression(parameterList, csharpbody);
                    return parenthizedLambdaExpression;
                }
            case SyntaxKind.EqualsExpression:
                {
                    var left = (ExpressionSyntax)children[0];
                    var right = (ExpressionSyntax)children[1];
                    var equalsExpression = SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, left, right);
                    return equalsExpression;
                }
            case SyntaxKind.IfStatement:
                {
                    var condition = (ExpressionSyntax)children[0];
                    var statementSyntax = (StatementSyntax)children[1];
                    var ifStatement = SyntaxFactory.IfStatement(condition, statementSyntax);
                    return ifStatement;
                }
            case SyntaxKind.UnaryMinusExpression:
                {
                    ExpressionSyntax expression = (ExpressionSyntax)children[0];
                    var unary = SyntaxFactory.PrefixUnaryExpression(kind, expression);
                    return unary;
                }
            case SyntaxKind.ReturnStatement:
                {
                    ExpressionSyntax expression = (ExpressionSyntax)children[0];
                    var returnStatement = SyntaxFactory.ReturnStatement(expression);
                    return returnStatement;
                }
            case SyntaxKind.ElseClause:
                {
                    var statatementSyntax = (StatementSyntax)children[0];
                    var elseClause = SyntaxFactory.ElseClause(statatementSyntax);
                    return elseClause;
                }
            case SyntaxKind.IdentifierName:
                {
                    SyntaxToken stoken = (SyntaxToken)children.First();
                    var identifierName = SyntaxFactory.IdentifierName(stoken);
                    return identifierName;
                }
            case SyntaxKind.TypeArgumentList:
                {
                    var listType = children.Select(child => (TypeSyntax)child).ToList();

                    var typespal = SyntaxFactory.SeparatedList(listType);
                    var typeArgument = SyntaxFactory.TypeArgumentList(typespal);
                    return typeArgument;
                }
            case SyntaxKind.GenericName:
                {
                    var gName = (GenericNameSyntax)node;
                    var typeArg = (TypeArgumentListSyntax)children[0];
                    var genericName = SyntaxFactory.GenericName(gName.Identifier, typeArg);
                    return genericName;
                }
            case SyntaxKind.NotEqualsExpression:
                {
                    var leftExpression = (ExpressionSyntax)children[0];
                    var rightExpression = (ExpressionSyntax)children[1];
                    var notEqualsExpression = SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression,
                        leftExpression, rightExpression);
                    return notEqualsExpression;
                }
            }

            return null;
        }

        /// <summary>
        /// Reconstruct the tree
        /// </summary>
        /// <param name="tree">Tree in another format</param>
        /// <returns>Reconstructed tree</returns>
        public static SyntaxNodeOrToken ReconstructTree(ITreeNode<SyntaxNodeOrToken> tree)
        {
            if (!tree.Children.Any())
            {
                return tree.Value;
            }

            var children = tree.Children.Select(ReconstructTree).ToList();
            var node = GetSyntaxElement(tree.Value.Kind(), children, tree.Value);
            return node;
        }
    }
}

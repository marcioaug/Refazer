﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProseFunctions.List;
using ProseFunctions.Spg.Bean;
using ProseFunctions.Spg.Semantic;
using TreeEdit.Spg.Match;
using TreeElement;
using TreeElement.Spg.Node;
using TreeElement.Token;

namespace ProseFunctions.Substrings
{
    public static class Semantics
    {
        private static Dictionary<Node, Node> dicBeforeAfter = new Dictionary<Node, Node>();

        /// <summary>
        /// Matches the element on the tree with specified kind and child nodes.
        /// </summary>
        /// <param name="kind">Syntax kind</param>
        /// <param name="children">Children nodes</param>
        /// <returns>The element on the tree with specified kind and child nodes</returns>
        public static Pattern Pattern(string kind, IEnumerable<Pattern> children)
        {
            return MatchSemanticFunctions.C(new Label(kind), children);
        }

        /// <summary>
        /// Splits node in elements of kind type.
        /// </summary>
        /// <param name="node">Source node</param>
        /// <param name="kind">Syntax kind</param>
        /// <returns>Elements of kind type</returns>
        public static List<TreeNode<SyntaxNodeOrToken>> SplitToNodes(TreeNode<SyntaxNodeOrToken> node, SyntaxKind kind)
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
        /// Searches a node with kind and occurrence
        /// </summary>
        /// <param name="kind">Label</param>
        /// <returns>Search result</returns>
        public static Pattern Abstract(string kind)
        {
            return MatchSemanticFunctions.Variable(new Label(kind));
        }

        public static Pattern Context(Pattern match, string k)
        {
            var pattern = new Pattern(match.Tree, k);
            return pattern;
        }

        public static Pattern SContext(Pattern match)
        {
            var pattern = new Pattern(match.Tree, ".");
            return pattern;
        }

        public static Pattern ContextPPP(Pattern match, string k)
        {
            var pattern = new Pattern(match.Tree, k);
            return pattern;
        }

        /// <summary>
        /// Literal
        /// </summary>
        /// <param name="tree">Value</param>
        /// <returns>Literal</returns>
        public static Pattern Concrete(SyntaxNodeOrToken tree)
        {
            return MatchSemanticFunctions.Literal(tree);
        }

        /// <summary>
        /// Insert the newNode node as in the k position of the node in the matching result 
        /// </summary>
        /// <param name="target">Target node</param>
        /// <param name="k">Position in witch the node will be inserted.</param>
        /// <param name="newNode">Node that will be insert</param>
        /// <returns>New node with the newNode node inserted as the k child</returns>
        public static Node Insert(Node target, Node newNode, int k)
        {
            var result = EditOperationSemanticFunctions.Insert(target, newNode, k);
            dicBeforeAfter.Add(result, target);
            return result;
        }

        public static string Insert(string target, string newNode)
        {
            return null;
        }

        /// <summary>
        /// Insert the newNode node as in the k position of the node in the matching result 
        /// </summary>
        /// <param name="target">Target node</param>
        /// <param name="node">Input data</param>
        /// <param name="newNode">Node that will be insert</param>
        /// <returns>New node with the newNode node inserted as the k child</returns>
        public static Node InsertBefore(Node target, Node node, Node newNode)
        {
            var result = EditOperationSemanticFunctions.InsertBefore(target, node, newNode);
            dicBeforeAfter.Add(result, target);
            return result;
        }

        /// <summary>
        /// Update edit operation
        /// </summary>
        /// <param name="target">Target node</param>
        /// <param name="to">New value</param>
        public static Node Update(Node target, Node to)
        {
            var result = EditOperationSemanticFunctions.Update(target, to);
            dicBeforeAfter.Add(result, target);
            return result;
        }

        /// <summary>
        /// Delete edit operation
        /// </summary>
        /// <param name="target">target</param>
        /// <param name="node">Input node</param>
        /// <returns>Result of the edit operation</returns>
        public static Node Delete(Node target, Node node)
        {
            var result = EditOperationSemanticFunctions.Delete(target, node);
            dicBeforeAfter.Add(result, target);
            return result;
        }

        /// <summary>
        /// Script semantic function
        /// </summary>
        /// <param name="node">Node</param>
        /// <param name="patch">Edit operations</param>
        /// <returns>Transformed node.</returns>
        public static IEnumerable<Node> Transformation(Node node, Patch patch)
        {
            var beforeFlorest = patch.Edits.Select(o => o.ToList());

            var resultList = new List<Node>();
            foreach (var edited in beforeFlorest)
            {
                foreach (var v in edited)
                {
                    if (!v.Value.IsLabel(new TLabel(SyntaxKind.None)))
                    {
                        var before = dicBeforeAfter[v];

                        SyntaxNodeOrToken n;
                        if (v.LeftNode != null)
                        {
                            n = ReconstructTree(v.LeftNode.Value);
                        }
                        else if (v.RightNode != null)
                        {
                            n = ReconstructTree(v.RightNode.Value);
                        }
                        else
                        {
                            n = ReconstructTree(v.Value);
                        }
                        string expHome = Environment.GetEnvironmentVariable("EXP_HOME", EnvironmentVariableTarget.User);
                        string file = expHome + "beforeafter.txt";
                        string separator = "EndLine";
                        File.AppendAllText(file, $"{before.Value.Value.SpanStart}{separator}{before.Value.Value.Span.Length}{separator}{before.Value.Value}{separator}{n}{separator}{before.Value.Value.SyntaxTree.FilePath}{separator}");

                        resultList.Add(new Node(ConverterHelper.ConvertCSharpToTreeNode(n)));
                    }
                    else
                    {
                        var before = dicBeforeAfter[v];
                        var n = ReconstructTree(v.Value);
                        string expHome = Environment.GetEnvironmentVariable("EXP_HOME", EnvironmentVariableTarget.User);
                        string file = expHome + "beforeafter.txt";
                        string separator = "EndLine";
                        File.AppendAllText(file, $"{before.Value.Value.SpanStart}{separator}{before.Value.Value.Span.Length}{separator}{before.Value.Value}{separator}{n}{separator}{before.Value.Value.SyntaxTree.FilePath}{separator}");
                        var treeNode = new TreeNode<SyntaxNodeOrToken>(default(SyntaxNodeOrToken), new TLabel(SyntaxKind.None));

                        resultList.Add(new Node(treeNode));
                    }
                }
            }
            return resultList;
        }

        public static Node Transformation(Node node, string patch)
        {
            return null;
        }

        /// <summary>
        /// Return a new node
        /// </summary>
        /// <param name="kind">Returned node SyntaxKind</param>
        /// <param name="childrenNodes">Children nodes</param>
        /// <returns>A new node with kind and child</returns>
        public static Node Node(SyntaxKind kind, IEnumerable<Node> childrenNodes)
        {
            var childrenList = (List<Node>)childrenNodes;
            if (!childrenList.Any()) return null;
            TreeNode<SyntaxNodeOrToken> parent = new TreeNode<SyntaxNodeOrToken>(null, new TLabel(kind));
            SyntaxNodeOrToken nodevalue = null;

            if (childrenList.Any(o => o.Value.IsLabel(new TLabel(SyntaxKind.None))))
            {
                var treeNode = new TreeNode<SyntaxNodeOrToken>(default(SyntaxNodeOrToken), new TLabel(SyntaxKind.None));
                return new Node(treeNode);
            }
            for (int i = 0; i < childrenList.Count(); i++)
            {
                var child = childrenList.ElementAt(i).Value;
                parent.AddChild(child, i);
                if (child.Value.Parent.IsKind(kind))
                {
                    nodevalue = child.Value.Parent;
                }
            }
            if (nodevalue != null)
            {
                var copy = ConverterHelper.MakeACopy(ConverterHelper.ConvertCSharpToTreeNode(nodevalue));
                copy.Children = parent.Children;
                var node = new Node(copy);
                return node;
            }
            else
            {
                var node = new Node(parent);
                return node;
            }
        }

        /// <summary>
        /// Create a constant node
        /// </summary>
        /// <param name="cst">Constant</param>
        /// <returns>A new constant node.</returns>
        public static Node ConstNode(SyntaxNodeOrToken cst)
        {
            var parent = new TreeNode<SyntaxNodeOrToken>(cst.Parent, new TLabel(cst.Parent.Kind()));
            var itreeNode = new TreeNode<SyntaxNodeOrToken>(cst, new TLabel(cst.Kind()));
            itreeNode.Parent = parent;
            var node = new Node(itreeNode);
            return node;
        }

        public static string ConstNode(string cst)
        {
            return null;
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
            var editList = GList<IEnumerable<Node>>.List(child1, cList.Edits).ToList();
            var patch = new Patch(editList);
            return patch;
        }

        public static Patch SE(IEnumerable<Node> child)
        {
            var editList = GList<IEnumerable<Node>>.Single(child).ToList();
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

        public static bool Match(Node sx, Pattern template)
        {
            var patternP = template;
            var parent = FindParent(sx.Value, patternP.K);
            if (parent == null) return false;
            var isValue = MatchManager.IsValueEachChild(parent, template.Tree);
            if (!isValue) return false;

            var node = FindChild(parent, patternP.K);
            var isValid = node.Equals(sx.Value);
            if (isValid)
            {
                //File.AppendAllText(@"C:\Users\SPG-04\Desktop\codefragments.cf", $"{node.Value.Parent} \n {node.Value.SyntaxTree.FilePath}" + Environment.NewLine);
                string expHome = Environment.GetEnvironmentVariable("EXP_HOME", EnvironmentVariableTarget.User);
                string file = expHome + "codefragments.txt";
                string separator = "EndLine";
                File.AppendAllText(file, $"{node.Value.Span.Start}{separator}{node.Value.Span.Length}{separator}{node.Value}{separator}{node.Value.SyntaxTree.FilePath}{separator}");
            }
            return isValid;
        }

        private static TreeNode<SyntaxNodeOrToken> FindParent(TreeNode<SyntaxNodeOrToken> value, string s)
        {
            var matches = Regex.Matches(s, "[0-9]");
            var current = value;
            foreach (var match in matches)
            {
                if (current == null) return null;
                current = current.Parent;
            }
            return current;
        }

        public static TreeNode<T> FindChild<T>(TreeNode<T> parent, string s)
        {
            var matches = Regex.Matches(s, "[0-9]");
            var current = parent;
            foreach (Match match in matches)
            {
                var index = Int32.Parse(match.Groups[0].Value);
                if (index > current.Children.Count) return null;
                current = current.Children[index - 1];
            }
            return current;
        }


        public static Node Reference(Node target, Pattern kmatch, int k)
        {
            var patternP = kmatch;
            //var k = ki.GetK(kmatch);
            if (k >= 0)
            {
                var nodes = MatchManager.Matches(target.Value, kmatch.Tree);
                if (!nodes.Any())
                {
                    var treeNode = new TreeNode<SyntaxNodeOrToken>(default(SyntaxNodeOrToken), new TLabel(SyntaxKind.None));
                    return new Node(treeNode);
                }
                var match = nodes.ElementAt(k - 1);
                var node = FindChild(match, patternP.K);
                return new Node(node);
            }
            else
            {
                //k = ki.GetKParent(kmatch);
                var ancestor = ConverterHelper.ConvertCSharpToTreeNode(target.Value.Value.Parent.Parent);
                var nodes = MatchManager.Matches(ancestor, kmatch.Tree);
                if (nodes.Any())
                {
                    var matches = MatchManager.Matches(ancestor, kmatch.Tree, target.Value);
                    matches = matches.OrderByDescending(o => o.Start).ToList();
                    var match = matches.ElementAt(Math.Abs(k) - 1);
                    var node = FindChild(match, patternP.K);
                    return new Node(node);
                }
                var treeNode = new TreeNode<SyntaxNodeOrToken>(default(SyntaxNodeOrToken), new TLabel(SyntaxKind.None));
                return new Node(treeNode);
            }
        }  

        public static IEnumerable<Node> Traversal(Node node, string type)
        {
            var traversal = new TreeTraversal<SyntaxNodeOrToken>();
            var itreenode = node.Value;
            var nodes = traversal.PostOrderTraversal(itreenode).ToList();
            var result = new List<Node>();
            foreach (var n in nodes)
            {
                result.Add(new Node(n));
            }
            return result;
        }

        /// <summary>
        /// Syntax node factory. This method will be removed in future
        /// </summary>
        /// <param name="kind">SyntaxKind of the node that will be created.</param>
        /// <param name="children">Children nodes.</param>
        /// <param name="node">Node</param>
        /// <returns>A SyntaxNode with specific king and children</returns>
        private static SyntaxNodeOrToken GetSyntaxElement(SyntaxKind kind, List<SyntaxNodeOrToken> children, SyntaxNodeOrToken node = default(SyntaxNodeOrToken), List<SyntaxNodeOrToken> identifiers = null)
        {
            switch (kind)
            {
            case SyntaxKind.ArrayCreationExpression:
                {
                    var arrayType = (ArrayTypeSyntax)children[0];
                    var newToken = SyntaxFactory.Token(SyntaxKind.NewKeyword).WithTrailingTrivia(new List<SyntaxTrivia> { SyntaxFactory.Space });
                    var arrayCreation = SyntaxFactory.ArrayCreationExpression(newToken, arrayType, null);
                    return arrayCreation;
                }
            case SyntaxKind.ImplicitArrayCreationExpression:
                {
                    var initializerExpression = (InitializerExpressionSyntax)children[0];
                    var arrayCreation = SyntaxFactory.ImplicitArrayCreationExpression(initializerExpression);
                    return arrayCreation;
                }
            case SyntaxKind.ArrayRankSpecifier:
                {
                    var expressions = children.Select(o => (ExpressionSyntax)o);
                    var spal = SyntaxFactory.SeparatedList<ExpressionSyntax>(expressions);
                    var arrayRank = SyntaxFactory.ArrayRankSpecifier(spal);
                    return arrayRank;
                }
            case SyntaxKind.ArrayType:
                {
                    var typeSyntax = (TypeSyntax)children[0];
                    var arrayRankList =
                        children.Where(o => o.IsKind(SyntaxKind.ArrayRankSpecifier))
                            .Select(o => (ArrayRankSpecifierSyntax)o);
                    var syntaxList = new SyntaxList<ArrayRankSpecifierSyntax>();
                    syntaxList.AddRange(arrayRankList);
                    var arrayType = SyntaxFactory.ArrayType(typeSyntax, syntaxList);
                    return arrayType;
                }
            case SyntaxKind.CatchDeclaration:
                {
                    var typesyntax = (TypeSyntax)children[0];
                    var catchDeclaration = SyntaxFactory.CatchDeclaration(typesyntax);
                    return catchDeclaration;
                }
            case SyntaxKind.CatchClause:
                {
                    var catchDeclaration =
                        (CatchDeclarationSyntax)children.SingleOrDefault(o => o.IsKind(SyntaxKind.CatchDeclaration));
                    var catchFilter =
                        (CatchFilterClauseSyntax)children.SingleOrDefault(o => o.IsKind(SyntaxKind.CatchFilterClause));
                    var body = (BlockSyntax)children.SingleOrDefault(o => o.IsKind(SyntaxKind.Block));

                    var catchClause = SyntaxFactory.CatchClause(catchDeclaration, catchFilter, body);
                    return catchClause;
                }
            case SyntaxKind.TryStatement:
                {
                    var body = (BlockSyntax)children[0];
                    var catches =
                        children.Where(o => o.IsKind(SyntaxKind.CatchClause))
                            .Select(o => (CatchClauseSyntax)o)
                            .ToList();
                    var spal = new SyntaxList<CatchClauseSyntax>();
                    spal.AddRange(catches);
                    var finallyClause =
                        children.Where(o => o.IsKind(SyntaxKind.FinallyClause))
                            .Select(o => (FinallyClauseSyntax)o)
                            .SingleOrDefault();
                    var tryStatement = SyntaxFactory.TryStatement(body, spal, finallyClause);
                    return tryStatement;
                }
            case SyntaxKind.FinallyClause:
                {
                    var blockSyntax = (BlockSyntax)children[0];
                    var finallyClause = SyntaxFactory.FinallyClause(blockSyntax);
                    return finallyClause;
                }
            case SyntaxKind.ThrowStatement:
                {
                    var expressionSyntax = (ExpressionSyntax)children[0];
                    var throwStatement = SyntaxFactory.ThrowStatement(expressionSyntax);
                    return throwStatement;
                }
            case SyntaxKind.MethodDeclaration:
                {
                    var method = (MethodDeclarationSyntax)node;
                    if (identifiers != null && identifiers.Any(o => o.IsKind(SyntaxKind.IdentifierToken)))
                    {
                        var index = identifiers.FindIndex(o => o.IsKind(SyntaxKind.IdentifierToken));
                        var name = (SyntaxToken) identifiers[index];
                        method = method.WithIdentifier(name);
                    }

                    if (identifiers != null)
                    {
                        var modifiers = new List<SyntaxToken>();
                        if (identifiers.Any(ConverterHelper.IsAcessModifier))
                        {
                            var index = identifiers.FindIndex(ConverterHelper.IsAcessModifier);
                            modifiers.Add((SyntaxToken)identifiers[index]);
                        }

                        if (identifiers.Any(o => o.IsKind(SyntaxKind.SealedKeyword)))
                        {
                            var index = identifiers.FindIndex(o => o.IsKind(SyntaxKind.SealedKeyword));
                            modifiers.Add((SyntaxToken)identifiers[index]);
                        }

                        if (identifiers.Any(o => o.IsKind(SyntaxKind.StaticKeyword)))
                        {
                            var index = identifiers.FindIndex(o => o.IsKind(SyntaxKind.StaticKeyword));
                            modifiers.Add((SyntaxToken)identifiers[index]);
                        }

                        if (identifiers.Any(o => o.IsKind(SyntaxKind.OverrideKeyword)))
                        {
                            var index = identifiers.FindIndex(o => o.IsKind(SyntaxKind.OverrideKeyword));
                            modifiers.Add((SyntaxToken)identifiers[index]);
                        }

                        if (modifiers.Any())
                        {
                            method = method.AddModifiers(modifiers.ToArray());
                        }
                    }

                    if (children.Any(o => o.IsKind(SyntaxKind.ArrowExpressionClause)))
                    {
                        var index = children.FindIndex(o => o.IsKind(SyntaxKind.PredefinedType));
                        method = method.WithExpressionBody((ArrowExpressionClauseSyntax)children[index]);
                    }

                    if (children.Any(o => o.IsKind(SyntaxKind.AttributeList)))
                    {
                        var index = children.FindIndex(o => o.IsKind(SyntaxKind.AttributeList));
                        var syntaList = new SyntaxList<AttributeListSyntax>();
                        var attributeListSyntax = (AttributeListSyntax)children[index];
                        method = method.WithAttributeLists(syntaList);
                        method = method.AddAttributeLists(attributeListSyntax);
                    }

                    if (children.Any(o => o.IsKind(SyntaxKind.PredefinedType)))
                    {
                        var index = children.FindIndex(o => o.IsKind(SyntaxKind.PredefinedType));
                        method = method.WithReturnType((TypeSyntax)children[index]);
                    }

                    if (children.Any(o => o.IsKind(SyntaxKind.ParameterList)))
                    {
                        var index = children.FindIndex(o => o.IsKind(SyntaxKind.ParameterList));
                        method = method.WithParameterList((ParameterListSyntax)children[index]);
                    }

                    if (children.Any(o => o.IsKind(SyntaxKind.Block)))
                    {
                        var index = children.FindIndex(o => o.IsKind(SyntaxKind.Block));
                        method = method.WithBody((BlockSyntax)children[index]);
                    }

                    return method;
                }

                case SyntaxKind.PropertyDeclaration:
                {
                    var type = (TypeSyntax)children[0];
                    string name = null;

                    if (identifiers != null && identifiers.Any(o => o.IsKind(SyntaxKind.IdentifierToken)))
                    {
                        var index = identifiers.FindIndex(o => o.IsKind(SyntaxKind.IdentifierToken));
                        name = identifiers[index].ToString();
                    }

                    var property = SyntaxFactory.PropertyDeclaration(type, name);

                    if (identifiers != null)
                    {
                        var modifiers = new List<SyntaxToken>();
                        if (identifiers.Any(ConverterHelper.IsAcessModifier))
                        {
                            var index = identifiers.FindIndex(ConverterHelper.IsAcessModifier);
                            modifiers.Add((SyntaxToken)identifiers[index]);
                        }

                        if (identifiers.Any(o => o.IsKind(SyntaxKind.SealedKeyword)))
                        {
                            var index = identifiers.FindIndex(o => o.IsKind(SyntaxKind.SealedKeyword));
                            modifiers.Add((SyntaxToken)identifiers[index]);
                        }

                        if (identifiers.Any(o => o.IsKind(SyntaxKind.StaticKeyword)))
                        {
                            var index = identifiers.FindIndex(o => o.IsKind(SyntaxKind.StaticKeyword));
                            modifiers.Add((SyntaxToken)identifiers[index]);
                        }

                        if (identifiers.Any(o => o.IsKind(SyntaxKind.OverrideKeyword)))
                        {
                            var index = identifiers.FindIndex(o => o.IsKind(SyntaxKind.OverrideKeyword));
                            modifiers.Add((SyntaxToken)identifiers[index]);
                        }
                        
                        if (modifiers.Any())
                        {
                            property = property.AddModifiers(modifiers.ToArray());
                        }
                    }

                    if (children.Any(o => o.IsKind(SyntaxKind.ArrowExpressionClause)))
                    {
                        var index = children.FindIndex(o => o.IsKind(SyntaxKind.PredefinedType));
                        property = property.WithExpressionBody((ArrowExpressionClauseSyntax)children[index]);
                    }

                    if (children.Any(o => o.IsKind(SyntaxKind.AccessorList)))
                    {
                        var index = children.FindIndex(o => o.IsKind(SyntaxKind.AccessorList));
                        property = property.WithAccessorList((AccessorListSyntax)children[index]);
                    }

                    return property;
                }
            case SyntaxKind.CastExpression:
                {
                    var typeSyntax = (TypeSyntax)children[0];
                    var expressionSyntax = (ExpressionSyntax)children[1];
                    var castExpression = SyntaxFactory.CastExpression(typeSyntax, expressionSyntax);
                    return castExpression;
                }
            case SyntaxKind.SwitchSection:
                {
                    var labels =
                        children.Where(o => o.IsKind(SyntaxKind.CaseSwitchLabel))
                            .Select(o => (SwitchLabelSyntax)o)
                            .ToList();
                    var values =
                        children.Where(o => !o.IsKind(SyntaxKind.CaseSwitchLabel))
                            .Select(o => (StatementSyntax)o)
                            .ToList();
                    var labelList = SyntaxFactory.List(labels);
                    var valueList = SyntaxFactory.List(values);
                    var switchSection = SyntaxFactory.SwitchSection(labelList, valueList);
                    return switchSection;
                }
            case SyntaxKind.CaseSwitchLabel:
                {
                    var expressionSyntax = (ExpressionSyntax)children.First();
                    var caseSwitchLabel = SyntaxFactory.CaseSwitchLabel(expressionSyntax);
                    return caseSwitchLabel;
                }
            case SyntaxKind.QualifiedName:
                {
                    var leftSyntax = (NameSyntax)children[0];
                    var rightSyntax = (SimpleNameSyntax)children[1];
                    var qualifiedName = SyntaxFactory.QualifiedName(leftSyntax, rightSyntax);
                    return qualifiedName;
                }
            case SyntaxKind.NameEquals:
                {
                    var identifierNameSyntax = (IdentifierNameSyntax)children[0];
                    var nameEquals = SyntaxFactory.NameEquals(identifierNameSyntax);
                    return nameEquals;
                }
            case SyntaxKind.NameColon:
                {
                    var identifier = (IdentifierNameSyntax)children[0];
                    var nameColon = SyntaxFactory.NameColon(identifier);
                    return nameColon;
                }
            case SyntaxKind.GreaterThanExpression:
            case SyntaxKind.LessThanExpression:
            case SyntaxKind.LessThanOrEqualExpression:
            case SyntaxKind.DivideExpression:
            case SyntaxKind.MultiplyExpression:
            case SyntaxKind.BitwiseAndExpression:
            case SyntaxKind.BitwiseOrExpression:
            case SyntaxKind.AsExpression:
            case SyntaxKind.AddExpression:
            case SyntaxKind.SubtractExpression:
            case SyntaxKind.GreaterThanOrEqualExpression:
            case SyntaxKind.LogicalOrExpression:
            case SyntaxKind.LogicalAndExpression:
                {
                    var leftExpression = (ExpressionSyntax)children[0];
                    var rightExpresssion = (ExpressionSyntax)children[1];
                    var logicalAndExpression = SyntaxFactory.BinaryExpression(kind,
                        leftExpression, rightExpresssion);
                    return logicalAndExpression;
                }
            case SyntaxKind.SimpleAssignmentExpression:
                {
                    var leftExpression = (ExpressionSyntax)children[0];
                    var rightExpression = (ExpressionSyntax)children[1];
                    var simpleAssignment = SyntaxFactory.AssignmentExpression(kind, leftExpression, rightExpression);
                    return simpleAssignment;
                }
            case SyntaxKind.LocalDeclarationStatement:
                {
                    var variableDeclation = (Microsoft.CodeAnalysis.CSharp.Syntax.VariableDeclarationSyntax)children[0];
                    var localDeclaration = SyntaxFactory.LocalDeclarationStatement(variableDeclation);
                    return localDeclaration;
                }
            case SyntaxKind.ForEachStatement:
                {
                    var foreachStt = (ForEachStatementSyntax)node;
                    var identifier = foreachStt.Identifier;
                    var typesyntax = (TypeSyntax)children[0];
                    var expressionSyntax = (ExpressionSyntax)children[1];
                    var statementSyntax = (StatementSyntax)children[2];
                    var foreachstatement = SyntaxFactory.ForEachStatement(typesyntax, identifier, expressionSyntax,
                        statementSyntax);
                    return foreachstatement;
                }
            case SyntaxKind.UsingStatement:
                {

                    VariableDeclarationSyntax variableDeclaration = null;
                    ExpressionSyntax expression = null;
                    if (children[0].IsKind(SyntaxKind.VariableDeclaration))
                    {
                        variableDeclaration = (VariableDeclarationSyntax)children[0];
                    }
                    else
                    {
                        expression = (ExpressionSyntax)children[0];
                    }
                    var statementSyntax = (StatementSyntax)children[1];
                    var usingStatement = SyntaxFactory.UsingStatement(variableDeclaration, expression, statementSyntax);
                    return usingStatement;
                }
            case SyntaxKind.VariableDeclaration:
                {
                    var typeSyntax = (TypeSyntax)children[0];
                    var listArguments = new List<VariableDeclaratorSyntax>();
                    for (int i = 1; i < children.Count; i++)
                    {
                        var variable = (VariableDeclaratorSyntax)children[i];
                        listArguments.Add(variable);
                    }
                    var spal = SyntaxFactory.SeparatedList(listArguments);
                    var variableDeclaration = SyntaxFactory.VariableDeclaration(typeSyntax, spal);
                    return variableDeclaration;
                }
             case SyntaxKind.VariableDeclarator:
                {
                    var property = (VariableDeclaratorSyntax)node;
                    SyntaxToken identifier = default(SyntaxToken);
                    if (identifiers.Any(o => o.IsKind(SyntaxKind.IdentifierToken)))
                    {
                        var index = identifiers.FindIndex(o => o.IsKind(SyntaxKind.IdentifierToken));
                        identifier = (SyntaxToken) identifiers[index];
                    }
                    else
                    {
                        identifier = property.Identifier;
                    }
                    
                    EqualsValueClauseSyntax equalsExpression = null;
                    if (children.Any(o => o.IsKind(SyntaxKind.EqualsValueClause)))
                    {
                        var index = children.FindIndex(o => o.IsKind(SyntaxKind.EqualsValueClause));
                        equalsExpression = (EqualsValueClauseSyntax)children[index];
                    }               
                    var variableDeclaration = SyntaxFactory.VariableDeclarator(identifier, null, equalsExpression);
                    return variableDeclaration;
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
                    if (!identifiers.Any())
                    {
                        var expressionSyntax = (ExpressionSyntax)children[0];
                        ArgumentListSyntax argumentList = (ArgumentListSyntax)children[1];
                        var invocation = SyntaxFactory.InvocationExpression(expressionSyntax, argumentList);
                        return invocation;
                    }
                    else
                    {
                        var expressionSyntax =
                            (ExpressionSyntax)GetSyntaxElement(SyntaxKind.IdentifierName, null, null, identifiers);
                        ArgumentListSyntax argumentList = (ArgumentListSyntax)children[0];
                        var invocation = SyntaxFactory.InvocationExpression(expressionSyntax, argumentList);
                        return invocation;
                    }

                }
            case SyntaxKind.SimpleMemberAccessExpression:
                {
                    if (!identifiers.Any())
                    {
                        var expressionSyntax = (ExpressionSyntax)children[0];
                        var syntaxName = (SimpleNameSyntax)children[1];
                        var simpleMemberExpression =
                            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                expressionSyntax, syntaxName);
                        return simpleMemberExpression;
                    }
                    else
                    {
                        var expressionSyntax = (ExpressionSyntax)children[0];
                        var syntaxName =
                            (SimpleNameSyntax)GetSyntaxElement(SyntaxKind.IdentifierName, null, null, identifiers);
                        var simpleMemberExpression =
                            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                expressionSyntax, syntaxName);
                        return simpleMemberExpression;
                    }
                }
            case SyntaxKind.ElementAccessExpression:
                {
                    var expressionSyntax = (ExpressionSyntax)children[0];
                    var bracketArgumentList = (BracketedArgumentListSyntax)children[1];
                    var elementAccessExpression = SyntaxFactory.ElementAccessExpression(expressionSyntax,
                        bracketArgumentList);
                    return elementAccessExpression;
                }
            case SyntaxKind.TypeOfExpression:
                {
                    var typeSyntax = (TypeSyntax)children[0];
                    var typeofExpression = SyntaxFactory.TypeOfExpression(typeSyntax);
                    return typeofExpression;
                }
            case SyntaxKind.ObjectCreationExpression:
                {
                    var typeSyntax = (TypeSyntax)children[0];
                    ArgumentListSyntax argumentList = null;
                    InitializerExpressionSyntax initializer = null;
                    if (children[1].IsKind(SyntaxKind.ArgumentList))
                    {
                        argumentList = (ArgumentListSyntax)children[1];
                    }
                    else
                    {
                        initializer = (InitializerExpressionSyntax)children[1];
                    }
                    var newToken =
                        SyntaxFactory.Token(SyntaxKind.NewKeyword)
                            .WithTrailingTrivia(new List<SyntaxTrivia> { SyntaxFactory.Space });
                    var objectcreation = SyntaxFactory.ObjectCreationExpression(newToken, typeSyntax, argumentList,
                        initializer);
                    return objectcreation;
                }
            case SyntaxKind.ParameterList:
                {
                    var parameterSyntaxList = new List<ParameterSyntax>();
                    children.ForEach(o => parameterSyntaxList.Add((ParameterSyntax)o));
                    var spal = SyntaxFactory.SeparatedList(parameterSyntaxList);
                    var parameterList = SyntaxFactory.ParameterList(spal);
                    return parameterList;
                }
            case SyntaxKind.ComplexElementInitializerExpression:
            case SyntaxKind.CollectionInitializerExpression:
            case SyntaxKind.ObjectInitializerExpression:
            case SyntaxKind.ArrayInitializerExpression:
                {
                    var expressionSyntaxs = children.Select(child => (ExpressionSyntax)child).ToList();
                    var spal = SyntaxFactory.SeparatedList(expressionSyntaxs);
                    var arrayInitializer = SyntaxFactory.InitializerExpression(kind, spal);
                    return arrayInitializer;
                }
            case SyntaxKind.Parameter:
                {
                    ParameterSyntax parameter;
                    if (node == null && children[0].IsKind(SyntaxKind.IdentifierName))
                    {
                        var name = (IdentifierNameSyntax)children[0];
                        parameter = SyntaxFactory.Parameter(name.Identifier);
                    }
                    else
                    {
                        parameter = (ParameterSyntax)node;
                    }

                    foreach (var c in children)
                    {
                        if (c.IsKind(SyntaxKind.GenericName))
                        {
                            var type = (TypeSyntax)c;
                            parameter = parameter.WithType(type);
                        }
                        if (c.IsKind(SyntaxKind.EqualsValueClause))
                        {
                            var equalsValue = (EqualsValueClauseSyntax)c;
                            parameter = parameter.WithDefault(equalsValue);
                        }
                    }
                    return parameter;
                }
            case SyntaxKind.BracketedArgumentList:
                {
                    var listArguments = children.Select(child => (ArgumentSyntax)child).ToList();
                    var spal = SyntaxFactory.SeparatedList(listArguments);
                    var bracketedArgumentList = SyntaxFactory.BracketedArgumentList(spal);
                    return bracketedArgumentList;
                }
            case SyntaxKind.Attribute:
                {
                    var name = (NameSyntax)children[0];
                    var atributeListSyntax = new List<AttributeArgumentSyntax>();
                    for (int i = 1; i < children.Count; i++)
                    {
                        try
                        {
                            atributeListSyntax.Add((AttributeArgumentSyntax)children[i]);
                        }
                        catch (Exception e)
                        {
                            var attributeListArgument = (AttributeArgumentListSyntax)children[1];
                            var att = SyntaxFactory.Attribute(name, attributeListArgument);
                            return att;
                        }
                    }
                    var spal = SyntaxFactory.SeparatedList(atributeListSyntax);
                    var atributeListArgument = SyntaxFactory.AttributeArgumentList(spal);
                    var attribute = SyntaxFactory.Attribute(name, atributeListArgument);
                    return attribute;
                }
            case SyntaxKind.AttributeArgument:
                {
                    if (children[0].IsKind(SyntaxKind.NameEquals))
                    {
                        var nameEqualsSyntax = (NameEqualsSyntax)children[0];
                        var expressionSyntax = (ExpressionSyntax)children[1];
                        var attributeArgument = SyntaxFactory.AttributeArgument(nameEqualsSyntax, null, expressionSyntax);
                        return attributeArgument;
                    }
                    else
                    {
                        var expressionSyntax = (ExpressionSyntax)children[0];
                        var attributeArgument = SyntaxFactory.AttributeArgument(null, null, expressionSyntax);
                        return attributeArgument;
                    }
                }
            case SyntaxKind.AttributeArgumentList:
                {
                    var atributeListSyntax = new List<AttributeArgumentSyntax>();
                    for (int i = 0; i < children.Count; i++)
                    {
                        atributeListSyntax.Add((AttributeArgumentSyntax)children[i]);
                    }
                    var spal = SyntaxFactory.SeparatedList(atributeListSyntax);
                    var atributeListArgument = SyntaxFactory.AttributeArgumentList(spal);
                    return atributeListArgument;
                }
            case SyntaxKind.AttributeList:
                {
                    var attributeSyntaxList = new List<AttributeSyntax>();
                    foreach (var v in children)
                    {
                        attributeSyntaxList.Add((AttributeSyntax)v);
                    }
                    var spal = SyntaxFactory.SeparatedList(attributeSyntaxList);
                    var attibuteList = SyntaxFactory.AttributeList(spal);
                    return attibuteList;
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
            case SyntaxKind.ParenthesizedExpression:
                {
                    var expressionSyntax = (ExpressionSyntax)children[0];
                    var parenthizedExpression = SyntaxFactory.ParenthesizedExpression(expressionSyntax);
                    return parenthizedExpression;
                }
            case SyntaxKind.SimpleLambdaExpression:
                {
                    var parameter = (ParameterSyntax)children[0];
                    var csharpbody = (CSharpSyntaxNode)children[1];
                    var simpleLambdaExpression = SyntaxFactory.SimpleLambdaExpression(parameter, csharpbody);
                    return simpleLambdaExpression;
                }
            case SyntaxKind.ParenthesizedLambdaExpression:
                {
                    var parameterList = (ParameterListSyntax)children[0];
                    var csharpbody = (CSharpSyntaxNode)children[1];
                    var parenthizedLambdaExpression = SyntaxFactory.ParenthesizedLambdaExpression(parameterList,
                        csharpbody);
                    return parenthizedLambdaExpression;
                }
            case SyntaxKind.EqualsExpression:
                {
                    var left = (ExpressionSyntax)children[0];
                    var right = (ExpressionSyntax)children[1];
                    var equalsExpression = SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, left, right);
                    return equalsExpression;
                }
            case SyntaxKind.EqualsValueClause:
                {
                    var expressionSyntax = (ExpressionSyntax)children[0];
                    var equalsValueClause = SyntaxFactory.EqualsValueClause(expressionSyntax);
                    return equalsValueClause;
                }
            case SyntaxKind.ConditionalExpression:
                {
                    var condition = (ExpressionSyntax)children[0];
                    var whenTrue = (ExpressionSyntax)children[1];
                    var whenFalse = (ExpressionSyntax)children[2];
                    var conditionalExpression = SyntaxFactory.ConditionalExpression(condition, whenTrue, whenFalse);
                    return conditionalExpression;
                }
            case SyntaxKind.IfStatement:
                {
                    var condition = (ExpressionSyntax)children[0];
                    var statementSyntax = (StatementSyntax)children[1];
                    var ifStatement = SyntaxFactory.IfStatement(condition, statementSyntax);
                    return ifStatement;
                }
            case SyntaxKind.PostIncrementExpression:
            case SyntaxKind.PostDecrementExpression:
            case SyntaxKind.PreIncrementExpression:
            case SyntaxKind.PreDecrementExpression:
            case SyntaxKind.LogicalNotExpression:
            case SyntaxKind.UnaryMinusExpression:
                {
                    ExpressionSyntax expression = (ExpressionSyntax)children[0];
                    var unary = SyntaxFactory.PrefixUnaryExpression(kind, expression);
                    return unary;
                }
            case SyntaxKind.YieldReturnStatement:
                {
                    var expression = (ExpressionSyntax)children[0];
                    var yieldReturn = SyntaxFactory.YieldStatement(kind, expression);
                    return yieldReturn;
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
                    SyntaxToken stoken = (SyntaxToken)identifiers.First();
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
            case SyntaxKind.SetAccessorDeclaration:
            case SyntaxKind.GetAccessorDeclaration:
                {
                    var blockSyntax = (BlockSyntax)children[0];
                    var getAcessor = SyntaxFactory.AccessorDeclaration(kind, blockSyntax);
                    return getAcessor;
                }
            case SyntaxKind.AccessorList:
                {
                    var list = children.Select(v => (AccessorDeclarationSyntax)v).ToList();
                    var syntaxList = new SyntaxList<AccessorDeclarationSyntax>();
                    syntaxList.AddRange(list);
                    var acessorList = SyntaxFactory.AccessorList();
                    acessorList = acessorList.AddAccessors(list.ToArray());
                    return acessorList;
                }
            }
            throw new Exception($"Ussupported Kind Support: {kind}");
        }

        /// <summary>
        /// Reconstruct the tree
        /// </summary>
        /// <param name="tree">Tree in another format</param>
        /// <returns>Reconstructed tree</returns>
        public static SyntaxNodeOrToken ReconstructTree(TreeNode<SyntaxNodeOrToken> tree)
        {
            if (!tree.Children.Any())
            {
                return tree.Value;
            }

            List<SyntaxNodeOrToken> children = new List<SyntaxNodeOrToken>();
            List<SyntaxNodeOrToken> identifier = new List<SyntaxNodeOrToken>();
            foreach (var v in tree.Children)
            {
                if (v.Value.IsNode)
                {
                    var result = ReconstructTree(v);
                    children.Add(result);
                }
                else
                {
                    identifier.Add(v.Value);
                }
            }
            var node = GetSyntaxElement((SyntaxKind)tree.Label.Label, children, tree.Value, identifier);
            return node.AsNode().NormalizeWhitespace();
        }
    }
}

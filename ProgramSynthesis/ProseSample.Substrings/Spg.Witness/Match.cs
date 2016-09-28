﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Rules;
using Microsoft.ProgramSynthesis.Specifications;
using TreeEdit.Spg.Match;
using TreeEdit.Spg.TreeEdit.Update;
using TreeElement.Spg.Node;

namespace ProseSample.Substrings.Spg.Witness
{
    public class Match
    {
        public static DisjunctiveExamplesSpec CKind(GrammarRule rule, int parameter, DisjunctiveExamplesSpec spec)
        {
            var kdExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (State input in spec.ProvidedInputs)
            {
                var syntaxKinds = new List<object>();
                foreach (TreeNode<SyntaxNodeOrToken> node in spec.DisjunctiveExamples[input])
                {
                    var sot = node.Value;
                    syntaxKinds.Add(sot.Kind());
                }
                kdExamples[input] = syntaxKinds;
            }
            return DisjunctiveExamplesSpec.From(kdExamples);
        }

        public static DisjunctiveExamplesSpec CChildren(GrammarRule rule, int parameter, DisjunctiveExamplesSpec spec, ExampleSpec kind)
        {
            var eExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (State input in spec.ProvidedInputs)
            {
                var matches = new List<List<TreeNode<SyntaxNodeOrToken>>>();
                foreach (TreeNode<SyntaxNodeOrToken> node in spec.DisjunctiveExamples[input])
                {
                    if (!node.Children.Any()) continue;
                    var lsot = node.Children;
                    matches.Add(lsot);
                }
                if (!matches.Any()) return null;
                eExamples[input] = matches;
            }
            return DisjunctiveExamplesSpec.From(eExamples);
        }

        public static DisjunctiveExamplesSpec MatchPattern(GrammarRule rule, int parameter, ExampleSpec spec)
        {
            var eExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (State input in spec.ProvidedInputs)
            {
                var kMatches = new List<TreeNode<SyntaxNodeOrToken>>();
                var target = (TreeNode<SyntaxNodeOrToken>)input[rule.Body[0]];
                foreach (TreeNode<SyntaxNodeOrToken> node in spec.DisjunctiveExamples[input])
                {
                    var found = TreeUpdate.FindNode(target, node);
                    if (found == null) continue;

                    kMatches.Add(node);

                    //var parent = node.Parent;
                    //if (parent != null)
                    //{
                    //    kMatches.Add(Tuple.Create(parent, node));
                    //}
                }
                if (!kMatches.Any()) return null;
                eExamples[input] = kMatches;
            }
            return new DisjunctiveExamplesSpec(eExamples);
        }

        public static DisjunctiveExamplesSpec MatchK(GrammarRule rule, int parameter, ExampleSpec spec, ExampleSpec kind)
        {
            var kExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (State input in spec.ProvidedInputs)
            {
                var mats = new List<object>();
                var patternExample = (Pattern)kind.Examples[input];
                var pattern = patternExample.Tree;
                var target = (TreeNode<SyntaxNodeOrToken>)input[rule.Body[0]];
                //var inputTree = (Node)input[rule.Grammar.InputSymbol];

                foreach (TreeNode<SyntaxNodeOrToken> node in spec.DisjunctiveExamples[input])
                {
                    var currentTree = WitnessFunctions.GetCurrentTree(node.SyntaxTree);
                    var matches = MatchManager.Matches(currentTree, pattern);

                    for (int i = 0; i < matches.Count; i++)
                    {
                        var match = matches[i];
                        //TreeNode<SyntaxNodeOrToken> compare = null;
                        //if (patternExample is PatternP)
                        //{
                        //    var patternP = patternExample as PatternP;
                            var compare = Semantics.FindChild(match, patternExample.K);
                        //}
                        //else
                        //{
                        //    compare = node;
                        //}

                        if (compare != null && compare.Equals(node))
                        {
                            mats.Add(i + 1);
                        }
                    }
                }
                if (!mats.Any()) return null;
                kExamples[input] = mats;
            }
            return DisjunctiveExamplesSpec.From(kExamples);
        }
    }
}

/*public static DisjunctiveExamplesSpec MatchPattern(GrammarRule rule, int parameter, DisjunctiveExamplesSpec spec)
        {
            var eExamples = new Dictionary<State, IEnumerable<object>>();
            var patterns = new List<TreeNode<Token>>();
            var indexChildList = new Dictionary<State, int>();
            foreach (State input in spec.ProvidedInputs)
            {
                var target = (TreeNode<SyntaxNodeOrToken>)input[rule.Body[0]];
                var inputTree = (Node)input[rule.Grammar.InputSymbol];
                var currentTree = WitnessFunctions.GetCurrentTree(target);
                var examples = spec.DisjunctiveExamples[input].Select(o => (TreeNode<SyntaxNodeOrToken>) o).ToList();

                if (examples.First().Equals(target) || target.Value.AsNode() != null && target.Value.AsNode().DescendantNodesAndSelf().Count() >= 100)
                {
                    return MatchPatternBasic(rule, parameter, spec);
                }

                var matchInInputTree = TreeUpdate.FindNode(currentTree, examples.First());
                if (matchInInputTree == null)
                {
                    currentTree = ConverterHelper.ConvertCSharpToTreeNode(target.Value.Parent.Parent);
                    matchInInputTree = TreeUpdate.FindNode(currentTree, examples.First());
                }

                if (matchInInputTree == null) return null;

                var parent = matchInInputTree.Value.Parent;
                var topattern = ConverterHelper.ConvertCSharpToTreeNode(parent);
                var targetIndex = topattern.Children.FindIndex(o => o.Equals(matchInInputTree));

                if (targetIndex == -1) return null;
                indexChildList.Add(input, targetIndex);

                var pattern = ConverterHelper.ConvertITreeNodeToToken(topattern);
                foreach (var o in pattern.DescendantNodesAndSelf())
                {
                    var value = TreeUpdate.FindNode(inputTree.Value, o.Value.Value);
                    if (value == null) return null;
                    o.Value.Value = value;
                }
                patterns.Add(pattern);
            }

            if (!indexChildList.Values.Any()) return null;
            if (indexChildList.Values.Any(o => !o.Equals(indexChildList.Values.First()))) return null;

            var commonPattern = BuildPattern(patterns);
            if (commonPattern.Tree.Value.Kind == SyntaxKind.EmptyStatement && !commonPattern.Tree.Children.Any()) return null;

            foreach (State input in spec.ProvidedInputs)
            {
                var patterncopy = ConverterHelper.MakeACopy(commonPattern.Tree);
                var patternP = new PatternP(patterncopy, $"/[{indexChildList[input] + 1}]");
                if (indexChildList[input] >= patterncopy.Children.Count) return MatchPatternBasic(rule, parameter, spec);

                eExamples[input] = new List<Pattern> { patternP, new Pattern(patterncopy.Children.ElementAt(indexChildList[input])) };
            }
            return DisjunctiveExamplesSpec.From(eExamples);
        }

        public static DisjunctiveExamplesSpec MatchPatternBasic(GrammarRule rule, int parameter, DisjunctiveExamplesSpec spec)
        {
            var eExamples = new Dictionary<State, IEnumerable<object>>();
            var patterns = new List<TreeNode<Token>>();
            foreach (State input in spec.ProvidedInputs)
            {
                var target = (TreeNode<SyntaxNodeOrToken>)input[rule.Body[0]];
                var currentTree = WitnessFunctions.GetCurrentTree(target);
                var examples = spec.DisjunctiveExamples[input].Select(o => (TreeNode<SyntaxNodeOrToken>)o).ToList();

                var matchInInputTree = TreeUpdate.FindNode(currentTree, examples.First());
                if (matchInInputTree == null)
                {
                    currentTree = ConverterHelper.ConvertCSharpToTreeNode(target.Value.Parent.Parent);
                    matchInInputTree = TreeUpdate.FindNode(currentTree, examples.First());
                }

                if (matchInInputTree == null) return null;
                var pattern = ConverterHelper.ConvertITreeNodeToToken(matchInInputTree);
                foreach (var o in pattern.DescendantNodesAndSelf())
                {
                    var value = TreeUpdate.FindNode(currentTree, o.Value.Value.Value);
                    if (value == null) return null;
                    o.Value.Value = value;
                }
                patterns.Add(pattern);
            }

            var commonPattern = BuildPattern(patterns);
            foreach (State input in spec.ProvidedInputs)
            {
                eExamples[input] = new List<Pattern> { new Pattern(ConverterHelper.MakeACopy(commonPattern.Tree)) };
            }
            return DisjunctiveExamplesSpec.From(eExamples);
        }*/

/*public static DisjunctiveExamplesSpec MatchK(GrammarRule rule, int parameter, DisjunctiveExamplesSpec spec, ExampleSpec kind)
   {
       var kExamples = new Dictionary<State, IEnumerable<object>>();
       var isFullTree = IsFullTree(spec, kind);

       //var intersectMatches = new List<int>();
       foreach (State input in spec.ProvidedInputs)
       {
           var mats = new List<object>();
           var patternExample = (Pattern)kind.Examples[input];
           var pattern = patternExample.Tree;
           var target = (TreeNode<SyntaxNodeOrToken>)input[rule.Body[0]];
           var inputTree = (Node)input[rule.Grammar.InputSymbol];

           foreach (TreeNode<SyntaxNodeOrToken> node in spec.DisjunctiveExamples[input])
           {
               var currentTree = WitnessFunctions.GetCurrentTree(node.SyntaxTree);
               var matches = MatchManager.Matches(currentTree, pattern);

               if (isFullTree)
               {
                   currentTree = ConverterHelper.ConvertCSharpToTreeNode(target.Value.Parent.Parent);
                   matches = MatchManager.Matches(currentTree, pattern);
                   //todo refactor this
                   for (int i = 0; i < matches.Count; i++)
                   {
                       var item = matches[i];
                       if (patternExample is PatternP)
                       {
                           if (item.Children.Any(o => MatchManager.IsValueEachChild(node, o)))
                           {
                               var beforeafter = SegmentElementsBeforeAfter(target, matches);
                               var bIndex = beforeafter.Item1.FindIndex(o => o.Equals(item));
                               var aIndex = beforeafter.Item2.FindIndex(o => o.Equals(item));

                               if (bIndex != -1)
                               {
                                   mats.Add(-(bIndex + 1));
                               }
                               else
                               {
                                   mats.Add(aIndex + 1);
                               }
                           }
                       }
                       else
                       {
                           if (MatchManager.IsValueEachChild(node, item))
                           {
                               var beforeafter = SegmentElementsBeforeAfter(target, matches);
                               var bIndex = beforeafter.Item1.FindIndex(o => o.Equals(item));
                               var aIndex = beforeafter.Item2.FindIndex(o => o.Equals(item));

                               if (bIndex != -1)
                               {
                                   mats.Add(-(bIndex + 1));
                               }
                               else
                               {
                                   mats.Add(aIndex + 1);
                               }
                           }
                       }
                   }
               }
               else
               {
                   for (int i = 0; i < matches.Count; i++)
                   {
                       var item = matches[i];
                       if (patternExample is PatternP)
                       {
                           if (item.Children.Any(o => MatchManager.IsValueEachChild(node, o)))
                           {
                               mats.Add(i + 1);
                           }
                       }
                       else {

                           if (MatchManager.IsValueEachChild(node, item))
                           {
                               mats.Add(i + 1);
                           }
                       }
                   }
               }
           }
           if (!mats.Any()) return null;
           kExamples[input] = mats;
       }
       return DisjunctiveExamplesSpec.From(kExamples);
   }

   private static bool IsFullTree(DisjunctiveExamplesSpec spec, ExampleSpec kind)
   {
       foreach (State input in spec.ProvidedInputs)
       {
           var patternExample = (Pattern)kind.Examples[input];
           var pattern = patternExample.Tree;
           foreach (TreeNode<SyntaxNodeOrToken> node in spec.DisjunctiveExamples[input])
           {
               var currentTree = WitnessFunctions.GetCurrentTree(node.SyntaxTree);
               var matches = MatchManager.Matches(currentTree, pattern);

               if (!matches.Any())
               {
                   return true;
               }
           }
       }
       return false;
   }

   private static Tuple<List<TreeNode<SyntaxNodeOrToken>>, List<TreeNode<SyntaxNodeOrToken>>> SegmentElementsBeforeAfter(TreeNode<SyntaxNodeOrToken> target, List<TreeNode<SyntaxNodeOrToken>> matches)
   {
       TreeNode<SyntaxNodeOrToken> node = null;
       if (target.Children.Any())
       {
           node = target.Children.First();
       }
       else
       {
           node = target;
       }
       var listBefore = new List<TreeNode<SyntaxNodeOrToken>>();
       var listAfter = new List<TreeNode<SyntaxNodeOrToken>>();
       foreach (var match in matches)
       {
           if (match.Value.SpanStart < node.Value.SpanStart)
           {
               listBefore.Add(match);
           }
           else
           {
               listAfter.Add(match);
           }
       }

       var tuple = Tuple.Create(listBefore, listAfter);
       return tuple;
   }*/

/*public static Pattern BuildPattern(List<TreeNode<Token>> patterns, bool leafToken = true)
    {
        var pattern = patterns.First();
        for (int i = 1; i < patterns.Count; i++)
        {
            pattern = BuildPattern(pattern, patterns[i], leafToken).Tree;
        }
        return new Pattern(pattern);
    }

    public static Pattern BuildPattern(TreeNode<Token> t1, TreeNode<Token> t2, bool considerLeafToken)
    {
        var emptyKind = SyntaxKind.EmptyStatement;
        var token = t1.Value.Kind == emptyKind && t2.Value.Kind == emptyKind ? new EmptyToken() : new Token(t1.Value.Kind, t1.Value.Value);
        var itreeNode = new TreeNode<Token>(token, new TLabel(token.Kind));
        Pattern pattern = (t1.Value.Kind != t2.Value.Kind) ? new Pattern(new TreeNode<Token>(new EmptyToken(), new TLabel(emptyKind))) : new Pattern(itreeNode); //EmptyToken pattern.
        if (!t1.Children.Any() || !t2.Children.Any())
        {
            if (!t1.Children.Any() && !t2.Children.Any() && t1.Value is DynToken && t2.Value is DynToken)
            {
                var v1 = ConverterHelper.ConvertCSharpToTreeNode(t1.Value.Value.Value);
                var v2 = ConverterHelper.ConvertCSharpToTreeNode(t2.Value.Value.Value);
                if (IsomorphicManager<SyntaxNodeOrToken>.IsIsomorphic(v1, v2))
                {
                    var dtoken = new DynToken(t1.Value.Kind, t1.Value.Value);
                    var ditreeNode = new TreeNode<Token>(dtoken, new TLabel(dtoken.Kind));
                    return new Pattern(ditreeNode);
                }
            }

            //if (!t1.Value.Value.Children.Any() && !t2.Value.Value.Children.Any() && considerLeafToken)
            //{
            //    var ltoken = new LeafToken(t1.Value.Kind, t1.Value.Value);
            //    var litreenode = new TreeNode<Token>(ltoken, new TLabel(token.Kind));
            //    return new Pattern(litreenode);
            //}
            return pattern;
        }

        if (t1.Children.Count == t2.Children.Count)
        {
            for (int j = 0; j < t1.Children.Count; j++)
            {
                var t1Child = t1.Children.ElementAt(j);
                var t2Child = t2.Children.ElementAt(j);
                Pattern patternChild = BuildPattern(t1Child, t2Child, considerLeafToken);
                pattern.Tree.AddChild(patternChild.Tree, j);
            }
        }
        return pattern;
    }*/

/*
    /// <summary>
    /// Build an XPath expression for the target node. To build this XPath, 
    /// we keep get the parent while the parent is null and build an XPath 
    /// from the last parent until the target node.
    /// </summary>
    /// <param name="target"></param>
    /// <param name="parent"></param>
    /// <returns>XPath</returns>
    public static string GetPath(TreeNode<SyntaxNodeOrToken> target, TreeNode<SyntaxNodeOrToken> parent)
    {
        string path = "";
        for (TreeNode<SyntaxNodeOrToken> node = target; !node.Equals(parent); node = node.Parent)
        {
            string append = "/";

            if (node.Parent != null && node.Parent.Children.Count >= 1)
            {
                append += "[";

                int index = 1;
                var previousSibling = PreviousSibling(node);
                while (previousSibling != null)
                {
                    index++;
                    previousSibling = PreviousSibling(previousSibling);
                }

                append += $"{index}]";
                path = append + path;
            }
        }
        return path;
    }

    private static TreeNode<SyntaxNodeOrToken> PreviousSibling(TreeNode<SyntaxNodeOrToken> node)
    {
        var parent = node.Parent;
        var parentIndex = parent.Children.FindIndex(o => o.Equals(node));
        if (parentIndex == 0) return null;
        return parent.Children[parentIndex - 1];
    }


    public static List<string> GetXpaths(TreeNode<SyntaxNodeOrToken> doc, SyntaxNodeOrToken stop)
    {
        var xpathList = new List<string>();
        var xpath = "";
        foreach (var child in doc.Children)
        {
            if (child.Value.Equals(stop)) return xpathList;
            GetXPaths(child, ref xpathList, xpath, stop);
        }
        return xpathList;
    }

    public static void GetXPaths(TreeNode<SyntaxNodeOrToken> node, ref List<string> xpathList, string xpath, SyntaxNodeOrToken stop)
    {
        xpath += "/" + node.Label;
        if (!xpathList.Contains(xpath))
            xpathList.Add(xpath);

        foreach (TreeNode<SyntaxNodeOrToken> child in node.Children)
        {
            if (child.Value.Equals(stop)) return;
            GetXPaths(child, ref xpathList, xpath, stop);
        }
    }
    //EndXPath
    */

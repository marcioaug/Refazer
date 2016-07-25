﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Rules;
using Microsoft.ProgramSynthesis.Specifications;
using TreeEdit.Spg.Isomorphic;
using TreeEdit.Spg.Match;
using TreeEdit.Spg.TreeEdit.Update;
using TreeElement;
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
                foreach (ITreeNode<Token> node in spec.DisjunctiveExamples[input])
                {
                    var sot = node.Value;
                    syntaxKinds.Add(sot.Kind);
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
                var matches = new List<List<ITreeNode<Token>>>();
                foreach (ITreeNode<Token> node in spec.DisjunctiveExamples[input])
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

        public static DisjunctiveExamplesSpec MatchPattern(GrammarRule rule, int parameter, DisjunctiveExamplesSpec spec)
        {
            var eExamples = new Dictionary<State, IEnumerable<object>>();
            var patterns = new List<ITreeNode<Token>>();
            foreach (State input in spec.ProvidedInputs)
            {
                var target = (Node)input[rule.Body[0]];
                var currentTree = WitnessFunctions.GetCurrentTree(target.Value);
                var matches = (from Node node in spec.DisjunctiveExamples[input] select node.Value).ToList();

                var pattern = ConverterHelper.ConvertITreeNodeToToken(matches.First());
                pattern.DescendantNodesAndSelf().ForEach(o => o.Value.Value = TreeUpdate.FindNode(currentTree, o.Value.Value.Value));
                patterns.Add(pattern);
            }

            var commonPattern = BuildPattern(patterns);
            foreach (State input in spec.ProvidedInputs)
            {
                eExamples[input] = new List<ITreeNode<Token>> { ConverterHelper.MakeACopy(commonPattern.Tree) };
            }
            return DisjunctiveExamplesSpec.From(eExamples);
        }

        public static Pattern BuildPattern(List<ITreeNode<Token>> patterns, bool leafToken = true)
        {
            var pattern = patterns.First();
            for (int i = 1; i < patterns.Count; i++)
            {
                pattern = BuildPattern(pattern, patterns[i], leafToken).Tree;
            }
            return new Pattern(pattern);
        }
 
        public static Pattern BuildPattern(ITreeNode<Token> t1, ITreeNode<Token> t2, bool considerLeafToken)
        {
            var emptyKind = SyntaxKind.EmptyStatement;
            var token = t1.Value.Kind == emptyKind && t2.Value.Kind == emptyKind ? new EmptyToken() : new Token(t1.Value.Kind, t1.Value.Value);
            var itreeNode = new TreeNode<Token>(token, new TLabel(token.Kind));
            Pattern pattern = new Pattern(itreeNode);
            if (t1.Value.Kind != t2.Value.Kind) return new Pattern(new TreeNode<Token>(new EmptyToken(), new TLabel(emptyKind))); //EmptyToken pattern.
            if (!t1.Children.Any() || !t2.Children.Any())
            {
                if (!t1.Children.Any() && !t2.Children.Any() && t1.Value is DynToken && t2.Value is DynToken)
                {
                    var v1 =/* ConverterHelper.ConvertCSharpToTreeNode(*/t1.Value.Value/*.Value)*/;
                    var v2 =/* ConverterHelper.ConvertCSharpToTreeNode(*/t2.Value.Value/*.Value)*/;
                    if (IsomorphicManager<SyntaxNodeOrToken>.IsIsomorphic(v1, v2))
                    {
                        var dtoken = new DynToken(t1.Value.Kind, t1.Value.Value);
                        var ditreeNode = new TreeNode<Token>(dtoken, new TLabel(dtoken.Kind));
                        return new Pattern(ditreeNode);
                    }
                }

                if (!t1.Value.Value.Children.Any() && !t2.Value.Value.Children.Any() && considerLeafToken)
                {
                    var ltoken = new LeafToken(t1.Value.Kind, t1.Value.Value);
                    var litreenode = new TreeNode<Token>(ltoken, new TLabel(token.Kind));
                    return new Pattern(litreenode);
                }
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
        }

        public static ExampleSpec MatchK(GrammarRule rule, int parameter, DisjunctiveExamplesSpec spec, DisjunctiveExamplesSpec kind)
        {
            var kExamples = new Dictionary<State, object>();
            foreach (State input in spec.ProvidedInputs)
            {
                var pattern = (ITreeNode<Token>)kind.DisjunctiveExamples[input].First();
                var target = (Node)input[rule.Body[0]];
                var inputTree = (Node)input[rule.Grammar.InputSymbol];
                var mats = new List<object>();
                foreach (Node node in spec.DisjunctiveExamples[input])
                {
                    var currentTree = WitnessFunctions.GetCurrentTree(node.Value.SyntaxTree);
                    var matches = MatchManager.Matches(currentTree, pattern);
                    for (int i = 0; i < matches.Count; i++)
                    {
                        var item = matches[i];
                        if (node.Value.Equals(item))
                        {
                            mats.Add(i + 1);
                        }
                    }
                }
                if (!mats.Any()) return null;
                if (!mats.All(o => o.Equals(mats.First()))) return null;
                kExamples[input] = mats.First();
            }
            return new ExampleSpec(kExamples);
        }
    }
}

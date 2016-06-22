﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Rules;
using Microsoft.ProgramSynthesis.Specifications;
using TreeEdit.Spg.Print;
using TreeEdit.Spg.Script;
using TreeElement.Spg.Node;

namespace ProseSample.Substrings.Spg.Witness
{
    public class Map
    {
        //public static SubsequenceSpec NodesMap(GrammarRule rule, int parameter, SubsequenceSpec spec)
        //{
        //    var linesExamples = new Dictionary<State, IEnumerable<object>>();
        //    foreach (State input in spec.ProvidedInputs)
        //    {
        //        var nodePrefix = spec.Examples[input].Cast<SyntaxNodeOrToken>();
        //        var tuple = (SyntaxNodeOrToken)input.Bindings.First().Value;

        //        var inpMapping = GetPair(tuple, nodePrefix);

        //        var linesContainingSelection = inpMapping;

        //        linesExamples[input] = linesContainingSelection;
        //    }
        //    return new SubsequenceSpec(linesExamples);
        //}


        public static SubsequenceSpec EditMap(GrammarRule rule, int parameter, SubsequenceSpec spec)
        {
            var editExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (State input in spec.ProvidedInputs)
            {
                var edits = (List<Edit<SyntaxNodeOrToken>>) spec.Examples[input];
                editExamples[input] = edits.Select(e => e.EditOperation.Parent).ToList();

                foreach (var edit in edits)
                {
                    //var key = input[rule.Body[0]];
                    var treeUp = WitnessFunctions.TreeUpdateDictionary[edit.EditOperation.Parent];
                    var previousTree = ConverterHelper.MakeACopy(treeUp.CurrentTree);
                    treeUp.ProcessEditOperation(edit.EditOperation);
                    

                    var modifieds = WitnessFunctions.TreeUpdateDictionary.Where(o => o.Value == treeUp);
                    foreach (var v in modifieds)
                    {
                        WitnessFunctions.CurrentTrees[v.Key] = previousTree;
                    }

                    Console.WriteLine("PREVIOUS TREE\n\n");
                    PrintUtil<SyntaxNodeOrToken>.PrintPretty(previousTree, "", true);
                    Console.WriteLine("UPDATED TREE\n\n");
                    PrintUtil<SyntaxNodeOrToken>.PrintPretty(treeUp.CurrentTree, "", true);
                }
            }
            return new SubsequenceSpec(editExamples);
        }

        /// <summary>
        /// Get the previous version of the transformed node on the input
        /// </summary>
        /// <param name="input">The source code before the transformation</param>
        /// <param name="nodePrefix">Transformed code fragments</param>
        /// <returns>Return the previous version of the transformed node on the input</returns>
        private static List<object> GetPair(SyntaxNodeOrToken input, IEnumerable<SyntaxNodeOrToken> nodePrefix)
        {
            return nodePrefix.Select(item => GetPair(input, item)).Cast<object>().ToList();
        }

        /// <summary>
        /// Get the corresponding pair of outTree in input tree
        /// </summary>
        /// <param name="inputTree">Input tree</param>
        /// <param name="outTree">output subTree</param>
        /// <returns>Corresponding pair</returns>
        private static SyntaxNodeOrToken GetPair(SyntaxNodeOrToken inputTree, SyntaxNodeOrToken outTree)
        {
            SyntaxNode node = inputTree.AsNode();

            var l = from nm in node.DescendantNodes()
                    where nm.IsKind(outTree.Kind())
                    select nm;

            MethodDeclarationSyntax m = (MethodDeclarationSyntax)outTree;
            return l.Cast<MethodDeclarationSyntax>().FirstOrDefault(mItem => m.Identifier.ToString().Equals(mItem.Identifier.ToString()));
        }
    }
}

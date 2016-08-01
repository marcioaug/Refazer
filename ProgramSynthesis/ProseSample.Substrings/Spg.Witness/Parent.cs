﻿using Microsoft.CodeAnalysis;
using TreeEdit.Spg.TreeEdit.Update;
using TreeElement.Spg.Node;

namespace ProseSample.Substrings.Spg.Witness
{
    public class Parent: Context
    {
        public override ITreeNode<SyntaxNodeOrToken> Target(ITreeNode<SyntaxNodeOrToken> sot)
        {
            if (sot.Parent == null)
            {
                return null;
            }
            var currentTree = WitnessFunctions.GetCurrentTree(sot.SyntaxTree);
            var node = TreeUpdate.FindNode(currentTree, sot.Value);
            return node.Parent;
        }
    }
}

﻿using System;
using System.Linq;
using ExampleRefactoring.Spg.ExampleRefactoring.AST;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Spg.ExampleRefactoring.Comparator
{
    /// <summary>
    /// Compare substring of a node
    /// </summary>
    public class ArrayInitializerElementTokenComparer : ComparerBase
    {
        /// <summary>
        /// First and second syntax node or token nodes content are equal
        /// </summary>
        /// <param name="first">First syntax node or token</param>
        /// <param name="second">Second syntax node or token</param>
        /// <returns>True if first and second syntax node or token nodes content are equal</returns>
        public override bool Match(SyntaxNodeOrToken first, SyntaxNodeOrToken second)
        {
            if(first == null || second == null)
            {
                throw new Exception("Syntax nodes or token cannot be null");
            }
            //bool isEqual = ASTManager.Parent(second).IsKind(SyntaxKind.ArrayInitializerExpression) &&
            //       (second.IsKind(SyntaxKind.StringLiteralToken) || second.IsKind(SyntaxKind.NullKeyword) ||
            //        second.IsKind(SyntaxKind.IdentifierToken));
            bool isEqual = ASTManager.Parent(second).IsKind(SyntaxKind.ArrayInitializerExpression) &&
                  !(second.IsKind(SyntaxKind.CommaToken) || second.IsKind(SyntaxKind.OpenBraceToken) ||
                   second.IsKind(SyntaxKind.CloseBraceToken));
            return isEqual;
        }
    }
}
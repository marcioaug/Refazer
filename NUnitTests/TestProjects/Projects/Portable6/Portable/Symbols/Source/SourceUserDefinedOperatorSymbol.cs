﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SourceUserDefinedOperatorSymbol : SourceUserDefinedOperatorSymbolBase
    {
        public static SourceUserDefinedOperatorSymbol CreateUserDefinedOperatorSymbol(
            SourceMemberContainerTypeSymbol containingType,
            OperatorDeclarationSyntax syntax,
            DiagnosticBag diagnostics)
        {
            var location = syntax.OperatorToken.GetLocation();

            string name = OperatorFacts.OperatorNameFromDeclaration(syntax);

            return new SourceUserDefinedOperatorSymbol(
                containingType, name, location, syntax, diagnostics,
                syntax.Body == null && syntax.ExpressionBody != null);
        }

        // NOTE: no need to call WithUnsafeRegionIfNecessary, since the signature
        // is bound lazily using binders from a BinderFactory (which will already include an
        // UnsafeBinder, if necessary).
        private SourceUserDefinedOperatorSymbol(
            SourceMemberContainerTypeSymbol containingType,
            string name,
            Location location,
            OperatorDeclarationSyntax syntax,
            DiagnosticBag diagnostics,
            bool isExpressionBodied) :
            base(
                MethodKind.UserDefinedOperator,
                name,
                containingType,
                location,
                syntax.GetReference(),
                syntax.Body.GetReferenceOrNull()
                ?? syntax.ExpressionBody.GetReferenceOrNull(),
                syntax.Modifiers,
                diagnostics,
                isExpressionBodied)
        {
        }

        protected override ParameterListSyntax ParameterListSyntax
        {
            get
            {
                var syntax = (OperatorDeclarationSyntax)syntaxReference.GetSyntax();
                return syntax.ParameterList;
            }
        }

        protected override TypeSyntax ReturnTypeSyntax
        {
            get
            {
                var syntax = (OperatorDeclarationSyntax)syntaxReference.GetSyntax();
                return syntax.ReturnType;
            }
        }

        internal override bool GenerateDebugInfo
        {
            get { return true; }
        }
    }
}

// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal struct NamespaceOrTypeAndUsingDirective
    {
        public readonly NamespaceOrTypeSymbol NamespaceOrType;
        public readonly UsingDirectiveSyntax UsingDirective;

        public NamespaceOrTypeAndUsingDirective(NamespaceOrTypeSymbol namespaceOrType, UsingDirectiveSyntax usingDirective)
        {
            this.NamespaceOrType = namespaceOrType;
            this.UsingDirective = usingDirective;
        }
    }
}
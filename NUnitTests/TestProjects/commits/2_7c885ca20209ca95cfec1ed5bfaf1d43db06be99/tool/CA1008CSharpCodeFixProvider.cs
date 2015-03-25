// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Design;

namespace Microsoft.CodeAnalysis.CSharp.FxCopAnalyzers.Design
{
    /// <summary>
    /// CA1008: Enums should have zero value
    /// </summary>
        /// <summary>
    /// CA1001: Types that own disposable fields should be disposable
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = "CA1008"), Shared]

    public class CA1008CSharpCodeFixProvider : CA1008CodeFixProviderBase
    {
    }
}
﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static partial class TypeParameterSymbolExtensions
    {
        public static bool DependsOn(this TypeParameterSymbol typeParameter1, TypeParameterSymbol typeParameter2)
        {
            Debug.Assert((object)typeParameter1 != null);
            Debug.Assert((object)typeParameter2 != null);

            Func<TypeParameterSymbol, IEnumerable<TypeParameterSymbol>> dependencies = x => x.ConstraintTypesNoUseSiteDiagnostics.OfType<TypeParameterSymbol>();
            return dependencies.TransitiveClosure(typeParameter1).Contains(typeParameter2);
        }
    }
}
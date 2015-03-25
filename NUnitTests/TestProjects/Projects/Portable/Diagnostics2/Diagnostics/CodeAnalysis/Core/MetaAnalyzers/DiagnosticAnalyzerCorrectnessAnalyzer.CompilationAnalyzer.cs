﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    public abstract partial class DiagnosticAnalyzerCorrectnessAnalyzer : DiagnosticAnalyzer
    {
        protected abstract class CompilationAnalyzer
        {
            private readonly INamedTypeSymbol diagnosticAnalyzer;
            private readonly INamedTypeSymbol diagnosticAnalyzerAttribute;

            public CompilationAnalyzer(INamedTypeSymbol diagnosticAnalyzer, INamedTypeSymbol diagnosticAnalyzerAttribute)
            {
                this.diagnosticAnalyzer = diagnosticAnalyzer;
                this.diagnosticAnalyzerAttribute = diagnosticAnalyzerAttribute;
            }

            protected INamedTypeSymbol DiagnosticAnalyzer { get { return this.diagnosticAnalyzer; } }
            protected INamedTypeSymbol DiagnosticAnalyzerAttribute { get { return this.diagnosticAnalyzerAttribute; } }

            protected bool IsDiagnosticAnalyzer(INamedTypeSymbol type)
            {
                return type.Equals(this.diagnosticAnalyzer);
            }

            internal void AnalyzeSymbol(SymbolAnalysisContext symbolContext)
            {
                var namedType = (INamedTypeSymbol)symbolContext.Symbol;
                if (namedType.GetBaseTypes().Any(IsDiagnosticAnalyzer))
                {
                    AnalyzeDiagnosticAnalyzer(symbolContext);
                }
            }

            protected abstract void AnalyzeDiagnosticAnalyzer(SymbolAnalysisContext symbolContext);

            protected bool HasDiagnosticAnalyzerAttribute(INamedTypeSymbol namedType)
            {
                foreach (var attribute in AttributeHelpers.GetApplicableAttributes(namedType))
                {
                    if (AttributeHelpers.DerivesFrom(attribute.AttributeClass, DiagnosticAnalyzerAttribute))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}

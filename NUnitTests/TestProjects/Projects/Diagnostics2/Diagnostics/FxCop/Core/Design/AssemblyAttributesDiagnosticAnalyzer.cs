﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Design
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class AssemblyAttributesDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        internal const string CA1016RuleName = "CA1016";
        internal const string CA1014RuleName = "CA1014";

        private static LocalizableString localizableMessageCA1016 = new LocalizableResourceString(nameof(FxCopRulesResources.AssembliesShouldBeMarkedWithAssemblyVersionAttribute), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));
        internal static DiagnosticDescriptor CA1016Rule = new DiagnosticDescriptor(CA1016RuleName,
                                                                         localizableMessageCA1016,
                                                                         localizableMessageCA1016,
                                                                         FxCopDiagnosticCategory.Design,
                                                                         DiagnosticSeverity.Warning,
                                                                         isEnabledByDefault: true,
                                                                         helpLink: "http://msdn.microsoft.com/library/ms182155.aspx",
                                                                         customTags: DiagnosticCustomTags.Microsoft);

        private static LocalizableString localizableMessageCA1014 = new LocalizableResourceString(nameof(FxCopRulesResources.MarkAssembliesWithCLSCompliantAttribute), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));
        private static LocalizableString localizableDescriptionCA1014 = new LocalizableResourceString(nameof(FxCopRulesResources.MarkAssembliesWithCLSCompliantDescription), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));
        internal static DiagnosticDescriptor CA1014Rule = new DiagnosticDescriptor(CA1014RuleName,
                                                                         localizableMessageCA1014,
                                                                         localizableMessageCA1014,
                                                                         FxCopDiagnosticCategory.Design,
                                                                         DiagnosticSeverity.Warning,
                                                                         isEnabledByDefault: false,
                                                                         description: localizableDescriptionCA1014,
                                                                         helpLink: "http://msdn.microsoft.com/library/ms182156.aspx",
                                                                         customTags: DiagnosticCustomTags.Microsoft);

        private static readonly ImmutableArray<DiagnosticDescriptor> supportedDiagnostics = ImmutableArray.Create(CA1016Rule, CA1014Rule);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return supportedDiagnostics;
            }
        }

        public override void Initialize(AnalysisContext analysisContext)
        {
            analysisContext.RegisterCompilationEndAction(AnalyzeCompilation);
        }

        private void AnalyzeCompilation(CompilationEndAnalysisContext context)
        {
            var assemblyVersionAttributeSymbol = WellKnownTypes.AssemblyVersionAttribute(context.Compilation);
            var assemblyComplianceAttributeSymbol = WellKnownTypes.CLSCompliantAttribute(context.Compilation);

            if (assemblyVersionAttributeSymbol == null && assemblyComplianceAttributeSymbol == null)
            {
                return;
            }

            bool assemblyVersionAttributeFound = false;
            bool assemblyComplianceAttributeFound = false;

            // Check all assembly level attributes for the target attribute
            foreach (var attribute in context.Compilation.Assembly.GetAttributes())
            {
                if (attribute.AttributeClass.Equals(assemblyVersionAttributeSymbol))
                {
                    // Mark the version attribute as found
                    assemblyVersionAttributeFound = true;
                }
                else if (attribute.AttributeClass.Equals(assemblyComplianceAttributeSymbol))
                {
                    // Mark the compliance attribute as found
                    assemblyComplianceAttributeFound = true;
                }
            }

            // Check for the case where we do not have the target attribute defined at all in our metadata references. If so, how can they reference it
            if (assemblyVersionAttributeSymbol == null)
            {
                assemblyVersionAttributeFound = false;
            }

            if (assemblyComplianceAttributeSymbol == null)
            {
                assemblyComplianceAttributeFound = false;
            }

            // If there's at least one diagnostic to report, let's report them
            if (!assemblyComplianceAttributeFound || !assemblyVersionAttributeFound)
            {
                if (!assemblyVersionAttributeFound)
                {
                    context.ReportDiagnostic(Diagnostic.Create(CA1016Rule, Location.None));
                }

                if (!assemblyComplianceAttributeFound)
                {
                    context.ReportDiagnostic(Diagnostic.Create(CA1014Rule, Location.None));
                }
            }
        }
    }
}

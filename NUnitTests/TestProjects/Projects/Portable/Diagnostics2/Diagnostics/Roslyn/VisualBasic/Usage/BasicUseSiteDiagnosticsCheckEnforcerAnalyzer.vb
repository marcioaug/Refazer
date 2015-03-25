﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Roslyn.Diagnostics.Analyzers.VisualBasic
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicUseSiteDiagnosticsCheckEnforcerAnalyzer
        Inherits AbstractSyntaxNodeAnalyzer(Of SyntaxKind)

        Private Shared localizableTitle As LocalizableString = New LocalizableResourceString(NameOf(RoslynDiagnosticsResources.UseSiteDiagnosticsCheckerDescription), RoslynDiagnosticsResources.ResourceManager, GetType(RoslynDiagnosticsResources))
        Private Shared localizableMessage As LocalizableString = New LocalizableResourceString(NameOf(RoslynDiagnosticsResources.UseSiteDiagnosticsCheckerMessage), RoslynDiagnosticsResources.ResourceManager, GetType(RoslynDiagnosticsResources))

        Private Shared _descriptor As DiagnosticDescriptor = New DiagnosticDescriptor(RoslynDiagnosticIds.UseSiteDiagnosticsCheckerRuleId,
                                                                             localizableTitle,
                                                                             localizableMessage,
                                                                             "Usage",
                                                                             DiagnosticSeverity.Error,
                                                                             False,
                                                                             WellKnownDiagnosticTags.Telemetry)

        Private Shared PropertiesToValidateMap As Dictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase) From
                {
                    {BaseTypeString, TypeSymbolFullyQualifiedName},
                    {InterfacesString, TypeSymbolFullyQualifiedName},
                    {AllInterfacesString, TypeSymbolFullyQualifiedName},
                    {TypeArgumentsString, NamedTypeSymbolFullyQualifiedName},
                    {ConstraintTypesString, TypeParameterSymbolFullyQualifiedName}
                }

        Private Const BaseTypeString = "BaseType"
        Private Const InterfacesString = "Interfaces"
        Private Const AllInterfacesString = "AllInterfaces"
        Private Const TypeArgumentsString = "TypeArguments"
        Private Const ConstraintTypesString = "ConstraintTypes"

        Private Const TypeSymbolFullyQualifiedName = "Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeSymbol"
        Private Const NamedTypeSymbolFullyQualifiedName = "Microsoft.CodeAnalysis.VisualBasic.Symbols.NamedTypeSymbol"
        Private Const TypeParameterSymbolFullyQualifiedName = "Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeParameterSymbol"

        Protected Overrides ReadOnly Property Descriptor As DiagnosticDescriptor
            Get
                Return _descriptor
            End Get
        End Property

        Protected Overrides ReadOnly Property SyntaxKindsOfInterest As SyntaxKind()
            Get
                Return {SyntaxKind.SimpleMemberAccessExpression}
            End Get
        End Property

        Protected Overrides Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)
            Dim name = DirectCast(context.Node, MemberAccessExpressionSyntax).Name
            If name.Kind = SyntaxKind.IdentifierName Then
                Dim identifier = DirectCast(name, IdentifierNameSyntax)
                Dim containingTypeName As String = Nothing
                If PropertiesToValidateMap.TryGetValue(identifier.ToString(), containingTypeName) Then
                    Dim sym As ISymbol = context.SemanticModel.GetSymbolInfo(identifier, context.CancellationToken).Symbol
                    If sym IsNot Nothing AndAlso sym.Kind = SymbolKind.Property Then
                        If containingTypeName = sym.ContainingType.ToDisplayString() Then
                            ReportDiagnostic(context, identifier, identifier.ToString())
                        End If
                    End If
                End If
            End If
        End Sub
    End Class
End Namespace

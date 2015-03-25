' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Usage
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers.Usage
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicCA2213DiagnosticAnalyzer
        Inherits CA2213DiagnosticAnalyzer

        Protected Overrides Function GetAnalyzer(context As CompilationStartAnalysisContext, disposableType As INamedTypeSymbol) As AbstractAnalyzer
            Dim analyzer As New Analyzer(disposableType)
            context.RegisterSyntaxNodeAction(AddressOf Analyzer.AnalyzeNode, SyntaxKind.SimpleMemberAccessExpression, SyntaxKind.UsingStatement)
            Return analyzer
        End Function

        Private NotInheritable Class Analyzer
            Inherits AbstractAnalyzer

            Public Sub New(disposableType As INamedTypeSymbol)
                MyBase.New(disposableType)
            End Sub

            Public Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)
                Select Case context.Node.Kind
                    Case SyntaxKind.SimpleMemberAccessExpression
                        ' NOTE: This cannot be optimized based on memberAccess.Name because a given method
                        ' may be an explicit interface implementation of IDisposable.Dispose.
                        Dim memberAccess = DirectCast(context.Node, MemberAccessExpressionSyntax)
                        Dim methodSymbol = TryCast(context.SemanticModel.GetSymbolInfo(memberAccess.Name).Symbol, IMethodSymbol)
                        If methodSymbol IsNot Nothing AndAlso
                            (methodSymbol.MetadataName = Dispose OrElse methodSymbol.ExplicitInterfaceImplementations.Any(Function(m) m.MetadataName = Dispose)) Then
                            Dim exp = RemoveParentheses(memberAccess.Expression)
                            Dim fieldSymbol = TryCast(context.SemanticModel.GetSymbolInfo(exp).Symbol, IFieldSymbol)
                            If fieldSymbol IsNot Nothing Then
                                NoteFieldDisposed(fieldSymbol)
                            End If
                        End If

                    Case SyntaxKind.UsingStatement
                        Dim usingStatementExpression = RemoveParentheses(DirectCast(context.Node, UsingStatementSyntax).Expression)
                        If usingStatementExpression IsNot Nothing Then
                            Dim fieldSymbol = TryCast(context.SemanticModel.GetSymbolInfo(usingStatementExpression).Symbol, IFieldSymbol)
                            If fieldSymbol IsNot Nothing Then
                                NoteFieldDisposed(fieldSymbol)
                            End If
                        End If
                End Select
            End Sub

            Private Function RemoveParentheses(exp As ExpressionSyntax) As ExpressionSyntax
                Dim syntax = TryCast(exp, ParenthesizedExpressionSyntax)
                If syntax IsNot Nothing Then
                    Return RemoveParentheses(syntax.Expression)
                End If

                Return exp
            End Function
        End Class
    End Class
End Namespace
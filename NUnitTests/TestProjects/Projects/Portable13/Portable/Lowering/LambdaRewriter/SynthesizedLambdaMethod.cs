﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A method that results from the translation of a single lambda expression.
    /// </summary>
    internal sealed class SynthesizedLambdaMethod : SynthesizedMethodBaseSymbol, ISynthesizedMethodBodyImplementationSymbol
    {
        private readonly MethodSymbol topLevelMethod;

        internal SynthesizedLambdaMethod(
            VariableSlotAllocator slotAllocatorOpt,
            TypeCompilationState compilationState,
            NamedTypeSymbol containingType,
            ClosureKind closureKind,
            MethodSymbol topLevelMethod,
            int topLevelMethodOrdinal,
            BoundLambda node,
            int lambdaOrdinal)
            : base(containingType,
                   node.Symbol,
                   null,
                   node.SyntaxTree.GetReference(node.Body.Syntax),
                   node.Syntax.GetLocation(),
                   MakeName(slotAllocatorOpt, compilationState, closureKind, topLevelMethod, topLevelMethodOrdinal, lambdaOrdinal),
                   (closureKind == ClosureKind.ThisOnly ? DeclarationModifiers.Private : DeclarationModifiers.Internal)
                       | (node.Symbol.IsAsync ? DeclarationModifiers.Async : 0))
        {
            this.topLevelMethod = topLevelMethod;

            TypeMap typeMap;
            ImmutableArray<TypeParameterSymbol> typeParameters;
            LambdaFrame lambdaFrame;

            if (!topLevelMethod.IsGenericMethod)
            {
                typeMap = TypeMap.Empty;
                typeParameters = ImmutableArray<TypeParameterSymbol>.Empty;
            }
            else if ((object)(lambdaFrame = this.ContainingType as LambdaFrame) != null)
            {
                typeMap = lambdaFrame.TypeMap;
                typeParameters = ImmutableArray<TypeParameterSymbol>.Empty;
            }
            else
            {
                typeMap = TypeMap.Empty.WithAlphaRename(topLevelMethod, this, out typeParameters);
            }

            AssignTypeMapAndTypeParameters(typeMap, typeParameters);
        }

        private static string MakeName(VariableSlotAllocator slotAllocatorOpt, TypeCompilationState compilationState, ClosureKind closureKind, MethodSymbol topLevelMethod, int topLevelMethodOrdinal, int lambdaOrdinal)
        {
            // TODO: slotAllocatorOpt?.GetPrevious()

            // Lambda method name must contain the declaring method ordinal to be unique unless the method is emitted into a closure class exclusive to the declaring method.
            // Lambdas that only close over "this" are emitted directly into the top-level method containing type.
            // Lambdas that don't close over anything (static) are emitted into a shared closure singleton.
            return GeneratedNames.MakeLambdaMethodName(
                topLevelMethod.Name,
                (closureKind == ClosureKind.General) ? -1 : topLevelMethodOrdinal,
                compilationState.ModuleBuilderOpt.CurrentGenerationOrdinal,
                lambdaOrdinal);
        }

        internal override int ParameterCount => this.BaseMethod.ParameterCount;

        // The lambda symbol might have declared no parameters in the case
        //
        // D d = delegate {};
        //
        // but there still might be parameters that need to be generated for the
        // synthetic method. If there are no lambda parameters, try the delegate 
        // parameters instead. 
        // 
        // UNDONE: In the native compiler in this scenario we make up new names for
        // UNDONE: synthetic parameters; in this implementation we use the parameter
        // UNDONE: names from the delegate. Does it really matter?
        protected override ImmutableArray<ParameterSymbol> BaseMethodParameters => this.BaseMethod.Parameters;

        internal override bool GenerateDebugInfo => !this.IsAsync;
        internal override bool IsExpressionBodied => false;

        IMethodSymbol ISynthesizedMethodBodyImplementationSymbol.Method => topLevelMethod;

        // The lambda method body needs to be updated when the containing top-level method body is updated.
        bool ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency => true;
    }
}

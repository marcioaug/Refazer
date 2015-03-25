﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal class SourceFixedFieldSymbol : SourceMemberFieldSymbol
    {
        private const int FixedSizeNotInitialized = -1;

        // In a fixed-size field declaration, stores the fixed size of the buffer
        private int fixedSize = FixedSizeNotInitialized;

        internal SourceFixedFieldSymbol(
            SourceMemberContainerTypeSymbol containingType,
            VariableDeclaratorSyntax declarator,
            DeclarationModifiers modifiers,
            bool modifierErrors,
            DiagnosticBag diagnostics)
            : base(containingType, declarator, modifiers, modifierErrors, diagnostics)
        {
            // Checked in parser: a fixed field declaration requires a length in square brackets

            Debug.Assert(this.IsFixed);
        }

        internal override void AddSynthesizedAttributes(ModuleCompilationState compilationState, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(compilationState, ref attributes);

            var compilation = this.DeclaringCompilation;
            var systemType = compilation.GetWellKnownType(WellKnownType.System_Type);
            var intType = compilation.GetSpecialType(SpecialType.System_Int32);
            var item1 = new TypedConstant(systemType, TypedConstantKind.Type, ((PointerTypeSymbol)this.Type).PointedAtType);
            var item2 = new TypedConstant(intType, TypedConstantKind.Primitive, this.FixedSize);
            AddSynthesizedAttribute(ref attributes, compilation.SynthesizeAttribute(
                WellKnownMember.System_Runtime_CompilerServices_FixedBufferAttribute__ctor,
                ImmutableArray.Create<TypedConstant>(item1, item2)));
        }

        public sealed override int FixedSize
        {
            get
            {
                if (this.fixedSize == FixedSizeNotInitialized)
                {
                    DiagnosticBag diagnostics = DiagnosticBag.GetInstance();
                    int size = 0;

                    VariableDeclaratorSyntax declarator = this.VariableDeclaratorNode;

                    if (declarator.ArgumentList == null)
                    {
                        // Diagnostic reported by parser.
                    }
                    else
                    {
                        SeparatedSyntaxList<ArgumentSyntax> arguments = declarator.ArgumentList.Arguments;

                        if (arguments.Count == 0 || arguments[0].Expression.Kind == SyntaxKind.OmittedArraySizeExpression)
                        {
                            Debug.Assert(declarator.ArgumentList.ContainsDiagnostics, "The parser should have caught this.");
                        }
                        else
                        {
                            if (arguments.Count > 1)
                            {
                                diagnostics.Add(ErrorCode.ERR_FixedBufferTooManyDimensions, declarator.ArgumentList.Location);
                            }

                            ExpressionSyntax sizeExpression = arguments[0].Expression;

                            BinderFactory binderFactory = this.DeclaringCompilation.GetBinderFactory(SyntaxTree);
                            Binder binder = binderFactory.GetBinder(sizeExpression);

                            TypeSymbol intType = binder.GetSpecialType(SpecialType.System_Int32, diagnostics, sizeExpression);
                            BoundExpression boundSizeExpression = binder.GenerateConversionForAssignment(
                                intType,
                                binder.BindValue(sizeExpression, diagnostics, Binder.BindValueKind.RValue),
                                diagnostics);

                            // GetAndValidateConstantValue doesn't generate a very intuitive-reading diagnostic
                            // for this situation, but this is what the Dev10 compiler produces.
                            ConstantValue sizeConstant = ConstantValueUtils.GetAndValidateConstantValue(boundSizeExpression, this, intType, sizeExpression.Location, diagnostics);

                            Debug.Assert(sizeConstant != null);
                            Debug.Assert(sizeConstant.IsIntegral || diagnostics.HasAnyErrors() || sizeExpression.HasErrors);

                            if (sizeConstant.IsIntegral)
                            {
                                int int32Value = sizeConstant.Int32Value;
                                if (int32Value > 0)
                                {
                                    size = int32Value;

                                    TypeSymbol elementType = ((PointerTypeSymbol)this.Type).PointedAtType;
                                    int elementSize = elementType.FixedBufferElementSizeInBytes();
                                    long totalSize = elementSize * 1L * int32Value;
                                    if (totalSize > int.MaxValue)
                                    {
                                        // Fixed size buffer of length '{0}' and type '{1}' is too big
                                        diagnostics.Add(ErrorCode.ERR_FixedOverflow, sizeExpression.Location, int32Value, elementType);
                                    }
                                }
                                else
                                {
                                    diagnostics.Add(ErrorCode.ERR_InvalidFixedArraySize, sizeExpression.Location);
                                }
                            }
                        }
                    }

                    // Winner writes diagnostics.
                    if (Interlocked.CompareExchange(ref this.fixedSize, size, FixedSizeNotInitialized) == FixedSizeNotInitialized)
                    {
                        this.AddSemanticDiagnostics(diagnostics);
                        if (state.NotePartComplete(CompletionPart.FixedSize))
                        {
                            // FixedSize is the last completion part for fields.
                            DeclaringCompilation.SymbolDeclaredEvent(this);
                        }
                    }

                    diagnostics.Free();
                }

                Debug.Assert(this.fixedSize != FixedSizeNotInitialized);
                return this.fixedSize;
            }
        }

        internal override NamedTypeSymbol FixedImplementationType(PEModuleBuilder emitModule)
        {
            return emitModule.SetFixedImplementationType(this);
        }
    }

    internal sealed class FixedFieldImplementationType : SynthesizedContainer
    {
        internal const string FixedElementFieldName = "FixedElementField";

        private readonly SourceMemberFieldSymbol field;
        private readonly MethodSymbol constructor;
        private readonly FieldSymbol internalField;

        public FixedFieldImplementationType(SourceMemberFieldSymbol field)
            : base(GeneratedNames.MakeFixedFieldImplementationName(field.Name), typeParameters: ImmutableArray<TypeParameterSymbol>.Empty, typeMap: TypeMap.Empty)
        {
            this.field = field;
            this.constructor = new SynthesizedInstanceConstructor(this);
            this.internalField = new SynthesizedFieldSymbol(this, ((PointerTypeSymbol)field.Type).PointedAtType, FixedElementFieldName, isPublic: true);
        }

        public override Symbol ContainingSymbol
        {
            get { return field.ContainingType; }
        }

        public override TypeKind TypeKind
        {
            get { return TypeKind.Struct; }
        }
                
        internal override MethodSymbol Constructor
        {
            get { return constructor; }
        }

        internal override TypeLayout Layout
        {
            get
            {
                int nElements = field.FixedSize;
                var elementType = ((PointerTypeSymbol)field.Type).PointedAtType;
                int elementSize = elementType.FixedBufferElementSizeInBytes();
                const int alignment = 0;
                int totalSize = nElements * elementSize;
                const LayoutKind layoutKind = LayoutKind.Sequential;
                return new TypeLayout(layoutKind, totalSize, alignment);
            }
        }

        internal override FieldSymbol FixedElementField
        {
            get { return internalField; }
        }

        internal override void AddSynthesizedAttributes(ModuleCompilationState compilationState, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(compilationState, ref attributes);
            var compilation = ContainingSymbol.DeclaringCompilation;
            AddSynthesizedAttribute(ref attributes, compilation.SynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_UnsafeValueTypeAttribute__ctor));
        }

        public override IEnumerable<string> MemberNames
        {
            get { return SpecializedCollections.SingletonEnumerable(FixedElementFieldName); }
        }

        public override ImmutableArray<Symbol> GetMembers()
        {
            return ImmutableArray.Create<Symbol>(constructor, internalField);
        }

        public override ImmutableArray<Symbol> GetMembers(string name)
        {
            return
                (name == constructor.Name) ? ImmutableArray.Create<Symbol>(constructor) :
                (name == FixedElementFieldName) ? ImmutableArray.Create<Symbol>(internalField) :
                ImmutableArray<Symbol>.Empty;
        }

        public override Accessibility DeclaredAccessibility
        {
            get { return Accessibility.Public; }
        }

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics
        {
            get { return ContainingAssembly.GetSpecialType(SpecialType.System_ValueType); }
        }
    }
}
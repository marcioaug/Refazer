﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.CodeGen
{
    /// <summary>
    /// We need a CCI representation for local constants because they are emitted as locals in
    /// PDB scopes to improve the debugging experience (see LocalScopeProvider.GetConstantsInScope).
    /// </summary>
    internal sealed class LocalConstantDefinition : Cci.ILocalDefinition
    {
        private readonly string name;
        private readonly Location location;
        private readonly Cci.IMetadataConstant compileTimeValue;
        private readonly bool isDynamic;

        //Gives the synthesized dynamic attributes of the local definition
        private readonly ImmutableArray<TypedConstant> dynamicTransformFlags;

        public LocalConstantDefinition(string name, Location location, Cci.IMetadataConstant compileTimeValue, bool isDynamic = false,
            ImmutableArray<TypedConstant> dynamicTransformFlags = default(ImmutableArray<TypedConstant>))
        {
            Debug.Assert(!string.IsNullOrEmpty(name));
            Debug.Assert(compileTimeValue != null);

            this.name = name;
            this.location = location;
            this.compileTimeValue = compileTimeValue;
            this.isDynamic = isDynamic;
            this.dynamicTransformFlags = dynamicTransformFlags;
        }

        public string Name
        {
            get { return name; }
        }

        public Location Location
        {
            get { return location; }
        }

        public Cci.IMetadataConstant CompileTimeValue
        {
            get { return compileTimeValue; }
        }

        public Cci.ITypeReference Type
        {
            get { return this.compileTimeValue.Type; }
        }

        public bool IsConstant
        {
            get { return true; }
        }

        public ImmutableArray<Cci.ICustomModifier> CustomModifiers
        {
            get { return ImmutableArray<Cci.ICustomModifier>.Empty; }
        }

        public bool IsModified
        {
            get { return false; }
        }

        public bool IsPinned
        {
            get { return false; }
        }

        public bool IsReference
        {
            get { return false; }
        }

        public LocalSlotConstraints Constraints
        {
            get { return LocalSlotConstraints.None; }
        }

        public bool IsDynamic
        {
            get { return this.isDynamic; }
        }

        public uint PdbAttributes
        {
            get { return Cci.PdbWriter.DefaultLocalAttributesValue; }
        }

        public ImmutableArray<TypedConstant> DynamicTransformFlags
        {
            get { return this.dynamicTransformFlags; }
        }

        public int SlotIndex
        {
            get { return -1; }
        }

        public byte[] Signature
        {
            get { return null; }
        }

        public LocalSlotDebugInfo SlotInfo
        {
            get { return new LocalSlotDebugInfo(SynthesizedLocalKind.UserDefined, LocalDebugId.None); }
        }
    }
}

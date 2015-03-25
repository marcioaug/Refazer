﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    partial class CommonReferenceManager<TCompilation, TAssemblySymbol>
    {
        /// <summary>
        /// Private helper class to capture information about AssemblySymbol instance we 
        /// should check for suitability. Used by the Bind method.
        /// </summary>
        private struct AssemblyReferenceCandidate
        {
            /// <summary>
            /// An index of the AssemblyData object in the input array. AssemblySymbol instance should 
            /// be checked for suitability against assembly described by that object, taking into account 
            /// assemblies described by other AssemblyData objects in the input array.
            /// </summary>
            public readonly int DefinitionIndex;

            /// <summary>
            /// AssemblySymbol instance to check for suitability.
            /// </summary>
            public readonly TAssemblySymbol AssemblySymbol;

            /// <summary>
            /// Convenience constructor to initialize fields of this structure.
            /// </summary>
            public AssemblyReferenceCandidate(int definitionIndex, TAssemblySymbol symbol)
            {
                DefinitionIndex = definitionIndex;
                AssemblySymbol = symbol;
            }
        }
    }
}

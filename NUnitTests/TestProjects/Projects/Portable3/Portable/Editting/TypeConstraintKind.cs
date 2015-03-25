﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Editting
{
    [Flags]
    public enum SpecialTypeConstraintKind
    {
        None          = 0x0000,

        /// <summary>
        /// Has the reference type constraint (i.e. 'class' constraint in C#)
        /// </summary>
        ReferenceType = 0x0001,

        /// <summary>
        /// Has the value type constraint (i.e. 'struct' constraint in C#)
        /// </summary>
        ValueType = 0x0002,

        /// <summary>
        /// Has the constructor constraint (i.e. 'new' constraint in C#)
        /// </summary>
        Constructor = 0x0004
    }
}

﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class StateMachineStates
    {
        internal readonly static int FinishedStateMachine = -2;
        internal readonly static int NotStartedStateMachine = -1;
        internal readonly static int FirstUnusedState = 0;
    }
}
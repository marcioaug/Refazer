﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Rename
{
    public static class RenameOptions
    {
        internal const string RenameFeatureName = "Rename";

        public static readonly Option<bool> RenameOverloads = new Option<bool>(RenameFeatureName, "RenameOverloads", defaultValue: false);
        public static readonly Option<bool> RenameInStrings = new Option<bool>(RenameFeatureName, "RenameInStrings", defaultValue: false);
        public static readonly Option<bool> RenameInComments = new Option<bool>(RenameFeatureName, "RenameInComments", defaultValue: false);
        public static readonly Option<bool> PreviewChanges = new Option<bool>(RenameFeatureName, "PreviewChanges", defaultValue: false);
    }
}

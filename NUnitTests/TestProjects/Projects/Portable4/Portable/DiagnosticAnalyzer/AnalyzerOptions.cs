﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Options passed to <see cref="DiagnosticAnalyzer"/>.
    /// </summary>
    public class AnalyzerOptions
    {
        internal static readonly AnalyzerOptions Empty = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty);

        /// <summary>
        /// A set of additional non-code text files that can be used by analyzers.
        /// </summary>
        public ImmutableArray<AdditionalText> AdditionalFiles { get; internal set; }

        /// <summary>
        /// Creates analyzer options to be passed to <see cref="DiagnosticAnalyzer"/>.
        /// </summary>
        /// <param name="additionalFiles">A set of additional non-code text files that can be used by analyzers.</param>
        public AnalyzerOptions(ImmutableArray<AdditionalText> additionalFiles)
        {
            this.AdditionalFiles = additionalFiles.IsDefault ? ImmutableArray<AdditionalText>.Empty : additionalFiles;
        }

        /// <summary>
        /// Returns analyzer options with the given <paramref name="additionalFiles"/>.
        /// </summary>
        public AnalyzerOptions WithAdditionalFiles(ImmutableArray<AdditionalText> additionalFiles)
        {
            if (this.AdditionalFiles == additionalFiles)
            {
                return this;
            }

            return new AnalyzerOptions(additionalFiles);
        }
    }
}

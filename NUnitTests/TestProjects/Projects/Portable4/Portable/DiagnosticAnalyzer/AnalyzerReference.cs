﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Represents an analyzer assembly reference that contains diagnostic analyzers.
    /// </summary>
    /// <remarks>
    /// Represents a logical location of the analyzer reference, not the content of the reference. 
    /// The content might change in time. A snapshot is taken when the compiler queries the reference for its analyzers.
    /// </remarks>
    public abstract class AnalyzerReference
    {
        protected AnalyzerReference()
        {
        }

        /// <summary>
        /// Full path describing the location of the analyzer reference, or null if the reference has no location.
        /// </summary>
        public abstract string FullPath { get; }

        /// <summary>
        /// Path or name used in error messages to identity the reference.
        /// </summary>
        public virtual string Display
        {
            get { return null; }
        }

        /// <summary>
        /// Returns true if this reference is an unresolved reference.
        /// </summary>
        public virtual bool IsUnresolved
        {
            get { return false; }
        }

        /// <summary>
        /// Gets all the diagnostic analyzers defined in this assembly reference, irrespective of the language supported by the analyzer.
        /// Use this method only if you need all the analyzers defined in the assembly, without a language context.
        /// In most instances, either the analyzer reference is associated with a project or is being queried for analyzers in a particular language context.
        /// If so, use <see cref="GetAnalyzers(string)"/> method.
        /// </summary>
        public abstract ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages();

        /// <summary>
        /// Gets all the diagnostic analyzers defined in this assembly reference for the given <paramref name="language"/>.
        /// </summary>
        /// <param name="language">Language name.</param>
        public abstract ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language);
    }
}

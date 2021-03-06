﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Formats <see cref="Diagnostic"/> messages.
    /// </summary>
    public class DiagnosticFormatter
    {

        internal string GetMessagePrefix(Diagnostic diagnostic, CultureInfo culture)
        {
            string prefix;
            switch (diagnostic.Severity)
            {
                case DiagnosticSeverity.Hidden:
                    prefix = CodeAnalysisResources.ResourceManager.GetString(nameof(CodeAnalysisResources.SeverityHidden), culture);
                break;
                case DiagnosticSeverity.Warning:
                    prefix = CodeAnalysisResources.SeverityWarning;
                    break;
                case DiagnosticSeverity.Error:
                    prefix = CodeAnalysisResources.SeverityError;
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(diagnostic.Severity);
            }

            return string.Format(culture, "{0} {1}",
                prefix,
                diagnostic.Id);
        }
        
        /// <summary>
        /// Formats the <see cref="Diagnostic"/> message using the optional <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="diagnostic">The diagnostic.</param>
        /// <param name="formatter">The formatter; or null to use the default formatter.</param>
        /// <returns>The formatted message.</returns>
        public virtual string Format(Diagnostic diagnostic, IFormatProvider formatter = null)
        {
            string prefix;
            switch (diagnostic.Severity)
            {
                case DiagnosticSeverity.Info:
                    prefix = CodeAnalysisResources.ResourceManager.GetString(nameof(CodeAnalysisResources.SeverityInfo), culture);
                break;
                case DiagnosticSeverity.Warning:
                    prefix = CodeAnalysisResources.SeverityWarning;
                    break;
                case DiagnosticSeverity.Error:
                    prefix = CodeAnalysisResources.SeverityError;
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(diagnostic.Severity);
            }

            if (diagnostic == null)
            {
                throw new ArgumentNullException("diagnostic");
            }

            var culture = formatter as CultureInfo;

            switch (diagnostic.Location.Kind)
            {
                case LocationKind.SourceFile:
                case LocationKind.XmlFile:
                case LocationKind.ExternalFile: 
                    var span = diagnostic.Location.GetLineSpan();
                    var mappedSpan = diagnostic.Location.GetMappedLineSpan();
                    if (!span.IsValid || !mappedSpan.IsValid)
                    {
                        goto default;
                    }

                    string path, basePath;
                    if (mappedSpan.HasMappedPath)
                    {
                        path = mappedSpan.Path;
                        basePath = span.Path;
                    }
                    else
                    {
                        path = span.Path;
                        basePath = null;
                    }

                    return string.Format(formatter, "{0}{1}: {2}: {3}",
                                         FormatSourcePath(path, basePath, formatter),
                                         FormatSourceSpan(mappedSpan.Span, formatter),
                                         GetMessagePrefix(diagnostic, culture),
                                         diagnostic.GetMessage(culture));

                default:
                    return string.Format(formatter, "{0}: {1}",
                                         GetMessagePrefix(diagnostic, culture),
                                         diagnostic.GetMessage(culture));
            }
        }

        internal virtual string FormatSourcePath(string path, string basePath, IFormatProvider formatter)
        {
            // ignore base path
            return path;
        }

        internal virtual string FormatSourceSpan(LinePositionSpan span, IFormatProvider formatter)
        {
            return string.Format("({0},{1})", span.Start.Line + 1, span.Start.Character + 1);
        }

        internal static readonly DiagnosticFormatter Instance = new DiagnosticFormatter();
    }
}
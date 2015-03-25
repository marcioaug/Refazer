﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.CSharp
{
    [Serializable]
    public sealed class CSharpSerializableCompilationOptions : SerializableCompilationOptions
    {
        private const string AllowUnsafeString = "AllowUnsafe";
        private const string UsingsString = "Usings";

        private readonly CSharpCompilationOptions options; 

        public CSharpSerializableCompilationOptions(CSharpCompilationOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            this.options = options;
        }

        private CSharpSerializableCompilationOptions(SerializationInfo info, StreamingContext context)
        {
            this.options = new CSharpCompilationOptions(
                outputKind: (OutputKind)info.GetInt32(OutputKindString),
                moduleName: info.GetString(ModuleNameString),
                mainTypeName: info.GetString(MainTypeNameString),
                scriptClassName: info.GetString(ScriptClassNameString),
                usings: (string[])info.GetValue(UsingsString, typeof(string[])),
                cryptoKeyContainer: info.GetString(CryptoKeyContainerString),
                cryptoKeyFile: info.GetString(CryptoKeyFileString),
                delaySign: (bool?)info.GetValue(DelaySignString, typeof(bool?)),
                optimizationLevel: (OptimizationLevel)info.GetInt32(OptimizeString),
                checkOverflow: info.GetBoolean(CheckOverflowString),
                allowUnsafe: info.GetBoolean(AllowUnsafeString),
                platform: (Platform)info.GetInt32(PlatformString),
                generalDiagnosticOption: (ReportDiagnostic)info.GetInt32(GeneralDiagnosticOptionString),
                warningLevel: info.GetInt32(WarningLevelString),
                specificDiagnosticOptions: ((Dictionary<string, ReportDiagnostic>)info.GetValue(SpecificDiagnosticOptionsString, typeof(Dictionary<string, ReportDiagnostic>))).ToImmutableDictionary(),
                concurrentBuild: info.GetBoolean(ConcurrentBuildString),
                extendedCustomDebugInformation: info.GetBoolean(ExtendedCustomDebugInformationString),
                xmlReferenceResolver: XmlFileResolver.Default,
                sourceReferenceResolver: SourceFileResolver.Default,
                metadataReferenceResolver: new AssemblyReferenceResolver(MetadataFileReferenceResolver.Default, MetadataFileReferenceProvider.Default),
                assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default,
                strongNameProvider: new DesktopStrongNameProvider(),
                metadataImportOptions: (MetadataImportOptions)info.GetByte(MetadataImportOptionsString),
                features: ((string[])info.GetValue(FeaturesString, typeof(string[]))).AsImmutable());
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            CommonGetObjectData(options, info, context);

            info.AddValue(UsingsString, options.Usings.ToArray());
            info.AddValue(AllowUnsafeString, options.AllowUnsafe);
        }

        public new CSharpCompilationOptions Options
        {
            get { return options; }
        }

        protected override CompilationOptions CommonOptions
        {
            get { return options; }
        }
    }
}

﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.GeneratedCodeRecognition;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Helper class for "Fix all occurrences" code fix providers.
    /// </summary>
    internal partial class BatchFixAllProvider : FixAllProvider
    {
        public static readonly FixAllProvider Instance = new BatchFixAllProvider();

        protected BatchFixAllProvider() { }

        #region "AbstractFixAllProvider methods"

        public override async Task<CodeAction> GetFixAsync(FixAllContext fixAllContext)
        {
            if (fixAllContext.Document != null)
            {
                var documentsAndDiagnosticsToFixMap = await GetDocumentDiagnosticsToFixAsync(fixAllContext).ConfigureAwait(false);
                return await GetFixAsync(documentsAndDiagnosticsToFixMap, fixAllContext).ConfigureAwait(false);
            }
            else
            {
                var projectsAndDiagnosticsToFixMap = await GetProjectDiagnosticsToFixAsync(fixAllContext).ConfigureAwait(false);
                return await GetFixAsync(projectsAndDiagnosticsToFixMap, fixAllContext).ConfigureAwait(false);
            }
        }

        #endregion

        public virtual async Task<CodeAction> GetFixAsync(
            ImmutableDictionary<Document, ImmutableArray<Diagnostic>> documentsAndDiagnosticsToFixMap,
            FixAllContext fixAllContext)
        {
            if (documentsAndDiagnosticsToFixMap != null && documentsAndDiagnosticsToFixMap.Any())
            {
                FixAllLogger.LogDiagnosticsStats(documentsAndDiagnosticsToFixMap);

                var fixesBag = new ConcurrentBag<CodeAction>();

                using (Logger.LogBlock(FunctionId.CodeFixes_FixAllOccurrencesComputation_Fixes, fixAllContext.CancellationToken))
                {
                    fixAllContext.CancellationToken.ThrowIfCancellationRequested();

                    var documents = documentsAndDiagnosticsToFixMap.Keys.ToImmutableArray();
                    var options = new ParallelOptions() { CancellationToken = fixAllContext.CancellationToken };
                    Parallel.ForEach(documents, options, document =>
                    {
                        fixAllContext.CancellationToken.ThrowIfCancellationRequested();
                        AddDocumentFixesAsync(document, documentsAndDiagnosticsToFixMap[document], fixesBag.Add, fixAllContext).Wait(fixAllContext.CancellationToken);
                    });
                }

                if (fixesBag.Any())
                {
                    using (Logger.LogBlock(FunctionId.CodeFixes_FixAllOccurrencesComputation_Merge, fixAllContext.CancellationToken))
                    {
                        FixAllLogger.LogFixesToMergeStats(fixesBag);
                        return await TryGetMergedFixAsync(fixesBag, fixAllContext).ConfigureAwait(false);
                    }
                }
            }

            return null;
        }

        public async virtual Task AddDocumentFixesAsync(Document document, ImmutableArray<Diagnostic> diagnostics, Action<CodeAction> addFix, FixAllContext fixAllContext)
        {
            Debug.Assert(!diagnostics.IsDefault);
            var cancellationToken = fixAllContext.CancellationToken;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var fixerTasks = new Task[diagnostics.Length];

            for (var i = 0; i < diagnostics.Length; i++)
            {
                var diagnostic = diagnostics[i];
                fixerTasks[i] = Task.Run(async () =>
                {
                    var fixes = new List<CodeAction>();
                    var context = new CodeFixContext(document, diagnostic,

                        // TODO: Can we share code between similar lambdas that we pass to this API in BatchFixAllProvider.cs, CodeFixService.cs and CodeRefactoringService.cs?
                        (a, d) =>
                        {
                            // Serialize access for thread safety - we don't know what thread the fix provider will call this delegate from.
                            lock (fixes)
                            {
                                fixes.Add(a);
                            }
                        },
                        cancellationToken);

                    // TODO: Wrap call to ComputeFixesAsync() below in IExtensionManager.PerformFunctionAsync() so that
                    // a buggy extension that throws can't bring down the host?
                    var task = fixAllContext.CodeFixProvider.ComputeFixesAsync(context) ?? SpecializedTasks.EmptyTask;
                    await task.ConfigureAwait(false);

                    foreach (var fix in fixes)
                    {
                        if (fix != null && fix.Id == fixAllContext.CodeActionId)
                        {
                            addFix(fix);
                        }
                    }
                });
            }

            Task.WaitAll(fixerTasks, cancellationToken);
        }

        public virtual async Task<CodeAction> GetFixAsync(
            ImmutableDictionary<Project, ImmutableArray<Diagnostic>> projectsAndDiagnosticsToFixMap,
            FixAllContext fixAllContext)
        {
            if (projectsAndDiagnosticsToFixMap != null && projectsAndDiagnosticsToFixMap.Any())
            {
                FixAllLogger.LogDiagnosticsStats(projectsAndDiagnosticsToFixMap);

                var fixesBag = new ConcurrentBag<CodeAction>();

                using (Logger.LogBlock(FunctionId.CodeFixes_FixAllOccurrencesComputation_Fixes, fixAllContext.CancellationToken))
                {
                    var options = new ParallelOptions() { CancellationToken = fixAllContext.CancellationToken };
                    Parallel.ForEach(projectsAndDiagnosticsToFixMap.Keys, options, project =>
                    {
                        fixAllContext.CancellationToken.ThrowIfCancellationRequested();
                        var diagnostics = projectsAndDiagnosticsToFixMap[project];
                        AddProjectFixesAsync(project, diagnostics, fixesBag.Add, fixAllContext).Wait(fixAllContext.CancellationToken);
                    });
                }

                if (fixesBag.Any())
                {
                    using (Logger.LogBlock(FunctionId.CodeFixes_FixAllOccurrencesComputation_Merge, fixAllContext.CancellationToken))
                    {
                        FixAllLogger.LogFixesToMergeStats(fixesBag);
                        return await TryGetMergedFixAsync(fixesBag, fixAllContext).ConfigureAwait(false);
                    }
                }
            }

            return null;
        }

        public virtual Task AddProjectFixesAsync(Project project, IEnumerable<Diagnostic> diagnostics, Action<CodeAction> addFix, FixAllContext fixAllContext)
        {
            throw new NotImplementedException();
        }

        public virtual async Task<CodeAction> TryGetMergedFixAsync(IEnumerable<CodeAction> batchOfFixes, FixAllContext fixAllContext)
        {
            Contract.ThrowIfNull(batchOfFixes);
            Contract.ThrowIfFalse(batchOfFixes.Any());

            var solution = fixAllContext.Solution;
            var cancellationToken = fixAllContext.CancellationToken;
            var newSolution = await TryMergeFixesAsync(solution, batchOfFixes, cancellationToken).ConfigureAwait(false);
            if (newSolution != null && newSolution != solution)
            {
                var title = GetFixAllTitle(fixAllContext);
                return new CodeAction.SolutionChangeAction(title, _ => Task.FromResult(newSolution));
            }

            return null;
        }

        public virtual string GetFixAllTitle(FixAllContext fixAllContext)
        {
            var diagnosticIds = fixAllContext.DiagnosticIds;
            string diagnosticId;
            if (diagnosticIds.Count() == 1)
            {
                diagnosticId = diagnosticIds.Single();
            }
            else
            {
                diagnosticId = string.Join(",", diagnosticIds.ToArray());
            }

            switch (fixAllContext.Scope)
            {
                case FixAllScope.Custom:
                    return string.Format(WorkspacesResources.FixAllOccurrencesOfDiagnostic, diagnosticId);

                case FixAllScope.Document:
                    var document = fixAllContext.Document;
                    return string.Format(WorkspacesResources.FixAllOccurrencesOfDiagnosticInScope, diagnosticId, document.Name);

                case FixAllScope.Project:
                    var project = fixAllContext.Project;
                    return string.Format(WorkspacesResources.FixAllOccurrencesOfDiagnosticInScope, diagnosticId, project.Name);

                case FixAllScope.Solution:
                    return string.Format(WorkspacesResources.FixAllOccurrencesOfDiagnosticInSolution, diagnosticId);

                default:
                    throw ExceptionUtilities.Unreachable;
            }
        }

        public virtual async Task<ImmutableDictionary<Document, ImmutableArray<Diagnostic>>> GetDocumentDiagnosticsToFixAsync(FixAllContext fixAllContext)
        {
            using (Logger.LogBlock(FunctionId.CodeFixes_FixAllOccurrencesComputation_Diagnostics, fixAllContext.CancellationToken))
            {
                IEnumerable<Document> documentsToFix = null;
                var document = fixAllContext.Document;
                var project = fixAllContext.Project;

                var generatedCodeServices = project.Solution.Workspace.Services.GetService<IGeneratedCodeRecognitionService>();

                switch (fixAllContext.Scope)
                {
                    case FixAllScope.Document:
                        if (document != null && !generatedCodeServices.IsGeneratedCode(document))
                        {
                            var diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false);
                            var kvp = SpecializedCollections.SingletonEnumerable(KeyValuePair.Create(document, diagnostics));
                            return ImmutableDictionary.CreateRange(kvp);
                        }

                        break;

                    case FixAllScope.Project:
                        documentsToFix = project.Documents;
                        break;

                    case FixAllScope.Solution:
                        documentsToFix = project.Solution.Projects
                            .Where(p => p.Language == project.Language)
                            .SelectMany(p => p.Documents);
                        break;
                }

                if (documentsToFix != null && documentsToFix.Any())
                {
                    var documentAndDiagnostics = new ConcurrentDictionary<Document, ImmutableArray<Diagnostic>>();
                    var options = new ParallelOptions() { CancellationToken = fixAllContext.CancellationToken };
                    Parallel.ForEach(documentsToFix, options, doc =>
                    {
                        fixAllContext.CancellationToken.ThrowIfCancellationRequested();

                        if (!generatedCodeServices.IsGeneratedCode(doc))
                        {
                            var documentDiagnostics = fixAllContext.GetDocumentDiagnosticsAsync(doc).WaitAndGetResult(fixAllContext.CancellationToken);
                            if (documentDiagnostics.Any())
                            {
                                documentAndDiagnostics.TryAdd(doc, documentDiagnostics);
                            }
                        }
                    });

                    return documentAndDiagnostics.ToImmutableDictionary();
                }

                return ImmutableDictionary<Document, ImmutableArray<Diagnostic>>.Empty;
            }
        }

        public virtual async Task<ImmutableDictionary<Project, ImmutableArray<Diagnostic>>> GetProjectDiagnosticsToFixAsync(FixAllContext fixAllContext)
        {
            using (Logger.LogBlock(FunctionId.CodeFixes_FixAllOccurrencesComputation_Diagnostics, fixAllContext.CancellationToken))
            {
                var project = fixAllContext.Project;
                if (project != null)
                {
                    switch (fixAllContext.Scope)
                    {
                        case FixAllScope.Project:
                            var diagnostics = await fixAllContext.GetProjectDiagnosticsAsync(project).ConfigureAwait(false);
                            var kvp = SpecializedCollections.SingletonEnumerable(KeyValuePair.Create(project, diagnostics));
                            return ImmutableDictionary.CreateRange(kvp);

                        case FixAllScope.Solution:
                            var projectsAndDiagnostics = new ConcurrentDictionary<Project, ImmutableArray<Diagnostic>>();
                            var options = new ParallelOptions() { CancellationToken = fixAllContext.CancellationToken };
                            Parallel.ForEach(project.Solution.Projects, options, proj =>
                            {
                                fixAllContext.CancellationToken.ThrowIfCancellationRequested();
                                var projectDiagnostics = fixAllContext.GetProjectDiagnosticsAsync(proj).WaitAndGetResult(fixAllContext.CancellationToken);
                                if (projectDiagnostics.Any())
                                {
                                    projectsAndDiagnostics.TryAdd(proj, projectDiagnostics);
                                }
                            });

                            return projectsAndDiagnostics.ToImmutableDictionary();
                    }
                }

                return ImmutableDictionary<Project, ImmutableArray<Diagnostic>>.Empty;
            }
        }

        public virtual async Task<Solution> TryMergeFixesAsync(Solution oldSolution, IEnumerable<CodeAction> codeActions, CancellationToken cancellationToken)
        {
            var changedDocumentsMap = new Dictionary<DocumentId, Document>();
            Dictionary<DocumentId, List<Document>> documentsToMergeMap = null;

            foreach (var codeAction in codeActions)
            {
                // TODO: Parallelize GetChangedSolutionInternalAsync for codeActions
                var changedSolution = await codeAction.GetChangedSolutionInternalAsync(cancellationToken).ConfigureAwait(false);

                var solutionChanges = new SolutionChanges(changedSolution, oldSolution);

                // TODO: Handle added/removed documents
                // TODO: Handle changed/added/removed additional documents

                var documentIdsWithChanges = solutionChanges
                    .GetProjectChanges()
                    .SelectMany(p => p.GetChangedDocuments());

                foreach (var documentId in documentIdsWithChanges)
                {
                    var document = changedSolution.GetDocument(documentId);

                    Document existingDocument;
                    if (changedDocumentsMap.TryGetValue(documentId, out existingDocument))
                    {
                        if (existingDocument != null)
                        {
                            changedDocumentsMap[documentId] = null;
                            var documentsToMerge = new List<Document>();
                            documentsToMerge.Add(existingDocument);
                            documentsToMerge.Add(document);
                            documentsToMergeMap = documentsToMergeMap ?? new Dictionary<DocumentId, List<Document>>();
                            documentsToMergeMap[documentId] = documentsToMerge;
                        }
                        else
                        {
                            documentsToMergeMap[documentId].Add(document);
                        }
                    }
                    else
                    {
                        changedDocumentsMap[documentId] = document;
                    }
                }
            }

            var currentSolution = oldSolution;
            foreach (var kvp in changedDocumentsMap)
            {
                var document = kvp.Value;
                if (document != null)
                {
                    var documentText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                    currentSolution = currentSolution.WithDocumentText(kvp.Key, documentText);
                }
            }

            if (documentsToMergeMap != null)
            {
                var mergedDocuments = new ConcurrentDictionary<DocumentId, SourceText>();

                var documentsToMergeArray = documentsToMergeMap.ToImmutableArray();
                bool mergeFailed = false;

                var mergeTasks = new Task[documentsToMergeArray.Length];
                for (int i = 0; i < documentsToMergeArray.Length; i++)
                {
                    var kvp = documentsToMergeArray[i];
                    var documentId = kvp.Key;
                    var documentsToMerge = kvp.Value;
                    var oldDocument = oldSolution.GetDocument(documentId);

                    mergeTasks[i] = Task.Run(async () =>
                    {
                        var appliedChanges = (await documentsToMerge[0].GetTextChangesAsync(oldDocument).ConfigureAwait(false)).ToList();

                        foreach (var document in documentsToMerge.Skip(1))
                        {
                            appliedChanges = await TryAddDocumentMergeChangesAsync(
                                oldDocument,
                                document,
                                appliedChanges,
                                cancellationToken).ConfigureAwait(false);

                            if (appliedChanges == null)
                            {
                                mergeFailed = true;
                                break;
                            }
                        }

                        if (!mergeFailed)
                        {
                            var oldText = await oldDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                            var newText = oldText.WithChanges(appliedChanges);
                            mergedDocuments.TryAdd(documentId, newText);
                        }
                    });
                }

                Task.WaitAll(mergeTasks, cancellationToken);

                if (mergeFailed)
                {
                    return null;
                }

                foreach (var kvp in mergedDocuments)
                {
                    currentSolution = currentSolution.WithDocumentText(kvp.Key, kvp.Value);
                }
            }

            return currentSolution;
        }

        private static async Task<List<TextChange>> TryAddDocumentMergeChangesAsync(
            Document oldDocument,
            Document newDocument,
            List<TextChange> cumulativeChanges,
            CancellationToken cancellationToken)
        {
            var successfullyMergedChanges = new List<TextChange>();

            int cumulativeChangeIndex = 0;
            foreach (var change in await newDocument.GetTextChangesAsync(oldDocument).ConfigureAwait(false))
            {
                while (cumulativeChangeIndex < cumulativeChanges.Count && cumulativeChanges[cumulativeChangeIndex].Span.End < change.Span.Start)
                {
                    // Existing change that does not overlap with the current change in consideration
                    successfullyMergedChanges.Add(cumulativeChanges[cumulativeChangeIndex]);
                    cumulativeChangeIndex++;
                }

                if (cumulativeChangeIndex < cumulativeChanges.Count)
                {
                    var cumulativeChange = cumulativeChanges[cumulativeChangeIndex];
                    if (!cumulativeChange.Span.IntersectsWith(change.Span))
                    {
                        // The current change in consideration does not intersect with any existing change
                        successfullyMergedChanges.Add(change);
                    }
                    else
                    {
                        if (change.Span != cumulativeChange.Span || change.NewText != cumulativeChange.NewText)
                        {
                            // The current change in consideration overlaps an existing change but
                            // the changes are not identical. 
                            // Bail out merge efforts.
                            continue;
                        }
                        else
                        {
                            // The current change in consideration is identical to an existing change
                            successfullyMergedChanges.Add(change);
                            cumulativeChangeIndex++;
                        }
                    }
                }
                else
                {
                    // The current change in consideration does not intersect with any existing change
                    successfullyMergedChanges.Add(change);
                }
            }

            while (cumulativeChangeIndex < cumulativeChanges.Count)
            {
                // Existing change that does not overlap with the current change in consideration
                successfullyMergedChanges.Add(cumulativeChanges[cumulativeChangeIndex]);
                cumulativeChangeIndex++;
            }

            return successfullyMergedChanges;
        }
    }
}

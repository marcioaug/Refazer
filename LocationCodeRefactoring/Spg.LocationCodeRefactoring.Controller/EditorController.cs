﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using ExampleRefactoring.Spg.ExampleRefactoring.Bean;
using ExampleRefactoring.Spg.ExampleRefactoring.LCS;
using ExampleRefactoring.Spg.ExampleRefactoring.Projects;
using ExampleRefactoring.Spg.ExampleRefactoring.Util;
using ExampleRefactoring.Spg.ExampleRefactoring.Workspace;
using LocationCodeRefactoring.Spg.LocationRefactor.Location;
using LocationCodeRefactoring.Spg.LocationRefactor.Operator.Map;
using LocationCodeRefactoring.Spg.LocationRefactor.Program;
using LocationCodeRefactoring.Spg.LocationRefactor.Transformation;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text.Projection;
using Spg.ExampleRefactoring.Synthesis;
using Spg.ExampleRefactoring.Util;
using Spg.LocationCodeRefactoring.Observer;
using Spg.LocationRefactor.Location;
using Spg.LocationRefactor.Operator.Filter;
using Spg.LocationRefactor.TextRegion;

namespace LocationCodeRefactoring.Spg.LocationCodeRefactoring.Controller
{
    /// <summary>
    /// Controller for editor graphical interface
    /// </summary>
    public class EditorController
    {
        private List<IHilightObserver> _hilightObservers;
        private List<IProgramsGeneratedObserver> _programGeneratedObservers;
        private List<IProgramRefactoredObserver> _programsRefactoredObserver;
        private List<ILocationsTransformedObserver> _locationsTransformedObserver;
        private List<ILocationsObserver> _locationsObversers;

        /// <summary>
        /// Locations
        /// </summary>
        /// <returns>Locations</returns>
        public List<CodeLocation> Locations { get; set; }

        /// <summary>
        /// Transformation
        /// </summary>
        /// <returns>transformation</returns>
        public List<CodeTransformation> CodeTransformations { get; set; }

        /// <summary>
        /// Tuple of source code before and after for every class edited.
        /// </summary>
        /// <returns>Transformation</returns>
        public List<Transformation> SourceTransformations { get; set; }

        /// <summary>
        /// Regions before the transformation
        /// </summary>
        /// <returns></returns>
        public List<TRegion> SelectedLocations { get; set; }

        /// <summary>
        /// Learned programs
        /// </summary>
        //public Dictionary<String, Prog> Progs { get; set; }
        public List<Prog> Progs { get; set; }

        ///// <summary>
        ///// Solution path
        ///// </summary>
        //
        //public string SolutionPath { get; set; }

        public ProjectInformation ProjectInformation { get; set; }

        /// <summary>
        /// Code before transformation
        /// </summary>
        /// <returns></returns>
        public string CurrentViewCodeBefore { get; set; }

        /// <summary>
        /// Code before transformation
        /// </summary>
        /// <returns>Code before transformation</returns>
        public string CurrentViewCodeAfter { get; set; }

        /// <summary>
        /// Current view source code path
        /// </summary>
        /// <returns></returns>
        public string CurrentViewCodePath { get; set; }

        public List<Tuple<string, string>> DocumentsBeforeAndAfter { get; set; }
        //public string CurrentProject { get; set; }
        public IProjectionBuffer ProjectionBuffer { get; set; }

        public Dictionary<string, IProjectionBuffer> ProjectionBuffers { get; set; }

        public Dictionary<string, List<Selection>> EditedLocations { get; set; }

        public Dictionary<string, bool> FilesOpened { get; set; }

        public Tuple<List<CodeLocation>, List<TRegion>> localionsComputerSoFar;

        /// <summary>
        /// Program computed for negative filtering
        /// </summary>
        /// <returns>Program learned for negative filtering</returns>
        public List<Prog> ProgramsWithNegatives { get; set; }

        /// <summary>
        /// Least Common ancestor of selected nodes
        /// </summary>
        public List<SyntaxNode> Lcas { get; set; }

        /// <summary>
        /// Singleton instance
        /// </summary>
        private static EditorController _instance;



        /// <summary>
        /// Constructor
        /// </summary>
        private EditorController()
        {
            Init();
        }

        public void SetSolution(string solution)
        {
            this.ProjectInformation.SolutionPath = solution;
        }

        public void SetProject(string project)
        {
            this.ProjectInformation.ProjectPath = project;
        }

        //Start controller
        public void Init()
        {
            this.SelectedLocations = new List<TRegion>();
            this._hilightObservers = new List<IHilightObserver>();
            this._programGeneratedObservers = new List<IProgramsGeneratedObserver>();
            this._programsRefactoredObserver = new List<IProgramRefactoredObserver>();
            this._locationsTransformedObserver = new List<ILocationsTransformedObserver>();
            this._locationsObversers = new List<ILocationsObserver>();
            this.FilesOpened = new Dictionary<string, bool>();
            //this.CurrentProject = null;
            //this.Progs = null;
            //this.SolutionPath = null;
            this.ProjectInformation = ProjectInformation.GetInstance();
            LCAManager.Init();
            RegionManager.Init();
            BoundaryManager.Init();
        }

        public static void ReInit()
        {
            _instance = null;
        }

        /// <summary>
        /// Get singleton EditorController instance
        /// </summary>
        /// <returns>Editor controller instance</returns>
        public static EditorController GetInstance()
        {
            if (_instance == null)
            {
                _instance = new EditorController();
            }
            return _instance;
        }

        /// <summary>
        /// Decompose locations
        /// </summary>
        public void Extract()
        {
            LocationExtractor extractor = new LocationExtractor(ProjectInformation.SolutionPath);
            //remove
            JsonUtil<List<TRegion>>.Write(SelectedLocations, "input_selection.json");
            //remove
            Progs = extractor.Extract(SelectedLocations);

            Progs = RecomputeWithNegativeLocations();

            NotifyLocationProgramGeneratedObservers(Progs);
        }

        public void Extract(List<TRegion> positives, List<TRegion> negatives)
        {
            LocationExtractor extractor = new LocationExtractor(ProjectInformation.SolutionPath);
            ProgramsWithNegatives = extractor.Extract(positives, negatives);

            //Progs = RecomputeWithNegativeLocations();

            NotifyLocationProgramGeneratedObservers(ProgramsWithNegatives);
        }

        /// <summary>
        /// Analise locations
        /// </summary>
        /// <returns>Location programs</returns>
        private List<Prog> RecomputeWithNegativeLocations()
        {
            if (SelectedLocations == null || !SelectedLocations.Any()) { throw new Exception("Selected regions cannot be null or empty."); }

            List<TRegion> regions = RetrieveLocations(CurrentViewCodeBefore, Progs.First());
            regions = NonDuplicateLocations(regions);

            TRegion deepestRegion = SelectedLocations.First();
            for (int i = 1; i < SelectedLocations.Count; i++)
            {
                if (SelectedLocations[i].Start > deepestRegion.Start)
                {
                    deepestRegion = SelectedLocations[i];
                }
            }

            List<TRegion> negativesExamples = new List<TRegion>();
            foreach (TRegion region in regions)
            {
                if (region.Start > deepestRegion.Start) { break; } //after deepest region does not work as negative example.
                bool negative = true;
                foreach (TRegion selectedRegion in SelectedLocations)
                {
                    if (selectedRegion.Start <= region.Start && region.Start <= selectedRegion.Start + selectedRegion.Length)
                    {
                        negative = false; break;
                    }

                    if (region.Start <= selectedRegion.Start && selectedRegion.Start <= region.Start + region.Length)
                    {
                        negative = false; break;
                    }
                }

                if (negative)
                {
                    region.Parent = deepestRegion.Parent;
                    negativesExamples.Add(region);
                }
            }

            if (!negativesExamples.Any())
            {
                return Progs;
            }
            LocationExtractor extrator = new LocationExtractor(this.ProjectInformation.SolutionPath);

            var withNegatives = extrator.Extract(SelectedLocations, negativesExamples);

            if (withNegatives.Any())
            {
                Progs = withNegatives;
            }

            return Progs;
        }

        /// <summary>
        /// Undo systematic editing
        /// </summary>
        public void Undo()
        {
            foreach (Transformation transformation in SourceTransformations)
            {
                string path = transformation.SourcePath;
                string sourceCode = transformation.transformation.Item1;
                FileUtil.WriteToFile(path, sourceCode);
            }
            this.CodeTransformations = null;
        }

        /// <summary>
        /// Edit locations
        /// </summary>
        /// <param name="start">Start position</param>
        /// <param name="length">Region length</param>
        /// <param name="sourceCode">Source code</param>
        public void Edit(int start, int length, String span, String sourceCode)
        {
            TRegion tregion = new TRegion();
            Color color = Color.LightGreen;
            tregion.Start = start;
            tregion.Length = length;
            tregion.Text = span;
            tregion.Color = color;
            tregion.Path = CurrentViewCodePath;

            SelectedLocations.Add(tregion);

            if (tregion.Parent == null)
            {
                TRegion parent = new TRegion
                {
                    Start = 0,
                    Length = sourceCode.Length,
                    Text = sourceCode,
                    Path = CurrentViewCodePath,
                    Color = Color.White
                };
                tregion.Parent = parent;
            }
        }

        /// <summary>
        /// Retrieve locations
        /// </summary>
        /// <param name="program">Selected program</param>
        public void RetrieveLocations(string program)
        {
            if (ProgramsWithNegatives != null)
            {
                RetrieveLocationsPosNegatives(program);
                return;
            }

            Prog prog = Progs.First();
            List<Tuple<string, string>> sourceFiles = SourceFiles();

            Tuple<List<CodeLocation>, List<TRegion>> tuple;
            if (sourceFiles.Count() > 1)
            {
                tuple = RetrieveLocationsMultiplesSourceClasses(prog, sourceFiles, program); 
            }
            else
            {
                tuple = RetrieveLocationsSingleSourceClass(prog);
            }
            var sourceLocations = tuple.Item1;

            this.Locations = NonDuplicateLocations(sourceLocations);

            //remove
            List<Selection> selections = new List<Selection>();
            foreach (CodeLocation location in Locations)
            {
                Selection selection = new Selection(location.Region.Start, location.Region.Length, location.SourceClass, location.SourceCode);
                selections.Add(selection);
            }
            JsonUtil<List<Selection>>.Write(selections, "found_locations.json");
            //remove

            localionsComputerSoFar = tuple;
            NotifyLocationsObservers(Locations);
        }

        public void RetrieveLocationsPosNegatives(string program)
        {
            Prog prog = ProgramsWithNegatives.First();
            List<Tuple<string, string>> sourceFiles = SourceFiles();

            Tuple<List<CodeLocation>, List<TRegion>> tuple;
            if (sourceFiles.Count() > 1)
            {
                tuple = RetrieveLocationsMultiplesSourceClasses(prog, sourceFiles, program);
            }
            else
            {
                tuple = RetrieveLocationsSingleSourceClassPosNegative(prog);
            }
            var sourceLocations = tuple.Item1;

            this.Locations = NonDuplicateLocations(sourceLocations);

            //remove
            List<Selection> selections = new List<Selection>();
            foreach (CodeLocation location in Locations)
            {
                Selection selection = new Selection(location.Region.Start, location.Region.Length, location.SourceClass, location.SourceCode);
                selections.Add(selection);
            }
            JsonUtil<List<Selection>>.Write(selections, "found_locations.json");
            //remove

            localionsComputerSoFar = tuple;
            NotifyLocationsObservers(Locations);
        }

        public void Done()
        {
            NotifyHilightObservers(localionsComputerSoFar.Item2);
        }

        private Tuple<List<CodeLocation>, List<TRegion>> RetrieveLocationsSingleSourceClass(Prog prog)
        {
            List<CodeLocation> sourceLocations = new List<CodeLocation>();

            SyntaxNode lca = RegionManager.LeastCommonAncestor(CurrentViewCodeBefore, SelectedLocations);
            Lcas = RegionManager.LeastCommonAncestors(CurrentViewCodeBefore, SelectedLocations);

            List<TRegion> regions = RetrieveLocations(lca, CurrentViewCodeBefore, prog);
            

            //remove
 //           var sm = WorkspaceManager.GetSemanticModel(CurrentProject, SolutionPath, CurrentViewCodePath, lcas.First());
 //           SyntaxTree tree = sm.SyntaxTree;
 //           SyntaxNode n = ASTManager.NodesWithSameStartEndAndKind(tree, lcas.First().Span.Start, lcas.First().Span.End,
 //               lcas.First().CSharpKind()).First();
 ////           var x = sm.GetDeclaredSymbol(n);
 //           var si = sm.GetSymbolInfo(n);
            
 //           var type = x.ContainingType;
 //           var s = type.ToDisplayString();
 //           var name = GetFullMetadataName(type);

            //foreach (ISymbol symbol in sm.LookupSymbols(241))
            //{
            //    if (symbol.CanBeReferencedByName && symbol.Name.Equals("a"))
            //    {
            //        var rlt = SymbolFinder.FindSourceDeclarationsAsync(project, symbol.Name, false).Result;
            //        var rlts = symbol.DeclaringSyntaxReferences;
            //    }
            //}
            //////remove
            //remove

            regions = RegionManager.GroupRegionBySyntaxKind(regions, Lcas);

            foreach (TRegion region in regions)
            {
                CodeLocation location = new CodeLocation { Region = region, SourceCode = CurrentViewCodeBefore, SourceClass = CurrentViewCodePath };
                sourceLocations.Add(location);
            }
            
            Tuple<List<CodeLocation>, List<TRegion>> tuple = Tuple.Create(sourceLocations, regions);

            return tuple;
        }

       private Tuple<List<CodeLocation>, List<TRegion>> RetrieveLocationsSingleSourceClassPosNegative(Prog prog)
        {
            List<TRegion> regions = new List<TRegion>();
            List<CodeLocation> locations = new List<CodeLocation>();
            foreach (var location in Locations)
            {
                MapBase m = (MapBase) prog.Ioperator;
                FilterBase filter = (FilterBase) m.SequenceExpression.Ioperator;
                bool isTruePositive = filter.IsMatch(location.Region.Node);
                if (isTruePositive)
                {
                    regions.Add(location.Region);
                    CodeLocation filtered = new CodeLocation
                    {
                        Region = location.Region,
                        SourceCode = CurrentViewCodeBefore,
                        SourceClass = CurrentViewCodePath
                    };
                    locations.Add(filtered);
                }
            }

            Tuple<List<CodeLocation>, List<TRegion>> tuple = Tuple.Create(locations, regions);

            return tuple;
        }

        private Tuple<List<CodeLocation>, List<TRegion>> RetrieveLocationsMultiplesSourceClasses(Prog prog, List<Tuple<string, string>> sourceFiles, string program)
        {
            LocationExtractor extractor = new LocationExtractor(this.ProjectInformation.SolutionPath);
            List<CodeLocation> sourceLocations = new List<CodeLocation>();
            
            Dictionary<string, List<TRegion>> dicRegions = new Dictionary<string, List<TRegion>>();

            var groups = RegionManager.GetInstance().GroupRegionBySourceFile(SelectedLocations);
           
            List<SyntaxNode> lcas = new List<SyntaxNode>();
            foreach (var item in groups)
            {
                var result = RegionManager.SyntaxNodesForFiltering(item.Key, item.Value);
                lcas.AddRange(result);
            }
            foreach (Tuple<string, string> source in sourceFiles)
            {
                List<TRegion> regions = RetrieveLocations(source.Item1, prog);

                regions = RegionManager.GroupRegionBySyntaxKind(regions, lcas);
                dicRegions.Add(source.Item2, regions);

                foreach (TRegion region in regions)
                {
                    CodeLocation location = new CodeLocation
                    {
                        Region = region,
                        SourceCode = source.Item1,
                        SourceClass = source.Item2
                    };
                    sourceLocations.Add(location);
                }
            }
            var rs = dicRegions[CurrentViewCodePath];//extractor.RetrieveString(prog, program);
            Tuple<List<CodeLocation>, List<TRegion>> tuple = Tuple.Create(sourceLocations, rs);
            return tuple;
        }

        /// <summary>
        /// Selected source files on the format source code, source code path
        /// </summary>
        /// <returns>Selected files</returns>
        private List<Tuple<string, string>> SourceFiles()
        {
            List<Tuple<string, string>> sourceFiles = new List<Tuple<string, string>>();
            string sourceCodePath = SelectedLocations.First().Path;
            for (int index = 1; index < SelectedLocations.Count; index++)
            {
                var slocation = SelectedLocations[index];
                if (!slocation.Path.Equals(sourceCodePath))
                {
                    return WorkspaceManager.GetInstance().SourceFiles(this.ProjectInformation.ProjectPath, this.ProjectInformation.SolutionPath);
                }
            }
            Tuple<string, string> sourceFile = Tuple.Create<string, string>(SelectedLocations.First().Parent.Text, sourceCodePath);
            sourceFiles.Add(sourceFile);
            return sourceFiles;
            
        }

        /// <summary>
        /// Retrieve locations
        /// </summary>
        /// <param name="sourceCode">Selected program</param>
        /// <param name="prog">Location program</param>
        public List<TRegion> RetrieveLocations(string sourceCode, Prog prog)
        {
            LocationExtractor extractor = new LocationExtractor(this.ProjectInformation.SolutionPath);
            List<TRegion> regions = extractor.RetrieveString(prog, sourceCode);
            return regions;
        }

        private List<TRegion> RetrieveLocations(SyntaxNode lca, string sourceCode, Prog prog)
        {
            LocationExtractor extractor = new LocationExtractor(this.ProjectInformation.SolutionPath);
            List<TRegion> regions = extractor.RetrieveString(prog, sourceCode, lca);
            return regions;
        }

        /// <summary>
        /// Selected only non duplicate locations
        /// </summary>
        /// <param name = "locations" > All locations </ param >
        /// < returns > Non duplicate locations</returns>
        //private List<CodeLocation> NonDuplicateLocations(List<CodeLocation> locations)
        //{
        //    List<CodeLocation> clocations = new List<CodeLocation>();
        //    var groupedLocations = RegionManager.GetInstance().GroupLocationsBySourceFile(locations);
        //    foreach (var item in groupedLocations)
        //    {
        //        Dictionary<Tuple<int, int>, CodeLocation> dic = new Dictionary<Tuple<int, int>, CodeLocation>();
        //        foreach (CodeLocation location in item.Value)
        //        {
        //            Tuple<int, int> tuple = Tuple.Create(location.Region.Start, location.Region.Length);
        //            CodeLocation value;
        //            if (!dic.TryGetValue(tuple, out value))
        //            {
        //                dic.Add(tuple, location);
        //            }
        //            else
        //            {
        //                if (value.Region.Node.GetText().ToString().Length < location.Region.Node.GetText().Length)
        //                {
        //                    dic[tuple] = location;
        //                }
        //            }
        //        }
        //        var nonDuplicateLocations = dic.Values.ToList();
        //        clocations.AddRange(nonDuplicateLocations);
        //    }
        //    return clocations;
        //}

        /// <summary>
        /// Selected only non duplicate locations
        /// </summary>
        /// <param name="locations">All locations</param>
        /// <returns>Non duplicate locations</returns>
        private List<CodeLocation> NonDuplicateLocations(List<CodeLocation> locations)
        {
            List<CodeLocation> clocations = new List<CodeLocation>();
            var groupedLocations = RegionManager.GetInstance().GroupLocationsBySourceFile(locations);

            bool [] analized = Enumerable.Repeat(false, locations.Count).ToArray();

            foreach (var item in groupedLocations)
            {
                for (int i = 0; i < item.Value.Count; i++)
                {
                    CodeLocation location = item.Value[i];
                    CodeLocation choosed = location;
                    if (!analized[i])
                    {
                        for (int j = i + 1; j < item.Value.Count; j++)
                        {
                            var otherLocation = item.Value[j];
                            bool intersect = location.Region.IntersectWith(otherLocation.Region);
                            if (intersect)
                            {
                                analized[j] = true;
                                if (otherLocation.Region.Start > location.Region.Start)
                                {
                                    choosed = otherLocation;
                                }
                                //break;
                            }
                        }
                        clocations.Add(choosed);
                    }
                }
            }
            return clocations;
        }
        /// <summary>
        /// Selected only non duplicate locations
        /// </summary>
        /// <param name="locations">All locations</param>
        /// <returns>Non duplicate locations</returns>
        private static List<TRegion> NonDuplicateLocations(List<TRegion> locations)
        {
            List<TRegion> nonDuplicateLocations = new List<TRegion>();
            foreach (TRegion location in locations)
            {
                bool distinct = true;
                foreach (TRegion region in nonDuplicateLocations)
                {
                    if (region.Start == location.Start && region.Length == location.Length)
                    {
                        distinct = false; break;
                    }
                }
                if (distinct)
                {
                    nonDuplicateLocations.Add(location);
                }
            }
            return nonDuplicateLocations;
        }

        /// <summary>
        /// Refactor a region
        /// </summary>
        public void Refact()
        {
            long millBefore = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            FillEditedLocations();
            LocationExtractor extractor = new LocationExtractor(this.ProjectInformation.SolutionPath);
            List<Transformation> transformations = extractor.TransformProgram(false);
            SourceTransformations = transformations;

            //SynthesizedProgram synthesized = extractor.TransformationProgram(SelectedLocations);
            long millAfer = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            long totalTime = (millAfer - millBefore);
            FileUtil.WriteToFile(@"edit.t", totalTime.ToString());

            NotifyProgramRefactoredObservers(transformations);
            //NotifyLocationsTransformedObservers(synthesized, Locations);

            ClearAfterRefact();

        }

        /// <summary>
        /// Fill edited locations. This method as created more for test purpose
        /// </summary>
        private void FillEditedLocations()
        {
            if (ProjectionBuffers == null) return;

            Dictionary<string, List<Selection>> dicSelections= new Dictionary<string, List<Selection>>();
            foreach (var item in ProjectionBuffers)
            {
                List<Selection> selections = new List<Selection>();
                foreach (var span in item.Value.CurrentSnapshot.GetSourceSpans())
                {
                    Selection selection = new Selection(span.Start, span.Length, item.Key, null);
                    selections.Add(selection);
                }
                dicSelections.Add(item.Key, selections);
            }

            EditedLocations = dicSelections;

            JsonUtil<Dictionary<string, List<Selection>>>.Write(dicSelections, "edited_selections.json");
        }


        /// <summary>
        /// Clear and start
        /// </summary>
        private void ClearAfterRefact()
        {
            //Init();
            this.SelectedLocations = new List<TRegion>();
            this.Progs = new List<Prog>();
            this.Locations = null;
            this.CurrentViewCodeBefore = null;
            this.CurrentViewCodeAfter = null;
            this.CurrentViewCodePath = null;
        }

        /// <summary>
        /// Add highlight observer
        /// </summary>
        /// <param name="observer">Observer</param>
        public void AddHilightObserver(IHilightObserver observer)
        {
            this._hilightObservers.Add(observer);
        }

        /// <summary>
        /// Notify highlight observers
        /// </summary>
        /// <param name="regions">Regions</param>
        private void NotifyHilightObservers(List<TRegion> regions)
        {
            HighlightEvent hEvent = new HighlightEvent(regions);
            foreach (IHilightObserver observer in _hilightObservers)
            {
                observer.NotifyHilightChanged(hEvent);
            }
        }

        /// <summary>
        /// Add program generated observer
        /// </summary>
        /// <param name="observer">Observer</param>
        public void AddProgramGeneratedObserver(IProgramsGeneratedObserver observer)
        {
            this._programGeneratedObservers.Add(observer);
        }

        /// <summary>
        /// Notify generated observers
        /// </summary>
        /// <param name="programs">Generated observers</param>
        private void NotifyLocationProgramGeneratedObservers(List<Prog> programs)
        {
            ProgramGeneratedEvent pEvent = new ProgramGeneratedEvent(programs);
            foreach (IProgramsGeneratedObserver observer in _programGeneratedObservers)
            {
                observer.NotifyProgramGenerated(pEvent);
            }
        }

        /// <summary>
        /// Add refactored observer
        /// </summary>
        /// <param name="observer">Observer</param>
        public void AddProgramRefactoredObserver(IProgramRefactoredObserver observer)
        {
            this._programsRefactoredObserver.Add(observer);
        }

        /// <summary>
        /// Notify program refactored observers
        /// </summary>
        /// <param name="transformations"></param>
        private void NotifyProgramRefactoredObservers(List<Transformation> transformations)
        {
            ProgramRefactoredEvent rEvent = new ProgramRefactoredEvent(transformations);

            foreach (IProgramRefactoredObserver observer in _programsRefactoredObserver)
            {
                observer.NotifyProgramRefactored(rEvent);
            }
        }

        /// <summary>
        /// Add locations transformed observer
        /// </summary>
        /// <param name="observer">observer</param>
        public void AddLocationsTransformedObvserver(ILocationsTransformedObserver observer)
        {
            this._locationsTransformedObserver.Add(observer);
        }

        ///// <summary>
        ///// Notify locations transformation observers
        ///// </summary>
        ///// <param name="program">Synthesized program</param>
        ///// <param name="locations">Code locations</param>
        //private void NotifyLocationsTransformedObservers(SynthesizedProgram program, IEnumerable<CodeLocation> locations)
        //{
        //    List<CodeTransformation> transformations = new List<CodeTransformation>();
        //    foreach (CodeLocation location in locations)
        //    {
        //        Tuple<string, string> transformedLocation = Transformation(location, program);

        //        if (transformedLocation == null) continue;

        //        CodeTransformation transformation = new CodeTransformation(location, transformedLocation);
        //        transformations.Add(transformation);
        //    }

        //    this.CodeTransformations = transformations;

        //    LocationsTransformedEvent ltEvent = new LocationsTransformedEvent(transformations);

        //    foreach (ILocationsTransformedObserver observer in _locationsTransformedObserver)
        //    {
        //        observer.NotifyLocationsTransformed(ltEvent);
        //    }
        //}

        ///// <summary>
        ///// Look for a place to put this. Refactor
        ///// </summary>
        ///// <returns></returns>
        //public Tuple<string, string> Transformation(CodeLocation location, SynthesizedProgram program)
        //{
        //    TRegion region = location.Region;
        //    try
        //    {
        //        ASTTransformation tree = ASTProgram.TransformString(region.Node, program);
        //        string transformation = tree.transformation;

        //        Tuple<string, string> transformedLocation = Tuple.Create(region.Node.GetText().ToString(), transformation);
        //        return transformedLocation;
        //    }
        //    catch (ArgumentOutOfRangeException e)
        //    { Console.WriteLine(e.Message); }

        //    return null;
        //}

        /// <summary>
        /// Add locations transformed observer
        /// </summary>
        /// <param name="observer">observer</param>
        public void AddLocationsObserver(ILocationsObserver observer)
        {
            this._locationsObversers.Add(observer);
        }

        /// <summary>
        /// Notify locations observers
        /// </summary>
        /// <param name="transformedLocations">Code locations</param>
        private void NotifyLocationsObservers(List<CodeLocation> transformedLocations)
        {
            LocationEvent lEvent = new LocationEvent(transformedLocations);

            foreach (ILocationsObserver observer in _locationsObversers)
            {
                observer.NotifyLocationsSelected(lEvent);
            }
        }
    }
}

//private Tuple<List<CodeLocation>, List<TRegion>> RetrieveLocationsSingleSourceClass(Prog prog)
//{
//    List<CodeLocation> sourceLocations = new List<CodeLocation>();

//    SyntaxNode lca = RegionManager.LeastCommonAncestor(CurrentViewCodeBefore, SelectedLocations);
//    Lcas = RegionManager.LeastCommonAncestors(CurrentViewCodeBefore, SelectedLocations);

//    List<TRegion> regions = RetrieveLocations(lca, CurrentViewCodeBefore, prog);


//    //remove
//    //           var sm = WorkspaceManager.GetSemanticModel(CurrentProject, SolutionPath, CurrentViewCodePath, lcas.First());
//    //           SyntaxTree tree = sm.SyntaxTree;
//    //           SyntaxNode n = ASTManager.NodesWithSameStartEndAndKind(tree, lcas.First().Span.Start, lcas.First().Span.End,
//    //               lcas.First().CSharpKind()).First();
//    ////           var x = sm.GetDeclaredSymbol(n);
//    //           var si = sm.GetSymbolInfo(n);

//    //           var type = x.ContainingType;
//    //           var s = type.ToDisplayString();
//    //           var name = GetFullMetadataName(type);

//    //foreach (ISymbol symbol in sm.LookupSymbols(241))
//    //{
//    //    if (symbol.CanBeReferencedByName && symbol.Name.Equals("a"))
//    //    {
//    //        var rlt = SymbolFinder.FindSourceDeclarationsAsync(project, symbol.Name, false).Result;
//    //        var rlts = symbol.DeclaringSyntaxReferences;
//    //    }
//    //}
//    //////remove
//    //remove

//    regions = RegionManager.GroupRegionBySyntaxKind(regions, Lcas);

//    foreach (TRegion region in regions)
//    {
//        CodeLocation location = new CodeLocation { Region = region, SourceCode = CurrentViewCodeBefore, SourceClass = CurrentViewCodePath };
//        sourceLocations.Add(location);
//    }

//    Tuple<List<CodeLocation>, List<TRegion>> tuple = Tuple.Create(sourceLocations, regions);

//    return tuple;
//}

///// <summary>
///// Decompose locations
///// </summary>
//public void Decompose()
//{
//    LocationExtractor extractor = new LocationExtractor(solutionPath);
//    //remove
//    JsonUtil<List<TRegion>>.Write(RegionsBeforeEdition, "simple_api_change_input.json");
//    //remove
//    //List<Prog> programs = extractor.Decompose(RegionsBeforeEdition);
//    Progs = extractor.Decompose(RegionsBeforeEdition);

//    //List<Prog> filtereds = new List<Prog>();
//    //foreach (Prog program in programs)
//    //{
//    //    Prog val;
//    //    if (!Progs.TryGetValue(program.ToString(), out val))
//    //    {
//    //        Progs.Add(program.ToString(), program);
//    //        filtereds.Add(program);
//    //    }
//    //}
//    //NotifyProgramGeneratedObservers(filtereds);
//}

///// <summary>
///// Regions after the transformation
///// </summary>
///// <returns></returns>
//public Dictionary<Color, List<TRegion>> RegionsAfterEdit { get; set; }
/*/// <summary>
       /// Decompose locations
       /// </summary>
       /// <param name="color"></param>
       public void Decompose(Color color)
       {
           LocationRefactor.Location.LocationExtractor extractor = new LocationRefactor.Location.LocationExtractor(syntaxKind, solutionPath);
           List<Prog> programs = extractor.Decompose(RegionsBeforeEdit, (Color)color);

           List<Prog> filtereds = new List<Prog>();
           foreach (Prog program in programs)
           {
               Prog val;
               if (!Progs.TryGetValue(program.ToString(), out val))
               {
                   Progs.Add(program.ToString(), program);
                   filtereds.Add(program);
               }
           }

           NotifyProgramGeneratedObservers(filtereds);
       }*/

/* private void NotifyLocationsTransformedObservers(List<Tuple<string, string>> transformedLocations)
         {
             LocationsTransformedEvent ltEvent = new LocationsTransformedEvent(transformedLocations);

             foreach (ILocationsTransformedObserver observer in locationsTransformedObserver) {
                 observer.NotifyLocationsTransformed(ltEvent);
             }
         }*/


/* /// <summary>
         /// Edit locations
         /// </summary>
         /// <param name="start">Start position</param>
         /// <param name="length">Region length</param>
         /// <param name="sourceCode">Source code</param>
         /// <param name="color">Color region</param>
         /// <param name="range">Region range</param>
         public void Edit(int start, int length, String sourceCode, Color color, Range range)
         {
             TRegion tregion = new TRegion();

             tregion.Start = start;
             tregion.Length = length;
             tregion.Text = sourceCode;
             tregion.Color = color;
             tregion.Range = range;

             List<TRegion> values;

             if (!RegionsBeforeEdit.TryGetValue(color, out values))
             {
                 values = new List<TRegion>();
                 RegionsBeforeEdit.Add(color, values);
             }

             values.Add(tregion);

             foreach (KeyValuePair<Color, List<TRegion>> dic in RegionsBeforeEdit)
             {
                 foreach (TRegion tr in dic.Value)
                 {
                     if (tregion.IsParent(tr))
                     {
                         tregion.Parent = tr;
                     }
                 }
             }

             if (tregion.Parent == null)
             {
                 tregion.Parent = RegionsBeforeEdit[Color.White][0];
             }
         }*/

///// <summary>
///// Load from file
///// </summary>
///// <param name="fileName">File name</param>
///// <returns>file content</returns>
//public string Load(String fileName)
//{
//    System.IO.StreamReader sr = new
//    System.IO.StreamReader(fileName);

//    String text = sr.ReadToEnd();

//    text = text.Replace("\r\n", "\n");
//    TRegion tRegion = new TRegion();
//    tRegion.Text = text;
//    tRegion.Color = Color.White;

//    List<TRegion> tRegions = new List<TRegion>();
//    tRegions.Add(tRegion);

//    RegionsBeforeEdit.Add(Color.White, tRegions);
//    sr.Close();

//    return text;
//}

///// <summary>
///// Refact a region
///// </summary>
///// <param name="program">Source code</param>
///// <param name="color">Selected color</param>
//public void Refact(String program, Color color)
//{
//    Prog prog = Progs[program];

//    LocationRefactor.Location.LocationExtractor extractor = new LocationRefactor.Location.LocationExtractor(syntaxKind, solutionPath);
//    String transformation = extractor.TransformProgram(RegionsBeforeEdit, RegionsAfterEdit, color, prog);

//    //List<Tuple<string, string>> transformedLocations = extractor.TransformLocations(RegionsBeforeEdit, RegionsAfterEdit, color, prog);
//    SynthesizedProgram synthesized = extractor.TransformationProgram(RegionsBeforeEdit, RegionsAfterEdit, color);

//    NotifyProgramRefactoredObservers(transformation);
//    NotifyLocationsTransformedObservers(synthesized, locations);
//}


///// <summary>
///// Edit done
///// </summary>
///// <param name="code">Source code</param>
///// <param name="start">Start position</param>
///// <param name="length">Region length</param>
//public void EditDone(String code, int start, int length)
//{
//    code = code.Replace("\r\n", "\n");
//    TRegion tRegion = new TRegion();
//    tRegion.Text = code;
//    tRegion.Color = Color.White;

//    List<TRegion> tRegions = new List<TRegion>();
//    tRegions.Add(tRegion);

//    RegionsAfterEdit[Color.White] = tRegions;

//    String text = code.Substring(start, length);
//    TRegion tregion = new TRegion();

//    Color color = Color.LightGreen;

//    tregion.Start = start;
//    tregion.Length = length;
//    tregion.Text = text;
//    tregion.Color = color;

//    List<TRegion> values;

//    if (!RegionsAfterEdit.TryGetValue(color, out values))
//    {
//        values = new List<TRegion>();
//        RegionsAfterEdit.Add(color, values);
//    }

//    values.Add(tregion);

//    foreach (KeyValuePair<Color, List<TRegion>> dic in RegionsAfterEdit)
//    {
//        foreach (TRegion tr in dic.Value)
//        {
//            if (tregion.IsParent(tr))
//            {
//                tregion.Parent = tr;
//            }
//        }
//    }

//    if (tregion.Parent == null)
//    {
//        tregion.Parent = RegionsAfterEdit[Color.White][0];
//    }
//}
// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if DEBUG
//#define CHECK_LOCALS // define CHECK_LOCALS to help debug some rewriting problems that would otherwise cause code-gen failures
#endif

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// The rewriter for removing lambda expressions from method bodies and introducing closure classes
    /// as containers for captured variables along the lines of the example in section 6.5.3 of the
    /// C# language specification.
    /// 
    /// The entry point is the public method <see cref="Rewrite"/>.  It operates as follows:
    /// 
    /// First, an analysis of the whole method body is performed that determines which variables are
    /// captured, what their scopes are, and what the nesting relationship is between scopes that
    /// have captured variables.  The result of this analysis is left in <see cref="analysis"/>.
    /// 
    /// Then we make a frame, or compiler-generated class, represented by an instance of
    /// <see cref="LambdaFrame"/> for each scope with captured variables.  The generated frames are kept
    /// in <see cref="frames"/>.  Each frame is given a single field for each captured
    /// variable in the corresponding scope.  These are are maintained in <see cref="MethodToClassRewriter.proxies"/>.
    /// 
    /// Finally, we walk and rewrite the input bound tree, keeping track of the following:
    /// (1) The current set of active frame pointers, in <see cref="framePointers"/>
    /// (2) The current method being processed (this changes within a lambda's body), in <see cref="currentMethod"/>
    /// (3) The "this" symbol for the current method in <see cref="currentFrameThis"/>, and
    /// (4) The symbol that is used to access the innermost frame pointer (it could be a local variable or "this" parameter)
    /// 
    /// There are a few key transformations done in the rewriting.
    /// (1) Lambda expressions are turned into delegate creation expressions, and the body of the lambda is
    ///     moved into a new, compiler-generated method of a selected frame class.
    /// (2) On entry to a scope with captured variables, we create a frame object and store it in a local variable.
    /// (3) References to captured variables are transformed into references to fields of a frame class.
    /// 
    /// In addition, the rewriting deposits into <see cref="TypeCompilationState.SynthesizedMethods"/> a (<see cref="MethodSymbol"/>, <see cref="BoundStatement"/>)
    /// pair for each generated method.
    /// 
    /// <see cref="Rewrite"/> produces its output in two forms.  First, it returns a new bound statement
    /// for the caller to use for the body of the original method.  Second, it returns a collection of
    /// (<see cref="MethodSymbol"/>, <see cref="BoundStatement"/>) pairs for additional methods that the lambda rewriter produced.
    /// These additional methods contain the bodies of the lambdas moved into ordinary methods of their
    /// respective frame classes, and the caller is responsible for processing them just as it does with
    /// the returned bound node.  For example, the caller will typically perform iterator method and
    /// asynchronous method transformations, and emit IL instructions into an assembly.
    /// </summary>
    sealed partial class LambdaRewriter : MethodToClassRewriter
    {
        private readonly Analysis analysis;
        private readonly MethodSymbol topLevelMethod;
        private readonly int topLevelMethodOrdinal;

        // lambda frame for static lambdas. 
        // initialized lazily and could be null if there are no static lambdas
        private LambdaFrame lazyStaticLambdaFrame;

        // A mapping from every lambda parameter to its corresponding method's parameter.
        private readonly Dictionary<ParameterSymbol, ParameterSymbol> parameterMap = new Dictionary<ParameterSymbol, ParameterSymbol>();

        // for each block with lifted (captured) variables, the corresponding frame type
        private readonly Dictionary<BoundNode, LambdaFrame> frames = new Dictionary<BoundNode, LambdaFrame>();

        // the current set of frame pointers in scope.  Each is either a local variable (where introduced),
        // or the "this" parameter when at the top level.  Keys in this map are never constructed types.
        private readonly Dictionary<NamedTypeSymbol, Symbol> framePointers = new Dictionary<NamedTypeSymbol, Symbol>();

        // True if the rewritten tree should include assignments of the
        // original locals to the lifted proxies. This is only useful for the
        // expression evaluator where the original locals are left as is.
        private readonly bool assignLocals;

        // The current method or lambda being processed.
        private MethodSymbol currentMethod;

        // The "this" symbol for the current method.
        private ParameterSymbol currentFrameThis;

        private int lambdaOrdinalDispenser;

        // ID dispenser for field names of frame references
        private int synthesizedFieldNameIdDispenser;

        // The symbol (field or local) holding the innermost frame
        private Symbol innermostFramePointer;

        // The mapping of type parameters for the current lambda body
        private TypeMap currentLambdaBodyTypeMap;

        // The current set of type parameters (mapped from the enclosing method's type parameters)
        private ImmutableArray<TypeParameterSymbol> currentTypeParameters;

        // Initialization for the proxy of the upper frame if it needs to be deferred.
        // Such situation happens when lifting this in a ctor.
        private BoundExpression thisProxyInitDeferred;

        // Set to true once we've seen the base (or self) constructor invocation in a constructor
        private bool seenBaseCall;

        // Set to true while translating code inside of an expression lambda.
        private bool inExpressionLambda;

        // When a lambda captures only 'this' of the enclosing method, we cache it in a local
        // variable.  This is the set of such local variables that must be added to the enclosing
        // method's top-level block.
        private ArrayBuilder<LocalSymbol> addedLocals;

        // Similarly, this is the set of statements that must be added to the enclosing method's
        // top-level block initializing those variables to null.
        private ArrayBuilder<BoundStatement> addedStatements;

        private LambdaRewriter(
            Analysis analysis,
            NamedTypeSymbol thisType,
            ParameterSymbol thisParameterOpt,
            MethodSymbol method,
            int methodOrdinal,
            int lambdaOrdinalDispenser,
            VariableSlotAllocator slotAllocatorOpt,
            TypeCompilationState compilationState,
            DiagnosticBag diagnostics,
            bool assignLocals)
            : base(slotAllocatorOpt, compilationState, diagnostics)
        {
            Debug.Assert(analysis != null);
            Debug.Assert(thisType != null);
            Debug.Assert(method != null);
            Debug.Assert(compilationState != null);
            Debug.Assert(diagnostics != null);

            this.topLevelMethod = method;
            this.topLevelMethodOrdinal = methodOrdinal;
            this.lambdaOrdinalDispenser = lambdaOrdinalDispenser;
            this.currentMethod = method;
            this.analysis = analysis;
            this.assignLocals = assignLocals;
            this.currentTypeParameters = method.TypeParameters;
            this.currentLambdaBodyTypeMap = TypeMap.Empty;
            this.innermostFramePointer = currentFrameThis = thisParameterOpt;
            this.framePointers[thisType] = thisParameterOpt;
            this.seenBaseCall = method.MethodKind != MethodKind.Constructor; // only used for ctors
            this.synthesizedFieldNameIdDispenser = 1;
        }

        protected override bool NeedsProxy(Symbol localOrParameter)
        {
            Debug.Assert(localOrParameter is LocalSymbol || localOrParameter is ParameterSymbol);
            return analysis.capturedVariables.ContainsKey(localOrParameter);
        }

        /// <summary>
        /// Rewrite the given node to eliminate lambda expressions.  Also returned are the method symbols and their
        /// bound bodies for the extracted lambda bodies. These would typically be emitted by the caller such as
        /// MethodBodyCompiler.  See this class' documentation
        /// for a more thorough explanation of the algorithm and its use by clients.
        /// </summary>
        /// <param name="node">The bound node to be rewritten</param>
        /// <param name="thisType">The type of the top-most frame</param>
        /// <param name="thisParameter">The "this" parameter in the top-most frame, or null if static method</param>
        /// <param name="method">The containing method of the node to be rewritten</param>
        /// <param name="methodOrdinal">Index of the method symbol in its containing type member list.</param>
        /// <param name="lambdaOrdinalDispenser">The current lambda ordinal.</param>
        /// <param name="scopeOrdinalDispenser">The current closure scope ordinal.</param>
        /// <param name="slotAllocatorOpt">Slot allocator.</param>
        /// <param name="compilationState">The caller's buffer into which we produce additional methods to be emitted by the caller</param>
        /// <param name="diagnostics">Diagnostic bag for diagnostics</param>
        /// <param name="analysis">A caller-provided analysis of the node's lambdas</param>
        /// <param name="assignLocals">The rewritten tree should include assignments of the original locals to the lifted proxies</param>
        public static BoundStatement Rewrite(
            BoundStatement node,
            NamedTypeSymbol thisType,
            ParameterSymbol thisParameter,
            MethodSymbol method,
            int methodOrdinal,
            ref int lambdaOrdinalDispenser,
            ref int scopeOrdinalDispenser,
            VariableSlotAllocator slotAllocatorOpt,
            TypeCompilationState compilationState,
            DiagnosticBag diagnostics,
            Analysis analysis,
            bool assignLocals = false)
        {
            Debug.Assert((object)thisType != null);
            Debug.Assert(((object)thisParameter == null) || (thisParameter.Type == thisType));

            CheckLocalsDefined(node);
            var rewriter = new LambdaRewriter(
                analysis,
                thisType,
                thisParameter,
                method,
                methodOrdinal,
                lambdaOrdinalDispenser,
                slotAllocatorOpt,
                compilationState,
                diagnostics,
                assignLocals);

            analysis.ComputeLambdaScopesAndFrameCaptures();
            rewriter.MakeFrames(ref scopeOrdinalDispenser);
            var body = rewriter.AddStatementsIfNeeded((BoundStatement)rewriter.Visit(node));
            CheckLocalsDefined(body);

            // Lambdas created during the rewriter are assigned indices and the dispenser field is updated.
            lambdaOrdinalDispenser = rewriter.lambdaOrdinalDispenser;
            return body;
        }

        BoundStatement AddStatementsIfNeeded(BoundStatement body)
        {
            if (addedLocals != null)
            {
                addedStatements.Add(body);
                body = new BoundBlock(body.Syntax, addedLocals.ToImmutableAndFree(), addedStatements.ToImmutableAndFree()) { WasCompilerGenerated = true };
                addedLocals = null;
                addedStatements = null;
            }
            else
            {
                Debug.Assert(addedStatements == null);
            }

            return body;
        }

        protected override TypeMap TypeMap
        {
            get { return currentLambdaBodyTypeMap; }
        }

        protected override MethodSymbol CurrentMethod
        {
            get { return currentMethod; }
        }

        protected override NamedTypeSymbol ContainingType
        {
            get { return this.topLevelMethod.ContainingType; }
        }

        /// <summary>
        /// Check that the top-level node is well-defined, in the sense that all
        /// locals that are used are defined in some enclosing scope.
        /// </summary>
        static partial void CheckLocalsDefined(BoundNode node);

        /// <summary>
        /// Create the frame types.
        /// </summary>
        private void MakeFrames(ref int scopeOrdinalDispenser)
        {
            NamedTypeSymbol containingType = this.ContainingType;

            foreach (var kvp in analysis.capturedVariables)
            {
                var captured = kvp.Key;

                BoundNode scope;
                if (!analysis.variableScope.TryGetValue(captured, out scope))
                {
                    continue;
                }

                LambdaFrame frame = GetFrameForScope(scope, ref scopeOrdinalDispenser);

                var hoistedField = LambdaCapturedVariable.Create(frame, captured, ref synthesizedFieldNameIdDispenser);
                proxies.Add(captured, new CapturedToFrameSymbolReplacement(hoistedField, isReusable: false));
                if (CompilationState.Emitting)
                {
                    CompilationState.ModuleBuilderOpt.AddSynthesizedDefinition(frame, hoistedField);
                }

                if (hoistedField.Type.IsRestrictedType())
                {
                    foreach (CSharpSyntaxNode syntax in kvp.Value)
                    {
                        // CS4013: Instance of type '{0}' cannot be used inside an anonymous function, query expression, iterator block or async method
                        this.Diagnostics.Add(ErrorCode.ERR_SpecialByRefInLambda, syntax.Location, hoistedField.Type);
                    }
                }
            }
        }

        private LambdaFrame GetFrameForScope(BoundNode scope, ref int scopeIndexDispenser)
        {
            LambdaFrame frame;
            if (!frames.TryGetValue(scope, out frame))
            {
                frame = new LambdaFrame(slotAllocatorOpt, CompilationState, topLevelMethod, topLevelMethodOrdinal, scope.Syntax, scopeIndexDispenser++, isStatic: false);
                frames.Add(scope, frame);

                if (CompilationState.Emitting)
                {
                    CompilationState.ModuleBuilderOpt.AddSynthesizedDefinition(this.ContainingType, frame);
                    CompilationState.AddSynthesizedMethod(
                        frame.Constructor, 
                        FlowAnalysisPass.AppendImplicitReturn(MethodCompiler.BindMethodBody(frame.Constructor, CompilationState, null), 
                        frame.Constructor));
                }
            }

            return frame;
        }

        private LambdaFrame GetStaticFrame(DiagnosticBag diagnostics, BoundNode lambda)
        {
            if (this.lazyStaticLambdaFrame == null)
            {
                var isNonGeneric = !topLevelMethod.IsGenericMethod;
                if (isNonGeneric)
                {
                    this.lazyStaticLambdaFrame = CompilationState.staticLambdaFrame;
                }

                if (this.lazyStaticLambdaFrame == null)
                {
                    // associate the frame with the the first lambda that caused it to exist. 
                    // we need to associate this with somme syntax.
                    // unfortunately either containing method or containing class could be synthetic
                    // therefore could have no syntax.
                    CSharpSyntaxNode syntax = lambda.Syntax;

                    this.lazyStaticLambdaFrame = new LambdaFrame(slotAllocatorOpt, CompilationState, topLevelMethod, isNonGeneric ? -1 : topLevelMethodOrdinal, syntax, scopeOrdinal: -1, isStatic: true);

                    // nongeneric static lambdas can share the frame
                    if (isNonGeneric)
                    {
                        CompilationState.staticLambdaFrame = this.lazyStaticLambdaFrame;
                    }

                    if (CompilationState.Emitting)
                    {
                        var frame = this.lazyStaticLambdaFrame;
                        
                        // add frame type
                        CompilationState.ModuleBuilderOpt.AddSynthesizedDefinition(this.ContainingType, frame);

                        // add its ctor
                        CompilationState.AddSynthesizedMethod(
                            frame.Constructor, 
                            FlowAnalysisPass.AppendImplicitReturn(MethodCompiler.BindMethodBody(frame.Constructor, CompilationState, null),
                            frame.Constructor));

                        // add cctor
                        // Frame.inst = new Frame()
                        var F = new SyntheticBoundNodeFactory(frame.StaticConstructor, syntax, CompilationState, diagnostics);
                        var body = F.Block(
                                F.Assignment(
                                    F.Field(null, frame.SingletonCache),
                                    F.New(frame.Constructor)),
                                F.Return()); 

                        CompilationState.AddSynthesizedMethod(frame.StaticConstructor, body);
                    }
                }

            }

            return this.lazyStaticLambdaFrame;
        }

        /// <summary>
        /// Produce a bound expression representing a pointer to a frame of a particular frame type.
        /// </summary>
        /// <param name="syntax">The syntax to attach to the bound nodes produced</param>
        /// <param name="frameType">The type of frame to be returned</param>
        /// <returns>A bound node that computes the pointer to the required frame</returns>
        private BoundExpression FrameOfType(CSharpSyntaxNode syntax, NamedTypeSymbol frameType)
        {
            BoundExpression result = FramePointer(syntax, frameType.OriginalDefinition);
            Debug.Assert(result.Type == frameType);
            return result;
        }

        /// <summary>
        /// Produce a bound expression representing a pointer to a frame of a particular frame class.
        /// Note that for generic frames, the frameClass parameter is the generic definition, but
        /// the resulting expression will be constructed with the current type parameters.
        /// </summary>
        /// <param name="syntax">The syntax to attach to the bound nodes produced</param>
        /// <param name="frameClass">The class type of frame to be returned</param>
        /// <returns>A bound node that computes the pointer to the required frame</returns>
        protected override BoundExpression FramePointer(CSharpSyntaxNode syntax, NamedTypeSymbol frameClass)
        {
            Debug.Assert(frameClass.IsDefinition);

            // If in an instance method of the right type, we can just return the "this" pointer.
            if ((object)currentFrameThis != null && currentFrameThis.Type == frameClass)
            {
                return new BoundThisReference(syntax, frameClass);
            }

            // Otherwise we need to return the value from a frame pointer local variable...
            Symbol framePointer = framePointers[frameClass];
            CapturedSymbolReplacement proxyField;
            if (proxies.TryGetValue(framePointer, out proxyField))
            {
                // However, frame pointer local variables themselves can be "captured".  In that case
                // the inner frames contain pointers to the enclosing frames.  That is, nested
                // frame pointers are organized in a linked list.
                return proxyField.Replacement(syntax, frameType => FramePointer(syntax, frameType));
            }

            var localFrame = framePointer as LocalSymbol;
            return new BoundLocal(syntax, localFrame, null, localFrame.Type);
        }

        private static void InsertAndFreePrologue(ArrayBuilder<BoundStatement> result, ArrayBuilder<BoundExpression> prologue)
        {
            foreach (var expr in prologue)
            {
                result.Add(new BoundExpressionStatement(expr.Syntax, expr));
            }

            prologue.Free();
        }

        /// <summary>
        /// Introduce a frame around the translation of the given node.
        /// </summary>
        /// <param name="node">The node whose translation should be translated to contain a frame</param>
        /// <param name="frame">The frame for the translated node</param>
        /// <param name="F">A function that computes the translation of the node.  It receives lists of added statements and added symbols</param>
        /// <returns>The translated statement, as returned from F</returns>
        private T IntroduceFrame<T>(BoundNode node, LambdaFrame frame, Func<ArrayBuilder<BoundExpression>, ArrayBuilder<LocalSymbol>, T> F)
        {
            NamedTypeSymbol frameType = frame.ConstructIfGeneric(StaticCast<TypeSymbol>.From(currentTypeParameters));
            LocalSymbol framePointer = new SynthesizedLocal(this.topLevelMethod, frameType, SynthesizedLocalKind.LambdaDisplayClass, frame.ScopeSyntaxOpt);

            CSharpSyntaxNode syntax = node.Syntax;

            // assign new frame to the frame variable

            var prologue = ArrayBuilder<BoundExpression>.GetInstance();

            MethodSymbol constructor = frame.Constructor.AsMember(frameType);
            Debug.Assert(frameType == constructor.ContainingType);
            var newFrame = new BoundObjectCreationExpression(
                syntax: syntax,
                constructor: constructor);

            prologue.Add(new BoundAssignmentOperator(syntax,
                new BoundLocal(syntax, framePointer, null, frameType),
                newFrame,
                frameType));

            CapturedSymbolReplacement oldInnermostFrameProxy = null;
            if ((object)innermostFramePointer != null)
            {
                proxies.TryGetValue(innermostFramePointer, out oldInnermostFrameProxy);
                if (analysis.needsParentFrame.Contains(node))
                {
                    var capturedFrame = LambdaCapturedVariable.Create(frame, innermostFramePointer, ref synthesizedFieldNameIdDispenser);
                    FieldSymbol frameParent = capturedFrame.AsMember(frameType);
                    BoundExpression left = new BoundFieldAccess(syntax, new BoundLocal(syntax, framePointer, null, frameType), frameParent, null);
                    BoundExpression right = FrameOfType(syntax, frameParent.Type as NamedTypeSymbol);
                    BoundExpression assignment = new BoundAssignmentOperator(syntax, left, right, left.Type);

                    if (this.currentMethod.MethodKind == MethodKind.Constructor && capturedFrame.Type == this.currentMethod.ContainingType && !this.seenBaseCall)
                    {
                        // Containing method is a constructor 
                        // Initialization statement for the "this" proxy must be inserted
                        // after the constructor initializer statement block
                        // This insertion will be done by the delegate F
                        Debug.Assert(thisProxyInitDeferred == null);
                        thisProxyInitDeferred = assignment;
                    }
                    else
                    {
                        prologue.Add(assignment);
                    }

                    if (CompilationState.Emitting)
                    {
                        CompilationState.ModuleBuilderOpt.AddSynthesizedDefinition(frame, capturedFrame);
                    }

                    proxies[innermostFramePointer] = new CapturedToFrameSymbolReplacement(capturedFrame, isReusable: false);
                }
            }

            // Capture any parameters of this block.  This would typically occur
            // at the top level of a method or lambda with captured parameters.
            // TODO: speed up the following by computing it in analysis.
            foreach (var variable in analysis.capturedVariables.Keys)
            {
                BoundNode varNode;
                if (!analysis.variableScope.TryGetValue(variable, out varNode) || varNode != node)
                {
                    continue;
                }

                InitVariableProxy(syntax, variable, framePointer, prologue);
            }

            Symbol oldInnermostFramePointer = innermostFramePointer;

            innermostFramePointer = framePointer;
            var addedLocals = ArrayBuilder<LocalSymbol>.GetInstance();
            addedLocals.Add(framePointer);
            framePointers.Add(frame, framePointer);

            var result = F(prologue, addedLocals);

            framePointers.Remove(frame);
            innermostFramePointer = oldInnermostFramePointer;

            if ((object)innermostFramePointer != null)
            {
                if (oldInnermostFrameProxy != null)
                {
                    proxies[innermostFramePointer] = oldInnermostFrameProxy;
                }
                else
                {
                    proxies.Remove(innermostFramePointer);
                }
            }

            return result;
        }

        private void InitVariableProxy(CSharpSyntaxNode syntax, Symbol symbol, LocalSymbol framePointer, ArrayBuilder<BoundExpression> prologue)
        {
            CapturedSymbolReplacement proxy;
            if (proxies.TryGetValue(symbol, out proxy))
            {
                BoundExpression value;
                switch (symbol.Kind)
                {
                    case SymbolKind.Parameter:
                        var parameter = (ParameterSymbol)symbol;
                        ParameterSymbol parameterToUse;
                        if (!parameterMap.TryGetValue(parameter, out parameterToUse))
                        {
                            parameterToUse = parameter;
                        }

                        value = new BoundParameter(syntax, parameterToUse);
                        break;

                    case SymbolKind.Local:
                        if (!this.assignLocals)
                        {
                            return;
                        }
                        
                        var local = (LocalSymbol)symbol;
                        LocalSymbol localToUse;
                        if (!localMap.TryGetValue(local, out localToUse))
                        {
                            localToUse = local;
                        }

                        value = new BoundLocal(syntax, localToUse, null, localToUse.Type);
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
                }

                var left = proxy.Replacement(syntax, frameType1 => new BoundLocal(syntax, framePointer, null, framePointer.Type));
                var assignToProxy = new BoundAssignmentOperator(syntax, left, value, value.Type);
                prologue.Add(assignToProxy);
            }
        }

        #region Visit Methods

        protected override BoundNode VisitUnhoistedParameter(BoundParameter node)
        {
            ParameterSymbol replacementParameter;
            if (this.parameterMap.TryGetValue(node.ParameterSymbol, out replacementParameter))
            {
                return new BoundParameter(node.Syntax, replacementParameter, replacementParameter.Type, node.HasErrors);
            }

            return base.VisitUnhoistedParameter(node);
        }

        public override BoundNode VisitThisReference(BoundThisReference node)
        {
            // "topLevelMethod.ThisParameter == null" can occur in a delegate creation expression because the method group
            // in the argument can have a "this" receiver even when "this"
            // is not captured because a static method is selected.  But we do preserve
            // the method group and its receiver in the bound tree.
            // No need to capture "this" in such case.

            // TODO: Why don't we drop "this" while lowering if method is static? 
            //       Actually, considering that method group expression does not evaluate to a particular value 
            //       why do we have it in the lowered tree at all?

            return (currentMethod == topLevelMethod || topLevelMethod.ThisParameter == null ?
                node :
                FramePointer(node.Syntax, (NamedTypeSymbol)node.Type));
        }

        public override BoundNode VisitBaseReference(BoundBaseReference node)
        {
            return (currentMethod.ContainingType == topLevelMethod.ContainingType)
                ? node
                : FramePointer(node.Syntax, topLevelMethod.ContainingType); // technically, not the correct static type
        }

        public override BoundNode VisitCall(BoundCall node)
        {
            var visited = base.VisitCall(node);
            if (visited.Kind != BoundKind.Call)
            {
                return visited;
            }

            var rewritten = (BoundCall)visited;

            // Check if we need to init the 'this' proxy in a ctor call
            if (!seenBaseCall)
            {
                seenBaseCall = currentMethod == topLevelMethod && node.IsConstructorInitializer();
                if (seenBaseCall && this.thisProxyInitDeferred != null)
                {
                    // Insert the this proxy assignment after the ctor call.
                    // Create bound sequence: { ctor call, thisProxyInitDeferred }
                    return new BoundSequence(
                        syntax: node.Syntax,
                        locals: ImmutableArray<LocalSymbol>.Empty,
                        sideEffects: ImmutableArray.Create<BoundExpression>(rewritten),
                        value: this.thisProxyInitDeferred,
                        type: rewritten.Type);
                }
            }

            return rewritten;
        }

        private BoundSequence RewriteSequence(BoundSequence node, ArrayBuilder<BoundExpression> prologue, ArrayBuilder<LocalSymbol> newLocals)
        {
            RewriteLocals(node.Locals, newLocals);

            foreach (var expr in node.SideEffects)
            {
                var replacement = (BoundExpression)this.Visit(expr);
                if (replacement != null) prologue.Add(replacement);
            }

            var newValue = (BoundExpression)this.Visit(node.Value);
            var newType = this.VisitType(node.Type);

            return node.Update(newLocals.ToImmutableAndFree(), prologue.ToImmutableAndFree(), newValue, newType);
        }

        public override BoundNode VisitBlock(BoundBlock node)
        {
            LambdaFrame frame;
            // Test if this frame has captured variables and requires the introduction of a closure class.
            if (frames.TryGetValue(node, out frame))
            {
                return IntroduceFrame(node, frame, (ArrayBuilder<BoundExpression> prologue, ArrayBuilder<LocalSymbol> newLocals) =>
                    RewriteBlock(node, prologue, newLocals));
            }
            else
            {
                return RewriteBlock(node, ArrayBuilder<BoundExpression>.GetInstance(), ArrayBuilder<LocalSymbol>.GetInstance());
            }
        }

        private BoundBlock RewriteBlock(BoundBlock node, ArrayBuilder<BoundExpression> prologue, ArrayBuilder<LocalSymbol> newLocals)
        {
            RewriteLocals(node.Locals, newLocals);

            var newStatements = ArrayBuilder<BoundStatement>.GetInstance();

            if (prologue.Count > 0)
            {
                newStatements.Add(new BoundSequencePoint(null, null) { WasCompilerGenerated = true });
            }

            InsertAndFreePrologue(newStatements, prologue);

            foreach (var statement in node.Statements)
            {
                var replacement = (BoundStatement)this.Visit(statement);
                if (replacement != null)
                {
                    newStatements.Add(replacement);
                }
            }

            // TODO: we may not need to update if there was nothing to rewrite.
            return node.Update(newLocals.ToImmutableAndFree(), newStatements.ToImmutableAndFree());
        }

        public override BoundNode VisitCatchBlock(BoundCatchBlock node)
        {
            // Test if this frame has captured variables and requires the introduction of a closure class.
            LambdaFrame frame;
            if (frames.TryGetValue(node, out frame))
            {
                return IntroduceFrame(node, frame, (ArrayBuilder<BoundExpression> prologue, ArrayBuilder<LocalSymbol> newLocals) =>
                {
                    return RewriteCatch(node, prologue, newLocals);
                });
            }
            else
            {
                return RewriteCatch(node, ArrayBuilder<BoundExpression>.GetInstance(), ArrayBuilder<LocalSymbol>.GetInstance());
            }
        }

        private BoundNode RewriteCatch(BoundCatchBlock node, ArrayBuilder<BoundExpression> prologue, ArrayBuilder<LocalSymbol> newLocals)
        {
            LocalSymbol newLocal;
            if ((object)node.LocalOpt != null && TryRewriteLocal(node.LocalOpt, out newLocal))
            {
                newLocals.Add(newLocal);
            }

            LocalSymbol rewrittenCatchLocal;

            if (newLocals.Count > 0)
            {
                // If the original LocalOpt was lifted into a closure,
                // the newLocals will contain a frame reference. In this case, 
                // instead of an actual local, catch will own the frame reference.

                Debug.Assert((object)node.LocalOpt != null && newLocals.Count == 1);
                rewrittenCatchLocal = newLocals[0];
            }
            else
            {
                Debug.Assert((object)node.LocalOpt == null);
                rewrittenCatchLocal = null;
            }

            // If exception variable got lifted, IntroduceFrame will give us frame init prologue.
            // It needs to run before the exception variable is accessed.
            // To ensure that, we will make exception variable a sequence that performs prologue as its its sideeffecs.
            BoundExpression rewrittenExceptionSource = null;
            var rewrittenFilter = (BoundExpression)this.Visit(node.ExceptionFilterOpt);
            if (node.ExceptionSourceOpt != null)
            {
                rewrittenExceptionSource = (BoundExpression)Visit(node.ExceptionSourceOpt);
                if (prologue.Count > 0)
                {
                    rewrittenExceptionSource = new BoundSequence(
                        rewrittenExceptionSource.Syntax,
                        ImmutableArray.Create<LocalSymbol>(),
                        prologue.ToImmutable(),
                        rewrittenExceptionSource,
                        rewrittenExceptionSource.Type);
                }
            }
            else if (prologue.Count > 0)
            {
                Debug.Assert(rewrittenFilter != null);
                rewrittenFilter = new BoundSequence(
                    rewrittenFilter.Syntax,
                    ImmutableArray.Create<LocalSymbol>(),
                    prologue.ToImmutable(),
                    rewrittenFilter,
                    rewrittenFilter.Type);
            }

            // done with this.
            newLocals.Free();
            prologue.Free();

            // rewrite filter and body
            // NOTE: this will proxy all accesses to exception local if that got lifted.
            var exceptionTypeOpt = this.VisitType(node.ExceptionTypeOpt);
            var rewrittenBlock = (BoundBlock)this.Visit(node.Body);

            return node.Update(
                rewrittenCatchLocal, 
                rewrittenExceptionSource,
                exceptionTypeOpt,
                rewrittenFilter,
                rewrittenBlock);
        }

        public override BoundNode VisitSequence(BoundSequence node)
        {
            LambdaFrame frame;
            // Test if this frame has captured variables and requires the introduction of a closure class.
            if (frames.TryGetValue(node, out frame))
            {
                return IntroduceFrame(node, frame, (ArrayBuilder<BoundExpression> prologue, ArrayBuilder<LocalSymbol> newLocals) =>
                {
                    return RewriteSequence(node, prologue, newLocals);
                });
            }
            else
            {
                return RewriteSequence(node, ArrayBuilder<BoundExpression>.GetInstance(), ArrayBuilder<LocalSymbol>.GetInstance());
            }
        }

        public override BoundNode VisitStatementList(BoundStatementList node)
        {
            LambdaFrame frame;
            // Test if this frame has captured variables and requires the introduction of a closure class.
            // That can occur for a BoundStatementList if it is the body of a method with captured parameters.
            if (frames.TryGetValue(node, out frame))
            {
                return IntroduceFrame(node, frame, (ArrayBuilder<BoundExpression> prologue, ArrayBuilder<LocalSymbol> newLocals) =>
                {
                    var newStatements = ArrayBuilder<BoundStatement>.GetInstance();
                    InsertAndFreePrologue(newStatements, prologue);

                    foreach (var s in node.Statements)
                    {
                        newStatements.Add((BoundStatement)this.Visit(s));
                    }

                    return new BoundBlock(node.Syntax, newLocals.ToImmutableAndFree(), newStatements.ToImmutableAndFree(), node.HasErrors);
                });
            }
            else
            {
                return base.VisitStatementList(node);
            }
        }

        public override BoundNode VisitSwitchStatement(BoundSwitchStatement node)
        {
            LambdaFrame frame;
            // Test if this frame has captured variables and requires the introduction of a closure class.
            if (frames.TryGetValue(node, out frame))
            {
                return IntroduceFrame(node, frame, (ArrayBuilder<BoundExpression> prologue, ArrayBuilder<LocalSymbol> newLocals) =>
                {
                    var newStatements = ArrayBuilder<BoundStatement>.GetInstance();
                    InsertAndFreePrologue(newStatements, prologue);
                    newStatements.Add((BoundStatement)base.VisitSwitchStatement(node));

                    return new BoundBlock(node.Syntax, newLocals.ToImmutableAndFree(), newStatements.ToImmutableAndFree(), node.HasErrors);
                });
            }
            else
            {
                return base.VisitSwitchStatement(node);
            }
        }

        public override BoundNode VisitDelegateCreationExpression(BoundDelegateCreationExpression node)
        {
            // A delegate creation expression of the form "new Action( ()=>{} )" is treated exactly like
            // (Action)(()=>{})
            if (node.Argument.Kind == BoundKind.Lambda)
            {
                return RewriteLambdaConversion((BoundLambda)node.Argument);
            }
            else
            {
                return base.VisitDelegateCreationExpression(node);
            }
        }

        public override BoundNode VisitConversion(BoundConversion conversion)
        {
            if (conversion.ConversionKind == ConversionKind.AnonymousFunction)
            {
                var result = (BoundExpression)RewriteLambdaConversion((BoundLambda)conversion.Operand);
                return inExpressionLambda && conversion.ExplicitCastInCode
                    ? new BoundConversion(
                        syntax: conversion.Syntax,
                        operand: result,
                        conversionKind: conversion.ConversionKind,
                        resultKind: conversion.ResultKind,
                        isBaseConversion: false,
                        symbolOpt: null,
                        @checked: false,
                        explicitCastInCode: true,
                        isExtensionMethod: false,
                        isArrayIndex: false,
                        constantValueOpt: conversion.ConstantValueOpt,
                        type: conversion.Type)
                    : result;
            }
            else
            {
                return base.VisitConversion(conversion);
            }
        }

        private BoundNode RewriteLambdaConversion(BoundLambda node)
        {
            var wasInExpressionLambda = inExpressionLambda;
            inExpressionLambda = inExpressionLambda || node.Type.IsExpressionTree();

            if (inExpressionLambda)
            {
                var newType = VisitType(node.Type);
                var newBody = (BoundBlock)Visit(node.Body);
                node = node.Update(node.Symbol, newBody, node.Diagnostics, node.Binder, newType);
                var result0 = wasInExpressionLambda ? node : ExpressionLambdaRewriter.RewriteLambda(node, CompilationState, TypeMap, Diagnostics);
                inExpressionLambda = wasInExpressionLambda;
                return result0;
            }

            NamedTypeSymbol translatedLambdaContainer;
            BoundNode lambdaScope = null;
            LambdaFrame containerAsFrame;

            ClosureKind closureKind;
            if (analysis.lambdaScopes.TryGetValue(node.Symbol, out lambdaScope))
            {
                translatedLambdaContainer = containerAsFrame = frames[lambdaScope];
                closureKind = ClosureKind.General;
            }
            else if (analysis.capturedVariablesByLambda[node.Symbol].Count == 0)
            {
                translatedLambdaContainer = containerAsFrame = GetStaticFrame(Diagnostics, node);
                closureKind = ClosureKind.Static;
            }
            else
            {
                containerAsFrame = null;
                translatedLambdaContainer = topLevelMethod.ContainingType;
                closureKind = ClosureKind.ThisOnly;
            }

            // Move the body of the lambda to a freshly generated synthetic method on its frame.
            int lambdaOrdinal = lambdaOrdinalDispenser++;
            var synthesizedMethod = new SynthesizedLambdaMethod(slotAllocatorOpt, CompilationState, translatedLambdaContainer, closureKind, topLevelMethod, topLevelMethodOrdinal, node, lambdaOrdinal);
            if (CompilationState.Emitting)
            {
                CompilationState.ModuleBuilderOpt.AddSynthesizedDefinition(translatedLambdaContainer, synthesizedMethod);
            }

            foreach (var parameter in node.Symbol.Parameters)
            {
                parameterMap.Add(parameter, synthesizedMethod.Parameters[parameter.Ordinal]);
            }

            // rewrite the lambda body as the generated method's body
            var oldMethod = currentMethod;
            var oldFrameThis = currentFrameThis;
            var oldTypeParameters = currentTypeParameters;
            var oldInnermostFramePointer = innermostFramePointer;
            var oldTypeMap = currentLambdaBodyTypeMap;
            var oldAddedStatements = addedStatements;
            var oldAddedLocals = addedLocals;
            addedStatements = null;
            addedLocals = null;

            // switch to the generated method

            currentMethod = synthesizedMethod;
            if (closureKind == ClosureKind.Static)
            {
                // no link from a static lambda to its container
                innermostFramePointer = currentFrameThis = null;
            }
            else
            {
                currentFrameThis = synthesizedMethod.ThisParameter;
                innermostFramePointer = null;
                framePointers.TryGetValue(translatedLambdaContainer, out innermostFramePointer);
            }

            if ((object)containerAsFrame != null)
            {
                currentTypeParameters = translatedLambdaContainer.TypeParameters;
                currentLambdaBodyTypeMap = ((LambdaFrame)translatedLambdaContainer).TypeMap;
            }
            else
            {
                currentTypeParameters = synthesizedMethod.TypeParameters;
                currentLambdaBodyTypeMap = new TypeMap(topLevelMethod.TypeParameters, currentTypeParameters);
            }

            var body = AddStatementsIfNeeded((BoundStatement)VisitBlock(node.Body));
            CheckLocalsDefined(body);
            CompilationState.AddSynthesizedMethod(synthesizedMethod, body);

            // return to the old method

            currentMethod = oldMethod;
            currentFrameThis = oldFrameThis;
            currentTypeParameters = oldTypeParameters;
            innermostFramePointer = oldInnermostFramePointer;
            currentLambdaBodyTypeMap = oldTypeMap;
            addedLocals = oldAddedLocals;
            addedStatements = oldAddedStatements;

            // Rewrite the lambda expression (and the enclosing anonymous method conversion) as a delegate creation expression
            NamedTypeSymbol constructedFrame = (object)containerAsFrame != null ? 
                translatedLambdaContainer.ConstructIfGeneric(StaticCast<TypeSymbol>.From(currentTypeParameters)) : 
                translatedLambdaContainer;

            // for instance lambdas, receiver is the frame
            // for static lambdas, get the singleton receiver 
            BoundExpression receiver;
            if (closureKind != ClosureKind.Static)
            {
                receiver = FrameOfType(node.Syntax, constructedFrame);
            }
            else
            {
                var field = containerAsFrame.SingletonCache.AsMember(constructedFrame);
                receiver = new BoundFieldAccess(node.Syntax, null, field, constantValueOpt: null);
            }

            MethodSymbol referencedMethod = synthesizedMethod.AsMember(constructedFrame);
            if (referencedMethod.IsGenericMethod)
            {
                referencedMethod = referencedMethod.Construct(StaticCast<TypeSymbol>.From(currentTypeParameters));
            }

            TypeSymbol type = this.VisitType(node.Type);

            // static lambdas are emitted as instance methods on a singleton receiver
            // delegates invoke dispatch is optimized for instance delegates so 
            // it is preferrable to emit lambdas as instance methods even when lambdas 
            // do not capture anything
            BoundExpression result = new BoundDelegateCreationExpression(
                node.Syntax,
                receiver,
                referencedMethod,
                isExtensionMethod: false,
                type: type);

            // if the block containing the lambda is not the innermost block,
            // or the lambda is static, then the lambda object should be cached in its frame.
            // NOTE: we are not caching static lambdas in static ctors - cannot reuse such cache.
            var shouldCacheForStaticMethod = closureKind == ClosureKind.Static &&
                currentMethod.MethodKind != MethodKind.StaticConstructor &&
                !referencedMethod.IsGenericMethod;

            // NOTE: We require "lambdaScope != null". 
            //       We do not want to introduce a field into an actual user's class (not a synthetic frame).
            var shouldCacheInLoop = lambdaScope != null &&
                lambdaScope != analysis.scopeParent[node.Body] &&
                InLoopOrLambda(node.Syntax, lambdaScope.Syntax);

            if (shouldCacheForStaticMethod || shouldCacheInLoop)
            {
                // replace the expression "new Delegate(frame.M)" with "frame.cache ?? (frame.cache = new Delegate(frame.M));
                var F = new SyntheticBoundNodeFactory(currentMethod, node.Syntax, CompilationState, Diagnostics);
                try
                {
                    BoundExpression cacheVariable;
                    if (shouldCacheForStaticMethod || shouldCacheInLoop && (object)containerAsFrame != null)
                    {
                        var cacheVariableType = containerAsFrame?
                            .TypeMap.SubstituteType(type) ?? 
                            translatedLambdaContainer;

                        // If we are generating the field into a display class created exclusively for the lambda the lambdaOrdinal itself is unique already, 
                        // no need to include the top-level method ordinal in the field name.
                        
                        // TODO: slot allocator
                        var cacheVariableName = GeneratedNames.MakeLambdaCacheFieldName(
                            (closureKind == ClosureKind.General) ? -1 : topLevelMethodOrdinal,
                            CompilationState.ModuleBuilderOpt.CurrentGenerationOrdinal,
                            lambdaOrdinal);

                        var cacheField = new SynthesizedLambdaCacheFieldSymbol(translatedLambdaContainer, cacheVariableType, cacheVariableName, topLevelMethod, isReadOnly: false, isStatic: closureKind == ClosureKind.Static);
                        CompilationState.ModuleBuilderOpt.AddSynthesizedDefinition(translatedLambdaContainer, cacheField);
                        cacheVariable = F.Field(receiver, cacheField.AsMember(constructedFrame)); //NOTE: the field was added to the unconstructed frame type.
                    }
                    else
                    {
                        // the lambda captures at most the "this" of the enclosing method.  We cache its delegate in a local variable.
                        var cacheLocal = F.SynthesizedLocal(type, kind: SynthesizedLocalKind.CachedAnonymousMethodDelegate);
                        if (addedLocals == null) addedLocals = ArrayBuilder<LocalSymbol>.GetInstance();
                        addedLocals.Add(cacheLocal);
                        if (addedStatements == null) addedStatements = ArrayBuilder<BoundStatement>.GetInstance();
                        cacheVariable = F.Local(cacheLocal);
                        addedStatements.Add(F.Assignment(cacheVariable, F.Null(type)));
                    }

                    result = F.Coalesce(cacheVariable, F.AssignmentExpression(cacheVariable, result));
                }
                catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
                {
                    Diagnostics.Add(ex.Diagnostic);
                    return new BoundBadExpression(F.Syntax, LookupResultKind.Empty, ImmutableArray<Symbol>.Empty, ImmutableArray.Create<BoundNode>(node), node.Type);
                }
            }

            return result;
        }

        // This helper checks syntactically whether there is a loop or lambda expression
        // between given lambda syntax and the syntax that corresponds to its closure.
        // we use this heuristic as a hint that the lambda delegate may be created 
        // multiple times with same closure.
        // In such cases it makes sense to cache the delegate.
        //
        // Examples:
        //            int x = 123;
        //            for (int i = 1; i< 10; i++)
        //            {
        //                if (i< 2)
        //                {
        //                    arr[i].Execute(arg => arg + x);  // delegate should be cached
        //                }
        //            }

        //            for (int i = 1; i< 10; i++)
        //            {
        //                var val = i;
        //                if (i< 2)
        //                {
        //                    int y = i + i;
        //                    System.Console.WriteLine(y);
        //                    arr[i].Execute(arg => arg + val);  // delegate should NOT be cached (closure created inside the loop)
        //                }
        //            }
        //
        private static bool InLoopOrLambda(SyntaxNode lambdaSyntax, SyntaxNode scopeSyntax)
        {
            var curSyntax = lambdaSyntax.Parent;
            while (curSyntax != null && curSyntax != scopeSyntax)
            {
                switch (curSyntax.Kind())
                {
                    case SyntaxKind.ForStatement:
                    case SyntaxKind.ForEachStatement:
                    case SyntaxKind.WhileStatement:
                    case SyntaxKind.DoStatement:
                    case SyntaxKind.SimpleLambdaExpression:
                    case SyntaxKind.ParenthesizedLambdaExpression:
                        return true;
                }

                curSyntax = curSyntax.Parent;
            }

            return false;
        }

        public override BoundNode VisitLambda(BoundLambda node)
        {
            // these nodes have been handled in the context of the enclosing anonymous method conversion.
            throw ExceptionUtilities.Unreachable;
        }

        #endregion

#if CHECK_LOCALS
        /// <summary>
        /// Ensure that local variables are always in scope where used in bound trees
        /// </summary>
        /// <param name="node"></param>
        static partial void CheckLocalsDefined(BoundNode node)
        {
            LocalsDefinedScanner.INSTANCE.Visit(node);
        }

        class LocalsDefinedScanner : BoundTreeWalker
        {
            internal static LocalsDefinedScanner INSTANCE = new LocalsDefinedScanner();

            HashSet<Symbol> localsDefined = new HashSet<Symbol>();

            public override BoundNode VisitLocal(BoundLocal node)
            {
                Debug.Assert(node.LocalSymbol.IsConst || localsDefined.Contains(node.LocalSymbol));
                return base.VisitLocal(node);
            }

            public override BoundNode VisitSequence(BoundSequence node)
            {
                try
                {
                    if (!node.Locals.IsNullOrEmpty)
                        foreach (var l in node.Locals)
                            localsDefined.Add(l);
                    return base.VisitSequence(node);
                }
                finally
                {
                    if (!node.Locals.IsNullOrEmpty)
                        foreach (var l in node.Locals)
                            localsDefined.Remove(l);
                }
            }

            public override BoundNode VisitCatchBlock(BoundCatchBlock node)
            {
                try
                {
                    if ((object)node.LocalOpt != null) localsDefined.Add(node.LocalOpt);
                    return base.VisitCatchBlock(node);
                }
                finally
                {
                    if ((object)node.LocalOpt != null) localsDefined.Remove(node.LocalOpt);
                }
            }

            public override BoundNode VisitSwitchStatement(BoundSwitchStatement node)
            {
                try
                {
                    if (!node.LocalsOpt.IsNullOrEmpty)
                        foreach (var l in node.LocalsOpt)
                            localsDefined.Add(l);
                    return base.VisitSwitchStatement(node);
                }
                finally
                {
                    if (!node.LocalsOpt.IsNullOrEmpty)
                        foreach (var l in node.LocalsOpt)
                            localsDefined.Remove(l);
                }
            }

            public override BoundNode VisitBlock(BoundBlock node)
            {
                try
                {
                    if (!node.LocalsOpt.IsNullOrEmpty)
                        foreach (var l in node.LocalsOpt)
                            localsDefined.Add(l);
                    return base.VisitBlock(node);
                }
                finally
                {
                    if (!node.LocalsOpt.IsNullOrEmpty)
                        foreach (var l in node.LocalsOpt)
                            localsDefined.Remove(l);
                }
            }
        }
#endif

    }
}

﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitConditionalAccess(BoundConditionalAccess node)
        {
            return RewriteConditionalAccess(node, used: true);
        }

        // null when currently enclosing conditional access node
        // is not supposed to be lowered.
        private BoundExpression currentConditionalAccessTarget = null;

        private enum ConditionalAccessLoweringKind
        {
            None,
            NoCapture,
            CaptureReceiverByVal,
            CaptureReceiverByRef,
            DuplicateCode
        }

        // in simple cases could be left unlowered.
        // IL gen can generate more compact code for unlowered conditional accesses 
        // by utilizing stack dup/pop instructions 
        internal BoundExpression RewriteConditionalAccess(BoundConditionalAccess node, bool used, BoundExpression rewrittenWhenNull = null)
        {
            Debug.Assert(!this.inExpressionLambda);

            var loweredReceiver = this.VisitExpression(node.Receiver);
            var receiverType = loweredReceiver.Type;

            //TODO: if AccessExpression does not contain awaits, the node could be left unlowered (saves a temp),
            //      but there seem to be no way of knowing that without walking AccessExpression.
            //      For now we will just check that we are in an async method, but it would be nice
            //      to have something more precise.
            var isAsync = this.factory.CurrentMethod.IsAsync;

            ConditionalAccessLoweringKind loweringKind;
            // CONSIDER: If we knew that loweredReceiver is not a captured local
            //       we could pass "false" for localsMayBeAssignedOrCaptured
            //       otherwise not capturing receiver into a temp
            //       could introduce additional races into the code if receiver is captured
            //       into a closure and is modified between null check of the receiver 
            //       and the actual access.
            //
            //       Nullable is special since we are not going to read any part of it twice
            //       we will read "HasValue" and then, conditionally will read "ValueOrDefault"
            //       that is no different than just reading both values unconditionally.
            //       As a result in the case of nullable, not reading captured local through a temp 
            //       does not introduce any additional races so it is irrelevant whether 
            //       the local is captured or not.
            var localsMayBeAssignedOrCaptured = !receiverType.IsNullableType();
            var needTemp = IntroducingReadCanBeObservable(loweredReceiver, localsMayBeAssignedOrCaptured);

            if (!isAsync && !node.AccessExpression.Type.IsDynamic() && rewrittenWhenNull == null &&
                (receiverType.IsReferenceType || receiverType.IsTypeParameter() && needTemp))
            {
                // trivial cases can be handled more efficiently in IL gen
                loweringKind = ConditionalAccessLoweringKind.None;
            }
            else if(needTemp)
            {
                if (receiverType.IsReferenceType || receiverType.IsNullableType())
                {
                    loweringKind = ConditionalAccessLoweringKind.CaptureReceiverByVal;
                }
                else
                {
                    loweringKind = isAsync ?
                        ConditionalAccessLoweringKind.DuplicateCode :
                        ConditionalAccessLoweringKind.CaptureReceiverByRef;
                }
            }
            else
            {
                // locals do not need to be captured
                loweringKind = ConditionalAccessLoweringKind.NoCapture;
            }


            var previousConditionalAccesTarget = currentConditionalAccessTarget;
            LocalSymbol temp = null;
            BoundExpression unconditionalAccess = null;

            switch (loweringKind)
            {
                case ConditionalAccessLoweringKind.None:
                    currentConditionalAccessTarget = null;
                    break;

                case ConditionalAccessLoweringKind.NoCapture:
                    currentConditionalAccessTarget = loweredReceiver;
                    break;

                case ConditionalAccessLoweringKind.DuplicateCode:
                    currentConditionalAccessTarget = loweredReceiver;
                    unconditionalAccess = used? 
                        this.VisitExpression(node.AccessExpression) :
                        this.VisitUnusedExpression(node.AccessExpression);

                    goto case ConditionalAccessLoweringKind.CaptureReceiverByVal;

                case ConditionalAccessLoweringKind.CaptureReceiverByVal:
                    temp = factory.SynthesizedLocal(receiverType);
                    currentConditionalAccessTarget = factory.Local(temp);
                    break;

                case ConditionalAccessLoweringKind.CaptureReceiverByRef:
                    temp = factory.SynthesizedLocal(receiverType, refKind: RefKind.Ref);
                    currentConditionalAccessTarget = factory.Local(temp);
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(loweringKind);
            }

            BoundExpression loweredAccessExpression = used ?
                        this.VisitExpression(node.AccessExpression) :
                        this.VisitUnusedExpression(node.AccessExpression);

            currentConditionalAccessTarget = previousConditionalAccesTarget;

            TypeSymbol type = this.VisitType(node.Type);

            TypeSymbol nodeType = node.Type;
            TypeSymbol accessExpressionType = loweredAccessExpression.Type;

            if (accessExpressionType.SpecialType == SpecialType.System_Void)
            {
                type = nodeType = accessExpressionType;
            }

            if (accessExpressionType != nodeType && nodeType.IsNullableType())
            {
                Debug.Assert(accessExpressionType == nodeType.GetNullableUnderlyingType());
                loweredAccessExpression = factory.New((NamedTypeSymbol)nodeType, loweredAccessExpression);

                if (unconditionalAccess != null)
                {
                    unconditionalAccess = factory.New((NamedTypeSymbol)nodeType, unconditionalAccess);
                }
            }
            else
            {
                Debug.Assert(accessExpressionType == nodeType ||
                    (nodeType.SpecialType == SpecialType.System_Void && !used));
            }

            BoundExpression result;
                     var objectType = compilation.GetSpecialType(SpecialType.System_Object);

            rewrittenWhenNull = rewrittenWhenNull ?? factory.Default(nodeType);

            switch (loweringKind)
            {
                case ConditionalAccessLoweringKind.None:
                    Debug.Assert(!receiverType.IsValueType);
                    result = node.Update(loweredReceiver, loweredAccessExpression, type);
                    break;

                case ConditionalAccessLoweringKind.CaptureReceiverByVal:
                    // capture the receiver into a temp
                    loweredReceiver = factory.Sequence(
                                            factory.AssignmentExpression(factory.Local(temp), loweredReceiver),
                                            factory.Local(temp));

                    goto case ConditionalAccessLoweringKind.NoCapture;

                case ConditionalAccessLoweringKind.NoCapture:
                    {

                        // (object)r != null ? access : default(T)
                        var condition = receiverType.IsNullableType() ?
                            MakeOptimizedHasValue(loweredReceiver.Syntax, loweredReceiver) :
                            factory.ObjectNotEqual(
                                factory.Convert(objectType, loweredReceiver),
                                factory.Null(objectType));

                        var consequence = loweredAccessExpression;

                        result = RewriteConditionalOperator(node.Syntax,
                            condition,
                            consequence,
                            rewrittenWhenNull,
                            null,
                            nodeType);

                        if (temp != null)
                        {
                            result = factory.Sequence(temp, result);
                        }
                    }
                    break;

                case ConditionalAccessLoweringKind.CaptureReceiverByRef:
                    // {ref T r; T v; 
                    //    r = ref receiver; 
                    //    (isClass && { v = r; r = ref v; v == null } ) ? 
                    //                                          null;
                    //                                          r.Foo()}

                    {
                        var v = factory.SynthesizedLocal(receiverType);

                        BoundExpression captureRef = factory.AssignmentExpression(factory.Local(temp), loweredReceiver, refKind: RefKind.Ref);
                        BoundExpression isNull = factory.LogicalAnd(
                                                IsClass(receiverType, objectType),
                                                factory.Sequence(
                                                    factory.AssignmentExpression(factory.Local(v), factory.Local(temp)),
                                                    factory.AssignmentExpression(factory.Local(temp), factory.Local(v), RefKind.Ref),
                                                    factory.ObjectEqual(factory.Convert(objectType, factory.Local(v)), factory.Null(objectType)))
                                                );

                        result = RewriteConditionalOperator(node.Syntax,
                           isNull,
                           rewrittenWhenNull,
                           loweredAccessExpression,
                           null,
                           nodeType);

                        result = factory.Sequence(
                                ImmutableArray.Create(temp, v),
                                captureRef,
                                result
                            );

                    }
                    break;

                case ConditionalAccessLoweringKind.DuplicateCode:
                    {
                        Debug.Assert(!receiverType.IsNullableType());

                        // if we have a class, do regular conditional access via a val temp
                        loweredReceiver = factory.AssignmentExpression(factory.Local(temp), loweredReceiver);
                        BoundExpression ifClass = RewriteConditionalOperator(node.Syntax,
                            factory.ObjectNotEqual(
                                                    factory.Convert(objectType, loweredReceiver),
                                                    factory.Null(objectType)),
                            loweredAccessExpression,
                            rewrittenWhenNull,
                            null,
                            nodeType);

                        if (temp != null)
                        {
                            ifClass = factory.Sequence(temp, ifClass);
                        }

                        // if we have a struct, then just access unconditionally
                        BoundExpression ifStruct = unconditionalAccess;

                        // (object)(default(T)) != null ? ifStruct: ifClass
                        result = RewriteConditionalOperator(node.Syntax,
                            IsClass(receiverType, objectType),
                            ifClass,
                            ifStruct,
                            null,
                            nodeType);
                    }
                    break;

                default:
                    throw ExceptionUtilities.Unreachable;
            }

            return result;
        }

        private BoundBinaryOperator IsClass(TypeSymbol receiverType, NamedTypeSymbol objectType)
        {
            return factory.ObjectEqual(
                                factory.Convert(objectType, factory.Default(receiverType)),
                                factory.Null(objectType));
        }

        public override BoundNode VisitConditionalReceiver(BoundConditionalReceiver node)
        {
            if (currentConditionalAccessTarget == null)
            {
                return node;
            }

            var newtarget = currentConditionalAccessTarget;
            if (newtarget.Type.IsNullableType())
            {
                newtarget = MakeOptimizedGetValueOrDefault(node.Syntax, newtarget);
            }

            return newtarget;
        }
    }
}

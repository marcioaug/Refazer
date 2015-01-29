﻿using System.Collections.Generic;
using LocationCodeRefactoring.Spg.LocationRefactor.Operator.Map;
using Spg.LocationRefactor.TextRegion;

namespace Spg.LocationRefactor.Operator
{
    /// <summary>
    /// Statement map
    /// </summary>
    public class StatementMap : MapBase
    {
        /// <summary>
        /// List of regions
        /// </summary>
        private List<TRegion> list;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="list"></param>
        public StatementMap(List<TRegion> list)
        {
            this.list = list;
        }
        public override string ToString()
        {
            return "StatementMap(λSyntaxNode: Pair(Pos(S, p1), Pos(S, p2)), S)"
                + "\n\tp1 = " + ((Pair)ScalarExpression.Ioperator).expression.p1.ToString()
                + "\n\tp2 = " + ((Pair)ScalarExpression.Ioperator).expression.p2.ToString()
                + "\n\tS=" + SequenceExpression.ToString();
        }
    }
}

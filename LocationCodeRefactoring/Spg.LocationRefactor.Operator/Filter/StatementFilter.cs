﻿using System;
using System.Collections.Generic;
using LocationCodeRefactoring.Spg.LocationRefactor.Learn.Filter;
using LocationCodeRefactoring.Spg.LocationRefactor.Location;
using Spg.LocationRefactor.Operator.Filter;
using Microsoft.CodeAnalysis;
using Spg.LocationRefactor.Learn;
using Spg.LocationRefactor.TextRegion;

namespace Spg.LocationRefactor.Operator
{
    /// <summary>
    /// Statement filter
    /// </summary>
    public class StatementFilter : FilterBase
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="list">Region list</param>
        public StatementFilter(List<TRegion> list): base(list)
        {
            if (list == null) { throw new Exception("List cannot be null"); }
        }

        /// <summary>
        /// Filter learner
        /// </summary>
        /// <returns>Filter learner</returns>
        protected override FilterLearnerBase GetFilterLearner(List<TRegion> list)
        {
            return new StatementFilterLearner(list);
        }

        ///// <summary>
        ///// Syntax nodes
        ///// </summary>
        ///// <param name="sourceCode">Source code</param>
        ///// <returns>Syntax nodes</returns>
        //protected override IEnumerable<SyntaxNode> SyntaxNodes(string sourceCode)
        //{
        //    return RegionManager.SyntaxNodesForFiltering(sourceCode, List);
        //}
    }
}

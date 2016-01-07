using System;
using System.Collections.Generic;
using Spg.ExampleRefactoring.Synthesis;
using Spg.LocationRefactor.Learn.Filter;
using Spg.LocationRefactor.Location;
using Spg.LocationRefactor.Operator.Map;
using Microsoft.CodeAnalysis;
using Spg.LocationRefactor.Node;
using Spg.LocationRefactor.Predicate;
using Spg.LocationRefactor.TextRegion;

namespace Spg.LocationRefactor.Learn.Map
{
    /// <summary>
    /// Statement map learner
    /// </summary>
    public class NodeMapLearner : MapLearnerBase
    {
        Decomposer strategy = Decomposer.GetInstance();
        /// <summary>
        /// Filter
        /// </summary>
        /// <returns>Filter</returns>
        protected override FilterLearnerBase GetFilter(List<TRegion> list)
        {
            return new NodeFilterLearner(list);
        }

        /// <summary>
        /// Map
        /// </summary>
        /// <returns>Map</returns>
        protected override MapBase GetMap(List<TRegion> list)
        {
            return new NodeMap();
        }

        /// <summary>
        /// Predicate
        /// </summary>
        /// <returns>Predicate</returns>
        protected override IPredicate GetPredicate()
        {
            return new Contains();
        }

        /// <summary>
        /// Decompose
        /// </summary>
        /// <param name="list">Examples</param>
        /// <returns>Examples</returns>
        public override List<Tuple<ListNode, ListNode>> Decompose(List<TRegion> list)
        {
            return strategy.Decompose(list);
        }

        /// <summary>
        /// Syntax nodes
        /// </summary>
        /// <param name="sourceCode">Source code</param>
        /// <returns>Syntax nodes</returns>
        public override List<SyntaxNode> SyntaxNodes(string sourceCode, List<TRegion> list)
        {
            return RegionManager.GetInstance().SyntaxNodes(sourceCode, list);
        }
    }
}



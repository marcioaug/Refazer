using System;
using System.Collections.Generic;
using System.Linq;
using Spg.ExampleRefactoring.Expression;
using Spg.ExampleRefactoring.Synthesis;
using Spg.LocationRefactor.Controller;
using Spg.LocationRefactor.Learn.Filter;
using Spg.LocationRefactor.Operator;
using Spg.LocationRefactor.Operator.Map;
using Spg.LocationRefactor.Predicate;
using Spg.LocationRefactor.Program;
using Spg.LocationRefactor.TextRegion;

namespace Spg.LocationRefactor.Learn
{
    /// <summary>
    /// Merge Learner
    /// </summary>
    public class MergeLearner: ILearn
    {
        /// <summary>
        /// Learn merge
        /// </summary>
        /// <param name="examples">Examples</param>
        /// <returns>Merge operators</returns>
        public List<Prog> Learn(List<Tuple<ListNode, ListNode>> examples)
        {
            //List<Prog> programs = new List<Prog>();
            //List<Tuple<ListNode, ListNode>> Q = MapBase.Decompose(examples);

            //ASTProgram P = new ASTProgram();
            //SynthesizedProgram h = P.GenerateStringProgram(examples).Single();

            //List<IExpression> X = new List<IExpression>();

            //IPredicate pred = GetPredicate();
            //EditorController contoller = EditorController.GetInstance();
            //List<TRegion> list = contoller.SelectedLocations;
            //FilterLearnerBase S = GetFilter(list);
            //S.Predicate = pred;

            //List<Prog> predicates = S.Learn(examples);
            //foreach (IExpression e in X)
            //{
            //    foreach (Prog predicate in predicates)
            //    {
            //        Prog scalar = new Prog();
            //        Pair pair = new Pair();
            //        pair.Expression = (SubStr)e;

            //        scalar.Ioperator = pair;

                    
            //        MapBase map = GetMap(list);
            //        map.ScalarExpression = scalar;
            //        map.SequenceExpression = predicate;
            //        Prog prog = new Prog();
            //        prog.Ioperator = map;
            //        programs.Add(prog);
            //    }
            //}
            //return programs;
            throw new NotImplementedException("");
        }

        /// <summary>
        /// Learn
        /// </summary>
        /// <param name="positiveExamples">Positive examples</param>
        /// <param name="negativeExamples">Negative examples</param>
        /// <returns>Location programs</returns>
        public List<Prog> Learn(List<Tuple<ListNode, ListNode>> positiveExamples, List<Tuple<ListNode, ListNode>> negativeExamples)
        {
            //List<Prog> programs = new List<Prog>();
            //List<Tuple<ListNode, ListNode>> Q = MapBase.Decompose(positiveExamples);

            //ASTProgram P = new ASTProgram();
            //SynthesizedProgram h = P.GenerateStringProgram(positiveExamples).Single();

            //List<IExpression> X = new List<IExpression>();

            //IPredicate pred = GetPredicate();
            //EditorController contoller = EditorController.GetInstance();
            //List<TRegion> list = contoller.SelectedLocations;
            //FilterLearnerBase S = GetFilter(list);
            //S.Predicate = pred;

            //List<Prog> predicates = S.Learn(positiveExamples);
            //foreach (IExpression e in X)
            //{
            //    foreach (Prog predicate in predicates)
            //    {
            //        Prog scalar = new Prog();
            //        Pair pair = new Pair();
            //        pair.Expression = (SubStr)e;

            //        scalar.Ioperator = pair;


            //        MapBase map = GetMap(list);
            //        map.ScalarExpression = scalar;
            //        map.SequenceExpression = predicate;
            //        Prog prog = new Prog();
            //        prog.Ioperator = map;
            //        programs.Add(prog);
            //    }
            //}
            //return programs;
            throw new NotImplementedException("");
        }

        /// <summary>
        /// Predicate
        /// </summary>
        /// <returns>Predicate</returns>
        protected IPredicate GetPredicate() {
            return new Contains();
        }

        /// <summary>
        /// Map
        /// </summary>
        /// <returns>Map</returns>
        protected MapBase GetMap(List<TRegion> list) {
            return new StatementMap(list);
        }

        /// <summary>
        /// Filter
        /// </summary>
        /// <returns>Filter</returns>
        protected FilterLearnerBase GetFilter(List<TRegion> list) {
            return new StatementFilterLearner(list);
        }
    }
}





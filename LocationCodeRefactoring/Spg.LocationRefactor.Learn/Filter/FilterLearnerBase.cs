﻿using System;
using System.Collections.Generic;
using DiGraph;
using ExampleRefactoring.Spg.ExampleRefactoring.Synthesis;
using Spg.ExampleRefactoring.Digraph;
using Spg.ExampleRefactoring.Expression;
using Spg.ExampleRefactoring.Position;
using Spg.ExampleRefactoring.Setting;
using Spg.ExampleRefactoring.Synthesis;
using Spg.ExampleRefactoring.Tok;
using Spg.LocationRefactor.Operator;
using Spg.LocationRefactor.Predicate;
using Spg.LocationRefactor.Program;
using Spg.LocationRefactor.TextRegion;
using Spg.LocationRefactoring.Tok;

namespace Spg.LocationRefactor.Learn
{
    /// <summary>
    /// Represents a filter operator
    /// </summary>
    public abstract class FilterLearnerBase : ILearn
    {
        /// <summary>
        /// Store the filters calculated
        /// </summary>
        private Dictionary<TokenSeq, Boolean> calculated;

        /// <summary>
        /// Predicate of the filter
        /// </summary>
        public IPredicate predicate { get; set; }

        public List<TRegion> list { get; set; }

        public FilterLearnerBase(List<TRegion> list) {
            this.list = list;
            calculated = new Dictionary<TokenSeq, Boolean>();
        }

        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="predicate">Predicate</param>
        public FilterLearnerBase(Predicate.IPredicate predicate, List<TRegion> list)
        {
            this.list = list;
            calculated = new Dictionary<TokenSeq, Boolean>();
            this.predicate = predicate;
        }

        /// <summary>
        /// Learn a list filters
        /// </summary>
        /// <param name="examples">Examples</param>
        /// <returns>List of filters</returns>
        public List<Prog> Learn(List<Tuple<ListNode, ListNode>> examples)
        {
            List<Tuple<ListNode, ListNode>> S = MapBase.Decompose(examples);

            List<Tuple<Tuple<ListNode, ListNode>, Boolean>> QLine = new List<Tuple<Tuple<ListNode, ListNode>, Boolean>>();
            foreach (Tuple<ListNode, ListNode> tuple in S)
            {
                Tuple<Tuple<ListNode, ListNode>, Boolean> t = Tuple.Create(tuple, true);
                QLine.Add(t);
            }
            List<Prog> programs = new List<Prog>();

            List<Predicate.IPredicate> predicates = BooleanLearning(QLine);
            /*var items = from pair in predicates
                        orderby Order(pair) descending, pair.Regex().Count() ascending
                        select pair;*/

            foreach (Predicate.IPredicate ipredicate in predicates)
            {
                Prog prog = new Prog();
                FilterBase filter = GetFilter(list);
                filter.predicate = ipredicate;

                prog.ioperator = filter;
                programs.Add(prog);
            }
            return programs;
        }

        private object Order(Predicate.IPredicate pair)
        {
            int count = 0;
            foreach(Token t in pair.Regex())
            {
                if(t is DymToken)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Learn boolean operators
        /// </summary>
        /// <param name="examples">Examples</param>
        /// <returns>List of boolean operators</returns>
        public List<Predicate.IPredicate> BooleanLearning(List<Tuple<Tuple<ListNode, ListNode>, Boolean>> examples)
        {
            List<Predicate.IPredicate> predicates = new List<Predicate.IPredicate>();

            /*List<Tuple<ListNode, ListNode>> lnodes = new List<Tuple<ListNode, ListNode>>();
            foreach (Tuple<Tuple<ListNode, ListNode>, Boolean> t in examples) {
                Tuple<ListNode, ListNode> tp = Tuple.Create(t.Item1.Item2, t.Item1.Item2);
                lnodes.Add(tp);
            }

            SynthesizerSetting setting = new SynthesizerSetting();
            setting.dynamicTokens = true;
            setting.deviation = lnodes[0].Item2.Length();
            ASTProgram program = new ASTProgram(setting, lnodes);*/
            //program.boundary = BoundaryManager.GetInstance().boundary;

            List<Tuple<ListNode, Boolean>> boolExamples = new List<Tuple<ListNode, Boolean>>();

            Dag T = null;
            foreach (Tuple<Tuple<ListNode, ListNode>, Boolean> e in examples)
            {
                boolExamples.Add(Tuple.Create(e.Item1.Item1, e.Item2));
            }

            T = CreateDag(examples);

            //T = program.GenerateString2(examples[0].Item1.Item2, examples[0].Item1.Item2);
            foreach (KeyValuePair<Tuple<Vertex, Vertex>, List<IExpression>> entry in T.mapping)
            {
                List<IExpression> expressions = entry.Value;
                foreach (IExpression exp in expressions)
                {
                    if (exp is SubStr)
                    {
                        IPosition p1 = ((SubStr)exp).p1;
                        IPosition p2 = ((SubStr)exp).p2;
                        List<IPosition> positions = new List<IPosition>();
                        positions.Add(p1);
                        positions.Add(p2);
                        foreach (IPosition position in positions)
                        {
                            if (position is Pos)
                            {
                                Pos positioncopy = (Pos)position;

                                TokenSeq r1 = positioncopy.r1;
                                TokenSeq r2 = positioncopy.r2;
                                TokenSeq merge = ASTProgram.ConcatenateRegularExpression(r1, r2);
                                TokenSeq regex = merge;

                                Boolean b = Indicator(predicate, boolExamples, regex);
                                if (b)
                                {
                                    Predicate.IPredicate clone = PredicateFactory.Create((Predicate.IPredicate)predicate);
                                    clone.r1 = r1;
                                    clone.r2 = r2;

                                    predicates.Add(clone);
                                }
                            }
                        }
                    }
                }
            }
            return predicates;
        }

        private Dag CreateDag(List<Tuple<Tuple<ListNode, ListNode>, bool>> examples)
        {
            List<Tuple<ListNode, ListNode>> exs = new List<Tuple<ListNode, ListNode>>();
            foreach (Tuple<Tuple<ListNode, ListNode>, Boolean> e in examples)
            {
                exs.Add(e.Item1);
            }

            List<Dag> dags = new List<Dag>();
            SynthesizerSetting setting = new SynthesizerSetting();
            setting.dynamicTokens = true;
            setting.deviation = 2;
            ASTProgram program = new ASTProgram(setting, exs);
            foreach (Tuple<ListNode, ListNode> e in exs)
            {
                List<int> boundary = new List<int>();
                for(int i = 0; i <= e.Item2.Length(); i++)
                {
                    boundary.Add(i);
                }
                Dag dag = program.GenerateStringBoundary(e.Item2, e.Item2, boundary);
                dags.Add(dag);
            }

            IntersectManager IntManager = new IntersectManager();
            Dag T = IntManager.Intersect(dags);

            ExpressionManager expmanager = new ExpressionManager();
            expmanager.FilterExpressions(T, exs);

            ASTProgram.Clear(T);

            return T;
        }

        /// <summary>
        /// True if regex match the input
        /// </summary>
        /// <param name="predicate">Predicate</param>
        /// <param name="examples">Examples</param>
        /// <param name="regex">Regular expression</param>
        /// <returns>True if regex match the input</returns>
        public Boolean Indicator(Predicate.IPredicate predicate, List<Tuple<ListNode, Boolean>> examples, TokenSeq regex)
        {
            Boolean b = true;
            Boolean entry;
            if (!calculated.TryGetValue(regex, out entry))
            {
                foreach (Tuple<ListNode, Boolean> example in examples)
                {
                    Boolean b02 = predicate.Evaluate(example.Item1, regex);
                    if (!(b02 == example.Item2))
                    {
                        calculated[regex] = false;
                        return false;
                    }
                }
                calculated[regex] = b;
                entry = b;
            }
            return entry;
        }

        /// <summary>
        /// True if the regex match the input
        /// </summary>
        /// <param name="predicate">Predicate</param>
        /// <param name="input">Input</param>
        /// <param name="regex">Regular expression</param>
        /// <returns>True if the regex match the input</returns>
        public Boolean Indicator(Predicate.IPredicate predicate, ListNode input, TokenSeq regex)
        {
            Boolean b = predicate.Evaluate(input, regex);
            return b;
        }

        protected abstract FilterBase GetFilter(List<TRegion> list);
    }
}

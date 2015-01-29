﻿using System;
using System.Collections.Generic;
using LocationCodeRefactoring.Spg.LocationRefactor.Program;
using Spg.ExampleRefactoring.Synthesis;

namespace LocationCodeRefactoring.Spg.LocationRefactor.Learn
{
    public interface ILearn
    {
        /// <summary>
        /// Learn from examples
        /// </summary>
        /// <param name="examples">Examples</param>
        /// <returns>Learned programs</returns>
        List<Prog> Learn(List<Tuple<ListNode, ListNode>> examples);

        /// <summary>
        /// Learn location from examples
        /// </summary>
        /// <param name="positiveExamples">Positive examples</param>
        /// <param name="negativeExamples">Negative examples</param>
        /// <returns>Locations programs</returns>
        List<Prog> Learn(List<Tuple<ListNode, ListNode>> positiveExamples, List<Tuple<ListNode, ListNode>> negativeExamples);
    }
}

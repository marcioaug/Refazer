﻿using System;
using System.Collections.Generic;

namespace Spg.ExampleRefactoring.Data.Dig
{
    /// <summary>
    /// Covert element to collection test
    /// </summary>
    public class AddLoopCollector : ExampleCommand
    {
        /// <summary>
        /// Return the train data set.
        /// </summary>
        /// <returns>List of examples</returns>
        public override List<Tuple<String, String>> Train()
        {
            List<Tuple<String, String>> tuples = new List<Tuple<string, string>>();

            String input01 =
@"void Start(){
    foreach(Task t in tasks){
        t.Execute();
    }
}
";


            String output01 =
@"void Start(){
    Set<TaskResult> results = new HashSet<>();
    foreach(Task t in tasks){
        t.Execute();
        results.Add(t.getResult());
    }
}
";
            Tuple<String, String> tuple01 = Tuple.Create(input01, output01);
            Console.WriteLine(input01);
            Console.WriteLine(output01);
            tuples.Add(tuple01);

            String input02 =
@"void Start2(){
    foreach(Command c in commands){
        c.Execute();
    }
}
";


            String output02 =
@"void Start2(){
    Set<TaskResult> results = new HashSet<>();
    foreach(Command c in commands){
        c.Execute();
        results.Add(c.getResult());
    }
}
";
            Tuple<String, String> tuple02 = Tuple.Create(input02, output02);
            Console.WriteLine(input02);
            Console.WriteLine(output02);
            tuples.Add(tuple02);
            return tuples;
        }

        /// <summary>
        /// Return the test data.
        /// </summary>
        /// <returns>Return a string to be tested.</returns>
        public override Tuple<String, String> Test()
        {
            String input01 =
@"void Start(){
    foreach(TaskResult p in tasks){
        p.Execute();
    }
}
";
            
            String output01 =
@"void Start(){
    Set<TaskResult> results = new HashSet<>();
    foreach(TaskResult p in tasks){
        p.Execute();
        results.Add(p.getResult());
    }
}
";
            Tuple<String, String> test = Tuple.Create(input01, output01);
            return test;
        }
    }
}

﻿using System;
using System.Collections.Generic;
using DiGraph;
using ExampleRefactoring.Spg.ExampleRefactoring.Expression;

namespace ExampleRefactoring.Spg.ExampleRefactoring.Digraph
{
    /// <summary>
    /// Class used to represent the set of mapping present in a given string
    /// </summary>
    public class Dag
    {
        /// <summary>
        /// Vertex mapping
        /// </summary>
        /// <returns>Vertex mapping</returns>
        public Dictionary<string, Vertex> Vertexes { get; set;}

        /// <summary>
        /// Directed Graph with the nodes and connection of indexes present on the string
        /// </summary>
        public  DirectedGraph dag { get; set; }

        /// <summary>
        /// Set of mapping used to construct the synthesizer.
        /// </summary>
        public Dictionary<Tuple<Vertex, Vertex>, List<IExpression>> Mapping { get; set; }

        /// <summary>
        /// First vertex
        /// </summary>
        /// <returns>First vertex</returns>
        public Vertex Init { get; set; } 

        /// <summary>
        /// End node
        /// </summary>
        /// <returns>End node</returns>
        public Vertex End { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dag">Directed graph</param>
        /// <param name="init">Start node</param>
        /// <param name="end">End node</param>
        /// <param name="mapping">Mapping</param>
        /// <param name="vertices">Vertexes</param>
        public Dag(DirectedGraph dag, Vertex init, Vertex end,  Dictionary<Tuple<Vertex, Vertex>, List<IExpression>> mapping, Dictionary<string, Vertex> vertices)
        {
            this.dag = dag;
            this.Init = init;
            this.End = end;
            this.Mapping = mapping;
            this.Vertexes = vertices;
        }
    }
}

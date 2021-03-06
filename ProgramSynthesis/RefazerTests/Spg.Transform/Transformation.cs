using System;

namespace UnitTests.Spg.Transform
{
    /// <summary>
    /// Represents a transformation
    /// </summary>
    public class Transformation
    {
        /// <summary>
        /// Before and after source code transformation
        /// </summary>
        /// <returns>Before and after source code transformation</returns>
        public Tuple<string, string> transformation { get; set; }

        /// <summary>
        /// Source path
        /// </summary>
        /// <returns>Source path</returns>
        public string SourcePath { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="transformation">Before and after transformation</param>
        /// <param name="SourcePath">Source path</param>
        public Transformation(Tuple<string, string> transformation, string SourcePath)
        {
            this.transformation = transformation;
            this.SourcePath = SourcePath;
        }
    }
}



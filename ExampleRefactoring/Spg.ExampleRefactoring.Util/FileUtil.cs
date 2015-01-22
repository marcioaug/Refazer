﻿using System;
using System.IO;

namespace Spg.ExampleRefactoring.Util
{
    /// <summary>
    /// Manage files
    /// </summary>
    public static class FileUtil
    {
        /// <summary>
        /// Read a file and return a string as its content.
        /// </summary>
        /// <returns>String representing the content of the file</returns>
        public static String ReadFile(String path) {
            String value = File.ReadAllText(path);
            return value;
        }

        /// <summary>
        /// Write string data to a file
        /// </summary>
        /// <param name="path">File path</param>
        /// <param name="sourceCode">Source code</param>
        public static void WriteToFile(string path, string sourceCode)
        {
            StreamWriter file = new StreamWriter(path);
            file.Write(sourceCode);
            file.Close();
        }
    }
}

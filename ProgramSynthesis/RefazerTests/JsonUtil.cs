using System;
using Newtonsoft.Json;
using Spg.ExampleRefactoring.Util;
using UnitTests;

namespace Spg.ExampleRefactoring.Util
{
    /// <summary>
    /// Json utility
    /// </summary>
    public class JsonUtil<T>
    {
        /// <summary>
        /// Write object to data
        /// </summary>
        /// <param name="t">Object</param>
        /// <param name="path">File path</param>
        public static void Write(T t, string path)
        {
            System.IO.StreamWriter file = new System.IO.StreamWriter(path);
            string json = "";
            try
            {
                json = JsonConvert.SerializeObject(t, Formatting.Indented,
                    new JsonSerializerSettings() {ReferenceLoopHandling = ReferenceLoopHandling.Ignore});
                file.Write(json);
            }
            catch (OutOfMemoryException)
            {
                //MessageBox.Show("Exception");
                Console.WriteLine("Could not write to file: " + path);
            }
            finally
            {
                file.Close();
            }
        }

        /// <summary>
        /// Read Json object
        /// </summary>
        /// <param name="path">File path</param>
        /// <returns>Object</returns>
        public static T Read(string path)
        {
            //JsonSerializerSettings settings = new JsonSerializerSettings();
            //settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            //settings.PreserveReferencesHandling = PreserveReferencesHandling.Objects;

            string json = FileUtil.ReadFile(path);
            T obj = JsonConvert.DeserializeObject<T>(json/*, settings*/);
            return obj;
        }
    }
}


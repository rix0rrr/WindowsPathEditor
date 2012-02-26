using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace WindowsPathEditor
{
    /// <summary>
    /// Class representing a hit in a filesystem search operation
    /// </summary>
    public class PathMatch
    {
        public PathMatch(string directory, string filename)
        {
            this.Directory = directory;
            this.Filename = filename;
        }

        public string Directory { get; private set; }
        public string Filename { get; private set; }
        public string FullPath { get { return Path.Combine(Directory, Filename); } }

        public override string ToString()
        {
            return FullPath;
        }
    }
}

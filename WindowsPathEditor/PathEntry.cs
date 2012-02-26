using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.IO;

namespace WindowsPathEditor
{
    public class PathEntry
    {
        public PathEntry(string symbolicPath)
        {
            SymbolicPath = symbolicPath;
        }

        /// <summary>
        /// The path with placeholders (%WINDIR%, etc...)
        /// </summary>
        public string SymbolicPath { get; private set; }

        /// <summary>
        /// The actual path
        /// </summary>
        public string ActualPath 
        {
            get { return Path.GetFullPath(Environment.ExpandEnvironmentVariables(SymbolicPath)); }
        }

        /// <summary>
        /// Whether the given directory actually exists
        /// </summary>
        public bool Exists
        {
            get { return Directory.Exists(ActualPath); }
        }

        public IEnumerable<PathMatch> Find(string prefix)
        {
            try
            {
                return Directory.EnumerateFiles(ActualPath, prefix + "*")
                    .Select(file => new PathMatch(ActualPath, Path.GetFileName(file)));
            } catch (IOException)
            {
                return Enumerable.Empty<PathMatch>();
            }
        }

        public override string ToString()
        {
            return SymbolicPath;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is PathEntry)) return false;
            return ((PathEntry)obj).SymbolicPath.ToLower() == SymbolicPath.ToLower();
        }

        public override int GetHashCode()
        {
            return SymbolicPath.ToLower().GetHashCode();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsPathEditor
{
    public class DiffPath
    {
        public DiffPath(PathEntry path, bool added)
        {
            Path = path;
            IsAdded = added;
        }

        public PathEntry Path { get; private set; }

        public bool IsAdded { get; private set; }

        public string SymbolicPath { get { return Path.SymbolicPath; } }
    }
}

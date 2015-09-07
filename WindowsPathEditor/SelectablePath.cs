using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsPathEditor
{
    public class SelectablePath
    {
        public SelectablePath(string path, bool selected)
        {
            Path = path;
            IsSelected = selected;
        }

        public string Path { get; private set; }
        public bool IsSelected { get; set; }
    }
}

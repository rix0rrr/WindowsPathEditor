using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.IO;

namespace WindowsPathEditor
{
    public class PathEntry : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private List<string> issues = new List<string>();

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
            get { return Environment.ExpandEnvironmentVariables(SymbolicPath); }
        }

        /// <summary>
        /// Whether the given directory actually exists
        /// </summary>
        public bool Exists
        {
            get { return Directory.Exists(ActualPath); }
        }

        public bool HasIssues { get { return issues.Count() > 0; } }

        /// <summary>
        /// Return all issues with the PathEntry
        /// </summary>
        public IEnumerable<string> Issues { get { return issues.ToList(); } }

        /// <summary>
        /// Add an issue 
        /// </summary>
        public void AddIssue(string issue)
        {
            issues.Add(issue);
            PropertyChanged.Notify(() => Issues);
        }

        public override string ToString()
        {
            return SymbolicPath;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace WindowsPathEditor
{
    /// <summary>
    /// Mutable wrapper for a PathEntry that can have issues added to i
    /// </summary>
    public class AnnotatedPathEntry : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private List<string> issues = new List<string>();

        public AnnotatedPathEntry(PathEntry path)
        {
            Path = path;
        }

        public PathEntry Path { get; private set; }

        /// <summary>
        /// Return the alert level (0, 1 or 2) depending on whether everything is ok, the dirty has issues or is missing
        /// </summary>
        public int AlertLevel
        {
            get
            {
                if (!Path.Exists) return 2;
                if (issues.Count() > 0) return 1;
                return 0;
            }
        }

        public bool Exists { get { return Path.Exists; } }

        public string SymbolicPath { get { return Path.SymbolicPath; } }


        /// <summary>
        /// Return all issues with the PathEntry
        /// </summary>
        public IEnumerable<string> Issues 
        {
            get
            {
                lock (issues) 
                {
                    return issues.ToList();
                }
            }
        }

        /// <summary>
        /// Add an issue 
        /// </summary>
        public void AddIssue(string issue)
        {
            lock (issues) issues.Add(issue);
            PropertyChanged.Notify(() => Issues);
            PropertyChanged.Notify(() => AlertLevel);
        }

        internal void ClearIssues()
        {
            lock(issues) issues.Clear();
            PropertyChanged.Notify(() => Issues);
            PropertyChanged.Notify(() => AlertLevel);
        }

        public override string ToString()
        {
            return Path.ToString();
        }

        public static AnnotatedPathEntry FromPath(PathEntry p)
        {
            return new AnnotatedPathEntry(p);
        }
    }
}

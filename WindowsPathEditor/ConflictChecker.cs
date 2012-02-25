using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;

namespace WindowsPathEditor
{
    /// <summary>
    /// Scans directories on disk for relevant files and raises events for them
    /// </summary>
    public class ConflictChecker
    {
        private readonly Thread checkerThread;

        private readonly BlockingCollection<PathEntry> work = new BlockingCollection<PathEntry>();
        private readonly Dictionary<string, string> found   = new Dictionary<string,string>();

        public ConflictChecker()
        {
            checkerThread = new Thread(BackgroundChecker) { IsBackground=true };
            checkerThread.Start();
        }

        /// <summary>
        /// Check all points in the given set
        /// </summary>
        public void Check(IEnumerable<PathEntry> paths)
        {
            Clear();
            
            foreach (var path in paths)
            {
                if (!path.Exists)
                {
                    path.AddIssue("Does not exist");
                    return;
                }
    
                work.Add(path);
                // FIXME: Do file checking in thread
            }
        }

        private void Clear()
        {
            PathEntry pe;
            while (remainder.TryDequeue(out pe)) /* Ignore */;
            found.Clear();
        }

        private void BackgroundChecker()
        {
            while (true)
            {
                PathEntry path = work.Take();
            }
        }
    }
}

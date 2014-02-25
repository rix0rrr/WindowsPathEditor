using System;
using System.Collections.Generic;
using System.IO;
using System.Security;

namespace WindowsPathEditor
{
    /// <summary>
    /// Class to search for 'bin' directories
    /// </summary>
    internal class SearchOperation
    {
        private readonly String root;
        private readonly List<String> results = new List<String>();
        private readonly IReportProgress progressSink;
        private readonly int maxDepth;

        public SearchOperation(string root, int maxDepth, IReportProgress progressSink)
        {
            this.root = root;
            this.progressSink = progressSink;
            this.maxDepth = maxDepth;
        }

        public IEnumerable<string> Run()
        {
            progressSink.Begin();

            Search(root, 0);

            progressSink.Done();
            return results;
        }

        private void Search(String dir, int level)
        {
            progressSink.ReportProgress(dir);
            if (progressSink.Cancelled) return;

            if (Path.GetFileName(dir).ToLower() == "bin")
            {
                results.Add(dir);
                return; // No need to descend further
            }

            // If this is not a 'bin' directory, search its children
            if (level < maxDepth)
            {
                try
                {
                    foreach (var subdir in Directory.EnumerateDirectories(dir))
                    {
                        Search(subdir, level + 1);
                        if (progressSink.Cancelled) return;
                    }
                }
                catch (SecurityException)
                {
                    // Ignore
                }
                catch (UnauthorizedAccessException)
                {
                    // Ignore
                }
            }
        }
    }
}
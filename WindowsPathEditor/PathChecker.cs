using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.IO;
using System.Collections;

namespace WindowsPathEditor
{
    public class PathChecker : IDisposable
    {
        /// <summary>
        /// The single thread that will be used to schedule all disk lookups
        /// </summary>
        private Thread thread;

        /// <summary>
        /// The queue used to communicate with the background thread
        /// </summary>
        private BlockingCollection<IEnumerable<AnnotatedPathEntry>> pathsToProcess = new BlockingCollection<IEnumerable<AnnotatedPathEntry>>(new ConcurrentQueue<IEnumerable<AnnotatedPathEntry>>());
         
        /// <summary>
        /// Cache for the listFiles operation
        /// </summary>
        private ConcurrentDictionary<string, IEnumerable<string>> fileCache = new ConcurrentDictionary<string,IEnumerable<string>>();

        /// <summary>
        /// The currently applicable path
        /// </summary>
        private IEnumerable<PathEntry> currentPath = Enumerable.Empty<PathEntry>();

        /// <summary>
        /// Extensions to check for conflicts
        /// </summary>
        private readonly IEnumerable<string> extensions;

        private bool running           = true;
        private bool abortCurrentCheck = false;

        public PathChecker(IEnumerable<string> extensions)
        {
            this.extensions = extensions.Concat(new[]{ ".dll" }).Select(_ => _.ToLower());
            thread = new Thread(CheckerLoop);
            thread.Start();
        }

        /// <summary>
        /// Check all paths in the given set
        /// </summary>
        public void Check(IEnumerable<AnnotatedPathEntry> paths)
        {
            currentPath = paths.Select(_ => _.Path);
            abortCurrentCheck = true;
            pathsToProcess.Add(paths);
        }

        /// <summary>
        /// Method to do the actual checking (call from thread)
        /// </summary>
        /// <param name="paths"></param>
        private void DoCheck(IEnumerable<AnnotatedPathEntry> paths)
        {
            foreach (var path in paths)
            {
                if (abortCurrentCheck) return;

                CheckPath(path);
            }
        }

        private void CheckPath(AnnotatedPathEntry path)
        {
            path.ClearIssues();
            if (!path.Path.Exists)
            {
                path.AddIssue("Does not exist");
                return;
            }

            listFiles(path.Path.ActualPath)
                .Select(file => new { file=file, hit=FirstDir(file)})
                .Where(fh => fh.hit.Directory.ToLower() != path.Path.ActualPath.ToLower())
                .Each(fh => path.AddIssue(string.Format("{0} shadowed by {1}", fh.file, fh.hit.FullPath)));
        }

        /// <summary>
        /// The background thread that will do the checking of the current path
        /// </summary>
        private void CheckerLoop()
        {
            while (running) 
            {
                IEnumerable<AnnotatedPathEntry> subject = pathsToProcess.Take();
                if (subject != null)
                {
                    abortCurrentCheck = false;
                    DoCheck(subject);
                }
            }
        }

        /// <summary>
        /// Search the current path set for all files starting with the given prefix
        /// </summary>
        /// <remarks>
        /// Excludes files with the same name later on in the path, and only files with
        /// applicable extensions.
        /// </remarks>
        public IEnumerable<PathMatch> Search(string prefix)
        {
            var xs = new List<PathMatch>();
            if (prefix == "") return xs;

            var seen = new HashSet<string>();
            foreach (var p in currentPath)
            {
                var newFound = p.Find(prefix)
                    .Where(match => extensions.Contains(Path.GetExtension(match.Filename)))
                    .Where(match => !seen.Contains(match.Filename));

                xs.AddRange(newFound);
                newFound.Select(match => match.Filename)
                    .Each(filename => seen.Add(filename));
            }

            return xs;
        }


        /// <summary>
        /// List all files in a directory, returning them from cache if available to speed up subsequent searches
        /// </summary>
        private IEnumerable<string> listFiles(string path)
        {
            IEnumerable<string> fromCache;
            if (fileCache.TryGetValue(path, out fromCache))
            {
                foreach (var s in fromCache) yield return s;
                yield break;
            }

            var files = new List<string>();
            foreach (var f in Directory.EnumerateFiles(path)
                .Where(_ => extensions.Contains(Path.GetExtension(_).ToLower()))
                .Select(Path.GetFileName))
            {
                files.Add(f);
                yield return f;
            }
            fileCache[path] = files;
        }

        /// <summary>
        /// Find the file on the given paths and return the first match
        /// </summary>
        private PathMatch FirstDir(string filename)
        {
            return currentPath
                .Where(path => File.Exists(Path.Combine(path.ActualPath, filename)))
                .Select(path => new PathMatch(path.ActualPath, filename))
                .FirstOrDefault() ?? new PathMatch("", "");
        }

        /// <summary>
        /// Try to find an environment variable that matches part of the path and
        /// return that, otherwise return a normal path entry.
        /// </summary>
        public PathEntry EntryFromFilePath(string path)
        {
            foreach (var de in Environment.GetEnvironmentVariables())
            {
                var e = (DictionaryEntry)de;
                var value = (string)e.Value;
                if (value != "" && Directory.Exists(value) && path.StartsWith(value))
                {
                    return new PathEntry("%" + e.Key + "%" + path.Substring(value.Length));
                }
            }
            return new PathEntry(path);
        }

        public void Dispose()
        {
            running = false;
            pathsToProcess.Add(null);
            thread.Join();
        }
    }

}

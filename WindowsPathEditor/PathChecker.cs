using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.IO;

namespace WindowsPathEditor
{
    public class PathChecker : IDisposable
    {
        /// <summary>
        /// The single thread that will be used to schedule all disk lookups
        /// </summary>
        private EventLoopScheduler diskScheduler = new EventLoopScheduler(ts => new Thread(ts));
         
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

        public PathChecker(IEnumerable<string> extensions)
        {
            this.extensions = extensions.Concat(new[]{ ".dll" }).Select(_ => _.ToLower());
        }

        /// <summary>
        /// Check all paths in the given set
        /// </summary>
        public void Check(IEnumerable<AnnotatedPathEntry> paths)
        {
            currentPath = paths.Select(_ => _.Path);

            foreach (var path in paths)
            {
                path.ClearIssues();
                if (!path.Path.Exists)
                {
                    path.AddIssue("Does not exist");
                    return;
                }

                var shadowed = Observable.ToObservable(listFiles(path.Path.ActualPath))
                    .Select(file => new { file=file, hit=FirstDir(file)})
                    .Where(fh => fh.hit.Directory.ToLower() != path.Path.ActualPath.ToLower())
                    .Subscribe(fh => path.AddIssue(string.Format("{0} shadowed by {1}", fh.file, fh.hit.FullPath)));
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
        /// List all files in a directory, returning them from cache if available
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
            foreach (var path in currentPath)
            {
                if (File.Exists(Path.Combine(path.ActualPath, filename)))
                    return new PathMatch(path.ActualPath, filename);
            }
            return new PathMatch("", "");
        }

        public void Dispose()
        {
            diskScheduler.Dispose();
        }
    }

}

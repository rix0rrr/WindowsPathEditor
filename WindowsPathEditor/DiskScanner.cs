using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;

namespace WindowsPathEditor
{
    /// <summary>
    /// Scans directories on disk for path-relevant files and raises events for them
    /// </summary>
    class DiskScanner
    {
        private readonly Thread scanThread;
        private readonly BlockingCollection<string> work        = new BlockingCollection<string>();
        private readonly Dictionary<string, List<string>> cache = new Dictionary<string, List<string>>();
        private readonly object stateLock = new object();

        public DiskScanner()
        {
            scanThread = new Thread(BackgroundScanner) { IsBackground=true };
            scanThread.Start();
        }

        /// <summary>
        /// Create an observable that scans the given directories
        /// </summary>
        public IObservable<DiskEntry> Scan(IEnumerable<string> paths)
        {
        }

        private void BackgroundScanner()
        {
            while (true)
            {
                string dir = work.Take();

                if (!cache.ContainsKey(dir)) cache[dir] = ScanDirectory(dir);
            }
        }

        private List<string> ScanDirectory(string dir)
        {
        }
    }

    class DiskEntry
    {
        public DiskEntry(string directory, string file)
        {
            Directory = directory;
            File      = file;
        }

        public string Directory { get; private set; }
        public string File      { get; private set; }
    }
}

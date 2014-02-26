using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace WindowsPathEditor
{
    public class PathEntry
    {
        public PathEntry(string symbolicPath)
        {
            SymbolicPath = symbolicPath
                .Replace('/', '\\');

            var invalidChars = new Regex("[" + Regex.Escape(string.Join("", Path.GetInvalidPathChars())) + "]");
            SymbolicPath = invalidChars.Replace(SymbolicPath, "");

            var stripLaterColons = new Regex("^(.*:.*):");
            while (stripLaterColons.IsMatch(SymbolicPath))
            {
                SymbolicPath = stripLaterColons.Replace(SymbolicPath, "$1");
            }
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
            get { return Path.GetFullPath(Environment.ExpandEnvironmentVariables(SymbolicPath)).TrimEnd('\\'); }
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
            }
            catch (IOException)
            {
                return Enumerable.Empty<PathMatch>();
            }
        }

        public override string ToString()
        {
            return SymbolicPath;
        }

        public override int GetHashCode()
        {
            return ActualPath.ToLower().GetHashCode();
        }

        private static IEnumerable<DictionaryEntry> environment;

        /// <summary>
        /// Try to find an environment variable that matches part of the path and
        /// return that, otherwise return a normal path entry.
        /// </summary>
        /// <remarks>
        /// Be sure to return the longest possible entry, but don't include entries
        /// of fewer than 4 characters (this is to avoid returning %HOMEDRIVE% all the
        /// time, because that one is fairly uninteresting and confusing).
        /// </remarks>
        public static PathEntry FromFilePath(string path)
        {
            if (environment == null)
            {
                environment = GetEnvironment()
                    .Where(entry => ((string)entry.Value).Length > 3)
                    .OrderBy(entry => -((string)entry.Value).Length).ToList();
            }

            foreach (var e in environment)
            {
                var value = (string)e.Value;
                if (value != "" && Directory.Exists(value) && path.StartsWith(value))
                {
                    return new PathEntry("%" + e.Key + "%" + path.Substring(value.Length));
                }
            }
            return new PathEntry(path);
        }

        private static IEnumerable<DictionaryEntry> GetEnvironment()
        {
            var ls = new List<DictionaryEntry>();
            foreach (var entry in Environment.GetEnvironmentVariables())
                ls.Add((DictionaryEntry)entry);
            return ls;
        }
    }
}
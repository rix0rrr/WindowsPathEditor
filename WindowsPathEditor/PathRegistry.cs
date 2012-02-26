using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.Security;

namespace WindowsPathEditor
{
    /// <summary>
    /// Class responsible for reading and writing paths to the registry
    /// </summary>
    class PathRegistry
    {
        private const string SystemEnvironmentKey = @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment";
        private const string UserEnvironmentKey   = @"Environment";

        /// <summary>
        /// Access the System Path
        /// </summary>
        public IEnumerable<PathEntry> SystemPath
        {
            get { return ReadPathFromRegistry(Registry.LocalMachine, SystemEnvironmentKey); }
            set { WritePathToRegistry(Registry.LocalMachine, SystemEnvironmentKey, value); }
        }

        /// <summary>
        /// Access the User Path
        /// </summary>
        public IEnumerable<PathEntry> UserPath
        {
            get { return ReadPathFromRegistry(Registry.CurrentUser, UserEnvironmentKey); }
            set { WritePathToRegistry(Registry.CurrentUser, UserEnvironmentKey, value); }
        }

        /// <summary>
        /// Return a list of all command-line executable extensions 
        /// </summary>
        public IEnumerable<String> ExecutableExtensions
        {
            get {
                return ReadMultipleFromRegistry(Registry.LocalMachine, SystemEnvironmentKey, "PathExt").Concat(
                    ReadMultipleFromRegistry(Registry.CurrentUser, SystemEnvironmentKey, "PathExt"));
            }
        }

        /// <summary>
        /// Whether the System Path is writable for the current user (if not, elevation will be required)
        /// </summary>
        public bool IsSystemPathWritable
        {
            get
            {
                try
                {
                    var k = Registry.LocalMachine.OpenSubKey(SystemEnvironmentKey, true);
                    if (k == null) return false;
                    k.Dispose();
                    return true;
                } 
                catch (SecurityException)
                {
                    return false;
                }
            }
        }

        private IEnumerable<PathEntry> ReadPathFromRegistry(RegistryKey rootKey, string key)
        {
            return ReadMultipleFromRegistry(rootKey, key, "Path").Select(_ => new PathEntry(_));
        }

        private IEnumerable<string> ReadMultipleFromRegistry(RegistryKey rootKey, string key, string value)
        {
            using (var k = rootKey.OpenSubKey(key, false))
            {
                if (k == null) return Enumerable.Empty<string>();

                var reg = k.GetValue(value, "", RegistryValueOptions.DoNotExpandEnvironmentNames) ?? "";
                var path = reg is string ? (string)reg : "";
    
                return path.Split(';').Where(_ => _ != "");
            }
        }

        private void WritePathToRegistry(RegistryKey rootKey, string key, IEnumerable<PathEntry> path)
        {
            using (var k = rootKey.OpenSubKey(key, true))
            {
                var reg = string.Join(";", path.Select(_ => _.SymbolicPath));
                k.SetValue("Path", reg, RegistryValueKind.ExpandString);
            }
        }
    }
}

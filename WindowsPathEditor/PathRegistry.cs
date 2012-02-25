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
        private const string SystemPathSubKey = @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment";
        private const string UserPathSubKey   = @"Environment";

        /// <summary>
        /// Access the System Path
        /// </summary>
        public IEnumerable<PathEntry> SystemPath
        {
            get { return ReadPathFromRegistry(Registry.LocalMachine, SystemPathSubKey); }
            set { WritePathToRegistry(Registry.LocalMachine, SystemPathSubKey, value); }
        }

        /// <summary>
        /// Access the User Path
        /// </summary>
        public IEnumerable<PathEntry> UserPath
        {
            get { return ReadPathFromRegistry(Registry.CurrentUser, UserPathSubKey); }
            set { WritePathToRegistry(Registry.CurrentUser, UserPathSubKey, value); }
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
                    var k = Registry.LocalMachine.OpenSubKey(SystemPathSubKey, true);
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
            using (var k = rootKey.OpenSubKey(key, false))
            {
                var reg = k.GetValue("Path", "", RegistryValueOptions.DoNotExpandEnvironmentNames) ?? "";
                var path = reg is string ? (string)reg : "";
    
                return path.Split(';').Where(_ => _ != "").Select(_ => new PathEntry(_));
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

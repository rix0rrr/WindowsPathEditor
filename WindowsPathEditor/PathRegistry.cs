using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;

namespace WindowsPathEditor
{
    /// <summary>
    /// Class responsible for reading and writing paths to the registry
    /// </summary>
    internal class PathRegistry
    {
        private const string SystemEnvironmentKey = @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment";
        private const string UserEnvironmentKey = @"Environment";

        private bool? systemPathWritable;

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
            get
            {
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
                if (!systemPathWritable.HasValue)
                {
                    try
                    {
                        var k = Registry.LocalMachine.OpenSubKey(SystemEnvironmentKey, true);
                        if (k == null)
                        {
                            systemPathWritable = false;
                            return false;
                        }
                        k.Dispose();
                        systemPathWritable = true;
                    }
                    catch (SecurityException)
                    {
                        systemPathWritable = false;
                    }
                }
                return systemPathWritable.Value;
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
            KickExplorer();
        }

        /// <summary>
        /// Give explorer and other programs a sign that the environment vars got updated
        /// </summary>
        /// <remarks>
        /// Explorer caches the environment vars internally. Without this signal,
        /// they won't get reread from the registry.
        /// </remarks>
        private static void KickExplorer()
        {
            UIntPtr retVal;
            IntPtr HWND_BROADCAST = new IntPtr(0xffff);

            if (IntPtr.Zero == SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, UIntPtr.Zero, "Environment", SendMessageTimeoutFlags.SMTO_ABORTIFHUNG, 5000, out retVal))
                throw new Win32Exception();
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessageTimeout(
            IntPtr hWnd,
            uint Msg,
            UIntPtr wParam,
            string lParam,
            SendMessageTimeoutFlags fuFlags,
            uint uTimeout,
            out UIntPtr lpdwResult);

        private const uint WM_SETTINGCHANGE = 0x1A;

        [Flags]
        private enum SendMessageTimeoutFlags : uint
        {
            SMTO_NORMAL = 0x0,
            SMTO_BLOCK = 0x1,
            SMTO_ABORTIFHUNG = 0x2,
            SMTO_NOTIMEOUTIFNOTHUNG = 0x8
        }
    }
}
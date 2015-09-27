using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Threading;
using System.Windows.Forms;

namespace WindowsPathEditor
{
    /// <summary>
    /// Similar to <see cref="FolderBrowserDialog"/>, but provides a more
    /// fine grained access to the creation flags of the dialog (<see cref="CreationFlags"/>
    /// property).
    /// </summary>
    public sealed class FolderBrowserDialogEx : CommonDialog
    {
        /// <summary>
        /// Various creation flags modifying the appearance 
        /// and the behavior of the dialog.
        /// </summary>
        [Flags]
        public enum Flags : int
        {
            /// <summary>
            /// Only return file system directories. If the user selects 
            /// folders that are not part of the file system, the OK button is grayed.
            /// </summary>
            /// <remarks>
            /// The OK button remains enabled for "\\server" items, as well as 
            /// "\\server\share" and directory items. However, if the user selects 
            /// a "\\server" item, passing the PIDL returned by SHBrowseForFolder 
            /// to SHGetPathFromIDList fails.
            /// </remarks>
            BIF_RETURNONLYFSDIRS = 0x0001,

            /// <summary>
            /// Do not include network folders below the domain level in 
            /// the dialog box's tree view control.
            /// </summary>
            BIF_DONTGOBELOWDOMAIN = 0x0002,

            /// <summary>
            /// Include a status area in the dialog box. The callback function can set 
            /// the status text by sending messages to the dialog box. This flag is not 
            /// supported when BIF_NEWDIALOGSTYLE is specified.
            /// </summary>
            BIF_STATUSTEXT = 0x0004,

            /// <summary>
            /// Only return file system ancestors. An ancestor is a subfolder that 
            /// is beneath the root folder in the namespace hierarchy. If the user 
            /// selects an ancestor of the root folder that is not part of the file 
            /// system, the OK button is grayed.
            /// </summary>
            BIF_RETURNANCESTORS = 0x0008,

            /// <summary>
            /// <b>Version 4.71.</b> Include an edit control in the browse dialog box 
            /// that allows the user to type the name of an item.
            /// </summary>
            BIF_EDITBOX = 0x0010,

            // TODO: continue filling documentation from MSDN...

            /// <summary>
            /// 
            /// </summary>
            BIF_VALIDATE = 0x0020,

            /// <summary>
            /// 
            /// </summary>
            BIF_NEWDIALOGSTYLE = 0x0040,

            /// <summary>
            /// 
            /// </summary>
            BIF_USENEWUI = 0x0050,

            /// <summary>
            /// 
            /// </summary>
            BIF_BROWSEINCLUDEURLS = 0x0080,

            /// <summary>
            /// 
            /// </summary>
            BIF_UAHINT = 0x0100,

            /// <summary>
            /// 
            /// </summary>
            BIF_NONEWFOLDERBUTTON = 0x0200,

            /// <summary>
            /// 
            /// </summary>
            BIF_NOTRANSLATETARGETS = 0x0400,

            /// <summary>
            /// 
            /// </summary>
            BIF_BROWSEFORCOMPUTER = 0x1000,

            /// <summary>
            /// 
            /// </summary>
            BIF_BROWSEFORPRINTER = 0x2000,

            /// <summary>
            /// 
            /// </summary>
            BIF_BROWSEINCLUDEFILES = 0x4000,

            /// <summary>
            /// 
            /// </summary>
            BIF_SHAREABLE = 0x8000
        }

        #region Interop

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class BROWSEINFO
        {
            public IntPtr hwndOwner;
            public IntPtr pidlRoot;
            public IntPtr pszDisplayName;
            public string lpszTitle;
            public int ulFlags;
            public BrowseCallbackProc lpfn;
            public IntPtr lParam;
            public int iImage;
        }

        [ComImport, Guid("00000002-0000-0000-c000-000000000046"),
        SuppressUnmanagedCodeSecurity,
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMalloc
        {
            [PreserveSig]
            IntPtr Alloc(int cb);
            [PreserveSig]
            IntPtr Realloc(IntPtr pv, int cb);
            [PreserveSig]
            void Free(IntPtr pv);
            [PreserveSig]
            int GetSize(IntPtr pv);
            [PreserveSig]
            int DidAlloc(IntPtr pv);
            [PreserveSig]
            void HeapMinimize();
        }

        private const int BFFM_INITIALIZED = 1;
        private const int BFFM_SELCHANGED = 2;
        private const int BFFM_SETSELECTION = 0x466;
        private const int BFFM_SETSELECTIONW = 0x467;
        private const int BFFM_ENABLEOK = 0x465;

        private delegate int BrowseCallbackProc(IntPtr hwnd, int msg, IntPtr lParam, IntPtr lpData);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(HandleRef hWnd, int msg, int wParam, string lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(HandleRef hWnd, int msg, int wParam, int lParam);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern bool SHGetPathFromIDList(IntPtr pidl, IntPtr pszPath);

        [DllImport("shell32.dll")]
        private static extern int SHGetMalloc([Out, MarshalAs(UnmanagedType.LPArray)] IMalloc[] ppMalloc);

        [DllImport("shell32.dll")]
        private static extern int SHGetSpecialFolderLocation(IntPtr hwnd, int csidl, ref IntPtr ppidl);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHBrowseForFolder([In] BROWSEINFO lpbi);

        #endregion

        private BrowseCallbackProc callback;
        private string descriptionText;
        private Environment.SpecialFolder rootFolder;
        private string selectedPath;
        private bool selectedPathNeedsCheck;

        private Flags flags;

        /// <summary>
        /// Initializes a new instance of the <see cref="FolderBrowserDialogEx"/> class.
        /// </summary>
        public FolderBrowserDialogEx() : base() { Reset(); }

        #region Properties

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>The description.</value>
        [Description("Folder Browser Dialog Description"),
         Category("Folder browsing"), DefaultValue(""),
         Browsable(true), Localizable(true)]
        public string Description
        {
            get { return descriptionText; }
            set { descriptionText = (value == null) ? string.Empty : value; }
        }

        /// <summary>
        /// Gets or sets the root folder.
        /// </summary>
        /// <value>The root folder.</value>
        [Description("Folder Browser Dialog Root Folder"), Localizable(false), DefaultValue(0),
        Category("Folder Browsing"), Browsable(true)]
        public Environment.SpecialFolder RootFolder
        {
            get { return rootFolder; }
            set
            {
                if (!Enum.IsDefined(typeof(Environment.SpecialFolder), value))
                    throw new InvalidEnumArgumentException("value",
                        (int)value, typeof(Environment.SpecialFolder));
                rootFolder = value;
            }
        }

        /// <summary>
        /// Gets or sets the selected path.
        /// </summary>
        /// <value>The selected path.</value>
        [Description("Folder Browser Dialog Selected Path"), Category("Folder Browsing"),
        Browsable(true), DefaultValue(""), Localizable(true)]
        public string SelectedPath
        {
            get
            {
                if ((selectedPath != null) && (selectedPath.Length != 0) && selectedPathNeedsCheck)
                    new FileIOPermission(FileIOPermissionAccess.PathDiscovery, selectedPath).Demand();
                return selectedPath;
            }
            set
            {
                selectedPath = (value == null) ? string.Empty : value;
                selectedPathNeedsCheck = false;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show the new folder button.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if the new folder button should be shown; otherwise, <c>false</c>.
        /// </value>
        [Category("Folder Browsing"), Localizable(false),
         Description("Folder Browser Dialog Show New Folder Button"),
         DefaultValue(true), Browsable(true)]
        public bool ShowNewFolderButton
        {
            get
            {
                return ((flags & Flags.BIF_NONEWFOLDERBUTTON) !=
                    Flags.BIF_NONEWFOLDERBUTTON);
            }
            set
            {
                if (value)
                    flags &= ~Flags.BIF_NONEWFOLDERBUTTON;
                else flags |= Flags.BIF_NONEWFOLDERBUTTON;
            }
        }

        /// <summary>
        /// Gets or sets the creation flags.
        /// </summary>
        /// <value>The creation flags.</value>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Flags CreationFlags
        {
            get { return flags; }
            set { flags = value; }
        }

        #endregion

        /// <summary>
        /// Resets the properties of this common dialog box to their default values.
        /// </summary>
        public override void Reset()
        {
            rootFolder = Environment.SpecialFolder.Desktop;
            descriptionText = string.Empty;
            selectedPath = string.Empty;
            selectedPathNeedsCheck = false;
            flags = Flags.BIF_USENEWUI;
        }

        /// <summary>
        /// When overridden in a derived class, specifies a common dialog box.
        /// </summary>
        /// <param name="hwndOwner">A value that represents the window handle of the owner window for the common dialog box.</param>
        /// <returns>
        /// true if the dialog box was successfully run; otherwise, false.
        /// </returns>
        protected override bool RunDialog(IntPtr hwndOwner)
        {
            IntPtr zero = IntPtr.Zero;
            bool flag = false;
            SHGetSpecialFolderLocation(hwndOwner, (int)rootFolder, ref zero);
            if (zero == IntPtr.Zero)
            {
                SHGetSpecialFolderLocation(hwndOwner, 0, ref zero);
                if (zero == IntPtr.Zero) throw new InvalidOperationException(
                    "Folder Browser Dialog: no root folder");
            }

            //int flags = 0x40;
            //if (!showNewFolderButton) flags += 0x200;

            if (Control.CheckForIllegalCrossThreadCalls && (Application.OleRequired() != ApartmentState.STA))
                throw new ThreadStateException("Debugging exception only: Thread must be STA");

            IntPtr pidl = IntPtr.Zero;
            IntPtr hglobal = IntPtr.Zero;
            IntPtr pszPath = IntPtr.Zero;
            try
            {
                BROWSEINFO lpbi = new BROWSEINFO();
                hglobal = Marshal.AllocHGlobal((int)(260 * Marshal.SystemDefaultCharSize));
                pszPath = Marshal.AllocHGlobal((int)(260 * Marshal.SystemDefaultCharSize));
                callback = new BrowseCallbackProc(FolderBrowserDialog_BrowseCallbackProc);
                lpbi.pidlRoot = zero;
                lpbi.hwndOwner = hwndOwner;
                lpbi.pszDisplayName = hglobal;
                lpbi.lpszTitle = descriptionText;
                lpbi.ulFlags = (int)flags;
                lpbi.lpfn = callback;
                lpbi.lParam = IntPtr.Zero;
                lpbi.iImage = 0;
                pidl = SHBrowseForFolder(lpbi);
                if (pidl != IntPtr.Zero)
                {
                    SHGetPathFromIDList(pidl, pszPath);
                    selectedPathNeedsCheck = true;
                    selectedPath = Marshal.PtrToStringAuto(pszPath);
                    flag = true;
                }
            }
            finally
            {
                IMalloc sHMalloc = GetSHMalloc();
                sHMalloc.Free(zero);
                if (pidl != IntPtr.Zero) sHMalloc.Free(pidl);
                if (pszPath != IntPtr.Zero) Marshal.FreeHGlobal(pszPath);
                if (hglobal != IntPtr.Zero) Marshal.FreeHGlobal(hglobal);

                callback = null;
            }
            return flag;
        }

        private static IMalloc GetSHMalloc()
        {
            IMalloc[] ppMalloc = new IMalloc[1];
            SHGetMalloc(ppMalloc);
            return ppMalloc[0];
        }
        private int FolderBrowserDialog_BrowseCallbackProc(IntPtr hwnd, int msg, IntPtr lParam, IntPtr lpData)
        {
            switch (msg)
            {
                case BFFM_INITIALIZED:
                    if (selectedPath.Length != 0)
                        SendMessage(new HandleRef(null, hwnd), BFFM_SETSELECTIONW, 1, selectedPath);
                    break;

                case BFFM_SELCHANGED:
                    IntPtr pidl = lParam;
                    if (pidl != IntPtr.Zero)
                    {
                        IntPtr pszPath = Marshal.AllocHGlobal((int)(260 * Marshal.SystemDefaultCharSize));
                        bool flag = SHGetPathFromIDList(pidl, pszPath);
                        Marshal.FreeHGlobal(pszPath);
                        SendMessage(new HandleRef(null, hwnd), BFFM_ENABLEOK, 0, flag ? 1 : 0);
                    }
                    break;
            }
            return 0;
        }
    }
}

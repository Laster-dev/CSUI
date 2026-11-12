// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using CSUI.Core.Interop;
using MS.Internal.Interop;
using IOPath = System.IO.Path;

namespace Microsoft.Win32
{
    /// <summary>
    /// Represents a known folder or file system path added to the Places list of a file dialog.
    /// </summary>
    public sealed class FileDialogCustomPlace
    {
        public FileDialogCustomPlace(Guid knownFolder)
        {
            KnownFolder = knownFolder;
        }

        public FileDialogCustomPlace(string path)
        {
            Path = path ?? string.Empty;
        }

        public Guid KnownFolder { get; }

        public string Path { get; }
    }

    /// <summary>
    /// Provides standard FileDialogCustomPlace locations corresponding to Windows Known Folders.
    /// </summary>
    public static class FileDialogCustomPlaces
    {
        public static FileDialogCustomPlace Contacts => new FileDialogCustomPlace(new Guid("56784854-C6CB-462B-8169-88E350ACB882"));
        public static FileDialogCustomPlace Cookies => new FileDialogCustomPlace(new Guid("2B0F765D-C0E9-4171-908E-08A611B84FF6"));
        public static FileDialogCustomPlace Desktop => new FileDialogCustomPlace(new Guid("B4BFCC3A-DB2C-424C-B029-7FE99A87C641"));
        public static FileDialogCustomPlace Documents => new FileDialogCustomPlace(new Guid("FDD39AD0-238F-46AF-ADB4-6C85480369C7"));
        public static FileDialogCustomPlace Favorites => new FileDialogCustomPlace(new Guid("1777F761-68AD-4D8A-87BD-30B759FA33DD"));
        public static FileDialogCustomPlace GameTasks => new FileDialogCustomPlace(new Guid("054FAE61-4DD8-4787-80B6-090220C4B700"));
        public static FileDialogCustomPlace History => new FileDialogCustomPlace(new Guid("D9DC8A3B-B784-432E-A781-5A1130A75963"));
        public static FileDialogCustomPlace InternetCache => new FileDialogCustomPlace(new Guid("352481E8-33BE-4251-BA85-6007CAEDCF9D"));
        public static FileDialogCustomPlace LocalApplicationData => new FileDialogCustomPlace(new Guid("F1B32785-6FBA-4FCF-9D55-7B8E7F157091"));
        public static FileDialogCustomPlace Music => new FileDialogCustomPlace(new Guid("4BD8D571-6D19-48D3-BE97-422220080E43"));
        public static FileDialogCustomPlace Pictures => new FileDialogCustomPlace(new Guid("33E28130-4E1E-4676-835A-98395C3BC3BB"));
        public static FileDialogCustomPlace ProgramFiles => new FileDialogCustomPlace(new Guid("905E63B6-C1BF-494E-B29C-65B732D3D21A"));
        public static FileDialogCustomPlace ProgramFilesCommon => new FileDialogCustomPlace(new Guid("F7F1ED05-9F6D-47A2-AAAE-29D317C6F066"));
        public static FileDialogCustomPlace Programs => new FileDialogCustomPlace(new Guid("A77F5D77-2E2B-44C3-A6A2-ABA601054A51"));
        public static FileDialogCustomPlace Recent => new FileDialogCustomPlace(new Guid("AE50C081-EBD2-438A-8655-8A092E34987A"));
        public static FileDialogCustomPlace RoamingApplicationData => new FileDialogCustomPlace(new Guid("3EB685DB-65F9-4CF6-A03A-E3EF65729F3D"));
        public static FileDialogCustomPlace SavedGames => new FileDialogCustomPlace(new Guid("4C5C32FF-BB9D-43B0-B5B4-2D72E54EAAA4"));
        public static FileDialogCustomPlace SavedSearches => new FileDialogCustomPlace(new Guid("7D1D3A04-DEBB-4115-95CF-2F29DA2920DA"));
        public static FileDialogCustomPlace SendTo => new FileDialogCustomPlace(new Guid("8983036C-27C0-404B-8F08-102D10DCFD74"));
        public static FileDialogCustomPlace StartMenu => new FileDialogCustomPlace(new Guid("625B53C3-AB48-4EC1-BA1F-A1EF4146FC19"));
        public static FileDialogCustomPlace Startup => new FileDialogCustomPlace(new Guid("B97D20BB-F46A-4C97-BA10-5E3608430854"));
        public static FileDialogCustomPlace System => new FileDialogCustomPlace(new Guid("1AC14E77-02E7-4E5D-B744-2EB1AE5198B7"));
        public static FileDialogCustomPlace Templates => new FileDialogCustomPlace(new Guid("A63293E8-664E-48DB-A079-DF759E0509F7"));
        public static FileDialogCustomPlace Videos => new FileDialogCustomPlace(new Guid("18989B1D-99B5-455B-841C-AB7C74E4DDFC"));
    }

    /// <summary>
    /// An abstract base class for displaying common Win32 dialogs.
    /// </summary>
    public abstract class CommonDialog
    {
        public object Tag { get; set; }

        public abstract void Reset();

        public virtual bool? ShowDialog()
        {
            CheckPermissionsToShowDialog();
            IntPtr activeWindow = MS.Win32.UnsafeNativeMethods.GetActiveWindow();
            return RunDialog(activeWindow);
        }

        public bool? ShowDialog(Window owner)
        {
            CheckPermissionsToShowDialog();
            if (owner == null)
            {
                return ShowDialog();
            }

            IntPtr ownerHwnd = new WindowInteropHelper(owner).CriticalHandle;
            if (ownerHwnd == IntPtr.Zero)
            {
                throw new InvalidOperationException("Cannot show dialog owned by a window that does not have a valid handle.");
            }

            return RunDialog(ownerHwnd);
        }

        protected virtual void CheckPermissionsToShowDialog()
        {
        }

        protected virtual IntPtr HookProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            return IntPtr.Zero;
        }

        protected abstract bool RunDialog(IntPtr hwndOwner);
    }

    /// <summary>
    /// Base class for OpenFileDialog and SaveFileDialog using zero-ComImport NativeAOT COM vtables.
    /// </summary>
    public abstract class FileDialog : CommonDialog
    {
        private List<string> _fileNames;
        private string _defaultExtension;
        private string _filter;
        private int _filterIndex;
        private string _initialDirectory;
        private string _title;
        private IList<FileDialogCustomPlace> _customPlaces;
        private FOS _options;

        protected FileDialog()
        {
            Initialize();
        }

        private void Initialize()
        {
            _options = FOS.NOCHANGEDIR | FOS.PATHMUSTEXIST | FOS.FORCEFILESYSTEM;
            AddExtension = true;
            DereferenceLinks = true;
            ValidateNames = true;
            _fileNames = new List<string>();
            _defaultExtension = string.Empty;
            _filter = string.Empty;
            _filterIndex = 1;
            _initialDirectory = string.Empty;
            _title = string.Empty;
            _customPlaces = new List<FileDialogCustomPlace>();
            RootDirectory = string.Empty;
            ClientGuid = null;
        }

        public override void Reset()
        {
            Initialize();
        }

        public bool AddExtension { get; set; }

        public virtual bool CheckFileExists
        {
            get => GetOption(FOS.FILEMUSTEXIST);
            set => SetOption(FOS.FILEMUSTEXIST, value);
        }

        public bool CheckPathExists
        {
            get => GetOption(FOS.PATHMUSTEXIST);
            set => SetOption(FOS.PATHMUSTEXIST, value);
        }

        public IList<FileDialogCustomPlace> CustomPlaces
        {
            get => _customPlaces;
            set => _customPlaces = value ?? new List<FileDialogCustomPlace>();
        }

        public string DefaultExt
        {
            get => _defaultExtension ?? string.Empty;
            set
            {
                if (value != null)
                {
                    if (value.StartsWith(".", StringComparison.Ordinal))
                    {
                        value = value.Substring(1);
                    }
                    else if (value.Length == 0)
                    {
                        value = null;
                    }
                }
                _defaultExtension = value ?? string.Empty;
            }
        }

        public bool DereferenceLinks
        {
            get => !GetOption(FOS.NODEREFERENCELINKS);
            set => SetOption(FOS.NODEREFERENCELINKS, !value);
        }

        public string FileName
        {
            get => _fileNames != null && _fileNames.Count > 0 ? _fileNames[0] : string.Empty;
            set
            {
                _fileNames.Clear();
                if (!string.IsNullOrEmpty(value))
                {
                    _fileNames.Add(value);
                }
            }
        }

        public string[] FileNames => _fileNames != null ? _fileNames.ToArray() : Array.Empty<string>();

        public string Filter
        {
            get => _filter ?? string.Empty;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _filter = string.Empty;
                    return;
                }

                string[] parts = value.Split('|');
                if (parts.Length % 2 != 0)
                {
                    throw new ArgumentException($"Filter string '{value}' must contain pairs of description and pattern separated by '|'.", nameof(value));
                }
                _filter = value;
            }
        }

        public int FilterIndex
        {
            get => _filterIndex;
            set => _filterIndex = value;
        }

        public string InitialDirectory
        {
            get => _initialDirectory ?? string.Empty;
            set => _initialDirectory = value ?? string.Empty;
        }

        public bool RestoreDirectory
        {
            get => GetOption(FOS.NOCHANGEDIR);
            set => SetOption(FOS.NOCHANGEDIR, value);
        }

        public string RootDirectory { get; set; }

        public Guid? ClientGuid { get; set; }

        public bool AddToRecent
        {
            get => !GetOption(FOS.DONTADDTORECENT);
            set => SetOption(FOS.DONTADDTORECENT, !value);
        }

        public bool ShowHiddenItems
        {
            get => GetOption(FOS.FORCESHOWHIDDEN);
            set => SetOption(FOS.FORCESHOWHIDDEN, value);
        }

        public string SafeFileName
        {
            get
            {
                string fileName = FileName;
                return string.IsNullOrEmpty(fileName) ? string.Empty : IOPath.GetFileName(fileName);
            }
        }

        public string[] SafeFileNames
        {
            get
            {
                if (_fileNames == null || _fileNames.Count == 0)
                {
                    return Array.Empty<string>();
                }

                var result = new string[_fileNames.Count];
                for (int i = 0; i < _fileNames.Count; i++)
                {
                    result[i] = IOPath.GetFileName(_fileNames[i]);
                }
                return result;
            }
        }

        public string Title
        {
            get => _title ?? string.Empty;
            set => _title = value ?? string.Empty;
        }

        public bool ValidateNames
        {
            get => !GetOption(FOS.NOVALIDATE);
            set => SetOption(FOS.NOVALIDATE, !value);
        }

        public event CancelEventHandler FileOk;

        protected void OnFileOk(CancelEventArgs e)
        {
            FileOk?.Invoke(this, e);
        }

        protected override bool RunDialog(IntPtr hwndOwner)
        {
            var results = RunVistaDialog(hwndOwner);
            if (results == null)
            {
                return false;
            }

            _fileNames.Clear();
            _fileNames.AddRange(results);

            var args = new CancelEventArgs();
            OnFileOk(args);
            return !args.Cancel;
        }

        private protected bool GetOption(FOS flag) => (_options & flag) != 0;

        private protected void SetOption(FOS flag, bool value)
        {
            if (value)
            {
                _options |= flag;
            }
            else
            {
                _options &= ~flag;
            }
        }

        internal FOS Options => _options;

        internal string[] FileNamesInternal
        {
            set
            {
                _fileNames.Clear();
                if (value != null)
                {
                    _fileNames.AddRange(value);
                }
            }
        }

        public override string ToString()
        {
            return $"{base.ToString()}: Title: {Title}, FileName: {FileName}";
        }

        internal abstract bool IsOpenDialog { get; }

        internal virtual void ConfigureOptions(ref FOS options)
        {
            if (AddExtension && !string.IsNullOrEmpty(DefaultExt))
            {
                options |= FOS.STRICTFILETYPES;
            }
        }

        private unsafe List<string> RunVistaDialog(IntPtr hwndOwner)
        {
            if (!ShellFileDialogNative.TryCreateFileDialog(IsOpenDialog, out void* dialog))
            {
                return null;
            }

            try
            {
                FOS options = _options;
                ConfigureOptions(ref options);
                ShellFileDialogNative.SetOptions(dialog, (uint)options);

                if (!string.IsNullOrEmpty(Title))
                {
                    ShellFileDialogNative.SetTitle(dialog, Title);
                }

                if (!string.IsNullOrEmpty(DefaultExt))
                {
                    ShellFileDialogNative.SetDefaultExtension(dialog, DefaultExt);
                }

                if (!string.IsNullOrEmpty(FileName))
                {
                    ShellFileDialogNative.SetFileName(dialog, IOPath.GetFileName(FileName));
                    string dir = IOPath.GetDirectoryName(FileName);
                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    {
                        ShellFileDialogNative.SetFolder(dialog, dir);
                    }
                }

                if (!string.IsNullOrEmpty(InitialDirectory) && Directory.Exists(InitialDirectory))
                {
                    ShellFileDialogNative.SetFolder(dialog, InitialDirectory);
                }

                if (ClientGuid.HasValue)
                {
                    ShellFileDialogNative.SetClientGuid(dialog, ClientGuid.Value);
                }

                if (!string.IsNullOrEmpty(Filter))
                {
                    ShellFileDialogNative.SetFileTypes(dialog, Filter, Math.Max(1, FilterIndex));
                }

                if (_customPlaces != null && _customPlaces.Count > 0)
                {
                    foreach (FileDialogCustomPlace place in _customPlaces)
                    {
                        if (!string.IsNullOrEmpty(place?.Path) && Directory.Exists(place.Path))
                        {
                            ShellFileDialogNative.AddPlace(dialog, place.Path);
                        }
                    }
                }

                int hr = ShellFileDialogNative.Show(dialog, hwndOwner);
                if (hr == ShellFileDialogNative.ERROR_CANCELLED || hr < 0)
                {
                    return null;
                }

                if (IsOpenDialog && (options & FOS.ALLOWMULTISELECT) != 0)
                {
                    return ShellFileDialogNative.GetOpenResults(dialog);
                }

                string single = ShellFileDialogNative.GetSingleResult(dialog);
                return single != null ? new List<string> { single } : null;
            }
            finally
            {
                ShellFileDialogNative.Release(dialog);
            }
        }
    }

    /// <summary>
    /// Represents a common dialog box that allows a user to specify options for opening one or more files.
    /// </summary>
    public sealed class OpenFileDialog : FileDialog
    {
        public OpenFileDialog()
        {
            Initialize();
        }

        private void Initialize()
        {
            SetOption(FOS.FILEMUSTEXIST, true);
        }

        public override void Reset()
        {
            base.Reset();
            Initialize();
        }

        public bool Multiselect
        {
            get => GetOption(FOS.ALLOWMULTISELECT);
            set => SetOption(FOS.ALLOWMULTISELECT, value);
        }

        public bool ReadOnlyChecked
        {
            get;
            set;
        }

        public bool ShowReadOnly
        {
            get;
            set;
        }

        public bool ForcePreviewPane
        {
            get => GetOption(FOS.FORCEPREVIEWPANEON);
            set => SetOption(FOS.FORCEPREVIEWPANEON, value);
        }

        internal override bool IsOpenDialog => true;

        public Stream OpenFile()
        {
            string filename = FileName;
            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("No file name was specified.");
            }
            return new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public Stream[] OpenFiles()
        {
            string[] names = FileNames;
            var streams = new Stream[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                streams[i] = new FileStream(names[i], FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            return streams;
        }
    }

    /// <summary>
    /// Represents a common dialog box that allows a user to specify options for saving a file.
    /// </summary>
    public sealed class SaveFileDialog : FileDialog
    {
        public SaveFileDialog()
        {
            Initialize();
        }

        private void Initialize()
        {
            SetOption(FOS.OVERWRITEPROMPT, true);
        }

        public override void Reset()
        {
            base.Reset();
            Initialize();
        }

        public bool CreatePrompt
        {
            get => GetOption(FOS.CREATEPROMPT);
            set => SetOption(FOS.CREATEPROMPT, value);
        }

        public bool OverwritePrompt
        {
            get => GetOption(FOS.OVERWRITEPROMPT);
            set => SetOption(FOS.OVERWRITEPROMPT, value);
        }

        public bool CreateTestFile
        {
            get => !GetOption(FOS.NOTESTFILECREATE);
            set => SetOption(FOS.NOTESTFILECREATE, !value);
        }

        internal override bool IsOpenDialog => false;

        public Stream OpenFile()
        {
            string filename = FileName;
            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("No file name was specified.");
            }
            return new FileStream(filename, FileMode.Create, FileAccess.ReadWrite);
        }
    }

    /// <summary>
    /// Represents a common dialog box that allows a user to select one or more folders.
    /// </summary>
    public sealed class OpenFolderDialog : CommonDialog
    {
        private List<string> _folderNames;
        private string _initialDirectory;
        private string _title;
        private IList<FileDialogCustomPlace> _customPlaces;
        private FOS _options;

        public OpenFolderDialog()
        {
            Initialize();
        }

        private void Initialize()
        {
            _options = FOS.NOCHANGEDIR | FOS.PICKFOLDERS | FOS.PATHMUSTEXIST | FOS.FORCEFILESYSTEM;
            _folderNames = new List<string>();
            _initialDirectory = string.Empty;
            _title = string.Empty;
            RootDirectory = string.Empty;
            DefaultDirectory = string.Empty;
            _customPlaces = new List<FileDialogCustomPlace>();
            ClientGuid = null;
        }

        public override void Reset()
        {
            Initialize();
        }

        public bool Multiselect
        {
            get => (_options & FOS.ALLOWMULTISELECT) != 0;
            set
            {
                if (value) _options |= FOS.ALLOWMULTISELECT;
                else _options &= ~FOS.ALLOWMULTISELECT;
            }
        }

        public string FolderName
        {
            get => _folderNames.Count > 0 ? _folderNames[0] : string.Empty;
            set
            {
                _folderNames.Clear();
                if (!string.IsNullOrEmpty(value))
                {
                    _folderNames.Add(value);
                }
            }
        }

        public string[] FolderNames => _folderNames.ToArray();

        public string SafeFolderName
        {
            get
            {
                string folder = FolderName;
                return string.IsNullOrEmpty(folder) ? string.Empty : IOPath.GetFileName(folder.TrimEnd(IOPath.DirectorySeparatorChar, IOPath.AltDirectorySeparatorChar));
            }
        }

        public string[] SafeFolderNames
        {
            get
            {
                var names = new string[_folderNames.Count];
                for (int i = 0; i < _folderNames.Count; i++)
                {
                    names[i] = IOPath.GetFileName(_folderNames[i].TrimEnd(IOPath.DirectorySeparatorChar, IOPath.AltDirectorySeparatorChar));
                }
                return names;
            }
        }

        public string InitialDirectory
        {
            get => _initialDirectory ?? string.Empty;
            set => _initialDirectory = value ?? string.Empty;
        }

        public string RootDirectory { get; set; }

        public string DefaultDirectory { get; set; }

        public string Title
        {
            get => _title ?? string.Empty;
            set => _title = value ?? string.Empty;
        }

        public bool ValidateNames
        {
            get => (_options & FOS.NOVALIDATE) == 0;
            set
            {
                if (!value) _options |= FOS.NOVALIDATE;
                else _options &= ~FOS.NOVALIDATE;
            }
        }

        public bool AddToRecent
        {
            get => (_options & FOS.DONTADDTORECENT) == 0;
            set
            {
                if (!value) _options |= FOS.DONTADDTORECENT;
                else _options &= ~FOS.DONTADDTORECENT;
            }
        }

        public bool ShowHiddenItems
        {
            get => (_options & FOS.FORCESHOWHIDDEN) != 0;
            set
            {
                if (value) _options |= FOS.FORCESHOWHIDDEN;
                else _options &= ~FOS.FORCESHOWHIDDEN;
            }
        }

        public bool DereferenceLinks
        {
            get => (_options & FOS.NODEREFERENCELINKS) == 0;
            set
            {
                if (!value) _options |= FOS.NODEREFERENCELINKS;
                else _options &= ~FOS.NODEREFERENCELINKS;
            }
        }

        public IList<FileDialogCustomPlace> CustomPlaces
        {
            get => _customPlaces;
            set => _customPlaces = value ?? new List<FileDialogCustomPlace>();
        }

        public Guid? ClientGuid { get; set; }

        public event CancelEventHandler FolderOk;

        protected override bool RunDialog(IntPtr hwndOwner)
        {
            unsafe
            {
                if (!ShellFileDialogNative.TryCreateFileDialog(true, out void* dialog))
                {
                    return false;
                }

                try
                {
                    ShellFileDialogNative.SetOptions(dialog, (uint)_options);
                    if (!string.IsNullOrEmpty(Title))
                    {
                        ShellFileDialogNative.SetTitle(dialog, Title);
                    }
                    if (!string.IsNullOrEmpty(InitialDirectory) && Directory.Exists(InitialDirectory))
                    {
                        ShellFileDialogNative.SetFolder(dialog, InitialDirectory);
                    }
                    if (ClientGuid.HasValue)
                    {
                        ShellFileDialogNative.SetClientGuid(dialog, ClientGuid.Value);
                    }
                    if (_customPlaces != null)
                    {
                        foreach (FileDialogCustomPlace place in _customPlaces)
                        {
                            if (!string.IsNullOrEmpty(place?.Path) && Directory.Exists(place.Path))
                            {
                                ShellFileDialogNative.AddPlace(dialog, place.Path);
                            }
                        }
                    }

                    int hr = ShellFileDialogNative.Show(dialog, hwndOwner);
                    if (hr == ShellFileDialogNative.ERROR_CANCELLED || hr < 0)
                    {
                        return false;
                    }

                    List<string> results = Multiselect
                        ? ShellFileDialogNative.GetOpenResults(dialog)
                        : ShellFileDialogNative.GetSingleResult(dialog) is string s ? new List<string> { s } : null;

                    if (results == null || results.Count == 0)
                    {
                        return false;
                    }

                    _folderNames.Clear();
                    _folderNames.AddRange(results);

                    var e = new CancelEventArgs();
                    FolderOk?.Invoke(this, e);
                    return !e.Cancel;
                }
                finally
                {
                    ShellFileDialogNative.Release(dialog);
                }
            }
        }
    }

    /// <summary>
    /// NativeAOT zero-ComImport / zero-DllImport wrapper for IFileOpenDialog / IFileSaveDialog / IShellItem.
    /// </summary>
    internal static unsafe class ShellFileDialogNative
    {
        public const int ERROR_CANCELLED = unchecked((int)0x800704C7);
        private const uint CLSCTX_INPROC_SERVER = 0x1;
        private const uint SIGDN_FILESYSPATH = 0x80058000;
        private const uint FDAP_BOTTOM = 0;

        private static readonly Guid CLSID_FileOpenDialog = new Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7");
        private static readonly Guid CLSID_FileSaveDialog = new Guid("C0B4E2F3-BA21-4773-8DBA-335EC946EB8B");
        private static readonly Guid IID_IFileOpenDialog = new Guid("D57C7288-D4AD-4768-BE02-9D969532D960");
        private static readonly Guid IID_IFileSaveDialog = new Guid("84BCCD23-5FDE-4CDB-AEA4-AF64B83D78AB");
        private static readonly Guid IID_IShellItem = new Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE");

        [StructLayout(LayoutKind.Sequential)]
        private struct COMDLG_FILTERSPEC
        {
            public char* pszName;
            public char* pszSpec;
        }

        private static readonly nint s_ole32 = NativeLibrary.TryLoad("ole32.dll", out nint hOle) ? hOle : 0;
        private static readonly nint s_shell32 = NativeLibrary.TryLoad("shell32.dll", out nint hShell) ? hShell : 0;

        private static readonly delegate* unmanaged[Stdcall]<Guid*, void*, uint, Guid*, void**, int> s_coCreateInstance =
            s_ole32 != 0 && NativeLibrary.TryGetExport(s_ole32, "CoCreateInstance", out nint p1)
                ? (delegate* unmanaged[Stdcall]<Guid*, void*, uint, Guid*, void**, int>)p1 : null;

        private static readonly delegate* unmanaged[Stdcall]<void*, void> s_coTaskMemFree =
            s_ole32 != 0 && NativeLibrary.TryGetExport(s_ole32, "CoTaskMemFree", out nint p2)
                ? (delegate* unmanaged[Stdcall]<void*, void>)p2 : null;

        private static readonly delegate* unmanaged[Stdcall]<char*, void*, Guid*, void**, int> s_shCreateItemFromParsingName =
            s_shell32 != 0 && NativeLibrary.TryGetExport(s_shell32, "SHCreateItemFromParsingName", out nint p3)
                ? (delegate* unmanaged[Stdcall]<char*, void*, Guid*, void**, int>)p3 : null;

        public static bool TryCreateFileDialog(bool isOpen, out void* dialog)
        {
            dialog = null;
            if (s_coCreateInstance == null)
            {
                return false;
            }

            Guid clsid = isOpen ? CLSID_FileOpenDialog : CLSID_FileSaveDialog;
            Guid iid = isOpen ? IID_IFileOpenDialog : IID_IFileSaveDialog;
            void* raw = null;
            int hr = s_coCreateInstance(&clsid, null, CLSCTX_INPROC_SERVER, &iid, &raw);
            if (hr < 0 || raw == null)
            {
                return false;
            }

            dialog = raw;
            return true;
        }

        public static void Release(void* comObj)
        {
            if (comObj != null)
            {
                ((delegate* unmanaged[Stdcall]<void*, uint>)(*(void***)comObj)[2])(comObj);
            }
        }

        // IModalWindow::Show (vtable slot 3)
        public static int Show(void* dialog, IntPtr hwndOwner)
            => ((delegate* unmanaged[Stdcall]<void*, IntPtr, int>)(*(void***)dialog)[3])(dialog, hwndOwner);

        // IFileDialog::SetFileTypes (vtable slot 4), SetFileTypeIndex (slot 5)
        public static void SetFileTypes(void* dialog, string filter, int filterIndex)
        {
            string[] parts = filter.Split('|');
            int count = parts.Length / 2;
            if (count <= 0) return;

            var specs = new COMDLG_FILTERSPEC[count];
            var handles = new GCHandle[count * 2];
            try
            {
                for (int i = 0; i < count; i++)
                {
                    handles[i * 2] = GCHandle.Alloc(parts[i * 2], GCHandleType.Pinned);
                    handles[i * 2 + 1] = GCHandle.Alloc(parts[i * 2 + 1], GCHandleType.Pinned);
                    specs[i].pszName = (char*)handles[i * 2].AddrOfPinnedObject();
                    specs[i].pszSpec = (char*)handles[i * 2 + 1].AddrOfPinnedObject();
                }

                fixed (COMDLG_FILTERSPEC* pSpecs = specs)
                {
                    ((delegate* unmanaged[Stdcall]<void*, uint, COMDLG_FILTERSPEC*, int>)(*(void***)dialog)[4])(dialog, (uint)count, pSpecs);
                }
                ((delegate* unmanaged[Stdcall]<void*, uint, int>)(*(void***)dialog)[5])(dialog, (uint)filterIndex);
            }
            finally
            {
                for (int i = 0; i < handles.Length; i++)
                {
                    if (handles[i].IsAllocated) handles[i].Free();
                }
            }
        }

        // IFileDialog::SetOptions (slot 9)
        public static void SetOptions(void* dialog, uint fos)
            => ((delegate* unmanaged[Stdcall]<void*, uint, int>)(*(void***)dialog)[9])(dialog, fos);

        // IFileDialog::SetFolder (slot 12)
        public static void SetFolder(void* dialog, string path)
        {
            void* shellItem = CreateShellItem(path);
            if (shellItem == null) return;
            try
            {
                ((delegate* unmanaged[Stdcall]<void*, void*, int>)(*(void***)dialog)[12])(dialog, shellItem);
            }
            finally
            {
                Release(shellItem);
            }
        }

        // IFileDialog::SetFileName (slot 15)
        public static void SetFileName(void* dialog, string fileName)
        {
            fixed (char* p = fileName)
            {
                ((delegate* unmanaged[Stdcall]<void*, char*, int>)(*(void***)dialog)[15])(dialog, p);
            }
        }

        // IFileDialog::SetTitle (slot 17)
        public static void SetTitle(void* dialog, string title)
        {
            fixed (char* p = title)
            {
                ((delegate* unmanaged[Stdcall]<void*, char*, int>)(*(void***)dialog)[17])(dialog, p);
            }
        }

        // IFileDialog::GetResult (slot 20)
        public static string GetSingleResult(void* dialog)
        {
            void* shellItem = null;
            int hr = ((delegate* unmanaged[Stdcall]<void*, void**, int>)(*(void***)dialog)[20])(dialog, &shellItem);
            if (hr < 0 || shellItem == null)
            {
                return null;
            }
            try
            {
                return GetShellItemPath(shellItem);
            }
            finally
            {
                Release(shellItem);
            }
        }

        // IFileDialog::AddPlace (slot 21)
        public static void AddPlace(void* dialog, string path)
        {
            void* shellItem = CreateShellItem(path);
            if (shellItem == null) return;
            try
            {
                ((delegate* unmanaged[Stdcall]<void*, void*, uint, int>)(*(void***)dialog)[21])(dialog, shellItem, FDAP_BOTTOM);
            }
            finally
            {
                Release(shellItem);
            }
        }

        // IFileDialog::SetDefaultExtension (slot 22)
        public static void SetDefaultExtension(void* dialog, string ext)
        {
            fixed (char* p = ext)
            {
                ((delegate* unmanaged[Stdcall]<void*, char*, int>)(*(void***)dialog)[22])(dialog, p);
            }
        }

        // IFileDialog::SetClientGuid (slot 24)
        public static void SetClientGuid(void* dialog, Guid guid)
            => ((delegate* unmanaged[Stdcall]<void*, Guid*, int>)(*(void***)dialog)[24])(dialog, &guid);

        // IFileOpenDialog::GetResults (slot 27), IShellItemArray::GetCount (slot 7), GetItemAt (slot 8)
        public static List<string> GetOpenResults(void* openDialog)
        {
            void* itemArray = null;
            int hr = ((delegate* unmanaged[Stdcall]<void*, void**, int>)(*(void***)openDialog)[27])(openDialog, &itemArray);
            if (hr < 0 || itemArray == null)
            {
                return null;
            }

            try
            {
                uint count = 0;
                hr = ((delegate* unmanaged[Stdcall]<void*, uint*, int>)(*(void***)itemArray)[7])(itemArray, &count);
                if (hr < 0)
                {
                    return null;
                }

                var list = new List<string>((int)count);
                for (uint i = 0; i < count; i++)
                {
                    void* item = null;
                    if (((delegate* unmanaged[Stdcall]<void*, uint, void**, int>)(*(void***)itemArray)[8])(itemArray, i, &item) >= 0 && item != null)
                    {
                        try
                        {
                            string path = GetShellItemPath(item);
                            if (!string.IsNullOrEmpty(path))
                            {
                                list.Add(path);
                            }
                        }
                        finally
                        {
                            Release(item);
                        }
                    }
                }
                return list;
            }
            finally
            {
                Release(itemArray);
            }
        }

        private static void* CreateShellItem(string path)
        {
            if (s_shCreateItemFromParsingName == null || string.IsNullOrEmpty(path))
            {
                return null;
            }

            Guid iid = IID_IShellItem;
            void* item = null;
            fixed (char* p = path)
            {
                int hr = s_shCreateItemFromParsingName(p, null, &iid, &item);
                return hr >= 0 ? item : null;
            }
        }

        // IShellItem::GetDisplayName (slot 5)
        private static string GetShellItemPath(void* shellItem)
        {
            char* pszName = null;
            int hr = ((delegate* unmanaged[Stdcall]<void*, uint, char**, int>)(*(void***)shellItem)[5])(shellItem, SIGDN_FILESYSPATH, &pszName);
            if (hr < 0 || pszName == null)
            {
                return null;
            }

            try
            {
                return new string(pszName);
            }
            finally
            {
                if (s_coTaskMemFree != null)
                {
                    s_coTaskMemFree(pszName);
                }
            }
        }
    }
}

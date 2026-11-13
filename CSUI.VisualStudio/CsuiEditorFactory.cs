using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace CSUI.VisualStudio
{
    [Guid(EditorFactoryGuidString)]
    public sealed class CsuiEditorFactory : IVsEditorFactory, IDisposable
    {
        public const string EditorFactoryGuidString = "A84B2F1D-6C3E-4A91-8D25-7E12C4A0B3F8";

        private readonly CsuiPackage _package;
        private ServiceProvider _vsServiceProvider;

        public CsuiEditorFactory(CsuiPackage package)
        {
            _package = package;
        }

        public int SetSite(IOleServiceProvider psp)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _vsServiceProvider = new ServiceProvider(psp);
            return VSConstants.S_OK;
        }

        public int MapLogicalView(ref Guid rguidLogicalView, out string pbstrPhysicalView)
        {
            pbstrPhysicalView = null;
            if (rguidLogicalView == VSConstants.LOGVIEWID.Primary_guid ||
                rguidLogicalView == VSConstants.LOGVIEWID.Designer_guid ||
                rguidLogicalView == VSConstants.LOGVIEWID.TextView_guid ||
                rguidLogicalView == VSConstants.LOGVIEWID.Code_guid)
            {
                return VSConstants.S_OK;
            }
            return VSConstants.E_NOTIMPL;
        }

        public int CreateEditorInstance(
            uint grfCreateDoc,
            string pszMkDocument,
            string pszPhysicalView,
            IVsHierarchy pvHier,
            uint itemid,
            IntPtr punkDocDataExisting,
            out IntPtr ppunkDocView,
            out IntPtr ppunkDocData,
            out string pbstrEditorCaption,
            out Guid pguidCmdUI,
            out int pgrfCDW)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ppunkDocView = IntPtr.Zero;
            ppunkDocData = IntPtr.Zero;
            pbstrEditorCaption = string.Empty;
            pguidCmdUI = Guid.Parse(EditorFactoryGuidString);
            pgrfCDW = 0;

            if ((grfCreateDoc & (VSConstants.CEF_OPENFILE | VSConstants.CEF_SILENT)) == 0)
            {
                return VSConstants.E_INVALIDARG;
            }

            // Only intercept .xaml files belonging to a CSUI project; let normal WPF projects use the standard VS designer.
            if (!IsCsuiProject(pvHier, pszMkDocument))
            {
                return VSConstants.VS_E_UNSUPPORTEDFORMAT;
            }

            var componentModel = (IComponentModel)_vsServiceProvider.GetService(typeof(SComponentModel));
            var adaptersFactory = componentModel?.GetService<IVsEditorAdaptersFactoryService>();
            if (adaptersFactory == null)
            {
                return VSConstants.E_FAIL;
            }

            IVsTextLines textLines = null;
            if (punkDocDataExisting != IntPtr.Zero)
            {
                object docDataObj = Marshal.GetObjectForIUnknown(punkDocDataExisting);
                textLines = docDataObj as IVsTextLines;
                if (textLines == null)
                {
                    return VSConstants.VS_E_INCOMPATIBLEDOCDATA;
                }
            }
            else
            {
                textLines = adaptersFactory.CreateVsTextBufferAdapter(_vsServiceProvider.GetService(typeof(IOleServiceProvider)) as IOleServiceProvider ?? (IOleServiceProvider)_package) as IVsTextLines;
                if (textLines == null)
                {
                    return VSConstants.E_FAIL;
                }
            }

            IVsCodeWindow codeWindow = adaptersFactory.CreateVsCodeWindowAdapter(_vsServiceProvider.GetService(typeof(IOleServiceProvider)) as IOleServiceProvider ?? (IOleServiceProvider)_package);
            if (codeWindow == null)
            {
                return VSConstants.E_FAIL;
            }

            codeWindow.SetBuffer(textLines);
            ErrorHandler.ThrowOnFailure(codeWindow.GetPrimaryView(out IVsTextView textView));

            IWpfTextViewHost wpfTextViewHost = adaptersFactory.GetWpfTextViewHost(textView);
            ITextBuffer textBuffer = adaptersFactory.GetDataBuffer(textLines);
            if (wpfTextViewHost == null || textBuffer == null)
            {
                return VSConstants.E_FAIL;
            }

            var designerPane = new CsuiDesignerPane(codeWindow, wpfTextViewHost, textBuffer, pszMkDocument);
            ppunkDocView = Marshal.GetIUnknownForObject(designerPane);
            ppunkDocData = Marshal.GetIUnknownForObject(textLines);
            pbstrEditorCaption = " [Design]";
            return VSConstants.S_OK;
        }

        private static bool IsCsuiProject(IVsHierarchy hierarchy, string xamlPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (hierarchy != null &&
                ErrorHandler.Succeeded(hierarchy.GetCanonicalName((uint)VSConstants.VSITEMID.Root, out string projectPath)) &&
                !string.IsNullOrEmpty(projectPath) &&
                File.Exists(projectPath) &&
                projectPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    string csproj = File.ReadAllText(projectPath);
                    if (csproj.IndexOf("CSUI.Core", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        csproj.IndexOf("CSUI.Markup", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                    return false;
                }
                catch
                {
                    // Fall through to path check
                }
            }

            if (!string.IsNullOrEmpty(xamlPath))
            {
                string dir = Path.GetDirectoryName(xamlPath);
                for (int i = 0; i < 4 && !string.IsNullOrEmpty(dir); i++)
                {
                    foreach (string csprojFile in Directory.GetFiles(dir, "*.csproj"))
                    {
                        try
                        {
                            string csproj = File.ReadAllText(csprojFile);
                            if (csproj.IndexOf("CSUI.Core", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                return true;
                            }
                        }
                        catch { }
                    }
                    dir = Path.GetDirectoryName(dir);
                }
            }

            return false;
        }

        public int Close() => VSConstants.S_OK;

        public void Dispose()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _vsServiceProvider?.Dispose();
            _vsServiceProvider = null;
        }
    }
}

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace CSUI.VisualStudio
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PackageGuidString)]
    [ProvideEditorFactory(typeof(CsuiEditorFactory), 101, TrustLevel = __VSEDITORTRUSTLEVEL.ETL_AlwaysTrusted)]
    [ProvideEditorExtension(typeof(CsuiEditorFactory), ".xaml", 100, NameResourceID = 101, DefaultName = "CSUI XAML Designer")]
    [ProvideEditorLogicalView(typeof(CsuiEditorFactory), VSConstants.LOGVIEWID.TextView_string)]
    [ProvideEditorLogicalView(typeof(CsuiEditorFactory), VSConstants.LOGVIEWID.Designer_string)]
    public sealed class CsuiPackage : AsyncPackage
    {
        public const string PackageGuidString = "9F3A7B1C-4D2E-4E8A-9C10-2A6B5C8D1E3F";

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            RegisterEditorFactory(new CsuiEditorFactory(this));
        }
    }
}

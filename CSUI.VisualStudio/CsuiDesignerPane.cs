using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace CSUI.VisualStudio
{
    internal sealed class CsuiDesignerPane : WindowPane, IVsCodeWindow
    {
        private readonly IVsCodeWindow _codeWindow;
        private readonly CsuiDesignerView _view;

        public CsuiDesignerPane(
            IVsCodeWindow codeWindow,
            IWpfTextViewHost wpfTextViewHost,
            ITextBuffer textBuffer,
            string filePath) : base(null)
        {
            _codeWindow = codeWindow;
            _view = new CsuiDesignerView(wpfTextViewHost, textBuffer, filePath);
            Content = _view;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _view?.Dispose();
                if (_codeWindow is IDisposable d)
                {
                    d.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        // Delegate IVsCodeWindow to the underlying native VS code window
        public int SetBuffer(IVsTextLines pBuffer) => _codeWindow.SetBuffer(pBuffer);
        public int GetBuffer(out IVsTextLines ppBuffer) => _codeWindow.GetBuffer(out ppBuffer);
        public int GetPrimaryView(out IVsTextView ppView) => _codeWindow.GetPrimaryView(out ppView);
        public int GetSecondaryView(out IVsTextView ppView) => _codeWindow.GetSecondaryView(out ppView);
        public int SetViewClassID(ref Guid clsidView) => _codeWindow.SetViewClassID(ref clsidView);
        public int GetViewClassID(out Guid pclsidView) => _codeWindow.GetViewClassID(out pclsidView);
        public int SetBaseEditorCaption(string[] pszBaseEditorCaption) => _codeWindow.SetBaseEditorCaption(pszBaseEditorCaption);
        public int GetEditorCaption(READONLYSTATUS dwReadOnly, out string pbstrEditorCaption) => _codeWindow.GetEditorCaption(dwReadOnly, out pbstrEditorCaption);
        public int Close() => _codeWindow.Close();
        public int GetLastActiveView(out IVsTextView ppView) => _codeWindow.GetLastActiveView(out ppView);
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CSUI.VisualStudio
{
    internal sealed class PreviewFrameData
    {
        public int RequestId { get; set; }
        public string DocumentTitle { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Stride { get; set; }
        public byte[] Pixels { get; set; }
    }

    internal sealed class PreviewerClient : IDisposable
    {
        private const byte MsgRenderXaml = 1;
        private const byte MsgShutdown = 255;

        private const byte RespFrame = 1;
        private const byte RespError = 2;

        private readonly string _xamlFilePath;
        private readonly object _sendLock = new object();
        private Process _process;
        private BinaryWriter _writer;
        private CancellationTokenSource _cts;
        private int _nextRequestId;
        private bool _disposed;

        public event Action<PreviewFrameData> FrameReceived;
        public event Action<string> ErrorReceived;

        public PreviewerClient(string xamlFilePath)
        {
            _xamlFilePath = xamlFilePath;
        }

        public void RequestRender(string xaml, double dpiScale, int maxWidth, int maxHeight)
        {
            if (_disposed || string.IsNullOrEmpty(xaml))
            {
                return;
            }

            try
            {
                EnsureStarted();
                if (_writer == null)
                {
                    return;
                }

                int reqId = Interlocked.Increment(ref _nextRequestId);
                lock (_sendLock)
                {
                    _writer.Write(MsgRenderXaml);
                    _writer.Write(reqId);
                    _writer.Write(dpiScale);
                    _writer.Write(maxWidth);
                    _writer.Write(maxHeight);
                    _writer.Write(xaml);
                    _writer.Flush();
                }
            }
            catch (Exception ex)
            {
                RestartProcess();
                ErrorReceived?.Invoke($"Previewer IPC error: {ex.Message}");
            }
        }

        private void EnsureStarted()
        {
            if (_process != null && !_process.HasExited)
            {
                return;
            }

            string executable = ResolvePreviewerExecutable(out string arguments);
            if (executable == null)
            {
                ErrorReceived?.Invoke("CSUI.Previewer.exe not found. Build CSUI.Previewer (Release) in the solution or bundle it with the VSIX.");
                return;
            }

            var psi = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(executable) ?? Environment.CurrentDirectory
            };

            _process = Process.Start(psi);
            if (_process == null)
            {
                ErrorReceived?.Invoke("Failed to launch CSUI.Previewer.");
                return;
            }

            _writer = new BinaryWriter(_process.StandardInput.BaseStream);
            _cts = new CancellationTokenSource();
            var reader = new BinaryReader(_process.StandardOutput.BaseStream);
            CancellationToken token = _cts.Token;

            _ = Task.Run(() => ReadLoop(reader, token), token);
        }

        private void ReadLoop(BinaryReader reader, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    int respType = reader.BaseStream.ReadByte();
                    if (respType < 0)
                    {
                        break;
                    }

                    int reqId = reader.ReadInt32();
                    if (respType == RespFrame)
                    {
                        string title = reader.ReadString();
                        int width = reader.ReadInt32();
                        int height = reader.ReadInt32();
                        int stride = reader.ReadInt32();
                        int byteCount = reader.ReadInt32();
                        byte[] pixels = reader.ReadBytes(byteCount);
                        if (pixels.Length == byteCount)
                        {
                            FrameReceived?.Invoke(new PreviewFrameData
                            {
                                RequestId = reqId,
                                DocumentTitle = title,
                                Width = width,
                                Height = height,
                                Stride = stride,
                                Pixels = pixels
                            });
                        }
                    }
                    else if (respType == RespError)
                    {
                        string err = reader.ReadString();
                        ErrorReceived?.Invoke(err);
                    }
                }
            }
            catch
            {
                // Process exited or disposed
            }
        }

        private string ResolvePreviewerExecutable(out string arguments)
        {
            // 1. Check alongside the VSIX extension assembly (Previewer\CSUI.Previewer.exe)
            string extDir = Path.GetDirectoryName(typeof(PreviewerClient).Assembly.Location);
            if (!string.IsNullOrEmpty(extDir))
            {
                string bundledExe = Path.Combine(extDir, "Previewer", "CSUI.Previewer.exe");
                if (File.Exists(bundledExe))
                {
                    arguments = "--ipc";
                    return bundledExe;
                }
            }

            // 2. Walk up from the .xaml file's directory to locate CSUI.Previewer build output
            string dir = !string.IsNullOrEmpty(_xamlFilePath) ? Path.GetDirectoryName(_xamlFilePath) : null;
            for (int depth = 0; depth < 8 && !string.IsNullOrEmpty(dir); depth++)
            {
                string[] candidates =
                {
                    Path.Combine(dir, "CSUI.Previewer", "bin", "Release", "net10.0", "win-x64", "CSUI.Previewer.exe"),
                    Path.Combine(dir, "CSUI.Previewer", "bin", "Debug", "net10.0", "win-x64", "CSUI.Previewer.exe"),
                    Path.Combine(dir, "CSUI.Previewer", "bin", "Release", "net10.0", "CSUI.Previewer.exe"),
                    Path.Combine(dir, "CSUI.Previewer", "bin", "Debug", "net10.0", "CSUI.Previewer.exe")
                };

                foreach (string candidate in candidates)
                {
                    if (File.Exists(candidate))
                    {
                        arguments = "--ipc";
                        return candidate;
                    }
                }

                string[] dllCandidates =
                {
                    Path.Combine(dir, "CSUI.Previewer", "bin", "Release", "net10.0", "win-x64", "CSUI.Previewer.dll"),
                    Path.Combine(dir, "CSUI.Previewer", "bin", "Debug", "net10.0", "win-x64", "CSUI.Previewer.dll")
                };

                foreach (string dll in dllCandidates)
                {
                    if (File.Exists(dll))
                    {
                        arguments = $"exec \"{dll}\" --ipc";
                        return "dotnet";
                    }
                }

                dir = Path.GetDirectoryName(dir);
            }

            arguments = null;
            return null;
        }

        private void RestartProcess()
        {
            try { _cts?.Cancel(); } catch { }
            try { _writer?.Dispose(); } catch { }
            try
            {
                if (_process != null && !_process.HasExited)
                {
                    _process.Kill();
                }
                _process?.Dispose();
            }
            catch { }

            _writer = null;
            _process = null;
            _cts = null;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                if (_writer != null && _process != null && !_process.HasExited)
                {
                    lock (_sendLock)
                    {
                        _writer.Write(MsgShutdown);
                        _writer.Flush();
                    }
                }
            }
            catch { }

            RestartProcess();
        }
    }
}

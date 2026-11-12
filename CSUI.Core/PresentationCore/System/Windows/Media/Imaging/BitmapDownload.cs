// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Cache;
using System.Threading;
using System.Windows.Threading;
using MS.Internal;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media.Imaging
{
    internal class QueueEntry
    {
        internal List<WeakReference> decoders;
        internal Uri inputUri;
        internal Stream inputStream;
        internal FileStream outputStream;
        internal string tempFilePath;
        internal RequestCachePolicy uriCachePolicy;
        internal string contentType;
        internal bool isLocalTempFile;
        internal long contentLength;
        internal int lastPercent;
    }

    internal static class BitmapDownload
    {
        static BitmapDownload()
        {
            _thread = new Thread(DownloadThreadProc);
            _uriTable = Hashtable.Synchronized(new Hashtable());
            _workQueue = Queue.Synchronized(new Queue());
            _waitEvent = new AutoResetEvent(false);
            _progressCallback = new DispatcherOperationCallback(ProgressCallback);
            _downloadCallback = new DispatcherOperationCallback(DownloadCallback);
            _failedCallback = new DispatcherOperationCallback(ExceptionCallback);
            _syncLock = new object();
        }

        private static void WaitForStart()
        {
            while (true)
            {
                ThreadStartState startState = (ThreadStartState)Interlocked.CompareExchange(
                    ref _threadStartState,
                    (int)ThreadStartState.Starting,
                    (int)ThreadStartState.Unstarted);

                if (startState == ThreadStartState.Unstarted)
                {
                    _thread.IsBackground = true;
                    _thread.Start();
                    _threadStartState = (int)ThreadStartState.Started;
                    break;
                }
                else if (startState == ThreadStartState.Started)
                {
                    break;
                }
                else
                {
                    Thread.Sleep(0);
                }
            }
        }

        internal static void BeginDownload(
            BitmapDecoder decoder,
            Uri uri,
            RequestCachePolicy uriCachePolicy,
            Stream stream)
        {
            WaitForStart();

            QueueEntry entry;
            lock (_syncLock)
            {
                if (uri != null)
                {
                    entry = (QueueEntry)_uriTable[uri];
                    if (entry != null)
                    {
                        entry.decoders.Add(new WeakReference(decoder));
                        return;
                    }
                }

                entry = new QueueEntry
                {
                    decoders = new List<WeakReference>(1) { new WeakReference(decoder) },
                    inputUri = uri,
                    inputStream = stream,
                    uriCachePolicy = uriCachePolicy,
                    lastPercent = 0,
                    contentLength = -1,
                };

                if (uri != null)
                {
                    _uriTable[uri] = entry;
                }

                _workQueue.Enqueue(entry);
            }

            _waitEvent.Set();
        }

        private static QueueEntry GetNextEntry()
        {
            lock (_syncLock)
            {
                if (_workQueue.Count > 0)
                {
                    return (QueueEntry)_workQueue.Dequeue();
                }

                return null;
            }
        }

        private static void DownloadThreadProc()
        {
            while (true)
            {
                _waitEvent.WaitOne();

                while (true)
                {
                    QueueEntry entry = GetNextEntry();
                    if (entry == null)
                    {
                        break;
                    }

                    ProcessEntry(entry);
                }
            }
        }

        private static void ProcessEntry(QueueEntry entry)
        {
            try
            {
                if (entry.inputStream != null)
                {
                    CopyFromInputStream(entry);
                }
                else if (entry.inputUri != null)
                {
                    DownloadFromUri(entry);
                }
                else
                {
                    throw new ArgumentException(SR.Format(SR.Image_NeitherArgument, "uri", "stream"));
                }

                if (entry.outputStream != null && entry.outputStream.CanSeek)
                {
                    entry.outputStream.Seek(0, SeekOrigin.Begin);
                }

                lock (_syncLock)
                {
                    if (entry.inputUri != null)
                    {
                        _uriTable.Remove(entry.inputUri);
                    }
                }

                MarshalEvents(_downloadCallback, entry.decoders, entry);
            }
            catch (Exception e)
            {
                try
                {
                    entry.outputStream?.Dispose();
                    entry.outputStream = null;
                    CleanupTempFile(entry);
                }
                catch
                {
                }

                lock (_syncLock)
                {
                    if (entry.inputUri != null)
                    {
                        _uriTable.Remove(entry.inputUri);
                    }
                }

                MarshalEvents(_failedCallback, entry.decoders, e);
            }
        }

        private static FileStream CreateTempOutputStream(QueueEntry entry)
        {
            string tempPath = Path.Combine(Path.GetTempPath(), $"csui_img_{Guid.NewGuid():N}.tmp");
            entry.tempFilePath = tempPath;
            entry.isLocalTempFile = true;
            return new FileStream(
                tempPath,
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.Read | FileShare.Delete,
                READ_SIZE);
        }

        private static void CleanupTempFile(QueueEntry entry)
        {
            if (entry.isLocalTempFile && !string.IsNullOrEmpty(entry.tempFilePath))
            {
                try
                {
                    if (File.Exists(entry.tempFilePath))
                    {
                        File.Delete(entry.tempFilePath);
                    }
                }
                catch
                {
                }
                entry.tempFilePath = null;
            }
        }

        private static void CopyFromInputStream(QueueEntry entry)
        {
            FileStream output;
            try
            {
                output = CreateTempOutputStream(entry);
            }
            catch (Exception e)
            {
                throw new IOException(SR.Image_CannotCreateTempFile, e);
            }

            entry.outputStream = output;

            long totalLength = -1;
            if (entry.inputStream.CanSeek)
            {
                try
                {
                    totalLength = entry.inputStream.Length;
                }
                catch
                {
                    totalLength = -1;
                }
            }
            entry.contentLength = totalLength;

            byte[] buffer = new byte[READ_SIZE];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = entry.inputStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, bytesRead);
                totalRead += bytesRead;
                ReportProgress(entry, totalRead, totalLength);
            }

            output.Flush();
        }

        private static void DownloadFromUri(QueueEntry entry)
        {
            Uri uri = entry.inputUri;

            if (uri.IsFile)
            {
                using FileStream source = new FileStream(uri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                entry.outputStream = CreateTempOutputStream(entry);
                entry.contentLength = source.Length;

                byte[] buffer = new byte[READ_SIZE];
                long totalRead = 0;
                int bytesRead;
                while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
                {
                    entry.outputStream.Write(buffer, 0, bytesRead);
                    totalRead += bytesRead;
                    ReportProgress(entry, totalRead, entry.contentLength);
                }

                entry.outputStream.Flush();
                return;
            }

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
            if (entry.uriCachePolicy != null)
            {
                if (entry.uriCachePolicy.Level == RequestCacheLevel.NoCacheNoStore ||
                    entry.uriCachePolicy.Level == RequestCacheLevel.Reload)
                {
                    request.Headers.CacheControl = new CacheControlHeaderValue
                    {
                        NoCache = true,
                        NoStore = entry.uriCachePolicy.Level == RequestCacheLevel.NoCacheNoStore
                    };
                }
                else if (entry.uriCachePolicy.Level == RequestCacheLevel.CacheOnly)
                {
                    request.Headers.CacheControl = new CacheControlHeaderValue { OnlyIfCached = true };
                }
            }

            using HttpResponseMessage response = s_httpClient.Send(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            entry.contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
            long totalLength = response.Content.Headers.ContentLength ?? -1;
            entry.contentLength = totalLength;

            try
            {
                entry.outputStream = CreateTempOutputStream(entry);
            }
            catch (Exception e)
            {
                throw new IOException(SR.Image_CannotCreateTempFile, e);
            }

            using Stream networkStream = response.Content.ReadAsStream();
            byte[] downloadBuffer = new byte[READ_SIZE];
            long totalDownloaded = 0;
            int chunk;

            while ((chunk = networkStream.Read(downloadBuffer, 0, downloadBuffer.Length)) > 0)
            {
                entry.outputStream.Write(downloadBuffer, 0, chunk);
                totalDownloaded += chunk;
                ReportProgress(entry, totalDownloaded, totalLength);
            }

            entry.outputStream.Flush();
        }

        private static void ReportProgress(QueueEntry entry, long totalRead, long totalLength)
        {
            int percent;
            if (totalLength > 0)
            {
                percent = (int)Math.Clamp((totalRead * 100L) / totalLength, 1L, 100L);
            }
            else
            {
                percent = Math.Min(99, entry.lastPercent + 1);
            }

            if (percent > entry.lastPercent)
            {
                entry.lastPercent = percent;
                MarshalEvents(_progressCallback, entry.decoders, percent);
            }
        }

        private static void MarshalEvents(DispatcherOperationCallback doc, List<WeakReference> decoders, params object[] args)
        {
            Debug.Assert(args.Length == 1);

            lock (_syncLock)
            {
                Hashtable table = new Hashtable();

                for (int i = 0; i < decoders.Count; i++)
                {
                    WeakReference decoderRef = decoders[i];
                    BitmapDecoder decoder = decoderRef.Target as BitmapDecoder;

                    if (decoder != null)
                    {
                        Dispatcher dispatcher = decoder.Dispatcher;

                        ArrayList list = (ArrayList)table[dispatcher];
                        if (list == null)
                        {
                            list = new ArrayList();
                            table[dispatcher] = list;
                        }
                        list.Add(decoderRef);
                    }
                }

                foreach (DictionaryEntry tableEntry in table)
                {
                    Dispatcher dispatcher = (Dispatcher)tableEntry.Key;
                    ArrayList list = (ArrayList)tableEntry.Value;

                    object[] callbackArgs = new object[] { list, args[0] };
                    dispatcher.BeginInvoke(DispatcherPriority.Normal, doc, callbackArgs);
                }
            }
        }

        private static object ProgressCallback(object arg)
        {
            object[] args = (object[])arg;
            ArrayList list = (ArrayList)args[0];
            int percentComplete = (int)args[1];

            foreach (WeakReference decoderRef in list)
            {
                if (decoderRef.Target is LateBoundBitmapDecoder decoder)
                {
                    decoder.ProgressCallback(percentComplete);
                }
            }

            return null;
        }

        private static object DownloadCallback(object arg)
        {
            object[] args = (object[])arg;
            ArrayList list = (ArrayList)args[0];
            QueueEntry entry = (QueueEntry)args[1];

            try
            {
                Stream sharedStream = entry.outputStream;
                int liveCount = 0;
                for (int i = 0; i < list.Count; i++)
                {
                    if (((WeakReference)list[i]).Target is LateBoundBitmapDecoder)
                    {
                        liveCount++;
                    }
                }

                int current = 0;
                foreach (WeakReference decoderRef in list)
                {
                    if (decoderRef.Target is LateBoundBitmapDecoder decoder)
                    {
                        current++;
                        Stream decoderStream;
                        if (current == liveCount && sharedStream != null)
                        {
                            sharedStream.Seek(0, SeekOrigin.Begin);
                            decoderStream = sharedStream;
                            entry.outputStream = null;
                        }
                        else if (!string.IsNullOrEmpty(entry.tempFilePath) && File.Exists(entry.tempFilePath))
                        {
                            decoderStream = new FileStream(
                                entry.tempFilePath,
                                FileMode.Open,
                                FileAccess.Read,
                                FileShare.Read | FileShare.Delete);
                        }
                        else
                        {
                            byte[] copy = File.ReadAllBytes(entry.tempFilePath);
                            decoderStream = new MemoryStream(copy, writable: false);
                        }

                        decoder.DownloadCallback(decoderStream, entry.tempFilePath, entry.contentType);
                    }
                }
            }
            finally
            {
                entry.outputStream?.Dispose();
                entry.outputStream = null;
                CleanupTempFile(entry);
            }

            return null;
        }

        private static object ExceptionCallback(object arg)
        {
            object[] args = (object[])arg;
            ArrayList list = (ArrayList)args[0];
            Exception e = (Exception)args[1];

            foreach (WeakReference decoderRef in list)
            {
                if (decoderRef.Target is LateBoundBitmapDecoder decoder)
                {
                    decoder.ExceptionCallback(e);
                }
            }

            return null;
        }

        private enum ThreadStartState : int
        {
            Unstarted = 0,
            Starting = 1,
            Started = 2
        }

        private static readonly HttpClient s_httpClient = new HttpClient();
        internal static Thread _thread;
        internal static int _threadStartState;
        internal static Hashtable _uriTable;
        internal static Queue _workQueue;
        internal static AutoResetEvent _waitEvent;
        internal static DispatcherOperationCallback _progressCallback;
        internal static DispatcherOperationCallback _downloadCallback;
        internal static DispatcherOperationCallback _failedCallback;
        internal static object _syncLock;

        private const int READ_SIZE = 1024;
    }
}

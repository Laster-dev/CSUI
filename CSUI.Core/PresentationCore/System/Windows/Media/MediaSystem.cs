// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable
using SR = MS.Internal.PresentationCore.SR;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;
using CSUI.Core.Interop.DirectX;
using MS.Internal;

namespace System.Windows.Media
{
    /// <summary>
    /// The MediaSystem class controls the Direct2D/DirectWrite media layer
    /// and maintains AppDomain-wide MediaContext registration.
    /// </summary>
    internal static class MediaSystem
    {
        private static readonly object s_lock = new object();
        private static int s_refCount;
        private static readonly List<MediaContext> s_mediaContexts = new List<MediaContext>();
        private static bool s_isConnected;
        private static bool s_disableDirtyRectangles;
        private static bool s_animationSmoothing = true;
        private static bool s_isSoftwareRenderingForcedForProcess;
        private static DxSharedDevice s_sharedDevice;

        /// <summary>
        /// Initializes the MediaSystem for the specified MediaContext.
        /// </summary>
        public static bool Startup(MediaContext mc)
        {
            ArgumentNullException.ThrowIfNull(mc);

            lock (s_lock)
            {
                if (!s_mediaContexts.Contains(mc))
                {
                    s_mediaContexts.Add(mc);
                }

                if (s_refCount == 0)
                {
                    s_isConnected = true;
                }

                s_refCount++;
            }

            return true;
        }

        /// <summary>
        /// Deinitializes the MediaSystem for the specified MediaContext.
        /// </summary>
        internal static void Shutdown(MediaContext mc)
        {
            lock (s_lock)
            {
                Debug.Assert(s_refCount > 0);
                s_mediaContexts.Remove(mc);
                s_refCount--;

                if (s_refCount == 0)
                {
                    s_isConnected = false;
                    s_sharedDevice?.Dispose();
                    s_sharedDevice = null;
                }
            }
        }

        /// <summary>
        /// Ensures and returns the shared D3D11/DXGI/Direct2D1/DirectWrite device bundle.
        /// </summary>
        internal static DxSharedDevice EnsureSharedDevice()
        {
            lock (s_lock)
            {
                s_sharedDevice ??= new DxSharedDevice();
                return s_sharedDevice;
            }
        }

        /// <summary>
        /// Forces recreation of the shared Direct2D device bundle after device loss/reset.
        /// </summary>
        internal static DxSharedDevice RecreateSharedDevice()
        {
            lock (s_lock)
            {
                if (s_sharedDevice == null)
                {
                    s_sharedDevice = new DxSharedDevice();
                }
                else
                {
                    s_sharedDevice.Recreate();
                }

                return s_sharedDevice;
            }
        }

        /// <summary>
        /// If the app has asked to disable dirty-rect processing, notify all contexts.
        /// </summary>
        internal static void PropagateDirtyRectangleSettings()
        {
            bool oldValue = s_disableDirtyRectangles;
            bool disableDirtyRectangles = CoreAppContextSwitches.DisableDirtyRectangles;

            if (disableDirtyRectangles != oldValue)
            {
                if (Interlocked.CompareExchange(ref s_disableDirtyRectangles, disableDirtyRectangles, oldValue) == oldValue)
                {
                    NotifyRedirectionEnvironmentChanged();
                }
            }
        }

        internal static bool DisableDirtyRectangles => s_disableDirtyRectangles;

        /// <summary>
        /// Notifies registered MediaContexts that the rendering environment or process render mode changed.
        /// </summary>
        internal static void NotifyRedirectionEnvironmentChanged()
        {
            lock (s_lock)
            {
                foreach (MediaContext mc in s_mediaContexts)
                {
                    mc.PostInvalidateRenderMode();
                }
            }
        }

        /// <summary>
        /// Checks if two DispatcherObjects have the same context affinity.
        /// </summary>
        internal static void AssertSameContext(
            DispatcherObject reference,
            DispatcherObject other)
        {
            Debug.Assert(reference != null, "The reference object can not be null.");

            if (other != null &&
                reference.Dispatcher != null &&
                other.Dispatcher != null &&
                reference.Dispatcher != other.Dispatcher)
            {
                throw new ArgumentException(SR.MediaSystem_ApiInvalidContext);
            }
        }

        internal static bool IsTransportConnected
        {
            get { return s_isConnected; }
            set { s_isConnected = value; }
        }

        internal static bool ForceSoftwareRendering
        {
            get
            {
                lock (s_lock)
                {
                    return s_isSoftwareRenderingForcedForProcess;
                }
            }
        }

        internal static bool IsSoftwareRenderingForcedForProcess
        {
            get
            {
                lock (s_lock)
                {
                    return s_isSoftwareRenderingForcedForProcess;
                }
            }
            set
            {
                bool changed = false;
                lock (s_lock)
                {
                    if (s_isSoftwareRenderingForcedForProcess != value)
                    {
                        s_isSoftwareRenderingForcedForProcess = value;
                        changed = true;
                    }
                }

                if (changed)
                {
                    NotifyRedirectionEnvironmentChanged();
                }
            }
        }

        internal static bool AnimationSmoothing
        {
            get { return s_animationSmoothing; }
        }
    }
}

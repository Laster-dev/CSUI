// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Ole;
using MS.Internal;
using MS.Internal.PresentationCore;
using MS.Win32;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows
{
    /// <summary>
    /// Provides helper methods and attached events for Drag-and-Drop operations.
    /// </summary>
    public static class DragDrop
    {
        #region Attached Routed Events

        /// <summary>PreviewQueryContinueDrag</summary>
        public static readonly RoutedEvent PreviewQueryContinueDragEvent = EventManager.RegisterRoutedEvent("PreviewQueryContinueDrag", RoutingStrategy.Tunnel, typeof(QueryContinueDragEventHandler), typeof(DragDrop));

        /// <summary>Adds a handler for the PreviewQueryContinueDrag attached event.</summary>
        public static void AddPreviewQueryContinueDragHandler(DependencyObject element, QueryContinueDragEventHandler handler)
        {
            UIElement.AddHandler(element, PreviewQueryContinueDragEvent, handler);
        }

        /// <summary>Removes a handler for the PreviewQueryContinueDrag attached event.</summary>
        public static void RemovePreviewQueryContinueDragHandler(DependencyObject element, QueryContinueDragEventHandler handler)
        {
            UIElement.RemoveHandler(element, PreviewQueryContinueDragEvent, handler);
        }

        /// <summary>QueryContinueDrag</summary>
        public static readonly RoutedEvent QueryContinueDragEvent = EventManager.RegisterRoutedEvent("QueryContinueDrag", RoutingStrategy.Bubble, typeof(QueryContinueDragEventHandler), typeof(DragDrop));

        /// <summary>Adds a handler for the QueryContinueDrag attached event.</summary>
        public static void AddQueryContinueDragHandler(DependencyObject element, QueryContinueDragEventHandler handler)
        {
            UIElement.AddHandler(element, QueryContinueDragEvent, handler);
        }

        /// <summary>Removes a handler for the QueryContinueDrag attached event.</summary>
        public static void RemoveQueryContinueDragHandler(DependencyObject element, QueryContinueDragEventHandler handler)
        {
            UIElement.RemoveHandler(element, QueryContinueDragEvent, handler);
        }

        /// <summary>PreviewGiveFeedback</summary>
        public static readonly RoutedEvent PreviewGiveFeedbackEvent = EventManager.RegisterRoutedEvent("PreviewGiveFeedback", RoutingStrategy.Tunnel, typeof(GiveFeedbackEventHandler), typeof(DragDrop));

        /// <summary>Adds a handler for the PreviewGiveFeedback attached event.</summary>
        public static void AddPreviewGiveFeedbackHandler(DependencyObject element, GiveFeedbackEventHandler handler)
        {
            UIElement.AddHandler(element, PreviewGiveFeedbackEvent, handler);
        }

        /// <summary>Removes a handler for the PreviewGiveFeedback attached event.</summary>
        public static void RemovePreviewGiveFeedbackHandler(DependencyObject element, GiveFeedbackEventHandler handler)
        {
            UIElement.RemoveHandler(element, PreviewGiveFeedbackEvent, handler);
        }

        /// <summary>GiveFeedback</summary>
        public static readonly RoutedEvent GiveFeedbackEvent = EventManager.RegisterRoutedEvent("GiveFeedback", RoutingStrategy.Bubble, typeof(GiveFeedbackEventHandler), typeof(DragDrop));

        /// <summary>Adds a handler for the GiveFeedback attached event.</summary>
        public static void AddGiveFeedbackHandler(DependencyObject element, GiveFeedbackEventHandler handler)
        {
            UIElement.AddHandler(element, GiveFeedbackEvent, handler);
        }

        /// <summary>Removes a handler for the GiveFeedback attached event.</summary>
        public static void RemoveGiveFeedbackHandler(DependencyObject element, GiveFeedbackEventHandler handler)
        {
            UIElement.RemoveHandler(element, GiveFeedbackEvent, handler);
        }

        /// <summary>PreviewDragEnter</summary>
        public static readonly RoutedEvent PreviewDragEnterEvent = EventManager.RegisterRoutedEvent("PreviewDragEnter", RoutingStrategy.Tunnel, typeof(DragEventHandler), typeof(DragDrop));

        /// <summary>Adds a handler for the PreviewDragEnter attached event.</summary>
        public static void AddPreviewDragEnterHandler(DependencyObject element, DragEventHandler handler)
        {
            UIElement.AddHandler(element, PreviewDragEnterEvent, handler);
        }

        /// <summary>Removes a handler for the PreviewDragEnter attached event.</summary>
        public static void RemovePreviewDragEnterHandler(DependencyObject element, DragEventHandler handler)
        {
            UIElement.RemoveHandler(element, PreviewDragEnterEvent, handler);
        }

        /// <summary>DragEnter</summary>
        public static readonly RoutedEvent DragEnterEvent = EventManager.RegisterRoutedEvent("DragEnter", RoutingStrategy.Bubble, typeof(DragEventHandler), typeof(DragDrop));

        /// <summary>Adds a handler for the DragEnter attached event.</summary>
        public static void AddDragEnterHandler(DependencyObject element, DragEventHandler handler)
        {
            UIElement.AddHandler(element, DragEnterEvent, handler);
        }

        /// <summary>Removes a handler for the DragEnter attached event.</summary>
        public static void RemoveDragEnterHandler(DependencyObject element, DragEventHandler handler)
        {
            UIElement.RemoveHandler(element, DragEnterEvent, handler);
        }

        /// <summary>PreviewDragOver</summary>
        public static readonly RoutedEvent PreviewDragOverEvent = EventManager.RegisterRoutedEvent("PreviewDragOver", RoutingStrategy.Tunnel, typeof(DragEventHandler), typeof(DragDrop));

        /// <summary>Adds a handler for the PreviewDragOver attached event.</summary>
        public static void AddPreviewDragOverHandler(DependencyObject element, DragEventHandler handler)
        {
            UIElement.AddHandler(element, PreviewDragOverEvent, handler);
        }

        /// <summary>Removes a handler for the PreviewDragOver attached event.</summary>
        public static void RemovePreviewDragOverHandler(DependencyObject element, DragEventHandler handler)
        {
            UIElement.RemoveHandler(element, PreviewDragOverEvent, handler);
        }

        /// <summary>DragOver</summary>
        public static readonly RoutedEvent DragOverEvent = EventManager.RegisterRoutedEvent("DragOver", RoutingStrategy.Bubble, typeof(DragEventHandler), typeof(DragDrop));

        /// <summary>Adds a handler for the DragOver attached event.</summary>
        public static void AddDragOverHandler(DependencyObject element, DragEventHandler handler)
        {
            UIElement.AddHandler(element, DragOverEvent, handler);
        }

        /// <summary>Removes a handler for the DragOver attached event.</summary>
        public static void RemoveDragOverHandler(DependencyObject element, DragEventHandler handler)
        {
            UIElement.RemoveHandler(element, DragOverEvent, handler);
        }

        /// <summary>PreviewDragLeave</summary>
        public static readonly RoutedEvent PreviewDragLeaveEvent = EventManager.RegisterRoutedEvent("PreviewDragLeave", RoutingStrategy.Tunnel, typeof(DragEventHandler), typeof(DragDrop));

        /// <summary>Adds a handler for the PreviewDragLeave attached event.</summary>
        public static void AddPreviewDragLeaveHandler(DependencyObject element, DragEventHandler handler)
        {
            UIElement.AddHandler(element, PreviewDragLeaveEvent, handler);
        }

        /// <summary>Removes a handler for the PreviewDragLeave attached event.</summary>
        public static void RemovePreviewDragLeaveHandler(DependencyObject element, DragEventHandler handler)
        {
            UIElement.RemoveHandler(element, PreviewDragLeaveEvent, handler);
        }

        /// <summary>DragLeave</summary>
        public static readonly RoutedEvent DragLeaveEvent = EventManager.RegisterRoutedEvent("DragLeave", RoutingStrategy.Bubble, typeof(DragEventHandler), typeof(DragDrop));

        /// <summary>Adds a handler for the DragLeave attached event.</summary>
        public static void AddDragLeaveHandler(DependencyObject element, DragEventHandler handler)
        {
            UIElement.AddHandler(element, DragLeaveEvent, handler);
        }

        /// <summary>Removes a handler for the DragLeave attached event.</summary>
        public static void RemoveDragLeaveHandler(DependencyObject element, DragEventHandler handler)
        {
            UIElement.RemoveHandler(element, DragLeaveEvent, handler);
        }

        /// <summary>PreviewDrop</summary>
        public static readonly RoutedEvent PreviewDropEvent = EventManager.RegisterRoutedEvent("PreviewDrop", RoutingStrategy.Tunnel, typeof(DragEventHandler), typeof(DragDrop));

        /// <summary>Adds a handler for the PreviewDrop attached event.</summary>
        public static void AddPreviewDropHandler(DependencyObject element, DragEventHandler handler)
        {
            UIElement.AddHandler(element, PreviewDropEvent, handler);
        }

        /// <summary>Removes a handler for the PreviewDrop attached event.</summary>
        public static void RemovePreviewDropHandler(DependencyObject element, DragEventHandler handler)
        {
            UIElement.RemoveHandler(element, PreviewDropEvent, handler);
        }

        /// <summary>Drop</summary>
        public static readonly RoutedEvent DropEvent = EventManager.RegisterRoutedEvent("Drop", RoutingStrategy.Bubble, typeof(DragEventHandler), typeof(DragDrop));

        /// <summary>Adds a handler for the Drop attached event.</summary>
        public static void AddDropHandler(DependencyObject element, DragEventHandler handler)
        {
            UIElement.AddHandler(element, DropEvent, handler);
        }

        /// <summary>Removes a handler for the Drop attached event.</summary>
        public static void RemoveDropHandler(DependencyObject element, DragEventHandler handler)
        {
            UIElement.RemoveHandler(element, DropEvent, handler);
        }

        #endregion

        #region Public API & Registration

        /// <summary>Initiates a drag-and-drop operation.</summary>
        public static DragDropEffects DoDragDrop(DependencyObject dragSource, object data, DragDropEffects allowedEffects)
        {
            ArgumentNullException.ThrowIfNull(dragSource);
            ArgumentNullException.ThrowIfNull(data);

            if (!IsValidDragDropEffects(allowedEffects))
            {
                throw new ArgumentException(SR.Format(SR.DragDrop_DragDropEffectsInvalid, nameof(allowedEffects)));
            }

            DataObject dataObject = data as DataObject;
            if (dataObject == null)
            {
                dataObject = new DataObject(data);
            }

            return OleDoDragDrop(dragSource, dataObject, allowedEffects);
        }

        internal static void RegisterDropTarget(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                return;
            }

            OleDropTarget dropTarget = new(windowHandle);
            int hr = OleServicesContext.CurrentOleServicesContext.OleRegisterDragDrop(
                new HandleRef(null, windowHandle),
                dropTarget);

            if (!NativeMethods.Succeeded(hr))
            {
                throw new InvalidOperationException(SR.Format(SR.OleRegisterDragDropFailure, hr, windowHandle));
            }
        }

        internal static void RevokeDropTarget(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                return;
            }

            int hr = OleServicesContext.CurrentOleServicesContext.OleRevokeDragDrop(
                new HandleRef(null, windowHandle));

            if (!NativeMethods.Succeeded(hr))
            {
                throw new InvalidOperationException(SR.Format(SR.OleRevokeDragDropFailure, hr, windowHandle));
            }
        }

        internal static bool IsValidDragDropEffects(DragDropEffects dragDropEffects)
        {
            const int validMask = (int)(DragDropEffects.None | DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link | DragDropEffects.Scroll);
            return ((int)dragDropEffects & ~validMask) == 0;
        }

        internal static bool IsValidDragAction(DragAction dragAction)
        {
            return dragAction == DragAction.Continue || dragAction == DragAction.Drop || dragAction == DragAction.Cancel;
        }

        internal static bool IsValidDragDropKeyStates(DragDropKeyStates dragDropKeyStates)
        {
            const int validMask = (int)(
                DragDropKeyStates.LeftMouseButton |
                DragDropKeyStates.RightMouseButton |
                DragDropKeyStates.ShiftKey |
                DragDropKeyStates.ControlKey |
                DragDropKeyStates.MiddleMouseButton |
                DragDropKeyStates.AltKey);
            return ((int)dragDropKeyStates & ~validMask) == 0;
        }

        #endregion

        #region Internal Helpers

        private static DragDropEffects OleDoDragDrop(DependencyObject dragSource, DataObject dataObject, DragDropEffects allowedEffects)
        {
            OleDragSource oleDragSource = new(dragSource);
            int[] finalEffect = new int[1];

            int hr = OleServicesContext.CurrentOleServicesContext.OleDoDragDrop(
                dataObject,
                oleDragSource,
                (int)allowedEffects,
                finalEffect);

            if (!NativeMethods.Succeeded(hr) && hr != NativeMethods.DRAGDROP_S_CANCEL && hr != NativeMethods.DRAGDROP_S_DROP)
            {
                return DragDropEffects.None;
            }

            return (DragDropEffects)finalEffect[0];
        }

        private static void RaiseEventOnElement(DependencyObject target, RoutedEventArgs args)
        {
            if (target is UIElement uie)
            {
                uie.RaiseEvent(args);
            }
            else if (target is ContentElement ce)
            {
                ce.RaiseEvent(args);
            }
        }

        #endregion

        #region OLE DragSource & DropTarget Implementations

        private sealed class OleDragSource : INativeDropSource
        {
            private readonly DependencyObject _dragSource;

            public OleDragSource(DependencyObject dragSource)
            {
                _dragSource = dragSource;
            }

            public int QueryContinueDrag(int fEscapePressed, uint grfKeyState)
            {
                bool escapePressed = fEscapePressed != 0;
                DragDropKeyStates keyStates = (DragDropKeyStates)(grfKeyState & 0x3F);

                QueryContinueDragEventArgs args = new(escapePressed, keyStates);
                RaiseDragSourceEvent(PreviewQueryContinueDragEvent, QueryContinueDragEvent, args);

                if (args.Action == DragAction.Continue)
                {
                    if (escapePressed)
                    {
                        args.Action = DragAction.Cancel;
                    }
                    else if ((keyStates & (DragDropKeyStates.LeftMouseButton | DragDropKeyStates.MiddleMouseButton | DragDropKeyStates.RightMouseButton)) == 0)
                    {
                        args.Action = DragAction.Drop;
                    }
                }

                return args.Action switch
                {
                    DragAction.Drop => NativeMethods.DRAGDROP_S_DROP,
                    DragAction.Cancel => NativeMethods.DRAGDROP_S_CANCEL,
                    _ => OleComBridge.S_OK,
                };
            }

            public int GiveFeedback(uint dwEffect)
            {
                DragDropEffects effects = (DragDropEffects)dwEffect;
                GiveFeedbackEventArgs args = new(effects, useDefaultCursors: true);
                RaiseDragSourceEvent(PreviewGiveFeedbackEvent, GiveFeedbackEvent, args);

                return args.UseDefaultCursors
                    ? NativeMethods.DRAGDROP_S_USEDEFAULTCURSORS
                    : OleComBridge.S_OK;
            }

            private void RaiseDragSourceEvent(RoutedEvent previewEvent, RoutedEvent bubbleEvent, RoutedEventArgs args)
            {
                args.RoutedEvent = previewEvent;
                RaiseEventOnElement(_dragSource, args);

                if (!args.Handled)
                {
                    args.RoutedEvent = bubbleEvent;
                    RaiseEventOnElement(_dragSource, args);
                }
            }
        }

        private sealed unsafe class OleDropTarget : INativeDropTarget
        {
            private readonly IntPtr _windowHandle;
            private IDataObject _dataObject;
            private DependencyObject _lastTarget;

            public OleDropTarget(IntPtr windowHandle)
            {
                _windowHandle = windowHandle;
            }

            public void DragEnter(void* pDataObject, uint grfKeyState, POINTL pt, ref uint pdwEffect)
            {
                _dataObject = ResolveDataObject(pDataObject);

                if (_dataObject == null)
                {
                    pdwEffect = (uint)DragDropEffects.None;
                    return;
                }

                DependencyObject target = GetCurrentTarget(pt, out Point targetPoint);
                _lastTarget = target;

                if (target != null)
                {
                    RaiseDragEvent(
                        DragEnterEvent,
                        grfKeyState,
                        ref pdwEffect,
                        target,
                        targetPoint);
                }
                else
                {
                    pdwEffect = (uint)DragDropEffects.None;
                }
            }

            public void DragOver(uint grfKeyState, POINTL pt, ref uint pdwEffect)
            {
                if (_dataObject == null)
                {
                    pdwEffect = (uint)DragDropEffects.None;
                    return;
                }

                DependencyObject target = GetCurrentTarget(pt, out Point targetPoint);

                if (target != null)
                {
                    if (target != _lastTarget)
                    {
                        try
                        {
                            if (_lastTarget != null)
                            {
                                uint leaveEffects = pdwEffect;
                                RaiseDragEvent(
                                    DragLeaveEvent,
                                    grfKeyState,
                                    ref leaveEffects,
                                    _lastTarget,
                                    new Point(0, 0));
                            }

                            RaiseDragEvent(
                                DragEnterEvent,
                                grfKeyState,
                                ref pdwEffect,
                                target,
                                targetPoint);
                        }
                        finally
                        {
                            _lastTarget = target;
                        }
                    }
                    else
                    {
                        RaiseDragEvent(
                            DragOverEvent,
                            grfKeyState,
                            ref pdwEffect,
                            target,
                            targetPoint);
                    }
                }
                else
                {
                    try
                    {
                        if (_lastTarget != null)
                        {
                            uint leaveEffects = pdwEffect;
                            RaiseDragEvent(
                                DragLeaveEvent,
                                grfKeyState,
                                ref leaveEffects,
                                _lastTarget,
                                new Point(0, 0));
                        }
                    }
                    finally
                    {
                        _lastTarget = null;
                        pdwEffect = (uint)DragDropEffects.None;
                    }
                }
            }

            public void DragLeave()
            {
                try
                {
                    if (_lastTarget != null && _dataObject != null)
                    {
                        uint effect = 0;
                        RaiseDragEvent(
                            DragLeaveEvent,
                            0,
                            ref effect,
                            _lastTarget,
                            new Point(0, 0));
                    }
                }
                finally
                {
                    _lastTarget = null;
                    _dataObject = null;
                }
            }

            public void Drop(void* pDataObject, uint grfKeyState, POINTL pt, ref uint pdwEffect)
            {
                IDataObject dropData = ResolveDataObject(pDataObject) ?? _dataObject;
                _dataObject = dropData;

                DependencyObject target = GetCurrentTarget(pt, out Point targetPoint);
                try
                {
                    if (target != null && dropData != null)
                    {
                        RaiseDragEvent(
                            DropEvent,
                            grfKeyState,
                            ref pdwEffect,
                            target,
                            targetPoint);
                    }
                    else
                    {
                        pdwEffect = (uint)DragDropEffects.None;
                    }
                }
                finally
                {
                    _lastTarget = null;
                    _dataObject = null;
                }
            }

            private static IDataObject ResolveDataObject(void* pDataObject)
            {
                if (pDataObject == null)
                {
                    return null;
                }

                if (OleComBridge.TryUnwrapManagedDataObject(pDataObject, out object key) &&
                    key is IDataObject managed)
                {
                    return managed;
                }

                return new DataObject(pDataObject);
            }

            private DependencyObject GetCurrentTarget(POINTL dragPoint, out Point targetPoint)
            {
                targetPoint = new Point(0, 0);

                HwndSource hwndSource = HwndSource.CriticalFromHwnd(_windowHandle);
                if (hwndSource == null)
                {
                    return null;
                }

                Point screenPoint = new(dragPoint.x, dragPoint.y);
                Point clientPoint = PointUtil.ScreenToClient(screenPoint, hwndSource);

                IInputElement rawHit = MouseDevice.LocalHitTest(true, clientPoint, hwndSource);
                DependencyObject candidate = rawHit as DependencyObject;

                DependencyObject dropTarget = null;
                while (candidate != null)
                {
                    if (candidate is UIElement uie)
                    {
                        if (uie.AllowDrop)
                        {
                            dropTarget = uie;
                            break;
                        }
                        candidate = VisualTreeHelper.GetParent(uie);
                    }
                    else if (candidate is ContentElement ce)
                    {
                        if (ce.AllowDrop)
                        {
                            dropTarget = ce;
                            break;
                        }
                        DependencyObject parent = ContentOperations.GetParent(ce);
                        candidate = parent ?? InputElement.GetContainingUIElement(ce, onlyTraverse2D: true);
                    }
                    else
                    {
                        candidate = InputElement.GetContainingUIElement(candidate, onlyTraverse2D: true);
                    }
                }

                if (dropTarget != null && hwndSource.RootVisual is DependencyObject rootDob)
                {
                    Point rootPoint = PointUtil.ClientToRoot(clientPoint, hwndSource);
                    targetPoint = InputElement.TranslatePoint(rootPoint, rootDob, dropTarget);
                }

                return dropTarget;
            }

            private void RaiseDragEvent(
                RoutedEvent dragEvent,
                uint dragDropKeyStates,
                ref uint effects,
                DependencyObject target,
                Point targetPoint)
            {
                Debug.Assert(_dataObject != null);
                Debug.Assert(target != null);

                DragDropKeyStates keyStates = (DragDropKeyStates)(dragDropKeyStates & 0x3F);
                DragDropEffects allowedEffects = (DragDropEffects)effects;

                DragEventArgs dragEventArgs = new(
                    _dataObject,
                    keyStates,
                    allowedEffects,
                    target,
                    targetPoint);

                if (dragEvent == DragEnterEvent)
                {
                    dragEventArgs.RoutedEvent = PreviewDragEnterEvent;
                }
                else if (dragEvent == DragOverEvent)
                {
                    dragEventArgs.RoutedEvent = PreviewDragOverEvent;
                }
                else if (dragEvent == DragLeaveEvent)
                {
                    dragEventArgs.RoutedEvent = PreviewDragLeaveEvent;
                }
                else if (dragEvent == DropEvent)
                {
                    dragEventArgs.RoutedEvent = PreviewDropEvent;
                }

                if (dragEventArgs.RoutedEvent != null)
                {
                    RaiseEventOnElement(target, dragEventArgs);
                }

                if (!dragEventArgs.Handled)
                {
                    dragEventArgs.RoutedEvent = dragEvent;
                    RaiseEventOnElement(target, dragEventArgs);
                }

                if (!dragEventArgs.Handled && dragEventArgs.Effects == allowedEffects)
                {
                    if (dragEvent == DragEnterEvent || dragEvent == DragOverEvent)
                    {
                        effects = (uint)ComputeDefaultDropEffect(keyStates, allowedEffects);
                        return;
                    }
                    else if (dragEvent == DropEvent)
                    {
                        effects = (uint)ComputeDefaultDropEffect(keyStates, allowedEffects);
                        return;
                    }
                }

                effects = (uint)(dragEventArgs.Effects & allowedEffects);
            }

            private static DragDropEffects ComputeDefaultDropEffect(DragDropKeyStates keyStates, DragDropEffects allowedEffects)
            {
                if ((keyStates & (DragDropKeyStates.ControlKey | DragDropKeyStates.ShiftKey)) == (DragDropKeyStates.ControlKey | DragDropKeyStates.ShiftKey) &&
                    (allowedEffects & DragDropEffects.Link) != 0)
                {
                    return DragDropEffects.Link;
                }

                if ((keyStates & DragDropKeyStates.ControlKey) != 0 &&
                    (allowedEffects & DragDropEffects.Copy) != 0)
                {
                    return DragDropEffects.Copy;
                }

                if ((allowedEffects & DragDropEffects.Move) != 0)
                {
                    return DragDropEffects.Move;
                }

                if ((allowedEffects & DragDropEffects.Copy) != 0)
                {
                    return DragDropEffects.Copy;
                }

                if ((allowedEffects & DragDropEffects.Link) != 0)
                {
                    return DragDropEffects.Link;
                }

                return DragDropEffects.None;
            }
        }

        #endregion
    }
}

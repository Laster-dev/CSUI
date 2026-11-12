// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using MS.Internal.PresentationFramework;

namespace System.Windows
{
    /// <summary>
    /// Specifies the buttons that are displayed on a message box.
    /// </summary>
    public enum MessageBoxButton
    {
        OK = 0,
        OKCancel = 1,
        YesNoCancel = 3,
        YesNo = 4,
    }

    /// <summary>
    /// Specifies the icon that is displayed by a message box.
    /// </summary>
    public enum MessageBoxImage
    {
        None = 0,
        Hand = 16,
        Question = 32,
        Exclamation = 48,
        Asterisk = 64,
        Stop = Hand,
        Error = Hand,
        Warning = Exclamation,
        Information = Asterisk,
    }

    /// <summary>
    /// Specifies special display options for a message box.
    /// </summary>
    [Flags]
    public enum MessageBoxOptions
    {
        None = 0x00000000,
        ServiceNotification = 0x00200000,
        DefaultDesktopOnly = 0x00020000,
        RightAlign = 0x00080000,
        RtlReading = 0x00100000,
    }

    /// <summary>
    /// Specifies which message box button that a user clicks.
    /// </summary>
    public enum MessageBoxResult
    {
        None = 0,
        OK = 1,
        Cancel = 2,
        Yes = 6,
        No = 7,
    }

    /// <summary>
    /// Displays a message box.
    /// </summary>
    public sealed class MessageBox
    {
        private const int IDOK = 1;
        private const int IDCANCEL = 2;
        private const int IDYES = 6;
        private const int IDNO = 7;

        private const int MB_DEFBUTTON1 = 0x00000000;
        private const int MB_DEFBUTTON2 = 0x00000100;
        private const int MB_DEFBUTTON3 = 0x00000200;

        private const int DEFAULT_BUTTON1 = 0x00000000;
        private const int DEFAULT_BUTTON2 = 0x00000100;
        private const int DEFAULT_BUTTON3 = 0x00000200;

        private static readonly unsafe delegate* unmanaged[Stdcall]<IntPtr, char*, char*, uint, int> s_messageBoxW = LoadMessageBoxW();

        private MessageBox()
        {
        }

        private static unsafe delegate* unmanaged[Stdcall]<IntPtr, char*, char*, uint, int> LoadMessageBoxW()
        {
            if (NativeLibrary.TryLoad("user32.dll", out nint user32) &&
                NativeLibrary.TryGetExport(user32, "MessageBoxW", out nint proc))
            {
                return (delegate* unmanaged[Stdcall]<IntPtr, char*, char*, uint, int>)proc;
            }
            return null;
        }

        public static MessageBoxResult Show(
            string messageBoxText,
            string caption,
            MessageBoxButton button,
            MessageBoxImage icon,
            MessageBoxResult defaultResult,
            MessageBoxOptions options)
        {
            return ShowCore(IntPtr.Zero, messageBoxText, caption, button, icon, defaultResult, options);
        }

        public static MessageBoxResult Show(
            string messageBoxText,
            string caption,
            MessageBoxButton button,
            MessageBoxImage icon,
            MessageBoxResult defaultResult)
        {
            return ShowCore(IntPtr.Zero, messageBoxText, caption, button, icon, defaultResult, 0);
        }

        public static MessageBoxResult Show(
            string messageBoxText,
            string caption,
            MessageBoxButton button,
            MessageBoxImage icon)
        {
            return ShowCore(IntPtr.Zero, messageBoxText, caption, button, icon, 0, 0);
        }

        public static MessageBoxResult Show(
            string messageBoxText,
            string caption,
            MessageBoxButton button)
        {
            return ShowCore(IntPtr.Zero, messageBoxText, caption, button, MessageBoxImage.None, 0, 0);
        }

        public static MessageBoxResult Show(string messageBoxText, string caption)
        {
            return ShowCore(IntPtr.Zero, messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.None, 0, 0);
        }

        public static MessageBoxResult Show(string messageBoxText)
        {
            return ShowCore(IntPtr.Zero, messageBoxText, string.Empty, MessageBoxButton.OK, MessageBoxImage.None, 0, 0);
        }

        public static MessageBoxResult Show(
            Window owner,
            string messageBoxText,
            string caption,
            MessageBoxButton button,
            MessageBoxImage icon,
            MessageBoxResult defaultResult,
            MessageBoxOptions options)
        {
            return Show(owner, messageBoxText, caption, button, icon, defaultResult, options, true);
        }

        public static MessageBoxResult Show(
            Window owner,
            string messageBoxText,
            string caption,
            MessageBoxButton button,
            MessageBoxImage icon,
            MessageBoxResult defaultResult)
        {
            return ShowCore(GetOwnerHandle(owner), messageBoxText, caption, button, icon, defaultResult, 0);
        }

        public static MessageBoxResult Show(
            Window owner,
            string messageBoxText,
            string caption,
            MessageBoxButton button,
            MessageBoxImage icon)
        {
            return ShowCore(GetOwnerHandle(owner), messageBoxText, caption, button, icon, 0, 0);
        }

        public static MessageBoxResult Show(
            Window owner,
            string messageBoxText,
            string caption,
            MessageBoxButton button)
        {
            return ShowCore(GetOwnerHandle(owner), messageBoxText, caption, button, MessageBoxImage.None, 0, 0);
        }

        public static MessageBoxResult Show(Window owner, string messageBoxText, string caption)
        {
            return ShowCore(GetOwnerHandle(owner), messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.None, 0, 0);
        }

        public static MessageBoxResult Show(Window owner, string messageBoxText)
        {
            return ShowCore(GetOwnerHandle(owner), messageBoxText, string.Empty, MessageBoxButton.OK, MessageBoxImage.None, 0, 0);
        }

        private static IntPtr GetOwnerHandle(Window owner)
        {
            ArgumentNullException.ThrowIfNull(owner);
            return new WindowInteropHelper(owner).CriticalHandle;
        }

        private static MessageBoxResult Show(
            Window owner,
            string messageBoxText,
            string caption,
            MessageBoxButton button,
            MessageBoxImage icon,
            MessageBoxResult defaultResult,
            MessageBoxOptions options,
            bool _)
        {
            return ShowCore(GetOwnerHandle(owner), messageBoxText, caption, button, icon, defaultResult, options);
        }

        internal static MessageBoxResult ShowCore(
            IntPtr owner,
            string messageBoxText,
            string caption,
            MessageBoxButton button,
            MessageBoxImage icon,
            MessageBoxResult defaultResult,
            MessageBoxOptions options)
        {
            if (!IsValidMessageBoxButton(button))
            {
                throw new InvalidEnumArgumentException(nameof(button), (int)button, typeof(MessageBoxButton));
            }
            if (!IsValidMessageBoxImage(icon))
            {
                throw new InvalidEnumArgumentException(nameof(icon), (int)icon, typeof(MessageBoxImage));
            }
            if (!IsValidMessageBoxResult(defaultResult))
            {
                throw new InvalidEnumArgumentException(nameof(defaultResult), (int)defaultResult, typeof(MessageBoxResult));
            }
            if (!IsValidMessageBoxOptions(options))
            {
                throw new InvalidEnumArgumentException(nameof(options), (int)options, typeof(MessageBoxOptions));
            }

            if ((options & (MessageBoxOptions.ServiceNotification | MessageBoxOptions.DefaultDesktopOnly)) != 0)
            {
                if (owner != IntPtr.Zero)
                {
                    throw new ArgumentException(SR.CantShowMBServiceWithOwner);
                }
            }
            else if (owner == IntPtr.Zero)
            {
                owner = MS.Win32.UnsafeNativeMethods.GetActiveWindow();
            }

            int style = BuildStyle(button, icon, defaultResult, options);

            int result;
            unsafe
            {
                if (s_messageBoxW == null)
                {
                    return MessageBoxResult.None;
                }

                fixed (char* pText = messageBoxText ?? string.Empty)
                fixed (char* pCaption = caption ?? string.Empty)
                {
                    result = s_messageBoxW(owner, pText, pCaption, (uint)style);
                }
            }

            return Win32ToMessageBoxResult(result);
        }

        internal static int BuildStyle(
            MessageBoxButton button,
            MessageBoxImage icon,
            MessageBoxResult defaultResult,
            MessageBoxOptions options)
        {
            int defaultButton = DefaultResultToButtonNumber(defaultResult, button);
            return (int)button | (int)icon | defaultButton | (int)options;
        }

        internal static int DefaultResultToButtonNumber(MessageBoxResult result, MessageBoxButton button)
        {
            if (result == 0)
            {
                return DEFAULT_BUTTON1;
            }

            switch (button)
            {
                case MessageBoxButton.OK:
                    return DEFAULT_BUTTON1;
                case MessageBoxButton.OKCancel:
                    return result == MessageBoxResult.Cancel ? DEFAULT_BUTTON2 : DEFAULT_BUTTON1;
                case MessageBoxButton.YesNo:
                    return result == MessageBoxResult.No ? DEFAULT_BUTTON2 : DEFAULT_BUTTON1;
                case MessageBoxButton.YesNoCancel:
                    if (result == MessageBoxResult.No)
                        return DEFAULT_BUTTON2;
                    if (result == MessageBoxResult.Cancel)
                        return DEFAULT_BUTTON3;
                    return DEFAULT_BUTTON1;
                default:
                    return DEFAULT_BUTTON1;
            }
        }

        internal static MessageBoxResult Win32ToMessageBoxResult(int value)
        {
            switch (value)
            {
                case IDOK:
                    return MessageBoxResult.OK;
                case IDCANCEL:
                    return MessageBoxResult.Cancel;
                case IDYES:
                    return MessageBoxResult.Yes;
                case IDNO:
                    return MessageBoxResult.No;
                default:
                    return MessageBoxResult.None;
            }
        }

        internal static bool IsValidMessageBoxButton(MessageBoxButton value)
        {
            return value == MessageBoxButton.OK
                || value == MessageBoxButton.OKCancel
                || value == MessageBoxButton.YesNo
                || value == MessageBoxButton.YesNoCancel;
        }

        internal static bool IsValidMessageBoxImage(MessageBoxImage value)
        {
            return value == MessageBoxImage.Asterisk
                || value == MessageBoxImage.Error
                || value == MessageBoxImage.Exclamation
                || value == MessageBoxImage.Hand
                || value == MessageBoxImage.Information
                || value == MessageBoxImage.None
                || value == MessageBoxImage.Question
                || value == MessageBoxImage.Stop
                || value == MessageBoxImage.Warning;
        }

        internal static bool IsValidMessageBoxResult(MessageBoxResult value)
        {
            return value == MessageBoxResult.Cancel
                || value == MessageBoxResult.No
                || value == MessageBoxResult.None
                || value == MessageBoxResult.OK
                || value == MessageBoxResult.Yes;
        }

        internal static bool IsValidMessageBoxOptions(MessageBoxOptions value)
        {
            const MessageBoxOptions validMask =
                MessageBoxOptions.ServiceNotification |
                MessageBoxOptions.DefaultDesktopOnly |
                MessageBoxOptions.RightAlign |
                MessageBoxOptions.RtlReading;
            return (value & ~validMask) == 0;
        }
    }
}

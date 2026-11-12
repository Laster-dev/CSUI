// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using MS.Win32;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows
{
    /// <summary>
    /// Translates between Windows clipboard format names and numeric clipboard IDs.
    /// </summary>
    public static class DataFormats
    {
        /// <summary>Specifies the standard ANSI text format.</summary>
        public static readonly string Text = "Text";

        /// <summary>Specifies the standard Windows Unicode text format.</summary>
        public static readonly string UnicodeText = "UnicodeText";

        /// <summary>Specifies the Windows Device Independent Bitmap (DIB) format.</summary>
        public static readonly string Dib = "DeviceIndependentBitmap";

        /// <summary>Specifies a Windows bitmap format.</summary>
        public static readonly string Bitmap = "Bitmap";

        /// <summary>Specifies the Windows enhanced metafile format.</summary>
        public static readonly string EnhancedMetafile = "EnhancedMetafile";

        /// <summary>Specifies the Windows metafile format.</summary>
        public static readonly string MetafilePicture = "MetaFilePict";

        /// <summary>Specifies the Windows symbolic link format.</summary>
        public static readonly string SymbolicLink = "SymbolicLink";

        /// <summary>Specifies the Windows Data Interchange Format (DIF).</summary>
        public static readonly string Dif = "DataInterchangeFormat";

        /// <summary>Specifies the Tagged Image File Format (TIFF).</summary>
        public static readonly string Tiff = "TaggedImageFileFormat";

        /// <summary>Specifies the standard Windows OEM text format.</summary>
        public static readonly string OemText = "OEMText";

        /// <summary>Specifies the Windows palette format.</summary>
        public static readonly string Palette = "Palette";

        /// <summary>Specifies the Windows pen data format.</summary>
        public static readonly string PenData = "PenData";

        /// <summary>Specifies the Resource Interchange File Format (RIFF) audio format.</summary>
        public static readonly string Riff = "RiffAudio";

        /// <summary>Specifies the wave audio format.</summary>
        public static readonly string WaveAudio = "WaveAudio";

        /// <summary>Specifies the Windows file drop format.</summary>
        public static readonly string FileDrop = "FileDrop";

        /// <summary>Specifies the Windows culture/locale format.</summary>
        public static readonly string Locale = "Locale";

        /// <summary>Specifies the HTML clipboard format.</summary>
        public static readonly string Html = "HTML Format";

        /// <summary>Specifies the Rich Text Format (RTF).</summary>
        public static readonly string Rtf = "Rich Text Format";

        /// <summary>Specifies a comma-separated value (CSV) format.</summary>
        public static readonly string CommaSeparatedValue = "Csv";

        /// <summary>Specifies a data format encapsulating a common language runtime string class.</summary>
        public static readonly string StringFormat = typeof(string).FullName;

        /// <summary>Specifies a data format encapsulating any serializable data object.</summary>
        public static readonly string Serializable = "PersistentObject";

        /// <summary>Specifies a data format as Xaml.</summary>
        public static readonly string Xaml = "Xaml";

        /// <summary>Specifies a data format as XamlPackage.</summary>
        public static readonly string XamlPackage = "XamlPackage";

        private static readonly Lock s_lock = new();
        private static List<DataFormat> s_formatList;

        /// <summary>
        /// Gets a <see cref="DataFormat"/> with the Windows clipboard format name and ID
        /// corresponding to the specified numeric ID.
        /// </summary>
        public static unsafe DataFormat GetDataFormat(int id)
        {
            uint formatId = unchecked((uint)(id & 0xFFFF));

            lock (s_lock)
            {
                EnsurePredefined();

                for (int i = 0; i < s_formatList.Count; i++)
                {
                    if ((s_formatList[i].Id & 0xFFFF) == (int)formatId)
                    {
                        return s_formatList[i];
                    }
                }

                char* buffer = stackalloc char[260];
                int len = UnsafeNativeMethods.GetClipboardFormatName(formatId, buffer, 260);
                string name = len > 0 ? new string(buffer, 0, len) : $"Format{id}";

                DataFormat created = new(name, id);
                s_formatList.Add(created);
                return created;
            }
        }

        /// <summary>
        /// Gets a <see cref="DataFormat"/> with the Windows clipboard format name and ID
        /// corresponding to the specified format name.
        /// </summary>
        public static DataFormat GetDataFormat(string format)
        {
            ArgumentNullException.ThrowIfNull(format);
            if (format.Length == 0)
            {
                throw new ArgumentException(SR.DataObject_EmptyFormatNotAllowed, nameof(format));
            }

            lock (s_lock)
            {
                EnsurePredefined();

                for (int i = 0; i < s_formatList.Count; i++)
                {
                    if (string.Equals(s_formatList[i].Name, format, StringComparison.Ordinal))
                    {
                        return s_formatList[i];
                    }
                }

                uint registeredId = UnsafeNativeMethods.RegisterClipboardFormat(format);
                if (registeredId == 0)
                {
                    throw new Win32Exception();
                }

                DataFormat created = new(format, unchecked((int)registeredId));
                s_formatList.Add(created);
                return created;
            }
        }

        internal static string ConvertToDataFormats(TextDataFormat textDataformat)
        {
            return textDataformat switch
            {
                TextDataFormat.Text => Text,
                TextDataFormat.UnicodeText => UnicodeText,
                TextDataFormat.Rtf => Rtf,
                TextDataFormat.Html => Html,
                TextDataFormat.CommaSeparatedValue => CommaSeparatedValue,
                TextDataFormat.Xaml => Xaml,
                _ => UnicodeText,
            };
        }

        internal static bool IsValidTextDataFormat(TextDataFormat textDataformat)
        {
            return textDataformat >= TextDataFormat.Text && textDataformat <= TextDataFormat.Xaml;
        }

        private static void EnsurePredefined()
        {
            if (s_formatList != null)
            {
                return;
            }

            s_formatList = new List<DataFormat>(16)
            {
                new DataFormat(UnicodeText, NativeMethods.CF_UNICODETEXT),
                new DataFormat(Text, NativeMethods.CF_TEXT),
                new DataFormat(Bitmap, NativeMethods.CF_BITMAP),
                new DataFormat(MetafilePicture, NativeMethods.CF_METAFILEPICT),
                new DataFormat(EnhancedMetafile, NativeMethods.CF_ENHMETAFILE),
                new DataFormat(Dif, NativeMethods.CF_DIF),
                new DataFormat(Tiff, NativeMethods.CF_TIFF),
                new DataFormat(OemText, NativeMethods.CF_OEMTEXT),
                new DataFormat(Dib, NativeMethods.CF_DIB),
                new DataFormat(Palette, NativeMethods.CF_PALETTE),
                new DataFormat(PenData, NativeMethods.CF_PENDATA),
                new DataFormat(Riff, NativeMethods.CF_RIFF),
                new DataFormat(WaveAudio, NativeMethods.CF_WAVE),
                new DataFormat(SymbolicLink, NativeMethods.CF_SYLK),
                new DataFormat(FileDrop, NativeMethods.CF_HDROP),
                new DataFormat(Locale, NativeMethods.CF_LOCALE),
            };
        }
    }
}

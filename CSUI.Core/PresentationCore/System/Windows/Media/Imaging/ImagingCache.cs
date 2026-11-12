// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections;

namespace System.Windows.Media.Imaging
{
    /// <summary>
    /// ImagingCache provides caching for distinct Images / Decoders.
    /// </summary>
    internal static class ImagingCache
    {
        internal static void AddToImageCache(Uri uri, object obj) => AddToCache(uri, obj, _imageCache);

        internal static void RemoveFromImageCache(Uri uri) => RemoveFromCache(uri, _imageCache);

        internal static object CheckImageCache(Uri uri) => CheckCache(uri, _imageCache);

        internal static void AddToDecoderCache(Uri uri, object obj) => AddToCache(uri, obj, _decoderCache);

        internal static void RemoveFromDecoderCache(Uri uri) => RemoveFromCache(uri, _decoderCache);

        internal static object CheckDecoderCache(Uri uri) => CheckCache(uri, _decoderCache);

        private static void AddToCache(Uri uri, object obj, Hashtable table)
        {
            lock (table)
            {
                if (table.Contains(uri))
                {
                    return;
                }

                if (table.Count == MAX_CACHE_SIZE)
                {
                    ArrayList al = new ArrayList();
                    foreach (DictionaryEntry de in table)
                    {
                        if (de.Value is WeakReference wr && wr.Target == null)
                        {
                            al.Add(de.Key);
                        }
                    }

                    foreach (object key in al)
                    {
                        table.Remove(key);
                    }
                }

                if (table.Count == MAX_CACHE_SIZE)
                {
                    return;
                }

                table[uri] = obj;
            }
        }

        private static void RemoveFromCache(Uri uri, Hashtable table)
        {
            lock (table)
            {
                if (table.Contains(uri))
                {
                    table.Remove(uri);
                }
            }
        }

        private static object CheckCache(Uri uri, Hashtable table)
        {
            lock (table)
            {
                return table[uri];
            }
        }

        private static readonly Hashtable _imageCache = new Hashtable();
        private static readonly Hashtable _decoderCache = new Hashtable();
        private const int MAX_CACHE_SIZE = 300;
    }
}

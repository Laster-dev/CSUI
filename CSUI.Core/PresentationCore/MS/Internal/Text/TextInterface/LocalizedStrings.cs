// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using CSUI.Core.Interop.DirectX;

namespace MS.Internal.Text.TextInterface
{
    internal sealed unsafe class LocalizedStrings : IDictionary<CultureInfo, string>, IDisposable
    {
        private nint _localizedStrings;
        private readonly bool _isEmptyWrapper;
        private Dictionary<CultureInfo, string>? _cache;

        internal LocalizedStrings()
        {
            _localizedStrings = IntPtr.Zero;
            _isEmptyWrapper = true;
            _cache = new Dictionary<CultureInfo, string>(0);
        }

        internal LocalizedStrings(void* localizedStrings)
        {
            _localizedStrings = (nint)localizedStrings;
            _isEmptyWrapper = localizedStrings == null;
        }

        ~LocalizedStrings()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            ComHelpers.ReleaseAtomic(ref _localizedStrings);
        }

        internal uint StringsCount
        {
            get
            {
                void* p = (void*)_localizedStrings;
                return p == null ? 0u : IDWriteLocalizedStringsVtbl.GetCount(p);
            }
        }

        internal bool FindLocaleName(string localeName, out uint index)
        {
            void* p = (void*)_localizedStrings;
            if (p == null || string.IsNullOrEmpty(localeName))
            {
                index = 0;
                return false;
            }

            int hr = IDWriteLocalizedStringsVtbl.FindLocaleName(p, localeName, out index, out bool exists);
            DWriteUtil.ConvertHresultToException(hr);
            return exists;
        }

        internal string GetString(uint index)
        {
            void* p = (void*)_localizedStrings;
            if (p == null)
                throw new ArgumentOutOfRangeException(nameof(index));

            int hr = IDWriteLocalizedStringsVtbl.GetStringLength(p, index, out uint length);
            DWriteUtil.ConvertHresultToException(hr);

            char[] buffer = new char[checked((int)length + 1)];
            fixed (char* pBuffer = buffer)
            {
                hr = IDWriteLocalizedStringsVtbl.GetString(p, index, pBuffer, length + 1);
                DWriteUtil.ConvertHresultToException(hr);
                return new string(pBuffer, 0, (int)length);
            }
        }

        internal string GetLocaleName(uint index)
        {
            void* p = (void*)_localizedStrings;
            if (p == null)
                throw new ArgumentOutOfRangeException(nameof(index));

            int hr = IDWriteLocalizedStringsExVtbl.GetLocaleNameLength(p, index, out uint length);
            DWriteUtil.ConvertHresultToException(hr);

            char[] buffer = new char[checked((int)length + 1)];
            fixed (char* pBuffer = buffer)
            {
                hr = IDWriteLocalizedStringsExVtbl.GetLocaleName(p, index, pBuffer, length + 1);
                DWriteUtil.ConvertHresultToException(hr);
                return new string(pBuffer, 0, (int)length);
            }
        }

        private Dictionary<CultureInfo, string> EnsureMap()
        {
            if (_cache != null)
                return _cache;

            Dictionary<CultureInfo, string> map = new Dictionary<CultureInfo, string>();
            uint count = StringsCount;
            for (uint i = 0; i < count; i++)
            {
                string localeName = GetLocaleName(i);
                CultureInfo culture;
                try
                {
                    culture = string.IsNullOrEmpty(localeName)
                        ? CultureInfo.InvariantCulture
                        : CultureInfo.GetCultureInfo(localeName);
                }
                catch (CultureNotFoundException)
                {
                    culture = CultureInfo.InvariantCulture;
                }

                map[culture] = GetString(i);
            }

            _cache = map;
            return map;
        }

        public string this[CultureInfo key]
        {
            get => EnsureMap()[key];
            set => throw new NotSupportedException();
        }

        public ICollection<CultureInfo> Keys => EnsureMap().Keys;
        public ICollection<string> Values => EnsureMap().Values;
        public int Count => _isEmptyWrapper ? 0 : (int)StringsCount;
        public bool IsReadOnly => true;

        public void Add(CultureInfo key, string value) => throw new NotSupportedException();
        public void Add(KeyValuePair<CultureInfo, string> item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Contains(KeyValuePair<CultureInfo, string> item) => ((ICollection<KeyValuePair<CultureInfo, string>>)EnsureMap()).Contains(item);
        public bool ContainsKey(CultureInfo key) => EnsureMap().ContainsKey(key);
        public void CopyTo(KeyValuePair<CultureInfo, string>[] array, int arrayIndex) => ((ICollection<KeyValuePair<CultureInfo, string>>)EnsureMap()).CopyTo(array, arrayIndex);
        public IEnumerator<KeyValuePair<CultureInfo, string>> GetEnumerator() => EnsureMap().GetEnumerator();
        public bool Remove(CultureInfo key) => throw new NotSupportedException();
        public bool Remove(KeyValuePair<CultureInfo, string> item) => throw new NotSupportedException();
        public bool TryGetValue(CultureInfo key, out string value) => EnsureMap().TryGetValue(key, out value!);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

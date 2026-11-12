// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections;
using System.Windows;

namespace MS.Utility
{
    internal class DTypeMap
    {
        public DTypeMap(int entryCount)
        {
            _entryCount = entryCount;
            _entries = new object[_entryCount];
            _activeDTypes = new ItemStructList<DependencyObjectType>(128);
        }

        public object this[DependencyObjectType dType]
        {
            get
            {
                if (dType.Id < _entryCount)
                {
                    return _entries[dType.Id];
                }
                else
                {
                    if (_overFlow != null)
                    {
                        return _overFlow[dType];
                    }

                    return null;
                }
            }

            set
            {
                if (dType.Id < _entryCount)
                {
                    _entries[dType.Id] = value;
                }
                else
                {
                    _overFlow ??= new Hashtable();
                    _overFlow[dType] = value;
                }

                _activeDTypes.Add(dType);
            }
        }

        public ItemStructList<DependencyObjectType> ActiveDTypes => _activeDTypes;

        public void Clear()
        {
            for (int i = 0; i < _entryCount; i++)
            {
                _entries[i] = null;
            }

            for (int i = 0; i < _activeDTypes.Count; i++)
            {
                _activeDTypes.List[i] = null;
            }

            _overFlow?.Clear();
        }

        private int _entryCount;
        private object[] _entries;
        private Hashtable _overFlow;
        private ItemStructList<DependencyObjectType> _activeDTypes;
    }
}

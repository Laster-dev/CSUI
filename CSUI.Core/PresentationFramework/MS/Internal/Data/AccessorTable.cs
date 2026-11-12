#nullable enable

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Windows;

namespace MS.Internal.Data
{
    internal sealed class AccessorInfo
    {
        internal AccessorInfo(object accessor, Type propertyType, object[]? args)
        {
            Accessor = accessor;
            PropertyType = propertyType;
            Args = args;
        }

        internal object Accessor { get; }
        internal Type PropertyType { get; }
        internal object[]? Args { get; }
        internal int Generation { get; set; }
    }

    internal sealed class AccessorTable
    {
        private readonly record struct AccessorTableKey(SourceValueType Kind, Type SourceType, string Name);

        private readonly ConcurrentDictionary<AccessorTableKey, AccessorInfo> _table = new();

        internal AccessorInfo? this[SourceValueType type, Type sourceType, string name]
        {
            get
            {
                if (sourceType == null || name == null)
                    return null;
                _table.TryGetValue(new AccessorTableKey(type, sourceType, name), out AccessorInfo? info);
                return info;
            }
            set
            {
                if (sourceType == null || name == null)
                    return;

                var key = new AccessorTableKey(type, sourceType, name);
                if (value == null)
                {
                    _table.TryRemove(key, out _);
                }
                else
                {
                    _table[key] = value;
                }
            }
        }

        internal bool TraceSize { get; set; }

        [Conditional("DEBUG")]
        internal void PrintStats()
        {
            Debug.WriteLine($"AccessorTable count: {_table.Count}");
        }
    }
}

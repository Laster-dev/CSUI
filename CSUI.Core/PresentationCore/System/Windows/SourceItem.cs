// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows
{
    internal struct SourceItem
    {
        internal SourceItem(int startIndex, object source)
        {
            _startIndex = startIndex;
            _source = source;
        }

        internal int StartIndex => _startIndex;
        internal object Source => _source;

        public override bool Equals(object o)
        {
            return o is SourceItem item && Equals(item);
        }

        public bool Equals(SourceItem sourceItem)
        {
            return sourceItem._startIndex == this._startIndex &&
                   sourceItem._source == this._source;
        }

        public override int GetHashCode() => base.GetHashCode();

        public static bool operator ==(SourceItem sourceItem1, SourceItem sourceItem2) => sourceItem1.Equals(sourceItem2);
        public static bool operator !=(SourceItem sourceItem1, SourceItem sourceItem2) => !sourceItem1.Equals(sourceItem2);

        private int _startIndex;
        private object _source;
    }
}

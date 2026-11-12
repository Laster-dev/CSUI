// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace MS.Internal.Data
{
    // Sentinel object stored in SourceValueState.info for indexers in PropertyPathWorker.
    internal sealed class IndexerPropertyInfo
    {
        private IndexerPropertyInfo() { }

        internal static readonly IndexerPropertyInfo Instance = new IndexerPropertyInfo();
    }
}

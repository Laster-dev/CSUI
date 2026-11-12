#nullable disable

using System;
using System.Windows;

namespace MS.Internal.Data
{
    // NativeAOT zero-reflection placeholder (ADO.NET / XLinq PropertyDescriptor caching removed).
    internal sealed class ValueTable : IWeakEventListener
    {
        bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            return false;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Markup;
using System.Windows.Navigation;

namespace System.Windows.Media
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct UriHolder
    {
        internal Uri BaseUri;
        internal Uri OriginalUri;
    }

    internal static class TypeConverterHelper
    {
        internal static UriHolder GetUriFromUriContext(ITypeDescriptorContext context, object inputString)
        {
            UriHolder uriHolder = new UriHolder();

            if (inputString is string str)
            {
                uriHolder.OriginalUri = new Uri(str, UriKind.RelativeOrAbsolute);
            }
            else if (inputString is Uri uri)
            {
                uriHolder.OriginalUri = uri;
            }

            if (uriHolder.OriginalUri != null && !uriHolder.OriginalUri.IsAbsoluteUri)
            {
                if (context != null && context.GetService(typeof(IUriContext)) is IUriContext iuc)
                {
                    if (iuc.BaseUri != null)
                    {
                        uriHolder.BaseUri = iuc.BaseUri;

                        if (!uriHolder.BaseUri.IsAbsoluteUri)
                        {
                            uriHolder.BaseUri = new Uri(BaseUriHelper.BaseUri, uriHolder.BaseUri);
                        }
                    }
                    else
                    {
                        uriHolder.BaseUri = BaseUriHelper.BaseUri;
                    }
                }
                else
                {
                    uriHolder.BaseUri = BaseUriHelper.BaseUri;
                }
            }

            return uriHolder;
        }
    }
}

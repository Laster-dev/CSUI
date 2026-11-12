// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Windows.Navigation;

namespace MS.Internal.Utility
{
    internal static class BindUriHelper
    {
        internal static string UriToString(Uri uri)
        {
            ArgumentNullException.ThrowIfNull(uri);

            return uri.GetComponents(
                uri.IsAbsoluteUri ? UriComponents.AbsoluteUri : UriComponents.SerializationInfoString,
                UriFormat.SafeUnescaped);
        }

        internal static Uri BaseUri
        {
            get => BaseUriHelper.BaseUri;
            set => BaseUriHelper.BaseUri = value;
        }

        internal static bool DoSchemeAndHostMatch(Uri firstUri, Uri secondUri)
        {
            return Uri.Compare(
                firstUri,
                secondUri,
                UriComponents.SchemeAndServer,
                UriFormat.SafeUnescaped,
                StringComparison.OrdinalIgnoreCase) == 0;
        }

        internal static Uri GetResolvedUri(Uri baseUri, Uri orgUri)
        {
            if (orgUri == null)
            {
                return null;
            }

            if (!orgUri.IsAbsoluteUri)
            {
                Uri resolvedBase = baseUri ?? BaseUriHelper.BaseUri;
                return new Uri(resolvedBase, orgUri);
            }

            return BaseUriHelper.FixFileUri(orgUri);
        }
    }
}

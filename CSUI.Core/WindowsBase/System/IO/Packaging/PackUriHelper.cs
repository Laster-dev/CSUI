// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;

namespace System.IO.Packaging
{
    internal static class PackUriHelper
    {
        public static readonly string UriSchemePack = "pack";

        static PackUriHelper()
        {
            if (!UriParser.IsKnownScheme(UriSchemePack))
            {
                UriParser.Register(new GenericUriParser(GenericUriParserOptions.GenericAuthority), UriSchemePack, -1);
            }
        }

        public static Uri Create(Uri packageUri)
        {
            return Create(packageUri, null);
        }

        public static Uri Create(Uri packageUri, Uri partUri)
        {
            ArgumentNullException.ThrowIfNull(packageUri);
            if (!packageUri.IsAbsoluteUri)
            {
                throw new ArgumentException("Package URI must be absolute.", nameof(packageUri));
            }

            string escapedPackage = packageUri.GetComponents(UriComponents.AbsoluteUri, UriFormat.UriEscaped)
                .Replace('/', ',');
            string partPath = partUri == null
                ? "/"
                : (partUri.IsAbsoluteUri
                    ? partUri.AbsolutePath
                    : "/" + partUri.OriginalString.TrimStart('/'));

            return new Uri($"{UriSchemePack}://{escapedPackage}{partPath}");
        }

        public static Uri GetPackageUri(Uri packUri)
        {
            ArgumentNullException.ThrowIfNull(packUri);
            if (!packUri.IsAbsoluteUri || !string.Equals(packUri.Scheme, UriSchemePack, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("URI must use pack:// scheme.", nameof(packUri));
            }

            string authority = packUri.GetComponents(UriComponents.HostAndPort, UriFormat.UriEscaped);
            if (string.IsNullOrEmpty(authority))
            {
                authority = packUri.Host;
            }

            string packageString = Uri.UnescapeDataString(authority).Replace(',', '/');
            if (!packageString.EndsWith('/'))
            {
                packageString += "/";
            }

            return new Uri(packageString, UriKind.Absolute);
        }

        public static Uri GetPartUri(Uri packUri)
        {
            ArgumentNullException.ThrowIfNull(packUri);
            if (!packUri.IsAbsoluteUri || !string.Equals(packUri.Scheme, UriSchemePack, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("URI must use pack:// scheme.", nameof(packUri));
            }

            string path = packUri.GetComponents(UriComponents.Path | UriComponents.KeepDelimiter, UriFormat.UriEscaped);
            if (string.IsNullOrEmpty(path) || path == "/")
            {
                return null;
            }

            if (!path.StartsWith('/'))
            {
                path = "/" + path;
            }

            return new Uri(path, UriKind.Relative);
        }

        public static string GetStringForPartUri(Uri partUri)
        {
            ArgumentNullException.ThrowIfNull(partUri);
            return partUri.OriginalString;
        }
    }
}

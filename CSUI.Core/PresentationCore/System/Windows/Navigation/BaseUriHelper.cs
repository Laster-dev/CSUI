// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Reflection;
using System.Windows.Markup;
using System.Windows.Media;

namespace System.Windows.Navigation
{
    /// <summary>
    /// BaseUriHelper class provides BaseUri related property and methods.
    /// Zero-OPC/zero-reflection NativeAOT implementation resolving relative URIs against application base directory.
    /// </summary>
    public static partial class BaseUriHelper
    {
        private static readonly Uri _defaultAppBaseUri = CreateDefaultAppBaseUri();
        private static Uri _baseUri = _defaultAppBaseUri;
        private static Assembly _resourceAssembly;

        public static readonly DependencyProperty BaseUriProperty =
            DependencyProperty.RegisterAttached(
                "BaseUri",
                typeof(Uri),
                typeof(BaseUriHelper),
                new PropertyMetadata((object)null));

        public static Uri GetBaseUri(DependencyObject element)
        {
            Uri baseUri = GetBaseUriCore(element);

            if (baseUri == null)
            {
                baseUri = BaseUri;
            }
            else if (!baseUri.IsAbsoluteUri)
            {
                baseUri = new Uri(BaseUri, baseUri);
            }

            return baseUri;
        }

        internal static Func<DependencyObject, DependencyObject> GetLogicalParentHook { get; set; }

        internal static Uri GetBaseUriCore(DependencyObject element)
        {
            ArgumentNullException.ThrowIfNull(element);

            DependencyObject current = element;
            while (current != null)
            {
                if (current.GetValue(BaseUriProperty) is Uri uri)
                {
                    return uri;
                }

                if (current is IUriContext uriContext && uriContext.BaseUri != null)
                {
                    return uriContext.BaseUri;
                }

                DependencyObject parent = GetLogicalParentHook?.Invoke(current);
                if (parent == null && current is Visual visual)
                {
                    parent = VisualTreeHelper.GetParent(visual);
                }

                current = parent;
            }

            return null;
        }

        internal static Uri GetResolvedUri(Uri baseUri, Uri orgUri)
        {
            return baseUri != null ? new Uri(baseUri, orgUri) : orgUri;
        }

        internal static Uri FixFileUri(Uri uri)
        {
            if (uri is not null && uri.IsAbsoluteUri &&
                string.Equals(uri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase) &&
                string.Compare(uri.OriginalString, 0, Uri.UriSchemeFile, 0, Uri.UriSchemeFile.Length, StringComparison.OrdinalIgnoreCase) != 0)
            {
                return new Uri(uri.AbsoluteUri);
            }

            return uri;
        }

        internal static Uri BaseUri
        {
            get => _baseUri;
            set => _baseUri = value ?? _defaultAppBaseUri;
        }

        internal static Uri PackAppBaseUri => _defaultAppBaseUri;

        internal static Assembly ResourceAssembly
        {
            get => _resourceAssembly;
            set => _resourceAssembly = value;
        }

        private static Uri CreateDefaultAppBaseUri()
        {
            string baseDir = AppContext.BaseDirectory;
            if (string.IsNullOrEmpty(baseDir))
            {
                baseDir = Environment.CurrentDirectory;
            }

            if (!baseDir.EndsWith(System.IO.Path.DirectorySeparatorChar) &&
                !baseDir.EndsWith(System.IO.Path.AltDirectorySeparatorChar))
            {
                baseDir += System.IO.Path.DirectorySeparatorChar;
            }

            return new Uri(baseDir, UriKind.Absolute);
        }
    }
}

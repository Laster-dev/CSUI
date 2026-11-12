// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.IO.Packaging;
using System.Reflection;

namespace System.Windows.Navigation
{
    public static partial class BaseUriHelper
    {
        private const string _packageApplicationBaseUriEscaped = "application:///";
        private static readonly Uri _siteOfOriginBaseUri = PackUriHelper.Create(new Uri("siteoforigin://"));

        internal static Uri SiteOfOriginBaseUri => _siteOfOriginBaseUri;

        /// <summary>
        /// Checks whether the input uri is in the "pack://application:,,," form.
        /// </summary>
        internal static bool IsPackApplicationUri(Uri uri)
        {
            if (uri == null || !uri.IsAbsoluteUri)
            {
                return false;
            }

            if (!string.Equals(uri.Scheme, PackUriHelper.UriSchemePack, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            Uri packageUri = PackUriHelper.GetPackageUri(uri);
            return packageUri != null && string.Equals(
                packageUri.GetComponents(UriComponents.AbsoluteUri, UriFormat.UriEscaped),
                _packageApplicationBaseUriEscaped,
                StringComparison.OrdinalIgnoreCase);
        }

        internal static void GetAssemblyAndPartNameFromPackAppUri(Uri uri, out Assembly assembly, out string partName)
        {
            Uri partUri = new Uri(uri.AbsolutePath, UriKind.Relative);
            GetAssemblyNameAndPart(partUri, out partName, out string assemblyName, out _, out _);

            if (string.IsNullOrEmpty(assemblyName))
            {
                assembly = ResourceAssembly ?? Assembly.GetEntryAssembly() ?? typeof(BaseUriHelper).Assembly;
            }
            else if (string.Equals(assemblyName, "PresentationCore", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(assemblyName, "CSUI.Core", StringComparison.OrdinalIgnoreCase))
            {
                assembly = typeof(BaseUriHelper).Assembly;
            }
            else
            {
                assembly = FindLoadedAssembly(assemblyName) ?? ResourceAssembly ?? typeof(BaseUriHelper).Assembly;
            }
        }

        private static Assembly FindLoadedAssembly(string assemblyName)
        {
            Assembly[] loaded = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < loaded.Length; i++)
            {
                if (string.Equals(loaded[i].GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase))
                {
                    return loaded[i];
                }
            }

            return null;
        }

        private static void GetAssemblyNameAndPart(
            Uri uri,
            out string partName,
            out string assemblyName,
            out string assemblyVersion,
            out string assemblyKey)
        {
            string original = uri.ToString();
            int start = 0;
            if (original.Length > 0 && original[0] == '/')
            {
                start = 1;
            }

            partName = original.Substring(start);
            assemblyName = string.Empty;
            assemblyVersion = string.Empty;
            assemblyKey = string.Empty;

            int firstSlash = partName.IndexOf('/');
            string firstSegment = firstSlash > 0 ? partName.Substring(0, firstSlash) : string.Empty;
            int componentIndex = firstSegment.IndexOf(";component", StringComparison.OrdinalIgnoreCase);
            if (componentIndex > 0)
            {
                partName = firstSlash + 1 < partName.Length ? partName.Substring(firstSlash + 1) : string.Empty;
                string asmToken = firstSegment.Substring(0, componentIndex);
                string[] parts = asmToken.Split(';');
                assemblyName = parts.Length > 0 ? Uri.UnescapeDataString(parts[0]) : string.Empty;
                for (int i = 1; i < parts.Length; i++)
                {
                    if (parts[i].StartsWith("v", StringComparison.OrdinalIgnoreCase))
                    {
                        assemblyVersion = parts[i].Substring(1);
                    }
                    else if (!string.IsNullOrEmpty(parts[i]))
                    {
                        assemblyKey = parts[i];
                    }
                }
            }
        }
    }
}

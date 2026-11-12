// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Adapted for CSUI NativeAOT: parser-free ProvideValueServiceProvider implementing IServiceProvider, IProvideValueTarget, and IUriContext.

using System;
using System.Windows.Markup;

#nullable disable

namespace System.Windows.Markup
{
    /// <summary>
    /// Minimal zero-reflection service provider passed to MarkupExtension.ProvideValue during
    /// programmatic template/style evaluation.
    /// </summary>
    internal class ProvideValueServiceProvider : IServiceProvider, IProvideValueTarget, IUriContext
    {
        internal ProvideValueServiceProvider()
        {
        }

        internal void SetData(object targetObject, object targetProperty)
        {
            _targetObject = targetObject;
            _targetProperty = targetProperty;
        }

        internal void ClearData()
        {
            _targetObject = null;
            _targetProperty = null;
        }

        object IProvideValueTarget.TargetObject => _targetObject;

        object IProvideValueTarget.TargetProperty => _targetProperty;

        Uri IUriContext.BaseUri
        {
            get => _baseUri;
            set => _baseUri = value;
        }

        public object GetService(Type service)
        {
            if (service == typeof(IProvideValueTarget))
            {
                return this as IProvideValueTarget;
            }
            if (service == typeof(IUriContext))
            {
                return this as IUriContext;
            }

            return null;
        }

        private object _targetObject;
        private object _targetProperty;
        private Uri _baseUri;
    }
}

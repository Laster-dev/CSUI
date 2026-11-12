// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Windows.Navigation;

namespace System.Windows.Controls
{
    /// <summary>
    /// Managed SoundPlayerAction without legacy winmm.dll synchronous audio blocking.
    /// </summary>
    public class SoundPlayerAction : TriggerAction, IDisposable
    {
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(
                nameof(Source),
                typeof(Uri),
                typeof(SoundPlayerAction),
                new FrameworkPropertyMetadata(null, OnSourceChanged));

        private Uri _resolvedSource;

        public SoundPlayerAction()
        {
        }

        public Uri Source
        {
            get => (Uri)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SoundPlayerAction action)
            {
                Uri uri = e.NewValue as Uri;
                action._resolvedSource = uri == null || uri.IsAbsoluteUri
                    ? uri
                    : BaseUriHelper.GetResolvedUri(BaseUriHelper.BaseUri, uri);
            }
        }

        internal override void Invoke(
            FrameworkElement fe,
            FrameworkContentElement fce,
            Style targetStyle,
            FrameworkTemplate targetTemplate,
            long layer)
        {
        }

        internal override void Invoke(FrameworkElement fe)
        {
        }
    }
}

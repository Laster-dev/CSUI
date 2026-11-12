// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Windows.Automation.Peers;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace System.Windows.Controls
{
    public enum MediaState : int
    {
        Manual = 0,
        Play = 1,
        Close = 2,
        Pause = 3,
        Stop = 4
    }

    public sealed class MediaScriptCommandRoutedEventArgs : RoutedEventArgs
    {
        internal MediaScriptCommandRoutedEventArgs(
            RoutedEvent routedEvent,
            object sender,
            string parameterType,
            string parameterValue)
            : base(routedEvent, sender)
        {
            ArgumentNullException.ThrowIfNull(parameterType);
            ArgumentNullException.ThrowIfNull(parameterValue);

            ParameterType = parameterType;
            ParameterValue = parameterValue;
        }

        public string ParameterType { get; }
        public string ParameterValue { get; }
    }

    public class MediaElement : FrameworkElement, IUriContext
    {
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(
                nameof(Source),
                typeof(Uri),
                typeof(MediaElement),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnSourceChanged));

        public static readonly DependencyProperty VolumeProperty =
            DependencyProperty.Register(
                nameof(Volume),
                typeof(double),
                typeof(MediaElement),
                new FrameworkPropertyMetadata(0.5));

        public static readonly DependencyProperty BalanceProperty =
            DependencyProperty.Register(
                nameof(Balance),
                typeof(double),
                typeof(MediaElement),
                new FrameworkPropertyMetadata(0.0));

        public static readonly DependencyProperty IsMutedProperty =
            DependencyProperty.Register(
                nameof(IsMuted),
                typeof(bool),
                typeof(MediaElement),
                new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty ScrubbingEnabledProperty =
            DependencyProperty.Register(
                nameof(ScrubbingEnabled),
                typeof(bool),
                typeof(MediaElement),
                new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty UnloadedBehaviorProperty =
            DependencyProperty.Register(
                nameof(UnloadedBehavior),
                typeof(MediaState),
                typeof(MediaElement),
                new FrameworkPropertyMetadata(MediaState.Close));

        public static readonly DependencyProperty LoadedBehaviorProperty =
            DependencyProperty.Register(
                nameof(LoadedBehavior),
                typeof(MediaState),
                typeof(MediaElement),
                new FrameworkPropertyMetadata(MediaState.Play));

        public static readonly DependencyProperty StretchProperty =
            Viewbox.StretchProperty.AddOwner(typeof(MediaElement));

        public static readonly DependencyProperty StretchDirectionProperty =
            Viewbox.StretchDirectionProperty.AddOwner(typeof(MediaElement));

        public static readonly RoutedEvent MediaFailedEvent =
            EventManager.RegisterRoutedEvent<EventHandler<ExceptionRoutedEventArgs>>(
                nameof(MediaFailed),
                RoutingStrategy.Bubble,
                typeof(MediaElement),
                static (handler, sender, args) => handler(sender, (ExceptionRoutedEventArgs)args));

        public static readonly RoutedEvent MediaOpenedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(MediaOpened),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(MediaElement));

        public static readonly RoutedEvent BufferingStartedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(BufferingStarted),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(MediaElement));

        public static readonly RoutedEvent BufferingEndedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(BufferingEnded),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(MediaElement));

        public static readonly RoutedEvent ScriptCommandEvent =
            EventManager.RegisterRoutedEvent<EventHandler<MediaScriptCommandRoutedEventArgs>>(
                nameof(ScriptCommand),
                RoutingStrategy.Bubble,
                typeof(MediaElement),
                static (handler, sender, args) => handler(sender, (MediaScriptCommandRoutedEventArgs)args));

        public static readonly RoutedEvent MediaEndedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(MediaEnded),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(MediaElement));

        private MediaClock _clock;
        private MediaState _currentState = MediaState.Close;
        private TimeSpan _position = TimeSpan.Zero;
        private double _speedRatio = 1.0;

        static MediaElement()
        {
            Style style = new Style(typeof(MediaElement), null);
            style.Setters.Add(new Setter(FlowDirectionProperty, FlowDirection.LeftToRight));
            style.Seal();
            StyleProperty.OverrideMetadata(typeof(MediaElement), new FrameworkPropertyMetadata(style));

            StretchProperty.OverrideMetadata(
                typeof(MediaElement),
                new FrameworkPropertyMetadata(Stretch.Uniform, FrameworkPropertyMetadataOptions.AffectsMeasure));

            StretchDirectionProperty.OverrideMetadata(
                typeof(MediaElement),
                new FrameworkPropertyMetadata(StretchDirection.Both, FrameworkPropertyMetadataOptions.AffectsMeasure));
        }

        public MediaElement()
        {
        }

        public Uri Source
        {
            get => (Uri)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public MediaClock Clock
        {
            get => _clock;
            set => _clock = value;
        }

        public Stretch Stretch
        {
            get => (Stretch)GetValue(StretchProperty);
            set => SetValue(StretchProperty, value);
        }

        public StretchDirection StretchDirection
        {
            get => (StretchDirection)GetValue(StretchDirectionProperty);
            set => SetValue(StretchDirectionProperty, value);
        }

        public double Volume
        {
            get => (double)GetValue(VolumeProperty);
            set => SetValue(VolumeProperty, value);
        }

        public double Balance
        {
            get => (double)GetValue(BalanceProperty);
            set => SetValue(BalanceProperty, value);
        }

        public bool IsMuted
        {
            get => (bool)GetValue(IsMutedProperty);
            set => SetValue(IsMutedProperty, value);
        }

        public bool ScrubbingEnabled
        {
            get => (bool)GetValue(ScrubbingEnabledProperty);
            set => SetValue(ScrubbingEnabledProperty, value);
        }

        public MediaState UnloadedBehavior
        {
            get => (MediaState)GetValue(UnloadedBehaviorProperty);
            set => SetValue(UnloadedBehaviorProperty, value);
        }

        public MediaState LoadedBehavior
        {
            get => (MediaState)GetValue(LoadedBehaviorProperty);
            set => SetValue(LoadedBehaviorProperty, value);
        }

        public bool CanPause => _currentState == MediaState.Play || _currentState == MediaState.Pause;
        public bool IsBuffering => false;
        public double DownloadProgress => Source != null ? 1.0 : 0.0;
        public double BufferingProgress => Source != null ? 1.0 : 0.0;
        public int NaturalVideoHeight => 0;
        public int NaturalVideoWidth => 0;
        public bool HasAudio => false;
        public bool HasVideo => false;
        public Duration NaturalDuration => Duration.Automatic;

        public TimeSpan Position
        {
            get => _position;
            set => _position = value;
        }

        public double SpeedRatio
        {
            get => _speedRatio;
            set => _speedRatio = value;
        }

        public bool IsLoadedMedia => Source != null && _currentState != MediaState.Close;

        Uri IUriContext.BaseUri { get; set; }

        public event EventHandler<ExceptionRoutedEventArgs> MediaFailed
        {
            add => AddHandler(MediaFailedEvent, value);
            remove => RemoveHandler(MediaFailedEvent, value);
        }

        public event RoutedEventHandler MediaOpened
        {
            add => AddHandler(MediaOpenedEvent, value);
            remove => RemoveHandler(MediaOpenedEvent, value);
        }

        public event RoutedEventHandler BufferingStarted
        {
            add => AddHandler(BufferingStartedEvent, value);
            remove => RemoveHandler(BufferingStartedEvent, value);
        }

        public event RoutedEventHandler BufferingEnded
        {
            add => AddHandler(BufferingEndedEvent, value);
            remove => RemoveHandler(BufferingEndedEvent, value);
        }

        public event EventHandler<MediaScriptCommandRoutedEventArgs> ScriptCommand
        {
            add => AddHandler(ScriptCommandEvent, value);
            remove => RemoveHandler(ScriptCommandEvent, value);
        }

        public event RoutedEventHandler MediaEnded
        {
            add => AddHandler(MediaEndedEvent, value);
            remove => RemoveHandler(MediaEndedEvent, value);
        }

        public void Play()
        {
            if (LoadedBehavior == MediaState.Manual)
            {
                _currentState = MediaState.Play;
            }
        }

        public void Pause()
        {
            if (LoadedBehavior == MediaState.Manual)
            {
                _currentState = MediaState.Pause;
            }
        }

        public void Stop()
        {
            if (LoadedBehavior == MediaState.Manual)
            {
                _currentState = MediaState.Stop;
                _position = TimeSpan.Zero;
            }
        }

        public void Close()
        {
            if (LoadedBehavior == MediaState.Manual)
            {
                _currentState = MediaState.Close;
                _position = TimeSpan.Zero;
            }
        }

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MediaElement media && e.NewValue is Uri)
            {
                if (media.LoadedBehavior == MediaState.Play)
                {
                    media._currentState = MediaState.Play;
                }
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new MediaElementAutomationPeer(this);

        protected override Size MeasureOverride(Size availableSize)
        {
            double w = double.IsInfinity(availableSize.Width) ? 0.0 : availableSize.Width;
            double h = double.IsInfinity(availableSize.Height) ? 0.0 : availableSize.Height;
            return new Size(w, h);
        }

        protected override Size ArrangeOverride(Size finalSize) => finalSize;
    }
}

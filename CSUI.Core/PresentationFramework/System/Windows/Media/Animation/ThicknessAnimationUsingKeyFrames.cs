#nullable disable

using System.Collections;
using System.ComponentModel;
using System.Windows.Markup;

namespace System.Windows.Media.Animation
{
    public abstract class ThicknessKeyFrame : Freezable, IKeyFrame
    {
        public static readonly DependencyProperty KeyTimeProperty =
            DependencyProperty.Register(
                "KeyTime",
                typeof(KeyTime),
                typeof(ThicknessKeyFrame),
                new PropertyMetadata(KeyTime.Uniform));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                "Value",
                typeof(Thickness),
                typeof(ThicknessKeyFrame),
                new PropertyMetadata(default(Thickness)));

        protected ThicknessKeyFrame()
        {
        }

        protected ThicknessKeyFrame(Thickness value)
        {
            Value = value;
        }

        protected ThicknessKeyFrame(Thickness value, KeyTime keyTime)
        {
            Value = value;
            KeyTime = keyTime;
        }

        public KeyTime KeyTime
        {
            get => (KeyTime)GetValue(KeyTimeProperty);
            set => SetValue(KeyTimeProperty, value);
        }

        public Thickness Value
        {
            get => (Thickness)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        object IKeyFrame.Value
        {
            get => Value;
            set => Value = (Thickness)value;
        }

        public Thickness InterpolateValue(Thickness baseValue, double keyFrameProgress)
        {
            if (keyFrameProgress <= 0.0) return baseValue;
            if (keyFrameProgress >= 1.0) return Value;
            return InterpolateValueCore(baseValue, keyFrameProgress);
        }

        protected abstract Thickness InterpolateValueCore(Thickness baseValue, double keyFrameProgress);
    }

    public class LinearThicknessKeyFrame : ThicknessKeyFrame
    {
        public LinearThicknessKeyFrame() { }
        public LinearThicknessKeyFrame(Thickness value) : base(value) { }
        public LinearThicknessKeyFrame(Thickness value, KeyTime keyTime) : base(value, keyTime) { }

        protected override Freezable CreateInstanceCore() => new LinearThicknessKeyFrame();

        protected override Thickness InterpolateValueCore(Thickness baseValue, double keyFrameProgress)
        {
            Thickness target = Value;
            return new Thickness(
                baseValue.Left + (target.Left - baseValue.Left) * keyFrameProgress,
                baseValue.Top + (target.Top - baseValue.Top) * keyFrameProgress,
                baseValue.Right + (target.Right - baseValue.Right) * keyFrameProgress,
                baseValue.Bottom + (target.Bottom - baseValue.Bottom) * keyFrameProgress);
        }
    }

    public class DiscreteThicknessKeyFrame : ThicknessKeyFrame
    {
        public DiscreteThicknessKeyFrame() { }
        public DiscreteThicknessKeyFrame(Thickness value) : base(value) { }
        public DiscreteThicknessKeyFrame(Thickness value, KeyTime keyTime) : base(value, keyTime) { }

        protected override Freezable CreateInstanceCore() => new DiscreteThicknessKeyFrame();

        protected override Thickness InterpolateValueCore(Thickness baseValue, double keyFrameProgress)
            => keyFrameProgress < 1.0 ? baseValue : Value;
    }

    public class SplineThicknessKeyFrame : ThicknessKeyFrame
    {
        public static readonly DependencyProperty KeySplineProperty =
            DependencyProperty.Register(
                "KeySpline",
                typeof(KeySpline),
                typeof(SplineThicknessKeyFrame),
                new PropertyMetadata(new KeySpline()));

        public SplineThicknessKeyFrame() { }
        public SplineThicknessKeyFrame(Thickness value) : base(value) { }
        public SplineThicknessKeyFrame(Thickness value, KeyTime keyTime) : base(value, keyTime) { }
        public SplineThicknessKeyFrame(Thickness value, KeyTime keyTime, KeySpline keySpline)
            : base(value, keyTime)
        {
            KeySpline = keySpline;
        }

        public KeySpline KeySpline
        {
            get => (KeySpline)GetValue(KeySplineProperty);
            set => SetValue(KeySplineProperty, value);
        }

        protected override Freezable CreateInstanceCore() => new SplineThicknessKeyFrame();

        protected override Thickness InterpolateValueCore(Thickness baseValue, double keyFrameProgress)
        {
            double p = KeySpline?.GetSplineProgress(keyFrameProgress) ?? keyFrameProgress;
            Thickness target = Value;
            return new Thickness(
                baseValue.Left + (target.Left - baseValue.Left) * p,
                baseValue.Top + (target.Top - baseValue.Top) * p,
                baseValue.Right + (target.Right - baseValue.Right) * p,
                baseValue.Bottom + (target.Bottom - baseValue.Bottom) * p);
        }
    }

    public class ThicknessKeyFrameCollection : FreezableCollection<ThicknessKeyFrame>
    {
        public static ThicknessKeyFrameCollection Empty { get; } = new ThicknessKeyFrameCollection();
    }

    [ContentProperty("KeyFrames")]
    public class ThicknessAnimationUsingKeyFrames : AnimationTimeline, IKeyFrameAnimation, IAddChild
    {
        private ThicknessKeyFrameCollection _keyFrames;

        public ThicknessKeyFrameCollection KeyFrames
        {
            get
            {
                ReadPreamble();
                if (_keyFrames == null)
                {
                    _keyFrames = new ThicknessKeyFrameCollection();
                }
                return _keyFrames;
            }
            set
            {
                WritePreamble();
                _keyFrames = value;
                WritePostscript();
            }
        }

        IList IKeyFrameAnimation.KeyFrames
        {
            get => KeyFrames;
            set => KeyFrames = (ThicknessKeyFrameCollection)value;
        }

        public override Type TargetPropertyType => typeof(Thickness);

        protected override Freezable CreateInstanceCore() => new ThicknessAnimationUsingKeyFrames();

        public override object GetCurrentValue(
            object defaultOriginValue,
            object defaultDestinationValue,
            AnimationClock animationClock)
        {
            if (_keyFrames == null || _keyFrames.Count == 0 || animationClock == null)
            {
                return defaultDestinationValue;
            }
            double? progress = animationClock.CurrentProgress;
            if (!progress.HasValue)
            {
                return defaultDestinationValue;
            }
            Thickness origin = defaultOriginValue is Thickness t ? t : default;
            if (progress.Value >= 1.0)
            {
                return _keyFrames[_keyFrames.Count - 1].Value;
            }
            int idx = Math.Min(_keyFrames.Count - 1, Math.Max(0, (int)(progress.Value * _keyFrames.Count)));
            return _keyFrames[idx].InterpolateValue(origin, progress.Value);
        }

        void IAddChild.AddChild(object value)
        {
            if (value is ThicknessKeyFrame kf)
            {
                KeyFrames.Add(kf);
            }
        }

        void IAddChild.AddText(string text) { }
    }
}

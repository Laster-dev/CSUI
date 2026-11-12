// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Text;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// A RepeatBehavior describes how a Timeline object may repeat its simple duration.
    /// </summary>
    public readonly struct RepeatBehavior : IFormattable
    {
        private readonly double _iterationCount;
        private readonly TimeSpan _repeatDuration;
        private readonly RepeatBehaviorType _type;

        #region Constructors

        public RepeatBehavior(double count)
        {
            if (double.IsInfinity(count) || double.IsNaN(count) || count < 0.0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.Timing_RepeatBehaviorInvalidIterationCount, count));

            _repeatDuration = TimeSpan.Zero;
            _iterationCount = count;
            _type = RepeatBehaviorType.IterationCount;
        }

        public RepeatBehavior(TimeSpan duration)
        {
            if (duration < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(duration), SR.Format(SR.Timing_RepeatBehaviorInvalidRepeatDuration, duration));

            _iterationCount = 0.0;
            _repeatDuration = duration;
            _type = RepeatBehaviorType.RepeatDuration;
        }

        private RepeatBehavior(RepeatBehaviorType behaviorType)
        {
            Debug.Assert(behaviorType == RepeatBehaviorType.Forever);

            _type = behaviorType;
        }

        #endregion

        #region Properties

        public bool HasCount => _type == RepeatBehaviorType.IterationCount;

        public bool HasDuration => _type == RepeatBehaviorType.RepeatDuration;

        public double Count
        {
            get
            {
                if (!HasCount)
                    throw new InvalidOperationException(SR.Format(SR.Timing_RepeatBehaviorNotIterationCount, this));

                return _iterationCount;
            }
        }

        public TimeSpan Duration
        {
            get
            {
                if (!HasDuration)
                    throw new InvalidOperationException(SR.Format(SR.Timing_RepeatBehaviorNotRepeatDuration, this));

                return _repeatDuration;
            }
        }

        public static RepeatBehavior Forever => new RepeatBehavior(RepeatBehaviorType.Forever);

        #endregion

        #region Methods

        public override bool Equals(object value) =>
            value is RepeatBehavior behavior && Equals(behavior);

        public bool Equals(RepeatBehavior repeatBehavior)
        {
            if (_type == repeatBehavior._type)
            {
                switch (_type)
                {
                    case RepeatBehaviorType.Forever:
                        return true;

                    case RepeatBehaviorType.IterationCount:
                        return _iterationCount == repeatBehavior._iterationCount;

                    case RepeatBehaviorType.RepeatDuration:
                        return _repeatDuration == repeatBehavior._repeatDuration;

                    default:
                        Debug.Fail("Unhandled RepeatBehaviorType");
                        return false;
                }
            }
            else
            {
                return false;
            }
        }

        public static bool Equals(RepeatBehavior repeatBehavior1, RepeatBehavior repeatBehavior2)
        {
            return repeatBehavior1.Equals(repeatBehavior2);
        }

        public override int GetHashCode()
        {
            switch (_type)
            {
                case RepeatBehaviorType.IterationCount:
                    return _iterationCount.GetHashCode();

                case RepeatBehaviorType.RepeatDuration:
                    return _repeatDuration.GetHashCode();

                case RepeatBehaviorType.Forever:
                    return int.MaxValue - 42;

                default:
                    Debug.Fail("Unhandled RepeatBehaviorType");
                    return base.GetHashCode();
            }
        }

        public override string ToString()
        {
            return InternalToString(null, null);
        }

        public string ToString(IFormatProvider formatProvider)
        {
            return InternalToString(null, formatProvider);
        }

        string IFormattable.ToString(string format, IFormatProvider formatProvider)
        {
            return InternalToString(format, formatProvider);
        }

        internal string InternalToString(string format, IFormatProvider formatProvider)
        {
            switch (_type)
            {
                case RepeatBehaviorType.Forever:
                    return "Forever";

                case RepeatBehaviorType.IterationCount:
                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat(
                        formatProvider,
                        "{0:" + format + "}x",
                        _iterationCount);
                    return sb.ToString();

                case RepeatBehaviorType.RepeatDuration:
                    return _repeatDuration.ToString();

                default:
                    Debug.Fail("Unhandled RepeatBehaviorType.");
                    return null;
            }
        }

        #endregion

        #region Operators

        public static bool operator ==(RepeatBehavior repeatBehavior1, RepeatBehavior repeatBehavior2)
        {
            return repeatBehavior1.Equals(repeatBehavior2);
        }

        public static bool operator !=(RepeatBehavior repeatBehavior1, RepeatBehavior repeatBehavior2)
        {
            return !repeatBehavior1.Equals(repeatBehavior2);
        }

        #endregion

        private enum RepeatBehaviorType
        {
            IterationCount,
            RepeatDuration,
            Forever
        }
    }
}

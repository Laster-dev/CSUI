// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Diagnostics;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows
{
    /// <summary>
    /// Duration struct provides sentinel values that are not included in TimeSpan.
    /// This structure may represent a TimeSpan, Automatic, or Forever value.
    /// </summary>
    public readonly struct Duration
    {
        private readonly TimeSpan _timeSpan;
        private readonly DurationType _durationType;

        /// <summary>
        /// Creates a Duration from a TimeSpan.
        /// </summary>
        public Duration(TimeSpan timeSpan)
        {
            if (timeSpan < TimeSpan.Zero)
                throw new ArgumentException(SR.Timing_InvalidArgNonNegative, nameof(timeSpan));

            _durationType = DurationType.TimeSpan;
            _timeSpan = timeSpan;
        }

        private Duration(DurationType durationType)
        {
            Debug.Assert(durationType == DurationType.Automatic || durationType == DurationType.Forever);

            _durationType = durationType;
        }

        #region Operators

        public static implicit operator Duration(TimeSpan timeSpan)
        {
            if (timeSpan < TimeSpan.Zero)
                throw new ArgumentException(SR.Timing_InvalidArgNonNegative, nameof(timeSpan));

            return new Duration(timeSpan);
        }

        public static Duration operator +(Duration t1, Duration t2)
        {
            if (t1.HasTimeSpan && t2.HasTimeSpan)
            {
                return new Duration(t1._timeSpan + t2._timeSpan);
            }
            else if (t1._durationType != DurationType.Automatic
                     && t2._durationType != DurationType.Automatic)
            {
                return Duration.Forever;
            }
            else
            {
                return Duration.Automatic;
            }
        }

        public static Duration operator -(Duration t1, Duration t2)
        {
            if (t1.HasTimeSpan && t2.HasTimeSpan)
            {
                return new Duration(t1._timeSpan - t2._timeSpan);
            }
            else if (t1._durationType == DurationType.Forever
                     && t2.HasTimeSpan)
            {
                return Duration.Forever;
            }
            else
            {
                return Duration.Automatic;
            }
        }

        public static bool operator ==(Duration t1, Duration t2)
        {
            return t1.Equals(t2);
        }

        public static bool operator !=(Duration t1, Duration t2)
        {
            return !(t1.Equals(t2));
        }

        public static bool operator >(Duration t1, Duration t2)
        {
            if (t1.HasTimeSpan && t2.HasTimeSpan)
            {
                return t1._timeSpan > t2._timeSpan;
            }
            else if (t1.HasTimeSpan && t2._durationType == DurationType.Forever)
            {
                return false;
            }
            else if (t1._durationType == DurationType.Forever && t2.HasTimeSpan)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool operator >=(Duration t1, Duration t2)
        {
            if (t1._durationType == DurationType.Automatic && t2._durationType == DurationType.Automatic)
            {
                return true;
            }
            else if (t1._durationType == DurationType.Automatic || t2._durationType == DurationType.Automatic)
            {
                return false;
            }
            else
            {
                return !(t1 < t2);
            }
        }

        public static bool operator <(Duration t1, Duration t2)
        {
            if (t1.HasTimeSpan && t2.HasTimeSpan)
            {
                return t1._timeSpan < t2._timeSpan;
            }
            else if (t1.HasTimeSpan && t2._durationType == DurationType.Forever)
            {
                return true;
            }
            else if (t1._durationType == DurationType.Forever && t2.HasTimeSpan)
            {
                return false;
            }
            else
            {
                return false;
            }
        }

        public static bool operator <=(Duration t1, Duration t2)
        {
            if (t1._durationType == DurationType.Automatic && t2._durationType == DurationType.Automatic)
            {
                return true;
            }
            else if (t1._durationType == DurationType.Automatic || t2._durationType == DurationType.Automatic)
            {
                return false;
            }
            else
            {
                return !(t1 > t2);
            }
        }

        public static int Compare(Duration t1, Duration t2)
        {
            if (t1._durationType == DurationType.Automatic)
            {
                if (t2._durationType == DurationType.Automatic)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            else if (t2._durationType == DurationType.Automatic)
            {
                return 1;
            }
            else
            {
                if (t1 < t2)
                {
                    return -1;
                }
                else if (t1 > t2)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }

        public static Duration Plus(Duration duration)
        {
            return duration;
        }

        public static Duration operator +(Duration duration)
        {
            return duration;
        }

        #endregion

        #region Properties

        public bool HasTimeSpan => _durationType == DurationType.TimeSpan;

        public static Duration Automatic => new Duration(DurationType.Automatic);

        public static Duration Forever => new Duration(DurationType.Forever);

        public TimeSpan TimeSpan =>
            HasTimeSpan ? _timeSpan : throw new InvalidOperationException(SR.Format(SR.Timing_NotTimeSpan, this));

        #endregion

        #region Methods

        public Duration Add(Duration duration) => this + duration;

        public override bool Equals(object value) => value is Duration duration && Equals(duration);

        public bool Equals(Duration duration)
        {
            if (HasTimeSpan)
            {
                if (duration.HasTimeSpan)
                {
                    return _timeSpan == duration._timeSpan;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return _durationType == duration._durationType;
            }
        }

        public static bool Equals(Duration t1, Duration t2) => t1.Equals(t2);

        public override int GetHashCode() =>
            HasTimeSpan ? _timeSpan.GetHashCode() : _durationType.GetHashCode() + 17;

        public Duration Subtract(Duration duration) => this - duration;

        public override string ToString()
        {
            return HasTimeSpan ? _timeSpan.ToString() : ToStringInvariant();
        }

        internal string ToStringInvariant()
        {
            if (_durationType == DurationType.Forever)
                return "Forever";
            else if (_durationType == DurationType.Automatic)
                return "Automatic";

            return _timeSpan.ToString();
        }

        #endregion

        private enum DurationType
        {
            Automatic,
            TimeSpan,
            Forever
        }
    }
}

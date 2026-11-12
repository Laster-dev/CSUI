// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel;
using System.Windows.Threading;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// Provides access to interactive methods for a given Clock.
    /// </summary>
    public sealed class ClockController : DispatcherObject
    {
        private Clock _owner;

        internal ClockController(Clock owner)
        {
            Debug.Assert(owner != null, "ClockController must have a non-null owner.");
            _owner = owner;
        }

        public void Begin()
        {
            _owner.InternalBegin();
        }

        public void SkipToFill()
        {
            _owner.InternalSkipToFill();
        }

        public void Pause()
        {
            _owner.InternalPause();
        }

        public void Resume()
        {
            _owner.InternalResume();
        }

        public void Seek(TimeSpan offset, TimeSeekOrigin origin)
        {
            if (!MS.Internal.TimeEnumHelper.IsValidTimeSeekOrigin(origin))
            {
                throw new InvalidEnumArgumentException(SR.Format(SR.Enum_Invalid, "TimeSeekOrigin"));
            }

            if (origin == TimeSeekOrigin.Duration)
            {
                Duration duration = _owner.ResolvedDuration;

                if (!duration.HasTimeSpan)
                {
                    throw new InvalidOperationException(SR.Timing_SeekDestinationIndefinite);
                }
                else
                {
                    offset += duration.TimeSpan;
                }
            }

            if (offset < TimeSpan.Zero)
            {
                throw new InvalidOperationException(SR.Timing_SeekDestinationNegative);
            }

            _owner.InternalSeek(offset);
        }

        public void SeekAlignedToLastTick(TimeSpan offset, TimeSeekOrigin origin)
        {
            if (!MS.Internal.TimeEnumHelper.IsValidTimeSeekOrigin(origin))
            {
                throw new InvalidEnumArgumentException(SR.Format(SR.Enum_Invalid, "TimeSeekOrigin"));
            }

            if (origin == TimeSeekOrigin.Duration)
            {
                Duration duration = _owner.ResolvedDuration;

                if (!duration.HasTimeSpan)
                {
                    throw new InvalidOperationException(SR.Timing_SeekDestinationIndefinite);
                }
                else
                {
                    offset += duration.TimeSpan;
                }
            }

            if (offset < TimeSpan.Zero)
            {
                throw new InvalidOperationException(SR.Timing_SeekDestinationNegative);
            }

            _owner.InternalSeekAlignedToLastTick(offset);
        }

        public void Stop()
        {
            _owner.InternalStop();
        }

        public void Remove()
        {
            _owner.InternalRemove();
        }

        public Clock Clock => _owner;

        public double SpeedRatio
        {
            get
            {
                return _owner.InternalGetSpeedRatio();
            }
            set
            {
                if (value < 0 || value > double.MaxValue || double.IsNaN(value))
                {
                    throw new ArgumentException(SR.Timing_InvalidArgFinitePositive, nameof(value));
                }

                _owner.InternalSetSpeedRatio(value);
            }
        }
    }
}

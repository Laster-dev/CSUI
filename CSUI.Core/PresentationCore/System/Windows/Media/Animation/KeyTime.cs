// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Globalization;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// A KeyTime is used to specify when relative to the time of an animation
    /// that a KeyFrame takes place.
    /// </summary>
    public struct KeyTime : IEquatable<KeyTime>
    {
        private object _value;
        private KeyTimeType _type;

        #region Static Create Methods

        public static KeyTime FromPercent(double percent)
        {
            if (percent < 0.0 || percent > 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(percent), SR.Format(SR.Animation_KeyTime_InvalidPercentValue, percent));
            }

            KeyTime keyTime = new KeyTime
            {
                _value = percent,
                _type = KeyTimeType.Percent
            };

            return keyTime;
        }

        public static KeyTime FromTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(timeSpan), SR.Format(SR.Animation_KeyTime_LessThanZero, timeSpan));
            }

            KeyTime keyTime = new KeyTime
            {
                _value = timeSpan,
                _type = KeyTimeType.TimeSpan
            };

            return keyTime;
        }

        public static KeyTime Uniform
        {
            get
            {
                KeyTime keyTime = new KeyTime
                {
                    _type = KeyTimeType.Uniform
                };

                return keyTime;
            }
        }

        public static KeyTime Paced
        {
            get
            {
                KeyTime keyTime = new KeyTime
                {
                    _type = KeyTimeType.Paced
                };

                return keyTime;
            }
        }

        #endregion

        #region Operators

        public static bool Equals(KeyTime keyTime1, KeyTime keyTime2)
        {
            if (keyTime1._type == keyTime2._type)
            {
                switch (keyTime1._type)
                {
                    case KeyTimeType.Uniform:
                        break;

                    case KeyTimeType.Paced:
                        break;

                    case KeyTimeType.Percent:
                        if ((double)keyTime1._value != (double)keyTime2._value)
                        {
                            return false;
                        }
                        break;

                    case KeyTimeType.TimeSpan:
                        if ((TimeSpan)keyTime1._value != (TimeSpan)keyTime2._value)
                        {
                            return false;
                        }
                        break;
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool operator ==(KeyTime keyTime1, KeyTime keyTime2)
        {
            return KeyTime.Equals(keyTime1, keyTime2);
        }

        public static bool operator !=(KeyTime keyTime1, KeyTime keyTime2)
        {
            return !KeyTime.Equals(keyTime1, keyTime2);
        }

        #endregion

        #region IEquatable<KeyTime>

        public bool Equals(KeyTime value)
        {
            return KeyTime.Equals(this, value);
        }

        #endregion

        #region Object Overrides

        public override bool Equals(object value)
        {
            if (value == null || !(value is KeyTime))
            {
                return false;
            }

            return this == (KeyTime)value;
        }

        public override int GetHashCode()
        {
            if (_value != null)
            {
                return _value.GetHashCode();
            }
            else
            {
                return _type.GetHashCode();
            }
        }

        public override string ToString()
        {
            switch (_type)
            {
                case KeyTimeType.Uniform:
                    return "Uniform";

                case KeyTimeType.Paced:
                    return "Paced";

                case KeyTimeType.Percent:
                    return (((double)_value) * 100.0).ToString(CultureInfo.CurrentCulture) + "%";

                case KeyTimeType.TimeSpan:
                    return ((TimeSpan)_value).ToString();

                default:
                    return "Uniform";
            }
        }

        #endregion

        #region Implicit Converters

        public static implicit operator KeyTime(TimeSpan timeSpan)
        {
            return KeyTime.FromTimeSpan(timeSpan);
        }

        #endregion

        #region Public Properties

        public TimeSpan TimeSpan
        {
            get
            {
                if (_type == KeyTimeType.TimeSpan)
                {
                    return (TimeSpan)_value;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        public double Percent
        {
            get
            {
                if (_type == KeyTimeType.Percent)
                {
                    return (double)_value;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        public KeyTimeType Type => _type;

        #endregion
    }
}

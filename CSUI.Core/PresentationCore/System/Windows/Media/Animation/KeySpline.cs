// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// This class is used by a SplineKeyFrame to define a key frame animation.
    /// </summary>
    public class KeySpline : Freezable, IFormattable
    {
        #region Constructors

        public KeySpline()
            : base()
        {
            _controlPoint1 = new Point(0.0, 0.0);
            _controlPoint2 = new Point(1.0, 1.0);
        }

        public KeySpline(double x1, double y1, double x2, double y2)
            : this(new Point(x1, y1), new Point(x2, y2))
        {
        }

        public KeySpline(Point controlPoint1, Point controlPoint2)
            : base()
        {
            if (!IsValidControlPoint(controlPoint1))
            {
                throw new ArgumentException(SR.Format(
                    SR.Animation_KeySpline_InvalidValue,
                    "controlPoint1",
                    controlPoint1));
            }

            if (!IsValidControlPoint(controlPoint2))
            {
                throw new ArgumentException(SR.Format(
                    SR.Animation_KeySpline_InvalidValue,
                    "controlPoint2",
                    controlPoint2));
            }

            _controlPoint1 = controlPoint1;
            _controlPoint2 = controlPoint2;

            _isDirty = true;
        }

        #endregion

        #region Freezable

        protected override Freezable CreateInstanceCore()
        {
            return new KeySpline();
        }

        protected override void CloneCore(Freezable sourceFreezable)
        {
            KeySpline sourceKeySpline = (KeySpline)sourceFreezable;
            base.CloneCore(sourceFreezable);
            CloneCommon(sourceKeySpline);
        }

        protected override void CloneCurrentValueCore(Freezable sourceFreezable)
        {
            KeySpline sourceKeySpline = (KeySpline)sourceFreezable;
            base.CloneCurrentValueCore(sourceFreezable);
            CloneCommon(sourceKeySpline);
        }

        protected override void GetAsFrozenCore(Freezable sourceFreezable)
        {
            KeySpline sourceKeySpline = (KeySpline)sourceFreezable;
            base.GetAsFrozenCore(sourceFreezable);
            CloneCommon(sourceKeySpline);
        }

        protected override void GetCurrentValueAsFrozenCore(Freezable sourceFreezable)
        {
            KeySpline sourceKeySpline = (KeySpline)sourceFreezable;
            base.GetCurrentValueAsFrozenCore(sourceFreezable);
            CloneCommon(sourceKeySpline);
        }

        protected override void OnChanged()
        {
            _isDirty = true;

            base.OnChanged();
        }

        #endregion

        #region Public

        public Point ControlPoint1
        {
            get
            {
                ReadPreamble();

                return _controlPoint1;
            }
            set
            {
                WritePreamble();

                if (value != _controlPoint1)
                {
                    if (!IsValidControlPoint(value))
                    {
                        throw new ArgumentException(SR.Format(
                            SR.Animation_KeySpline_InvalidValue,
                            "ControlPoint1",
                            value));
                    }

                    _controlPoint1 = value;

                    WritePostscript();
                }
            }
        }

        public Point ControlPoint2
        {
            get
            {
                ReadPreamble();

                return _controlPoint2;
            }
            set
            {
                WritePreamble();

                if (value != _controlPoint2)
                {
                    if (!IsValidControlPoint(value))
                    {
                        throw new ArgumentException(SR.Format(
                            SR.Animation_KeySpline_InvalidValue,
                            "ControlPoint2",
                            value));
                    }

                    _controlPoint2 = value;

                    WritePostscript();
                }
            }
        }

        public double GetSplineProgress(double linearProgress)
        {
            ReadPreamble();

            if (_isDirty)
            {
                Build();
            }

            if (!_isSpecified)
            {
                return linearProgress;
            }
            else
            {
                SetParameterFromX(linearProgress);

                return GetBezierValue(_By, _Cy, _parameter);
            }
        }

        #endregion

        #region Private

        private bool IsValidControlPoint(Point point)
        {
            return point.X >= 0.0
                && point.X <= 1.0;
        }

        private void Build()
        {
            Debug.Assert(_isDirty);

            if (_controlPoint1 == new Point(0, 0)
                && _controlPoint2 == new Point(1, 1))
            {
                _isSpecified = false;
            }
            else
            {
                _isSpecified = true;

                _parameter = 0;

                _Bx = 3 * _controlPoint1.X;
                _Cx = 3 * _controlPoint2.X;
                _Cx_Bx = 2 * (_Cx - _Bx);
                _three_Cx = 3 - _Cx;

                _By = 3 * _controlPoint1.Y;
                _Cy = 3 * _controlPoint2.Y;
            }

            _isDirty = false;
        }

        private static double GetBezierValue(double b, double c, double t)
        {
            double s = 1.0 - t;
            double t2 = t * t;

            return b * t * s * s + c * t2 * s + t2 * t;
        }

        private void GetXAndDx(double t, out double x, out double dx)
        {
            Debug.Assert(_isSpecified);

            double s = 1.0 - t;
            double t2 = t * t;
            double s2 = s * s;

            x = _Bx * t * s2 + _Cx * t2 * s + t2 * t;
            dx = _Bx * s2 + _Cx_Bx * s * t + _three_Cx * t2;
        }

        private void SetParameterFromX(double time)
        {
            Debug.Assert(_isSpecified);

            double bottom = 0;
            double top = 1;

            if (time == 0)
            {
                _parameter = 0;
            }
            else if (time == 1)
            {
                _parameter = 1;
            }
            else
            {
                while (top - bottom > fuzz)
                {
                    double x, dx, absdx;

                    GetXAndDx(_parameter, out x, out dx);
                    absdx = Math.Abs(dx);

                    if (x > time)
                    {
                        top = _parameter;
                    }
                    else
                    {
                        bottom = _parameter;
                    }

                    if (Math.Abs(x - time) < accuracy * absdx)
                    {
                        break;
                    }

                    if (absdx > fuzz)
                    {
                        double next = _parameter - (x - time) / dx;

                        if (next >= top)
                        {
                            _parameter = (_parameter + top) / 2;
                        }
                        else if (next <= bottom)
                        {
                            _parameter = (_parameter + bottom) / 2;
                        }
                        else
                        {
                            _parameter = next;
                        }
                    }
                    else
                    {
                        _parameter = (bottom + top) / 2;
                    }
                }
            }
        }

        private void CloneCommon(KeySpline sourceKeySpline)
        {
            _controlPoint1 = sourceKeySpline._controlPoint1;
            _controlPoint2 = sourceKeySpline._controlPoint2;
            _isDirty = true;
        }

        #endregion

        #region IFormattable

        public override string ToString()
        {
            ReadPreamble();

            return InternalConvertToString(null, null);
        }

        public string ToString(IFormatProvider formatProvider)
        {
            ReadPreamble();

            return InternalConvertToString(null, formatProvider);
        }

        string IFormattable.ToString(string format, IFormatProvider formatProvider)
        {
            ReadPreamble();

            return InternalConvertToString(format, formatProvider);
        }

        internal string InternalConvertToString(string format, IFormatProvider formatProvider)
        {
            char separator = MS.Internal.TokenizerHelper.GetNumericListSeparator(formatProvider);

            return string.Format(
                formatProvider,
                "{1}{0}{2}",
                separator,
                _controlPoint1,
                _controlPoint2);
        }

        #endregion

        #region Data

        private Point _controlPoint1;
        private Point _controlPoint2;
        private bool _isSpecified;
        private bool _isDirty;

        private double _parameter;

        private double _Bx;
        private double _Cx;
        private double _Cx_Bx;
        private double _three_Cx;

        private double _By;
        private double _Cy;

        private const double accuracy = .001;
        private const double fuzz = .000001;

        #endregion
    }
}
